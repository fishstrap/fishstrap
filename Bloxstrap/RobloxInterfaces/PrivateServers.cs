using Bloxstrap.AppData;

namespace Bloxstrap.RobloxInterfaces
{
    public static class PrivateServers
    {
        private const int PageSize = 25;

        private static Uri ServerUrl(long id) => new($"https://games.roblox.com/v1/vip-servers/{id}");

        private static Uri PermissionsUrl(long id) => new($"https://games.roblox.com/v1/vip-servers/{id}/permissions");

        public static Task<PrivateServersPage> ListPageAsync(long placeId, string? cursor)
        {
            string url = $"https://games.roblox.com/v1/games/{placeId}/private-servers?limit={PageSize}&sortOrder=Desc";

            if (!String.IsNullOrEmpty(cursor))
                url += $"&cursor={Uri.EscapeDataString(cursor)}";

            return Http.AuthGetJson<PrivateServersPage>(new Uri(url));
        }

        public static string ServersPageUrl(long placeId) => $"https://www.roblox.com/games/{placeId}#!/game-instances";

        public static Task<PrivateServerDetails> DetailsAsync(long id) => Http.AuthGetJson<PrivateServerDetails>(ServerUrl(id));

        public static Task RenameAsync(PrivateServerDetails current, string name) =>
            UpdateAsync(current, "name", name, name, current.Active, false);

        public static Task SetActiveAsync(PrivateServerDetails current, bool active) =>
            UpdateAsync(current, "active", active.ToString(), current.Name, active, false);

        public static Task RegenerateLinkAsync(PrivateServerDetails current) =>
            UpdateAsync(current, "newJoinCode", "true", current.Name, current.Active, true);

        public static Task SetFriendsAllowedAsync(PrivateServerDetails current, bool allowed) =>
            UpdatePermissionsAsync(current, "friendsAllowed", allowed.ToString(), allowed, null, null);

        public static Task AddUserAsync(PrivateServerDetails current, PrivateServerUser user) =>
            UpdatePermissionsAsync(current, "usersToAdd", user.Id.ToString(), null, user.Id, null);

        public static Task RemoveUserAsync(PrivateServerDetails current, PrivateServerUser user) =>
            UpdatePermissionsAsync(current, "usersToRemove", user.Id.ToString(), null, null, user.Id);

        public static async Task<PrivateServerUser?> FindUserAsync(string username)
        {
            var body = new Dictionary<string, object> { ["usernames"] = new[] { username }, ["excludeBannedUsers"] = true };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://users.roblox.com/v1/usernames/users")
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };

            var response = await Http.SendJson<UsernameLookupResponse>(request);

            return response.Data.FirstOrDefault();
        }

        public static void Join(long placeId, string accessCode)
        {
            const string LOG_IDENT = "PrivateServers::Join";

            App.Logger.WriteLine(LOG_IDENT, $"Joining a private server of {placeId}");

            Process.Start(new RobloxPlayerData().ExecutablePath,
                $"roblox://experiences/start?placeId={placeId}&accessCode={Uri.EscapeDataString(accessCode)}");
        }

        private static Task UpdateAsync(PrivateServerDetails current, string setting, string value, string? name, bool active, bool newJoinCode) =>
            PatchAsync(ServerUrl(current.Id), setting, value, new Dictionary<string, object?>
            {
                ["name"] = name,
                ["active"] = active,
                ["newJoinCode"] = newJoinCode
            });

        private static Task UpdatePermissionsAsync(PrivateServerDetails current, string setting, string value, bool? friendsAllowed, long? addUser, long? removeUser)
        {
            PrivateServerPermissions permissions = current.Permissions ?? new PrivateServerPermissions();

            return PatchAsync(PermissionsUrl(current.Id), setting, value, new Dictionary<string, object?>
            {
                ["clanAllowed"] = permissions.ClanAllowed,
                ["enemyClanId"] = permissions.EnemyClanId,
                ["friendsAllowed"] = friendsAllowed ?? permissions.FriendsAllowed,
                ["usersToAdd"] = addUser is long add ? new[] { add } : Array.Empty<long>(),
                ["usersToRemove"] = removeUser is long remove ? new[] { remove } : Array.Empty<long>()
            });
        }

        private static Task PatchAsync(Uri url, string setting, string value, Dictionary<string, object?> body) =>
            AccountRequests.SendAsync(HttpMethod.Patch, url, body, setting, value);
    }
}
