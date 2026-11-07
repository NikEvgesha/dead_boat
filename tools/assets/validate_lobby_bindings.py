"""Validate explicit serialized island bindings without opening or modifying Unity.

This checks YAML reference integrity, not physics, native deserialization or services.
Use --required-capacity 10 to also reject the currently incomplete eight-plot layout.
"""
import argparse
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]


def validate(text, guid, capacity=None):
    headers = list(re.finditer(r"^--- !u!(\d+) &(-?\d+)[^\n]*\n", text, re.M))
    blocks = {}
    for i, match in enumerate(headers):
        ident = int(match[2])
        if ident in blocks:
            raise ValueError("Duplicate YAML fileID: " + str(ident))
        blocks[ident] = (int(match[1]), text[match.end():headers[i + 1].start() if i + 1 < len(headers) else len(text)])

    def reference(block, key):
        match = re.search(r"^\s*" + re.escape(key) + r": \{fileID: (-?\d+)\}", block, re.M)
        if not match:
            raise ValueError("Missing reference: " + key)
        return int(match[1])

    def get(ident, kind):
        if ident not in blocks or blocks[ident][0] != kind:
            raise ValueError("Missing/wrong-type YAML reference: " + str(ident))
        return blocks[ident][1]

    registries = [(ident, body) for ident, (kind, body) in blocks.items()
                  if kind == 114 and "guid: " + guid + "," in body]
    if len(registries) != 1:
        raise ValueError("Expected exactly one island binding component")
    registry_id, body = registries[0]
    owner = reference(body, "m_GameObject")
    owner_body = get(owner, 1)
    component_ids = [int(x) for x in re.findall(r"component: \{fileID: (\d+)\}", owner_body)]
    if component_ids.count(registry_id) != 1 or blocks[component_ids[0]][0] != 4:
        raise ValueError("Registry ownership or first Transform component is invalid")
    root = reference(body, "visualRoot")
    if reference(get(root, 4), "m_GameObject") != owner:
        raise ValueError("Registry must be attached to its visual root")

    def active_under(point, ancestor):
        visited = set()
        current = point
        under = False
        while current:
            if current in visited:
                raise ValueError("Transform cycle")
            visited.add(current)
            under |= current == ancestor
            transform = get(current, 4)
            go = get(reference(transform, "m_GameObject"), 1)
            if not re.search(r"^  m_IsActive: 1$", go, re.M):
                raise ValueError("Binding points into an inactive hierarchy: " + str(point))
            current = reference(transform, "m_Father")
        if not under:
            raise ValueError("Binding is outside its owning hierarchy: " + str(point))

    active_under(root, root)
    arrivals_body = body.split("  centralArrivals:\n", 1)[1].split("  plots:\n", 1)[0]
    arrivals = [int(x) for x in re.findall(r"fileID: (\d+)", arrivals_body)]
    if not arrivals or len(set(arrivals)) != len(arrivals):
        raise ValueError("Missing/duplicate arrivals")
    for point in arrivals:
        active_under(point, root)
    plot_body = body.split("  plots:\n", 1)[1].split("  services:\n", 1)[0]
    plots = re.findall(r"^  - id: (\d+)\n(.*?)(?=^  - id:|\Z)", plot_body, re.M | re.S)
    ids, roots = set(), set()
    for ident, fields in plots:
        plot_root = reference(fields, "root")
        if int(ident) < 1 or ident in ids or plot_root in roots:
            raise ValueError("Invalid/duplicate personal plot")
        ids.add(ident)
        roots.add(plot_root)
        active_under(plot_root, root)
        for key in ("arrival", "animalSpawn", "boatSpawn", "departure", "income"):
            active_under(reference(fields, key), plot_root)
    if not plots:
        raise ValueError("No personal plots")
    services = re.findall(r"^  - service: (\d+)\n    interaction: \{fileID: (\d+)\}", body, re.M)
    if sorted(int(kind) for kind, _ in services) != list(range(6)):
        raise ValueError("Service roles must occur exactly once")
    for _, point in services:
        active_under(int(point), root)
    result = {"plots": len(plots), "arrivals": len(arrivals), "service_anchors": len(services),
              "scope": "Explicit YAML integrity only; no Unity/physics/ownership/service execution"}
    if capacity is not None and len(plots) < capacity:
        raise ValueError("Only " + str(len(plots)) + " personal plots for capacity " + str(capacity))
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--required-capacity", type=int)
    args = parser.parse_args()
    scene = ROOT / "Assets/Scenes/LobbyIslandWorkshop.unity"
    meta = ROOT / "Assets/Scripts/Lobby/LobbyIslandBindings.cs.meta"
    guid = re.search(r"guid: (\w+)", meta.read_text(encoding="utf-8"))[1]
    print(json.dumps(validate(scene.read_text(encoding="utf-8-sig"), guid, args.required_capacity), indent=2))


if __name__ == "__main__":
    main()
