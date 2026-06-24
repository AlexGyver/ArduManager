using System.Text.RegularExpressions;
using ArdulibsManager.Models;

namespace ArdulibsManager.Services;

public static class VersionService
{
    public static IReadOnlyList<GithubTag> SortTags(IEnumerable<GithubTag> tags)
        => tags.Select(x => new { Tag = x, Version = ParseVersionOrNull(x.Name) })
               .OrderByDescending(x => x.Version is not null)
               .ThenByDescending(x => x.Version)
               .ThenByDescending(x => x.Tag.Name, StringComparer.OrdinalIgnoreCase)
               .Select(x => x.Tag)
               .ToList();

    public static bool IsNewer(string? candidate, string? current)
    {
        var a = ParseVersionOrNull(candidate);
        var b = ParseVersionOrNull(current);

        // Для автообновлений сравниваем только теги, похожие на нормальные версии.
        // Иначе репозиторий с посторонними тегами вроде TM74HC595_Gyver может ошибочно
        // выглядеть как обновление для локальной версии 1.0.
        if (a is null || b is null) return false;

        return a > b;
    }

    public static bool IsSameVersion(string? a, string? b)
    {
        var va = ParseVersionOrNull(a);
        var vb = ParseVersionOrNull(b);
        if (va is not null && vb is not null) return va == vb;
        return Normalize(a).Equals(Normalize(b), StringComparison.OrdinalIgnoreCase);
    }

    public static bool LooksLikeVersion(string? value) => ParseVersionOrNull(value) is not null;

    private static string Normalize(string? value)
        => (value ?? string.Empty).Trim().TrimStart('v', 'V');

    private static Version? ParseVersionOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = Normalize(value);
        var match = Regex.Match(normalized, @"(?<major>\d+)\.(?<minor>\d+)(?:\.(?<patch>\d+))?(?:\.(?<build>\d+))?");
        if (!match.Success) return null;
        int major = int.Parse(match.Groups["major"].Value);
        int minor = int.Parse(match.Groups["minor"].Value);
        int patch = match.Groups["patch"].Success ? int.Parse(match.Groups["patch"].Value) : 0;
        int build = match.Groups["build"].Success ? int.Parse(match.Groups["build"].Value) : 0;
        return new Version(major, minor, patch, build);
    }
}
