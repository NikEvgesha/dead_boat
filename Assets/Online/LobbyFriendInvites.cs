using System;
using System.Linq;
using System.Threading.Tasks;
using MirraCloud.Core.Groups.Dto.Request;
using UnityEngine;

namespace DeadBoat.Online
{
    // SDK 0.10.0 can accept a targeted invite but has no incoming-invite inbox.
    // The code is therefore delivered by the players, not by an unverified client notification.
    public sealed class LobbyFriendInvites : MonoBehaviour
    {
        private const string PendingGroupKey = "DeadBoat.PendingInviteGroup.v1";
        [Serializable]
        private sealed class Rendezvous
        {
            public int schema = 1;
            public string room;
            public string region;
            public string version;
            public string target;
            public long expires;
            public bool ready;
        }

        private LobbyOnlineBootstrap lobby;
        private MirraSocialService social;
        private bool busy;
        private int generation;
        public string Code { get; private set; }
        public string Status { get; private set; } = "";
        public bool Busy => busy;
        public event Action Changed;

        public void Initialize(LobbyOnlineBootstrap owner)
        {
            lobby = owner;
            social = MirraSocialService.Instance;
        }

        public async Task InviteAsync(string profileId)
        {
            if (busy || !lobby.IsOnline || lobby.IsInDepartureRoom ||
                !social.Friends.Any(f => f?.PlayerId == profileId))
            {
                SetStatus("Для приглашения подключитесь к лобби и выберите друга.");
                return;
            }

            busy = true;
            int epoch = ++generation;
            string groupId = null;
            string inviteId = null;
            bool hiddenRoom = false;
            try
            {
                if (!await social.ConnectAsync()) throw new InvalidOperationException("Mirra offline");
                await CleanupPreviousGroupAsync();
                string region = lobby.CurrentRegion;
                string currentRoom = lobby.CurrentSessionName;
                bool roomFull = lobby.LobbyPlayerCount >= lobby.LobbyCapacity;
                var state = new Rendezvous
                {
                    room = roomFull ? "" : currentRoom,
                    region = region,
                    version = Application.version,
                    target = profileId,
                    expires = DateTimeOffset.UtcNow.AddMinutes(3).ToUnixTimeSeconds(),
                    ready = false
                };
                var created = social.Sdk.Groups.CreateAsync(new CreateGroupDto
                {
                    Name = "Dead Boat lobby invite",
                    Visibility = "private",
                    JoinPolicy = "invite",
                    MaxMembers = 2,
                    Metadata = JsonUtility.ToJson(state),
                    CreateChat = false,
                    AutoJoinMembers = false
                });
                await WaitForSdk(created.Task());
                if (!created.Result.IsSuccess || created.Result.Data == null)
                    throw new InvalidOperationException("Could not create private invite group");
                groupId = created.Result.Data.GroupId;
                if (!ValidServerId(groupId)) throw new InvalidOperationException("Invalid group ID");
                PlayerPrefs.SetString(PendingGroupKey, groupId);
                PlayerPrefs.Save();
                var invited = social.Sdk.Groups.CreateInviteAsync(groupId, new CreateInviteDto
                {
                    TargetPlayerId = profileId,
                    InviteType = "direct",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(3)
                });
                await WaitForSdk(invited.Task());
                if (!invited.Result.IsSuccess || invited.Result.Data == null)
                    throw new InvalidOperationException("Could not create targeted invite");
                inviteId = invited.Result.Data.InviteId;
                if (!ValidServerId(inviteId)) throw new InvalidOperationException("Invalid invite ID");
                Code = "DB1:" + groupId + ":" + inviteId;
                GUIUtility.systemCopyBuffer = Code;
                SetStatus(roomFull
                    ? "Код скопирован. Новое лобби после принятия. Регион: " + region + "; версия: " + state.version
                    : "Код скопирован. Лобби: " + currentRoom + "; регион: " + region + "; версия: " + state.version);

                bool accepted = false;
                while (IsCurrent(epoch) && DateTimeOffset.UtcNow.ToUnixTimeSeconds() < state.expires)
                {
                    await Task.Delay(2000);
                    var members = social.Sdk.Groups.GetMembersAsync(groupId, 1, 10);
                    await WaitForSdk(members.Task());
                    if (!members.Result.IsSuccess) continue;
                    accepted = members.Result.Data?.Items?.Any(m => m.ProfileId == profileId) == true;
                    if (accepted) break;
                }
                if (!accepted || !IsCurrent(epoch))
                {
                    SetStatus("Приглашение истекло или отменено.");
                    return;
                }

                if (roomFull || lobby.CurrentSessionName != currentRoom ||
                    lobby.LobbyPlayerCount >= lobby.LobbyCapacity)
                {
                    string newRoom = "friends-" + Guid.NewGuid().ToString("N");
                    if (!await lobby.JoinFriendLobbyAsync(newRoom, region, true, true))
                        throw new InvalidOperationException("Could not create party room");
                    hiddenRoom = true;
                    state.room = newRoom;
                }
                state.ready = true;
                var updated = social.Sdk.Groups.UpdateAsync(groupId,
                    new UpdateGroupDto { Metadata = JsonUtility.ToJson(state) });
                await WaitForSdk(updated.Task());
                if (!updated.Result.IsSuccess)
                    throw new InvalidOperationException("Could not publish party room");
                SetStatus("Лобби готово. Ожидание друга…");
                DateTime fullSince = DateTime.MinValue;

                while (IsCurrent(epoch) && DateTimeOffset.UtcNow.ToUnixTimeSeconds() < state.expires)
                {
                    if (lobby.IsOnline && lobby.CurrentSessionName == state.room &&
                        FriendPresent(profileId))
                    {
                        if (hiddenRoom) lobby.OpenFriendLobbyToPublic();
                        SetStatus("Друг вошёл в лобби. Комната открыта для остальных.");
                        return;
                    }
                    // A public room can fill while the invited client is connecting.
                    // Publish a different room instead of sending the friend through random matchmaking.
                    if (!hiddenRoom && lobby.IsOnline && lobby.CurrentSessionName == state.room &&
                        lobby.LobbyPlayerCount >= lobby.LobbyCapacity)
                    {
                        if (fullSince == DateTime.MinValue) fullSince = DateTime.UtcNow;
                        if ((DateTime.UtcNow - fullSince).TotalSeconds < 3)
                        {
                            await Task.Delay(500);
                            continue;
                        }
                        string replacement = "friends-" + Guid.NewGuid().ToString("N");
                        if (!await lobby.JoinFriendLobbyAsync(replacement, region, true, true))
                            throw new InvalidOperationException("Could not relocate full lobby");
                        hiddenRoom = true;
                        state.room = replacement;
                        var relocation = social.Sdk.Groups.UpdateAsync(groupId,
                            new UpdateGroupDto { Metadata = JsonUtility.ToJson(state) });
                        await WaitForSdk(relocation.Task());
                        if (!relocation.Result.IsSuccess)
                            throw new InvalidOperationException("Could not publish relocated room");
                        SetStatus("Лобби заполнилось. Новая комната готова для друга.");
                    }
                    else fullSince = DateTime.MinValue;
                    await Task.Delay(500);
                }
                // Do not leave a hidden room inaccessible if a friend failed to join.
                if (hiddenRoom) lobby.OpenFriendLobbyToPublic();
                SetStatus("Друг не вошёл вовремя. Лобби открыто для остальных.");
            }
            catch (Exception exception)
            {
                if (hiddenRoom) lobby.OpenFriendLobbyToPublic();
                Debug.LogWarning("[Lobby invite] Failed: " + exception.GetType().Name);
                SetStatus("Не удалось пригласить или перевести друга. Проверьте сеть и повторите.");
            }
            finally
            {
                busy = false;
                Changed?.Invoke();
                // Group is transient. The recipient has already consumed its metadata by now.
                if (groupId != null && social.Ready)
                {
                    try
                    {
                        if (inviteId != null)
                        {
                            var revoke = social.Sdk.Groups.RevokeInviteAsync(groupId, inviteId);
                            await WaitForSdk(revoke.Task());
                        }
                        var delete = social.Sdk.Groups.DeleteAsync(groupId);
                        await WaitForSdk(delete.Task());
                        if (delete.Result.IsSuccess && PlayerPrefs.GetString(PendingGroupKey) == groupId)
                        {
                            PlayerPrefs.DeleteKey(PendingGroupKey);
                            PlayerPrefs.Save();
                        }
                    }
                    catch (Exception) { /* Cleanup is best effort; expiry still protects the invite. */ }
                }
            }
        }

