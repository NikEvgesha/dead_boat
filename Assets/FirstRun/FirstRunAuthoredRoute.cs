using UnityEngine;

namespace DeadBoat.FirstRun
{
    // Scene-authored references, not a replacement for inventory, combat or boat logic.
    public sealed class FirstRunAuthoredRoute : MonoBehaviour
    {
        public const int Seed = 14052026;
        public const int ContentVersion = 2;
        public TutorialManager tutorial;
        public Transform[] stops;
        public Transform arena;
        public PickableItem[] supplies;
        public ZombieController[] guards;
    }
}
