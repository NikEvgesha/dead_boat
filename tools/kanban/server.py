#!/usr/bin/env python3
"""Local, dependency-free Kanban for Docs/kanban/tasks/*.md."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import tempfile
import threading
import webbrowser
from http.server import BaseHTTPRequestHandler, HTTPServer
from pathlib import Path
from urllib.parse import unquote


ROOT = Path(__file__).resolve().parents[2]
TASKS = ROOT / "Docs" / "kanban" / "tasks"
INDEX = Path(__file__).with_name("index.html")
STATUSES = ("backlog", "ready", "in_progress", "in_review", "done")
HORIZONS = ("now", "next", "later")
SECTIONS = ("Контекст", "Готово, когда", "Подсказки ИИ", "Источники")
ID_RE = re.compile(r"DB-ON-\d{3,}")
SECTION_RE = re.compile(r"^## (Контекст|Готово, когда|Подсказки ИИ|Источники)\s*$", re.M)
LOCK = threading.Lock()


def revision(raw: bytes) -> str:
    return hashlib.sha256(raw).hexdigest()


def parse_task(path: Path) -> dict:
    raw = path.read_bytes()
    text = raw.decode("utf-8-sig").replace("\r\n", "\n")
    if any(marker in text for marker in ("<<<<<<< ", "=======\n", ">>>>>>> ")):
        raise ValueError(f"{path.name}: unresolved Git conflict markers")
    if not text.startswith("---\n"):
        raise ValueError(f"{path.name}: missing YAML header")
    parts = text.split("\n---\n", 1)
    if len(parts) != 2:
        raise ValueError(f"{path.name}: unterminated YAML header")
    fields = {}
    for line in parts[0][4:].splitlines():
        if not line.strip():
            continue
        if ":" not in line:
            raise ValueError(f"{path.name}: invalid header line: {line}")
        key, value = line.split(":", 1)
        fields[key.strip()] = value.strip()
    task = {key: fields.get(key, "") for key in ("id", "title", "status", "when", "ai")}
    matches = list(SECTION_RE.finditer(parts[1]))
    if len(matches) != len(SECTIONS) or tuple(m.group(1) for m in matches) != SECTIONS:
        raise ValueError(f"{path.name}: expected sections {', '.join(SECTIONS)}")
    for i, match in enumerate(matches):
        end = matches[i + 1].start() if i + 1 < len(matches) else len(parts[1])
        task[match.group(1)] = parts[1][match.end():end].strip()
    task["revision"] = revision(raw)
    validate(task, path.name)
    if path.stem != task["id"]:
        raise ValueError(f"{path.name}: filename and ID differ")
    return task


def validate(task: dict, name: str = "task") -> None:
    if not ID_RE.fullmatch(task.get("id", "")):
        raise ValueError(f"{name}: invalid ID")
    if not isinstance(task.get("title"), str) or not task["title"].strip() or "\n" in task["title"]:
        raise ValueError(f"{name}: title must be one nonempty line")
    if len(task["title"]) > 160 or ": " in task["title"][:2]:
        raise ValueError(f"{name}: title is too long or invalid")
    if task.get("status") not in STATUSES or task.get("when") not in HORIZONS:
        raise ValueError(f"{name}: invalid status or horizon")
    if task.get("ai") not in ("true", "false"):
        raise ValueError(f"{name}: ai must be true or false")
    for section in SECTIONS:
        value = task.get(section)
        if not isinstance(value, str) or not value.strip():
            raise ValueError(f"{name}: missing {section}")
        if SECTION_RE.search(value):
            raise ValueError(f"{name}: unexpected section heading inside {section}")


def serialize(task: dict) -> bytes:
    validate(task)
    header = "\n".join(f"{key}: {task[key]}" for key in ("id", "title", "status", "when", "ai"))
    body = "\n\n".join(f"## {section}\n\n{task[section].strip()}" for section in SECTIONS)
    return f"---\n{header}\n---\n\n{body}\n".encode("utf-8")


def all_tasks() -> list[dict]:
    tasks = [parse_task(path) for path in sorted(TASKS.glob("DB-ON-*.md"))]
    ids = [task["id"] for task in tasks]
    if len(ids) != len(set(ids)):
        raise ValueError("duplicate task ID")
    return tasks


def atomic_write(path: Path, data: bytes) -> None:
    TASKS.mkdir(parents=True, exist_ok=True)
    fd, name = tempfile.mkstemp(prefix=".kanban-", dir=TASKS)
    try:
        with os.fdopen(fd, "wb") as stream:
            stream.write(data)
        os.replace(name, path)
    finally:
        if os.path.exists(name):
            os.unlink(name)


class Handler(BaseHTTPRequestHandler):
    def _json(self, status: int, value: object) -> None:
        data = json.dumps(value, ensure_ascii=False).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(data)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(data)

    def _body(self) -> dict:
        length = int(self.headers.get("Content-Length", "0"))
        if length <= 0 or length > 100_000:
            raise ValueError("invalid request size")
        value = json.loads(self.rfile.read(length))
        if not isinstance(value, dict):
            raise ValueError("expected JSON object")
        return value

    def _same_origin(self) -> bool:
        origin = self.headers.get("Origin", "")
        expected = f"http://127.0.0.1:{self.server.server_port}"
        return origin == expected

    def do_GET(self) -> None:
        path = unquote(self.path.split("?", 1)[0])
        if path == "/":
            data = INDEX.read_bytes()
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.send_header("Content-Length", str(len(data)))
            self.send_header("Cache-Control", "no-store")
            self.end_headers()
            self.wfile.write(data)
        elif path == "/api/tasks":
            try:
                self._json(200, all_tasks())
            except (ValueError, OSError) as error:
                self._json(500, {"error": str(error)})
        else:
            self._json(404, {"error": "not found"})

    def do_POST(self) -> None:
        if self.path != "/api/tasks":
            self._json(404, {"error": "not found"})
            return
        if not self._same_origin():
            self._json(403, {"error": "origin mismatch"})
            return
        try:
            payload = self._body()
            with LOCK:
                existing = all_tasks()
                number = max((int(task["id"].split("-")[-1]) for task in existing), default=0) + 1
                task = {key: payload.get(key, "") for key in ("title", "status", "when", "ai", *SECTIONS)}
                task["id"] = f"DB-ON-{number:03d}"
                data = serialize(task)
                atomic_write(TASKS / f"{task['id']}.md", data)
                task["revision"] = revision(data)
            self._json(201, task)
        except (ValueError, OSError, json.JSONDecodeError) as error:
            self._json(400, {"error": str(error)})

    def do_PUT(self) -> None:
        match = re.fullmatch(r"/api/tasks/(DB-ON-\d{3,})", self.path)
        if not match:
            self._json(404, {"error": "not found"})
            return
        if not self._same_origin():
            self._json(403, {"error": "origin mismatch"})
            return
        try:
            payload = self._body()
            path = TASKS / f"{match.group(1)}.md"
            with LOCK:
                current = parse_task(path)
                if payload.get("revision") != current["revision"]:
                    self._json(409, {"error": "Карточка изменилась. Обновите доску и повторите правку."})
                    return
                task = {key: payload.get(key, current[key]) for key in ("title", "status", "when", "ai", *SECTIONS)}
                task["id"] = current["id"]
                data = serialize(task)
                atomic_write(path, data)
                task["revision"] = revision(data)
            self._json(200, task)
        except FileNotFoundError:
            self._json(404, {"error": "task not found"})
        except (ValueError, OSError, json.JSONDecodeError) as error:
            self._json(400, {"error": str(error)})


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="validate all Markdown cards and exit")
    parser.add_argument("--port", type=int, default=8765)
    parser.add_argument("--no-browser", action="store_true")
    args = parser.parse_args()
    tasks = all_tasks()
    if args.check:
        print(f"OK: {len(tasks)} tasks")
        return
    server = HTTPServer(("127.0.0.1", args.port), Handler)
    url = f"http://127.0.0.1:{server.server_port}/"
    print(f"Kanban: {url} ({len(tasks)} tasks)", flush=True)
    if not args.no_browser:
        threading.Timer(0.3, lambda: webbrowser.open(url)).start()
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nStopped")
    finally:
        server.server_close()


if __name__ == "__main__":
    main()
