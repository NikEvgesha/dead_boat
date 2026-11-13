using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace DeadBoat.Online.Editor
{
    // Draft protocol subtest. The caller owns both runners, the world and all managers.
    public static class WorldActions266PortalProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [Serializable] private sealed class Report
        {
            public string result, error;
            public string scope = "Two selected Cloud peers within the caller's crew, one actual ruins prefab and one shared local PlayerMovement. Other crew members are not used by this subtest's near checks. Production podium/portal RPCs and PublishDrop are used; physical trigger motion and independent browser player poses are not tested. Child records are deliberately removed and readiness is simulated through the runtime's private wasReady seam, followed by explicit production repair. No owner keys written; only this subtest's items are disposed. Runners/world teardown belongs to the caller.";
            public bool offering, wrongItem, portalCooldowns, contextTeleport, readyEdgeRepair, explicitRepair, hpGate, adGate, cleanup;
            public int callbacksAfterOffer, callbacksAfterRepair;
            public double elapsedSeconds;
            public List<string> passed = new List<string>(), skipped = new List<string>();
        }
        private sealed class AvatarBefore
        {
            public LobbyNetworkAvatar Avatar; public Vector3 Position; public float Health; public object LocalPlayer;
        }
        public static async Task CheckAsync(SharedDepartureState state, SharedDepartureState remote, GameObject actualRuins, Action pump)
        {
            var watch = Stopwatch.StartNew(); var report = new Report();
            var ownObjects = new List<GameObject>(); var ownIds = new HashSet<ulong>();
            var avatars = new List<AvatarBefore>();
            var player = PlayerMovement.Instance;
            Vector3 playerPosition = player != null ? player.transform.position : Vector3.zero;
            Quaternion playerRotation = player != null ? player.transform.rotation : Quaternion.identity;
            var playerFields = new Dictionary<FieldInfo, object>();
            string identityBefore = PlayerPrefs.GetString("DeadBoat.FusionIdentity.v1", "");
            CheckItemOnLocation podium = null; UnityAction<PickableItem> listener = null;
            long adToken = 0; bool oldAdExists = false; SharedAdLease oldAd = default;
            Exception failure = null;
            void Check(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); }
            void Pump() { Check(watch.Elapsed.TotalSeconds < 90, "Portal subtest global90s deadline"); pump(); }
            async Task Until(Func<bool> condition, string label)
            {
                double deadline = watch.Elapsed.TotalSeconds + 8;
                do { Pump(); if (condition()) return; await Task.Delay(25); }
                while (watch.Elapsed.TotalSeconds < deadline);
                throw new TimeoutException("Portal subtest8s: " + label);
            }
            async Task Settle() { for (int i = 0; i < 5; i++) { Pump(); await Task.Delay(25); } }
            void Pose(Vector3 localPosition)
            {
                player.transform.position = localPosition;
                foreach (var saved in avatars)
                    if (saved.Avatar != null && saved.Avatar.Object.IsValid)
                    {
                        saved.Avatar.transform.position = localPosition + LobbyNetworkAvatar.Origin;
                        typeof(LobbyNetworkAvatar).GetField("localPlayer", Private).SetValue(saved.Avatar, player);
                    }
            }
            async Task MoveNear(Component target)
            {
                Pose(target.transform.position);
                await Until(() => CrewNear(state, state.Runner.LocalPlayer, remote.Runner.LocalPlayer,
                    target.transform.position + LobbyNetworkAvatar.Origin), "both selected peer poses near " + target.name);
            }
            async Task<WorldSpawnIdentity> Publish(PickableItem prefab)
            {
                var item = SharedItemScale.CreateReplica(prefab, podium.transform.position, Quaternion.identity, Vector3.zero);
                ownObjects.Add(item.gameObject);
                foreach (var behaviour in item.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
                foreach (var body in item.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
                SharedItemsRuntime.PublishDrop(item);
                var identity = item.GetComponent<WorldSpawnIdentity>();
                Check(identity != null, "Production PublishDrop did not assign identity."); ownIds.Add(identity.Id);
                await Until(() => state.Items.ContainsKey(identity.Id) && remote.Items.ContainsKey(identity.Id), "published item payload");
                return identity;
            }
            try
            {
                Check(state != null && remote != null && state.Object.HasStateAuthority && SharedRunContext.State == state &&
                    SharedRunContext.Playing && actualRuins != null && player != null && pump != null, "Caller must supply ready authority context and one fixture player.");
                foreach (string name in new[] { "_inTeleport", "_isStart", "_waitStart", "_velocity" })
                { var field = typeof(PlayerMovement).GetField(name, Private); if (field != null) playerFields[field] = field.GetValue(player); }
                foreach (var avatar in LobbyNetworkAvatar.All)
                    if ((avatar.Runner == state.Runner || avatar.Runner == remote.Runner) && avatar.Object.HasStateAuthority)
                        avatars.Add(new AvatarBefore { Avatar = avatar, Position = avatar.transform.position, Health = avatar.Health,
                            LocalPlayer = typeof(LobbyNetworkAvatar).GetField("localPlayer", Private).GetValue(avatar) });
                Check(avatars.Count == 2, "Exactly two fixture authority avatars expected.");
                podium = actualRuins.GetComponentsInChildren<CheckItemOnLocation>(true).Single(p => p.RequiredItems.Count > 0 && p.RequiredItems.Any(d => d != null && d.Name == "Skull"));
                Check(podium.RequiredItems.Count == 1 && podium.PutItem != null, "Actual one-Skull podium expected; decorative empty checks are ignored.");
                var portals = actualRuins.GetComponentsInChildren<Portal>(true);
                Check(portals.Length == 2 && portals[0].NextPortal == portals[1] && portals[1].NextPortal == portals[0], "Actual portal pair must be mutual.");
                var podiumNode = SharedWorldActionsRuntime.FindNode(podium);
                var portalNodes = portals.Select(p => SharedWorldActionsRuntime.FindNode(p)).ToArray();
                Check(podiumNode != null && portalNodes.All(n => n != null), "Caller must bind the actual ruins components.");
                Check(!state.TryWorldAction(podiumNode.Id, out var initial) || initial.Flags == 0, "Podium already consumed before this subtest.");
                int callbacks = 0; listener = _ => callbacks++; podium.PutItem.AddListener(listener);
                var catalog = SharedItemCatalog.Load();
                var skullPrefab = catalog.entries.First(e => e.prefab != null && e.prefab.Data != null && e.prefab.Data.Name == "Skull").prefab;
                var wrongPrefab = catalog.entries.First(e => e.prefab != null && e.prefab.name.StartsWith("Loot203_T", StringComparison.Ordinal)).prefab;
                await MoveNear(podium);
                var wrong = await Publish(wrongPrefab);
                state.RPC_WorldOffering(podiumNode.Id, wrong.Id); await Settle();
                Check(state.Items[wrong.Id].Mode != 4 && (!state.TryWorldAction(podiumNode.Id, out var rejected) || rejected.Flags == 0) && callbacks == 0, "Wrong item was consumed or activated the podium.");
                report.wrongItem = true;

                // A remote-owned skull gives both senders a real race while only the owner may commit.
                var skull = await Publish(skullPrefab); ulong skullId = skull.Id;
                remote.RPC_ClaimItem(skullId);
                await Until(() => state.Items[skullId].Owner == remote.Runner.LocalPlayer && remote.Items[skullId].Mode == 1, "remote skull claim");
                state.RPC_WorldOffering(podiumNode.Id, skullId);
                remote.RPC_WorldOffering(podiumNode.Id, skullId);
                remote.RPC_WorldOffering(podiumNode.Id, skullId);
                await Until(() => state.Items[skullId].Mode == 4 && state.TryWorldAction(podiumNode.Id, out var accepted) && accepted.Flags == 1 &&
                    portalNodes.All(n => state.TryWorldAction(n.Id, out var p) && (p.Flags & 1) != 0), "one offering and both permanent portal unlocks");
                var consumed = state.Items[skullId];
                Check(consumed.ConsumedByAction == podiumNode.Id && consumed.ActionSlot == 0 && callbacks == 1, "Offering marker/slot/callback count invalid.");
                remote.RPC_WorldOffering(podiumNode.Id, skullId); await Settle();
                Check(callbacks == 1 && state.TryWorldAction(podiumNode.Id, out var repeat) && repeat.Flags == 1, "Repeated accepted item replayed the podium.");
                report.offering = true; report.callbacksAfterOffer = callbacks;
                report.passed.Add("Wrong Data.Name rejected; simultaneous authority/remote repeated offers consume the one remotely-owned Skull once, marker+slot0/mask1 and both portal records.");

                void DeletePortalHeaders()
                {
                    foreach (var page in SharedWorldPage.All(state.Runner))
                        foreach (var node in portalNodes) page.Actions.Remove(node.Id);
                    Check(portalNodes.All(n => !state.TryWorldAction(n.Id, out _)), "Child deletion seam failed.");
                }
                bool Repaired() => portalNodes.All(n => state.TryWorldAction(n.Id, out var p) && (p.Flags & 1) != 0);
                DeletePortalHeaders();
                var runtime = typeof(SharedWorldActionsRuntime).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                Check(runtime != null, "Runtime readiness seam missing.");
                typeof(SharedWorldActionsRuntime).GetField("wasReady", Private).SetValue(runtime, false);
                await Until(Repaired, "became-ready repairs children after podium presentation already applied");
                Check(callbacks == 1 && state.Items[skullId].Mode == 4 && state.Items[skullId].ConsumedByAction == podiumNode.Id, "Readiness repair replayed presentation or consumed another item.");
                report.readyEdgeRepair = true;
                DeletePortalHeaders();
                typeof(SharedDepartureState).GetMethod("RepairWorldOffering", Private).Invoke(state, new object[] { podiumNode });
                await Until(Repaired, "explicit receipt/header child repair");
                Check(callbacks == 1, "Explicit child repair re-invoked presentation callbacks.");
                report.explicitRepair = true; report.callbacksAfterRepair = callbacks;
                report.passed.Add("Already-applied podium survives deleted child headers: simulated readiness edge and explicit production repair recreate both unlocks without presentation replay or second Skull.");

                var unused = actualRuins.GetComponentsInChildren<MonoBehaviour>(true)
                    .Where(c => c is Lever || c is ActivateObject || c is ActivateZone)
                    .Select(c => SharedWorldActionsRuntime.FindNode(c)).FirstOrDefault(n => n != null && (!state.TryWorldAction(n.Id, out var a) || (a.Flags & 1) == 0));
                if (unused == null) report.skipped.Add("HP/ad rejection: no unused supported action node remains in this actual ruins fixture.");
                else
                {
                    await MoveNear(unused.Component);
                    var actor = avatars.Single(a => a.Avatar.Runner == state.Runner).Avatar;
                    typeof(LobbyNetworkAvatar).GetProperty("Health").SetValue(actor, 0f);
                    state.RPC_WorldAction(unused.Id, 1); // Source-authority local RPC invocation; no forged RpcInfo.
                    Check(!state.TryWorldAction(unused.Id, out var dead) || (dead.Flags & 1) == 0, "HP0 actor activated an unused action.");
                    typeof(LobbyNetworkAvatar).GetProperty("Health").SetValue(actor, 100f);
                    await Settle();
                    Check(!state.TryWorldAction(unused.Id, out var deadLate) || (deadLate.Flags & 1) == 0, "Rejected HP0 request activated later.");
                    report.hpGate = true;
                    oldAdExists = state.Ads.TryGet(state.Runner.LocalPlayer, out oldAd);
                    adToken = DateTime.UtcNow.Ticks; state.RPC_AdBegin(adToken);
                    await Until(() => state.AdShowing(state.Runner.LocalPlayer), "advertising gate open");
                    state.RPC_WorldAction(unused.Id, 1); await Settle();
                    Check(!state.TryWorldAction(unused.Id, out var ad) || (ad.Flags & 1) == 0, "Ad-showing actor activated an unused action.");
                    state.RPC_AdEnd(adToken); await Until(() => !state.AdShowing(state.Runner.LocalPlayer), "advertising gate closed");
                    report.adGate = true;
                    report.passed.Add("Own-source HP0 and ad-showing RPCs cannot activate a genuinely unused action node; no foreign sender is forged.");
                }

                await Until(() => portalNodes.All(n => state.TryWorldAction(n.Id, out var p) && p.Due.ExpiredOrNotRunning(state.Runner)), "portal unlock delay elapsed");
                await MoveNear(portals[0]);
                int sourceSlot = state.CrewSlot(state.Runner.LocalPlayer), otherSlot = state.CrewSlot(remote.Runner.LocalPlayer);
                Check(sourceSlot >= 0 && otherSlot >= 0 && sourceSlot != otherSlot, "Distinct crew cooldown slots expected.");
                state.RPC_WorldPortal(portalNodes[0].Id);
                await Until(() => (player.transform.position - portals[1].transform.position).sqrMagnitude < .01f &&
                    !state.PortalCooldowns[sourceSlot].ExpiredOrNotRunning(state.Runner), "context actor confirmed teleport");
                var sourceDeadline = state.PortalCooldowns[sourceSlot].TargetTick;
                Check(state.PortalCooldowns[otherSlot].ExpiredOrNotRunning(state.Runner), "Source travel spent another player's cooldown.");
                await Task.Delay(100);
                // Restore a valid source-side pose before retry, so range is not the rejection reason.
                avatars.Single(a => a.Avatar.Runner == state.Runner).Avatar.transform.position = portals[0].transform.position + LobbyNetworkAvatar.Origin;
                state.RPC_WorldPortal(portalNodes[0].Id);
                Check(state.PortalCooldowns[sourceSlot].TargetTick == sourceDeadline, "Repeated source travel extended/restarted active cooldown.");
                report.contextTeleport = true;
                await MoveNear(portals[0]); // One global player remains the authority-context player during remote feedback.
                Vector3 contextBeforeRemote = player.transform.position;
                remote.RPC_WorldPortal(portalNodes[0].Id);
                await Until(() => !state.PortalCooldowns[otherSlot].ExpiredOrNotRunning(state.Runner), "remote separate portal cooldown");
                await Settle();
                Check((player.transform.position - contextBeforeRemote).sqrMagnitude < .01f && state.PortalCooldowns[sourceSlot].TargetTick == sourceDeadline,
                    "Remote confirmation moved the authority-context player or overwrote its cooldown.");
                report.portalCooldowns = true;
                report.passed.Add("Authority-context actor really teleports; duplicate request does not extend cooldown; peer2 spends its own slot and cannot move the one authority-context PlayerMovement. Independent peer2 pose is not asserted.");
                Check(PlayerPrefs.GetString("DeadBoat.FusionIdentity.v1", "") == identityBefore, "Identity storage changed.");
                report.result = "PASS";
            }
            catch (Exception error) { failure = error; report.result = "FAIL"; report.error = error.ToString(); }
            finally
            {
                if (podium != null && listener != null) podium.PutItem.RemoveListener(listener);
                foreach (var obj in ownObjects) if (obj != null)
                {
                    foreach (var id in obj.GetComponentsInChildren<WorldSpawnIdentity>(true))
                        typeof(WorldSpawnIdentity).GetProperty("RemovingReplica", Private).SetValue(id, true);
                    foreach (var mesh in obj.GetComponentsInChildren<UnityEngine.ProBuilder.ProBuilderMesh>(true))
                        mesh.preserveMeshAssetOnDestroy = true;
                    Object.Destroy(obj);
                }
                if (state != null && state.Object != null && state.Object.IsValid && state.Object.HasStateAuthority)
                {
                    foreach (ulong id in ownIds)
                        foreach (var page in SharedWorldPage.All(state.Runner))
                            if (page.Object.HasStateAuthority) page.Items.Remove(id);
                    if (adToken != 0 && state.Ads.TryGet(state.Runner.LocalPlayer, out var currentAd) && currentAd.Token == adToken)
                    { if (oldAdExists) state.Ads.Set(state.Runner.LocalPlayer, oldAd); else state.Ads.Remove(state.Runner.LocalPlayer); }
                }
                if (player != null)
                {
                    player.transform.SetPositionAndRotation(playerPosition, playerRotation);
                    foreach (var field in playerFields) field.Key.SetValue(player, field.Value);
                }
                foreach (var saved in avatars) if (saved.Avatar != null && saved.Avatar.Object != null && saved.Avatar.Object.IsValid && saved.Avatar.Object.HasStateAuthority)
                {
                    saved.Avatar.transform.position = saved.Position;
                    typeof(LobbyNetworkAvatar).GetProperty("Health").SetValue(saved.Avatar, saved.Health);
                    typeof(LobbyNetworkAvatar).GetField("localPlayer", Private).SetValue(saved.Avatar, saved.LocalPlayer);
                }
                await Task.Delay(50);
                report.cleanup = ownObjects.All(o => o == null) && ownIds.All(id => !SharedItemsRuntime.TryFind(id, out _)) &&
                    PlayerPrefs.GetString("DeadBoat.FusionIdentity.v1", "") == identityBefore;
                if (!report.cleanup) { report.result = "FAIL"; report.error += "\nOwn item cleanup or read-only identity check failed."; }
                report.elapsedSeconds = watch.Elapsed.TotalSeconds;
                string folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs", "WorldActions266-20261114"); Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "portal.json"), JsonUtility.ToJson(report, true));
            }
            if (failure != null || !report.cleanup) throw new InvalidOperationException("Portal266 " + report.result, failure);
        }
        private static bool CrewNear(SharedDepartureState state, PlayerRef source, PlayerRef remote, Vector3 point)
        {
            if (source == remote) return false;
            bool sourceFound = false, remoteFound = false;
            foreach (var avatar in LobbyNetworkAvatar.All)
            {
                if (avatar.Runner != state.Runner) continue;
                PlayerRef owner = avatar.Object.StateAuthority;
                if (owner != source && owner != remote) continue;
                if ((avatar.LogicalPosition - point).sqrMagnitude > 9) return false;
                if (owner == source) sourceFound = true; else remoteFound = true;
            }
            return sourceFound && remoteFound;
        }
    }
}
