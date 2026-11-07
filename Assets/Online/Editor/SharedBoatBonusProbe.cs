using System;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    public static class SharedBoatBonusProbe
    {
        private static float sink;
        private static SharedBoatProfile Neutral() => new SharedBoatProfile
        {
            CardConsumption = 1, CardFill = 1, ProfessionFuelMult = 1, ProfessionSpeedMult = 1,
            ProfessionConsumption = 1, ProfessionFill = 1, AnimalConsumption = 1, AnimalFill = 1
        };
        private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
        private static void Near(float value, float expected, string message) => Require(Mathf.Abs(value - expected) < .001f, message + ": " + value + " vs " + expected);
        private static void Expected(SharedBoatBonuses value, float fuel, float speed, float consumption, float fill)
        {
            Near(value.MaxFuel, fuel, "fuel"); Near(value.MaxSpeed, speed, "speed");
            Near(value.Consumption, consumption, "consumption"); Near(value.Fill, fill, "fill");
        }
        public static void Run()
        {
            Require(!EditorApplication.isPlaying, "idle Editor");
            var p = new[] { Neutral(), Neutral(), Neutral(), Neutral() };
            p[0].CardFuel = 100; p[0].CardSpeed = 5; p[0].CardConsumption = .8f; p[0].CardFill = 1.2f;
            p[0].ProfessionFuel = 10; p[0].ProfessionFuelMult = 2; p[0].ProfessionSpeedMult = 1.5f;
            p[0].ProfessionConsumption = .8f; p[0].ProfessionFill = 1.4f;
            p[0].AnimalFuel = 30; p[0].AnimalSpeed = 1; p[0].AnimalConsumption = .9f; p[0].AnimalFill = 1.1f;
            p[1].CardFuel = 50; p[1].CardSpeed = 10; p[1].CardConsumption = .9f; p[1].CardFill = 1.1f;
            p[1].ProfessionSpeed = 20; p[1].AnimalFuel = 40; p[1].AnimalSpeed = 5;
            p[1].AnimalConsumption = .8f; p[1].AnimalFill = 1.2f;
            Expected(SharedBoatBonusMath.Calculate(1000, 85, 2, p, 2), 2360, 155, 2 * .7f * .8f * .8f, 2.184f);

            p[1].ProfessionFuel = 100; p[1].ProfessionFuelMult = 1.2f; p[1].ProfessionConsumption = .9f; p[1].ProfessionFill = 1.1f;
            p[2].CardFuel = 25; p[2].ProfessionFuel = 500; p[2].ProfessionConsumption = .6f; p[2].ProfessionFill = 1.2f;
            p[2].AnimalFuel = 80; p[2].AnimalSpeed = 2; p[2].AnimalConsumption = .95f; p[2].AnimalFill = 1.5f;
            p[3].CardFuel = 75; p[3].CardSpeed = 2; p[3].CardConsumption = .7f; p[3].CardFill = 1.3f;
            p[3].ProfessionSpeed = 10; p[3].ProfessionSpeedMult = 2; p[3].ProfessionFill = 1.3f;
            p[3].AnimalFuel = 60; p[3].AnimalSpeed = 3; p[3].AnimalConsumption = .7f;
            foreach (var profile in p) Require(profile.Valid(), "valid four-player input");
            var four = SharedBoatBonusMath.Calculate(1000, 85, 2, p, 4);
            Expected(four, 2600, 229, .336f, 3.36f);
            int permutations = 0;
            var order = new SharedBoatProfile[4];
            for (int a = 0; a < 4; a++) for (int b = 0; b < 4; b++)
            for (int c = 0; c < 4; c++) for (int d = 0; d < 4; d++)
            {
                if (a == b || a == c || a == d || b == c || b == d || c == d) continue;
                order[0] = p[a]; order[1] = p[b]; order[2] = p[c]; order[3] = p[d];
                Require(SharedBoatBonusMath.Calculate(1000, 85, 2, order, 4).Equals(four), "all 24 permutations agree bit-for-bit");
                permutations++;
            }
            // A takeover starts with a new local buffer and the same replicated eligible profiles.
            var fresh = (SharedBoatProfile[])p.Clone();
            Require(SharedBoatBonusMath.Calculate(1000, 85, 2, fresh, 4).Equals(four), "fresh authority buffer");
            // A disconnected member keeps the profile; an abandoned member is omitted by roster selection.
            var withoutFourth = new[] { p[0], p[1], p[2] };
            var reduced = SharedBoatBonusMath.Calculate(1000, 85, 2, withoutFourth, 3);
            Expected(reduced, 2450, 155, 2 * .7f * .6f * .8f, 1.3f * 1.4f * 1.5f);
            var changed = p[3]; changed.CardFuel += 20; p[3] = changed;
            Near(SharedBoatBonusMath.Calculate(1000, 85, 2, p, 4).MaxFuel, 2640, "profile replacement adds once");
            Require(SharedBoatBonusMath.Calculate(1000, 85, 2, p, 3).Equals(reduced), "stale tail outside eligible count is ignored");
            Expected(SharedBoatBonusMath.Calculate(1000, 85, 2, null, 0), 1000, 85, 2, 1);
            var neutral = new[] { Neutral() };
            Expected(SharedBoatBonusMath.Calculate(1000, 85, 2, neutral, 1), 1000, 85, 2, 1);
            var board = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BoardController.prefab").GetComponent<BoardController>();
            var authored = SharedBoatBonusMath.Calculate(board.SharedBaseFuel, board.SharedBaseSpeed, board.SharedBaseConsumption, neutral, 1);
            Expected(authored, board.SharedBaseFuel, board.SharedBaseSpeed, board.SharedBaseConsumption, 1);
            var animals = new AnimalRunBuffs();
            animals.Add(new AnimalRunBuffs { maxFuelFlat = 10, fuelConsumptionMultiplier = .8f });
            animals.Add(new AnimalRunBuffs { maxFuelFlat = 20, fuelConsumptionMultiplier = .9f });
            Near(animals.maxFuelFlat, 30, "animals within one player add flat");
            Near(animals.SafeFuelConsumptionMultiplier, .72f, "animals within one player keep existing multiplication");
            for (int i = 0; i < p.Length; i++) { p[i].CardConsumption = .5f; p[i].CardFill = .5f; }
            var floor = SharedBoatBonusMath.Calculate(1000, 85, 2, p, 4);
            Near(floor.Consumption, 0, "no negative consumption"); Near(floor.Fill, 0, "no negative fill");
            var invalid = Neutral(); invalid.CardFuel = float.NaN; Require(!invalid.Valid(), "NaN rejected");
            invalid = Neutral(); invalid.ProfessionFuelMult = float.PositiveInfinity; Require(!invalid.Valid(), "infinity rejected");
            for (int i = 0; i < 100; i++) sink = SharedBoatBonusMath.Calculate(1000, 85, 2, p, 4).MaxFuel;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) sink = SharedBoatBonusMath.Calculate(1000, 85, 2, p, 4).MaxFuel;
            long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Require(bytes == 0, "no managed allocations in 10000 calculations: " + bytes);
            Debug.Log("[Boat bonus math] PASS: old 2-player baseline; 4 distinct winners/" + permutations +
                " permutations; fresh/reduced/replaced/count-tail; authored board; animal totals; zero floor/invalid inputs; 10000 calculations allocated=" + bytes +
                ". Pure math, not live four-peer leave/reconnect/handoff; no Play/Build/user saves");
        }
    }
}
