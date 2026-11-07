# DB-ON-139: проверки реестра новой сцены

- Unity Roslyn: полный runtime Assembly-CSharp по текущим rsp/refs/defines/analyzers с новыми LobbySceneRole и LobbyIslandBindings — exit0. Логи: `Logs/LobbyBindingsCompile`. Runtime DLL Unity не заменялась.
- `python tools/assets/validate_lobby_bindings.py` — PASS: 8 участков, 8 arrival, 6 ролей сервисов; ссылки указывают на Transform активной иерархии внутри нужного участка/корня. Это проверка явных YAML-записей.
- Проверка capacity10 ожидаемо отвергнута: Only 8 personal plots for capacity 10. Модель комнаты не уменьшали.
- Повреждённая в памяти ссылка departure на отсутствующий/неверный тип fileID=1 отвергнута; файл сцены этим отрицательным тестом не менялся.
- После удаления добавленного MonoBehaviour и единственной ссылки m_Component SHA256 исходной сцены совпадает. Итоговая сцена: `0228f063a354800871d3bf5b230ba6d0a6ce3d3b2ceb0d3016b299bd227c1c2d`.
- Канбан: OK, 145 карточек. Новый код не исполнялся в native Unity/Play/Photon; импорт, фактический телепорт, коллизии, UI и работа сервисов не приняты. Player Build не запускался.

Далее ИИ — проверить игровые коллизии и проходы, добавить недостающие зоны и распределение, затем подключать старые обработчики к новым точкам. Владелец — позже общий игровой тест. DB-ON-139 остаётся in_progress.
