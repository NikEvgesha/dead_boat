# Локальная установка Photon Fusion

Версия проекта: Unity 6000.3.9f1, Fusion **2.1.3 Stable Build 2390**.

Этот Git-репозиторий публичный. Сам Photon Fusion SDK и файл `PhotonAppSettings.asset` с App ID не публикуются в нём. Они остаются локально в `Assets/Photon/`. В Git хранятся настройки Unity, которые нужны после импорта SDK: зависимость `Mono.Cecil` и define symbols Fusion.

## Установка в другом checkout

1. Войти в свой аккаунт Photon и скачать [Fusion SDK 2.1.3](https://doc.photonengine.com/fusion/v2/getting-started/sdk-download).
2. Открыть проект в Unity 6000.3.9f1. Импортировать `.unitypackage` через `Assets → Import Package → Custom Package`, выбрав все файлы. Дождаться компиляции.
3. Открыть `Tools → Fusion → Realtime Settings` и указать App ID приложения **Fusion 2** в поле `App Id Fusion`. Для этого проекта используется приложение `River`; сам ID нужно взять из Photon Dashboard или у владельца проекта.
4. Проверить, что созданы `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset` и `NetworkProjectConfig.fusion`, а в Unity Console нет ошибок компиляции.

На текущем рабочем компьютере SDK импортирован и App ID настроен локально. Текущий Dashboard показывает лимит **20 CCU**. До релизного правила «до 100 игроков онлайн» потребуется увеличить лимит приложения. Входящий в SDK Fusion Hub не включает сетевой режим автоматически: Shared Mode будет выбран в коде через `GameMode.Shared` в первом прототипе.

Источники: [официальная загрузка и требования SDK](https://doc.photonengine.com/fusion/v2/getting-started/sdk-download), [настройка App ID](https://doc.photonengine.com/fusion/v2/getting-started/appid-instructions), [условия лицензии Photon](https://www.photonengine.com/terms/licenseterms).
