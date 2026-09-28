"""Run with: python -m unittest discover tools/kanban -p test_server.py"""

import json
import tempfile
import threading
import unittest
from pathlib import Path
from unittest.mock import patch
from urllib.error import HTTPError
from urllib.request import Request, urlopen

import server


class KanbanServerTest(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.tasks = Path(self.directory.name)
        self.task = {
            "id": "DB-ON-001", "title": "Первый тест", "status": "ready",
            "when": "now", "ai": "true", "Контекст": "Проверить запись.",
            "Готово, когда": "- Состояние сохранено.",
            "Подсказки ИИ": "Проверить ревизию.", "Источники": "- `README.md`",
        }
        (self.tasks / "DB-ON-001.md").write_bytes(server.serialize(self.task))
        self.patch = patch.object(server, "TASKS", self.tasks)
        self.patch.start()
        self.http = server.HTTPServer(("127.0.0.1", 0), server.Handler)
        self.thread = threading.Thread(target=self.http.serve_forever, daemon=True)
        self.thread.start()
        self.base = f"http://127.0.0.1:{self.http.server_port}"

    def tearDown(self):
        self.http.shutdown()
        self.http.server_close()
        self.thread.join(timeout=3)
        self.patch.stop()
        self.directory.cleanup()

    def call(self, path, method="GET", data=None, origin=True):
        headers = {"Content-Type": "application/json"}
        if origin:
            headers["Origin"] = self.base
        request = Request(self.base + path, data=json.dumps(data).encode() if data is not None else None,
                          headers=headers, method=method)
        try:
            with urlopen(request) as response:
                return response.status, json.load(response)
        except HTTPError as error:
            return error.code, json.load(error)

    def test_edit_conflict_and_create(self):
        status, original = self.call("/api/tasks")
        self.assertEqual((status, len(original)), (200, 1))
        card = original[0]
        updated = {**card, "status": "in_progress"}
        status, saved = self.call("/api/tasks/DB-ON-001", "PUT", updated)
        self.assertEqual((status, saved["status"]), (200, "in_progress"))
        self.assertEqual(server.parse_task(self.tasks / "DB-ON-001.md")["status"], "in_progress")
        self.assertEqual(self.call("/api/tasks/DB-ON-001", "PUT", updated)[0], 409)
        self.assertEqual(self.call("/api/tasks/DB-ON-001", "PUT", saved, origin=False)[0], 403)
        new_task = {**self.task, "title": "Вторая задача"}
        status, created = self.call("/api/tasks", "POST", new_task)
        self.assertEqual((status, created["id"]), (201, "DB-ON-002"))
        self.assertEqual(len(server.all_tasks()), 2)

    def test_crlf_checkout_and_unresolved_merge(self):
        path = self.tasks / "DB-ON-001.md"
        path.write_bytes(path.read_bytes().replace(b"\n", b"\r\n"))
        self.assertEqual(server.parse_task(path)["id"], "DB-ON-001")
        path.write_bytes(path.read_bytes() + b"\n<<<<<<< HEAD\n=======\n>>>>>>> branch\n")
        with self.assertRaisesRegex(ValueError, "unresolved Git conflict"):
            server.parse_task(path)


if __name__ == "__main__":
    unittest.main()
