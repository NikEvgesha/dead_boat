using System;
using System.Linq;
using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    public struct CompanionRecord : INetworkStruct
    {
        public NetworkString<_32> Animal;
        public int Stage;
    }

    [DefaultExecutionOrder(110)]
    public sealed class AvatarCompanions : MonoBehaviour
    {
        private LobbyNetworkAvatar avatar;
        private EggHatchingCatalog catalog;
        private readonly CompanionTrail trail = new();
        private readonly GameObject[] models = new GameObject[3];
        private readonly CompanionRecord[] displayed = new CompanionRecord[3];
        private Vector3 origin;
        private bool visible;
        private double nextLoadout;
        public void Initialize(LobbyNetworkAvatar value) { avatar = value; visible = false; nextLoadout = 0; catalog = Resources.Load<EggHatchingCatalog>("Eggs/EggHatchingCatalog"); }
        private void LateUpdate()
        {
            if (avatar == null || avatar.Object == null || !avatar.Object.IsValid) return;
            bool active = SharedRunContext.Playing && avatar.Runner == SharedRunContext.State.Runner;
            if (avatar.Object.HasStateAuthority && Time.realtimeSinceStartupAsDouble >= nextLoadout)
            {
                nextLoadout = Time.realtimeSinceStartupAsDouble + 1;
                var equipped = active ? EggFeatureStorage.Load()?.placedAnimals?.Where(x => x != null)
                    .OrderBy(x => x.pointId, StringComparer.Ordinal).Take(3).ToArray() : null;
                for (int i = 0; i < 3; i++)
                {
                    var record = default(CompanionRecord);
                    if (equipped != null && i < equipped.Length && catalog != null &&
                        catalog.TryGetAnimal(equipped[i].animalId, out var animal) && animal.animalId.Length <= 32)
                    { record.Animal = animal.animalId; record.Stage = Mathf.Clamp(equipped[i].stage, 1, animal.maxStage); }
                    var previous = avatar.Companions[i];
                    if (!previous.Animal.Equals(record.Animal) || previous.Stage != record.Stage) avatar.Companions.Set(i, record);
                }
            }
            Vector3 logical = avatar.LogicalPosition;
            if (!visible || origin != LobbyNetworkAvatar.Origin) trail.Reset(logical);
            origin = LobbyNetworkAvatar.Origin;
            visible = active;
            if (!active)
            {
                foreach (var model in models) if (model != null) model.SetActive(false);
                return;
            }
            trail.Observe(logical);
            for (int i = 0; i < 3; i++)
            {
                var record = avatar.Companions[i];
                if (!displayed[i].Animal.Equals(record.Animal) || displayed[i].Stage != record.Stage)
                {
                    if (models[i] != null) Destroy(models[i]);
                    models[i] = null; displayed[i] = record;
                    string id = record.Animal.ToString();
                    if (catalog != null && catalog.TryGetAnimalStage(id, record.Stage, out _, out var stage))
                        models[i] = CompanionVisual.Create(stage.animalPrefab, stage.tint, transform);
                }
                var model = models[i]; if (model == null) continue;
                model.SetActive(true);
                Vector3 position = trail.Sample((i + 1) * 1.3f, avatar.transform.forward) - origin;
                position.y += .45f + Mathf.Sin(Time.unscaledTime * 2 + i * .8f) * .1f;
                Vector3 direction = trail.Sample(Mathf.Max(0, (i + 1) * 1.3f - .3f), avatar.transform.forward) - origin - position;
                direction.y = 0;
                model.transform.SetPositionAndRotation(position, direction.sqrMagnitude > .0001f ? Quaternion.LookRotation(direction) : avatar.WorldTransform.rotation);
            }
        }
    }
}
