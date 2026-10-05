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

        private void RefreshBoatBonuses(BoardController board)
        {
            float cardFuel = 0, cardSpeed = 0, cardConsumption = 1, cardFill = 1;
            foreach (var entry in BoatProfiles)
            {
                cardFuel += entry.Value.CardFuel;
                cardSpeed += entry.Value.CardSpeed;
                cardConsumption += entry.Value.CardConsumption - 1;
                cardFill += entry.Value.CardFill - 1;
            }
            float maxFuel = float.NegativeInfinity, maxSpeed = float.NegativeInfinity;
            float animalFuel = float.NegativeInfinity, animalSpeed = float.NegativeInfinity;
            float professionConsumption = float.PositiveInfinity, animalConsumption = float.PositiveInfinity;
            float professionFill = float.NegativeInfinity, animalFill = float.NegativeInfinity;
            foreach (var entry in BoatProfiles)
            {
                var p = entry.Value;
                maxFuel = Mathf.Max(maxFuel, (board.SharedBaseFuel + cardFuel + p.ProfessionFuel) * p.ProfessionFuelMult);
                maxSpeed = Mathf.Max(maxSpeed, (board.SharedBaseSpeed + cardSpeed + p.ProfessionSpeed) * p.ProfessionSpeedMult);
                animalFuel = Mathf.Max(animalFuel, p.AnimalFuel);
                animalSpeed = Mathf.Max(animalSpeed, p.AnimalSpeed);
                professionConsumption = Mathf.Min(professionConsumption, p.ProfessionConsumption);
                animalConsumption = Mathf.Min(animalConsumption, p.AnimalConsumption);
                professionFill = Mathf.Max(professionFill, p.ProfessionFill);
                animalFill = Mathf.Max(animalFill, p.AnimalFill);
            }
            BoatMaxFuel = Mathf.Max(0, maxFuel + animalFuel);
            BoatMaxSpeed = Mathf.Max(0, maxSpeed + animalSpeed);
            BoatConsumption = board.SharedBaseConsumption * Mathf.Max(0, cardConsumption) * professionConsumption * animalConsumption;
            BoatFillMultiplier = Mathf.Max(0, cardFill) * professionFill * animalFill;
            BoatFuel = Mathf.Min(BoatFuel, BoatMaxFuel);
        }
    }
}
