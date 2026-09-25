# Локальная установка Photon Fusion

Версия проекта: Unity 6000.3.9f1, Fusion **2.1.3 Stable Build 2390**.

Этот Git-репозиторий публичный. Сам Photon Fusion SDK и файл `PhotonAppSettings.asset` с App ID не публикуются в нём. Они остаются локально в `Assets/Photon/`. В Git хранятся настройки Unity, которые нужны после импорта SDK: зависимость `Mono.Cecil` и define symbols Fusion.

## Установка в другом checkout

1. Войти в свой аккаунт Photon и скачать [Fusion SDK 2.1.3](https://doc.photonengine.com/fusion/v2/getting-started/sdk-download).
2. Открыть проект в Unity 6000.3.9f1. Импортировать `.unitypackage` через `Assets → Import Package → Custom Package`, выбрав все файлы. Дождаться компиляции.
3. Открыть `Tools → Fusion → Realtime Settings` и указать App ID приложения **Fusion 2** в поле `App Id Fusion`. Для этого проекта используется приложение `River Online`; сам ID нужно взять из Photon Dashboard или у владельца проекта. Прежнее приложение `River` имеет тип **Realtime** и вызывает ошибку `Plugin Mismatch` при запуске Fusion.
4. Проверить, что созданы `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset` и `NetworkProjectConfig.fusion`, а в Unity Console нет ошибок компиляции.

На текущем рабочем компьютере SDK импортирован и App ID `River Online` настроен локально. Dashboard показывает лимит **20 CCU** для него. До релизного правила «до 100 игроков онлайн» потребуется увеличить лимит приложения. Входящий в SDK Fusion Hub не включает сетевой режим автоматически: Shared Mode выбирается в коде через `GameMode.Shared`. Отдельный WebGL smoke test успешно соединил два браузерных клиента в одной комнате (2/10) локально и в черновике Яндекс Игр 2026-09-25. Для повторной сборки используйте release WebGL: development-вариант превысил ограничение Яндекса по размеру распакованных файлов. Подробности в [проверке черновика](YANDEX_GAMES_PHOTON_DRAFT.md).

Источники: [официальная загрузка и требования SDK](https://doc.photonengine.com/fusion/v2/getting-started/sdk-download), [настройка App ID](https://doc.photonengine.com/fusion/v2/getting-started/appid-instructions), [условия лицензии Photon](https://www.photonengine.com/terms/licenseterms).
