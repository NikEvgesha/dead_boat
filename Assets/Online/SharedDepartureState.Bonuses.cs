using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    public struct SharedBoatProfile : INetworkStruct
    {
        public float CardFuel, CardSpeed, CardConsumption, CardFill;
        public float ProfessionFuel, ProfessionFuelMult, ProfessionSpeed, ProfessionSpeedMult;
        public float ProfessionConsumption, ProfessionFill;
        public float AnimalFuel, AnimalSpeed, AnimalConsumption, AnimalFill;
        public bool Valid()
        {
            foreach (float value in new[] { CardFuel, CardSpeed, CardConsumption, CardFill,
                ProfessionFuel, ProfessionFuelMult, ProfessionSpeed, ProfessionSpeedMult,
                ProfessionConsumption, ProfessionFill, AnimalFuel, AnimalSpeed, AnimalConsumption, AnimalFill })
                if (float.IsNaN(value) || float.IsInfinity(value) || Mathf.Abs(value) > 1000000) return false;
            return ProfessionFuelMult > 0 && ProfessionSpeedMult > 0 && ProfessionConsumption >= 0 &&
                ProfessionFill >= 0 && AnimalConsumption >= 0 && AnimalFill >= 0;
        }

        public static SharedBoatProfile Local()
        {
            var cards = LevelStatManager.Instance != null ? LevelStatManager.Instance.Stats : new Stats();
            var profession = ProfessionService.GetCurrentPassiveBonuses();
            var animals = EggAnimalBuffService.GetCurrentBuffsSnapshot();
            return new SharedBoatProfile
            {
                CardFuel = cards.MaxFuel, CardSpeed = cards.MaxSpeedBoard,
                CardConsumption = LevelStatManager.Instance != null ? cards.ConsumptionFuel : 1,
                CardFill = LevelStatManager.Instance != null ? cards.AddMultFuel : 1,
                ProfessionFuel = profession.maxFuelFlat, ProfessionFuelMult = profession.SafeMaxFuelMultiplier,
                ProfessionSpeed = profession.boatSpeedFlat, ProfessionSpeedMult = profession.SafeBoatSpeedMultiplier,
                ProfessionConsumption = profession.SafeFuelConsumptionMultiplier, ProfessionFill = profession.SafeFuelFillMultiplier,
                AnimalFuel = animals.maxFuelFlat, AnimalSpeed = animals.boatSpeedFlat,
                AnimalConsumption = animals.SafeFuelConsumptionMultiplier, AnimalFill = animals.SafeFuelFillMultiplier
            };
        }
    }

    public sealed partial class SharedDepartureState
    {
        [Networked, Capacity(4)] public NetworkDictionary<PlayerRef, SharedBoatProfile> BoatProfiles => default;
        [Networked] public float BoatFillMultiplier { get; private set; }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_BoatProfile(SharedBoatProfile profile, RpcInfo info = default)
        {
            if (Phase != 3 || !Includes(info.Source) || !profile.Valid()) return;
            // Profiles are client-reported game stats in this prototype; production
            // reward verification remains a separate backend task.
            if (BoatProfiles.ContainsKey(info.Source)) BoatProfiles.Set(info.Source, profile);
            else if (BoatProfiles.Count < 4) BoatProfiles.Add(info.Source, profile);
        }

        private readonly SharedBoatProfile[] eligibleBoatProfiles = new SharedBoatProfile[4];

        private bool HaveRequiredBoatProfiles()
        {
            for (int slot = 0; slot < CrewCount; slot++)
                if (Includes(Crew[slot]) && !BoatProfiles.ContainsKey(Crew[slot])) return false;
            return true;
        }

        private void RefreshBoatBonuses(BoardController board)
        {
            int count = 0;
            // A temporary disconnect keeps its sealed slot/profile for recovery.
            // An irreversible abandon removes its contribution without deleting recovery history.
            for (int slot = 0; slot < CrewCount; slot++)
                if (Includes(Crew[slot]) && BoatProfiles.TryGet(Crew[slot], out var profile))
                    eligibleBoatProfiles[count++] = profile;
            var bonuses = SharedBoatBonusMath.Calculate(board.SharedBaseFuel, board.SharedBaseSpeed,
                board.SharedBaseConsumption, eligibleBoatProfiles, count);
            BoatMaxFuel = bonuses.MaxFuel;
            BoatMaxSpeed = bonuses.MaxSpeed;
            BoatConsumption = bonuses.Consumption;
            BoatFillMultiplier = bonuses.Fill;
            BoatFuel = Mathf.Min(BoatFuel, BoatMaxFuel);
        }
    }
}