        public async Task JoinAsync(string code)
        {
            if (busy || !lobby.IsOnline || lobby.IsInDepartureRoom) return;
            var parts = (code ?? "").Trim().Split(':');
            if (parts.Length != 3 || parts[0] != "DB1" ||
                !ValidServerId(parts[1]) || !ValidServerId(parts[2]))
            {
                SetStatus("Неверный код приглашения.");
                return;
            }

            busy = true;
            int epoch = ++generation;
            try
            {
                if (!await social.ConnectAsync()) throw new InvalidOperationException("Mirra offline");
                var accepted = social.Sdk.Groups.AcceptInviteAsync(parts[1], parts[2]);
                await WaitForSdk(accepted.Task());
                if (!accepted.Result.IsSuccess)
                    throw new InvalidOperationException("Invite expired or belongs to another player");
                SetStatus("Приглашение принято. Ожидание комнаты друга…");
                string failedRoom = null;
                for (int attempt = 0; attempt < 60 && IsCurrent(epoch); attempt++)
                {
                    var group = social.Sdk.Groups.GetAsync(parts[1]);
                    await WaitForSdk(group.Task());
                    if (!group.Result.IsSuccess || group.Result.Data == null)
                        throw new InvalidOperationException("Invite group disappeared");
                    Rendezvous state = JsonUtility.FromJson<Rendezvous>(group.Result.Data.Metadata ?? "{}");
                    if (state.schema != 1 || state.target != social.FriendId ||
                        state.version != Application.version ||
                        DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= state.expires)
                        throw new InvalidOperationException("Invite version, identity or expiry mismatch");
                    if (state.region != lobby.CurrentRegion)
                        throw new InvalidOperationException("Friend is in another Photon region");
                    if (state.ready && !string.IsNullOrEmpty(state.room) && state.room != failedRoom &&
                        lobby.IsOnline && !lobby.IsConnecting)
                    {
                        SetStatus("Вход в " + state.room + " (" + state.region + ", v" + state.version + ")…");
                        if (!await lobby.JoinFriendLobbyAsync(state.room, state.region, false, false))
                        {
                            failedRoom = state.room;
                            SetStatus("Комната заполнилась. Ожидание другого лобби…");
                            continue;
                        }
                        SetStatus("Вы в лобби друга.");
                        lobby.OpenFriendLobbyToPublic();
                        return;
                    }
                    await Task.Delay(1000);
                }
                throw new TimeoutException("Friend room was not ready");
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Lobby invite] Join failed: " + exception.GetType().Name);
                SetStatus("Не удалось войти по приглашению. Вы остались в текущем лобби или переподключаетесь.");
            }
            finally { busy = false; Changed?.Invoke(); }
        }

