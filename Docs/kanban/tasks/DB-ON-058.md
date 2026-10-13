---
id: DB-ON-058
title: Обработать браузерный отказ захвата мыши без Unity alert
status: in_review
when: now
ai: true
---

## Контекст

Владелец прислал периодический Unity alert с Chromium UnknownError. IAB уже показывал такой же Unhandled rejection при кликах/возврате в игру. В готовом framework.js requestPointerLock вызывается без обработки возвращаемого Promise. Конкретный стек браузерной ошибки пока не подтвердил API; защиту проверять по диагностике и настоящему WebGL, не объявлять любую UnknownError ошибкой курсора.

## Готово, когда

- Ожидаемый DOMException из canvas.requestPointerLock обрабатывается и логируется без глобального Unity alert.
- Не подавляются прочие JS ошибки; сохранены настоящий статус захвата мыши и возможность повторить клик.
- Promise/legacy void API, параллельные запросы, синхронный отказ и повтор проверены.
- Новый скрипт установлен до Unity loader, не дублируется при повторной упаковке.
- Проверено в WebGL; SDK не изменён. Приёмка владельца — до done.

## Подсказки ИИ

Собственная обвязка tools/online-webgl/pointer-lock-guard.js; подключение в MirraCloudSmokeBuild.AddWebViewScripts. Можно переупаковать готовый eee34894, поскольку C#/wasm игры не меняются. Не редактировать Mirra/Photon или генерированный framework.js. Не перехватывать глобально alert/unhandledrejection.

## Источники

- https://developer.mozilla.org/en-US/docs/Web/API/Element/requestPointerLock
- Assets/Igrodelnya/Managers/ControlManager.cs
- Docs/COOP_RECOVERY_20261012.md

Следующий шаг — ИИ: проверки JS, подключение в готовую сборку, черновик и журнал браузера. Владелец: проверить захват/отпускание курсора при меню, рекламе и возврате.


## Реализация и проверки 2026-10-13

Добавлен узкий guard для requestPointerLock на unity-canvas: обработка ожидаемых DOMException, объединение параллельных запросов, сохранение исходного Promise/this/options, повтор после отказа, поддержка legacy void. Не перехватывает alert/unhandledrejection глобально; неожиданные ошибки сохраняют прежнее поведение. Mirra/Photon SDK и framework.js не редактировались.

`node tools/tests/test_pointer_lock_guard.cjs` PASS: отсутствие unhandledRejection при UnknownError, повтор/синхронный отказ/legacy/идемпотентность/объединение/контекст, неожиданная ошибка по-прежнему порождает unhandledRejection. Настоящий Unity AddWebViewScripts выполнен дважды на готовом eee34894: guard скопирован до unityApp.js, по одному экземпляру каждого скрипта. Изменился только index.html и добавился guard; новая компиляция payload не нужна. Браузерную причину подтверждать по warning guard, а не по одному общему тексту UnknownError.

Следующий шаг — ИИ: архив/черновик и реальная диагностика отказа захвата; владелец: проверить меню/рекламу/возврат и работу мыши. До приёмки in_review.
