using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    public struct SharedDragonBrain : INetworkStruct
    {
        public ulong Id;
        public int State, Direction, DiveHitMask;
        public float Timer, LastShot, Angle, DiveTimer, DiveTime1, DiveTime2;
        public float ShootElapsed, DiveElapsed, Speed, PreviousHP;
        public Vector3 Shooting, DiveStart, DiveMid, DiveEnd, Dead;
    }
    public sealed partial class SharedDepartureState
    {
        [Networked] public SharedDragonBrain DragonBrain { get; private set; }
    }
}
