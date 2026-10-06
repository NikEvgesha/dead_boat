using System;
using System.Threading.Tasks;
using MirraCloud;
using MirraCloud.Core;
using MirraCloud.Core.Friends.Dto;
using UnityEngine;

namespace DeadBoat.Online
{
    // One owner for the SDK/session across Lobby -> Run -> Lobby transitions.
    // Photon identity is deliberately separate until server authentication is available.
    public sealed class MirraSocialService : MonoBehaviour
    {
        private static MirraSocialService instance;
        private MirraCloudSDK sdk;
        private Task<bool> connection;
        private bool restored;
        private float retryAfter;
        private float refreshAfter;
        private float mutationAfter;
        public bool Busy { get; private set; }
        public bool Ready => sdk?.Authentication?.IsAuth == true && !string.IsNullOrEmpty(PlayerId);
        public string PlayerId => sdk?.Authentication?.IsAuth == true ? sdk.PlayerAccount?.PlayerAccountInfo?.Id : null;
        public string Status { get; private set; } = "Откройте друзей для подключения";
        public GetPlayerDto[] Friends { get; private set; } = Array.Empty<GetPlayerDto>();
        public GetFriendRequestDto[] Incoming { get; private set; } = Array.Empty<GetFriendRequestDto>();
        public GetFriendRequestDto[] Outgoing { get; private set; } = Array.Empty<GetFriendRequestDto>();
        public event Action Changed;

        public static MirraSocialService Instance
        {
            get
            {
                if (instance == null)
                {
                    var root = new GameObject("Mirra social session");
                    DontDestroyOnLoad(root);
                    instance = root.AddComponent<MirraSocialService>();
                }
                return instance;
            }
        }

        public Task<bool> ConnectAsync()
        {
            if (Ready) return Task.FromResult(true);
            if (connection != null && !connection.IsCompleted) return connection;
            if (Time.realtimeSinceStartup < retryAfter) return Task.FromResult(false);
            connection = ConnectCoreAsync();
            return connection;
        }

        private async Task<bool> ConnectCoreAsync()
        {
            Status = "Подключение Mirra…";
            Changed?.Invoke();
            try
            {
                var config = Configuration.Load();
                if (string.IsNullOrWhiteSpace(config.ProjectId) || string.IsNullOrWhiteSpace(config.BranchId) ||
                    string.IsNullOrWhiteSpace(config.PlatformKey))
                    throw new InvalidOperationException();
                if (sdk == null)
                {
                    sdk = MirraCloudSDK.Create();
                    sdk.Initialize();
                    sdk.Authentication.OnSessionExpired += OnExpired;
                }
                if (!restored)
                {
                    var restore = sdk.Authentication.InitializeAsync();
                    await restore.Task();
                    // A failed restore may be temporary: do not silently create another identity.
                    if (!restore.Result.IsSuccess) return Failure(restore.Result);
                    restored = true;
                }
                if (!sdk.Authentication.IsAuth)
                {
                    var login = sdk.Authentication.LoginGuestAsync();
                    await login.Task();
                    if (!login.Result.IsSuccess) return Failure(login.Result);
                }
                if (string.IsNullOrEmpty(PlayerId))
                {
                    var account = sdk.PlayerAccount.GetAccountAsync();
                    await account.Task();
                    if (!account.Result.IsSuccess) return Failure(account.Result);
                }
                Status = "Подключено. Гостевой профиль этого браузера";
                return true;
            }
            catch (Exception)
            {
                Status = "Mirra недоступна. Одиночная игра работает";
                retryAfter = Time.realtimeSinceStartup + 5;
                return false;
            }
            finally { Changed?.Invoke(); }
        }

        private bool Failure(RestApiResult result)
        {
            // Never log raw responses, auth tokens, account IDs or server messages.
            Status = "Mirra: запрос не выполнен (HTTP " + result?.HttpStatusCode + "). Повторите позже";
            retryAfter = Time.realtimeSinceStartup + 5;
            return false;
        }

        public async Task RefreshAsync()
        {
            if (Busy || Time.realtimeSinceStartup < refreshAfter) return;
            Busy = true;
            Changed?.Invoke();
            try
            {
                if (!await ConnectAsync()) return;
                var friends = sdk.Friends.GetFriendsAsync();
                await friends.Task();
                if (!friends.Result.IsSuccess) { Failure(friends.Result); return; }
                var incoming = sdk.Friends.GetIncomingAsync();
                await incoming.Task();
                if (!incoming.Result.IsSuccess) { Failure(incoming.Result); return; }
                var outgoing = sdk.Friends.GetOutgoingAsync();
                await outgoing.Task();
                if (!outgoing.Result.IsSuccess) { Failure(outgoing.Result); return; }
                Friends = friends.Result.Data ?? Array.Empty<GetPlayerDto>();
                Incoming = incoming.Result.Data ?? Array.Empty<GetFriendRequestDto>();
                Outgoing = outgoing.Result.Data ?? Array.Empty<GetFriendRequestDto>();
                Status = "Список друзей обновлён";
            }
            catch (Exception) { Status = "Не удалось обновить друзей. Повторите позже"; }
            finally
            {
                refreshAfter = Time.realtimeSinceStartup + 5;
                Busy = false;
                Changed?.Invoke();
            }
        }

        public async Task ChangeFriendAsync(string playerId, string action)
        {
            if (Busy) return;
            if (Time.realtimeSinceStartup < mutationAfter)
            { Status = "Подождите несколько секунд перед следующим запросом"; Changed?.Invoke(); return; }
            playerId = playerId?.Trim();
            if (string.IsNullOrEmpty(playerId) || playerId.Length > 128 || playerId == PlayerId)
            { Status = "Введите ID другого игрока Mirra"; Changed?.Invoke(); return; }
            Busy = true;
            Changed?.Invoke();
            bool success = false;
            try
            {
                if (!await ConnectAsync()) return;
                if (playerId == PlayerId) { Status = "Нельзя добавить себя"; return; }
                var operation = action switch
                {
                    "send" => sdk.Friends.SendAsync(playerId),
                    "accept" => sdk.Friends.AcceptAsync(playerId),
                    "reject" => sdk.Friends.RejectAsync(playerId),
                    "revoke" => sdk.Friends.RevokeAsync(playerId),
                    "remove" => sdk.Friends.RemoveFriendAsync(playerId),
                    _ => throw new ArgumentException()
                };
                await operation.Task();
                success = operation.Result.IsSuccess;
                if (!success) Failure(operation.Result);
            }
            catch (Exception) { Status = "Не удалось изменить друзей. Повторите позже"; }
            finally { mutationAfter = Time.realtimeSinceStartup + 2; Busy = false; Changed?.Invoke(); }
            if (success) { refreshAfter = 0; await RefreshAsync(); }
        }

        private void OnExpired()
        {
            Friends = Array.Empty<GetPlayerDto>();
            Incoming = Outgoing = Array.Empty<GetFriendRequestDto>();
            Status = "Сессия Mirra завершена. Подключитесь снова";
            Changed?.Invoke();
        }

        private void OnDestroy()
        {
            if (instance != this) return;
            instance = null;
            if (sdk != null) sdk.Authentication.OnSessionExpired -= OnExpired;
            sdk?.Dispose();
        }
    }
}