        private bool IsCurrent(int epoch) => this != null && isActiveAndEnabled && epoch == generation;
        private async Task CleanupPreviousGroupAsync()
        {
            string previous = PlayerPrefs.GetString(PendingGroupKey, "");
            if (!ValidServerId(previous)) return;
            try
            {
                var delete = social.Sdk.Groups.DeleteAsync(previous);
                await WaitForSdk(delete.Task());
                if (delete.Result.IsSuccess || delete.Result.HttpStatusCode == 404)
                {
                    PlayerPrefs.DeleteKey(PendingGroupKey);
                    PlayerPrefs.Save();
                }
            }
            catch (Exception) { /* Retry on the next invitation. */ }
        }
        private static async Task WaitForSdk(Task request)
        {
            if (await Task.WhenAny(request, Task.Delay(TimeSpan.FromSeconds(12))) != request)
                throw new TimeoutException("Mirra request timed out");
            await request;
        }
        private bool FriendPresent(string profileId)
        {
            foreach (var avatar in LobbyNetworkAvatar.All)
                if (avatar.Runner == lobby.LobbyRunner &&
                    avatar.MirraProfileId.ToString() == profileId) return true;
            return false;
        }
        private static bool ValidServerId(string value) => !string.IsNullOrEmpty(value) && value.Length <= 64 &&
            value.All(c => char.IsLetterOrDigit(c) || c == '-');
        private void SetStatus(string message) { Status = message; Changed?.Invoke(); }
        private void OnDestroy() { ++generation; }
    }
}
