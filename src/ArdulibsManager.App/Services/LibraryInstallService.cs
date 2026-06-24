using System.IO.Compression;
using ArdulibsManager.Models;

namespace ArdulibsManager.Services;

public sealed class LibraryInstallService
{
    private readonly GithubService _github;
    private readonly SettingsService _settings;

    public LibraryInstallService(GithubService github, SettingsService settings)
    {
        _github = github;
        _settings = settings;
    }

    public async Task<InstalledLibrary> InstallAsync(GithubRepository repo, string tag, string librariesPath, IProgress<string>? log = null, string? targetPathOverride = null, CancellationToken ct = default)
    {
        Directory.CreateDirectory(librariesPath);
        var librariesRoot = Path.GetFullPath(librariesPath);
        var tempRoot = Path.Combine(Path.GetTempPath(), "ArdulibsManager", Guid.NewGuid().ToString("N"));
        var stagingPath = Path.Combine(librariesRoot, ".ardulibs-staging-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var zipPath = Path.Combine(tempRoot, "repo.zip");
        var extractPath = Path.Combine(tempRoot, "extract");

        try
        {
            log?.Report($"Скачивание {repo.FullName} {tag}...");
            await _github.DownloadZipballAsync(repo, tag, zipPath, ct);

            log?.Report("Распаковка...");
            ZipFile.ExtractToDirectory(zipPath, extractPath);

            var propsPath = Directory.EnumerateFiles(extractPath, "library.properties", SearchOption.AllDirectories)
                .OrderBy(p => p.Count(ch => ch == Path.DirectorySeparatorChar))
                .FirstOrDefault();

            if (propsPath is null)
                throw new InvalidOperationException("В архиве не найден library.properties. Это может быть не Arduino-библиотека.");

            var libRoot = Path.GetDirectoryName(propsPath)!;
            var props = LibraryProperties.Parse(await File.ReadAllTextAsync(propsPath, ct));
            var folderName = SanitizeFolderName(props.Name ?? repo.Name);
            var targetPath = !string.IsNullOrWhiteSpace(targetPathOverride)
                ? Path.GetFullPath(targetPathOverride)
                : Path.Combine(librariesRoot, folderName);

            EnsureInsideLibraries(librariesRoot, targetPath);

            log?.Report("Подготовка новой версии...");
            CopyDirectory(libRoot, stagingPath);

            var stagedProps = Path.Combine(stagingPath, "library.properties");
            if (!File.Exists(stagedProps))
                throw new InvalidOperationException("Ошибка подготовки: library.properties не найден в подготовленной папке.");

            log?.Report("Атомарная замена библиотеки...");
            ReplaceDirectoryAtomic(stagingPath, targetPath, librariesRoot, log);

            log?.Report("Готово.");
            return new InstalledLibrary
            {
                Name = props.Name ?? repo.Name,
                Version = props.Version ?? tag,
                Maintainer = props.Maintainer,
                Url = props.Url ?? repo.Url,
                LocalPath = targetPath,
                RepositoryFullName = repo.FullName,
                Status = "Установлено"
            };
        }
        finally
        {
            TryDeleteDirectory(tempRoot);
            TryDeleteDirectory(stagingPath);
        }
    }

    public Task RemoveAsync(InstalledLibrary lib)
    {
        var librariesRoot = Path.GetFullPath(_settings.Current.LibrariesPath);
        var targetPath = Path.GetFullPath(lib.LocalPath);
        EnsureInsideLibraries(librariesRoot, targetPath);

        var propsPath = Path.Combine(targetPath, "library.properties");
        if (!File.Exists(propsPath))
            throw new InvalidOperationException("Удаление отменено: в папке библиотеки не найден library.properties.");

        if (Directory.Exists(targetPath))
            Directory.Delete(targetPath, recursive: true);

        return Task.CompletedTask;
    }

    private static void ReplaceDirectoryAtomic(string stagingPath, string targetPath, string librariesRoot, IProgress<string>? log)
    {
        var oldPath = Path.Combine(librariesRoot, ".ardulibs-old-" + Path.GetFileName(targetPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) + "-" + Guid.NewGuid().ToString("N"));

        if (!Directory.Exists(targetPath))
        {
            Directory.Move(stagingPath, targetPath);
            return;
        }

        Directory.Move(targetPath, oldPath);
        try
        {
            Directory.Move(stagingPath, targetPath);
        }
        catch
        {
            TryDeleteDirectory(targetPath);
            if (Directory.Exists(oldPath) && !Directory.Exists(targetPath))
                Directory.Move(oldPath, targetPath);
            throw;
        }

        try
        {
            Directory.Delete(oldPath, recursive: true);
        }
        catch (Exception ex)
        {
            log?.Report("Новая версия установлена, но временную старую папку не удалось удалить: " + ex.Message);
        }
    }

    private static void EnsureInsideLibraries(string librariesRoot, string targetPath)
    {
        librariesRoot = Path.GetFullPath(librariesRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        targetPath = Path.GetFullPath(targetPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!targetPath.StartsWith(librariesRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Операция отменена: путь библиотеки находится вне папки Arduino libraries.");
    }

    private static string SanitizeFolderName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name.Replace(' ', '_');
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dir.Replace(source, destination));
        }
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(source, destination), overwrite: true);
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // best effort cleanup
        }
    }
}
