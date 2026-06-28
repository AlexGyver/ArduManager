# ArduManager
Кастомный менеджер Arduino-библиотек и плат для Windows. Работает напрямую с GitHub, а не с экосистемой Arduino, которая заблокировала доступ пользователям из России

Скачать свежую версию:
- Windows 64 bit: [релиз](https://github.com/AlexGyver/ArduManager/releases/latest/download/ArduManager-x64.exe), [Яндекс.Диск](https://disk.yandex.ru/d/bZ77gGHrPNZJBw)
- Windows 32 bit: [релиз](https://github.com/AlexGyver/ArduManager/releases/latest/download/ArduManager-x86.exe), [Яндекс.Диск](https://disk.yandex.ru/d/9WKubhTYC0kM2g)

Ссылки на перезаливы Arduino IDE есть в уроках:
- [Arduino IDE v1](https://alexgyver.ru/lessons/arduino-ide/)
- [Arduino IDE v2](https://alexgyver.ru/lessons/arduino-ide-v2/)

Возможности:
- Работа напрямую с GitHub-списком библиотек в обход Arduino
- Рекурсивная установка зависимостей
- Сканирование и менеджмент установленных библиотек
- Проверка обновлений
- Установка и обновление плат

![ardu](/arduman.png)

## GitHub token
Токен необязателен. Он нужен только для повышения лимитов GitHub API. Когда может понадобиться:

- Установлено много библиотек
- Часто проверяются обновления
- Часто открываются dropdown-ы версий
- Часто ставятся библиотеки с зависимостями
- GitHub начал отвечать ошибкой rate limit

Без авторизации GitHub REST API обычно ограничен 60 запросами в час на IP. С Personal Access Token лимит для пользователя обычно выше — 5000 запросов в час.

Где взять токен:

1. Открой GitHub.
2. Перейди в **Settings** → **Developer settings** → **Personal access tokens**.
3. Лучше создать **Fine-grained token**.
4. Для публичных репозиториев специальные права обычно не нужны. Если GitHub требует выбрать permissions, выбирай минимальные read-only permissions.
5. Скопируй токен и вставь его в настройках приложения.

Официальная документация GitHub:

- Managing your personal access tokens: https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens
- Rate limits for the REST API: https://docs.github.com/en/rest/using-the-rest-api/rate-limits-for-the-rest-api

## Ограничения плат
- Не все файлы плат и платформ лежат на GitHub, где-то скачивание может производиться с заблокированных для РФ ресурсов. В этом случае нужно делать свой index.json и размещать файлы в доступных хранилищах. Данная утилита просто качает файлы по указанным ссылкам

## Ограничения библиотек
- Версии библиотек берутся из GitHub tags - не все репозитории имеют аккуратные tags/releases, что-то может пойти не так
- Если dependency name сильно отличается от repo name, автоматический поиск зависимости может не сработать

## Запуск исходника
Нужен [SDK .NET 8](https://dotnet.microsoft.com/ru-ru/download/dotnet/8.0). Далее запускаем `dotnet restore && dotnet run --project src/ArduManager.App` или `run.bat` из репозитория.
