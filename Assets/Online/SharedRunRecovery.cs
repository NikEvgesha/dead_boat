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
        private NetworkRunner retiringProbe;
        private SharedDepartureState probeState;
        private bool busy, lost, offered, restoring, suppressOffer;
        private bool checking;
        private int checkVersion;
        private double checkDeadline;
        private string checkStage;
        private TaskCompletionSource<bool> checkCanceled;
        private bool transportClock;
        private float previousTimeScale;
        private double nextCapture, nextAttempt;
        private int revision;
        private SharedPersonalData lastData;
        private SharedRecoveryItem[] lastItems;
        private SharedSoloSnapshot fallback;
        public string Message { get; private set; } = "";
        public bool Visible => lost || offered;
        public bool Lost => lost;
        public bool Busy => busy;
        public bool CanCancelCheck => checking;
        public bool CanSolo => lost && fallback != null;
        public bool CanRetry => ticket != null;
        public static bool Restoring => instance != null && instance.restoring;
        public static bool Disconnected => instance != null && instance.lost;
        public static bool NeedsTransportClock => instance != null &&
            (instance.checking || instance.probe != null || instance.retiringProbe != null || instance.restoring);
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
            if (checking) CancelCheck();
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
            if (instance != null && instance.checking) instance.CancelCheck();
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
            UpdateTransportClock();
            // Update uses real time even when gameplay is paused. A failed
            // transport/timer must never permanently lock the recovery dialog.
            if (checking && Time.realtimeSinceStartupAsDouble >= checkDeadline)
                CancelCheck(true);
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
            if (busy || probe != null || ticket == null || suppressOffer || offered || lost || SharedRunContext.Active ||
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
            int attempt = BeginCheck();
            var requested = ticket;
            offered = !lost;
            Message = "Проверяем, продолжает ли экипаж забег…";
            try
            {
                if (requested.Version != Application.version || requested.Generator != RunRandom.Version)
                { Message = "Версия забега изменилась. Вернуться не получится."; InvalidateReturn(); return; }
                var lobby = FindAnyObjectByType<LobbyOnlineBootstrap>();
                if (lobby != null) await AwaitCheck(lobby.SuspendForRecoveryAsync(), attempt, 7, "stop lobby");
                if (lost) await AwaitCheck(SharedRunLifetime.ReleaseLostTransportAsync(), attempt, 7, "stop old run");
                await AwaitCheck(StopProbeAsync(), attempt, 7, "stop previous probe");
                var root = new GameObject("Shared return runner");
                DontDestroyOnLoad(root);
                probe = root.AddComponent<NetworkRunner>();
                var scenes = root.AddComponent<LobbySceneManager>();
                var provider = root.AddComponent<NetworkObjectProviderDefault>();
                var start = probe.StartGame(new StartGameArgs
                {
                    GameMode = GameMode.Shared, Config = LobbyOnlineBootstrap.CreateDepartureNetworkConfig(),
                    CustomPhotonAppSettings = Settings(requested.Region), PlayerUniqueId = requested.PlayerId,
                    SessionName = requested.Room, CustomLobbyName = requested.Lobby,
                    EnableClientSessionCreation = false, SceneManager = scenes, ObjectProvider = provider
                });
                Message = "Подключаемся к экипажу…";
                await AwaitCheck(start, attempt, 25, "connect to run");
                var result = await start;
                EnsureCheck(attempt);
                if (!result.Ok)
                {
                    if (result.ShutdownReason.ToString().Contains("GameNotFound") ||
                        result.ShutdownReason.ToString().Contains("GameClosed"))
                    { Message = "Экипаж уже завершил забег. Можно начать новую поездку."; InvalidateReturn(); }
                    else Message = "Не удалось проверить экипаж. Попробуй переподключиться позже.";
                    await AwaitCheck(StopProbeAsync(), attempt, 7, "stop rejected probe"); return;
                }
                double until = Time.realtimeSinceStartupAsDouble + 8;
                Message = "Получаем состояние забега…";
                checkStage = "receive run state";
                checkDeadline = until + 1;
                while (probeState == null && Time.realtimeSinceStartupAsDouble < until)
                {
                    probeState = FindObjectsByType<SharedDepartureState>(FindObjectsSortMode.None)
                        .FirstOrDefault(s => s.Runner == probe && s.Object != null && s.Object.IsValid);
                    if (probeState == null) await CheckYield(attempt);
                }
                while (probeState != null && probeState.Phase == 3 && !probeState.HasOtherPlayingCrew(probe.LocalPlayer) &&
                    Time.realtimeSinceStartupAsDouble < until)
                    await CheckYield(attempt);
                EnsureCheck(attempt);
                if (probeState == null || probeState.Seed != requested.Seed ||
                    !probeState.CanReturn(probe.LocalPlayer) || !probeState.HasOtherPlayingCrew(probe.LocalPlayer))
                { Message = "Этот забег уже недоступен для возврата."; InvalidateReturn(); await AwaitCheck(StopProbeAsync(), attempt, 7, "stop unavailable probe"); return; }
                Message = "Экипаж ещё в пути. Можно вернуться к команде.";
            }
            catch (OperationCanceledException) { }
            catch (Exception exception)
            {
                if (attempt == checkVersion)
                {
                    Debug.LogWarning("[Shared recovery] Check failed at " + checkStage + ": " + exception.GetType().Name);
                    Message = "Соединение пока недоступно. Можно повторить попытку.";
                    try { await AwaitCheck(StopProbeAsync(), attempt, 7, "cleanup failed check"); }
                    catch (OperationCanceledException) { }
                }
            }
            finally { EndCheck(attempt); }
        }
        private int BeginCheck()
        {
            checking = busy = true;
            UpdateTransportClock();
            checkCanceled = new TaskCompletionSource<bool>();
            checkStage = "prepare";
            checkDeadline = Time.realtimeSinceStartupAsDouble + 7;
            return ++checkVersion;
        }
        private void EnsureCheck(int attempt)
        {
            if (!checking || attempt != checkVersion) throw new OperationCanceledException();
        }
        private void EndCheck(int attempt)
        {
            if (attempt == checkVersion) checking = busy = false;
        }
        private void UpdateTransportClock()
        {
            if (NeedsTransportClock)
            {
                if (!transportClock) { previousTimeScale = Time.timeScale; transportClock = true; }
                // Local controls remain blocked by the modal/lost context. Fusion
                // startup and shutdown must keep receiving engine frames.
                Time.timeScale = 1;
            }
            else if (transportClock)
            {
                transportClock = false;
                if (!SharedRunContext.Playing && !SoloRunBranch.Active)
                    Time.timeScale = lost ? 0 : previousTimeScale;
            }
        }
        private async Task AwaitCheck(Task task, int attempt, double seconds, string stage)
        {
            EnsureCheck(attempt);
            checkStage = stage;
            checkDeadline = Time.realtimeSinceStartupAsDouble + seconds;
            var cancellation = checkCanceled.Task;
            await Task.WhenAny(task, cancellation);
            EnsureCheck(attempt);
            await task;
        }
        private async Task CheckYield(int attempt)
        {
            await Task.WhenAny(Task.Delay(100), checkCanceled.Task);
            EnsureCheck(attempt);
        }
        public void CancelCheck() => CancelCheck(false);
        private void CancelCheck(bool timedOut)
        {
            if (!checking) return;
            Debug.LogWarning("[Shared recovery] " + (timedOut ? "Timeout" : "Canceled") + " at " + checkStage);
            ++checkVersion;
            checking = busy = false;
            checkCanceled?.TrySetResult(true);
            offered = !lost;
            nextAttempt = Time.realtimeSinceStartupAsDouble + 15;
            Message = timedOut ? "Подключение заняло слишком долго. Можно повторить попытку." : "Проверка остановлена. Можно повторить подключение позже.";
            var current = probe;
            _ = StopProbeAsync();
            // Do not keep an unresponsive runner alive alongside the next attempt.
            if (current != null) Destroy(current.gameObject);
            if (retiringProbe != null) Destroy(retiringProbe.gameObject);
        }
        public async Task ResumeAsync()
        {
            if (busy) return;
            if (probeState == null)
            {
                int expectedCheck = checkVersion + 1;
                await CheckAsync();
                if (checkVersion != expectedCheck) return;
            }
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
            retiringProbe = current;
            try
            {
                var shutdown = current.Shutdown();
                if (await Task.WhenAny(shutdown, Task.Delay(5000)) == shutdown) await shutdown;
                else Debug.LogWarning("[Shared recovery] Probe shutdown timed out.");
            }
            catch (Exception) { }
            if (retiringProbe == current) retiringProbe = null;
            if (current != null) Destroy(current.gameObject);
        }
    }
}
