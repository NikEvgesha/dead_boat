"""Check proposal data and a toy Blood Pact model. NOT a Unity/gameplay test."""
from collections import deque
import json
from math import isclose
from pathlib import Path
from string import Formatter


def new_health(maximum, current, loss=0.20):
    # Recompute from the unmodified input, never from yesterday's reduced maximum.
    maximum = max(1, int(maximum * (1 - loss) + 0.5))
    return maximum, min(current, maximum)


class BloodPactModel:
    def __init__(self, maximum=80, health=40, fraction=0.04, cap=0.02):
        self.maximum, self.health = maximum, health
        self.fraction, self.cap = fraction, cap
        self.recent = deque()
        self.seen = set()  # Test history, not a proposed unbounded runtime cache.

    def attack(self, now, attack_id, hits, kind="weapon", enemy=True):
        if attack_id in self.seen or kind != "weapon" or not enemy or self.health <= 0:
            return 0.0
        self.seen.add(attack_id)
        while self.recent and self.recent[0][0] <= now - 1:
            self.recent.popleft()
        # hits are (post-mitigation damage, victim HP before hit), confirmed once.
        removed = sum(max(0, min(damage, before)) for damage, before in hits)
        remaining = max(0, self.maximum * self.cap - sum(x[1] for x in self.recent))
        healed = min(removed * self.fraction, remaining, self.maximum - self.health)
        self.health += healed
        if healed > 0:
            self.recent.append((now, healed))
        return healed


def main():
    root = Path(__file__).resolve().parents[2]
    data = json.loads((root / "Docs/design/card-progression-proposal.json").read_text(encoding="utf-8"))
    assert data["status"] == "proposal_not_implemented"
    ids = set()
    for card in data["cards"]:
        assert card["id"] not in ids
        ids.add(card["id"])
        for language in ("ru", "en"):
            assert set(card[language]) == {"title", "description"}
            fields = {name for _, name, _, _ in Formatter().parse(card[language]["description"]) if name}
            assert fields == set(card["parameters"]), (card["id"], language, fields)
            text = card[language]["description"].format(**card["parameters"])
            assert text and "{" not in text and "}" not in text
    assert len(ids) == 12
    assert new_health(100, 100) == (80, 80)
    assert new_health(100, 50) == (80, 50)
    assert new_health(100, 1) == (80, 1)
    assert new_health(1, 1) == (1, 1)
    model = BloodPactModel()
    assert isclose(model.attack(0, 1, [(100, 5)]), 0.2), "overkill must not heal"
    assert model.attack(0, 1, [(100, 5)]) == 0, "replayed attack"
    assert model.attack(0, 2, [(100, 100)], kind="burn") == 0
    assert model.attack(0, 3, [(100, 100)], kind="proc") == 0
    assert model.attack(0, 4, [(100, 100)], enemy=False) == 0
    assert model.attack(0, 5, [(100, 0)]) == 0, "corpse"
    assert isclose(model.attack(.1, 6, [(100, 100)] * 12), 1.4), "one shared budget for pellets/AoE"
    assert model.attack(.5, 7, [(100, 100)]) == 0, "cap across attacks"
    assert isclose(model.attack(1, 8, [(100, 100)]), .2), "sliding expiry, not a frame reset"
    full = BloodPactModel(health=80)
    assert full.attack(0, 1, [(10, 10)]) == 0
    dead = BloodPactModel(health=0)
    assert dead.attack(0, 1, [(10, 10)]) == 0, "no resurrection"
    for heal_fraction, cap in ((.04, .02), (.06, .03), (.08, .04)):
        ranked = BloodPactModel(fraction=heal_fraction, cap=cap)
        assert isclose(ranked.attack(0, 1, [(1000, 1000)]), 80 * cap)
    print("PASS: 12 proposal cards, RU/EN parameter parity; toy model maxHP/overkill/duplicate/source/cap/expiry/death/ranks")
    print("NOT VERIFIED: Unity integration, network protocol, playtest enjoyment or balance.")


if __name__ == "__main__":
    main()
