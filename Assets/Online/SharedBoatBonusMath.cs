using UnityEngine;

namespace DeadBoat.Online
{
    public struct SharedBoatBonuses
    {
        public float MaxFuel, MaxSpeed, Consumption, Fill;
    }

    /// <summary>Input contains one validated profile per eligible crew slot, in sealed crew order.</summary>
    public static class SharedBoatBonusMath
    {
        public static SharedBoatBonuses Calculate(float baseFuel, float baseSpeed, float baseConsumption,
            SharedBoatProfile[] profiles, int count)
        {
            count = profiles == null ? 0 : Mathf.Clamp(count, 0, Mathf.Min(4, profiles.Length));
            if (count == 0) return new SharedBoatBonuses
            {
                MaxFuel = Mathf.Max(0, baseFuel), MaxSpeed = Mathf.Max(0, baseSpeed),
                Consumption = Mathf.Max(0, baseConsumption), Fill = 1
            };
            // Double accumulation avoids order-dependent float rounding of four card contributions.
            double fuel = 0, speed = 0, consumption = 1, fill = 1;
            for (int i = 0; i < count; i++)
            {
                var p = profiles[i];
                fuel += p.CardFuel; speed += p.CardSpeed;
                consumption += (double)p.CardConsumption - 1;
                fill += (double)p.CardFill - 1;
            }
            float professionFuel = float.NegativeInfinity, professionSpeed = float.NegativeInfinity;
            float animalFuel = float.NegativeInfinity, animalSpeed = float.NegativeInfinity;
            float professionConsumption = float.PositiveInfinity, animalConsumption = float.PositiveInfinity;
            float professionFill = float.NegativeInfinity, animalFill = float.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                var p = profiles[i];
                professionFuel = Mathf.Max(professionFuel, (baseFuel + (float)fuel + p.ProfessionFuel) * p.ProfessionFuelMult);
                professionSpeed = Mathf.Max(professionSpeed, (baseSpeed + (float)speed + p.ProfessionSpeed) * p.ProfessionSpeedMult);
                animalFuel = Mathf.Max(animalFuel, p.AnimalFuel);
                animalSpeed = Mathf.Max(animalSpeed, p.AnimalSpeed);
                professionConsumption = Mathf.Min(professionConsumption, p.ProfessionConsumption);
                animalConsumption = Mathf.Min(animalConsumption, p.AnimalConsumption);
                professionFill = Mathf.Max(professionFill, p.ProfessionFill);
                animalFill = Mathf.Max(animalFill, p.AnimalFill);
            }
            return new SharedBoatBonuses
            {
                MaxFuel = Mathf.Max(0, professionFuel + animalFuel),
                MaxSpeed = Mathf.Max(0, professionSpeed + animalSpeed),
                Consumption = baseConsumption * Mathf.Max(0, (float)consumption) * professionConsumption * animalConsumption,
                Fill = Mathf.Max(0, (float)fill) * professionFill * animalFill
            };
        }
    }
}
