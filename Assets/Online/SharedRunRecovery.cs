using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    [Serializable]
    public sealed class SharedReturnTicket
    {
        public string Room, Region, Lobby, Version;
        public long PlayerId;
        public int Seed, Level, Generator;
    }

    // This ticket is a capability for a live room, never a persisted online world.
    public sealed class SharedRunRecovery : MonoBehaviour
    {
        private const string TicketKey = "DeadBoat.SharedReturn.v1";
        private const string IdentityKey = "DeadBoat.FusionIdentity.v1";
        private static SharedRunRecovery instance;
        private SharedReturnTicket ticket;
        private NetworkRunner probe;
        private SharedDepartureState probeState;
        private bool busy, lost, offered, restoring, suppressOffer;
        private double nextCapture, nextAttempt;
        private int revision;
        private SharedPersonalData lastData;
        private SharedRecoveryItem[] lastItems;
        private SharedSoloSnapshot fallback;
        public string Message { get; private set; } = "";
        public bool Visible => lost || offered;
        public bool Lost => lost;
        public bool Busy => busy;
        public bool CanSolo => lost && fallback != null;
        public bool CanRetry => ticket != null;
        public static bool Restoring => instance != null && instance.restoring;
        public static bool Disconnected => instance != null && instance.lost;
        public static bool HasPending => instance != null ? instance.ticket != null : ReadTicket() != null;
        public static SharedRunRecovery Instance
        {
            get
            {
                if (instance == null)
                {
                    var root = new GameObject("Shared run recovery");
                    DontDestroyOnLoad(root);
                    instance = root.AddComponent<SharedRunRecovery>();
                    instance.ticket = ReadTicket();
                    root.AddComponent<SharedRunRecoveryUI>().Initialize(instance);
                }
                return instance;
            }
        }
        public static long LocalId
        {
            get
            {
                if (long.TryParse(PlayerPrefs.GetString(IdentityKey), out long id) && id != 0) return id;
                id = BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0);
                if (id == 0) id = 1;
                PlayerPrefs.SetString(IdentityKey, id.ToString(System.Globalization.CultureInfo.InvariantCulture));
                PlayerPrefs.Save();
                return id;
            }
        }
        private static SharedReturnTicket ReadTicket()
        {
            try
            {
                var t = JsonUtility.FromJson<SharedReturnTicket>(PlayerPrefs.GetString(TicketKey, ""));
                if (t != null && SoloRunBranch.ClosesReturn(t.Room, t.PlayerId)) return null;
                return t != null && !string.IsNullOrEmpty(t.Room) && t.PlayerId != 0 ? t : null;
            }
            catch (Exception) { return null; }
        }
        public static Fusion.Photon.Realtime.FusionAppSettings Settings(string region = null)
        {
            var settings = Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings.GetCopy();
            settings.emptyRoomTtl = 0; // Last player leaves => this run cannot be revived.
            if (!string.IsNullOrEmpty(region)) settings.FixedRegion = region;
            return settings;
        }
        public void Track(NetworkRunner runner, SharedDepartureState state, string lobbyName)
        {
            SoloRunBranch.Suspend();
            ticket = new SharedReturnTicket
            {
                Room = runner.SessionInfo.Name, Region = runner.SessionInfo.Region, Lobby = lobbyName,
                PlayerId = LocalId, Seed = state.Seed, Level = state.LevelId,
                Generator = state.GenerationVersion, Version = Application.version
            };
            PlayerPrefs.SetString(TicketKey, JsonUtility.ToJson(ticket));
            PlayerPrefs.Save();
            suppressOffer = lost = offered = false;
            lastItems = null; revision = 0; nextCapture = 0;
        }
        public static void Forget()
        {
            PlayerPrefs.DeleteKey(TicketKey);
            PlayerPrefs.Save();
            if (instance == null) return;
            instance.ticket = null;
            instance.lost = instance.offered = instance.restoring = false;
            instance.fallback = null;
        }
        private void InvalidateReturn()
        {
            PlayerPrefs.DeleteKey(TicketKey);
            PlayerPrefs.Save();
            ticket = null;
            // A disconnected in-memory scene still offers an explicit solo fork.
            offered = !lost;
        }
        public void ConnectionLost()
        {
            if (ticket == null) return;
            lost = true;
            Message = "Связь потеряна. Ждём интернет или выбери одиночный режим.";
            Time.timeScale = 0;
        }
        private void Update()
        {
            if (probe != null && !busy && !probe.IsConnectedToServer)
            {
                Message = "Проверка прервалась. Повтори подключение, когда появится сеть.";
                _ = StopProbeAsync();
            }
            else if (probe != null && probeState != null && !busy && !probeState.HasOtherPlayingCrew(probe.LocalPlayer))
            {
                Message = "Все участники вышли. Вернуться в этот забег уже нельзя.";
                InvalidateReturn();
                _ = StopProbeAsync();
            }
            if (SharedRunContext.Playing && !lost && !restoring)
            {
                var state = SharedRunContext.State;
                if (Time.realtimeSinceStartupAsDouble >= nextCapture)
                {
                    nextCapture = Time.realtimeSinceStartupAsDouble + 2;
                    CapturePersonal(state);
                    if (state.Personal.ContainsKey(state.Runner.LocalPlayer))
                        fallback = SharedSoloSnapshot.Capture(state);
                }
                return;
            }
            if (busy || probe != null || ticket == null || suppressOffer || SharedRunContext.Active ||
                LoadingManager.Instance == null || LoadingManager.Instance.CurrentLocation != Location.Lobby ||
                Time.realtimeSinceStartupAsDouble < nextAttempt) return;
            nextAttempt = Time.realtimeSinceStartupAsDouble + 15;
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                offered = true;
                Message = "Есть запись о прошлом экипаже. Интернет нужен, чтобы проверить возможность возврата.";
                return;
            }
            _ = CheckAsync();
        }
        private void CapturePersonal(SharedDepartureState state)
        {
            if (Inventory.Instance == null || PlayerAmmoManager.Instance == null || PlayerStatsManager.Instance == null) return;
            var inventory = Inventory.Instance.GetInventoryList();
            if (inventory.Count > SharedDepartureState.RecoverySlots) return;
            var items = new List<SharedRecoveryItem>();
            var active = Inventory.Instance.GetComponent<ActiveItemManager>()?.Active;
            foreach (var item in inventory)
            {
                int template = SharedItemCatalog.Load()?.Identify(item) ?? 0;
                if (template == 0) return; // Never replace a complete bag with a partial one.
                items.Add(new SharedRecoveryItem
                {
                    Template = template, WorldId = item.GetComponent<WorldSpawnIdentity>()?.Id ?? 0,
                    Ammo = item.GetComponentInChildren<RangedWeaponController>(true)?.CurrentAmmo ?? -1,
                    Equipped = item == active ? 1 : 0
                });
            }
            var data = SharedPersonalData.Capture();
            bool changed = !data.Equals(lastData) || lastItems == null || !items.SequenceEqual(lastItems);
            if (changed) { lastData = data; lastItems = items.ToArray(); revision++; }
            if (revision < 1) revision = 1;
            if (state.Personal.TryGet(state.Runner.LocalPlayer, out var confirmed) && confirmed.Revision == revision) return;
            for (int i = 0; i < items.Count; i++) state.RPC_RecoveryItem(revision, i, items[i]);
            data.Revision = revision; data.ItemCount = items.Count;
            state.RPC_RecoveryCommit(data);
        }
        public async Task CheckAsync()
        {
            if (busy || ticket == null) return;
            busy = true;
            offered = !lost;
            Message = "Проверяем, продолжает ли экипаж забег…";
            try
            {
                if (ticket.Version != Application.version || ticket.Generator != RunRandom.Version)
                { Message = "Версия забега изменилась. Вернуться не получится."; InvalidateReturn(); return; }
                var lobby = FindAnyObjectByType<LobbyOnlineBootstrap>();
                if (lobby != null) await lobby.SuspendForRecoveryAsync();
                if (lost) await SharedRunLifetime.ReleaseLostTransportAsync();
                await StopProbeAsync();
                var root = new GameObject("Shared return runner");
                DontDestroyOnLoad(root);
                probe = root.AddComponent<NetworkRunner>();
                var scenes = root.AddComponent<LobbySceneManager>();
                var provider = root.AddComponent<NetworkObjectProviderDefault>();
                var start = probe.StartGame(new StartGameArgs
                {
                    GameMode = GameMode.Shared, Config = LobbyOnlineBootstrap.CreateDepartureNetworkConfig(),
                    CustomPhotonAppSettings = Settings(ticket.Region), PlayerUniqueId = ticket.PlayerId,
                    SessionName = ticket.Room, CustomLobbyName = ticket.Lobby,
                    EnableClientSessionCreation = false, SceneManager = scenes, ObjectProvider = provider
                });
                if (await Task.WhenAny(start, Task.Delay(20000)) != start) throw new TimeoutException();
                var result = await start;
                if (!result.Ok)
                {
                    if (result.ShutdownReason.ToString().Contains("GameNotFound") ||
                        result.ShutdownReason.ToString().Contains("GameClosed"))
                    { Message = "Экипаж уже завершил забег. Можно начать новую поездку."; InvalidateReturn(); }
                    else Message = "Не удалось проверить экипаж. Попробуй переподключиться позже.";
                    await StopProbeAsync(); return;
                }
                double until = Time.realtimeSinceStartupAsDouble + 8;
                while (probeState == null && Time.realtimeSinceStartupAsDouble < until)
                {
                    probeState = FindObjectsByType<SharedDepartureState>(FindObjectsSortMode.None)
                        .FirstOrDefault(s => s.Runner == probe && s.Object != null && s.Object.IsValid);
                    if (probeState == null) await Task.Delay(100);
                }
                while (probeState != null && probeState.Phase == 3 && !probeState.HasOtherPlayingCrew(probe.LocalPlayer) &&
                    Time.realtimeSinceStartupAsDouble < until)
                    await Task.Delay(100);
                if (probeState == null || probeState.Seed != ticket.Seed ||
                    !probeState.CanReturn(probe.LocalPlayer) || !probeState.HasOtherPlayingCrew(probe.LocalPlayer))
                { Message = "Этот забег уже недоступен для возврата."; InvalidateReturn(); await StopProbeAsync(); return; }
                Message = "Экипаж ещё в пути. Можно вернуться к команде.";
            }
            catch (Exception) { Message = "Соединение пока недоступно. Можно повторить попытку."; await StopProbeAsync(); }
            finally { busy = false; }
        }
        public async Task ResumeAsync()
        {
            if (busy) return;
            if (probeState == null) await CheckAsync();
            if (probeState == null || probe == null || ticket == null) return;
            if (!probeState.CanReturn(probe.LocalPlayer) || !probeState.HasOtherPlayingCrew(probe.LocalPlayer))
            { Message = "Экипаж уже вышел из забега."; await StopProbeAsync(); InvalidateReturn(); return; }
            if (!probeState.Personal.TryGet(probe.LocalPlayer, out var personal))
            { Message = "Экипаж ещё не получил твоё состояние. Повтори попытку."; return; }
            busy = restoring = true;
            offered = lost = false;
            try
            {
                if (SharedRunContext.Active) await SharedRunLifetime.LeaveAsync(preserveReturn: true);
                if (!LevelManager.Instance.TryGetLevel(ticket.Level, out var level)) throw new InvalidOperationException();
                if (GameManager.Instance != null) GameManager.Instance.isEndGame = true;
                if (PlayerManager.Instance != null)
                {
                    PlayerManager.Instance.transform.SetParent(null);
                    DontDestroyOnLoad(PlayerManager.Instance.gameObject);
                }
                var root = new GameObject("Shared run lifetime");
                SharedRunContext.Begin(ticket.Seed, ticket.Level, root, SaveManager.Instance.PersistentProvider, probeState);
                float origin = Mathf.Max(0, Mathf.Floor(probeState.BoatDistance / 1000) * 1000);
                personal.SeedSave(SharedRunContext.Save, ticket.Level, probeState.BoatDistance, origin, probeState.BoatFuel);
                var value = probe; probe = null;
                root.AddComponent<SharedRunLifetime>().Initialize(value);
                bool loaded = false;
                Action onLoaded = () => loaded = true;
                GameLoader.Instance.OnSceneLoaded += onLoaded;
                LoadingManager.Instance.LoadLocation(Location.Game, level.Scene, false);
                double until = Time.realtimeSinceStartupAsDouble + 180;
                while ((!loaded || BoardController.Instance == null || !BoardController.Instance.StartGame ||
                    LoadingManager.Instance.CurrentLocation != Location.Game) && Time.realtimeSinceStartupAsDouble < until)
                    await Task.Delay(100);
                GameLoader.Instance.OnSceneLoaded -= onLoaded;
                if (!loaded || BoardController.Instance == null || !BoardController.Instance.StartGame) throw new TimeoutException();
                SharedRecoveryInventory.Restore(probeState, personal);
                LevelStatManager.Instance.Stats = personal.Cards;
                CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, personal.Coins - CurrencyManager.Instance.Coins);
                PlayerAmmoManager.Instance.RestoreRecovery(personal.Pistol, personal.Rifle, personal.Shotgun);
                PlayerStatsManager.Instance.LoadExp(personal.Experience, personal.Level);
                PlayerStatsManager.Instance.RestoreRecoveryHealth(personal.Health);
                restoring = false;
                SharedRunLifetime.SpawnLocalRecoveryAvatar(value);
                Time.timeScale = 1;
                nextCapture = 0;
                revision = personal.Revision;
                lastItems = null;
                Message = "Вы вернулись к экипажу.";
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Shared recovery] Restore failed: " + exception);
                restoring = false; lost = true;
                Message = "Возврат не завершился. Повтори подключение.";
            }
            finally { busy = false; }
        }
        public async Task ContinueSoloAsync()
        {
            if (busy || !CanSolo) return;
            busy = true;
            var copy = fallback;
            // Persist the decision before any asynchronous shutdown.
            SoloRunBranch.Store(copy);
            if (probe != null && probe.IsConnectedToServer && probeState != null)
                probeState.RPC_AbandonRun();
            Forget();
            if (probe != null) await Task.Delay(200);
            await StopProbeAsync();
            await SharedRunLifetime.LeaveAsync();
            SoloRunBranch.Begin(copy);
            Time.timeScale = 1;
            busy = false;
        }
        public async Task LaterAsync()
        {
            suppressOffer = true; offered = false;
            await StopProbeAsync();
            FindAnyObjectByType<LobbyOnlineBootstrap>()?.RetryConnection();
        }
        public async Task ExitAsync()
        {
            if (busy) return;
            Forget();
            await StopProbeAsync();
            if (SharedRunContext.Active && GameManager.Instance != null) GameManager.Instance.EndGame(true, false);
            else FindAnyObjectByType<LobbyOnlineBootstrap>()?.RetryConnection();
        }
        private async Task StopProbeAsync()
        {
            var current = probe; probe = null; probeState = null;
            if (current == null) return;
            try { await current.Shutdown(); } catch (Exception) { }
            if (current != null) Destroy(current.gameObject);
        }
    }
}
