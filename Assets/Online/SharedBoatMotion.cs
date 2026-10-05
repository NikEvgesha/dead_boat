using UnityEngine;

namespace DeadBoat.Online
{
    public static class SharedBoatMotion
    {
        public static void Step(float speed, float fuel, float distance, float input, float dt,
            float acceleration, float maxSpeed, float consumption, float coast, float brake, float end,
            out float nextSpeed, out float nextFuel, out float nextDistance)
        {
            nextFuel = Mathf.Max(0, fuel);
            if (input > 0 && nextFuel > 0)
            {
                speed = Mathf.Clamp(speed + acceleration * input * dt, 0, maxSpeed);
                nextFuel = Mathf.Max(0, nextFuel - consumption * dt);
            }
            else speed = Mathf.MoveTowards(speed, 0, (input < 0 ? brake : coast) * dt);
            nextDistance = Mathf.Min(end, distance + speed * dt);
            nextSpeed = nextDistance >= end ? 0 : speed;
        }
    }
}
