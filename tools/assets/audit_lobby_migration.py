"""Read-only inventory of explicit Unity YAML records, not resolved prefab/runtime state.
Writes its JSON report only under the repository Logs directory.
"""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
COLLIDERS = {64: "MeshCollider", 65: "BoxCollider", 135: "SphereCollider", 136: "CapsuleCollider", 154: "TerrainCollider"}


def field(text, name):
    match = re.search(r"^  " + re.escape(name) + r": (.*)$", text, re.M)
    return match.group(1).rstrip("\r") if match else None


def ref(text, name):
    value = field(text, name)
    match = re.search(r"fileID: (-?\d+)", value or "")
    return int(match.group(1)) if match else 0


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--scene", type=Path, default=ROOT / "Assets/Scenes/LobbyIslandWorkshop.unity")
    parser.add_argument("--output", type=Path, default=ROOT / "Logs/lobby-migration-audit.json")
    args = parser.parse_args()
    scene = args.scene.resolve()
    output = args.output.resolve()
    if not output.is_relative_to((ROOT / "Logs").resolve()):
        raise ValueError("Audit output must stay under the repository Logs directory")
    data = scene.read_bytes()
    text = data.decode("utf-8-sig")
    headers = list(re.finditer(r"^--- !u!(\d+) &(-?\d+)[^\n]*\n", text, re.M))
    objects, transforms, go_transform, components, prefabs = {}, {}, {}, [], []
    for i, match in enumerate(headers):
        kind, ident = int(match.group(1)), int(match.group(2))
        if kind not in {1, 4, 224, 114, 20, 54, 1001} | set(COLLIDERS):
            continue
        block = text[match.end():headers[i + 1].start() if i + 1 < len(headers) else len(text)]
        if kind == 1:
            name = field(block, "m_Name")
            if name is not None:
                objects[ident] = {"name": name.strip('"'), "active_self": field(block, "m_IsActive") == "1"}
        elif kind in (4, 224):
            go = ref(block, "m_GameObject")
            transforms[ident] = {"go": go, "parent": ref(block, "m_Father")}
            if go:
                go_transform[go] = ident
        elif kind == 1001:
            parent = re.search(r"m_TransformParent: \{fileID: (-?\d+)", block)
            prefabs.append(int(parent.group(1)) if parent else 0)
        else:
            components.append({"class": kind, "go": ref(block, "m_GameObject")})

    def chain(go):
        visited = set()
        while go and go not in visited:
            visited.add(go)
            yield go
            if go not in go_transform:
                yield None
                break
            node = transforms.get(go_transform[go], {})
            parent = node.get("parent", 0)
            if not parent:
                break
            go = transforms.get(parent, {}).get("go", 0)
            if not go:
                yield -parent
                break

    def describe(go):
        ids = list(chain(go))
        names = [objects[x]["name"] if x in objects else "<unresolved:" + str(x) + ">" for x in reversed(ids)]
        active = all(objects[x]["active_self"] for x in ids if x in objects)
        if active and any(x not in objects for x in ids):
            active = None
        return {"file_id": go, "path": "/".join(names), "explicit_hierarchy_active": active}

    roots = [go for go in objects if go in go_transform and transforms[go_transform[go]]["parent"] == 0]
    prototype = [go for go, row in objects.items() if row["name"] == "Lobby_Island_Visual_Prototype"]
    proto = prototype[0] if len(prototype) == 1 else None
    def under(go):
        return proto is not None and proto in chain(go)

    plots = [describe(go) for go, row in objects.items() if re.fullmatch(r"Personal_Island_\d{2}", row["name"])]
    anchors = [describe(go) for go, row in objects.items() if re.search(r"(?:^|_)Anchor(?:\d|_|$)", row["name"]) and under(go)]
    anchor_paths = Counter(row["path"] for row in anchors)
    ambiguous = [{"path": path, "count": count, "file_ids": [row["file_id"] for row in anchors if row["path"] == path]}
                 for path, count in sorted(anchor_paths.items()) if count > 1]
    inline = Counter(COLLIDERS[c["class"]] for c in components if c["class"] in COLLIDERS and under(c["go"]))
    build_text = (ROOT / "ProjectSettings/EditorBuildSettings.asset").read_text(encoding="utf-8")
    build_scenes = re.findall(r"- enabled: ([01])\s+path: ([^\r\n]+)", build_text)
    report = {
        "scope": "Explicit scene records only; prefab inheritance, stripped records and runtime are not resolved. No native Unity verification.",
        "scene": str(scene.relative_to(ROOT)), "sha256": hashlib.sha256(data).hexdigest(),
        "scene_bytes": len(data), "named_gameobjects": len(objects),
        "build_scenes": [{"enabled": enabled == "1", "path": path} for enabled, path in build_scenes],
        "in_enabled_build_scenes": any(enabled == "1" and path == scene.relative_to(ROOT).as_posix() for enabled, path in build_scenes),
        "explicit_roots": sorted((describe(go) for go in roots), key=lambda x: x["path"]),
        "prototype_roots": [describe(go) for go in prototype],
        "personal_plots": sorted(plots, key=lambda x: x["path"]),
        "inline_colliders_under_prototype": dict(inline),
        "inline_collider_objects": [dict(describe(c["go"]), collider=COLLIDERS[c["class"]]) for c in components if c["class"] in COLLIDERS and under(c["go"])],
        "inline_rigidbodies_under_prototype": sum(c["class"] == 54 and under(c["go"]) for c in components),
        "prefab_instances_parented_under_prototype": sum(under(transforms.get(t, {}).get("go", 0)) for t in prefabs),
        "anchors": sorted(anchors, key=lambda x: x["path"]),
        "ambiguous_anchor_paths": ambiguous,
    }
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"report": str(output.relative_to(ROOT)), "plots": len(plots), "anchors": len(anchors), "ambiguous_anchor_paths": len(ambiguous), "inline_colliders": dict(inline), "inline_rigidbodies": report["inline_rigidbodies_under_prototype"], "prefab_instances": report["prefab_instances_parented_under_prototype"], "enabled_in_build": report["in_enabled_build_scenes"], "roots": report["explicit_roots"]}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
