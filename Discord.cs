using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using MajickDiscordWrapper.MajickRegex;
using MajickDiscordWrapper.Discord.Gateway;
using System.Timers;
using System.Security.Cryptography.X509Certificates;
using System.IO;

namespace MajickDiscordWrapper.Discord
{
    public class DiscordClient
    {
        private DiscordGateway _gateway;
        private List<DiscordGateway> _shards = new List<DiscordGateway>();
        public DiscordUser Bot { get; set; }
        public string BotToken { get; set; }
        public string Base64Icon { get; set; }
        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
        public string ClientToken { get; set; }
        public DiscordGateway Gateway { get { return _gateway; } }
        public List<DiscordGateway> Shards { get { return _shards; } }
        public DiscordClient(string client_id, string client_secret, string bot_token)
        {
            Bot = new DiscordUser();
            ClientID = client_id;
            ClientSecret = client_secret;
            BotToken = bot_token;
        }
        public void GetClientCredentials()
        {
            RestClient rcAuthClient;
            RestRequest rrAuthRequest;
            RestResponse rsAuthResponse;

            rcAuthClient = new RestClient("https://discord.com/api");
            rrAuthRequest = new RestRequest("/oauth2/token", Method.Post);
            rrAuthRequest.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            rrAuthRequest.AddParameter("client_id", ClientID);
            rrAuthRequest.AddParameter("client_secret", ClientSecret);
            rrAuthRequest.AddParameter("grant_type", "client_credentials");
            rrAuthRequest.AddParameter("scope", "identify bot guilds applications.commands");
            rsAuthResponse = rcAuthClient.Execute(rrAuthRequest);

            MajickRegex.JsonObject response = new MajickRegex.JsonObject(rsAuthResponse.Content);
            ClientToken = response.Attributes["access_token"].text_value;
        }
        public void GetGateway(List<DiscordGatewayIntent> Intents)
        {
            string sGateway;
            RestClient rcGatewayClient;
            RestRequest rrGatewayRequest;
            RestResponse rsGatewayResponse;

            rcGatewayClient = new RestClient("https://discord.com/api");
            rrGatewayRequest = new RestRequest("/gateway", Method.Get);
            rsGatewayResponse = rcGatewayClient.Execute(rrGatewayRequest);
            MajickRegex.JsonObject jsonGateway = new MajickRegex.JsonObject(rsGatewayResponse.Content);
            sGateway = jsonGateway.Attributes["url"].text_value;
            _gateway = new DiscordGateway(sGateway, this, Intents);
            _gateway.READY += _gateway_READY;
        }
        public void GetBotGateway(List<DiscordGatewayIntent> Intents)
        {
            int shards;
            string sGateway;
            RestClient rcGatewayClient;
            RestRequest rrGatewayRequest;
            RestResponse rsGatewayResponse;

            rcGatewayClient = new RestClient("https://discord.com/api");
            rrGatewayRequest = new RestRequest("/gateway/bot", Method.Get);
            rrGatewayRequest.AddParameter("token", "Bot " + BotToken);
            rsGatewayResponse = rcGatewayClient.Execute(rrGatewayRequest);
            MajickDiscordWrapper.MajickRegex.JsonObject jsonGateway = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGatewayResponse.Content.Substring(1, rsGatewayResponse.Content.Length - 2));
            sGateway = jsonGateway.Attributes["url"].text_value.Replace("\"", "");
            if (jsonGateway.Attributes.ContainsKey("shards"))
            {
                if (int.TryParse(jsonGateway.Attributes["shards"].text_value, out shards))
                {
                    if (shards == 1) { _gateway = new DiscordGateway(sGateway, this, Intents); }
                    else
                    {
                        for (int i = 0; i < shards; i++)
                        {
                            _shards.Add(new DiscordGateway(sGateway, this, Intents, i, shards));
                        }
                    }
                }
            }
            else { _gateway = new DiscordGateway(sGateway, this, Intents); }
            _gateway = new DiscordGateway(sGateway, this, Intents);
            _gateway.READY += _gateway_READY;
        }
        private void _gateway_READY(object sender, ReadyEventArgs e)
        {
            Bot = e.user;
        }
        public void ConnectAsync()
        {
            if (_gateway != null) { _gateway.Connect(); }
            else if (_shards.Count > 0)
            {
                foreach (DiscordGateway gateway in _shards)
                {
                    gateway.Connect();
                }
            }
        }
        public void ReconnectAsync()
        {
            if (_gateway != null) { _gateway.Reconnect(); }
            else if (_shards.Count > 0)
            {
                foreach (DiscordGateway gateway in _shards)
                {
                    gateway.Reconnect();
                }
            }
        }
        public string GetApplicationIcon()
        {
            string icon = "";
            string app_id = "";
            RestClient rcAppClient;
            RestRequest rrAppRequest;
            RestResponse rsAppResponse;
            rcAppClient = new RestClient("https://discord.com/api");
            rrAppRequest = new RestRequest("/oauth2/applications/@me", Method.Get);
            rrAppRequest.AddHeader("Content-Type", "application/json");
            rrAppRequest.AddHeader("Authorization", "Bot " + BotToken);
            rsAppResponse = rcAppClient.Execute(rrAppRequest);
            MajickDiscordWrapper.MajickRegex.JsonObject ApplicationObject = new MajickDiscordWrapper.MajickRegex.JsonObject(rsAppResponse.Content);
            if (ApplicationObject.Attributes.ContainsKey("icon")) { icon = ApplicationObject.Attributes["icon"].text_value; }
            if (ApplicationObject.Attributes.ContainsKey("id")) { app_id = ApplicationObject.Attributes["id"].text_value; }
            RestClient rcIconClient;
            RestRequest rrIconRequest;
            RestResponse rsIconResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject MessageRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject MessageResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcIconClient = new RestClient("https://cdn.discord.com/");
            rrIconRequest = new RestRequest("/app-icons/" + app_id + "/" + icon + ".png", Method.Get);
            rrIconRequest.RequestFormat = DataFormat.Json;
            rrIconRequest.AddHeader("Content-Type", "application/json");
            rsIconResponse = rcIconClient.Execute(rrIconRequest);
            byte[] avatar_bytes = Encoding.UTF8.GetBytes(rsIconResponse.Content);
            string avatar_string = System.Convert.ToBase64String(avatar_bytes);
            Base64Icon = avatar_string;
            return avatar_string;
        }
    }
    public class DiscordUser
    {
        public string auth_token { get; set; }
        public string id { get; set; }
        public string username { get; set; }
        public string mention { get { return "<@" + id + ">"; } }
        public string discriminator { get; set; }
        public string avatar { get; set; }
        public bool bot { get; set; }
        public bool system { get; set; }
        public bool mfa_enabled { get; set; }
        public string locale { get; set; }
        public bool verified { get; set; }
        public string email { get; set; }
        public NitroType type { get; set; }
        public int flags { get; set; }
        public Dictionary<string, DiscordGuild> guilds { get; set; }
        public Dictionary<string, DiscordChannel> group_message_channels { get; set; }
        public Dictionary<string, DiscordChannel> direct_message_channels { get; set; }
        public DiscordUser()
        {
            auth_token = "";
            username = "";
            guilds = new Dictionary<string, DiscordGuild>();
            group_message_channels = new Dictionary<string, DiscordChannel>();
            direct_message_channels = new Dictionary<string, DiscordChannel>();
        }
        public DiscordUser(string newToken, string newName = "")
        {
            auth_token = newToken;
            username = newName;
            guilds = new Dictionary<string, DiscordGuild>();
            group_message_channels = new Dictionary<string, DiscordChannel>();
            direct_message_channels = new Dictionary<string, DiscordChannel>();
        }
        public DiscordUser(MajickDiscordWrapper.MajickRegex.JsonObject user_object)
        {
            if (user_object.Attributes.ContainsKey("auth_token")) { auth_token = user_object.Attributes["auth_token"].text_value; }
            if (user_object.Attributes.ContainsKey("id")) { id = user_object.Attributes["id"].text_value; }
            if (user_object.Attributes.ContainsKey("username")) { username = user_object.Attributes["username"].text_value; }
            if (user_object.Attributes.ContainsKey("discriminator")) { discriminator = user_object.Attributes["discriminator"].text_value; }
            if (user_object.Attributes.ContainsKey("avatar")) { avatar = user_object.Attributes["avatar"].text_value; }
            if (user_object.Attributes.ContainsKey("bot"))
            {
                bool is_bot;
                if (bool.TryParse(user_object.Attributes["bot"].text_value, out is_bot)) { bot = is_bot; }
                else { bot = (true && false); }
            }
            else { bot = (true && false); }
            if (user_object.Attributes.ContainsKey("system"))
            {
                bool is_system;
                if (bool.TryParse(user_object.Attributes["system"].text_value, out is_system)) { system = is_system; }
                else { system = (true && false); }
            }
            else { system = (true && false); }
            if (user_object.Attributes.ContainsKey("mfa_enabled"))
            {
                bool is_mfa_enabled;
                if (bool.TryParse(user_object.Attributes["mfa_enabled"].text_value, out is_mfa_enabled)) { mfa_enabled = is_mfa_enabled; }
                else { mfa_enabled = (true && false); }
            }
            else { mfa_enabled = (true && false); }
            if (user_object.Attributes.ContainsKey("locale")) { locale = user_object.Attributes["locale"].text_value; }
            if (user_object.Attributes.ContainsKey("verified"))
            {
                bool is_verified;
                if (bool.TryParse(user_object.Attributes["verified"].text_value, out is_verified)) { verified = is_verified; }
                else { verified = (true && false); }
            }
            else { verified = (true && false); }
            if (user_object.Attributes.ContainsKey("email")) { locale = user_object.Attributes["email"].text_value; }
            if (user_object.Attributes.ContainsKey("flags"))
            {
                int temp_flags;
                if (int.TryParse(user_object.Attributes["flags"].text_value, out temp_flags)) { flags = temp_flags; }
                else { flags = -1; }
            }
            else { flags = -1; }
            if (user_object.Attributes.ContainsKey("type"))
            {
                NitroType nitro_type;
                if (Enum.TryParse(user_object.Attributes["verified"].text_value, out nitro_type)) { type = nitro_type; }
                else { type = NitroType.Unspecified; }
            }
            guilds = new Dictionary<string, DiscordGuild>();
            group_message_channels = new Dictionary<string, DiscordChannel>();
            direct_message_channels = new Dictionary<string, DiscordChannel>();
        }
        public async Task<Dictionary<string, DiscordGuild>> GetGuildsAsync(string token, bool is_bot = true) { return await Task.Run(() => GetGuilds(token, is_bot)); }
        public Dictionary<string, DiscordGuild> GetGuilds(string token, bool is_bot = true)
        {
            DiscordGuild guild;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordGuild> guilds = new Dictionary<string, DiscordGuild>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/users/@me/guilds", Method.Get);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            if (is_bot) { rrGuildRequest.AddHeader("Authorization", "Bot " + token); }
            else { rrGuildRequest.AddHeader("Authorization", "Bearer " + token); }
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_guild in GuildResponseContent.ObjectLists["objects"])
            {
                guild = new DiscordGuild(current_guild);
                guilds.Add(guild.id, guild);
            }
            return guilds;
        }
        public async Task<DiscordGuild> GetGuildByIDAsync(string guild_id, string bot_token) { return await Task.Run(() => GetGuildByID(guild_id, bot_token)); }
        public DiscordGuild GetGuildByID(string guild_id, string bot_token)
        {
            DiscordGuild guild;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject MessageRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject MessageResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + guild_id, Method.Get);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrGuildRequest.AddJsonBody(MessageRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            guild = new DiscordGuild(new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content));
            return guild;
        }
        public async Task<bool> LeaveGuildByIDAsync(string guild_id, string bot_token) { return await Task.Run(() => LeaveGuildByID(guild_id, bot_token)); }
        public bool LeaveGuildByID(string guild_id, string bot_token)
        {
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject MessageRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject MessageResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + guild_id, Method.Delete);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrGuildRequest.AddJsonBody(MessageRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            return rsGuildResponse.IsSuccessful;
        }
        public async Task<bool> JoinGuildByIDAsync(string guild_id, string bot_token) { return await Task.Run(() => JoinGuildByID(guild_id, bot_token)); }
        public bool JoinGuildByID(string guild_id, string bot_token)
        {
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject MessageRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject MessageResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + guild_id + "/members/" + id, Method.Put);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            MessageRequestBody.AddAttribute("auth_token", auth_token);
            rrGuildRequest.AddJsonBody(MessageRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            return rsGuildResponse.IsSuccessful;
        }
        public async Task<DiscordChannel> GetDMChannelAsync(string bot_token) { return await Task.Run(() => GetDMChannel(bot_token)); }
        public DiscordChannel GetDMChannel(string bot_token)
        {
            DiscordChannel dm_channel;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject MessageRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject MessageResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/users/@me/channels", Method.Post);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            MessageRequestBody.AddAttribute("recipient_id", id);
            rrGuildRequest.AddJsonBody(MessageRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            dm_channel = new DiscordChannel(new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content));
            return dm_channel;
        }
        public async Task<string> GetAvatarAsync() { return await Task.Run(() => GetAvatar()); }
        public string GetAvatar()
        {
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject MessageRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject MessageResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcGuildClient = new RestClient("https://cdn.discord.com/");
            rrGuildRequest = new RestRequest("/avatars/" + id + "/" + avatar + ".png", Method.Get);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            string avatar_string = rsGuildResponse.Content;
            return avatar_string;
        }
        public List<DiscordPermission> calculate_permissions(int perm_value)
        {
            List<DiscordPermission> my_perms = new List<DiscordPermission>();
            if (perm_value >= (int)DiscordPermission.MANAGE_EMOJIS)
            {
                my_perms.Add(DiscordPermission.MANAGE_EMOJIS);
                perm_value -= (int)DiscordPermission.MANAGE_EMOJIS;
            }
            if (perm_value >= (int)DiscordPermission.MANAGE_WEBHOOKS)
            {
                my_perms.Add(DiscordPermission.MANAGE_WEBHOOKS);
                perm_value -= (int)DiscordPermission.MANAGE_WEBHOOKS;
            }
            if (perm_value >= (int)DiscordPermission.MANAGE_ROLES)
            {
                my_perms.Add(DiscordPermission.MANAGE_ROLES);
                perm_value -= (int)DiscordPermission.MANAGE_ROLES;
            }
            if (perm_value >= (int)DiscordPermission.MANAGE_NICKNAMES)
            {
                my_perms.Add(DiscordPermission.MANAGE_NICKNAMES);
                perm_value -= (int)DiscordPermission.MANAGE_NICKNAMES;
            }
            if (perm_value >= (int)DiscordPermission.CHANGE_NICKNAME)
            {
                my_perms.Add(DiscordPermission.CHANGE_NICKNAME);
                perm_value -= (int)DiscordPermission.CHANGE_NICKNAME;
            }
            if (perm_value >= (int)DiscordPermission.USE_VAD)
            {
                my_perms.Add(DiscordPermission.USE_VAD);
                perm_value -= (int)DiscordPermission.USE_VAD;
            }
            if (perm_value >= (int)DiscordPermission.MOVE_MEMBERS)
            {
                my_perms.Add(DiscordPermission.MOVE_MEMBERS);
                perm_value -= (int)DiscordPermission.MOVE_MEMBERS;
            }
            if (perm_value >= (int)DiscordPermission.DEAFEN_MEMBERS)
            {
                my_perms.Add(DiscordPermission.DEAFEN_MEMBERS);
                perm_value -= (int)DiscordPermission.DEAFEN_MEMBERS;
            }
            if (perm_value >= (int)DiscordPermission.MUTE_MEMBERS)
            {
                my_perms.Add(DiscordPermission.MUTE_MEMBERS);
                perm_value -= (int)DiscordPermission.MUTE_MEMBERS;
            }
            if (perm_value >= (int)DiscordPermission.SPEAK)
            {
                my_perms.Add(DiscordPermission.SPEAK);
                perm_value -= (int)DiscordPermission.SPEAK;
            }
            if (perm_value >= (int)DiscordPermission.CONNECT)
            {
                my_perms.Add(DiscordPermission.CONNECT);
                perm_value -= (int)DiscordPermission.CONNECT;
            }
            if (perm_value >= (int)DiscordPermission.VIEW_GUILD_INSIGHTS)
            {
                my_perms.Add(DiscordPermission.VIEW_GUILD_INSIGHTS);
                perm_value -= (int)DiscordPermission.VIEW_GUILD_INSIGHTS;
            }
            if (perm_value >= (int)DiscordPermission.USE_EXTERNAL_EMOJIS)
            {
                my_perms.Add(DiscordPermission.USE_EXTERNAL_EMOJIS);
                perm_value -= (int)DiscordPermission.USE_EXTERNAL_EMOJIS;
            }
            if (perm_value >= (int)DiscordPermission.MENTION_EVERYONE)
            {
                my_perms.Add(DiscordPermission.MENTION_EVERYONE);
                perm_value -= (int)DiscordPermission.MENTION_EVERYONE;
            }
            if (perm_value >= (int)DiscordPermission.READ_MESSAGE_HISTORY)
            {
                my_perms.Add(DiscordPermission.READ_MESSAGE_HISTORY);
                perm_value -= (int)DiscordPermission.READ_MESSAGE_HISTORY;
            }
            if (perm_value >= (int)DiscordPermission.ATTACH_FILES)
            {
                my_perms.Add(DiscordPermission.ATTACH_FILES);
                perm_value -= (int)DiscordPermission.ATTACH_FILES;
            }
            if (perm_value >= (int)DiscordPermission.EMBED_LINKS)
            {
                my_perms.Add(DiscordPermission.EMBED_LINKS);
                perm_value -= (int)DiscordPermission.EMBED_LINKS;
            }
            if (perm_value >= (int)DiscordPermission.MANAGE_MESSAGES)
            {
                my_perms.Add(DiscordPermission.MANAGE_MESSAGES);
                perm_value -= (int)DiscordPermission.MANAGE_MESSAGES;
            }
            if (perm_value >= (int)DiscordPermission.SEND_TTS_MESSAGES)
            {
                my_perms.Add(DiscordPermission.SEND_TTS_MESSAGES);
                perm_value -= (int)DiscordPermission.SEND_TTS_MESSAGES;
            }
            if (perm_value >= (int)DiscordPermission.SEND_MESSAGES)
            {
                my_perms.Add(DiscordPermission.SEND_MESSAGES);
                perm_value -= (int)DiscordPermission.SEND_MESSAGES;
            }
            if (perm_value >= (int)DiscordPermission.VIEW_CHANNEL)
            {
                my_perms.Add(DiscordPermission.VIEW_CHANNEL);
                perm_value -= (int)DiscordPermission.VIEW_CHANNEL;
            }
            if (perm_value >= (int)DiscordPermission.STREAM)
            {
                my_perms.Add(DiscordPermission.STREAM);
                perm_value -= (int)DiscordPermission.STREAM;
            }
            if (perm_value >= (int)DiscordPermission.PRIORITY_SPEAKER)
            {
                my_perms.Add(DiscordPermission.PRIORITY_SPEAKER);
                perm_value -= (int)DiscordPermission.PRIORITY_SPEAKER;
            }
            if (perm_value >= (int)DiscordPermission.VIEW_AUDIT_LOG)
            {
                my_perms.Add(DiscordPermission.VIEW_AUDIT_LOG);
                perm_value -= (int)DiscordPermission.VIEW_AUDIT_LOG;
            }
            if (perm_value >= (int)DiscordPermission.ADD_REACTIONS)
            {
                my_perms.Add(DiscordPermission.ADD_REACTIONS);
                perm_value -= (int)DiscordPermission.ADD_REACTIONS;
            }
            if (perm_value >= (int)DiscordPermission.MANAGE_GUILDS)
            {
                my_perms.Add(DiscordPermission.MANAGE_GUILDS);
                perm_value -= (int)DiscordPermission.MANAGE_GUILDS;
            }
            if (perm_value >= (int)DiscordPermission.MANAGE_CHANNELS)
            {
                my_perms.Add(DiscordPermission.MANAGE_CHANNELS);
                perm_value -= (int)DiscordPermission.MANAGE_CHANNELS;
            }
            if (perm_value >= (int)DiscordPermission.ADMINISTRATOR)
            {
                my_perms.Add(DiscordPermission.ADMINISTRATOR);
                perm_value -= (int)DiscordPermission.ADMINISTRATOR;
            }
            if (perm_value >= (int)DiscordPermission.BAN_MEMBERS)
            {
                my_perms.Add(DiscordPermission.BAN_MEMBERS);
                perm_value -= (int)DiscordPermission.BAN_MEMBERS;
            }
            if (perm_value >= (int)DiscordPermission.KICK_MEMBERS)
            {
                my_perms.Add(DiscordPermission.KICK_MEMBERS);
                perm_value -= (int)DiscordPermission.KICK_MEMBERS;
            }
            if (perm_value >= (int)DiscordPermission.CREATE_INSTANT_INVITE)
            {
                my_perms.Add(DiscordPermission.CREATE_INSTANT_INVITE);
                perm_value -= (int)DiscordPermission.CREATE_INSTANT_INVITE;
            }
            return my_perms;
        }
        public List<UserFlag> NamedBadges()
        {
            int perm_value = flags;
            List<UserFlag> my_flags = new List<UserFlag>();
            if (perm_value >= (int)UserFlag.DiscordCertifiedModerator)
            {
                my_flags.Add(UserFlag.DiscordCertifiedModerator);
                perm_value -= (int)UserFlag.DiscordCertifiedModerator;
            }
            if (perm_value >= (int)UserFlag.VerifiedBotDeveloper)
            {
                my_flags.Add(UserFlag.VerifiedBotDeveloper);
                perm_value -= (int)UserFlag.VerifiedBotDeveloper;
            }
            if (perm_value >= (int)UserFlag.VerifiedBot)
            {
                my_flags.Add(UserFlag.VerifiedBot);
                perm_value -= (int)UserFlag.VerifiedBot;
            }
            if (perm_value >= (int)UserFlag.BugHunterLevel2)
            {
                my_flags.Add(UserFlag.BugHunterLevel2);
                perm_value -= (int)UserFlag.BugHunterLevel2;
            }
            if (perm_value >= (int)UserFlag.TeamUser)
            {
                my_flags.Add(UserFlag.TeamUser);
                perm_value -= (int)UserFlag.TeamUser;
            }
            if (perm_value >= (int)UserFlag.EarlySupporter)
            {
                my_flags.Add(UserFlag.EarlySupporter);
                perm_value -= (int)UserFlag.EarlySupporter;
            }
            if (perm_value >= (int)UserFlag.HouseBalance)
            {
                my_flags.Add(UserFlag.HouseBalance);
                perm_value -= (int)UserFlag.HouseBalance;
            }
            if (perm_value >= (int)UserFlag.HouseBrilliance)
            {
                my_flags.Add(UserFlag.HouseBrilliance);
                perm_value -= (int)UserFlag.HouseBrilliance;
            }
            if (perm_value >= (int)UserFlag.HouseBravery)
            {
                my_flags.Add(UserFlag.HouseBravery);
                perm_value -= (int)UserFlag.HouseBravery;
            }
            if (perm_value >= (int)UserFlag.BugHunterLevel1)
            {
                my_flags.Add(UserFlag.BugHunterLevel1);
                perm_value -= (int)UserFlag.BugHunterLevel1;
            }
            if (perm_value >= (int)UserFlag.HypesquadEvents)
            {
                my_flags.Add(UserFlag.HypesquadEvents);
                perm_value -= (int)UserFlag.HypesquadEvents;
            }
            if (perm_value >= (int)UserFlag.DiscordPartner)
            {
                my_flags.Add(UserFlag.DiscordPartner);
                perm_value -= (int)UserFlag.DiscordPartner;
            }
            if (perm_value >= (int)UserFlag.DiscordEmployee)
            {
                my_flags.Add(UserFlag.DiscordEmployee);
                perm_value -= (int)UserFlag.DiscordEmployee;
            }
            if(my_flags.Count == 0) { my_flags.Add(UserFlag.None); }
            return my_flags;
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject user_object = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (auth_token != null) { user_object.AddAttribute("auth_token", auth_token); }
            if (id != null) { user_object.AddAttribute("id", id); }
            if (username != null) { user_object.AddAttribute("username", username); }
            if (discriminator != null) { user_object.AddAttribute("discriminator", discriminator); }
            if (avatar != null) { user_object.AddAttribute("avatar", avatar); }
            if (bot != (true && false)) { user_object.AddAttribute("bot", bot.ToString(), true); }
            if (mfa_enabled != (true && false)) { user_object.AddAttribute("mfa_enabled", mfa_enabled.ToString(), true); }
            if (locale != null) { user_object.AddAttribute("locale", locale); }
            if (verified != (true && false)) { user_object.AddAttribute("verified", verified.ToString(), true); }
            if (email != null) { user_object.AddAttribute("email", email); }
            if (flags > -1) { user_object.AddAttribute("flags", flags.ToString(), true); }
            if (type != NitroType.Unspecified) { user_object.AddAttribute("type", ((int)type).ToString(), true); }
            return user_object;
        }
    }
    public class DiscordConnection
    {
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public bool revoked { get; set; }
        public List<DiscordIntegration> integrations { get; set; }
        public DiscordConnection() { }
        public DiscordConnection(MajickDiscordWrapper.MajickRegex.JsonObject connection_object)
        {
            bool temp_revoked = false;
            if (connection_object.Attributes.ContainsKey("id")) { id = connection_object.Attributes["id"].text_value; }
            if (connection_object.Attributes.ContainsKey("name")) { name = connection_object.Attributes["name"].text_value; }
            if (connection_object.Attributes.ContainsKey("type")) { type = connection_object.Attributes["type"].text_value; }
            if (connection_object.Attributes.ContainsKey("revoked"))
            {
                if (bool.TryParse(connection_object.Attributes["revoked"].text_value, out temp_revoked)) { revoked = temp_revoked; }
                else { revoked = (true && false); }
            }
            else { revoked = (true && false); }
            if (connection_object.ObjectLists.ContainsKey("integrations"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_integration in connection_object.ObjectLists["integrations"])
                {
                    integrations.Add(new DiscordIntegration(current_integration));
                }
            }
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject connection_object = new MajickDiscordWrapper.MajickRegex.JsonObject();
            List<MajickDiscordWrapper.MajickRegex.JsonObject> integration_objects = new List<MajickDiscordWrapper.MajickRegex.JsonObject>();
            if (id != null) { connection_object.AddAttribute("id", id); }
            if (name != null) { connection_object.AddAttribute("name", name); }
            if (type != null) { connection_object.AddAttribute("type", type); }
            if (revoked != (true && false)) { connection_object.AddAttribute("revoked", revoked.ToString(), true); }
            if (integrations != null)
            {
                foreach (DiscordIntegration integration in integrations) { integration_objects.Add(integration.ToJson()); }
                connection_object.AddObjectList("integrations", integration_objects);
            }
            return connection_object;
        }
    }
    public class TempRole : System.Timers.Timer
    {
        public string Token { get; set; }
        public string GuildID { get; set; }
        public string MemberID { get; set; }
        public string ChannelID { get; set; }
        public string RoleID { get; set; }
        public TempRole(string role_id, int expires_in) : base(expires_in)
        {
            RoleID = role_id;
        } 
    }
    public class DiscordGuildMember
    {
        public string guild_id { get; set; }
        public DiscordUser user { get; set; }
        public string nick { get; set; }
        public List<string> roles { get; set; }
        public DateTime joined_at { get; set; }
        public DateTime premium_since { get; set; }
        public bool deaf { get; set; }
        public bool mute { get; set; }
        public bool pending { get; set; }
        public string permissions { get; set; }
        public List<DiscordRole> assigned_roles { get; set; }
        public List<string> before_roles { get; set; }
        public List<string> sniped_roles { get; set; }
        public List<string> removed_roles { get; set; }
        public MajickDiscordWrapper.MajickRegex.JsonObject guildmember_object { get; set; }
        public Dictionary<string, TempRole> TempRoles { get; set; }
        public DiscordGuildMember() { }
        public DiscordGuildMember(MajickDiscordWrapper.MajickRegex.JsonObject member_object, string my_guild_id = "")
        {
            guild_id = my_guild_id;
            guildmember_object = member_object;
            if (member_object.Objects.ContainsKey("user")) { user = new DiscordUser(member_object.Objects["user"]); }
            if (member_object.Attributes.ContainsKey("nick")) { nick = member_object.Attributes["nick"].text_value; }
            if (member_object.Attributes.ContainsKey("guild_id")) { guild_id = member_object.Attributes["guild_id"].text_value; }
            roles = new List<string>();
            if (member_object.AttributeLists.ContainsKey("roles"))
            {
                foreach (JsonAttribute feature in member_object.AttributeLists["roles"]) { roles.Add(feature.text_value); }
            }
            if (member_object.Attributes.ContainsKey("joined_at"))
            {
                DateTime when_joined;
                if (DateTime.TryParse(member_object.Attributes["joined_at"].text_value, out when_joined)) { joined_at = when_joined; }
            }
            if (member_object.Attributes.ContainsKey("premium_since"))
            {
                DateTime when_premium;
                if (DateTime.TryParse(member_object.Attributes["premium_since"].text_value, out when_premium)) { premium_since = when_premium; }
            }
            if (member_object.Attributes.ContainsKey("deaf"))
            {
                bool is_deaf;
                if (bool.TryParse(member_object.Attributes["deaf"].text_value, out is_deaf)) { deaf = is_deaf; }
                else { deaf = (true && false); }
            }
            else { deaf = (true && false); }
            if (member_object.Attributes.ContainsKey("mute"))
            {
                bool is_muted;
                if (bool.TryParse(member_object.Attributes["mute"].text_value, out is_muted)) { mute = is_muted; }
                else { mute = (true && false); }
            }
            else { mute = (true && false); }
            if (member_object.Attributes.ContainsKey("pending"))
            {
                bool is_pending;
                if (bool.TryParse(member_object.Attributes["pending"].text_value, out is_pending)) { pending = is_pending; }
                else { pending = (true && false); }
            }
            else { mute = (true && false); }
            if (member_object.Attributes.ContainsKey("permissions")) { permissions = member_object.Attributes["permissions"].text_value; }
            before_roles = new List<string>();
            sniped_roles = new List<string>();
            removed_roles = new List<string>();
            assigned_roles = new List<DiscordRole>();
            TempRoles = new Dictionary<string, TempRole>();
        }
        public void AssignRoleWithTimer(string BotToken, string temprole_id, int expires_in = 630000)
        {
            if (!roles.Contains(temprole_id)) { AddRole(temprole_id, BotToken); }
            if (!TempRoles.ContainsKey(temprole_id)) 
            {
                TempRole temp_role = new TempRole(temprole_id, expires_in);
                temp_role.AutoReset = false;
                temp_role.Token = BotToken;
                temp_role.Start();
                temp_role.Elapsed += Temp_role_Elapsed;
                temp_role.MemberID = user.id;
                TempRoles.Add(temprole_id, temp_role); 
            }
        }
        private void Temp_role_Elapsed(object sender, ElapsedEventArgs e)
        {
            TempRole CurrentRole = (TempRole)sender;
            RemoveRole(CurrentRole.RoleID, CurrentRole.Token);
            if (TempRoles.ContainsKey(CurrentRole.RoleID)) { TempRoles.Remove(CurrentRole.RoleID); }
        }

        public async Task<bool> KickAsync(string bot_token) { return await Task.Run(() => Kick(bot_token)); }
        public bool Kick(string bot_token)
        {
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + guild_id + "/members/" + user.id, Method.Delete);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            return rsGuildResponse.IsSuccessful;
        }
        public async Task<DiscordBan> BanAsync(string bot_token, int days = -1, string reason = "") { return await Task.Run(() => Ban(bot_token, days, reason)); }
        public DiscordBan Ban(string bot_token, int days = -1, string reason = "")
        {
            DiscordBan ban;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + guild_id + "/bans/" + user.id, Method.Put); 
            if (days > -1) { rrGuildRequest.AddParameter("delete-message-days", days); }
            if (reason != "") { rrGuildRequest.AddParameter("reason", reason); }
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            ban = new DiscordBan(GuildResponseContent);
            return ban;
        }
        public async Task<bool> ChangeNickAsync(string new_nick, string bot_token) { return await Task.Run(() => ChangeNick(new_nick, bot_token)); }
        public bool ChangeNick(string new_nick, string bot_token)
        {
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + guild_id + "/members/" + user.id, Method.Patch); 
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            GuildRequestBody.AddAttribute("nick", new_nick);
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            return rsGuildResponse.IsSuccessful;
        }
        public async Task<bool> SetRolesAsync(List<string> assigned_roles, string bot_token) { return await Task.Run(() => SetRoles(assigned_roles, bot_token)); }
        public bool SetRoles(List<string> assigned_roles, string bot_token)
        {
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + guild_id + "/members/" + user.id, Method.Patch); 
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            List<JsonAttribute> role_ids = new List<JsonAttribute>();
            foreach (string role_id in assigned_roles)
            {
                JsonAttribute current_role = new JsonAttribute(role_id);
                role_ids.Add(current_role);
            }
            GuildRequestBody.AddAttributeList("roles", role_ids);
            if (assigned_roles.Contains(guild_id)) { assigned_roles.Remove(guild_id); }
            roles = assigned_roles;
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            return rsGuildResponse.IsSuccessful;
        }
        public void SetCurrentRoles (List<string> new_roles)
        {
            roles = new_roles;
        }
        public async Task<bool> AddRoleAsync(string role_id, string bot_token) { return await Task.Run(() => AddRole(role_id, bot_token)); }
        public bool AddRole(string role_id, string bot_token)
        {
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + guild_id + "/members/" + user.id + "/roles/" + role_id, Method.Put); 
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            return rsGuildResponse.IsSuccessful;
        }
        public async Task<bool> RemoveRoleAsync(string role_id, string bot_token) { return await Task.Run(() => RemoveRole(role_id, bot_token)); }
        public bool RemoveRole(string role_id, string bot_token)
        {
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + guild_id + "/members/" + user.id + "/roles/" + role_id, Method.Delete); 
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            return rsGuildResponse.IsSuccessful;
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson() 
        {
            MajickRegex.JsonObject fake_user = new MajickRegex.JsonObject();
            List<JsonAttribute> roles_list = new List<JsonAttribute>();
            MajickDiscordWrapper.MajickRegex.JsonObject guild_member_object = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if(user != null)
            {
                if(user.id != null) 
                {
                    fake_user.AddAttribute("id", user.id);
                    guild_member_object.AddObject("user", fake_user); 
                }
            }
            if (nick != null) { guild_member_object.AddAttribute("nick", nick); }
            if (roles.Count > 0)
            {
                foreach (string role_id in roles) { roles_list.Add(new JsonAttribute(role_id)); }
                guild_member_object.AddAttributeList("roles", roles_list);
            }
            if (mute != (true && false)) { guild_member_object.AddAttribute("mute", mute.ToString(), true); }
            if (deaf != (true && false)) { guild_member_object.AddAttribute("deaf", deaf.ToString(), true); }
            return guild_member_object;
        }
    }
    public class DiscordGuild
    {
        public string id { get; set; }
        public string name { get; set; }
        public string icon { get; set; }
        public string icon_hash { get; set; }
        public string splash { get; set; }
        public string discovery_splash { get; set; }
        public bool owner { get; set; }
        public string owner_id { get; set; }
        public int permissions { get; set; }
        public string region { get; set; }
        public string afk_channel_id { get; set; }
        public int afk_channel_timeout { get; set; }
        public bool widget_enabled { get; set; }
        public string widget_channel_id { get; set; }
        public VerificationLevel verification_level { get; set; }
        public MessageNotificationSettings default_message_notifications { get; set; }
        public ExplicitContentFilter explicit_content_filter { get; set; }
        public Dictionary<string, DiscordRole> roles { get; set; }
        public List<DiscordEmoji> emojis { get; set; }
        public List<string> features { get; set; }
        public DiscordMFALevel mfa_level { get; set; }
        public string application_id { get; set; }
        public string system_channel_id { get; set; }
        public int system_channel_flags { get; set; }
        public string rules_channel_id { get; set; }
        public DateTime joined_at { get; set; }
        public bool large { get; set; }
        public bool unavailable { get; set; }
        public int member_count { get; set; }
        public List<DiscordVoiceState> voice_states { get; set; }
        public Dictionary<string, DiscordGuildMember> members { get; set; }
        public Dictionary<string, DiscordChannel> channels { get; set; }
        public Dictionary<string, DiscordChannel> threads { get; set; }
        public Dictionary<string, ApplicationCommand> slash_commands { get; set; }
        public List<DiscordPresenceUpdate> presences { get; set; }
        public int max_presences { get; set; }
        public int max_members { get; set; }
        public string vanity_url_code { get; set; }
        public string description { get; set; }
        public string banner { get; set; }
        public GuildPremiumTier premium_tier { get; set; }
        public int premium_subscription_count { get; set; }
        public string preferred_locale { get; set; }
        public string public_updates_channel_id { get; set; }
        public int max_video_channel_users { get; set; }
        public int approximate_member_count { get; set; }
        public int approximate_presence_count { get; set; }
        public GuildWelcomeScreen welcome_screen { get; set; }
        public NSFWLevel nsfw_level { get; set; }
        public List<DiscordStageInstance> stage_instances { get; set; }
        public Dictionary<string, DiscordBan> bans { get; set; }
        public Dictionary<string, DiscordWebhook> webhooks { get; set; }
        public DiscordGuild()
        {
            roles = new Dictionary<string, DiscordRole>();
            emojis = new List<DiscordEmoji>();
            features = new List<string>();
            voice_states = new List<DiscordVoiceState>();
            members = new Dictionary<string, DiscordGuildMember>();
            channels = new Dictionary<string, DiscordChannel>();
            presences = new List<DiscordPresenceUpdate>();
            bans = new Dictionary<string, DiscordBan>();
            webhooks = new Dictionary<string, DiscordWebhook>();
        }
        public DiscordGuild(MajickDiscordWrapper.MajickRegex.JsonObject guild_object)
        {
            bans = new Dictionary<string, DiscordBan>();
            webhooks = new Dictionary<string, DiscordWebhook>();
            if (guild_object.Attributes.ContainsKey("id")) { id = guild_object.Attributes["id"].text_value; }
            if (guild_object.Attributes.ContainsKey("name")) { name = guild_object.Attributes["name"].text_value; }
            if (guild_object.Attributes.ContainsKey("icon")) { icon = guild_object.Attributes["icon"].text_value; }
            if (guild_object.Attributes.ContainsKey("icon_hash")) { icon_hash = guild_object.Attributes["icon_hash"].text_value; }
            if (guild_object.Attributes.ContainsKey("discovery_splash")) { discovery_splash = guild_object.Attributes["discovery_splash"].text_value; }
            if (guild_object.Attributes.ContainsKey("owner"))
            {
                bool is_owner;
                if (bool.TryParse(guild_object.Attributes["owner"].text_value, out is_owner)) { owner = is_owner; }
            }
            if (guild_object.Attributes.ContainsKey("owner_id")) { owner_id = guild_object.Attributes["owner_id"].text_value; }
            if (guild_object.Attributes.ContainsKey("permissions"))
            {
                int temp_perms;
                if (int.TryParse(guild_object.Attributes["permissions"].text_value, out temp_perms)) { permissions = temp_perms; }
            }
            if (guild_object.Attributes.ContainsKey("region")) { region = guild_object.Attributes["region"].text_value; }
            if (guild_object.Attributes.ContainsKey("afk_channel_id")) { afk_channel_id = guild_object.Attributes["afk_channel_id"].text_value; }
            if (guild_object.Attributes.ContainsKey("afk_channel_timeout"))
            {
                int afk_timeout = 0;
                if (int.TryParse(guild_object.Attributes["afk_timeout"].text_value, out afk_timeout)) { afk_channel_timeout = afk_timeout; }
            }
            if (guild_object.Attributes.ContainsKey("widget_enabled"))
            {
                bool is_widget_enabled;
                if (bool.TryParse(guild_object.Attributes["widget_enabled"].text_value, out is_widget_enabled)) { widget_enabled = is_widget_enabled; }
            }
            if (guild_object.Attributes.ContainsKey("widget_channel_id")) { widget_channel_id = guild_object.Attributes["widget_channel_id"].text_value; }
            if (guild_object.Attributes.ContainsKey("verification_level"))
            {
                VerificationLevel temp_verification;
                if (Enum.TryParse(guild_object.Attributes["verification_level"].text_value, out temp_verification)) { verification_level = temp_verification; }
            }
            if (guild_object.Attributes.ContainsKey("default_message_notifications"))
            {
                MessageNotificationSettings temp_notifications;
                if (Enum.TryParse(guild_object.Attributes["default_message_notifications"].text_value, out temp_notifications)) { default_message_notifications = temp_notifications; }
            }
            if (guild_object.Attributes.ContainsKey("explicit_content_filter"))
            {
                ExplicitContentFilter explicit_filter;
                if (Enum.TryParse(guild_object.Attributes["explicit_content_filter"].text_value, out explicit_filter)) { explicit_content_filter = explicit_filter; }
            }
            roles = new Dictionary<string, DiscordRole>();
            if (guild_object.ObjectLists.ContainsKey("roles"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_role in guild_object.ObjectLists["roles"])
                {
                    DiscordRole role_added = new DiscordRole(current_role);
                    roles.Add(role_added.id, role_added);
                }
            }
            emojis = new List<DiscordEmoji>();
            if (guild_object.ObjectLists.ContainsKey("emojis"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_emoji in guild_object.ObjectLists["emojis"])
                {
                    emojis.Add(new DiscordEmoji(current_emoji));
                }
            }
            features = new List<string>();
            if (guild_object.AttributeLists.ContainsKey("features"))
            {
                foreach (JsonAttribute feature in guild_object.AttributeLists["features"]) { features.Add(feature.text_value); }
            }
            if (guild_object.Attributes.ContainsKey("mfa_level"))
            {
                DiscordMFALevel temp_mfa;
                if (Enum.TryParse(guild_object.Attributes["mfa_level"].text_value, out temp_mfa)) { mfa_level = temp_mfa; }
            }
            if (guild_object.Attributes.ContainsKey("application_id")) { application_id = guild_object.Attributes["application_id"].text_value; }
            if (guild_object.Attributes.ContainsKey("system_channel_id")) { system_channel_id = guild_object.Attributes["system_channel_id"].text_value; }
            if (guild_object.Attributes.ContainsKey("system_channel_flags"))
            {
                int temp_system_channel_flags;
                if (int.TryParse(guild_object.Attributes["system_channel_flags"].text_value, out temp_system_channel_flags)) { system_channel_flags = temp_system_channel_flags; }
            }
            if (guild_object.Attributes.ContainsKey("rules_channel_id")) { rules_channel_id = guild_object.Attributes["rules_channel_id"].text_value; }
            if (guild_object.Attributes.ContainsKey("joined_at"))
            {
                DateTime when_joined;
                if (DateTime.TryParse(guild_object.Attributes["joined_at"].text_value, out when_joined)) { joined_at = when_joined; }
            }
            if (guild_object.Attributes.ContainsKey("large"))
            {
                bool is_large;
                if (bool.TryParse(guild_object.Attributes["large"].text_value, out is_large)) { large = is_large; }
            }
            if (guild_object.Attributes.ContainsKey("unavailable"))
            {
                bool is_unavailable;
                if (bool.TryParse(guild_object.Attributes["unavailable"].text_value, out is_unavailable)) { unavailable = is_unavailable; }
            }
            if (guild_object.Attributes.ContainsKey("member_count"))
            {
                int temp_member_count;
                if (int.TryParse(guild_object.Attributes["member_count"].text_value, out temp_member_count)) { member_count = temp_member_count; }
            }
            voice_states = new List<DiscordVoiceState>();
            if (guild_object.ObjectLists.ContainsKey("voice_states"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_state in guild_object.ObjectLists["voice_states"])
                {
                    voice_states.Add(new DiscordVoiceState(current_state));
                }
            }
            members = new Dictionary<string, DiscordGuildMember>();
            if (guild_object.ObjectLists.ContainsKey("members"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_member in guild_object.ObjectLists["members"])
                {
                    DiscordGuildMember new_member = new DiscordGuildMember(current_member, id);
                    if (new_member.user != null) { if (!members.ContainsKey(new_member.user.id)) { members.Add(new_member.user.id, new_member); } }
                }
            }
            channels = new Dictionary<string, DiscordChannel>();
            if (guild_object.ObjectLists.ContainsKey("channels"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_chanel in guild_object.ObjectLists["channels"])
                {
                    DiscordChannel channel_added = new DiscordChannel(current_chanel);
                    channels.Add(channel_added.id, channel_added);
                }
            }
            threads = new Dictionary<string, DiscordChannel>();
            if (guild_object.ObjectLists.ContainsKey("threads"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_chanel in guild_object.ObjectLists["threads"])
                {
                    DiscordChannel channel_added = new DiscordChannel(current_chanel);
                    threads.Add(channel_added.id, channel_added);
                }
            }
            presences = new List<DiscordPresenceUpdate>();
            if (guild_object.ObjectLists.ContainsKey("presences"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_presence in guild_object.ObjectLists["presences"])
                {
                    presences.Add(new DiscordPresenceUpdate(current_presence));
                }
            }
            if (guild_object.Attributes.ContainsKey("max_presences"))
            {
                int temp_max_presences;
                if (int.TryParse(guild_object.Attributes["max_presences"].text_value, out temp_max_presences)) { max_presences = temp_max_presences; }
            }
            if (guild_object.Attributes.ContainsKey("max_members"))
            {
                int temp_max_members;
                if (int.TryParse(guild_object.Attributes["max_members"].text_value, out temp_max_members)) { max_members = temp_max_members; }
            }
            if (guild_object.Attributes.ContainsKey("vanity_url_code")) { vanity_url_code = guild_object.Attributes["vanity_url_code"].text_value; }
            if (guild_object.Attributes.ContainsKey("description")) { description = guild_object.Attributes["description"].text_value; }
            if (guild_object.Attributes.ContainsKey("banner")) { banner = guild_object.Attributes["banner"].text_value; }
            if (guild_object.Attributes.ContainsKey("premium_tier"))
            {
                GuildPremiumTier temp_premium_tier;
                if (Enum.TryParse(guild_object.Attributes["premium_tier"].text_value, out temp_premium_tier)) { premium_tier = temp_premium_tier; }
            }
            if (guild_object.Attributes.ContainsKey("premium_subscription_count"))
            {
                int temp_premium_subscription_count;
                if (int.TryParse(guild_object.Attributes["premium_subscription_count"].text_value, out temp_premium_subscription_count)) { premium_subscription_count = temp_premium_subscription_count; }
            }
            if (guild_object.Attributes.ContainsKey("preferred_locale")) { preferred_locale = guild_object.Attributes["preferred_locale"].text_value; }
            if (guild_object.Attributes.ContainsKey("public_updates_channel_id")) { public_updates_channel_id = guild_object.Attributes["public_updates_channel_id"].text_value; }
            if (guild_object.Attributes.ContainsKey("max_video_channel_users"))
            {
                int temp_max_video_channel_users;
                if (int.TryParse(guild_object.Attributes["max_video_channel_users"].text_value, out temp_max_video_channel_users)) { max_video_channel_users = temp_max_video_channel_users; }
            }
            if (guild_object.Attributes.ContainsKey("approximate_member_count"))
            {
                int temp_approximate_member_count;
                if (int.TryParse(guild_object.Attributes["approximate_member_count"].text_value, out temp_approximate_member_count)) { approximate_member_count = temp_approximate_member_count; }
            }
            if (guild_object.Attributes.ContainsKey("approximate_presence_count"))
            {
                int temp_approximate_presence_count;
                if (int.TryParse(guild_object.Attributes["approximate_presence_count"].text_value, out temp_approximate_presence_count)) { approximate_presence_count = temp_approximate_presence_count; }
            }
            if (guild_object.Objects.ContainsKey("welcome_screen")) { welcome_screen = new GuildWelcomeScreen(guild_object.Objects["welcome_screen"]); }
            if (guild_object.Attributes.ContainsKey("nsfw_level"))
            {
                NSFWLevel temp_nsfw_level;
                if (Enum.TryParse(guild_object.Attributes["nsfw_level"].text_value, out temp_nsfw_level)) { nsfw_level = temp_nsfw_level; }
            }
            stage_instances = new List<DiscordStageInstance>();
            if (guild_object.ObjectLists.ContainsKey("stage_instances"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_stage_instance in guild_object.ObjectLists["stage_instances"])
                {
                    stage_instances.Add(new DiscordStageInstance(current_stage_instance));
                }
            }
        }
        public List<SystemChannelFlags> GetNamedSystemChannelFlags(int perm_value)
        {
            List<SystemChannelFlags> my_flags = new List<SystemChannelFlags>();
            if (perm_value >= (int)SystemChannelFlags.SUPPRESS_GUILD_REMINDER_NOTIFICATIONS)
            {
                my_flags.Add(SystemChannelFlags.SUPPRESS_GUILD_REMINDER_NOTIFICATIONS);
                perm_value -= (int)SystemChannelFlags.SUPPRESS_GUILD_REMINDER_NOTIFICATIONS;
            }
            if (perm_value >= (int)SystemChannelFlags.SUPPRESS_PREMIUM_SUBSCRIPTIONS)
            {
                my_flags.Add(SystemChannelFlags.SUPPRESS_PREMIUM_SUBSCRIPTIONS);
                perm_value -= (int)SystemChannelFlags.SUPPRESS_PREMIUM_SUBSCRIPTIONS;
            }
            if (perm_value >= (int)SystemChannelFlags.SUPPRESS_JOIN_NOTIFICATIONS)
            {
                my_flags.Add(SystemChannelFlags.SUPPRESS_JOIN_NOTIFICATIONS);
                perm_value -= (int)SystemChannelFlags.SUPPRESS_JOIN_NOTIFICATIONS;
            }
            return my_flags;
        }
        public async Task SetNameAsync(string guild_name, string bot_token) { await Task.Run(() => SetName(guild_name, bot_token)); }
        public DiscordGuild SetName(string guild_name, string bot_token)
        {
            DiscordGuild guild;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + id, Method.Patch);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            GuildRequestBody.AddAttribute("name", guild_name);
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            guild = new DiscordGuild(new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content));
            name = guild.name;
            return guild;
        }
        public async Task<DiscordGuildMember> JoinMemberAsync(string bot_token, string user_id, string oauth_token, string nick = "") { return await Task.Run(() => JoinMember(bot_token, user_id, oauth_token, nick)); }
        public DiscordGuildMember JoinMember(string bot_token, string user_id, string oauth_token, string nick = "")
        {
            DiscordGuildMember guild_member;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + id + "/members/" + user_id, Method.Put);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            GuildRequestBody.AddAttribute("access_token", oauth_token);
            if (nick != "") { GuildRequestBody.AddAttribute("nick", nick); }
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            guild_member = new DiscordGuildMember(GuildResponseContent);
            return guild_member;
        }
        public async Task<Dictionary<string, DiscordInvite>> GetInvitesAsync(string bot_token) { return await Task.Run(() => GetInvites(bot_token)); }
        public Dictionary<string, DiscordInvite> GetInvites(string bot_token)
        {
            DiscordInvite invite;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + id + "/invites", Method.Get);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_message in GuildResponseContent.ObjectLists["objects"])
            {
                invite = new DiscordInvite(current_message);
                invites.Add(invite.code, invite);
            }
            return invites;
        }
        public async Task<DiscordInvite> DeleteInviteByCodeAsync(string invite_code, string bot_token) { return await Task.Run(() => DeleteInviteByCode(invite_code, bot_token)); }
        public DiscordInvite DeleteInviteByCode(string invite_code, string bot_token)
        {
            DiscordInvite invite;
            RestClient rcInviteClient;
            RestRequest rrInviteRequest;
            RestResponse rsInviteResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject MessageRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject MessageResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcInviteClient = new RestClient("https://discord.com/api");
            rrInviteRequest = new RestRequest("/invites/" + invite_code, Method.Delete);
            rrInviteRequest.RequestFormat = DataFormat.Json;
            rrInviteRequest.AddHeader("Content-Type", "application/json");
            rrInviteRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrInviteRequest.AddJsonBody(MessageRequestBody.ToRawText(false));
            rsInviteResponse = rcInviteClient.Execute(rrInviteRequest);
            invite = new DiscordInvite(new MajickDiscordWrapper.MajickRegex.JsonObject(rsInviteResponse.Content));
            return invite;
        }
        public void GetRoles(string bot_token)
        {
            DiscordRole role;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + id + "/roles", Method.Get);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            string response_object = "{\"roles\":" + rsGuildResponse.Content + "}";
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(response_object);
            foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_message in GuildResponseContent.ObjectLists["roles"])
            {
                role = new DiscordRole(current_message, this);
                if (!roles.ContainsKey(role.id)) { roles.Add(role.id, role); }
            }
        }
        public async Task<Dictionary<string, DiscordChannel>> GetChannelsAsync(string bot_token) { return await Task.Run(() => GetChannels(bot_token)); }
        public Dictionary<string, DiscordChannel> GetChannels(string bot_token)
        {
            DiscordChannel channel;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordChannel> channels = new Dictionary<string, DiscordChannel>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + id + "/channels", Method.Get);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_message in GuildResponseContent.ObjectLists["objects"])
            {
                channel = new DiscordChannel(current_message);
                if (!channels.ContainsKey(channel.id)) { channels.Add(channel.id, channel); }
            }
            return channels;
        }
        public async Task<DiscordChannel> CreateChannelAsync(string bot_token, string channel_name, ChannelType type = ChannelType.GUILD_TEXT) { return await Task.Run(() => CreateChannel(bot_token, channel_name, type)); }
        public DiscordChannel CreateChannel(string bot_token, string channel_name, ChannelType type = ChannelType.GUILD_TEXT)
        {
            DiscordChannel channel;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordChannel> channels = new Dictionary<string, DiscordChannel>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + id + "/channels", Method.Post);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            GuildRequestBody.AddAttribute("name", channel_name);
            GuildRequestBody.AddAttribute("type", ((int)type).ToString());
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            channel = new DiscordChannel(GuildResponseContent);
            return channel;
        }
        public async Task<DiscordChannel> CreateChannelAsync(ChannelUpdateObject new_channel, string bot_token) { return await Task.Run(() => CreateChannel(new_channel, bot_token)); }
        public DiscordChannel CreateChannel(ChannelUpdateObject new_channel, string bot_token)
        {
            DiscordChannel channel;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordChannel> channels = new Dictionary<string, DiscordChannel>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + id + "/channels", Method.Post);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            GuildRequestBody = new_channel.ToJson();
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            channel = new DiscordChannel(GuildResponseContent);
            return channel;
        }
        public async Task<DiscordChannel> DeleteChannelAsync(string channel_id, string bot_token) { return await Task.Run(() => DeleteChannel(channel_id, bot_token)); }
        public DiscordChannel DeleteChannel(string channel_id, string bot_token)
        {
            DiscordChannel channel;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordChannel> channels = new Dictionary<string, DiscordChannel>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/channels/" + channel_id, Method.Delete);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            channel = new DiscordChannel(GuildResponseContent);
            return channel;
        }
        public async Task<bool> MoveChannelAsync(string channel_id, int position, string bot_token) { return await Task.Run(() => MoveChannel(channel_id, position, bot_token)); }
        public bool MoveChannel(string channel_id, int position, string bot_token)
        {
            string channel_list_text;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            List<MajickDiscordWrapper.MajickRegex.JsonObject> obj_channels = new List<MajickDiscordWrapper.MajickRegex.JsonObject>();
            MajickDiscordWrapper.MajickRegex.JsonObject obj_channel = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordChannel> channels = new Dictionary<string, DiscordChannel>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + id + "/channels", Method.Patch);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            obj_channel.AddAttribute("id", channel_id);
            obj_channel.AddAttribute("position", position.ToString(), true);
            obj_channels.Add(obj_channel);
            GuildRequestBody.AddObjectList("channels", obj_channels);
            channel_list_text = GuildRequestBody.ToRawText(false);
            channel_list_text = channel_list_text.Substring(1, channel_list_text.Length - 2);
            channel_list_text = channel_list_text.Substring(channel_list_text.IndexOf(":") + 1);
            rrGuildRequest.AddJsonBody(channel_list_text);
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            return rsGuildResponse.IsSuccessful;
        }
        public async Task<bool> MoveChannelsAsync(Dictionary<string, int> channel_positions, string bot_token) { return await Task.Run(() => MoveChannels(channel_positions, bot_token)); }
        public bool MoveChannels(Dictionary<string, int> channel_positions, string bot_token)
        {
            string channel_list_text;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            List<MajickDiscordWrapper.MajickRegex.JsonObject> obj_channels = new List<MajickDiscordWrapper.MajickRegex.JsonObject>();
            MajickDiscordWrapper.MajickRegex.JsonObject obj_channel = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordChannel> channels = new Dictionary<string, DiscordChannel>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + id + "/channels", Method.Patch);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            foreach(string channel_id in channel_positions.Keys)
            {
                obj_channel.AddAttribute("id", channel_id);
                obj_channel.AddAttribute("position", channel_positions[channel_id].ToString(), true);
                obj_channels.Add(obj_channel);
            }
            GuildRequestBody.AddObjectList("channels", obj_channels);
            channel_list_text = GuildRequestBody.ToRawText(false);
            channel_list_text = channel_list_text.Substring(1, channel_list_text.Length - 2);
            channel_list_text = channel_list_text.Substring(channel_list_text.IndexOf(":") + 1);
            rrGuildRequest.AddJsonBody(channel_list_text);
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            return rsGuildResponse.IsSuccessful;
        }
        public async Task<Dictionary<string, DiscordGuildMember>> GetMembersAsync(string bot_token, int limit = 0, string after_id = "") { return await Task.Run(() => GetMembers(bot_token, limit, after_id)); }
        public Dictionary<string, DiscordGuildMember> GetMembers(string bot_token, int limit = 0, string after_id = "")
        {
            DiscordGuildMember member;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordGuildMember> members = new Dictionary<string, DiscordGuildMember>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + id + "/members", Method.Get);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            if(limit != 0) { rrGuildRequest.AddParameter("limit", limit); }
            if(after_id != "") { rrGuildRequest.AddParameter("after", after_id); }
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            if (GuildResponseContent.ObjectLists.ContainsKey("objects"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_message in GuildResponseContent.ObjectLists["objects"])
                {
                    member = new DiscordGuildMember(current_message, id);
                    members.Add(member.user.id, member);
                }
            }
            return members;
        }
        public async Task<DiscordGuildMember> GetMemberAsync(string member_id, string bot_token) { return await Task.Run(() => GetMember(member_id, bot_token)); }
        public DiscordGuildMember GetMember(string member_id, string bot_token)
        {
            DiscordGuildMember member;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordGuildMember> Roles = new Dictionary<string, DiscordGuildMember>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + id + "/members/" + member_id, Method.Get);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            member = new DiscordGuildMember(GuildResponseContent, id);
            return member;
        }
        public async Task<bool> KickMemberAsync(string user_id, string bot_token) { return await Task.Run(() => KickMember(user_id, bot_token)); }
        public bool KickMember(string user_id, string bot_token)
        {
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + id + "/members/" + user_id, Method.Delete);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            return rsGuildResponse.IsSuccessful;
        }
        public async Task<DiscordBan> GetBanAsync(string user_id, string bot_token) { return await Task.Run(() => GetBan(user_id, bot_token)); }
        public DiscordBan GetBan(string user_id, string bot_token)
        {
            DiscordBan ban;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + id + "/bans/" + user_id, Method.Get);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            ban = new DiscordBan(GuildResponseContent);
            return ban;
        }
        public async Task<Dictionary<string, DiscordBan>> GetBansAsync(string bot_token) { return await Task.Run(() => GetBans(bot_token)); }
        public Dictionary<string, DiscordBan> GetBans(string bot_token)
        {
            DiscordBan ban;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordBan> bans = new Dictionary<string, DiscordBan>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + id + "/roles", Method.Get);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_ban in GuildResponseContent.ObjectLists["objects"])
            {
                ban = new DiscordBan(current_ban);
                bans.Add(ban.user.id, ban);
            }
            return bans;
        }
        public async Task<DiscordBan> BanUserByIDAsync(string bot_token, string user_id, int days = -1, string reason = "") { return await Task.Run(() => BanUserByID(bot_token, user_id, days, reason)); }
        public DiscordBan BanUserByID(string bot_token, string user_id, int days = -1, string reason = "")
        {
            DiscordBan ban;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + id + "/bans/" + user_id, Method.Put);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            if (days > -1) { rrGuildRequest.AddQueryParameter("delete-message-days", days.ToString()); }
            if (reason != "") { rrGuildRequest.AddQueryParameter("reason", reason); }
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            ban = new DiscordBan(GuildResponseContent);
            return ban;
        }
        public async Task<bool> RemoveBanAsync(string user_id, string bot_token) { return RemoveBan(user_id, bot_token); }
        public bool RemoveBan(string user_id, string bot_token)
        {
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + id + "/bans/" + user_id, Method.Delete);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            return rsGuildResponse.IsSuccessful;
        }
        public async Task<DiscordRole> CreateRoleAsync(string role_name, string bot_token) { return await Task.Run(() => CreateRole(role_name, bot_token)); }
        public DiscordRole CreateRole(string Role_name, string bot_token)
        {
            DiscordRole Role;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordRole> Roles = new Dictionary<string, DiscordRole>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + id + "/roles", Method.Post);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            GuildRequestBody.AddAttribute("name", Role_name);
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            Role = new DiscordRole(GuildResponseContent);
            return Role;
        }
        public async Task<DiscordRole> CreateRoleAsync(RoleUpdateObject new_role, string bot_token) { return await Task.Run(() => CreateRole(new_role, bot_token)); }
        public DiscordRole CreateRole(RoleUpdateObject new_role, string bot_token)
        {
            DiscordRole Role;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordRole> Roles = new Dictionary<string, DiscordRole>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + id + "/roles", Method.Post);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            GuildRequestBody = new_role.ToJson();
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            Role = new DiscordRole(GuildResponseContent);
            return Role;
        }
        public async Task<bool> MoveRoleAsync(string role_id, int position, string bot_token) { return await Task.Run(() => MoveRole(role_id, position, bot_token)); }
        public bool MoveRole(string role_id, int position, string bot_token)
        {
            string role_list_text;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            List<MajickDiscordWrapper.MajickRegex.JsonObject> obj_Roles = new List<MajickDiscordWrapper.MajickRegex.JsonObject>();
            MajickDiscordWrapper.MajickRegex.JsonObject obj_Role = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordRole> Roles = new Dictionary<string, DiscordRole>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + id + "/roles", Method.Patch);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            obj_Role.AddAttribute("id", role_id);
            obj_Role.AddAttribute("position", position.ToString(), true);
            obj_Roles.Add(obj_Role);
            GuildRequestBody.AddObjectList("roles", obj_Roles);
            role_list_text = GuildRequestBody.ToRawText(false);
            role_list_text = role_list_text.Substring(1, role_list_text.Length - 2);
            role_list_text = role_list_text.Substring(role_list_text.IndexOf(":") + 1);
            rrGuildRequest.AddJsonBody(role_list_text);
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            return rsGuildResponse.IsSuccessful;
        }
        public async Task<bool> MoveRolesAsync(Dictionary<string, int> role_positions, string bot_token) { return await Task.Run(() => MoveRoles(role_positions, bot_token)); }
        public bool MoveRoles(Dictionary<string, int> role_positions, string bot_token)
        {
            string role_list_text;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            List<MajickDiscordWrapper.MajickRegex.JsonObject> obj_roles = new List<MajickDiscordWrapper.MajickRegex.JsonObject>();
            MajickDiscordWrapper.MajickRegex.JsonObject obj_role = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordRole> Roles = new Dictionary<string, DiscordRole>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + id + "/roles", Method.Patch);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            foreach (string role_id in role_positions.Keys)
            {
                obj_role.AddAttribute("id", role_id);
                obj_role.AddAttribute("position", role_positions[role_id].ToString(), true);
                obj_roles.Add(obj_role);
            }
            GuildRequestBody.AddObjectList("roles", obj_roles);
            role_list_text = GuildRequestBody.ToRawText(false);
            role_list_text = role_list_text.Substring(1, role_list_text.Length - 2);
            role_list_text = role_list_text.Substring(role_list_text.IndexOf(":") + 1);
            rrGuildRequest.AddJsonBody(role_list_text);
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            return rsGuildResponse.IsSuccessful;
        }
        public async Task<bool> DeleteRoleAsync(string role_id, string bot_token) { return await Task.Run(() => DeleteRole(role_id, bot_token)); }
        public bool DeleteRole(string role_id, string bot_token)
        {
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + id + "/roles/" + role_id, Method.Delete);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            return rsGuildResponse.IsSuccessful;
        }
        public async Task GetWebhooksAsync(string bot_token) { await Task.Run(() => GetWebhooks(bot_token)); }
        public void GetWebhooks(string bot_token)
        {
            DiscordWebhook webhook;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordRole> Roles = new Dictionary<string, DiscordRole>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + id + "/webhooks", Method.Get);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            foreach (MajickDiscordWrapper.MajickRegex.JsonObject webhook_obj in GuildResponseContent.ObjectLists["data"])
            {
                webhook = new DiscordWebhook(webhook_obj);
                if (!webhooks.ContainsKey(webhook.id)) { webhooks.Add(webhook.id, webhook); }
            }
        }
        public async Task<DiscordWebhook> GetWebhookByIDAsync(string webhook_id, string bot_token) { return await Task.Run(() => GetWebhookByID(webhook_id, bot_token)); }
        public DiscordWebhook GetWebhookByID(string webhook_id, string bot_token)
        {
            DiscordWebhook webhook;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordRole> Roles = new Dictionary<string, DiscordRole>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/webhooks/" + webhook_id, Method.Get);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            webhook = new DiscordWebhook(GuildResponseContent);
            if (!webhooks.ContainsKey(webhook.id)) { webhooks.Add(webhook.id, webhook); }
            return webhook;
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject GuildObject = new MajickDiscordWrapper.MajickRegex.JsonObject();
            //fill in all the properties here
            if (id != null) { GuildObject.AddAttribute("id", id); }
            if (name != null) { GuildObject.AddAttribute("name", name); }
            if (icon != null) { GuildObject.AddAttribute("icon", icon); }
            if (splash != null) { GuildObject.AddAttribute("splash", splash); }
            if (owner_id != null) { GuildObject.AddAttribute("owner_id", owner_id); }
            if (region != null) { GuildObject.AddAttribute("region", region); }
            if (afk_channel_id != null) { GuildObject.AddAttribute("afk_channel_id", afk_channel_id); }
            if (application_id != null) { GuildObject.AddAttribute("application_id", application_id); }
            if (widget_channel_id != null) { GuildObject.AddAttribute("widget_channel_id", widget_channel_id); }
            if (system_channel_id != null) { GuildObject.AddAttribute("system_channel_id", system_channel_id); }
            if (widget_enabled != (true && false)) { GuildObject.AddAttribute("widget_enabled", widget_enabled.ToString(), true); }
            GuildObject.AddAttribute("verification_level", verification_level.ToString()); ;
            GuildObject.AddAttribute("explicit_content_filter", explicit_content_filter.ToString());
            GuildObject.AddAttribute("default_message_notifications", default_message_notifications.ToString());
            GuildObject.AddAttribute("mfa_level", mfa_level.ToString());
            if (afk_channel_timeout > -1) { GuildObject.AddAttribute("afk_channel_timeout", afk_channel_timeout.ToString(), true); }
            if (permissions > -1) { GuildObject.AddAttribute("permissions", permissions.ToString(), true); }
            if(channels.Count > 0)
            {
                List<MajickRegex.JsonObject> channel_list = new List<MajickRegex.JsonObject>();
                foreach (DiscordChannel channel in channels.Values)
                {
                    channel_list.Add(channel.ToJson());
                }
                GuildObject.AddObjectList("channels", channel_list);
            }
            if(roles.Count > 0)
            {
                List<MajickRegex.JsonObject> role_list = new List<MajickRegex.JsonObject>();
                foreach (DiscordRole role in roles.Values)
                {
                    role_list.Add(role.ToJson());
                }
                GuildObject.AddObjectList("roles", role_list);
            }
            if (members.Count > 0)
            {
                List<MajickRegex.JsonObject> member_list = new List<MajickRegex.JsonObject>();
                foreach (DiscordGuildMember member in members.Values)
                {
                    member_list.Add(member.ToJson());
                }
                GuildObject.AddObjectList("members", member_list);
            }
            if (bans.Count > 0)
            {
                List<MajickRegex.JsonObject> ban_list = new List<MajickRegex.JsonObject>();
                foreach (DiscordBan ban in bans.Values)
                {
                    ban_list.Add(ban.ToJson());
                }
                GuildObject.AddObjectList("bans", ban_list);
            }
            return GuildObject;
        }
    }
    public class GuildWidget
    {
        bool enabled { get; set; }
        string channel_id { get; set; }
        public GuildWidget() { }
        public GuildWidget(MajickRegex.JsonObject new_widget)
        {
            if (new_widget.Attributes.ContainsKey("enabled"))
            {
                bool is_enabled;
                if (bool.TryParse(new_widget.Attributes["enabled"].text_value, out is_enabled)) { enabled = is_enabled; }
            }
            if (new_widget.Attributes.ContainsKey("channel_id")) { channel_id = new_widget.Attributes["channel_id"].text_value; }
        }
    }
    public class GuildWelcomeScreen
    {
        public string description { get; set; }
        public Dictionary<string, WelcomeScreenChannel> welcome_channels { get; set; }
        public GuildWelcomeScreen() { }
        public GuildWelcomeScreen(MajickRegex.JsonObject new_welcome) 
        {
            if (new_welcome.Attributes.ContainsKey("channel_description")) { description = new_welcome.Attributes["description"].text_value; }
            welcome_channels = new Dictionary<string, WelcomeScreenChannel>();
            if (new_welcome.ObjectLists.ContainsKey("roles"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_welcome in new_welcome.ObjectLists["roles"])
                {
                    WelcomeScreenChannel added_welcome = new WelcomeScreenChannel(current_welcome);
                    welcome_channels.Add(added_welcome.channel_id, added_welcome);
                }
            }
        }
    }
    public class WelcomeScreenChannel
    {
        public string channel_id { get; set; }
        public string description { get; set; }
        public string emoji_id { get; set; }
        public string emoji_name { get; set; }
        public WelcomeScreenChannel() { }
        public WelcomeScreenChannel(MajickRegex.JsonObject new_welcome_channel) 
        {
            if (new_welcome_channel.Attributes.ContainsKey("channel_id")) { channel_id = new_welcome_channel.Attributes["channel_id"].text_value; }
            if (new_welcome_channel.Attributes.ContainsKey("description")) { description = new_welcome_channel.Attributes["description"].text_value; }
            if (new_welcome_channel.Attributes.ContainsKey("emoji_id")) { emoji_id = new_welcome_channel.Attributes["emoji_id"].text_value; }
            if (new_welcome_channel.Attributes.ContainsKey("emoji_name")) { emoji_name = new_welcome_channel.Attributes["emoji_name"].text_value; }
        }
    }
    public class DiscordStageInstance
    {
        public string id { get; set; }
        public string guild_id { get; set; }
        public string channel_id { get; set; }
        public string topic { get; set; }
        public StagePrivacyLevel privacy_level { get; set; }
        public bool discoverable_disabled { get; set; }
        public DiscordStageInstance() { }
        public DiscordStageInstance(MajickRegex.JsonObject new_stage_instance) 
        {
            if (new_stage_instance.Attributes.ContainsKey("id")) { id = new_stage_instance.Attributes["id"].text_value; }
            if (new_stage_instance.Attributes.ContainsKey("guild_id")) { guild_id = new_stage_instance.Attributes["guild_id"].text_value; }
            if (new_stage_instance.Attributes.ContainsKey("channel_id")) { channel_id = new_stage_instance.Attributes["channel_id"].text_value; }
            if (new_stage_instance.Attributes.ContainsKey("topic")) { topic = new_stage_instance.Attributes["topic"].text_value; }
            if (new_stage_instance.Attributes.ContainsKey("enabled"))
            {
                StagePrivacyLevel temp_privacy_level;
                if (Enum.TryParse(new_stage_instance.Attributes["privacy_level"].text_value, out temp_privacy_level)) { privacy_level = temp_privacy_level; }
            }
            if (new_stage_instance.Attributes.ContainsKey("enabled"))
            {
                bool is_discoverable_disabled;
                if (bool.TryParse(new_stage_instance.Attributes["discoverable_disabled"].text_value, out is_discoverable_disabled)) { discoverable_disabled = is_discoverable_disabled; }
            }
        }
    }
    public static class DiscordGuildExtentions
    {
        public static async Task<DiscordRole> CreateAsync(this Dictionary<string, DiscordRole> current_roles, string role_name, string guild_id, string bot_token) { return await Task.Run(() => Create(current_roles, role_name, guild_id, bot_token)); }
        public static DiscordRole Create(this Dictionary<string, DiscordRole> current_roles, string role_name, string guild_id, string bot_token)
        {
            DiscordRole role;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordRole> Roles = new Dictionary<string, DiscordRole>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + guild_id + "/roles", Method.Post);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            GuildRequestBody.AddAttribute("name", role_name);
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            role = new DiscordRole(GuildResponseContent);
            current_roles.Add(role.id, role);
            return role;
        }
        public static async Task<DiscordRole> CreateAsync(this Dictionary<string, DiscordRole> current_roles, RoleUpdateObject new_role, string guild_id, string bot_token) { return await Task.Run(() => Create(current_roles, new_role, guild_id, bot_token)); }
        public static DiscordRole Create(this Dictionary<string, DiscordRole> current_roles, RoleUpdateObject new_role, string guild_id, string bot_token)
        {
            DiscordRole role;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordRole> Roles = new Dictionary<string, DiscordRole>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + guild_id + "/roles", Method.Post);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            GuildRequestBody = new_role.ToJson();
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            role = new DiscordRole(GuildResponseContent);
            current_roles.Add(role.id, role);
            return role;
        }
        public static async Task<DiscordChannel> CreateAsync(this Dictionary<string, DiscordChannel> current_channels, string channel_name, string guild_id, string bot_token) { return await Task.Run(() => Create(current_channels, channel_name, guild_id, bot_token)); }
        public static DiscordChannel Create(this Dictionary<string, DiscordChannel> current_channels, string channel_name, string guild_id, string bot_token)
        {
            DiscordChannel channel;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordChannel> channels = new Dictionary<string, DiscordChannel>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + guild_id + "/channels", Method.Post);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            GuildRequestBody.AddAttribute("name", channel_name);
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            channel = new DiscordChannel(GuildResponseContent);
            current_channels.Add(channel.id, channel);
            return channel;
        }
        public static async Task<DiscordChannel> CreateAsync(this Dictionary<string, DiscordChannel> current_channels, ChannelUpdateObject new_channel, string guild_id, string bot_token) { return await Task.Run(() => Create(current_channels, new_channel, guild_id, bot_token)); }
        public static DiscordChannel Create(this Dictionary<string, DiscordChannel> current_channels, ChannelUpdateObject new_channel, string guild_id, string bot_token)
        {
            DiscordChannel channel;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordChannel> channels = new Dictionary<string, DiscordChannel>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + guild_id + "/channels", Method.Post);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            GuildRequestBody = new_channel.ToJson();
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            channel = new DiscordChannel(GuildResponseContent);
            current_channels.Add(channel.id, channel);
            return channel;
        }
        public static async Task<DiscordWebhook> CreateAsync(this Dictionary<string, DiscordWebhook> current_webhooks, string channel_id, string bot_token) { return await Task.Run(() => Create(current_webhooks, channel_id, bot_token)); }
        public static DiscordWebhook Create(this Dictionary<string, DiscordWebhook> current_webhooks, string channel_id, string bot_token)
        {
            DiscordWebhook webhook;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordRole> Roles = new Dictionary<string, DiscordRole>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/channels/" + channel_id + "/webhooks", Method.Post);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            webhook = new DiscordWebhook(GuildResponseContent);
            current_webhooks.Add(webhook.id, webhook);
            return webhook;
        }
    }
    public class DiscordIntegration
    {
        public string id { get; set; }
        public string type { get; set; }
        public string name { get; set; }
        public bool enabled { get; set; }
        public bool syncing { get; set; }
        public string role_id { get; set; }
        public int expire_behavior { get; set; }
        public int expire_grace_period { get; set; }
        public DiscordUser user { get; set; }
        public DiscordIntegrationAccount account { get; set; }
        public DateTime synced_at { get; set; }
        public DiscordIntegration(MajickDiscordWrapper.MajickRegex.JsonObject integration_object)
        {            
            if (integration_object.Attributes.ContainsKey("id")) { id = integration_object.Attributes["id"].text_value; }
            if (integration_object.Attributes.ContainsKey("type")) { type = integration_object.Attributes["type"].text_value; }
            if (integration_object.Attributes.ContainsKey("name")) { name = integration_object.Attributes["name"].text_value; }
            if (integration_object.Attributes.ContainsKey("enabled"))
            {
                bool temp_enabled;
                if (bool.TryParse(integration_object.Attributes["enabled"].text_value, out temp_enabled)) { enabled = temp_enabled; }
                else { enabled = (true && false); }
            }
            else { enabled = (true && false); }
            if (integration_object.Attributes.ContainsKey("syncing"))
            {
                bool temp_syncing;
                if (bool.TryParse(integration_object.Attributes["syncing"].text_value, out temp_syncing)) { syncing = temp_syncing; }
                else { enabled = (true && false); }
            }
            else { enabled = (true && false); }
            if (integration_object.Attributes.ContainsKey("role_id")) { role_id = integration_object.Attributes["role_id"].text_value; }
            if (integration_object.Attributes.ContainsKey("expire_behavior"))
            {
                int temp_behavior;
                if (int.TryParse(integration_object.Attributes["expire_behavior"].text_value, out temp_behavior)) { expire_behavior = temp_behavior; }
                else { expire_behavior = -1; }
            }
            else { expire_behavior = -1; }
            if (integration_object.Attributes.ContainsKey("expire_grace_period"))
            {
                int temp_grace_period;
                if (int.TryParse(integration_object.Attributes["expire_grace_period"].text_value, out temp_grace_period)) { expire_grace_period = temp_grace_period; }
                else { expire_grace_period = -1; }
            }
            else { expire_grace_period = -1; }
            if (integration_object.Objects.Keys.Contains("user")) { user = new DiscordUser(integration_object.Objects["user"]); }
            if (integration_object.Objects.Keys.Contains("account")) { account = new DiscordIntegrationAccount(integration_object.Objects["account"]); }
            if (integration_object.Attributes.ContainsKey("synced_at"))
            {
                DateTime when_synced;
                if (DateTime.TryParse(integration_object.Attributes["synced_at"].text_value, out when_synced)) { synced_at = when_synced; }
            }
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject integration_object = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (id != null) { integration_object.AddAttribute("id", id); }
            if (type != null) { integration_object.AddAttribute("type", type); }
            if (name != null) { integration_object.AddAttribute("name", name); }
            if (enabled != (true && false)) { integration_object.AddAttribute("enabled", enabled.ToString(), true); }
            if (syncing != (true && false)) { integration_object.AddAttribute("syncing", syncing.ToString(), true); }
            if (role_id != null) { integration_object.AddAttribute("role_id", role_id); }
            if (expire_behavior > -1) { integration_object.AddAttribute("expire_behavior", expire_behavior.ToString(), true); }
            if (expire_grace_period > -1) { integration_object.AddAttribute("expire_grace_period", expire_grace_period.ToString(), true); }
            if (user != null) { integration_object.AddObject("user", user.ToJson()); }
            if (account != null) { integration_object.AddObject("account", account.ToJson()); }
            if (synced_at != null) { integration_object.AddAttribute("synced_at", synced_at.ToString(), true); }
            return integration_object;
        }
    }
    public class DiscordIntegrationAccount
    {
        public string id { get; set; }
        public string name { get; set; }
        public DiscordIntegrationAccount() { }
        public DiscordIntegrationAccount(MajickDiscordWrapper.MajickRegex.JsonObject account_object)
        {
            if (account_object.Attributes.ContainsKey("id")) { id = account_object.Attributes["id"].text_value; }
            if (account_object.Attributes.ContainsKey("name")) { name = account_object.Attributes["name"].text_value; }
        }
        public DiscordIntegrationAccount(string new_id, string new_name)
        {
            id = new_id;
            name = new_name;
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject integration_account = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (id != null) { integration_account.AddAttribute("id", id); }
            if (name != null) { integration_account.AddAttribute("name", name); }
            return integration_account;
        }
    }
    public class DiscordBan
    {
        public string reason { get; set; }
        public DiscordUser user { get; set; }
        public DiscordBan() { }
        public DiscordBan(MajickDiscordWrapper.MajickRegex.JsonObject ban_object)
        {
            if (ban_object.Attributes.ContainsKey("reason")) { reason = ban_object.Attributes["reason"].text_value; }
            if (ban_object.Objects.Keys.Contains("user")) { user = new DiscordUser(ban_object.Objects["user"]); }
        }
        public DiscordBan(string new_reason, DiscordUser banned_user)
        {
            reason = new_reason;
            user = banned_user;
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject ban_object = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (reason != null) { ban_object.AddAttribute("reason", reason); }
            if (user != null) { ban_object.AddObject("user", user.ToJson()); }
            return ban_object;
        }
    }
    public class DiscordGuildPreview
    {
        public string id { get; set; }
        public string name { get; set; }
        public string icon { get; set; }
        public string splash { get; set; }
        public string discovery_splash { get; set; }
        public Dictionary<string, DiscordEmoji> emojis { get; set; }
        public List<string> features { get; set; }
        public int approximate_member_count { get; set; }
        public int approximate_presence_count { get; set; }
        public string description { get; set; }
        public DiscordGuildPreview() { }
        public DiscordGuildPreview(MajickRegex.JsonObject new_guild_preview) 
        {
            if (new_guild_preview.Attributes.ContainsKey("id")) { id = new_guild_preview.Attributes["id"].text_value; }
            if (new_guild_preview.Attributes.ContainsKey("name")) { name = new_guild_preview.Attributes["name"].text_value; }
            if (new_guild_preview.Attributes.ContainsKey("icon")) { icon = new_guild_preview.Attributes["icon"].text_value; }
            if (new_guild_preview.Attributes.ContainsKey("splash")) { splash = new_guild_preview.Attributes["splash"].text_value; }
            if (new_guild_preview.Attributes.ContainsKey("discovery_splash")) { name = new_guild_preview.Attributes["discovery_splash"].text_value; }
            if (new_guild_preview.ObjectLists.ContainsKey("emojis"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_emoji in new_guild_preview.ObjectLists["emojis"])
                {
                    DiscordEmoji preview_emoji = new DiscordEmoji(current_emoji);
                    emojis.Add(preview_emoji.id, preview_emoji);
                }
            }
            features = new List<string>();
            if (new_guild_preview.AttributeLists.ContainsKey("features"))
            {
                foreach (JsonAttribute feature in new_guild_preview.AttributeLists["features"]) { features.Add(feature.text_value); }
            }
            if (new_guild_preview.Attributes.ContainsKey("approximate_member_count"))
            {
                int temp_approximate_member_count;
                if (int.TryParse(new_guild_preview.Attributes["approximate_member_count"].text_value, out temp_approximate_member_count)) { approximate_member_count = temp_approximate_member_count; }
            }
            if (new_guild_preview.Attributes.ContainsKey("approximate_presence_count"))
            {
                int temp_approximate_presence_count;
                if (int.TryParse(new_guild_preview.Attributes["approximate_presence_count"].text_value, out temp_approximate_presence_count)) { approximate_presence_count = temp_approximate_presence_count; }
            }
            if (new_guild_preview.Attributes.ContainsKey("description")) { description = new_guild_preview.Attributes["description"].text_value; }
        }
    }
    public class DiscordChannel
    {
        public string id { get; set; }
        public ChannelType type { get; set; }
        public string guild_id { get; set; }
        public int position { get; set; }
        public Dictionary<string, PermissionOverwrite> permission_overwrites { get; set; }
        public string name { get; set; }
        public string mention { get { return "<#" + id + ">"; } }
        public string topic { get; set; }
        public bool nsfw { get; set; }
        public string last_message_id { get; set; }
        public int bitrate { get; set; }
        public int user_limit { get; set; }
        public int rate_limit_per_user { get; set; }
        public List<DiscordUser> recipients { get; set; }
        public string icon { get; set; }
        public string owner_id { get; set; }
        public string application_id { get; set; }
        public string parent_id { get; set; }
        public DateTime last_pin_timestamp { get; set; }
        public string rtc_region { get; set; }
        public VideoQualityMode video_quality_mode { get; set; }
        public DiscordThreadMetadata thread_metadata { get; set; }
        public DiscordThreadMember member { get; set; }
        public int default_auto_archive_duration { get; set; }
        public Dictionary<string, DiscordMessage> messages { get; set; }
        public Dictionary<string, DiscordWebhook> webhooks { get; set; }
        public MajickDiscordWrapper.MajickRegex.JsonObject base_object { get; set; }
        public DiscordChannel() { }
        public DiscordChannel(MajickDiscordWrapper.MajickRegex.JsonObject channel_object)
        {
            base_object = channel_object;
            ChannelType temp_type = ChannelType.GUILD_TEXT;
            recipients = new List<DiscordUser>();
            messages = new Dictionary<string, DiscordMessage>();
            webhooks = new Dictionary<string, DiscordWebhook>();
            permission_overwrites = new Dictionary<string, PermissionOverwrite>();
            if (channel_object.Attributes.ContainsKey("id")) { id = channel_object.Attributes["id"].text_value; }
            if (channel_object.Attributes.ContainsKey("type"))
            {
                if (Enum.TryParse(channel_object.Attributes["type"].text_value, out temp_type)) { type = temp_type; }
            }
            if (channel_object.Attributes.ContainsKey("guild_id")) { guild_id = channel_object.Attributes["guild_id"].text_value; }
            if (channel_object.Attributes.ContainsKey("position"))
            {
                int temp_position;
                if (int.TryParse(channel_object.Attributes["position"].text_value, out temp_position)) { position = temp_position; }
            }
            if (channel_object.ObjectLists.ContainsKey("permission_overwrites"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_overwrite in channel_object.ObjectLists["permission_overwrites"])
                {
                    PermissionOverwrite current_channel_perm = new PermissionOverwrite(current_overwrite);
                    permission_overwrites.Add(current_channel_perm.id, current_channel_perm);
                }
            }
            if (channel_object.Attributes.ContainsKey("name")) { name = channel_object.Attributes["name"].text_value; }
            if (channel_object.Attributes.ContainsKey("topic")) { topic = channel_object.Attributes["topic"].text_value; }
            if (channel_object.Attributes.ContainsKey("nsfw"))
            {
                bool temp_nsfw;
                if (bool.TryParse(channel_object.Attributes["nsfw"].text_value, out temp_nsfw)) { nsfw = temp_nsfw; }
            }
            if (channel_object.Attributes.ContainsKey("last_message_id")) { last_message_id = channel_object.Attributes["last_message_id"].text_value; }
            if (channel_object.Attributes.ContainsKey("bitrate"))
            {
                int temp_bitrate;
                if (int.TryParse(channel_object.Attributes["bitrate"].text_value, out temp_bitrate)) { bitrate = temp_bitrate; }
            }
            if (channel_object.Attributes.ContainsKey("user_limit"))
            {
                int temp_user_limit;
                if (int.TryParse(channel_object.Attributes["user_limit"].text_value, out temp_user_limit)) { user_limit = temp_user_limit; }
            }
            if (channel_object.Attributes.ContainsKey("rate_limit_per_user"))
            {
                int temp_rate_limit;
                if (int.TryParse(channel_object.Attributes["rate_limit_per_user"].text_value, out temp_rate_limit)) { rate_limit_per_user = temp_rate_limit; }
            }
            if (channel_object.ObjectLists.ContainsKey("recipients"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_recipient in channel_object.ObjectLists["recipients"])
                {
                    recipients.Add(new DiscordUser(current_recipient));
                }
            }
            if (channel_object.Attributes.ContainsKey("icon")) { icon = channel_object.Attributes["icon"].text_value; }
            if (channel_object.Attributes.ContainsKey("owner_id")) { owner_id = channel_object.Attributes["owner_id"].text_value; }
            if (channel_object.Attributes.ContainsKey("application_id")) { application_id = channel_object.Attributes["application_id"].text_value; }
            if (channel_object.Attributes.ContainsKey("parent_id")) { parent_id = channel_object.Attributes["parent_id"].text_value; }
            if (channel_object.Attributes.ContainsKey("last_pin_timestamp"))
            {
                DateTime temp_last_pin;
                if (DateTime.TryParse(channel_object.Attributes["last_pin_timestamp"].text_value, out temp_last_pin)) { last_pin_timestamp = temp_last_pin; }
            }
            if (channel_object.Attributes.ContainsKey("rtc_region")) { rtc_region = channel_object.Attributes["rtc_region"].text_value; }
            if (channel_object.Attributes.ContainsKey("video_quality_mode"))
            {
                VideoQualityMode temp_video_quality_mode;
                if (Enum.TryParse(channel_object.Attributes["video_quality_mode"].text_value, out temp_video_quality_mode)) { video_quality_mode = temp_video_quality_mode; }
            }
            if (channel_object.Objects.ContainsKey("thread_metadata")) { thread_metadata = new DiscordThreadMetadata(channel_object.Objects["thread_metadata"]); }
            if (channel_object.Objects.ContainsKey("member")) { member = new DiscordThreadMember(channel_object.Objects["member"]); }
            if (channel_object.Attributes.ContainsKey("default_auto_archive_duration"))
            {
                int temp_default_auto_archive_duration;
                if (int.TryParse(channel_object.Attributes["default_auto_archive_duration"].text_value, out temp_default_auto_archive_duration)) { default_auto_archive_duration = temp_default_auto_archive_duration; }
            }
        }
        public async Task<DiscordChannel> CreateAsync(string bot_token, string channel_name, ChannelType type = ChannelType.GUILD_TEXT) { return await Task.Run(() => Create(bot_token, channel_name, type)); }
        public DiscordChannel Create(string bot_token, string channel_name, ChannelType type = ChannelType.GUILD_TEXT)
        {
            DiscordChannel channel;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordChannel> channels = new Dictionary<string, DiscordChannel>();
            rcGuildClient = new RestClient("https://discord.com/api");
            if (guild_id != "") { rrGuildRequest = new RestRequest("/guilds/" + guild_id + "/channels", Method.Post); }
            else { rrGuildRequest = new RestRequest("/guilds/" + guild_id + "/channels", Method.Post); }
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            GuildRequestBody.AddAttribute("name", channel_name);
            GuildRequestBody.AddAttribute("type", ((int)type).ToString());
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            channel = new DiscordChannel(GuildResponseContent);
            return channel;
        }
        public async Task<DiscordChannel> CreateAsync(ChannelUpdateObject new_channel, string bot_token) { return await Task.Run(() => Create(new_channel, bot_token)); }
        public DiscordChannel Create(ChannelUpdateObject new_channel, string bot_token)
        {
            DiscordChannel channel;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordChannel> channels = new Dictionary<string, DiscordChannel>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/guilds/" + id + "/channels", Method.Post);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            GuildRequestBody = new_channel.ToJson();
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            channel = new DiscordChannel(GuildResponseContent);
            return channel;
        }
        public async Task<DiscordChannel> UpdateAsync(ChannelUpdateObject new_channel, string bot_token) { return await Task.Run(() => Update(new_channel, bot_token)); }
        public DiscordChannel Update(ChannelUpdateObject new_channel, string bot_token)
        {
            DiscordChannel channel;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordChannel> channels = new Dictionary<string, DiscordChannel>();
            rcGuildClient = new RestClient("https://discord.com/api");
            if (guild_id != "") { rrGuildRequest = new RestRequest("/channels/" + id, Method.Patch); }
            else { rrGuildRequest = new RestRequest("/channels/" + id, Method.Patch); }
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            GuildRequestBody = new_channel.ToJson();
            //GuildRequestBody.AddAttribute("name", new_channel.name);
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            channel = new DiscordChannel(GuildResponseContent);
            return channel;
        }
        public async Task<Dictionary<string, DiscordInvite>> GetInvitesAsync(string bot_token) { return await Task.Run(() => GetInvites(bot_token)); }
        public Dictionary<string, DiscordInvite> GetInvites(string bot_token)
        {
            DiscordInvite invite;
            RestClient rcChannelClient;
            RestRequest rrChannelRequest;
            RestResponse rsChannelResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject ChannelRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject ChannelResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
            rcChannelClient = new RestClient("https://discord.com/api");
            rrChannelRequest = new RestRequest("/channels/" + id + "/invites", Method.Get);
            rrChannelRequest.RequestFormat = DataFormat.Json;
            rrChannelRequest.AddHeader("Content-Type", "application/json");
            rrChannelRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrChannelRequest.AddJsonBody(ChannelRequestBody.ToRawText(false));
            rsChannelResponse = rcChannelClient.Execute(rrChannelRequest);
            ChannelResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsChannelResponse.Content);
            foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_message in ChannelResponseContent.ObjectLists["objects"])
            {
                invite = new DiscordInvite(current_message);
                invites.Add(invite.code, invite);
            }
            return invites;
        }
        public async Task<bool> AddPermissionOverwriteAsync(PermissionOverwrite overwrite, string bot_token) { return await Task.Run(() => AddPermissionOverwrite(overwrite, bot_token)); }
        public bool AddPermissionOverwrite(PermissionOverwrite overwrite, string bot_token)
        {
            RestClient rcChannelClient;
            RestRequest rrChannelRequest;
            RestResponse rsChannelResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject ChannelRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject ChannelResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
            rcChannelClient = new RestClient("https://discord.com/api");
            rrChannelRequest = new RestRequest("/channels/" + id + "/permissions/" + overwrite.id, Method.Put);
            rrChannelRequest.RequestFormat = DataFormat.Json;
            rrChannelRequest.AddHeader("Content-Type", "application/json");
            rrChannelRequest.AddHeader("Authorization", "Bot " + bot_token);
            if (overwrite.allow != -1) { ChannelRequestBody.AddAttribute("allow", overwrite.allow.ToString()); }
            if (overwrite.deny != -1) { ChannelRequestBody.AddAttribute("deny", overwrite.deny.ToString()); }
            ChannelRequestBody.AddAttribute("type", overwrite.type);
            rrChannelRequest.AddJsonBody(ChannelRequestBody.ToRawText(false));
            rsChannelResponse = rcChannelClient.Execute(rrChannelRequest);
            return rsChannelResponse.IsSuccessful;
        }
        public async Task<bool> DeletePermissionOverwriteAsync(PermissionOverwrite overwrite, string bot_token) { return await Task.Run(() => DeletePermissionOverwrite(overwrite, bot_token)); }
        public bool DeletePermissionOverwrite(PermissionOverwrite overwrite, string bot_token)
        {
            RestClient rcChannelClient;
            RestRequest rrChannelRequest;
            RestResponse rsChannelResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject ChannelRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject ChannelResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcChannelClient = new RestClient("https://discord.com/api");
            rrChannelRequest = new RestRequest("/channels/" + id + "/permissions/" + overwrite.id, Method.Delete);
            rrChannelRequest.RequestFormat = DataFormat.Json;
            rrChannelRequest.AddHeader("Content-Type", "application/json");
            rrChannelRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrChannelRequest.AddJsonBody(ChannelRequestBody.ToRawText(false));
            rsChannelResponse = rcChannelClient.Execute(rrChannelRequest);
            return rsChannelResponse.IsSuccessful;
        }
        public async Task<bool> StartTypingAsync(string bot_token) { return await Task.Run(() => StartTyping(bot_token)); }
        public bool StartTyping(string bot_token)
        {
            RestClient rcChannelClient;
            RestRequest rrChannelRequest;
            RestResponse rsChannelResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject ChannelRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject ChannelResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
            rcChannelClient = new RestClient("https://discord.com/api");
            rrChannelRequest = new RestRequest("/channels/" + id + "/typing", Method.Post);
            rrChannelRequest.RequestFormat = DataFormat.Json;
            rrChannelRequest.AddHeader("Content-Type", "application/json");
            rrChannelRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrChannelRequest.AddJsonBody(ChannelRequestBody.ToRawText(false));
            rsChannelResponse = rcChannelClient.Execute(rrChannelRequest);
            return rsChannelResponse.IsSuccessful;
        }
        public async Task<DiscordMessage> SendMessageAsync(string bot_token, MajickRegex.JsonObject new_message)
        {
            return await Task.Run(() => SendMessage(bot_token, new_message));
        }
        public DiscordMessage SendMessage(string bot_token, MajickRegex.JsonObject new_message)
        {
            DiscordMessage message;
            RestClient rcMessageClient;
            RestRequest rrMessageRequest;
            RestResponse rsMessageResponse;
            rcMessageClient = new RestClient("https://discord.com/api");
            rrMessageRequest = new RestRequest("/channels/" + id + "/messages", Method.Post);
            rrMessageRequest.RequestFormat = DataFormat.Json;
            rrMessageRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrMessageRequest.AddJsonBody(new_message.ToRawText(false));
            rsMessageResponse = rcMessageClient.Execute(rrMessageRequest);
            message = new DiscordMessage(new MajickDiscordWrapper.MajickRegex.JsonObject(rsMessageResponse.Content));
            return message;
        }
            public async Task<DiscordMessage> SendMessageAsync(string bot_token, string message_text, Embed embed = null, MajickDiscordWrapper.MajickRegex.JsonObject payload_json = null)
        {
            return await Task.Run(() => SendMessage(bot_token, message_text, embed, payload_json));
        }
        public DiscordMessage SendMessage(string bot_token, string message_text, Embed embed = null, MajickDiscordWrapper.MajickRegex.JsonObject payload_json = null, string filename = "")
        {
            DiscordMessage message;
            RestClient rcMessageClient;
            RestRequest rrMessageRequest;
            RestResponse rsMessageResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject MessageRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcMessageClient = new RestClient("https://discord.com/api");
            rrMessageRequest = new RestRequest("/channels/" + id + "/messages", Method.Post);
            if (payload_json != null)
            {
                MessageRequestBody.AddObject("payload_json", payload_json);
                MessageRequestBody.AddAttribute("file", message_text);
                rrMessageRequest.AddHeader("Content-Type", "multipart/form-data");
                rrMessageRequest.AddHeader("Content-Disposition", "attachment; " + filename);
            }
            else
            {
                MessageRequestBody.AddAttribute("content", message_text);
                if (embed != null) { MessageRequestBody.AddObject("embed", embed.ToJson()); }
                rrMessageRequest.AddHeader("Content-Type", "application/json");
            }
            //MajickRegex.JsonObject AllowedMentions = new MajickRegex.JsonObject();
            //AllowedMentions.Name = "allowed_mentions";
            //List<JsonAttribute> MentionsList = new List<JsonAttribute>();
            //AllowedMentions.AddAttributeList("parse", MentionsList);
            //MessageRequestBody.AddObject("", AllowedMentions);
            rrMessageRequest.RequestFormat = DataFormat.Json;            
            rrMessageRequest.AddHeader("Authorization", "Bot " + bot_token);
            string json_body = MessageRequestBody.ToRawText(false).Substring(0, MessageRequestBody.ToRawText(false).Length - 1) + ",\"allowed_mentions\":{\"parse\":[]}}";
            string message_components = "";
            json_body += message_components;
            rrMessageRequest.AddJsonBody(json_body);
            rsMessageResponse = rcMessageClient.Execute(rrMessageRequest);
            message = new DiscordMessage(new MajickDiscordWrapper.MajickRegex.JsonObject(rsMessageResponse.Content));
            return message;
        }
        public async Task<DiscordMessage> SendMessageAsync(string bot_token, string message_text, string allowed_mention = "", Embed embed = null, MajickDiscordWrapper.MajickRegex.JsonObject payload_json = null)
        {
            return await Task.Run(() => SendMessage(bot_token, message_text, allowed_mention, embed, payload_json));
        }
        public DiscordMessage SendMessage(string bot_token, string message_text, string allowed_mention = "", Embed embed = null, MajickDiscordWrapper.MajickRegex.JsonObject payload_json = null, string filename = "")
        {
            DiscordMessage message;
            RestClient rcMessageClient;
            RestRequest rrMessageRequest;
            RestResponse rsMessageResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject MessageRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcMessageClient = new RestClient("https://discord.com/api");
            rrMessageRequest = new RestRequest("/channels/" + id + "/messages", Method.Post);
            if (payload_json != null)
            {
                MessageRequestBody.AddObject("payload_json", payload_json);
                MessageRequestBody.AddAttribute("file", message_text);
                rrMessageRequest.AddHeader("Content-Type", "multipart/form-data");
                rrMessageRequest.AddHeader("Content-Disposition", "attachment; " + filename);
            }
            else
            {
                MessageRequestBody.AddAttribute("content", message_text);
                if (embed != null) { MessageRequestBody.AddObject("embed", embed.ToJson()); }
                rrMessageRequest.AddHeader("Content-Type", "application/json");
            }
            //MajickRegex.JsonObject AllowedMentions = new MajickRegex.JsonObject();
            //AllowedMentions.Name = "allowed_mentions";
            //List<JsonAttribute> MentionsList = new List<JsonAttribute>();
            //AllowedMentions.AddAttributeList("parse", MentionsList);
            //MessageRequestBody.AddObject("", AllowedMentions);
            rrMessageRequest.RequestFormat = DataFormat.Json;
            rrMessageRequest.AddHeader("Authorization", "Bot " + bot_token);
            string json_body = MessageRequestBody.ToRawText(false).Substring(0, MessageRequestBody.ToRawText(false).Length - 1) + ",\"allowed_mentions\":{\"users\":[" + allowed_mention + "]}}";
            rrMessageRequest.AddJsonBody(json_body);
            rsMessageResponse = rcMessageClient.Execute(rrMessageRequest);
            message = new DiscordMessage(new MajickDiscordWrapper.MajickRegex.JsonObject(rsMessageResponse.Content));
            return message;
        }
        public async Task<Dictionary<string, DiscordMessage>> GetMessagesAsync(string bot_token, MessageSearchType search_type = MessageSearchType.before, string message_id = "", int limit = 100)
        {
            return await Task.Run(() => GetMessages(bot_token, search_type, message_id, limit));
        }
        public Dictionary<string, DiscordMessage> GetMessages(string bot_token, MessageSearchType search_type = MessageSearchType.before, string message_id = "", int limit = 100)
        {
            DiscordMessage message;
            RestClient rcMessageClient;
            RestRequest rrMessageRequest;
            RestResponse rsMessageResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject MessageRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject MessageResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordMessage> messages = new Dictionary<string, DiscordMessage>();
            rcMessageClient = new RestClient("https://discord.com/api");
            rrMessageRequest = new RestRequest("/channels/" + id + "/messages", Method.Get);
            rrMessageRequest.RequestFormat = DataFormat.Json;
            rrMessageRequest.AddHeader("Content-Type", "application/json");
            rrMessageRequest.AddHeader("Authorization", "Bot " + bot_token);
            //rrMessageRequest.AddHeader("Authorization", "Bot " + OwnerClient.BotToken);
            switch (search_type)
            {
                case MessageSearchType.around:
                    rrMessageRequest.AddParameter("around", message_id);
                    break;
                case MessageSearchType.before:
                    rrMessageRequest.AddParameter("before", message_id);
                    break;
                case MessageSearchType.after:
                    rrMessageRequest.AddParameter("after", message_id);
                    break;
            }
            rrMessageRequest.AddParameter("limit", limit.ToString());
            rrMessageRequest.AddJsonBody(MessageRequestBody.ToRawText(false));
            rsMessageResponse = rcMessageClient.Execute(rrMessageRequest);
            MessageResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsMessageResponse.Content);
            foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_message in MessageResponseContent.ObjectLists["objects"])
            {
                message = new DiscordMessage(current_message, id, guild_id);
                messages.Add(message.id, message);
            }
            return messages;
        }
        public async Task<Dictionary<string, DiscordMessage>> GetPinnedMessagesAsync(string bot_token) { return await Task.Run(() => GetPinnedMessages(bot_token)); }
        public Dictionary<string, DiscordMessage> GetPinnedMessages(string bot_token)
        {
            DiscordMessage message;
            RestClient rcMessageClient;
            RestRequest rrMessageRequest;
            RestResponse rsMessageResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject MessageRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject MessageResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordMessage> pins = new Dictionary<string, DiscordMessage>();
            rcMessageClient = new RestClient("https://discord.com/api");
            rrMessageRequest = new RestRequest("/channels/" + id + "/pins", Method.Get);
            rrMessageRequest.RequestFormat = DataFormat.Json;
            rrMessageRequest.AddHeader("Content-Type", "application/json");
            rrMessageRequest.AddHeader("Authorization", "Bot " + bot_token);
            //rrMessageRequest.AddHeader("Authorization", "Bot " + OwnerClient.BotToken);
            rrMessageRequest.AddJsonBody(MessageRequestBody.ToRawText(false));
            rsMessageResponse = rcMessageClient.Execute(rrMessageRequest);
            MessageResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsMessageResponse.Content);
            foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_message in MessageResponseContent.ObjectLists["objects"])
            {
                message = new DiscordMessage(current_message, id, guild_id);
                pins.Add(message.id, message);
            }
            return pins;
        }
        public async Task<DiscordMessage> GetMessageByIDAsync(string message_id, string bot_token) { return await Task.Run(() => GetMessageByID(message_id, bot_token)); }
        public DiscordMessage GetMessageByID(string message_id, string bot_token)
        {
            DiscordMessage message;
            RestClient rcMessageClient;
            RestRequest rrMessageRequest;
            RestResponse rsMessageResponse;
            rcMessageClient = new RestClient("https://discord.com/api");
            rrMessageRequest = new RestRequest("/channels/" + id + "/messages/" + message_id, Method.Get);
            rrMessageRequest.RequestFormat = DataFormat.Json;
            rrMessageRequest.AddHeader("Content-Type", "application/json");
            rrMessageRequest.AddHeader("Authorization", "Bot " + bot_token);
            //rrMessageRequest.AddHeader("Authorization", "Bot " + OwnerClient.BotToken);
            rsMessageResponse = rcMessageClient.Execute(rrMessageRequest);
            message = new DiscordMessage(new MajickDiscordWrapper.MajickRegex.JsonObject(rsMessageResponse.Content), id, guild_id);
            return message;
        }
        public async Task<bool> PinByIDAsync(string message_id, string bot_token) { return PinByID(message_id, bot_token); }
        public bool PinByID(string message_id, string bot_token)
        {
            RestClient rcMessageClient;
            RestRequest rrMessageRequest;
            RestResponse rsMessageResponse;
            rcMessageClient = new RestClient("https://discord.com/api");
            rrMessageRequest = new RestRequest("/channels/" + id + "/pins/" + message_id, Method.Put); 
            rrMessageRequest.RequestFormat = DataFormat.Json;
            rrMessageRequest.AddHeader("Content-Type", "application/json");
            rrMessageRequest.AddHeader("Authorization", "Bot " + bot_token);
            //rrMessageRequest.AddHeader("Authorization", "Bot " + OwnerClient.BotToken);
            rsMessageResponse = rcMessageClient.Execute(rrMessageRequest);
            return rsMessageResponse.IsSuccessful;
        }
        public async Task<bool> RemovePinByIDAsync(string message_id, string bot_token) { return await Task.Run(() => RemovePinByID(message_id, bot_token)); }
        public bool RemovePinByID(string message_id, string bot_token)
        {
            RestClient rcMessageClient;
            RestRequest rrMessageRequest;
            RestResponse rsMessageResponse;
            rcMessageClient = new RestClient("https://discord.com/api");
            rrMessageRequest = new RestRequest("/channels/" + id + "/pins/" + message_id, Method.Delete); 
            rrMessageRequest.RequestFormat = DataFormat.Json;
            rrMessageRequest.AddHeader("Content-Type", "application/json");
            rrMessageRequest.AddHeader("Authorization", "Bot " + bot_token);
            //rrMessageRequest.AddHeader("Authorization", "Bot " + OwnerClient.BotToken);
            rsMessageResponse = rcMessageClient.Execute(rrMessageRequest);
            return rsMessageResponse.IsSuccessful;
        }
        public async Task<DiscordMessage> EditMessageByIDAsync(string content, string message_id, string bot_token) { return await Task.Run(() => EditMessageByID(content, message_id, bot_token)); }
        public DiscordMessage EditMessageByID(string content, string message_id, string bot_token)
        {
            DiscordMessage message;
            RestClient rcMessageClient;
            RestRequest rrMessageRequest;
            RestResponse rsMessageResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject MessageRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcMessageClient = new RestClient("https://discord.com/api");
            rrMessageRequest = new RestRequest("/channels/" + id + "/messages/" + message_id, Method.Patch);
            rrMessageRequest.RequestFormat = DataFormat.Json;
            rrMessageRequest.AddHeader("Content-Type", "application/json");
            rrMessageRequest.AddHeader("Authorization", "Bot " + bot_token);
            //rrMessageRequest.AddHeader("Authorization", "Bot " + OwnerClient.BotToken);
            MessageRequestBody.AddAttribute("content", content);
            rrMessageRequest.AddJsonBody(MessageRequestBody.ToRawText(false));
            rsMessageResponse = rcMessageClient.Execute(rrMessageRequest);
            message = new DiscordMessage(new MajickDiscordWrapper.MajickRegex.JsonObject(rsMessageResponse.Content), id, guild_id);
            return message;
        }
        public async Task<bool> DeleteMessageByIDAsync(string message_id, string bot_token) { return await Task.Run(() => DeleteMessageByID(message_id, bot_token)); }
        public bool DeleteMessageByID(string message_id, string bot_token)
        {
            RestClient rcMessageClient;
            RestRequest rrMessageRequest;
            RestResponse rsMessageResponse;
            rcMessageClient = new RestClient("https://discord.com/api");
            rrMessageRequest = new RestRequest("/channels/" + id + "/messages/" + message_id, Method.Delete);
            rrMessageRequest.RequestFormat = DataFormat.Json;
            rrMessageRequest.AddHeader("Content-Type", "application/json");
            rrMessageRequest.AddHeader("Authorization", "Bot " + bot_token);
            //rrMessageRequest.AddHeader("Authorization", "Bot " + OwnerClient.BotToken);
            rsMessageResponse = rcMessageClient.Execute(rrMessageRequest);
            return rsMessageResponse.IsSuccessful;
        }
        public async Task<bool> DeleteMessages_BulkAsync(List<string> deleted_ids, string bot_token) { return await Task.Run(() => DeleteMessages_Bulk(deleted_ids, bot_token)); }
        public bool DeleteMessages_Bulk(List<string> deleted_ids, string bot_token)
        {
            RestClient rcMessageClient;
            RestRequest rrMessageRequest;
            RestResponse rsMessageResponse;
            List<JsonAttribute> deleted_id_atts = new List<JsonAttribute>();
            MajickDiscordWrapper.MajickRegex.JsonObject MessageRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            foreach (string id in deleted_ids)
            {
                JsonAttribute current_id = new JsonAttribute(id);
                deleted_id_atts.Add(current_id);
            }
            MessageRequestBody.AddAttributeList("messages", deleted_id_atts);
            rcMessageClient = new RestClient("https://discord.com/api");
            rrMessageRequest = new RestRequest("/channels/" + id + "/messages/bulk-delete", Method.Post);
            rrMessageRequest.RequestFormat = DataFormat.Json;
            rrMessageRequest.AddHeader("Content-Type", "application/json");
            rrMessageRequest.AddHeader("Authorization", "Bot " + bot_token);
            //rrMessageRequest.AddHeader("Authorization", "Bot " + OwnerClient.BotToken);
            rrMessageRequest.AddJsonBody(MessageRequestBody.ToRawText(false));
            rsMessageResponse = rcMessageClient.Execute(rrMessageRequest);
            return rsMessageResponse.IsSuccessful;
        }
        public async Task<DiscordWebhook> CreateWebhookAsync(string name, string bot_token, string avatar = "") { return await Task.Run(() => CreateWebhook(name, bot_token, avatar)); }
        public DiscordWebhook CreateWebhook(string name, string bot_token, string avatar = "")
        {
            DiscordWebhook webhook;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/channels/" + id + "/webhooks", Method.Post);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            GuildRequestBody.AddAttribute("name", name);
            if(avatar != "") { GuildRequestBody.AddAttribute("avatar", avatar); }
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            webhook = new DiscordWebhook(GuildResponseContent);
            if (!webhooks.ContainsKey(webhook.id)) { webhooks.Add(webhook.id, webhook); }
            return webhook;
        }
        public async Task GetWebhooksAsync(string bot_token) { await Task.Run(() => GetWebhooks(bot_token)); }
        public void GetWebhooks(string bot_token)
        {
            DiscordWebhook webhook;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordRole> Roles = new Dictionary<string, DiscordRole>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/channels/" + id + "/webhooks", Method.Get);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            foreach(MajickDiscordWrapper.MajickRegex.JsonObject webhook_obj in GuildResponseContent.ObjectLists["data"])
            {
                webhook = new DiscordWebhook(webhook_obj);
                if (!webhooks.ContainsKey(webhook.id)) { webhooks.Add(webhook.id, webhook); }
            }
        }
        public async Task<DiscordWebhook> GetWebhookByIDAsync(string webhook_id, string bot_token) { return await Task.Run(() => GetWebhookByID(webhook_id, bot_token)); }
        public DiscordWebhook GetWebhookByID(string webhook_id, string bot_token)
        {
            DiscordWebhook webhook;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/webhooks/" + webhook_id, Method.Get);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            webhook = new DiscordWebhook(GuildResponseContent);
            if (!webhooks.ContainsKey(webhook.id)) { webhooks.Add(webhook.id, webhook); }
            return webhook;
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject ChannelObject = new MajickDiscordWrapper.MajickRegex.JsonObject();
            //fill in all the properties here
            if (id != null) { ChannelObject.AddAttribute("id", id); }
            if (name != null) { ChannelObject.AddAttribute("name", name); }
            if (icon != null) { ChannelObject.AddAttribute("icon", icon); }
            if (owner_id != null) { ChannelObject.AddAttribute("owner_id", owner_id); }
            if (topic != null) { ChannelObject.AddAttribute("topic", topic); }
            if (application_id != null) { ChannelObject.AddAttribute("application_id", application_id); }
            if (parent_id != null) { ChannelObject.AddAttribute("parent_id", parent_id); }
            if (nsfw != (true && false)) { ChannelObject.AddAttribute("nsfw", nsfw.ToString(), true); }
            ChannelObject.AddAttribute("type", type.ToString()); ;
            if (position > -1) { ChannelObject.AddAttribute("position", position.ToString(), true); }
            if (rate_limit_per_user > -1) { ChannelObject.AddAttribute("rate_limit_per_user", rate_limit_per_user.ToString(), true); }
            if (user_limit > -1) { ChannelObject.AddAttribute("user_limit", user_limit.ToString(), true); }
            if (permission_overwrites.Count > 0)
            {
                List<MajickRegex.JsonObject> overwrite_list = new List<MajickRegex.JsonObject>();
                foreach (PermissionOverwrite overwrite in permission_overwrites.Values)
                {
                    overwrite_list.Add(overwrite.ToJson());
                }
                ChannelObject.AddObjectList("overwrites", overwrite_list);
            }
            return ChannelObject;
        }
    }
    public class PermissionOverwrite
    {
        public string id { get; set; }
        public string type { get; set; }
        public int allow { get; set; }
        public int deny { get; set; }
        public PermissionOverwrite() { }
        public PermissionOverwrite(MajickDiscordWrapper.MajickRegex.JsonObject overwrite_object)
        {
            if (overwrite_object.Attributes.ContainsKey("id")) { id = overwrite_object.Attributes["id"].text_value; }
            if (overwrite_object.Attributes.ContainsKey("type")) { type = overwrite_object.Attributes["type"].text_value; }
            if (overwrite_object.Attributes.ContainsKey("allow"))
            {
                int temp_allow;
                if (int.TryParse(overwrite_object.Attributes["allow"].text_value, out temp_allow)) { allow = temp_allow; }
            }
            if (overwrite_object.Attributes.ContainsKey("deny"))
            {
                int temp_deny;
                if (int.TryParse(overwrite_object.Attributes["deny"].text_value, out temp_deny)) { deny = temp_deny; }
            }
        }
        public PermissionOverwrite(string new_id, string new_type, int new_allow, int new_deny)
        {
            id = new_id;
            type = new_type;
            allow = new_allow;
            deny = new_deny;
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject overwrite_object = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (id != null) { overwrite_object.AddAttribute("id", id); }
            if (type != null) { overwrite_object.AddAttribute("type", type); }
            if (allow > -1) { overwrite_object.AddAttribute("allow", allow.ToString(), true); }
            if (deny > -1) { overwrite_object.AddAttribute("deny", deny.ToString(), true); }
            return overwrite_object;
        }
    }
    public class DiscordMessageReference
    {
        public string message_id { get; set; }
        public string channel_id { get; set; }
        public string guild_id { get; set; }
        public bool fail_if_not_exists { get; set; }
        public DiscordMessageReference() { }
        public DiscordMessageReference(MajickRegex.JsonObject new_reference)
        {
            if (new_reference.Attributes.ContainsKey("message_id")) { message_id = new_reference.Attributes["message_id"].text_value; }
            if (new_reference.Attributes.ContainsKey("channel_id")) { channel_id = new_reference.Attributes["channel_id"].text_value; }
            if (new_reference.Attributes.ContainsKey("guild_id")) { guild_id = new_reference.Attributes["guild_id"].text_value; }
            if (new_reference.Attributes.ContainsKey("fail_if_not_exists"))
            {
                bool temp_fail_if_not_exists;
                if (bool.TryParse(new_reference.Attributes["fail_if_not_exists"].text_value, out temp_fail_if_not_exists)) { fail_if_not_exists = temp_fail_if_not_exists; }
            }
        }
        public MajickRegex.JsonObject ToJson()
        {
            MajickRegex.JsonObject this_json = new MajickRegex.JsonObject();
            this_json.AddAttribute("message_id", message_id);
            this_json.AddAttribute("channel_id", channel_id);
            this_json.AddAttribute("guild_id", guild_id);
            this_json.AddAttribute("fail_if_not_exists", fail_if_not_exists.ToString());
            return this_json;
        }
    }
    public class DiscordSticker
    {
        public string id { get; set; }
        public string pack_id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string tags { get; set; }
        public StickerFormatType format_type { get; set; }
        public bool available { get; set; }
        public string guild_id { get; set; }
        public DiscordUser user { get; set; }
        public int sort_value { get; set; }
        public DiscordSticker() { }
        public DiscordSticker(MajickRegex.JsonObject new_sticker) 
        {
            if (new_sticker.Attributes.ContainsKey("id")) { id = new_sticker.Attributes["id"].text_value; }
            if (new_sticker.Attributes.ContainsKey("pack_id")) { pack_id = new_sticker.Attributes["pack_id"].text_value; }
            if (new_sticker.Attributes.ContainsKey("name")) { name = new_sticker.Attributes["name"].text_value; }
            if (new_sticker.Attributes.ContainsKey("description")) { description = new_sticker.Attributes["description"].text_value; }
            if (new_sticker.Attributes.ContainsKey("tags")) { tags = new_sticker.Attributes["tags"].text_value; }
            if (new_sticker.Attributes.ContainsKey("format_type"))
            {
                StickerFormatType temp_format_type;
                if (Enum.TryParse(new_sticker.Attributes["format_type"].text_value, out temp_format_type)) { format_type = temp_format_type; }
            }
            if (new_sticker.Attributes.ContainsKey("available"))
            {
                bool temp_available;
                if (bool.TryParse(new_sticker.Attributes["available"].text_value, out temp_available)) { available = temp_available; }
            }
            if (new_sticker.Attributes.ContainsKey("guild_id")) { guild_id = new_sticker.Attributes["guild_id"].text_value; }
            if (new_sticker.Objects.ContainsKey("user")) { user = new DiscordUser(new_sticker.Objects["user"]); }
            if (new_sticker.Attributes.ContainsKey("sort_value"))
            {
                int temp_sort_value;
                if (int.TryParse(new_sticker.Attributes["sort_value"].text_value, out temp_sort_value)) { sort_value = temp_sort_value; }
            }
        }
    }
    public class DiscordStickerItem
    {
        public string id { get; set; }
        public string name { get; set; }
        public StickerFormatType format_type { get; set; }
        public DiscordStickerItem() { }
        public DiscordStickerItem(MajickRegex.JsonObject new_sticker_item)
        {
            if (new_sticker_item.Attributes.ContainsKey("id")) { id = new_sticker_item.Attributes["id"].text_value; }
            if (new_sticker_item.Attributes.ContainsKey("name")) { name = new_sticker_item.Attributes["name"].text_value; }
            if (new_sticker_item.Attributes.ContainsKey("format_type"))
            {
                StickerFormatType temp_format_type;
                if (Enum.TryParse(new_sticker_item.Attributes["format_type"].text_value, out temp_format_type)) { format_type = temp_format_type; }
            }
        }
    }
    public class DiscordMessage
    {
        public string id { get; set; }
        public string channel_id { get; set; }
        public string guild_id { get; set; }
        public string guild { get; set; }
        public DiscordUser author { get; set; }
        public DiscordGuildMember member { get; set; }
        public string content { get; set; }
        public DateTime timestamp { get; set; }
        public DateTime edited_timestamp { get; set; }
        public bool tts { get; set; }
        public bool mention_everyone { get; set; }
        public Dictionary<string, DiscordGuildMember> mentions { get; set; }
        public List<string> mention_roles { get; set; }
        public List<DiscordChannelMention> mention_channels { get; set; }
        public List<Attachment> attachments { get; set; }
        public List<Embed> embeds { get; set; }
        public Dictionary<string, DiscordReaction> reactions { get; set; }
        public string nonce { get; set; }
        public bool pinned { get; set; }
        public string webhook_id { get; set; }
        public MessageType type { get; set; }
        public DiscordMessageActivity activity { get; set; }
        public DiscordApplication application { get; set; }
        public string application_id { get; set; }
        public DiscordMessageReference message_reference { get; set; }
        public int flags { get; set; }
        public DiscordMessage referenced_message { get; set; }
        public MessageInteraction interaction { get; set; }
        public DiscordChannel thread { get; set; }
        public List<DiscordMessageComponent> components { get; set; }
        public List<DiscordStickerItem> sticker_items { get; set; }
        public List<DiscordSticker> stickers { get; set; }
        public DiscordMessage() { }
        public DiscordMessage(MajickDiscordWrapper.MajickRegex.JsonObject message_object, string parent_channel = "", string parent_guild = "")
        {            
            guild_id = "";
            if (parent_guild != null) { guild = parent_guild; }
            if (parent_channel != null) { channel_id = parent_channel; }
            if (message_object.Attributes.ContainsKey("id")) { id = message_object.Attributes["id"].text_value; }
            if (message_object.Attributes.ContainsKey("guild_id")) { guild_id = message_object.Attributes["guild_id"].text_value; }
            if (message_object.Attributes.ContainsKey("channel_id")) { channel_id = message_object.Attributes["channel_id"].text_value; }
            if (message_object.Objects.Keys.Contains("author")) { author = new DiscordUser(message_object.Objects["author"]); }
            if (message_object.Objects.Keys.Contains("member")) { member = new DiscordGuildMember(message_object.Objects["member"], guild_id); }
            else { member = new DiscordGuildMember(new MajickDiscordWrapper.MajickRegex.JsonObject(), guild_id); }
            if (message_object.Attributes.ContainsKey("content")) { content = message_object.Attributes["content"].text_value; }
            if (message_object.Attributes.ContainsKey("timestamp"))
            {
                DateTime temp_timestamp;
                if (DateTime.TryParse(message_object.Attributes["timestamp"].text_value, out temp_timestamp)) { timestamp = temp_timestamp; }
            }
            if (message_object.Attributes.ContainsKey("edited_timestamp"))
            {
                DateTime temp_edited;
                if (DateTime.TryParse(message_object.Attributes["edited_timestamp"].text_value, out temp_edited)) { edited_timestamp = temp_edited; }
            }
            if (message_object.Attributes.ContainsKey("tts"))
            {
                bool is_tts;
                if (bool.TryParse(message_object.Attributes["tts"].text_value, out is_tts)) { tts = is_tts; }
            }
            if (message_object.Attributes.ContainsKey("mention_everyone"))
            {
                bool did_mention_everyone;
                if (bool.TryParse(message_object.Attributes["mention_everyone"].text_value, out did_mention_everyone)) { mention_everyone = did_mention_everyone; }
            }
            mentions = new Dictionary<string, DiscordGuildMember>();
            if (message_object.ObjectLists.ContainsKey("mentions"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_mention in message_object.ObjectLists["mentions"])
                {
                    if (current_mention.Attributes.ContainsKey("member")) { }
                    DiscordGuildMember current_member = new DiscordGuildMember(current_mention, guild_id);
                    current_member.user = new DiscordUser(current_mention);
                    mentions.Add(current_member.user.id, current_member);
                }
            }
            mention_roles = new List<string>();
            if (message_object.AttributeLists.ContainsKey("mention_roles"))
            {
                foreach (JsonAttribute current_role in message_object.AttributeLists["mention_roles"]) { mention_roles.Add(current_role.text_value); }
            }
            mention_channels = new List<DiscordChannelMention>();
            if (message_object.ObjectLists.ContainsKey("mention_channels"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_mention in message_object.ObjectLists["mention_channels"])
                {
                    mention_channels.Add(new DiscordChannelMention(current_mention));
                }
            }
            attachments = new List<Attachment>();
            if (message_object.ObjectLists.ContainsKey("attachments"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_attachment in message_object.ObjectLists["attachments"])
                {
                    attachments.Add(new Attachment(current_attachment));
                }
            }
            embeds = new List<Embed>();
            if (message_object.ObjectLists.ContainsKey("embeds"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_embed in message_object.ObjectLists["embeds"])
                {
                    embeds.Add(new Embed(current_embed));
                }
            }
            reactions = new Dictionary<string, DiscordReaction>();
            if (message_object.ObjectLists.ContainsKey("reactions"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_reaction in message_object.ObjectLists["reactions"])
                {
                    DiscordReaction current_emoji = new DiscordReaction(current_reaction);
                    if (!reactions.ContainsKey(current_emoji.emoji.name))
                    reactions.Add(current_emoji.emoji.name, current_emoji);
                }
            }
            if (message_object.Attributes.ContainsKey("nonce")) { nonce = message_object.Attributes["nonce"].text_value; }
            if (message_object.Attributes.ContainsKey("pinned"))
            {
                bool is_pinned;
                if (bool.TryParse(message_object.Attributes["pinned"].text_value, out is_pinned)) { pinned = is_pinned; }
            }
            if (message_object.Attributes.ContainsKey("webhook_id")) { webhook_id = message_object.Attributes["webhook_id"].text_value; }
            if (message_object.Objects.Keys.Contains("activity")) { activity = new DiscordMessageActivity(message_object.Objects["activity"]); }
            if (message_object.Objects.Keys.Contains("application")) { application = new DiscordApplication(message_object.Objects["application"]); }
            if (message_object.Attributes.ContainsKey("application_id")) { application_id = message_object.Attributes["application_id"].text_value; }
            if (message_object.Objects.Keys.Contains("message_reference")) { message_reference = new DiscordMessageReference(message_object.Objects["message_reference"]); }
            if (message_object.Attributes.ContainsKey("flags"))
            {
                int temp_flags;
                if (int.TryParse(message_object.Attributes["flags"].text_value, out temp_flags)) { flags = temp_flags; }
            }
            if (message_object.Objects.Keys.Contains("referenced_message")) { referenced_message = new DiscordMessage(message_object.Objects["referenced_message"]); }
            components = new List<DiscordMessageComponent>();
            if (message_object.Objects.Keys.Contains("interaction")) { interaction = new MessageInteraction(message_object.Objects["interaction"]); }
            if (message_object.Objects.Keys.Contains("thread")) { thread = new DiscordChannel(message_object.Objects["thread"]); }
            if (message_object.ObjectLists.ContainsKey("components"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_component in message_object.ObjectLists["components"])
                {
                    DiscordMessageComponent inner_component;
                    if (current_component.Attributes.ContainsKey("type"))
                    {
                        DiscordMessageComponentType inner_type;
                        if (Enum.TryParse(current_component.Attributes["type"].text_value, out inner_type))
                        {
                            switch (inner_type)
                            {
                                case DiscordMessageComponentType.Button:
                                    inner_component = new DiscordButton(current_component);
                                    break;
                                case DiscordMessageComponentType.SelectMenu:
                                    inner_component = new DiscordSelectMenu(current_component);
                                    break;
                                default:
                                    inner_component = new DiscordActionRow(current_component);
                                    break;
                            }
                            components.Add(inner_component);
                        }                        
                    }
                }
            }
            if (message_object.ObjectLists.ContainsKey("sticker_items"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_sticker_item in message_object.ObjectLists["sticker_items"])
                {
                    sticker_items.Add(new DiscordStickerItem(current_sticker_item));
                }
            }
            if (message_object.ObjectLists.ContainsKey("stickers"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_sticker in message_object.ObjectLists["stickers"])
                {
                    stickers.Add(new DiscordSticker(current_sticker));
                }
            }
        }
        public List<MessageFlags> GetNamedMessageFlags(int flag_value)
        {
            List<MessageFlags> NamedFlags = new List<MessageFlags>();
            if (flag_value >= (int)MessageFlags.LOADING)
            {
                NamedFlags.Add(MessageFlags.LOADING);
                flag_value -= (int)MessageFlags.LOADING;
            }
            if (flag_value >= (int)MessageFlags.EPHEMERAL)
            {
                NamedFlags.Add(MessageFlags.EPHEMERAL);
                flag_value -= (int)MessageFlags.EPHEMERAL;
            }
            if (flag_value >= (int)MessageFlags.HAS_THREAD)
            {
                NamedFlags.Add(MessageFlags.HAS_THREAD);
                flag_value -= (int)MessageFlags.HAS_THREAD;
            }
            if (flag_value >= (int)MessageFlags.URGENT)
            {
                NamedFlags.Add(MessageFlags.URGENT);
                flag_value -= (int)MessageFlags.URGENT;
            }
            if (flag_value >= (int)MessageFlags.SOURCE_MESSAGE_DELETED)
            {
                NamedFlags.Add(MessageFlags.SOURCE_MESSAGE_DELETED);
                flag_value -= (int)MessageFlags.SOURCE_MESSAGE_DELETED;
            }
            if (flag_value >= (int)MessageFlags.SUPPRESS_EMBEDS)
            {
                NamedFlags.Add(MessageFlags.SUPPRESS_EMBEDS);
                flag_value -= (int)MessageFlags.SUPPRESS_EMBEDS;
            }
            if (flag_value >= (int)MessageFlags.IS_CROSSPOST)
            {
                NamedFlags.Add(MessageFlags.IS_CROSSPOST);
                flag_value -= (int)MessageFlags.IS_CROSSPOST;
            }
            if (flag_value >= (int)MessageFlags.CROSSPOSTED)
            {
                NamedFlags.Add(MessageFlags.CROSSPOSTED);
                flag_value -= (int)MessageFlags.CROSSPOSTED;
            }
            return NamedFlags;
        }
        public async Task<bool> PinAsync(string bot_token) { return await Task.Run(() => Pin(bot_token)); }
        public bool Pin(string bot_token)
        {
            RestClient rcMessageClient;
            RestRequest rrMessageRequest;
            RestResponse rsMessageResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject MessageRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcMessageClient = new RestClient("https://discord.com/api");
            if (channel_id != "") { rrMessageRequest = new RestRequest("/channels/" + channel_id + "/pins/" + id, Method.Put); }
            else { rrMessageRequest = new RestRequest("/channels/" + channel_id + "/pins/" + id, Method.Put); }
            rrMessageRequest.RequestFormat = DataFormat.Json;
            rrMessageRequest.AddHeader("Content-Type", "application/json");
            rrMessageRequest.AddHeader("Authorization", "Bot " + bot_token);
            //rrMessageRequest.AddHeader("Authorization", "Bot " + OwnerClient.BotToken);
            rrMessageRequest.AddJsonBody(MessageRequestBody.ToRawText(false));
            rsMessageResponse = rcMessageClient.Execute(rrMessageRequest);
            return rsMessageResponse.IsSuccessful;
        }
        public async Task<bool> RemovePinAsync(string bot_token) { return await Task.Run(() => RemovePin(bot_token)); }
        public bool RemovePin(string bot_token)
        {
            RestClient rcMessageClient;
            RestRequest rrMessageRequest;
            RestResponse rsMessageResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject MessageRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcMessageClient = new RestClient("https://discord.com/api");
            if (channel_id != "") { rrMessageRequest = new RestRequest("/channels/" + channel_id + "/pins/" + id, Method.Delete); }
            else { rrMessageRequest = new RestRequest("/channels/" + channel_id + "/pins/" + id, Method.Delete); }
            rrMessageRequest.RequestFormat = DataFormat.Json;
            rrMessageRequest.AddHeader("Content-Type", "application/json");
            rrMessageRequest.AddHeader("Authorization", "Bot " + bot_token);
            //rrMessageRequest.AddHeader("Authorization", "Bot " + OwnerClient.BotToken);
            rrMessageRequest.AddJsonBody(MessageRequestBody.ToRawText(false));
            rsMessageResponse = rcMessageClient.Execute(rrMessageRequest);
            return rsMessageResponse.IsSuccessful;
        }
        public async Task<bool> AddReactionAsync(DiscordEmoji emoji, string bot_token) { return await Task.Run(() => AddReaction(emoji, bot_token)); }
        public bool AddReaction(DiscordEmoji emoji, string bot_token)
        {
            RestClient rcMessageClient;
            RestRequest rrMessageRequest;
            RestResponse rsMessageResponse;
            rcMessageClient = new RestClient("https://discord.com/api");
            if (emoji.id != "" && emoji.id != null)
            {
                rrMessageRequest = new RestRequest("/channels/" + channel_id + "/messages/" + id + "/reactions/" + emoji.name + "%3A" + emoji.id + "/@me", Method.Put);
            }
            else
            {
                rrMessageRequest = new RestRequest("/channels/" + channel_id + "/messages/" + id + "/reactions/" + emoji.name + "/@me", Method.Put);
            }
            rrMessageRequest.RequestFormat = DataFormat.Json;
            rrMessageRequest.AddHeader("Content-Type", "application/json");
            rrMessageRequest.AddHeader("Authorization", "Bot " + bot_token);
            //rrMessageRequest.AddHeader("Authorization", "Bot " + OwnerClient.BotToken);
            rsMessageResponse = rcMessageClient.Execute(rrMessageRequest);
            return rsMessageResponse.IsSuccessful;
        }
        public async Task<bool> RemoveReactionAsync(DiscordEmoji emoji, string bot_token) { return await Task.Run(() => RemoveReaction(emoji, bot_token)); }
        public bool RemoveReaction(DiscordEmoji emoji, string bot_token)
        {
            RestClient rcMessageClient;
            RestRequest rrMessageRequest;
            RestResponse rsMessageResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject MessageRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcMessageClient = new RestClient("https://discord.com/api");
            if (emoji.is_custom)
            {
                rrMessageRequest = new RestRequest("/channels/" + channel_id + "/messages/" + id + "/reactions/" + emoji.id + ":" + emoji.name + "/@me", Method.Delete);
            }
            else
            {
                rrMessageRequest = new RestRequest("/channels/" + channel_id + "/messages/" + id + "/reactions/" + emoji.name + "/@me", Method.Delete);
            }
            rrMessageRequest.RequestFormat = DataFormat.Json;
            rrMessageRequest.AddHeader("Content-Type", "application/json");
            rrMessageRequest.AddHeader("Authorization", "Bot " + bot_token);
            //rrMessageRequest.AddHeader("Authorization", "Bot " + OwnerClient.BotToken);
            rrMessageRequest.AddJsonBody(MessageRequestBody.ToRawText(false));
            rsMessageResponse = rcMessageClient.Execute(rrMessageRequest);
            return rsMessageResponse.IsSuccessful;
        }
        public async Task<bool> RemoveReactionForUserAsync(DiscordEmoji emoji, string user_id, string bot_token) { return await Task.Run(() => RemoveReactionForUser(emoji, user_id, bot_token)); }
        public bool RemoveReactionForUser(DiscordEmoji emoji, string user_id, string bot_token)
        {
            RestClient rcMessageClient;
            RestRequest rrMessageRequest;
            RestResponse rsMessageResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject MessageRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcMessageClient = new RestClient("https://discord.com/api");
            if (emoji.is_custom)
            {
                if (channel_id != "") { rrMessageRequest = new RestRequest("/channels/" + channel_id + "/messages/" + id + "/reactions/" + emoji.id + ":" + emoji.name, Method.Delete); }
                else { rrMessageRequest = new RestRequest("/channels/" + channel_id + "/messages/" + id + "/reactions/" + emoji.id + ":" + emoji.name, Method.Delete); }
            }
            else
            {
                if (channel_id != "") { rrMessageRequest = new RestRequest("/channels/" + channel_id + "/messages/" + id + "/reactions/" + emoji.name, Method.Delete); }
                else { rrMessageRequest = new RestRequest("/channels/" + channel_id + "/messages/" + id + "/reactions/" + emoji.name, Method.Delete); }
            }
            rrMessageRequest.RequestFormat = DataFormat.Json;
            rrMessageRequest.AddHeader("Content-Type", "application/json");
            rrMessageRequest.AddHeader("Authorization", "Bot " + bot_token);
            //rrMessageRequest.AddHeader("Authorization", "Bot " + OwnerClient.BotToken);
            rrMessageRequest.AddJsonBody(MessageRequestBody.ToRawText(false));
            rsMessageResponse = rcMessageClient.Execute(rrMessageRequest);
            return rsMessageResponse.IsSuccessful;
        }
        public async Task<DiscordMessage> EditAsync(string bot_token, string content, Embed embed = null) { return await Task.Run(() => Edit(bot_token, content, embed)); }
        public DiscordMessage Edit(string bot_token, string content, Embed embed = null)
        {
            DiscordMessage message;
            RestClient rcMessageClient;
            RestRequest rrMessageRequest;
            RestResponse rsMessageResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject MessageRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject MessageRequestResponse = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcMessageClient = new RestClient("https://discord.com/api");
            rrMessageRequest = new RestRequest("/channels/" + channel_id + "/messages/" + id, Method.Patch);        
            rrMessageRequest.RequestFormat = DataFormat.Json;
            rrMessageRequest.AddHeader("Content-Type", "application/json");
            rrMessageRequest.AddHeader("Authorization", "Bot " + bot_token);
            if(embed != null) { MessageRequestBody.AddObject("embed", embed.ToJson()); }
            rrMessageRequest.AddJsonBody(MessageRequestBody.ToRawText(false));
            rsMessageResponse = rcMessageClient.Execute(rrMessageRequest);
            MessageRequestResponse = new MajickDiscordWrapper.MajickRegex.JsonObject(rsMessageResponse.Content);
            message = new DiscordMessage(MessageRequestResponse);
            return message;
        }
        public async Task<Dictionary<string, DiscordUser>> GetReactionsAsync(string bot_token, DiscordEmoji emoji, MessageSearchType search_type = MessageSearchType.after, string user_id = "") { return await Task.Run(() => GetReactions(bot_token, emoji, search_type, user_id)); }
        public Dictionary<string, DiscordUser> GetReactions(string bot_token, DiscordEmoji emoji, MessageSearchType search_type = MessageSearchType.after, string user_id = "")
        {
            DiscordUser user;
            RestClient rcReactionClient;
            RestRequest rrReactionRequest;
            RestResponse rsReactionResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject ReactionRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (user_id != "") { ReactionRequestBody.AddAttribute(search_type.ToString(), user_id); }
            MajickDiscordWrapper.MajickRegex.JsonObject ReactionResponseContent;
            Dictionary<string, DiscordUser> reacting_users = new Dictionary<string, DiscordUser>();
            rcReactionClient = new RestClient("https://discord.com/api");
            if (emoji.is_custom)
            {
                rrReactionRequest = new RestRequest("/channels/" + channel_id + "/messages/" + id + "/reactions/" + emoji.name + "%3A" + emoji.id, Method.Get);
            }
            else
            {
                rrReactionRequest = new RestRequest("/channels/" + channel_id + "/messages/" + id + "/reactions/" + emoji.name.Replace("\\", "%5C"), Method.Get);
            }
            rrReactionRequest.RequestFormat = DataFormat.Json;
            rrReactionRequest.AddHeader("Content-Type", "application/json");
            rrReactionRequest.AddHeader("Authorization", "Bot " + bot_token);
            //rrReactionRequest.AddHeader("Authorization", "Bot " + OwnerClient.BotToken);
            if (user_id != "") { rrReactionRequest.AddJsonBody(ReactionRequestBody.ToRawText(false)); }
            rsReactionResponse = rcReactionClient.Execute(rrReactionRequest);
            string response_object = "{\"users\":" + rsReactionResponse.Content + "}";
            ReactionResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(response_object);
            foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_user in ReactionResponseContent.ObjectLists["users"])
            {
                user = new DiscordUser(current_user);
                reacting_users.Add(user.id, user);
            }
            return reacting_users;
        }
        public async Task<bool> RemoveAllReactionsAsync(string bot_token) { return await Task.Run(() => RemoveAllReactions(bot_token)); }
        public bool RemoveAllReactions(string bot_token)
        {
            RestClient rcMessageClient;
            RestRequest rrMessageRequest;
            RestResponse rsMessageResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject MessageRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcMessageClient = new RestClient("https://discord.com/api");
            if (channel_id != "") { rrMessageRequest = new RestRequest("/channels/" + channel_id + "/messages/" + id + "/reactions", Method.Delete); }
            else { rrMessageRequest = new RestRequest("/channels/" + channel_id + "/messages/" + id + "/reactions", Method.Delete); }
            rrMessageRequest.RequestFormat = DataFormat.Json;
            rrMessageRequest.AddHeader("Content-Type", "application/json");
            rrMessageRequest.AddHeader("Authorization", "Bot " + bot_token);
            //rrMessageRequest.AddHeader("Authorization", "Bot " + OwnerClient.BotToken);
            rrMessageRequest.AddJsonBody(MessageRequestBody.ToRawText(false));
            rsMessageResponse = rcMessageClient.Execute(rrMessageRequest);
            return rsMessageResponse.IsSuccessful;
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject JsonObject = new MajickDiscordWrapper.MajickRegex.JsonObject();
            return JsonObject;
        }
    }
    public class MessageUpdateObject
    {
        public string content { get; set; }
        public bool tts { get; set; }
        public List<Embed> embeds { get; set; }
        public MajickRegex.JsonObject payload_json { get; set; }
        public AllowedMentions allowed_mentions { get; set; }
        public DiscordMessageReference message_reference { get; set; }
        public List<DiscordMessageComponent> components { get; set; }
        public MessageUpdateObject() 
        {
            embeds = new List<Embed>();
            components = new List<DiscordMessageComponent>();
        }
        public MajickRegex.JsonObject ToJson()
        {
            MajickRegex.JsonObject this_json = new MajickRegex.JsonObject();
            this_json.AddAttribute("content", content);
            if (embeds != null)
            {
                if (embeds.Count > 0)
                {
                    List<MajickRegex.JsonObject> json_embeds = new List<MajickRegex.JsonObject>();
                    foreach (Embed embed in embeds)
                    {
                        json_embeds.Add(embed.ToJson());
                    }
                    this_json.AddObjectList("embeds", json_embeds);
                }
            }
            if (payload_json != null) { this_json.AddObject("payload_json", payload_json); }
            if (allowed_mentions != null) { this_json.AddObject("allowed_mentions", allowed_mentions.ToJson()); }
            if (message_reference != null) { this_json.AddObject("message_reference", message_reference.ToJson()); }
            if (components.Count > 0)
            {
                List<MajickRegex.JsonObject> inner_components = new List<MajickRegex.JsonObject>();
                foreach (DiscordMessageComponent component in components)
                {
                    switch (component.type)
                    {
                        case DiscordMessageComponentType.Button:
                            inner_components.Add(((DiscordButton)component).ToJson());
                            break;
                        case DiscordMessageComponentType.SelectMenu:
                            inner_components.Add(((DiscordSelectMenu)component).ToJson());
                            break;
                        default:
                            inner_components.Add(((DiscordActionRow)component).ToJson());
                            break;
                    }
                }
                this_json.AddObjectList("components", inner_components);
            }
            return this_json;
        }
    }
    public class DiscordChannelMention
    {
        public string id { get; set; }
        public string guild_id { get; set; }
        public ChannelType type { get; set; }
        public string name { get; set; }
        public DiscordChannelMention() { }
        public DiscordChannelMention(MajickRegex.JsonObject new_channel_mention) 
        {
            if (new_channel_mention.Attributes.ContainsKey("id")) { id = new_channel_mention.Attributes["id"].text_value; }
            if (new_channel_mention.Attributes.ContainsKey("guild_id")) { guild_id = new_channel_mention.Attributes["guild_id"].text_value; }
            if (new_channel_mention.Attributes.ContainsKey("type"))
            {
                ChannelType temp_nsfw_level;
                if (Enum.TryParse(new_channel_mention.Attributes["type"].text_value, out temp_nsfw_level)) { type = temp_nsfw_level; }
            }
            if (new_channel_mention.Attributes.ContainsKey("name")) { name = new_channel_mention.Attributes["name"].text_value; }
        }
    }
    public class Attachment
    {
        public string id { get; set; }
        public string filename { get; set; }
        public int size { get; set; }
        public string url { get; set; }
        public string proxy_url { get; set; }
        public int height { get; set; }
        public int width { get; set; }
        public Attachment() { }
        public Attachment(MajickDiscordWrapper.MajickRegex.JsonObject attachment_object)
        {
            if (attachment_object.Attributes.ContainsKey("id")) { id = attachment_object.Attributes["id"].text_value; }
            if (attachment_object.Attributes.ContainsKey("filename")) { filename = attachment_object.Attributes["filename"].text_value; }
            if (attachment_object.Attributes.ContainsKey("size"))
            {
                int temp_size;
                if (int.TryParse(attachment_object.Attributes["size"].text_value, out temp_size)) { size = temp_size; }
                else { size = -1; }
            }
            else { size = -1; }
            if (attachment_object.Attributes.ContainsKey("url")) { url = attachment_object.Attributes["url"].text_value; }
            if (attachment_object.Attributes.ContainsKey("proxy_url")) { proxy_url = attachment_object.Attributes["proxy_url"].text_value; }
            if (attachment_object.Attributes.ContainsKey("height"))
            {
                int temp_height;
                if (int.TryParse(attachment_object.Attributes["height"].text_value, out temp_height)) { height = temp_height; }
                else { height = -1; }
            }
            else { height = -1; }
            if (attachment_object.Attributes.ContainsKey("width"))
            {
                int temp_width;
                if (int.TryParse(attachment_object.Attributes["width"].text_value, out temp_width)) { width = temp_width; }
                else { width = -1; }
            }
            else { width = -1; }
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject attachment_object = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (id != null) { attachment_object.AddAttribute("id", id); }
            if (filename != null) { attachment_object.AddAttribute("filename", filename); }
            if (size > -1) { attachment_object.AddAttribute("size", size.ToString(), true); }
            if (url != null) { attachment_object.AddAttribute("url", url); }
            if (proxy_url != null) { attachment_object.AddAttribute("proxy_url", proxy_url); }
            if (height > -1) { attachment_object.AddAttribute("height", height.ToString(), true); }
            if (width > -1) { attachment_object.AddAttribute("width", width.ToString(), true); }
            return attachment_object;
        }
    }
    public class Embed
    {
        public string title { get; set; }
        public string type { get; set; }
        public string description { get; set; }
        public string url { get; set; }
        public DateTime timestamp { get; set; }
        public int color { get; set; }
        public EmbedFooter footer { get; set; }
        public EmbedImage image { get; set; }
        public EmbedThumbnail thumbnail { get; set; }
        public EmbedVideo video { get; set; }
        public EmbedProvider provider { get; set; }
        public EmbedAuthor author { get; set; }
        public List<EmbedField> fields { get; set; }
        public Embed() { }
        public Embed(MajickDiscordWrapper.MajickRegex.JsonObject embed_object)
        {
            if (embed_object.Attributes.ContainsKey("title")) { title = embed_object.Attributes["title"].text_value; }
            if (embed_object.Attributes.ContainsKey("type")) { type = embed_object.Attributes["type"].text_value; }
            if (embed_object.Attributes.ContainsKey("description")) { description = embed_object.Attributes["description"].text_value; }
            if (embed_object.Attributes.ContainsKey("url")) { url = embed_object.Attributes["url"].text_value; }
            if (embed_object.Attributes.ContainsKey("timestamp"))
            {
                DateTime temp_timestamp;
                if (DateTime.TryParse(embed_object.Attributes["timestamp"].text_value, out temp_timestamp)) { timestamp = temp_timestamp; }
            }
            if (embed_object.Attributes.ContainsKey("color"))
            {
                int temp_color;
                if (int.TryParse(embed_object.Attributes["color"].text_value, out temp_color)) { color = temp_color; }
                else { color = -1; }
            }
            else { color = -1; }
            if (embed_object.Objects.Keys.Contains("footer")) { footer = new EmbedFooter(embed_object.Objects["footer"]); }
            if (embed_object.Objects.Keys.Contains("image")) { image = new EmbedImage(embed_object.Objects["image"]); }
            if (embed_object.Objects.Keys.Contains("thumbnail")) { thumbnail = new EmbedThumbnail(embed_object.Objects["thumbnail"]); }
            if (embed_object.Objects.Keys.Contains("video")) { video = new EmbedVideo(embed_object.Objects["video"]); }
            if (embed_object.Objects.Keys.Contains("provider")) { provider = new EmbedProvider(embed_object.Objects["provider"]); }
            if (embed_object.Objects.Keys.Contains("author")) { author = new EmbedAuthor(embed_object.Objects["author"]); }
            fields = new List<EmbedField>();
            if (embed_object.ObjectLists.ContainsKey("fields"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_field in embed_object.ObjectLists["fields"])
                {
                    fields.Add(new EmbedField(current_field));
                }
            }
        }
        public Embed (string new_description)
        {
            description = new_description;
        }
        public Embed(string new_title, List<EmbedField> my_fields)
        {
            title = new_title;
            fields = my_fields;
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject embed_object = new MajickDiscordWrapper.MajickRegex.JsonObject();
            List<MajickDiscordWrapper.MajickRegex.JsonObject> fields_list = new List<MajickDiscordWrapper.MajickRegex.JsonObject>();
            if (title != null) { embed_object.AddAttribute("title", title); }
            if (type != null) { embed_object.AddAttribute("type", type); }
            if (description != null) { embed_object.AddAttribute("description", description); }
            if (url != null) { embed_object.AddAttribute("url", url); }
            //if (timestamp != null) { embed_object.AddAttribute("timestamp", timestamp.ToString(), true); }
            if (color > -1) { embed_object.AddAttribute("color", color.ToString(), true); }
            if (footer != null) { embed_object.AddObject("footer", footer.ToJson()); }
            if (image != null) { embed_object.AddObject("image", image.ToJson()); }
            if (video != null) { embed_object.AddObject("video", video.ToJson()); }
            if (thumbnail != null) { embed_object.AddObject("thumbnail", thumbnail.ToJson()); }
            if (provider != null) { embed_object.AddObject("provider", provider.ToJson()); }
            if (fields != null)
            {
                foreach (EmbedField field in fields) { fields_list.Add(field.ToJson()); }
                embed_object.AddObjectList("fields", fields_list);
            }
            return embed_object;
        }
    }
    public class EmbedFooter
    {
        public string text { get; set; }
        public string icon_url { get; set; }
        public string proxy_icon_url { get; set; }
        public EmbedFooter() { }
        public EmbedFooter(MajickDiscordWrapper.MajickRegex.JsonObject footer_object)
        {
            if (footer_object.Attributes.ContainsKey("text")) { text = footer_object.Attributes["text"].text_value; }
            if (footer_object.Attributes.ContainsKey("icon_url")) { icon_url = footer_object.Attributes["icon_url"].text_value; }
            if (footer_object.Attributes.ContainsKey("proxy_icon_url")) { proxy_icon_url = footer_object.Attributes["proxy_icon_url"].text_value; }
        }
        public EmbedFooter(string footer_text)
        {
            text = footer_text;
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject embed_thumbnail = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (text != null) { embed_thumbnail.AddAttribute("text", text); }
            if (icon_url != null) { embed_thumbnail.AddAttribute("icon_url", icon_url); }
            if (proxy_icon_url != null) { embed_thumbnail.AddAttribute("proxy_icon_url", proxy_icon_url); }
            return embed_thumbnail;
        }
    }
    public class EmbedImage
    {
        public string url { get; set; }
        public string proxy_url { get; set; }
        public int height { get; set; }
        public int width { get; set; }
        public EmbedImage() { }
        public EmbedImage(MajickDiscordWrapper.MajickRegex.JsonObject image_object)
        {
            if (image_object.Attributes.ContainsKey("url")) { url = image_object.Attributes["url"].text_value; }
            if (image_object.Attributes.ContainsKey("proxy_url")) { proxy_url = image_object.Attributes["proxy_url"].text_value; }
            if (image_object.Attributes.ContainsKey("height"))
            {
                int temp_height;
                if (int.TryParse(image_object.Attributes["height"].text_value, out temp_height)) { height = temp_height; }
                else { height = -1; }
            }
            else { height = -1; }
            if (image_object.Attributes.ContainsKey("width"))
            {
                int temp_width;
                if (int.TryParse(image_object.Attributes["width"].text_value, out temp_width)) { width = temp_width; }
                else { width = -1; }
            }
            else { width = -1; }
        }
        public EmbedImage(string new_url)
        {
            url = new_url;
            proxy_url = "";
            height = 100;
            width = 100;
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject embed_image = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (url != null) { embed_image.AddAttribute("url", url); }
            if (proxy_url != null) { embed_image.AddAttribute("proxy_url", proxy_url); }
            if (height > -1) { embed_image.AddAttribute("height", height.ToString(), true); }
            if (width > -1) { embed_image.AddAttribute("width", width.ToString(), true); }
            return embed_image;
        }
    }
    public class EmbedVideo
    {
        public string url { get; set; }
        public int height { get; set; }
        public int width { get; set; }
        public EmbedVideo() { }
        public EmbedVideo(MajickDiscordWrapper.MajickRegex.JsonObject video_object)
        {
            if (video_object.Attributes.ContainsKey("url")) { url = video_object.Attributes["url"].text_value; }
            if (video_object.Attributes.ContainsKey("height"))
            {
                int temp_height;
                if (int.TryParse(video_object.Attributes["height"].text_value, out temp_height)) { height = temp_height; }
                else { height = -1; }
            }
            else { height = -1; }
            if (video_object.Attributes.ContainsKey("width"))
            {
                int temp_width;
                if (int.TryParse(video_object.Attributes["width"].text_value, out temp_width)) { width = temp_width; }
                else { width = -1; }
            }
            else { width = -1; }
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject embed_video = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (url != null) { embed_video.AddAttribute("url", url); }
            if (height > -1) { embed_video.AddAttribute("height", height.ToString(), true); }
            if (width > -1) { embed_video.AddAttribute("width", width.ToString(), true); }
            return embed_video;
        }
    }
    public class EmbedThumbnail
    {
        public string url { get; set; }
        public string proxy_url { get; set; }
        public int height { get; set; }
        public int width { get; set; }
        public EmbedThumbnail() { }
        public EmbedThumbnail(MajickDiscordWrapper.MajickRegex.JsonObject thumbnail_object)
        {            
            if (thumbnail_object.Attributes.ContainsKey("url")) { url = thumbnail_object.Attributes["url"].text_value; }
            if (thumbnail_object.Attributes.ContainsKey("proxy_url")) { proxy_url = thumbnail_object.Attributes["proxy_url"].text_value; }
            if (thumbnail_object.Attributes.ContainsKey("height"))
            {
                int temp_height;
                if (int.TryParse(thumbnail_object.Attributes["height"].text_value, out temp_height)) { height = temp_height; }
                else { height = -1; }
            }
            else { height = -1; }
            if (thumbnail_object.Attributes.ContainsKey("width"))
            {
                int temp_width;
                if (int.TryParse(thumbnail_object.Attributes["width"].text_value, out temp_width)) { width = temp_width; }
                else { width = -1; }
            }
            else { width = -1; }
        }
        public EmbedThumbnail(string new_url, int new_height = 100, int new_width = 100)
        {
            url = new_url;
            proxy_url = "";
            height = new_height;
            width = new_width;
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject embed_thumbnail = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (url != null) { embed_thumbnail.AddAttribute("url", url); }
            if (proxy_url != null) { embed_thumbnail.AddAttribute("proxy_url", proxy_url); }
            if (height > -1) { embed_thumbnail.AddAttribute("height", height.ToString(), true); }
            if (width > -1) { embed_thumbnail.AddAttribute("width", width.ToString(), true); }
            return embed_thumbnail;
        }
    }
    public class EmbedProvider
    {
        public string name { get; set; }
        public string url { get; set; }
        public EmbedProvider() { }
        public EmbedProvider(MajickDiscordWrapper.MajickRegex.JsonObject provider_object)
        {
            if (provider_object.Attributes.ContainsKey("name")) { name = provider_object.Attributes["name"].text_value; }
            if (provider_object.Attributes.ContainsKey("url")) { url = provider_object.Attributes["url"].text_value; }
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject embed_provider = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (name != null) { embed_provider.AddAttribute("name", name); }
            if (url != null) { embed_provider.AddAttribute("url", url); }
            return embed_provider;
        }
    }
    public class EmbedAuthor
    {
        public string name { get; set; }
        public string url { get; set; }
        public string icon_url { get; set; }
        public string proxy_icon_url { get; set; }
        public EmbedAuthor() { }
        public EmbedAuthor(MajickDiscordWrapper.MajickRegex.JsonObject author_object)
        {
            if (author_object.Attributes.ContainsKey("name")) { name = author_object.Attributes["name"].text_value; }
            if (author_object.Attributes.ContainsKey("url")) { url = author_object.Attributes["url"].text_value; }
            if (author_object.Attributes.ContainsKey("icon_url")) { icon_url = author_object.Attributes["icon_url"].text_value; }
            if (author_object.Attributes.ContainsKey("proxy_icon_url")) { proxy_icon_url = author_object.Attributes["proxy_icon_url"].text_value; }
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject embed_author = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (name != null) { embed_author.AddAttribute("name", name); }
            if (url != null) { embed_author.AddAttribute("url", url); }
            if (icon_url != null) { embed_author.AddAttribute("icon_url", icon_url); }
            if (proxy_icon_url != null) { embed_author.AddAttribute("proxy_icon_url", proxy_icon_url); }
            return embed_author;
        }
    }
    public class EmbedField
    {
        public string name { get; set; }
        public string value { get; set; }
        public bool inline { get; set; }
        public EmbedField() { }
        public EmbedField(MajickDiscordWrapper.MajickRegex.JsonObject field_object)
        {
            if (field_object.Attributes.ContainsKey("name")) { name = field_object.Attributes["name"].text_value; }
            if (field_object.Attributes.ContainsKey("value")) { value = field_object.Attributes["value"].text_value; }
            if (field_object.Attributes.ContainsKey("inline"))
            {
                bool temp_inline;
                if (bool.TryParse(field_object.Attributes["inline"].text_value, out temp_inline)) { inline = temp_inline; }
                else { inline = true && false; }
            }
            else { inline = true && false; }
        }
        public EmbedField(string new_name, string new_value, bool is_inline)
        {
            name = new_name;
            value = new_value;
            inline = is_inline;
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject embed_field = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (name != null) { embed_field.AddAttribute("name", name); }
            if (value != null) { embed_field.AddAttribute("value", value); }
            if (inline != (true && false)) { embed_field.AddAttribute("inline", inline.ToString(), true); }
            return embed_field;
        }
    }
    public class DiscordReaction
    {
        public int count { get; set; }
        public bool me { get; set; }
        public DiscordEmoji emoji { get; set; }
        public DiscordReaction() { }
        public DiscordReaction(DiscordEmoji new_emoji)
        {
            count = -1;
            me = true && false;
            emoji = new_emoji;
        }
        public DiscordReaction(MajickDiscordWrapper.MajickRegex.JsonObject reaction_object)
        {
            if (reaction_object.Attributes.ContainsKey("count"))
            {
                int temp_count;
                if (int.TryParse(reaction_object.Attributes["count"].text_value, out temp_count)) { count = temp_count; }
                else { count = -1; }
            }
            else { count = -1; }
            if (reaction_object.Attributes.ContainsKey("me"))
            {
                bool was_me = false;
                if (bool.TryParse(reaction_object.Attributes["me"].text_value, out was_me)) { me = was_me; }
                else { me = true && false; }
            }
            else { me = true && false; }
            if (reaction_object.Objects.Keys.Contains("emoji")) { emoji = new DiscordEmoji(reaction_object.Objects["emoji"]); }
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject reaction_object = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (count > -1) { reaction_object.AddAttribute("count", count.ToString(), true); }
            if (me != (true && false)) { reaction_object.AddAttribute("me", me.ToString(), true); }
            if (emoji != null) { reaction_object.AddObject("emoji", emoji.ToJson()); }
            return reaction_object;
        }
    }
    public class DiscordMessageActivity
    {
        public MessageActivityType type { get; set; }
        public string party_id { get; set; }
        public DiscordMessageActivity() { }
        public DiscordMessageActivity(MajickDiscordWrapper.MajickRegex.JsonObject msg_activity_object)
        {
            if (msg_activity_object.Attributes.ContainsKey("party_id")) { party_id = msg_activity_object.Attributes["party_id"].text_value; }
            if (msg_activity_object.Attributes.ContainsKey("type"))
            {
                MessageActivityType temp_type;
                if (Enum.TryParse(msg_activity_object.Attributes["type"].text_value, out temp_type)) { type = temp_type; }
                else { type = MessageActivityType.UNSPECIFIED; }
            }
            else { type = MessageActivityType.UNSPECIFIED; }
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject message_activity = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (type != MessageActivityType.UNSPECIFIED) { message_activity.AddAttribute("type", ((int)type).ToString(), true); }
            if (party_id != null) { message_activity.AddAttribute("party_id", party_id); }
            return message_activity;
        }
    }
    public class DiscordApplication
    {
        public string id { get; set; }
        public string name { get; set; }
        public string icon { get; set; }
        public string description { get; set; }
        public List<string> rpc_origin { get; set; }
        public bool bot_public { get; set; }
        public bool bot_require_code_grant { get; set; }
        public string terms_of_service_url { get; set; }
        public string privacy_policy_url { get; set; }
        public DiscordUser owner { get; set; }
        public string summary { get; set; }
        public string verify_key { get; set; }
        public DiscordTeam team { get; set; }
        public string guild_id { get; set; }
        public string primary_sku_id { get; set; }
        public string slug { get; set; }
        public string cover_image { get; set; }
        public int flags { get; set; }
        public DiscordApplication() { }
        public DiscordApplication(MajickDiscordWrapper.MajickRegex.JsonObject msg_application_object)
        {
            if (msg_application_object.Attributes.ContainsKey("id")) { id = msg_application_object.Attributes["id"].text_value; }
            if (msg_application_object.Attributes.ContainsKey("name")) { name = msg_application_object.Attributes["name"].text_value; }
            if (msg_application_object.Attributes.ContainsKey("icon")) { icon = msg_application_object.Attributes["icon"].text_value; }
            if (msg_application_object.Attributes.ContainsKey("description")) { description = msg_application_object.Attributes["description"].text_value; }
            rpc_origin = new List<string>();
            if (msg_application_object.AttributeLists.ContainsKey("rpc_origin"))
            {
                foreach (JsonAttribute new_origin in msg_application_object.AttributeLists["rpc_origin"]) { rpc_origin.Add(new_origin.text_value); }
            }
            if (msg_application_object.Attributes.ContainsKey("terms_of_service_url")) { terms_of_service_url = msg_application_object.Attributes["terms_of_service_url"].text_value; }
            if (msg_application_object.Attributes.ContainsKey("privacy_policy_url")) { privacy_policy_url = msg_application_object.Attributes["privacy_policy_url"].text_value; }
            if (msg_application_object.Objects.Keys.Contains("owner")) { owner = new DiscordUser(msg_application_object.Objects["owner"]); }
            if (msg_application_object.Attributes.ContainsKey("summary")) { summary = msg_application_object.Attributes["summary"].text_value; }
            if (msg_application_object.Attributes.ContainsKey("verify_key")) { verify_key = msg_application_object.Attributes["verify_key"].text_value; }
            if (msg_application_object.Objects.Keys.Contains("team")) { team = new DiscordTeam(msg_application_object.Objects["team"]); }
            if (msg_application_object.Attributes.ContainsKey("guild_id")) { guild_id = msg_application_object.Attributes["guild_id"].text_value; }
            if (msg_application_object.Attributes.ContainsKey("primary_sku_id")) { primary_sku_id = msg_application_object.Attributes["primary_sku_id"].text_value; }
            if (msg_application_object.Attributes.ContainsKey("slug")) { slug = msg_application_object.Attributes["slug"].text_value; }
            if (msg_application_object.Attributes.ContainsKey("cover_image")) { cover_image = msg_application_object.Attributes["cover_image"].text_value; }
            if (msg_application_object.Attributes.ContainsKey("flags"))
            {
                int temp_flags;
                if (int.TryParse(msg_application_object.Attributes["flags"].text_value, out temp_flags)) { flags = temp_flags; }
            }
        }
        //public List<ApplicationCommand> GetGlobalCommands(string bot_token)
        //{
        //    List<ApplicationCommand> global_commands = new List<ApplicationCommand>();
        //    RestClient rcCommandClient;
        //    RestRequest rrCommandRequest;
        //    RestResponse rsCommandResponse;
        //    MajickDiscordWrapper.MajickRegex.JsonObject CommandRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
        //    MajickDiscordWrapper.MajickRegex.JsonObject CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
        //    Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
        //    rcCommandClient = new RestClient("https://discord.com/api");
        //    rrCommandRequest = new RestRequest("/applications/" + id + "/commands" , Method.Get);
        //    rrCommandRequest.RequestFormat = DataFormat.Json;
        //    rrCommandRequest.AddHeader("Content-Type", "application/json");
        //    rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
        //    rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
        //    CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsCommandResponse.Content);
        //    foreach(MajickRegex.JsonObject current_command in CommandResponseContent.ObjectLists["objects"])
        //    {
        //        global_commands.Add(new ApplicationCommand(current_command));
        //    }
        //    return global_commands;
        //}
        //public List<ApplicationCommand> OverwriteGlobalCommands(string bot_token, MajickRegex.JsonObject new_commands)
        //{
        //    List<ApplicationCommand> global_commands = new List<ApplicationCommand>();
        //    RestClient rcCommandClient;
        //    RestRequest rrCommandRequest;
        //    RestResponse rsCommandResponse;
        //    MajickDiscordWrapper.MajickRegex.JsonObject CommandRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
        //    MajickDiscordWrapper.MajickRegex.JsonObject CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
        //    Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
        //    rcCommandClient = new RestClient("https://discord.com/api");
        //    rrCommandRequest = new RestRequest("/applications/" + id + "/commands", Method.Put);
        //    rrCommandRequest.RequestFormat = DataFormat.Json;
        //    rrCommandRequest.AddHeader("Content-Type", "application/json");
        //    rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
        //    rrCommandRequest.AddJsonBody(new_commands.ToRawText());
        //    rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
        //    CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsCommandResponse.Content);
        //    foreach (MajickRegex.JsonObject current_command in CommandResponseContent.ObjectLists["objects"])
        //    {
        //        global_commands.Add(new ApplicationCommand(current_command));
        //    }
        //    return global_commands;
        //}
        //public ApplicationCommand CreateGlobalCommand(string bot_token, MajickRegex.JsonObject new_command)
        //{
        //    ApplicationCommand CreatedCommand = new ApplicationCommand();
        //    RestClient rcCommandClient;
        //    RestRequest rrCommandRequest;
        //    RestResponse rsCommandResponse;
        //    MajickDiscordWrapper.MajickRegex.JsonObject CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
        //    Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
        //    rcCommandClient = new RestClient("https://discord.com/api");
        //    rrCommandRequest = new RestRequest("/applications/" + id + "/commands", Method.Post);
        //    rrCommandRequest.RequestFormat = DataFormat.Json;
        //    rrCommandRequest.AddHeader("Content-Type", "application/json");
        //    rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
        //    rrCommandRequest.AddJsonBody(new_command.ToRawText());
        //    rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
        //    CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsCommandResponse.Content);
        //    CreatedCommand = new ApplicationCommand(CommandResponseContent);
        //    return CreatedCommand;
        //}
        //public ApplicationCommand GetGlobalCommand(string bot_token, string command_id)
        //{
        //    ApplicationCommand RequestedCommand = new ApplicationCommand();
        //    RestClient rcCommandClient;
        //    RestRequest rrCommandRequest;
        //    RestResponse rsCommandResponse;
        //    MajickDiscordWrapper.MajickRegex.JsonObject CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
        //    Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
        //    rcCommandClient = new RestClient("https://discord.com/api");
        //    rrCommandRequest = new RestRequest("/applications/" + id + "/commands/" + command_id, Method.Get);
        //    rrCommandRequest.RequestFormat = DataFormat.Json;
        //    rrCommandRequest.AddHeader("Content-Type", "application/json");
        //    rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
        //    rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
        //    CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsCommandResponse.Content);
        //    RequestedCommand = new ApplicationCommand(CommandResponseContent);
        //    return RequestedCommand;
        //}
        //public ApplicationCommand UpdateGlobalCommand(string bot_token, string command_id, MajickRegex.JsonObject new_command)
        //{
        //    ApplicationCommand UpdatedCommand = new ApplicationCommand();
        //    RestClient rcCommandClient;
        //    RestRequest rrCommandRequest;
        //    RestResponse rsCommandResponse;
        //    MajickDiscordWrapper.MajickRegex.JsonObject CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
        //    Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
        //    rcCommandClient = new RestClient("https://discord.com/api");
        //    rrCommandRequest = new RestRequest("/applications/" + id + "/commands/" + command_id, Method.Patch);
        //    rrCommandRequest.RequestFormat = DataFormat.Json;
        //    rrCommandRequest.AddHeader("Content-Type", "application/json");
        //    rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
        //    rrCommandRequest.AddJsonBody(new_command.ToRawText());
        //    rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
        //    CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsCommandResponse.Content);
        //    UpdatedCommand = new ApplicationCommand(CommandResponseContent);
        //    return UpdatedCommand;
        //}
        //public bool DeleteGlobalCommand(string bot_token, string command_id)
        //{
        //    ApplicationCommand CreatedCommand = new ApplicationCommand();
        //    RestClient rcCommandClient;
        //    RestRequest rrCommandRequest;
        //    RestResponse rsCommandResponse;
        //    MajickDiscordWrapper.MajickRegex.JsonObject CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
        //    Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
        //    rcCommandClient = new RestClient("https://discord.com/api");
        //    rrCommandRequest = new RestRequest("/applications/" + id + "/commands/" + command_id, Method.Delete);
        //    rrCommandRequest.RequestFormat = DataFormat.Json;
        //    rrCommandRequest.AddHeader("Content-Type", "application/json");
        //    rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
        //    rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
        //    return rsCommandResponse.IsSuccessful;
        //}
        //public List<ApplicationCommand> GetGuildCommands(string bot_token, string guild_id)
        //{
        //    List<ApplicationCommand> guild_commands = new List<ApplicationCommand>();
        //    RestClient rcCommandClient;
        //    RestRequest rrCommandRequest;
        //    RestResponse rsCommandResponse;
        //    MajickDiscordWrapper.MajickRegex.JsonObject CommandRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
        //    MajickDiscordWrapper.MajickRegex.JsonObject CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
        //    Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
        //    rcCommandClient = new RestClient("https://discord.com/api");
        //    rrCommandRequest = new RestRequest("/applications/" + id + "/guilds/" + guild_id + "/commands", Method.Get);
        //    rrCommandRequest.RequestFormat = DataFormat.Json;
        //    rrCommandRequest.AddHeader("Content-Type", "application/json");
        //    rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
        //    rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
        //    CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsCommandResponse.Content);
        //    foreach (MajickRegex.JsonObject current_command in CommandResponseContent.ObjectLists["objects"])
        //    {
        //        guild_commands.Add(new ApplicationCommand(current_command));
        //    }
        //    return guild_commands;
        //}
        //public List<GuildApplicationCommandPermissions> GetAllGuildCommandPermissions(string bot_token, string guild_id)
        //{
        //    List<GuildApplicationCommandPermissions> guild_command_permissions = new List<GuildApplicationCommandPermissions>();
        //    RestClient rcCommandClient;
        //    RestRequest rrCommandRequest;
        //    RestResponse rsCommandResponse;
        //    MajickDiscordWrapper.MajickRegex.JsonObject CommandRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
        //    MajickDiscordWrapper.MajickRegex.JsonObject CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
        //    Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
        //    rcCommandClient = new RestClient("https://discord.com/api");
        //    rrCommandRequest = new RestRequest("/applications/" + id + "/guilds/" + guild_id + "/commands/permissions", Method.Get);
        //    rrCommandRequest.RequestFormat = DataFormat.Json;
        //    rrCommandRequest.AddHeader("Content-Type", "application/json");
        //    rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
        //    rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
        //    CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsCommandResponse.Content);
        //    foreach (MajickRegex.JsonObject current_permission in CommandResponseContent.ObjectLists["objects"])
        //    {
        //        guild_command_permissions.Add(new GuildApplicationCommandPermissions(current_permission));
        //    }
        //    return guild_command_permissions;
        //}
        //public List<GuildApplicationCommandPermissions> GetGuildCommandPermissions(string bot_token, string guild_id, string command_id)
        //{
        //    List<GuildApplicationCommandPermissions> guild_command_permissions = new List<GuildApplicationCommandPermissions>();
        //    RestClient rcCommandClient;
        //    RestRequest rrCommandRequest;
        //    RestResponse rsCommandResponse;
        //    MajickDiscordWrapper.MajickRegex.JsonObject CommandRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
        //    MajickDiscordWrapper.MajickRegex.JsonObject CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
        //    Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
        //    rcCommandClient = new RestClient("https://discord.com/api");
        //    rrCommandRequest = new RestRequest("/applications/" + id + "/guilds/" + guild_id + "/commands/" + command_id + "/permissions", Method.Get);
        //    rrCommandRequest.RequestFormat = DataFormat.Json;
        //    rrCommandRequest.AddHeader("Content-Type", "application/json");
        //    rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
        //    rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
        //    CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsCommandResponse.Content);
        //    foreach (MajickRegex.JsonObject current_permission in CommandResponseContent.ObjectLists["objects"])
        //    {
        //        guild_command_permissions.Add(new GuildApplicationCommandPermissions(current_permission));
        //    }
        //    return guild_command_permissions;
        //}
        //public List<GuildApplicationCommandPermissions> UpdateGuildCommandPermissions(string bot_token, string guild_id, string command_id, MajickRegex.JsonObject new_permissions)
        //{
        //    List<GuildApplicationCommandPermissions> guild_command_permissions = new List<GuildApplicationCommandPermissions>();
        //    RestClient rcCommandClient;
        //    RestRequest rrCommandRequest;
        //    RestResponse rsCommandResponse;
        //    MajickDiscordWrapper.MajickRegex.JsonObject CommandRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
        //    MajickDiscordWrapper.MajickRegex.JsonObject CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
        //    Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
        //    rcCommandClient = new RestClient("https://discord.com/api");
        //    rrCommandRequest = new RestRequest("/applications/" + id + "/guilds/" + guild_id + "/commands/" + command_id + "/permissions", Method.Put);
        //    rrCommandRequest.RequestFormat = DataFormat.Json;
        //    rrCommandRequest.AddHeader("Content-Type", "application/json");
        //    rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
        //    rrCommandRequest.AddJsonBody(new_permissions.ToRawText());
        //    rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
        //    CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsCommandResponse.Content);
        //    foreach (MajickRegex.JsonObject current_permission in CommandResponseContent.ObjectLists["objects"])
        //    {
        //        guild_command_permissions.Add(new GuildApplicationCommandPermissions(current_permission));
        //    }
        //    return guild_command_permissions;
        //}
        //public List<GuildApplicationCommandPermissions> UpdateAllGuildCommandPermissions(string bot_token, string guild_id, string command_id, MajickRegex.JsonObject new_permissions)
        //{
        //    List<GuildApplicationCommandPermissions> guild_command_permissions = new List<GuildApplicationCommandPermissions>();
        //    RestClient rcCommandClient;
        //    RestRequest rrCommandRequest;
        //    RestResponse rsCommandResponse;
        //    MajickDiscordWrapper.MajickRegex.JsonObject CommandRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
        //    MajickDiscordWrapper.MajickRegex.JsonObject CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
        //    Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
        //    rcCommandClient = new RestClient("https://discord.com/api");
        //    rrCommandRequest = new RestRequest("/applications/" + id + "/guilds/" + guild_id + "/commands/permissions", Method.Put);
        //    rrCommandRequest.RequestFormat = DataFormat.Json;
        //    rrCommandRequest.AddHeader("Content-Type", "application/json");
        //    rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
        //    rrCommandRequest.AddJsonBody(new_permissions.ToRawText());
        //    rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
        //    CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsCommandResponse.Content);
        //    foreach (MajickRegex.JsonObject current_permission in CommandResponseContent.ObjectLists["objects"])
        //    {
        //        guild_command_permissions.Add(new GuildApplicationCommandPermissions(current_permission));
        //    }
        //    return guild_command_permissions;
        //}
        //public List<ApplicationCommand> OverwriteGuildCommands(string bot_token, string guild_id, MajickRegex.JsonObject new_commands)
        //{
        //    List<ApplicationCommand> guild_commands = new List<ApplicationCommand>();
        //    RestClient rcCommandClient;
        //    RestRequest rrCommandRequest;
        //    RestResponse rsCommandResponse;
        //    MajickDiscordWrapper.MajickRegex.JsonObject CommandRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
        //    MajickDiscordWrapper.MajickRegex.JsonObject CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
        //    Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
        //    rcCommandClient = new RestClient("https://discord.com/api");
        //    rrCommandRequest = new RestRequest("/applications/" + id + "/guilds/" + guild_id + "/commands", Method.Put);
        //    rrCommandRequest.RequestFormat = DataFormat.Json;
        //    rrCommandRequest.AddHeader("Content-Type", "application/json");
        //    rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
        //    rrCommandRequest.AddJsonBody(new_commands.ToRawText());
        //    rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
        //    CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsCommandResponse.Content);
        //    foreach (MajickRegex.JsonObject current_command in CommandResponseContent.ObjectLists["objects"])
        //    {
        //        guild_commands.Add(new ApplicationCommand(current_command));
        //    }
        //    return guild_commands;
        //}
        //public ApplicationCommand CreateGuildCommand(string bot_token, string guild_id, MajickRegex.JsonObject new_command)
        //{
        //    ApplicationCommand CreatedCommand = new ApplicationCommand();
        //    RestClient rcCommandClient;
        //    RestRequest rrCommandRequest;
        //    RestResponse rsCommandResponse;
        //    MajickDiscordWrapper.MajickRegex.JsonObject CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
        //    Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
        //    rcCommandClient = new RestClient("https://discord.com/api");
        //    rrCommandRequest = new RestRequest("/applications/" + id + "/guilds/" + guild_id + "/commands", Method.Post);
        //    rrCommandRequest.RequestFormat = DataFormat.Json;
        //    rrCommandRequest.AddHeader("Content-Type", "application/json");
        //    rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
        //    rrCommandRequest.AddJsonBody(new_command.ToRawText());
        //    rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
        //    CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsCommandResponse.Content);
        //    CreatedCommand = new ApplicationCommand(CommandResponseContent);
        //    return CreatedCommand;
        //}
        //public ApplicationCommand GetGuildCommand(string bot_token, string guild_id, string command_id)
        //{
        //    ApplicationCommand RequestedCommand = new ApplicationCommand();
        //    RestClient rcCommandClient;
        //    RestRequest rrCommandRequest;
        //    RestResponse rsCommandResponse;
        //    MajickDiscordWrapper.MajickRegex.JsonObject CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
        //    Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
        //    rcCommandClient = new RestClient("https://discord.com/api");
        //    rrCommandRequest = new RestRequest("/applications/" + id + "/guilds/" + guild_id + "/commands/" + command_id, Method.Get);
        //    rrCommandRequest.RequestFormat = DataFormat.Json;
        //    rrCommandRequest.AddHeader("Content-Type", "application/json");
        //    rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
        //    rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
        //    CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsCommandResponse.Content);
        //    RequestedCommand = new ApplicationCommand(CommandResponseContent);
        //    return RequestedCommand;
        //}
        //public ApplicationCommand UpdateGuildCommand(string bot_token, string guild_id, string command_id, MajickRegex.JsonObject new_command)
        //{
        //    ApplicationCommand UpdatedCommand = new ApplicationCommand();
        //    RestClient rcCommandClient;
        //    RestRequest rrCommandRequest;
        //    RestResponse rsCommandResponse;
        //    MajickDiscordWrapper.MajickRegex.JsonObject CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
        //    Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
        //    rcCommandClient = new RestClient("https://discord.com/api");
        //    rrCommandRequest = new RestRequest("/applications/" + id + "/guilds/" + guild_id + "/commands/" + command_id, Method.Patch);
        //    rrCommandRequest.RequestFormat = DataFormat.Json;
        //    rrCommandRequest.AddHeader("Content-Type", "application/json");
        //    rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
        //    rrCommandRequest.AddJsonBody(new_command.ToRawText());
        //    rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
        //    CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsCommandResponse.Content);
        //    UpdatedCommand = new ApplicationCommand(CommandResponseContent);
        //    return UpdatedCommand;
        //}
        //public bool DeleteGuildCommand(string bot_token, string guild_id, string command_id)
        //{
        //    ApplicationCommand CreatedCommand = new ApplicationCommand();
        //    RestClient rcCommandClient;
        //    RestRequest rrCommandRequest;
        //    RestResponse rsCommandResponse;
        //    MajickDiscordWrapper.MajickRegex.JsonObject CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
        //    Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
        //    rcCommandClient = new RestClient("https://discord.com/api");
        //    rrCommandRequest = new RestRequest("/applications/" + id + "/guilds/" + guild_id + "/commands/" + command_id, Method.Delete);
        //    rrCommandRequest.RequestFormat = DataFormat.Json;
        //    rrCommandRequest.AddHeader("Content-Type", "application/json");
        //    rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
        //    rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
        //    return rsCommandResponse.IsSuccessful;
        //}
        public List<DiscordApplicationFlags> GetNamedApplicationFlags(int perm_value)
        {
            List<DiscordApplicationFlags> my_flags = new List<DiscordApplicationFlags>();
            if (perm_value >= (int)DiscordApplicationFlags.EMBEDDED)
            {
                my_flags.Add(DiscordApplicationFlags.EMBEDDED);
                perm_value -= (int)DiscordApplicationFlags.EMBEDDED;
            }
            if (perm_value >= (int)DiscordApplicationFlags.VERIFICATION_PENDING_GUILD_LIMIT)
            {
                my_flags.Add(DiscordApplicationFlags.VERIFICATION_PENDING_GUILD_LIMIT);
                perm_value -= (int)DiscordApplicationFlags.VERIFICATION_PENDING_GUILD_LIMIT;
            }
            if (perm_value >= (int)DiscordApplicationFlags.GATEWAY_GUILD_MEMBERS_LIMITED)
            {
                my_flags.Add(DiscordApplicationFlags.GATEWAY_GUILD_MEMBERS_LIMITED);
                perm_value -= (int)DiscordApplicationFlags.GATEWAY_GUILD_MEMBERS_LIMITED;
            }
            if (perm_value >= (int)DiscordApplicationFlags.GATEWAY_GUILD_MEMBERS)
            {
                my_flags.Add(DiscordApplicationFlags.GATEWAY_GUILD_MEMBERS);
                perm_value -= (int)DiscordApplicationFlags.GATEWAY_GUILD_MEMBERS;
            }
            if (perm_value >= (int)DiscordApplicationFlags.GATEWAY_PRESENCE_LIMITED)
            {
                my_flags.Add(DiscordApplicationFlags.GATEWAY_PRESENCE_LIMITED);
                perm_value -= (int)DiscordApplicationFlags.GATEWAY_PRESENCE_LIMITED;
            }
            if (perm_value >= (int)DiscordApplicationFlags.GATEWAY_PRESENCE)
            {
                my_flags.Add(DiscordApplicationFlags.GATEWAY_PRESENCE);
                perm_value -= (int)DiscordApplicationFlags.GATEWAY_PRESENCE;
            }
            return my_flags;
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject message_app_object = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (id != null) { message_app_object.AddAttribute("id", id); }
            if (cover_image != null) { message_app_object.AddAttribute("cover_image", cover_image); }
            if (description != null) { message_app_object.AddAttribute("description", description); }
            if (icon != null) { message_app_object.AddAttribute("icon", icon); }
            if (name != null) { message_app_object.AddAttribute("name", name); }
            return message_app_object;
        }
    }
    public class DiscordTeam
    {
        public string icon { get; set; }
        public List<DiscordTeamMember> members { get; set; }
        public string name { get; set; }
        public string owner_user_id { get; set; }
        public DiscordTeam() { }
        public DiscordTeam(MajickRegex.JsonObject new_team) 
        {
            if (new_team.Attributes.ContainsKey("icon")) { icon = new_team.Attributes["icon"].text_value; }
            members = new List<DiscordTeamMember>();
            if (new_team.ObjectLists.ContainsKey("members"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_team_member in new_team.ObjectLists["members"])
                {
                    members.Add(new DiscordTeamMember(current_team_member));
                }
            }
            if (new_team.Attributes.ContainsKey("name")) { name = new_team.Attributes["name"].text_value; }
            if (new_team.Attributes.ContainsKey("owner_user_id")) { owner_user_id = new_team.Attributes["owner_user_id"].text_value; }
        }
    }
    public class DiscordTeamMember
    {
        public DiscordTeamMembershipState membership_state { get; set; }
        public List<string> permissions { get; set; }
        public string team_id { get; set; }
        public DiscordUser user { get; set; }
        public DiscordTeamMember() { }
        public DiscordTeamMember(MajickRegex.JsonObject new_team_member) 
        {
            if (new_team_member.Attributes.ContainsKey("type"))
            {
                DiscordTeamMembershipState temp_membership_state;
                if (Enum.TryParse(new_team_member.Attributes["membership_state"].text_value, out temp_membership_state)) { membership_state = temp_membership_state; }
            }
            permissions = new List<string>();
            if (new_team_member.AttributeLists.ContainsKey("permissions"))
            {
                foreach (JsonAttribute new_permission in new_team_member.AttributeLists["permissions"]) { permissions.Add(new_permission.text_value); }
            }
            if (new_team_member.Attributes.ContainsKey("team_id")) { team_id = new_team_member.Attributes["team_id"].text_value; }
            if (new_team_member.Objects.Keys.Contains("user")) { user = new DiscordUser(new_team_member.Objects["user"]); }
        }
    }
    public class DiscordInvite
    {
        public string code { get; set; }
        public string guild_id { get; set; }
        public DiscordGuild guild { get; set; }
        public string channel_id { get; set; }
        public DiscordChannel channel { get; set; }
        public int approximate_presence_count { get; set; }
        public int approximate_member_count { get; set; }
        public InviteMetadata metadata { get; set; }
        public DiscordInvite() { }
        public DiscordInvite(MajickDiscordWrapper.MajickRegex.JsonObject invite_object)
        {
            int temp_presence_count;
            int temp_member_count;
            if (invite_object.Attributes.ContainsKey("code")) { code = invite_object.Attributes["code"].text_value; }
            if (invite_object.Objects.Keys.Contains("guild")) { guild = new DiscordGuild(invite_object.Objects["guild"]); }
            if (invite_object.Objects.Keys.Contains("channel")) { channel = new DiscordChannel(invite_object.Objects["channel"]); }
            if (invite_object.Attributes.ContainsKey("approximate_presence_count"))
            {
                if (int.TryParse(invite_object.Attributes["approximate_presence_count"].text_value, out temp_presence_count)) { approximate_presence_count = temp_presence_count; }
                else { approximate_presence_count = -1; }
            }
            else { approximate_presence_count = -1; }
            if (invite_object.Attributes.ContainsKey("approximate_member_count"))
            {
                if (int.TryParse(invite_object.Attributes["approximate_member_count"].text_value, out temp_member_count)) { approximate_member_count = temp_member_count; }
                else { approximate_member_count = -1; }
            }
            else { approximate_member_count = -1; }
            if (invite_object.Objects.Keys.Contains("metadata")) { metadata = new InviteMetadata(invite_object.Objects["metadata"]); }
        }
        public DiscordInvite(string new_code, string new_guild_id, string new_channel_id, InviteMetadata new_meta)
        {
            code = new_code;
            guild_id = new_guild_id;
            channel_id = new_channel_id;
            metadata = new_meta;
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject invite_object = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (code != null) { invite_object.AddAttribute("code", code); }
            if (guild != null) { invite_object.AddObject("guild", guild.ToJson()); }
            if (channel != null) { invite_object.AddObject("channel", channel.ToJson()); }
            if (approximate_presence_count > -1) { invite_object.AddAttribute("approximate_presence_count", approximate_presence_count.ToString(), true); }
            if (approximate_member_count > -1) { invite_object.AddAttribute("approximate_member_count", approximate_member_count.ToString(), true); }
            if (metadata != null) { invite_object.AddObject("metadata", metadata.ToJson()); }
            return invite_object;
        }
    }
    public class InviteMetadata
    {
        public DiscordUser inviter { get; set; }
        public int uses { get; set; }
        public int max_uses { get; set; }
        public int max_age { get; set; }
        public bool temporary { get; set; }
        public DateTime created_at { get; set; }
        public bool revoked { get; set; }
        public InviteMetadata() { }
        public InviteMetadata(MajickDiscordWrapper.MajickRegex.JsonObject metadata_object)
        {
            int temp_uses;
            int temp_max_uses;
            int temp_max_age;
            bool is_temporary;
            DateTime when_created;
            bool is_revoked;
            if (metadata_object.Objects.Keys.Contains("inviter")) { inviter = new DiscordUser(metadata_object.Objects["inviter"]); }
            if (metadata_object.Attributes.ContainsKey("uses"))
            {
                if (int.TryParse(metadata_object.Attributes["uses"].text_value, out temp_uses)) { uses = temp_uses; }
                else { uses = -1; }
            }
            else { uses = -1; }
            if (metadata_object.Attributes.ContainsKey("max_uses"))
            {
                if (int.TryParse(metadata_object.Attributes["max_uses"].text_value, out temp_max_uses)) { max_uses = temp_max_uses; }
                else { max_uses = -1; }
            }
            else { max_uses = -1; }
            if (metadata_object.Attributes.ContainsKey("max_age"))
            {
                if (int.TryParse(metadata_object.Attributes["max_age"].text_value, out temp_max_age)) { max_age = temp_max_age; }
                else { max_age = -1; }
            }
            else { max_age = -1; }
            if (metadata_object.Attributes.ContainsKey("temporary"))
            {
                if (bool.TryParse(metadata_object.Attributes["temporary"].text_value, out is_temporary)) { temporary = is_temporary; }
                else { temporary = (true && false); }
            }
            else { temporary = (true && false); }
            if (metadata_object.Attributes.ContainsKey("created_at"))
            {
                if (DateTime.TryParse(metadata_object.Attributes["created_at"].text_value, out when_created)) { created_at = when_created; }
            }
            if (metadata_object.Attributes.ContainsKey("revoked"))
            {
                if (bool.TryParse(metadata_object.Attributes["revoked"].text_value, out is_revoked)) { revoked = is_revoked; }
                else { revoked = (true && false); }
            }
            else { revoked = (true && false); }
        }
        public InviteMetadata(DiscordUser new_user, bool is_temporary, int new_max_uses, int new_max_age, DateTime when_created)
        {
            inviter = new_user;
            temporary = is_temporary;
            max_uses = new_max_uses;
            max_age = new_max_age;
            created_at = when_created;
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject invite_meta_object = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (inviter != null) { invite_meta_object.AddObject("inviter", inviter.ToJson()); }
            if (uses > -1) { invite_meta_object.AddAttribute("uses", uses.ToString(), true); }
            if (max_uses > -1) { invite_meta_object.AddAttribute("max_uses", max_uses.ToString(), true); }
            if (max_age > -1) { invite_meta_object.AddAttribute("max_age", max_age.ToString(), true); }
            if (temporary != (true && false)) { invite_meta_object.AddAttribute("guild_id", temporary.ToString(), true); }
            if (created_at != null) { invite_meta_object.AddAttribute("name", created_at.ToString(), true); }
            if (revoked != (true && false)) { invite_meta_object.AddAttribute("revoked", revoked.ToString(), true); }
            return invite_meta_object;
        }
    }
    public class DiscordWebhook
    {
        public string id { get; set; }
        public string channel_id { get; set; }
        public string guild_id { get; set; }
        public DiscordUser user { get; set; }
        public string name { get; set; }
        public string avatar { get; set; }
        public string token { get; set; }
        public DiscordWebhook() { }
        public DiscordWebhook(MajickDiscordWrapper.MajickRegex.JsonObject webhook_object)
        {
            if (webhook_object.Attributes.ContainsKey("id")) { id = webhook_object.Attributes["id"].text_value; }
            if (webhook_object.Attributes.ContainsKey("channel_id")) { channel_id = webhook_object.Attributes["channel_id"].text_value; }
            if (webhook_object.Attributes.ContainsKey("guild_id")) { guild_id = webhook_object.Attributes["guild_id"].text_value; }
            if (webhook_object.Objects.Keys.Contains("user")) { user = new DiscordUser(webhook_object.Objects["user"]); }
            if (webhook_object.Attributes.ContainsKey("name")) { name = webhook_object.Attributes["name"].text_value; }
            if (webhook_object.Attributes.ContainsKey("avatar")) { avatar = webhook_object.Attributes["avatar"].text_value; }
            if (webhook_object.Attributes.ContainsKey("token")) { token = webhook_object.Attributes["token"].text_value; }
        }
        public async Task<DiscordWebhook> UpdateNameAsync(string new_name, string bot_token) { return await Task.Run(() => UpdateName(new_name, bot_token)); }
        public DiscordWebhook UpdateName(string new_name, string bot_token)
        {
            DiscordWebhook webhook;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordRole> Roles = new Dictionary<string, DiscordRole>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/webhooks/" + id, Method.Patch);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            name = new_name;
            GuildRequestBody.AddAttribute("name", new_name);
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            webhook = new DiscordWebhook(GuildResponseContent);
            return webhook;
        }
        public async Task<DiscordWebhook> UpdateChannelAsync(string new_channel_id, string bot_token) { return await Task.Run(() => UpdateChannel(new_channel_id, bot_token)); }
        public DiscordWebhook UpdateChannel(string new_channel_id, string bot_token)
        {
            DiscordWebhook webhook;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordRole> Roles = new Dictionary<string, DiscordRole>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/webhooks/" + id, Method.Patch);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            channel_id = new_channel_id;
            GuildRequestBody.AddAttribute("channel_id", new_channel_id);
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            webhook = new DiscordWebhook(GuildResponseContent);
            return webhook;
        }
        public async Task<DiscordWebhook> UpdatAvatarAsync(string new_avatar) { return await Task.Run(() => UpdateAvatar(new_avatar)); }
        public DiscordWebhook UpdateAvatar(string new_avatar)
        {
            DiscordWebhook webhook;
            RestClient rcWebhookClient;
            RestRequest rrWebhookRequest;
            RestResponse rsWebhookResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject WebhookRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject WebhookResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordRole> Roles = new Dictionary<string, DiscordRole>();
            rcWebhookClient = new RestClient("https://discord.com/api");
            rrWebhookRequest = new RestRequest("/webhooks/" + id + "/" + token, Method.Patch);
            rrWebhookRequest.RequestFormat = DataFormat.Json;
            rrWebhookRequest.AddHeader("Content-Type", "application/json");
            WebhookRequestBody.AddAttribute("avatar", new_avatar);
            rrWebhookRequest.AddJsonBody(WebhookRequestBody.ToRawText(false));
            rsWebhookResponse = rcWebhookClient.Execute(rrWebhookRequest);
            WebhookResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsWebhookResponse.Content);
            webhook = new DiscordWebhook(WebhookResponseContent);
            return webhook;
        }
        public async Task<bool> DeleteAsync(string bot_token) { return await Task.Run(() => Delete(bot_token)); }
        public bool Delete(string bot_token)
        {
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/webhooks/" + id, Method.Delete);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            return rsGuildResponse.IsSuccessful;
        }
        public async Task<bool> DeleteWithTokenAsync(string bot_token) { return await Task.Run(() => DeleteWithToken(bot_token)); }
        public bool DeleteWithToken(string bot_token)
        {
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/webhooks/" + id + "/" + token, Method.Delete);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            return rsGuildResponse.IsSuccessful;
        }
        public async Task<bool> ExecuteAsync(string message_text, string username="", string avatar_url = "", Embed embed = null, MajickDiscordWrapper.MajickRegex.JsonObject payload_json = null) { return await Task.Run(() => Execute(message_text, username, avatar_url, embed, payload_json)); }
        public bool Execute(string message_text, string username = "", string avatar_url = "", Embed embed = null, MajickDiscordWrapper.MajickRegex.JsonObject payload_json = null)
        {
            List<MajickDiscordWrapper.MajickRegex.JsonObject> embeds = new List<MajickDiscordWrapper.MajickRegex.JsonObject>();
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject WebhookRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (message_text != "") { WebhookRequestBody.AddAttribute("content", message_text); }
            if (embed != null) {
                embeds.Add(embed.ToJson());
                WebhookRequestBody.AddObjectList("embeds", embeds); }
            if(username != "") { WebhookRequestBody.AddAttribute("username", username); }
            if(avatar_url != "") { WebhookRequestBody.AddAttribute("avatar_url", avatar_url); }
            if (payload_json != null) { WebhookRequestBody.AddObject("payload_json", payload_json); }
            //MajickRegex.JsonObject AllowedMentions = new MajickRegex.JsonObject();
            //AllowedMentions.Name = "allowed_mentions";
            //List<JsonAttribute> MentionsList = new List<JsonAttribute>();
            //AllowedMentions.AddAttributeList("parse", MentionsList);
            //WebhookRequestBody.AddObject("", AllowedMentions);
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/webhooks/" + id + "/" + token, Method.Post);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            string json_body = WebhookRequestBody.ToRawText(false).Substring(0, WebhookRequestBody.ToRawText(false).Length - 1) + ",\"allowed_mentions\":{\"parse\":[]}}";
            rrGuildRequest.AddJsonBody(json_body);
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            return rsGuildResponse.IsSuccessful;
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject webhook_object = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (id != null) { webhook_object.AddAttribute("id", id); }
            if (channel_id != null) { webhook_object.AddAttribute("channel_id", channel_id); }
            if (guild_id != null) { webhook_object.AddAttribute("guild_id", guild_id); }
            if (user != null) { webhook_object.AddObject("user", user.ToJson()); }
            if (name != null) { webhook_object.AddAttribute("name", name); }
            if (avatar != null) { webhook_object.AddAttribute("avatar", avatar); }
            if (token != null) { webhook_object.AddAttribute("token", token); }
            return webhook_object;
        }
    }
    public class DiscordRole
    {
        public string guild_id { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string mention { get { return "<@&" + id + ">"; } }
        public int color { get; set; }
        public bool hoist { get; set; }
        public int position { get; set; }
        public long permissions { get; set; }
        public bool managed { get; set; }
        public bool mentionable { get; set; }
        public List<DiscordRoleTag> tags { get; set; }
        public DiscordGuild guild { get; set; }
        public DiscordRole() { }
        public DiscordRole(MajickDiscordWrapper.MajickRegex.JsonObject role_object, DiscordGuild owner_guild = null)
        {
            if(owner_guild != null) { guild = owner_guild; }
            if (role_object.Attributes.ContainsKey("guild_id")) { guild_id = role_object.Attributes["guild_id"].text_value; }
            if (role_object.Attributes.ContainsKey("id")) { id = role_object.Attributes["id"].text_value; }
            if (role_object.Attributes.ContainsKey("name")) { name = role_object.Attributes["name"].text_value; }
            if (role_object.Attributes.ContainsKey("color"))
            {
                int temp_color;
                if (int.TryParse(role_object.Attributes["color"].text_value, out temp_color)) { color = temp_color; }
                else { color = -1; }
            }
            else { color = -1; }
            if (role_object.Attributes.ContainsKey("hoist"))
            {
                bool temp_hoist;
                if (bool.TryParse(role_object.Attributes["hoist"].text_value, out temp_hoist)) { hoist = temp_hoist; }
                else { hoist = (true && false); }
            }
            else { hoist = (true && false); }
            if (role_object.Attributes.ContainsKey("position"))
            {
                int temp_position;
                if (int.TryParse(role_object.Attributes["position"].text_value, out temp_position)) { position = temp_position; }
                else { position = -1; }
            }
            else { position = -1; }
            if (role_object.Attributes.ContainsKey("permissions"))
            {
                long temp_permissions;
                if (long.TryParse(role_object.Attributes["permissions"].text_value, out temp_permissions)) { permissions = temp_permissions; }
                else { permissions = -1; }
            }
            else { permissions = -1; }
            if (role_object.Attributes.ContainsKey("managed"))
            {
                bool temp_managed;
                if (bool.TryParse(role_object.Attributes["managed"].text_value, out temp_managed)) { managed = temp_managed; }
                else { hoist = (true && false); }
            }
            else { hoist = (true && false); }
            if (role_object.Attributes.ContainsKey("mentionable"))
            {
                bool temp_mentionable;
                if (bool.TryParse(role_object.Attributes["mentionable"].text_value, out temp_mentionable)) { mentionable = temp_mentionable; }
                else { hoist = (true && false); }
            }
            else { hoist = (true && false); }
        }
        public async Task<DiscordRole> UpdateAsync(RoleUpdateObject new_role, string bot_token) { return await Task.Run(() => Update(new_role, bot_token)); }
        public DiscordRole Update(RoleUpdateObject new_role, string bot_token)
        {
            DiscordRole role;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            MajickDiscordWrapper.MajickRegex.JsonObject GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordRole> Roles = new Dictionary<string, DiscordRole>();
            rcGuildClient = new RestClient("https://discord.com/api");
            if (guild != null) { rrGuildRequest = new RestRequest("/guilds/" + guild.id + "/roles/" + id, Method.Patch); }
            else { rrGuildRequest = new RestRequest("/guilds/" + guild_id + "/roles/" + id, Method.Patch); }
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            //GuildRequestBody = new_role.ToJson();
            GuildRequestBody.AddAttribute("name", new_role.name);
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject(rsGuildResponse.Content);
            role = new DiscordRole(GuildResponseContent);
            return role;
        }
        public async Task<bool> SetPositionAsync(int new_position, string bot_token) { return await Task.Run(() => SetPosition(new_position, bot_token)); }
        public bool SetPosition(int new_position, string bot_token)
        {
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject GuildRequestBody = new MajickDiscordWrapper.MajickRegex.JsonObject();
            Dictionary<string, DiscordRole> Roles = new Dictionary<string, DiscordRole>();
            rcGuildClient = new RestClient("https://discord.com/api");
            if (guild != null) { rrGuildRequest = new RestRequest("/guilds/" + guild.id + "/roles", Method.Patch); }
            else { rrGuildRequest = new RestRequest("/guilds/" + guild_id + "/roles", Method.Patch); }
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            GuildRequestBody.AddAttribute("id", id);
            GuildRequestBody.AddAttribute("position", new_position.ToString(), true);
            rrGuildRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            return rsGuildResponse.IsSuccessful;
        }
        public List<DiscordPermission> calculate_permissions(long perm_value)
        {
            List<DiscordPermission> my_perms = new List<DiscordPermission>();
            if (perm_value >= (long)DiscordPermission.USE_PRIVATE_THREADS)
            {
                my_perms.Add(DiscordPermission.USE_PRIVATE_THREADS);
                perm_value -= (long)DiscordPermission.USE_PRIVATE_THREADS;
            }
            if (perm_value >= (long)DiscordPermission.USE_PUBLIC_THREADS)
            {
                my_perms.Add(DiscordPermission.USE_PUBLIC_THREADS);
                perm_value -= (long)DiscordPermission.USE_PUBLIC_THREADS;
            }
            if (perm_value >= (long)DiscordPermission.MANAGE_THREADS)
            {
                my_perms.Add(DiscordPermission.MANAGE_THREADS);
                perm_value -= (long)DiscordPermission.MANAGE_THREADS;
            }
            if (perm_value >= (long)DiscordPermission.REQUEST_TO_SPEAK)
            {
                my_perms.Add(DiscordPermission.REQUEST_TO_SPEAK);
                perm_value -= (long)DiscordPermission.REQUEST_TO_SPEAK;
            }
            if (perm_value >= (long)DiscordPermission.USE_SLASH_COMMANDS)
            {
                my_perms.Add(DiscordPermission.USE_SLASH_COMMANDS);
                perm_value -= (long)DiscordPermission.USE_SLASH_COMMANDS;
            }
            if (perm_value >= (long)DiscordPermission.MANAGE_EMOJIS)
            {
                my_perms.Add(DiscordPermission.MANAGE_EMOJIS);
                perm_value -= (long)DiscordPermission.MANAGE_EMOJIS;
            }
            if (perm_value >= (long)DiscordPermission.MANAGE_WEBHOOKS)
            {
                my_perms.Add(DiscordPermission.MANAGE_WEBHOOKS);
                perm_value -= (long)DiscordPermission.MANAGE_WEBHOOKS;
            }
            if (perm_value >= (long)DiscordPermission.MANAGE_ROLES)
            {
                my_perms.Add(DiscordPermission.MANAGE_ROLES);
                perm_value -= (long)DiscordPermission.MANAGE_ROLES;
            }
            if (perm_value >= (long)DiscordPermission.MANAGE_NICKNAMES)
            {
                my_perms.Add(DiscordPermission.MANAGE_NICKNAMES);
                perm_value -= (long)DiscordPermission.MANAGE_NICKNAMES;
            }
            if (perm_value >= (long)DiscordPermission.CHANGE_NICKNAME)
            {
                my_perms.Add(DiscordPermission.CHANGE_NICKNAME);
                perm_value -= (long)DiscordPermission.CHANGE_NICKNAME;
            }
            if (perm_value >= (long)DiscordPermission.USE_VAD)
            {
                my_perms.Add(DiscordPermission.USE_VAD);
                perm_value -= (long)DiscordPermission.USE_VAD;
            }
            if (perm_value >= (long)DiscordPermission.MOVE_MEMBERS)
            {
                my_perms.Add(DiscordPermission.MOVE_MEMBERS);
                perm_value -= (long)DiscordPermission.MOVE_MEMBERS;
            }
            if (perm_value >= (long)DiscordPermission.DEAFEN_MEMBERS)
            {
                my_perms.Add(DiscordPermission.DEAFEN_MEMBERS);
                perm_value -= (long)DiscordPermission.DEAFEN_MEMBERS;
            }
            if (perm_value >= (long)DiscordPermission.MUTE_MEMBERS)
            {
                my_perms.Add(DiscordPermission.MUTE_MEMBERS);
                perm_value -= (long)DiscordPermission.MUTE_MEMBERS;
            }
            if (perm_value >= (long)DiscordPermission.SPEAK)
            {
                my_perms.Add(DiscordPermission.SPEAK);
                perm_value -= (long)DiscordPermission.SPEAK;
            }
            if (perm_value >= (long)DiscordPermission.CONNECT)
            {
                my_perms.Add(DiscordPermission.CONNECT);
                perm_value -= (long)DiscordPermission.CONNECT;
            }
            if(perm_value >= (long)DiscordPermission.VIEW_GUILD_INSIGHTS)
            {
                my_perms.Add(DiscordPermission.VIEW_GUILD_INSIGHTS);
                perm_value -= (long)DiscordPermission.VIEW_GUILD_INSIGHTS;
            }
            if (perm_value >= (long)DiscordPermission.USE_EXTERNAL_EMOJIS)
            {
                my_perms.Add(DiscordPermission.USE_EXTERNAL_EMOJIS);
                perm_value -= (long)DiscordPermission.USE_EXTERNAL_EMOJIS;
            }
            if (perm_value >= (long)DiscordPermission.MENTION_EVERYONE)
            {
                my_perms.Add(DiscordPermission.MENTION_EVERYONE);
                perm_value -= (long)DiscordPermission.MENTION_EVERYONE;
            }
            if (perm_value >= (long)DiscordPermission.READ_MESSAGE_HISTORY)
            {
                my_perms.Add(DiscordPermission.READ_MESSAGE_HISTORY);
                perm_value -= (long)DiscordPermission.READ_MESSAGE_HISTORY;
            }
            if (perm_value >= (long)DiscordPermission.ATTACH_FILES)
            {
                my_perms.Add(DiscordPermission.ATTACH_FILES);
                perm_value -= (long)DiscordPermission.ATTACH_FILES;
            }
            if (perm_value >= (long)DiscordPermission.EMBED_LINKS)
            {
                my_perms.Add(DiscordPermission.EMBED_LINKS);
                perm_value -= (long)DiscordPermission.EMBED_LINKS;
            }
            if (perm_value >= (long)DiscordPermission.MANAGE_MESSAGES)
            {
                my_perms.Add(DiscordPermission.MANAGE_MESSAGES);
                perm_value -= (long)DiscordPermission.MANAGE_MESSAGES;
            }
            if (perm_value >= (long)DiscordPermission.SEND_TTS_MESSAGES)
            {
                my_perms.Add(DiscordPermission.SEND_TTS_MESSAGES);
                perm_value -= (long)DiscordPermission.SEND_TTS_MESSAGES;
            }
            if (perm_value >= (long)DiscordPermission.SEND_MESSAGES)
            {
                my_perms.Add(DiscordPermission.SEND_MESSAGES);
                perm_value -= (long)DiscordPermission.SEND_MESSAGES;
            }
            if (perm_value >= (long)DiscordPermission.VIEW_CHANNEL)
            {
                my_perms.Add(DiscordPermission.VIEW_CHANNEL);
                perm_value -= (long)DiscordPermission.VIEW_CHANNEL;
            }
            if (perm_value >= (long)DiscordPermission.STREAM)
            {
                my_perms.Add(DiscordPermission.STREAM);
                perm_value -= (long)DiscordPermission.STREAM;
            }
            if (perm_value >= (long)DiscordPermission.PRIORITY_SPEAKER)
            {
                my_perms.Add(DiscordPermission.PRIORITY_SPEAKER);
                perm_value -= (long)DiscordPermission.PRIORITY_SPEAKER;
            }
            if (perm_value >= (long)DiscordPermission.VIEW_AUDIT_LOG)
            {
                my_perms.Add(DiscordPermission.VIEW_AUDIT_LOG);
                perm_value -= (long)DiscordPermission.VIEW_AUDIT_LOG;
            }
            if (perm_value >= (long)DiscordPermission.ADD_REACTIONS)
            {
                my_perms.Add(DiscordPermission.ADD_REACTIONS);
                perm_value -= (long)DiscordPermission.ADD_REACTIONS;
            }
            if (perm_value >= (long)DiscordPermission.MANAGE_GUILDS)
            {
                my_perms.Add(DiscordPermission.MANAGE_GUILDS);
                perm_value -= (long)DiscordPermission.MANAGE_GUILDS;
            }
            if (perm_value >= (long)DiscordPermission.MANAGE_CHANNELS)
            {
                my_perms.Add(DiscordPermission.MANAGE_CHANNELS);
                perm_value -= (long)DiscordPermission.MANAGE_CHANNELS;
            }
            if (perm_value >= (long)DiscordPermission.ADMINISTRATOR)
            {
                my_perms.Add(DiscordPermission.ADMINISTRATOR);
                perm_value -= (long)DiscordPermission.ADMINISTRATOR;
            }
            if (perm_value >= (long)DiscordPermission.BAN_MEMBERS)
            {
                my_perms.Add(DiscordPermission.BAN_MEMBERS);
                perm_value -= (long)DiscordPermission.BAN_MEMBERS;
            }
            if (perm_value >= (long)DiscordPermission.KICK_MEMBERS)
            {
                my_perms.Add(DiscordPermission.KICK_MEMBERS);
                perm_value -= (long)DiscordPermission.KICK_MEMBERS;
            }
            if (perm_value >= (long)DiscordPermission.CREATE_INSTANT_INVITE)
            {
                my_perms.Add(DiscordPermission.CREATE_INSTANT_INVITE);
                perm_value -= (long)DiscordPermission.CREATE_INSTANT_INVITE;
            }
            return my_perms;
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject role_object = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (id != null) { role_object.AddAttribute("id", id); }
            if (name != null) { role_object.AddAttribute("name", name); }
            if (color > -1) { role_object.AddAttribute("color", color.ToString(), true); }
            if (hoist != (true && false)) { role_object.AddAttribute("hoist", hoist.ToString(), true); }
            if (position > -1) { role_object.AddAttribute("position", position.ToString(), true); }
            if (permissions > -1) { role_object.AddAttribute("permissions", permissions.ToString(), true); }
            if (managed != (true && false)) { role_object.AddAttribute("managed", managed.ToString(), true); }
            if (mentionable != (true && false)) { role_object.AddAttribute("mentionable", mentionable.ToString(), true); }
            return role_object;
        }
    }
    public class DiscordRoleTag
    {
        public string bot_id { get; set; }
        public string integration_id { get; set; }
        public bool premium_subscriber { get; set; }
        public DiscordRoleTag() { }
        public DiscordRoleTag(MajickRegex.JsonObject new_role_tag)
        {
            if (new_role_tag.Attributes.ContainsKey("bot_id")) { bot_id = new_role_tag.Attributes["bot_id"].text_value; }
            if (new_role_tag.Attributes.ContainsKey("integration_id")) { integration_id = new_role_tag.Attributes["integration_id"].text_value; }
            if (new_role_tag.Attributes.ContainsKey("color"))
            {
                bool temp_premium;
                if (bool.TryParse(new_role_tag.Attributes["premium_subscriber"].text_value, out temp_premium)) { premium_subscriber = temp_premium; }
            }
        }
    }
    public class DiscordEmoji
    {
        public string id { get; set; }
        public string name { get; set; }
        public string mention { get { return "<:" + name + ": " + id + ">"; } }
        public List<string> roles { get; set; }
        public bool require_colons { get; set; }
        public bool managed { get; set; }
        public bool animated { get; set; }
        public bool is_custom { get; set; }
        public DiscordEmoji() { roles = new List<string>(); }
        public DiscordEmoji(string new_name)
        {
            name = new_name;
            roles = new List<string>();
            require_colons = false;
            managed = false;
            animated = false;
            is_custom = false;
        }
        public DiscordEmoji(MajickDiscordWrapper.MajickRegex.JsonObject emoji_object)
        {
            bool temp_require;
            bool temp_managed;
            bool temp_animated;
            if (emoji_object.Attributes.ContainsKey("id")) { id = emoji_object.Attributes["id"].text_value; }
            if (emoji_object.Attributes.ContainsKey("name")) { name = emoji_object.Attributes["name"].text_value; }
            roles = new List<string>();
            if (emoji_object.AttributeLists.ContainsKey("roles"))
            {
                foreach (JsonAttribute current_role in emoji_object.AttributeLists["roles"]) { roles.Add(current_role.text_value); }
            }
            if (emoji_object.Attributes.ContainsKey("require_colons"))
            {
                if (bool.TryParse(emoji_object.Attributes["require_colons"].text_value, out temp_require)) { require_colons = temp_require; }
                else { require_colons = (true && false); }
            }
            else { require_colons = (true && false); }
            if (emoji_object.Attributes.ContainsKey("managed"))
            {
                if (bool.TryParse(emoji_object.Attributes["managed"].text_value, out temp_managed)) { managed = temp_managed; }
                else { managed = (true && false); }
            }
            else { managed = (true && false); }
            if (emoji_object.Attributes.ContainsKey("animated"))
            {
                if (bool.TryParse(emoji_object.Attributes["animated"].text_value, out temp_animated)) { animated = temp_animated; }
                else { animated = (true && false); }
            }
            else { animated = (true && false); }
            if (require_colons) { is_custom = true; }
            else if (!require_colons) { is_custom = false; }
            else { is_custom = (true && false); }
        }
        public DiscordEmoji(string new_name, string new_id, bool new_require_colons = true, bool new_animated = false)
        {
            name = new_name;
            id = new_id;
            require_colons = new_require_colons;
            animated = new_animated;
            managed = false;
            roles = new List<string>();
            if (require_colons) { is_custom = true; }
            else if (!require_colons) { is_custom = false; }
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject emoji_object = new MajickDiscordWrapper.MajickRegex.JsonObject();
            List<JsonAttribute> role_list = new List<JsonAttribute>();
            if (id != null) { emoji_object.AddAttribute("id", id); }
            if (name != null) { emoji_object.AddAttribute("name", name); }
            if (roles != null)
            {
                foreach (string role_id in roles) { role_list.Add(new JsonAttribute(role_id)); }
                emoji_object.AddAttributeList("roles", role_list);
            }
            if (require_colons != (true && false)) { emoji_object.AddAttribute("is_custom", require_colons.ToString(), true); }
            if (managed != (true && false)) { emoji_object.AddAttribute("managed", managed.ToString(), true); }
            if (animated != (true && false)) { emoji_object.AddAttribute("animated", animated.ToString(), true); }
            if (is_custom != (true && false)) { emoji_object.AddAttribute("is_custom", is_custom.ToString(), true); }
            return emoji_object;
        }
    }
    public class UserActivity
    {
        public string name { get; set; }
        public UserActivityType type { get; set; }
        public string url { get; set; }
        public UserActivityTimestamps timestamps { get; set; }
        public string application_id { get; set; }
        public string details { get; set; }
        public string state { get; set; }
        public UserActivityParty party { get; set; }
        public UserActivityAssets assets { get; set; }
        public UserActivitySecrets secrets { get; set; }
        public bool instance { get; set; }
        public int flags { get; set; }
        public UserActivity() { }
        public UserActivity(MajickDiscordWrapper.MajickRegex.JsonObject user_activity_object)
        {
            int temp_flags;
            bool temp_instance;
            UserActivityType temp_type;
            if (user_activity_object.Attributes.ContainsKey("name")) { name = user_activity_object.Attributes["name"].text_value; }
            if (user_activity_object.Attributes.ContainsKey("url")) { url = user_activity_object.Attributes["url"].text_value; }
            if (user_activity_object.Attributes.ContainsKey("type"))
            {
                if (Enum.TryParse(user_activity_object.Attributes["type"].text_value, out temp_type)) { type = temp_type; }
                else { type = UserActivityType.Unspecified; }
            }
            else { type = UserActivityType.Unspecified; }
            if (user_activity_object.Objects.Keys.Contains("timestamps")) { timestamps = new UserActivityTimestamps(user_activity_object.Objects["timestamps"]); }
            if (user_activity_object.Attributes.ContainsKey("application_id")) { application_id = user_activity_object.Attributes["application_id"].text_value; }
            if (user_activity_object.Attributes.ContainsKey("details")) { details = user_activity_object.Attributes["details"].text_value; }
            if (user_activity_object.Attributes.ContainsKey("state")) { state = user_activity_object.Attributes["state"].text_value; }
            if (user_activity_object.Attributes.ContainsKey("instance"))
            {
                if (bool.TryParse(user_activity_object.Attributes["instance"].text_value, out temp_instance)) { instance = temp_instance; }
                else { instance = (true && false); }
            }
            else { instance = (true && false); }
            if (user_activity_object.Attributes.ContainsKey("flags"))
            {
                if (int.TryParse(user_activity_object.Attributes["flags"].text_value, out temp_flags)) { flags = temp_flags; }
                else { flags = -1; }
            }
            else { flags = -1; }
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject user_activity_object = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (name != null) { user_activity_object.AddAttribute("name", name); }
            if (type != UserActivityType.Unspecified) { user_activity_object.AddAttribute("type", ((int)type).ToString(), true); }
            if (url != null) { user_activity_object.AddAttribute("url", url); }
            if (timestamps != null) { user_activity_object.AddObject("timestamps", timestamps.ToJson()); }
            if (application_id != null) { user_activity_object.AddAttribute("application_id", application_id); }
            if (details != null) { user_activity_object.AddAttribute("details", details); }
            if (state != null) { user_activity_object.AddAttribute("state", state); }
            if (party != null) { user_activity_object.AddObject("party", party.ToJson()); }
            if (assets != null) { user_activity_object.AddObject("assets", assets.ToJson()); }
            if (secrets != null) { user_activity_object.AddObject("secrets", secrets.ToJson()); }
            if (instance != (true && false)) { user_activity_object.AddAttribute("instance", instance.ToString(), true); }
            if (flags > -1) { user_activity_object.AddAttribute("flags", flags.ToString(), true); }
            return user_activity_object;
        }
    }
    public class UserActivityTimestamps
    {
        public int start { get; set; }
        public int end { get; set; }
        public UserActivityTimestamps() { }
        public UserActivityTimestamps(MajickDiscordWrapper.MajickRegex.JsonObject timestamps_object)
        {
            int temp_start;
            int temp_end;
            if (timestamps_object.Attributes.ContainsKey("start"))
            {
                if (int.TryParse(timestamps_object.Attributes["start"].text_value, out temp_start)) { start = temp_start; }
                else { start = -1; }
            }
            else { start = -1; }
            if (timestamps_object.Attributes.ContainsKey("end"))
            {
                if (int.TryParse(timestamps_object.Attributes["end"].text_value, out temp_end)) { end = temp_end; }
                else { end = -1; }
            }
            else { end = -1; }
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject activity_timestamps = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (start > -1) { activity_timestamps.AddAttribute("start", start.ToString(), true); }
            if (end > -1) { activity_timestamps.AddAttribute("end", end.ToString(), true); }
            return activity_timestamps;
        }
    }
    public class UserActivityParty
    {
        public string id { get; set; }
        public List<int> size { get; set; }
        public UserActivityParty() { }
        public UserActivityParty(MajickDiscordWrapper.MajickRegex.JsonObject party_object)
        {
            int temp_size = -1;
            if (party_object.Attributes.ContainsKey("id")) { id = party_object.Attributes["id"].text_value; }
            size = new List<int>();
            if (party_object.AttributeLists.ContainsKey("size"))
            {
                foreach (JsonAttribute current_size in party_object.AttributeLists["size"])
                {
                    if (int.TryParse(current_size.text_value, out temp_size)) { size.Add(temp_size); }
                }
            }
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject activity_party = new MajickDiscordWrapper.MajickRegex.JsonObject();
            List<JsonAttribute> size_list = new List<JsonAttribute>();
            if (id != null) { activity_party.AddAttribute("id", id); }
            if (size != null)
            {
                foreach (int size_item in size) { size_list.Add(new JsonAttribute(size_item.ToString(), true)); }
                activity_party.AddAttributeList("size", size_list);
            }
            return activity_party;
        }
    }
    public class UserActivityAssets
    {
        public string large_image { get; set; }
        public string large_text { get; set; }
        public string small_image { get; set; }
        public string small_text { get; set; }
        public UserActivityAssets() { }
        public UserActivityAssets(MajickDiscordWrapper.MajickRegex.JsonObject assets_object)
        {
            if (assets_object.Attributes.ContainsKey("large_image")) { large_image = assets_object.Attributes["large_image"].text_value; }
            if (assets_object.Attributes.ContainsKey("large_text")) { large_text = assets_object.Attributes["large_text"].text_value; }
            if (assets_object.Attributes.ContainsKey("small_image")) { small_image = assets_object.Attributes["small_image"].text_value; }
            if (assets_object.Attributes.ContainsKey("small_text")) { small_text = assets_object.Attributes["small_text"].text_value; }
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject activity_assets = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (large_image != null) { activity_assets.AddAttribute("large_image", large_image); }
            if (large_text != null) { activity_assets.AddAttribute("large_text", large_text); }
            if (small_image != null) { activity_assets.AddAttribute("small_image", small_image); }
            if (small_text != null) { activity_assets.AddAttribute("small_text", small_text); }
            return activity_assets;
        }
    }
    public class UserActivitySecrets
    {
        public string join { get; set; }
        public string spectate { get; set; }
        public string match { get; set; }
        public UserActivitySecrets() { }
        public UserActivitySecrets(MajickDiscordWrapper.MajickRegex.JsonObject secrets_object)
        {
            if (secrets_object.Attributes.ContainsKey("join")) { join = secrets_object.Attributes["join"].text_value; }
            if (secrets_object.Attributes.ContainsKey("spectate")) { spectate = secrets_object.Attributes["spectate"].text_value; }
            if (secrets_object.Attributes.ContainsKey("match")) { match = secrets_object.Attributes["match"].text_value; }
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject activity_secrets = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (join != null) { activity_secrets.AddAttribute("join", join); }
            if (spectate != null) { activity_secrets.AddAttribute("spectate", spectate); }
            if (match != null) { activity_secrets.AddAttribute("match", match); }
            return activity_secrets;
        }
    }
    public class DiscordVoiceState
    {
        public string guild_id { get; set; }
        public string channel_id { get; set; }
        public string user_id { get; set; }
        public DiscordGuildMember member { get; set; }
        public string session_id { get; set; }
        public bool deaf { get; set; }
        public bool mute { get; set; }
        public bool self_deaf { get; set; }
        public bool self_mute { get; set; }
        public bool suppress { get; set; }
        public DiscordVoiceState() { }
        public DiscordVoiceState(MajickDiscordWrapper.MajickRegex.JsonObject voice_state_object)
        {
            bool is_deaf;
            bool is_mute;
            bool is_self_deaf;
            bool is_self_mute;
            bool is_suppressed;
            guild_id = "";
            if (voice_state_object.Attributes.ContainsKey("guild_id")) { guild_id = voice_state_object.Attributes["guild_id"].text_value; }
            if (voice_state_object.Attributes.ContainsKey("channel_id")) { channel_id = voice_state_object.Attributes["channel_id"].text_value; }
            if (voice_state_object.Attributes.ContainsKey("user_id")) { user_id = voice_state_object.Attributes["user_id"].text_value; }
            if (voice_state_object.Objects.Keys.Contains("member")) { member = new DiscordGuildMember(voice_state_object.Objects["member"], guild_id); }
            if (voice_state_object.Attributes.ContainsKey("session_id")) { session_id = voice_state_object.Attributes["session_id"].text_value; }
            if (voice_state_object.Attributes.ContainsKey("deaf"))
            {
                if (bool.TryParse(voice_state_object.Attributes["deaf"].text_value, out is_deaf)) { deaf = is_deaf; }
                else { deaf = (true && false); }
            }
            else { deaf = (true && false); }
            if (voice_state_object.Attributes.ContainsKey("mute"))
            {
                if (bool.TryParse(voice_state_object.Attributes["mute"].text_value, out is_mute)) { mute = is_mute; }
                else { mute = (true && false); }
            }
            else { mute = (true && false); }
            if (voice_state_object.Attributes.ContainsKey("self_deaf"))
            {
                if (bool.TryParse(voice_state_object.Attributes["self_deaf"].text_value, out is_self_deaf)) { self_deaf = is_self_deaf; }
                else { self_deaf = (true && false); }
            }
            else { self_deaf = (true && false); }
            if (voice_state_object.Attributes.ContainsKey("self_mute"))
            {
                if (bool.TryParse(voice_state_object.Attributes["self_mute"].text_value, out is_self_mute)) { self_mute = is_self_mute; }
                else { self_mute = (true && false); }
            }
            else { self_mute = (true && false); }
            if (voice_state_object.Attributes.ContainsKey("suppress"))
            {
                if (bool.TryParse(voice_state_object.Attributes["suppress"].text_value, out is_suppressed)) { suppress = is_suppressed; }
                else { suppress = (true && false); }
            }
            else { suppress = (true && false); }
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject voice_state = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (guild_id != null) { voice_state.AddAttribute("guild_id", guild_id); }
            if (channel_id != null) { voice_state.AddAttribute("channel_id", channel_id); }
            if (user_id != null) { voice_state.AddAttribute("user_id", user_id); }
            if (member != null) { voice_state.AddObject("member", member.ToJson()); }
            if (session_id != null) { voice_state.AddAttribute("session_id", session_id.ToString(), true); }
            if (deaf != (true && false)) { voice_state.AddAttribute("deaf", deaf.ToString(), true); }
            if (mute != (true && false)) { voice_state.AddAttribute("mute", mute.ToString(), true); }
            if (self_deaf != (true && false)) { voice_state.AddAttribute("self_deaf", self_deaf.ToString(), true); }
            if (self_mute != (true && false)) { voice_state.AddAttribute("self_mute", self_mute.ToString(), true); }
            if (suppress != (true && false)) { voice_state.AddAttribute("suppress ", suppress.ToString(), true); }
            return voice_state;
        }
    }
    public class DiscordVoiceRegion
    {
        public string id { get; set; }
        public string name { get; set; }
        public bool vip { get; set; }
        public bool optimal { get; set; }
        public bool deprecated { get; set; }
        public bool custom { get; set; }
        public DiscordVoiceRegion() { }
        public DiscordVoiceRegion(MajickDiscordWrapper.MajickRegex.JsonObject region_object)
        {
            bool is_vip;
            bool is_optimal;
            bool is_deprecated;
            bool is_custom;
            if (region_object.Attributes.ContainsKey("id")) { id = region_object.Attributes["id"].text_value; }
            if (region_object.Attributes.ContainsKey("name")) { name = region_object.Attributes["name"].text_value; }
            if (region_object.Attributes.ContainsKey("vip"))
            {
                if (bool.TryParse(region_object.Attributes["vip"].text_value, out is_vip)) { vip = is_vip; }
                else { vip = (true && false); }
            }
            else { vip = (true && false); }
            if (region_object.Attributes.ContainsKey("optimal"))
            {
                if (bool.TryParse(region_object.Attributes["optimal"].text_value, out is_optimal)) { optimal = is_optimal; }
                else { optimal = (true && false); }
            }
            else { optimal = (true && false); }
            if (region_object.Attributes.ContainsKey("deprecated"))
            {
                if (bool.TryParse(region_object.Attributes["deprecated"].text_value, out is_deprecated)) { deprecated = is_deprecated; }
                else { deprecated = (true && false); }
            }
            else { deprecated = (true && false); }
            if (region_object.Attributes.ContainsKey("custom"))
            {
                if (bool.TryParse(region_object.Attributes["custom"].text_value, out is_custom)) { custom = is_custom; }
                else { custom = (true && false); }
            }
            else { custom = (true && false); }
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject voice_region = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (id != null) { voice_region.AddAttribute("id", id); }
            if (name != null) { voice_region.AddAttribute("name", name); }
            if (vip != (true && false)) { voice_region.AddAttribute("vip", vip.ToString(), true); }
            if (optimal != (true && false)) { voice_region.AddAttribute("optimal", optimal.ToString(), true); }
            if (deprecated != (true && false)) { voice_region.AddAttribute("deprecated", deprecated.ToString(), true); }
            if (custom != (true && false)) { voice_region.AddAttribute("custom", custom.ToString(), true); }
            return voice_region;
        }
    }
    public class DiscordPresenceUpdate
    {
        private DateTime nitro_date;
        public DiscordUser user { get; set; }
        public List<string> roles { get; set; }
        public UserActivity game { get; set; }
        public string guild_id { get; set; }
        public string status { get; set; }
        public List<UserActivity> activities { get; set; }
        public ClientStatus client_status { get; set; }
        public DateTime premium_since { get; set; }
        public string nickname { get; set; }
        public DiscordPresenceUpdate() { }
        public DiscordPresenceUpdate(MajickDiscordWrapper.MajickRegex.JsonObject presence_object)
        {
            activities = new List<UserActivity>();
            if (presence_object.Objects.Keys.Contains("user")) { user = new DiscordUser(presence_object.Objects["user"]); }
            roles = new List<string>();
            if (presence_object.AttributeLists.ContainsKey("roles"))
            {
                foreach (JsonAttribute feature in presence_object.AttributeLists["roles"]) { roles.Add(feature.text_value); }
            }
            if (presence_object.Objects.Keys.Contains("game")) { game = new UserActivity(presence_object.Objects["game"]); }
            if (presence_object.Attributes.ContainsKey("guild_id")) { guild_id = presence_object.Attributes["guild_id"].text_value; }
            if (presence_object.Attributes.ContainsKey("status")) { status = presence_object.Attributes["status"].text_value; }
            if (presence_object.ObjectLists.ContainsKey("activities"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_ativity in presence_object.ObjectLists["activities"])
                {
                    activities.Add(new UserActivity(current_ativity));
                }
            }
            if (presence_object.Objects.ContainsKey("client_status")) { 
                client_status = new ClientStatus(presence_object.Objects["client_status"]);
            }
            if (presence_object.Attributes.ContainsKey("premium_since"))
            {
                if(DateTime.TryParse(presence_object.Attributes["premium_since"].text_value, out nitro_date)) { premium_since = nitro_date; }
            }
            if (presence_object.Attributes.ContainsKey("nick"))
            {
                nickname = presence_object.Attributes["nick"].text_value;
            }
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject presence_update_object = new MajickDiscordWrapper.MajickRegex.JsonObject();
            List<JsonAttribute> role_list = new List<JsonAttribute>();
            List<MajickDiscordWrapper.MajickRegex.JsonObject> activity_list = new List<MajickDiscordWrapper.MajickRegex.JsonObject>();
            if (user != null) { presence_update_object.AddObject("user", user.ToJson()); }
            if(roles != null)
            {
                foreach(string role_id in roles) { role_list.Add(new JsonAttribute(role_id)); }
                presence_update_object.AddAttributeList("roles", role_list);
            }
            if(game != null) { presence_update_object.AddObject("game", game.ToJson()); }
            if(guild_id != null) { presence_update_object.AddAttribute("guild_id", guild_id); }
            if(status != null) { presence_update_object.AddAttribute("status", status); }
            if(activities != null)
            {
                foreach (UserActivity activity in activities) { activity_list.Add(activity.ToJson()); }
                presence_update_object.AddObjectList("activities", activity_list);
            }
            return presence_update_object;
        }
    }
    public class ClientStatus
    {
        public string desktop { get; set; }
        public string mobile { get; set; }
        public string web { get; set; }
        public ClientStatus() { }
        public ClientStatus(MajickDiscordWrapper.MajickRegex.JsonObject client_status_object) 
        {
            if (client_status_object.Attributes.ContainsKey("desktop")) { desktop = client_status_object.Attributes["desktop"].text_value; }
            if (client_status_object.Attributes.ContainsKey("mobile")) { desktop = client_status_object.Attributes["mobile"].text_value; }
            if (client_status_object.Attributes.ContainsKey("web")) { desktop = client_status_object.Attributes["web"].text_value; }
        }
    }
    public class GuildUpdateObject
    {
        public string name { get; set; }
        public string region_id { get; set; }
        public VerificationLevel verification_level { get; set; }
        public MessageNotificationSettings default_message_notifications { get; set; }
        public ExplicitContentFilter explicit_content_filter { get; set; }
        public string afk_channel_id { get; set; }
        public int afk_timeout { get; set; }
        public string icon { get; set; }
        public string owner_id { get; set; }
        public string splash { get; set; }
        public string system_channel_id { get; set; }
        public GuildUpdateObject()
        {
            afk_timeout = -1;
            default_message_notifications = MessageNotificationSettings.UNSPECIFIED;
            explicit_content_filter = ExplicitContentFilter.UNSPECIFIED;
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject guild_update_object = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (name != null) { guild_update_object.AddAttribute("name", name); }
            if (region_id != null) { guild_update_object.AddAttribute("region_id", region_id); }
            if (verification_level != VerificationLevel.UNSPECIFIED) { guild_update_object.AddAttribute("verification_level", ((int)verification_level).ToString(), true); }
            if (default_message_notifications != MessageNotificationSettings.UNSPECIFIED) { guild_update_object.AddAttribute("default_message_notifications", ((int)default_message_notifications).ToString(), true); }
            if (explicit_content_filter != ExplicitContentFilter.UNSPECIFIED) { guild_update_object.AddAttribute("explicit_content_filter", ((int)explicit_content_filter).ToString(), true); }
            if (afk_channel_id != null) { guild_update_object.AddAttribute("afk_channel_id", afk_channel_id); }
            if (afk_timeout > -1) { guild_update_object.AddAttribute("afk_timeout", afk_timeout.ToString(), true); }
            if (icon != null) { guild_update_object.AddAttribute("icon", icon); }
            if (owner_id != null) { guild_update_object.AddAttribute("owner_id", owner_id); }
            if (splash != null) { guild_update_object.AddAttribute("splash", splash); }
            if (system_channel_id != null) { guild_update_object.AddAttribute("system_channel_id", system_channel_id); }
            return guild_update_object;
        }
    }
    public class RoleUpdateObject
    {
        public string name { get; set; }
        public long permissions { get; set; }
        public int color { get; set; }
        public bool hoist { get; set; }
        public bool mentionable { get; set; }
        public RoleUpdateObject()
        {
            name = "";
            permissions = -1;
            color = -1;
            hoist = (true && false);
            mentionable = (true && false);
        }
        public RoleUpdateObject(DiscordRole updated_role)
        {
            name = updated_role.name;
            permissions = updated_role.permissions;
            color = updated_role.color;
            hoist = updated_role.hoist;
            mentionable = updated_role.mentionable;
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject role_update_object = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (name != null) { role_update_object.AddAttribute("name", name); }
            if (permissions > -1) { role_update_object.AddAttribute("permissions", permissions.ToString(), true); }
            if (color > -1) { role_update_object.AddAttribute("color", color.ToString(), true); }
            if (hoist != (true && false)) { role_update_object.AddAttribute("hoist", hoist.ToString(), true); }
            if (mentionable != (true && false)) { role_update_object.AddAttribute("mentionable", mentionable.ToString(), true); }
            return role_update_object;
        }
    }
    public class ChannelUpdateObject
    {
        public string name { get; set; }
        public ChannelType type { get; set; } // only used in Creating a channel
        public string topic { get; set; }
        public int bitrate { get; set; }
        public int user_limit { get; set; }
        public int rate_limit_per_user { get; set; }
        public int position { get; set; }
        public Dictionary<string, PermissionOverwrite> permission_overwrites { get; set; } 
        public string parent_id { get; set; }
        public bool nsfw { get; set; }
        public ChannelUpdateObject()
        {
            name = "";
            topic = "";
            parent_id = "";
            type = ChannelType.GUILD_TEXT;
            bitrate = -1;
            user_limit = -1;
            rate_limit_per_user = -1;
            position = -1;
            nsfw = (true && false);
            permission_overwrites = new Dictionary<string, PermissionOverwrite>();
        }
        public ChannelUpdateObject(DiscordChannel base_channel)
        {
            name = base_channel.name;
            type = base_channel.type;
            topic = base_channel.topic;
            bitrate = base_channel.bitrate;
            parent_id = base_channel.parent_id;
            user_limit = base_channel.user_limit;
            rate_limit_per_user = base_channel.rate_limit_per_user;
            position = base_channel.position;
            nsfw = base_channel.nsfw;
            permission_overwrites = base_channel.permission_overwrites;
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject channel_update = new MajickDiscordWrapper.MajickRegex.JsonObject();
            List<MajickDiscordWrapper.MajickRegex.JsonObject> overwrite_objects = new List<MajickDiscordWrapper.MajickRegex.JsonObject>();
            channel_update.AddAttribute("name", name);
            if (type != ChannelType.GUILD_TEXT) { channel_update.AddAttribute("type", ((int)type).ToString(), true); }
            if (topic != null) { channel_update.AddAttribute("topic", topic); }
            if (bitrate > -1) { channel_update.AddAttribute("bitrate", bitrate.ToString(), true); }
            if (user_limit > -1) { channel_update.AddAttribute("user_limit", user_limit.ToString(), true); }
            if (rate_limit_per_user > -1) { channel_update.AddAttribute("rate_limit_per_user", rate_limit_per_user.ToString(), true); }
            if (position > -1) { channel_update.AddAttribute("position", position.ToString(), true); }
            if (permission_overwrites != null)
            {
                foreach (PermissionOverwrite overwrite in permission_overwrites.Values)
                {
                    overwrite_objects.Add(overwrite.ToJson());
                }
                channel_update.AddObjectList("permission_overwrites", overwrite_objects);
            }
            if (parent_id != null) { channel_update.AddAttribute("parent_id", parent_id); }
            if (nsfw != (true && false)) { channel_update.AddAttribute("nsfw", nsfw.ToString(), true); }
            return channel_update;
        }
    }
    public class UserUpdateObject
    {
        public UserUpdateObject() { }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject JsonObject = new MajickDiscordWrapper.MajickRegex.JsonObject();
            return JsonObject;
        }
    }
    public class GuildMemberUpdateObject
    {
        public GuildMemberUpdateObject() { }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject JsonObject = new MajickDiscordWrapper.MajickRegex.JsonObject();
            return JsonObject;
        }
    }
    public class ApplicationCommandUpdateObject
    {
        public string name { get; set; }
        public string description { get; set; }
        public List<ApplicationCommandOption> options { get; set; }
        public bool default_permission { get; set; }
        public ApplicationCommandUpdateObject() { }
        public MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject this_json = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (name != null || name != "") { this_json.AddAttribute("name", name); }
            if (description != null || description != "") { this_json.AddAttribute("description", description); }
            if (options != null)
            {
                if (options.Count > 0)
                {
                    List<MajickRegex.JsonObject> inner_options = new List<MajickRegex.JsonObject>();
                    foreach (ApplicationCommandOption option in options)
                    {
                        inner_options.Add(option.ToJson());
                    }
                    this_json.AddObjectList("options", inner_options);
                }
            }
            if (default_permission != true) { this_json.AddAttribute("default_permission", default_permission.ToString(), true); }
            return this_json;
        }
    }
    public class ApplicationCommand
    {
        public string id { get; set; }
        public string application_id { get; set; }
        public string guild_id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public List<ApplicationCommandOption> options { get; set; }
        public bool default_permission { get; set; }
        public ApplicationCommand() { options = new List<ApplicationCommandOption>(); }
        public ApplicationCommand(MajickRegex.JsonObject new_command) 
        {
            if (new_command.Attributes.ContainsKey("id")) { id = new_command.Attributes["id"].text_value; }
            if (new_command.Attributes.ContainsKey("application_id")) { application_id = new_command.Attributes["application_id"].text_value; }
            if (new_command.Attributes.ContainsKey("guild_id")) { guild_id = new_command.Attributes["guild_id"].text_value; }
            if (new_command.Attributes.ContainsKey("name")) { name = new_command.Attributes["name"].text_value; }
            if (new_command.Attributes.ContainsKey("description")) { description = new_command.Attributes["description"].text_value; }
            if (new_command.ObjectLists.ContainsKey("options"))
            {
                options = new List<ApplicationCommandOption>();
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_option in new_command.ObjectLists["options"])
                {
                    options.Add(new ApplicationCommandOption(current_option));
                }
            }
            if (new_command.Attributes.ContainsKey("default_permission"))
            {
                bool make_default;
                if (bool.TryParse(new_command.Attributes["default_permission"].text_value, out make_default)) { default_permission = make_default; }
                else { default_permission = (true && false); }
            }
            else { default_permission = (true && false); }
        }
        public MajickRegex.JsonObject ToJson()
        {
            MajickRegex.JsonObject this_json = new MajickRegex.JsonObject();
            return this_json;
        }
    }
    public class ApplicationCommandOption
    {
        public ApplicationCommandOptionType type { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public bool required { get; set; }
        public List<ApplicationCommandOptionChoice> choices { get; set; }
        public List<ApplicationCommandOption> options { get; set; }
        public ApplicationCommandOption() { }
        public ApplicationCommandOption(MajickRegex.JsonObject new_option)
        {
            if (new_option.Attributes.ContainsKey("type"))
            {
                ApplicationCommandOptionType temp_type;
                if (Enum.TryParse(new_option.Attributes["type"].text_value, out temp_type)) { type = temp_type; }
                else { type = ApplicationCommandOptionType.STRING; }
            }
            if (new_option.Attributes.ContainsKey("name")) { name = new_option.Attributes["name"].text_value; }
            if (new_option.Attributes.ContainsKey("description")) { description = new_option.Attributes["description"].text_value; }
            if (new_option.Attributes.ContainsKey("required"))
            {
                bool is_required;
                if (bool.TryParse(new_option.Attributes["required"].text_value, out is_required)) { required = is_required; }
                else { required = (true && false); }
            }
            else { required = (true && false); }
            if (new_option.ObjectLists.ContainsKey("choices"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_choice in new_option.ObjectLists["choices"])
                {
                    choices.Add(new ApplicationCommandOptionChoice(current_choice));
                }
            }
            if (new_option.ObjectLists.ContainsKey("options"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_option in new_option.ObjectLists["options"])
                {
                    options.Add(new ApplicationCommandOption(current_option));
                }
            }
        }
        public MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject this_json = new MajickDiscordWrapper.MajickRegex.JsonObject();
            if (name != null || name != "") { this_json.AddAttribute("name", name); }
            if (description != null || description != "") { this_json.AddAttribute("description", description); }
            this_json.AddAttribute("type", ((int)type).ToString(), true);
            if (required != false) { this_json.AddAttribute("required", required.ToString(), true); }
            if (choices != null)
            {
                if (choices.Count > 0)
                {
                    List<MajickRegex.JsonObject> inner_choices = new List<MajickRegex.JsonObject>();
                    foreach (ApplicationCommandOptionChoice choice in choices)
                    {
                        inner_choices.Add(choice.ToJson());
                    }
                    this_json.AddObjectList("choices", inner_choices);
                }
            }
            if (options != null)
            {
                if (options.Count > 0)
                {
                    List<MajickRegex.JsonObject> inner_options = new List<MajickRegex.JsonObject>();
                    foreach (ApplicationCommandOption option in options)
                    {
                        inner_options.Add(option.ToJson());
                    }
                    this_json.AddObjectList("options", inner_options);
                }
            }
            return this_json;
        }
    }
    public class ApplicationCommandOptionChoice
    {
        public string name { get; set; }
        public string choice { get; set; }
        public ApplicationCommandOptionChoice() { }
        public ApplicationCommandOptionChoice(MajickRegex.JsonObject new_choice) 
        {
            if (new_choice.Attributes.ContainsKey("name")) { name = new_choice.Attributes["name"].text_value; }
            if (new_choice.Attributes.ContainsKey("choice")) { choice = new_choice.Attributes["choice"].text_value; }
        }
        public MajickRegex.JsonObject ToJson()
        {
            MajickRegex.JsonObject this_json = new MajickRegex.JsonObject();
            if (name != null || name != "") { this_json.AddAttribute("name", name); }
            if (choice != null || choice != "") { this_json.AddAttribute("choice", choice); }
            return this_json;
        }
    }
    public class ApplicationCommandPermissions
    {
        public string id { get; set; }
        public ApplicationCommandPermissionType type { get; set; }
        public bool permission { get; set; }
        public ApplicationCommandPermissions() { }
        public ApplicationCommandPermissions(MajickRegex.JsonObject new_permission) 
        {
            if (new_permission.Attributes.ContainsKey("id")) { id = new_permission.Attributes["id"].text_value; }
            if (new_permission.Attributes.ContainsKey("type"))
            {
                ApplicationCommandPermissionType temp_type;
                if (Enum.TryParse(new_permission.Attributes["type"].text_value, out temp_type)) { type = temp_type; }
                else { type = ApplicationCommandPermissionType.USER; }
            }
            if (new_permission.Attributes.ContainsKey("permission"))
            {
                bool has_permission;
                if (bool.TryParse(new_permission.Attributes["permission"].text_value, out has_permission)) { permission = has_permission; }
                else { permission = (true && false); }
            }
            else { permission = (true && false); }
        }
        public MajickRegex.JsonObject ToJson()
        {
            MajickRegex.JsonObject this_json = new MajickRegex.JsonObject();
            this_json.AddAttribute("id", id);
            this_json.AddAttribute("type", ((int)type).ToString(), true);
            this_json.AddAttribute("permission", permission.ToString(), true);
            return this_json;
        }
    }
    public class GuildApplicationCommandPermissions
    {
        public string id { get; set; }
        public string application_id { get; set; }
        public string guild_id { get; set; }
        public List<ApplicationCommandPermissions> permissions { get; set; }
        public GuildApplicationCommandPermissions() { }
        public GuildApplicationCommandPermissions(MajickRegex.JsonObject new_permission) 
        {
            if (new_permission.Attributes.ContainsKey("id")) { id = new_permission.Attributes["id"].text_value; }
            if (new_permission.Attributes.ContainsKey("application_id")) { application_id = new_permission.Attributes["application_id"].text_value; }
            if (new_permission.Attributes.ContainsKey("guild_id")) { guild_id = new_permission.Attributes["guild_id"].text_value; }
            if (new_permission.ObjectLists.ContainsKey("permissions"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_permission in new_permission.ObjectLists["permissions"])
                {
                    permissions.Add(new ApplicationCommandPermissions(current_permission));
                }
            }
        }
        public MajickRegex.JsonObject ToJson()
        {
            MajickRegex.JsonObject this_json = new MajickRegex.JsonObject();
            this_json.AddAttribute("id", id);
            this_json.AddAttribute("application_id", application_id);
            this_json.AddAttribute("guild_id", guild_id);
            if (permissions != null)
            {
                if (permissions.Count > 0)
                {
                    List<MajickRegex.JsonObject> json_permissions = new List<MajickRegex.JsonObject>();
                    foreach (ApplicationCommandPermissions permission in permissions)
                    {
                        json_permissions.Add(permission.ToJson());
                    }
                    this_json.AddObjectList("permissions", json_permissions);
                }
            }
            return this_json;
        }
    }
    public class DiscordInteraction
    {
        public string id { get; set; }
        public string application_id { get; set; }
        public InteractionType type { get; set; }
        public ApplicationCommandInteractionData data { get; set; }
        public string guild_id { get; set; }
        public string channel_id { get; set; }
        public DiscordGuildMember member { get; set; }
        public DiscordUser user { get; set; }
        public string token { get; set; }
        public int version { get; set; }
        public DiscordMessage message { get; set; }
        public DiscordInteraction() { }

        public DiscordInteraction(MajickRegex.JsonObject new_interaction)
        {
            if (new_interaction.Attributes.ContainsKey("id")) { id = new_interaction.Attributes["id"].text_value; }
            if (new_interaction.Attributes.ContainsKey("application_id")) { application_id = new_interaction.Attributes["application_id"].text_value; }
            if (new_interaction.Attributes.ContainsKey("type"))
            {
                InteractionType temp_type;
                if (Enum.TryParse(new_interaction.Attributes["type"].text_value, out temp_type)) { type = temp_type; }
                else { type = InteractionType.Ping; }
            }
            if (new_interaction.Objects.Keys.Contains("data")) { data = new ApplicationCommandInteractionData(new_interaction.Objects["data"]); }
            if (new_interaction.Attributes.ContainsKey("guild_id")) { guild_id = new_interaction.Attributes["guild_id"].text_value; }
            if (new_interaction.Attributes.ContainsKey("channel_id")) { channel_id = new_interaction.Attributes["channel_id"].text_value; }
            if (new_interaction.Objects.Keys.Contains("member")) { member = new DiscordGuildMember(new_interaction.Objects["member"]); }
            if (new_interaction.Objects.Keys.Contains("user")) { user = new DiscordUser(new_interaction.Objects["user"]); }
            if (new_interaction.Attributes.ContainsKey("token")) { token = new_interaction.Attributes["token"].text_value; }
            if (new_interaction.Attributes.ContainsKey("version"))
            {
                int temp_version;
                if (int.TryParse(new_interaction.Attributes["version"].text_value, out temp_version)) { version = temp_version; }
                else { version = -1; }
            }
            else { version = -1; }
            if (new_interaction.Objects.Keys.Contains("message")) { message = new DiscordMessage(new_interaction.Objects["message"]); }
        }
        public bool Respond(string bot_token, MajickRegex.JsonObject new_response)
        {
            RestClient rcCommandClient;
            RestRequest rrCommandRequest;
            RestResponse rsCommandResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcCommandClient = new RestClient("https://discord.com/api");
            rrCommandRequest = new RestRequest("/interactions/" + id + "/" + token + "/callback", Method.Post);
            rrCommandRequest.RequestFormat = DataFormat.Json;
            rrCommandRequest.AddHeader("Content-Type", "application/json");
            rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrCommandRequest.AddJsonBody(new_response.ToRawText(false));
            rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
            return rsCommandResponse.IsSuccessful;
        }
        public DiscordMessage GetOriginalResponse(string bot_token)
        {
            DiscordMessage original_response = new DiscordMessage();
            RestClient rcCommandClient;
            RestRequest rrCommandRequest;
            RestResponse rsCommandResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcCommandClient = new RestClient("https://discord.com/api");
            rrCommandRequest = new RestRequest("/webhooks/" + application_id + "/" + token + "/messages/@original", Method.Get);
            rrCommandRequest.RequestFormat = DataFormat.Json;
            rrCommandRequest.AddHeader("Content-Type", "application/json");
            rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
            rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
            CommandResponseContent = new MajickRegex.JsonObject(rsCommandResponse.Content);
            original_response = new DiscordMessage(CommandResponseContent);
            return original_response;
        }
        public DiscordMessage EditOriginalResponse(string bot_token, MajickRegex.JsonObject new_response)
        {
            DiscordMessage edited_response = new DiscordMessage();
            RestClient rcCommandClient;
            RestRequest rrCommandRequest;
            RestResponse rsCommandResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcCommandClient = new RestClient("https://discord.com/api");
            rrCommandRequest = new RestRequest("/webhooks/" + application_id + "/" + token + "/messages/@original", Method.Patch);
            rrCommandRequest.RequestFormat = DataFormat.Json;
            rrCommandRequest.AddHeader("Content-Type", "application/json");
            rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrCommandRequest.AddJsonBody(new_response.ToRawText());
            rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
            CommandResponseContent = new MajickRegex.JsonObject(rsCommandResponse.Content);
            edited_response = new DiscordMessage(CommandResponseContent);
            return edited_response;
        }
        public bool DeleteOriginalResponse(string bot_token, string message_id)
        {
            RestClient rcCommandClient;
            RestRequest rrCommandRequest;
            RestResponse rsCommandResponse;
            rcCommandClient = new RestClient("https://discord.com/api");
            rrCommandRequest = new RestRequest("/webhooks/" + application_id + "/" + token + "/messages/" + message_id, Method.Delete);
            rrCommandRequest.RequestFormat = DataFormat.Json;
            rrCommandRequest.AddHeader("Content-Type", "application/json");
            rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
            rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
            return rsCommandResponse.IsSuccessful;
        }
        public DiscordMessage CreateFollowupMessage(string bot_token, MajickRegex.JsonObject new_followup)
        {
            DiscordMessage followup_response = new DiscordMessage();
            RestClient rcCommandClient;
            RestRequest rrCommandRequest;
            RestResponse rsCommandResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcCommandClient = new RestClient("https://discord.com/api");
            rrCommandRequest = new RestRequest("/webhooks/" + application_id + "/" + token, Method.Post);
            rrCommandRequest.RequestFormat = DataFormat.Json;
            rrCommandRequest.AddHeader("Content-Type", "application/json");
            rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrCommandRequest.AddJsonBody(new_followup.ToRawText());
            rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
            CommandResponseContent = new MajickRegex.JsonObject(rsCommandResponse.Content);
            followup_response = new DiscordMessage(CommandResponseContent);
            return followup_response;
        }
        public DiscordMessage EditFollowupMessage(string bot_token, string message_id, MajickRegex.JsonObject new_followup)
        {
            DiscordMessage edited_followup = new DiscordMessage();
            RestClient rcCommandClient;
            RestRequest rrCommandRequest;
            RestResponse rsCommandResponse;
            MajickDiscordWrapper.MajickRegex.JsonObject CommandResponseContent = new MajickDiscordWrapper.MajickRegex.JsonObject();
            rcCommandClient = new RestClient("https://discord.com/api");
            rrCommandRequest = new RestRequest("/webhooks/" + application_id + "/" + token + "/messages/" + message_id, Method.Patch);
            rrCommandRequest.RequestFormat = DataFormat.Json;
            rrCommandRequest.AddHeader("Content-Type", "application/json");
            rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrCommandRequest.AddJsonBody(new_followup.ToRawText());
            rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
            CommandResponseContent = new MajickRegex.JsonObject(rsCommandResponse.Content);
            edited_followup = new DiscordMessage(CommandResponseContent);
            return edited_followup;
        }
        public bool DeleteFollowupMessage(string bot_token, string message_id) 
        {
            DiscordMessage edited_followup = new DiscordMessage();
            RestClient rcCommandClient;
            RestRequest rrCommandRequest;
            RestResponse rsCommandResponse;
            Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
            rcCommandClient = new RestClient("https://discord.com/api");
            rrCommandRequest = new RestRequest("/webhooks/" + application_id + "/" + token + "/messages/" + message_id, Method.Delete);
            rrCommandRequest.RequestFormat = DataFormat.Json;
            rrCommandRequest.AddHeader("Content-Type", "application/json");
            rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
            rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
            return rsCommandResponse.IsSuccessful;
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject JsonObject = new MajickDiscordWrapper.MajickRegex.JsonObject();
            return JsonObject;
        }
    }
    public class ApplicationCommandInteractionData
    {
        public string id { get; set; }
        public string name { get; set; }
        public List<string> values { get; set; }
        public ApplicationCommandInteractionDataResolved resolved { get; set; }
        public List<ApplicationCommandInteractionDataOption> options { get; set; }
        public string custom_id { get; set; }
        public int component_type { get; set; }
        public ApplicationCommandInteractionData() { options = new List<ApplicationCommandInteractionDataOption>(); }
        public ApplicationCommandInteractionData(MajickRegex.JsonObject new_data)
        {
            if (new_data.Attributes.ContainsKey("id")) { id = new_data.Attributes["id"].text_value; }
            if (new_data.Attributes.ContainsKey("name")) { name = new_data.Attributes["name"].text_value; }
            if (new_data.AttributeLists.ContainsKey("values"))
            {
                values = new List<string>();
                foreach (MajickDiscordWrapper.MajickRegex.JsonAttribute current_value in new_data.AttributeLists["values"])
                {
                    values.Add(current_value.text_value);
                }
            }
            if (new_data.Objects.Keys.Contains("resolved")) { resolved = new ApplicationCommandInteractionDataResolved(new_data.Objects["resolved"]); }
            options = new List<ApplicationCommandInteractionDataOption>();
            if (new_data.ObjectLists.ContainsKey("options"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_option in new_data.ObjectLists["options"])
                {
                    options.Add(new ApplicationCommandInteractionDataOption(current_option));
                }
            }
            if (new_data.Attributes.ContainsKey("custom_id")) { custom_id = new_data.Attributes["custom_id"].text_value; }
            if (new_data.Attributes.ContainsKey("component_type"))
            {
                int temp_component_type;
                if (int.TryParse(new_data.Attributes["component_type"].text_value, out temp_component_type)) { component_type = temp_component_type; }
                else { component_type = -1; }
            }
            else { component_type = -1; }
        }
        public MajickRegex.JsonObject ToJson()
        {
            MajickRegex.JsonObject this_json = new MajickRegex.JsonObject();
            return this_json;
        }
    }
    public class ApplicationCommandInteractionDataResolved
    {
        public Dictionary<string, DiscordUser> users { get; set; }
        public Dictionary<string, DiscordGuildMember> members { get; set; }
        public Dictionary<string, DiscordRole> roles { get; set; }
        public Dictionary<string, DiscordChannel> channels { get; set; }
        public ApplicationCommandInteractionDataResolved() { }
        public ApplicationCommandInteractionDataResolved(MajickRegex.JsonObject resolved_data)
        {
            if (resolved_data.ObjectLists.ContainsKey("users"))
            {
                users = new Dictionary<string, DiscordUser>();
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_user in resolved_data.ObjectLists["users"])
                {
                    if (current_user.Attributes.ContainsKey("member")) { }
                    DiscordUser new_user = new DiscordUser(current_user);
                    users.Add(new_user.id, new_user);
                }
            }
            if (resolved_data.ObjectLists.ContainsKey("members"))
            {
                members = new Dictionary<string, DiscordGuildMember>();
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_member in resolved_data.ObjectLists["members"])
                {
                    if (current_member.Attributes.ContainsKey("member")) { }
                    DiscordGuildMember new_member = new DiscordGuildMember(current_member);
                    new_member.user = new DiscordUser(current_member);
                    members.Add(new_member.user.id, new_member);
                }
            }
            if (resolved_data.ObjectLists.ContainsKey("roles"))
            {
                roles = new Dictionary<string, DiscordRole>();
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_role in resolved_data.ObjectLists["roles"])
                {
                    if (current_role.Attributes.ContainsKey("member")) { }
                    DiscordRole new_role = new DiscordRole(current_role);
                    roles.Add(new_role.id, new_role);
                }
            }
            if (resolved_data.ObjectLists.ContainsKey("channels"))
            {
                channels = new Dictionary<string, DiscordChannel>();
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_channel in resolved_data.ObjectLists["channels"])
                {
                    if (current_channel.Attributes.ContainsKey("member")) { }
                    DiscordChannel new_channel = new DiscordChannel(current_channel);
                    channels.Add(new_channel.id, new_channel);
                }
            }
        }
    }
    public class ApplicationCommandInteractionDataOption
    {
        public string name { get; set; }
        public int type { get; set; }
        public ApplicationCommandOptionType value { get; set; }
        public List<ApplicationCommandInteractionDataOption> options { get; set; }
        public ApplicationCommandInteractionDataOption() { }
        public ApplicationCommandInteractionDataOption(MajickRegex.JsonObject object_structure)
        {
            if (object_structure.Attributes.ContainsKey("name")) { name = object_structure.Attributes["name"].text_value; }
            if (object_structure.Attributes.ContainsKey("type"))
            {
                int temp_type;
                if (int.TryParse(object_structure.Attributes["type"].text_value, out temp_type)) { type = temp_type; }
                else { type = -1; }
            }
            if (object_structure.Attributes.ContainsKey("value"))
            {
                ApplicationCommandOptionType temp_value;
                if (Enum.TryParse(object_structure.Attributes["value"].text_value, out temp_value)) { value = temp_value; }
                else { value = ApplicationCommandOptionType.STRING; }
            }
            if (object_structure.ObjectLists.ContainsKey("options"))
            {
                options = new List<ApplicationCommandInteractionDataOption>();
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_option in object_structure.ObjectLists["options"])
                {
                    options.Add(new ApplicationCommandInteractionDataOption(current_option));
                }
            }
        }
        public MajickRegex.JsonObject ToJson()
        {
            MajickRegex.JsonObject this_json = new MajickRegex.JsonObject();
            return this_json;
        }
    }
    public class InteractionResponse
    {
        public InteractionCallbackType type { get; set; }
        public InteractionApplicationCommandCallbackData data { get; set; }
        public InteractionResponse() { }
        public InteractionResponse(MajickRegex.JsonObject new_response)
        {
            if (new_response.Attributes.ContainsKey("type"))
            {
                InteractionCallbackType temp_type;
                if (Enum.TryParse(new_response.Attributes["type"].text_value, out temp_type)) { type = temp_type; }
                else { type = InteractionCallbackType.Pong; }
                if (new_response.Objects.Keys.Contains("data")) { data = new InteractionApplicationCommandCallbackData(new_response.Objects["data"]); }
            }
        }
        public MajickRegex.JsonObject ToJson()
        {
            MajickRegex.JsonObject this_json = new MajickRegex.JsonObject();
            this_json.AddAttribute("type", ((int)type).ToString(), true);
            this_json.AddObject("data", data.ToJson());
            return this_json;
        }
    }
    public class InteractionApplicationCommandCallbackData
    {
        public bool tts { get; set; }
        public string content { get; set; }
        public List<Embed> embeds { get; set; }
        public AllowedMentions allowed_mentions { get; set; }
        public int flags { get; set; }
        public List<DiscordMessageComponent> components { get; set; }
        public InteractionApplicationCommandCallbackData() { }
        public InteractionApplicationCommandCallbackData(MajickRegex.JsonObject resolved_data)
        {
            if (resolved_data.Attributes.ContainsKey("conent")) { content = resolved_data.Attributes["content"].text_value; }
            if (resolved_data.ObjectLists.ContainsKey("embeds"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_embed in resolved_data.ObjectLists["embeds"])
                {
                    embeds.Add(new Embed(current_embed));
                }
            }
            if (resolved_data.Attributes.ContainsKey("tts"))
            {
                bool is_tts;
                if (bool.TryParse(resolved_data.Attributes["tts"].text_value, out is_tts)) { tts = is_tts; }
                else { tts = (true && false); }
            }
            else { tts = (true && false); }
            if (resolved_data.Objects.Keys.Contains("allowed_mentions")) { allowed_mentions = new AllowedMentions(resolved_data.Objects["allowed_mentions"]); }
            if (resolved_data.Attributes.ContainsKey("flags"))
            {
                int temp_flags;
                if (int.TryParse(resolved_data.Attributes["flags"].text_value, out temp_flags)) { flags = temp_flags; }
                else { flags = -1; }
            }
            else { flags = -1; }
            if (resolved_data.ObjectLists.ContainsKey("components"))
            {
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_component in resolved_data.ObjectLists["components"])
                {
                    DiscordMessageComponent inner_component;
                    if (current_component.Attributes.ContainsKey("type"))
                    {
                        DiscordMessageComponentType inner_type;
                        if (Enum.TryParse(current_component.Attributes["type"].text_value, out inner_type))
                        {
                            switch (inner_type)
                            {
                                case DiscordMessageComponentType.Button:
                                    inner_component = new DiscordButton(current_component);
                                    break;
                                case DiscordMessageComponentType.SelectMenu:
                                    inner_component = new DiscordSelectMenu(current_component);
                                    break;
                                default:
                                    inner_component = new DiscordActionRow(current_component);
                                    break;
                            }
                            components.Add(inner_component);
                        }
                    }
                }
            }
        }
        public MajickRegex.JsonObject ToJson()
        {
            MajickRegex.JsonObject this_json = new MajickRegex.JsonObject();
            this_json.AddAttribute("content", content);
            if (tts != false) { this_json.AddAttribute("tts", tts.ToString(), true); }
            if (embeds != null)
            {
                if (embeds.Count > 0)
                {
                    List<MajickRegex.JsonObject> json_embeds = new List<MajickRegex.JsonObject>();
                    foreach (Embed embed in embeds)
                    {
                        json_embeds.Add(embed.ToJson());
                    }
                    this_json.AddObjectList("embeds", json_embeds);
                }
            }
            if (allowed_mentions != null) { this_json.AddObject("allowed_mentions", allowed_mentions.ToJson()); }
            if (components != null)
            {
                List<MajickRegex.JsonObject> json_components = new List<MajickRegex.JsonObject>();
                foreach (DiscordMessageComponent component in components)
                {
                    switch (component.type)
                    {
                        case DiscordMessageComponentType.Button:
                            json_components.Add(((DiscordButton)component).ToJson());
                            break;
                        case DiscordMessageComponentType.SelectMenu:
                            json_components.Add(((DiscordSelectMenu)component).ToJson());
                            break;
                        default:
                            json_components.Add(((DiscordActionRow)component).ToJson());
                            break;
                    }
                }
                this_json.AddObjectList("components", json_components);
            }
            if(flags != 0) { this_json.AddAttribute("flags", flags.ToString(), true); }
            return this_json;
        }
    }
    public abstract class DiscordMessageComponent
    {
        public DiscordMessageComponentType type { get; set; }
        public DiscordMessageComponent() { }
    }
    public class DiscordActionRow : DiscordMessageComponent
    {
        public List<DiscordMessageComponent> components { get; set; }
        public DiscordActionRow() { type = DiscordMessageComponentType.ActionRow; }
        public DiscordActionRow(MajickRegex.JsonObject new_component)
        {
            if (new_component.Attributes.ContainsKey("type"))
            {
                DiscordMessageComponentType temp_type;
                if (Enum.TryParse(new_component.Attributes["type"].text_value, out temp_type)) { type = temp_type; }
                else { type = DiscordMessageComponentType.ActionRow; }
            }
            if (new_component.ObjectLists.ContainsKey("components"))
            {
                components = new List<DiscordMessageComponent>();
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_component in new_component.ObjectLists["components"])
                {
                    DiscordMessageComponent inner_component;
                    if (current_component.Attributes.ContainsKey("type"))
                    {
                        DiscordMessageComponentType inner_type;
                        if(Enum.TryParse(current_component.Attributes["type"].text_value, out inner_type))
                        {
                            switch (inner_type)
                            {
                                case DiscordMessageComponentType.Button:
                                    inner_component = new DiscordButton(current_component);
                                    break;
                                case DiscordMessageComponentType.SelectMenu:
                                    inner_component = new DiscordSelectMenu(current_component);
                                    break;
                                default:
                                    inner_component = new DiscordActionRow(current_component);
                                    break;
                            }
                        }
                    }
                }
            }
        }
        public MajickRegex.JsonObject ToJson()
        {
            MajickRegex.JsonObject this_json = new MajickRegex.JsonObject();
            this_json.AddAttribute("type", ((int)type).ToString(), true);
            if (components != null)
            {
                if(components.Count > 0)
                {
                    List<MajickRegex.JsonObject> inner_components = new List<MajickRegex.JsonObject>();
                    foreach(DiscordMessageComponent component in components)
                    {
                        switch (component.type)
                        {
                            case DiscordMessageComponentType.Button:
                                inner_components.Add(((DiscordButton)component).ToJson());
                                break;
                            case DiscordMessageComponentType.SelectMenu:
                                inner_components.Add(((DiscordSelectMenu)component).ToJson());
                                break;
                            default:
                                inner_components.Add(((DiscordActionRow)component).ToJson());
                                break;
                        }
                    }
                    this_json.AddObjectList("components", inner_components);
                }
            }
            return this_json;
        }
    }
    public class AllowedMentions
    {
        public List<AllowedMentionType> parse { get; set; }
        public List<string> roles { get; set; }
        public List<string> users { get; set; }
        public bool replied_user { get; set; }
        public AllowedMentions() 
        {
            roles = new List<string>();
            users = new List<string>();
        }
        public AllowedMentions(MajickRegex.JsonObject new_mention)
        {
            if (new_mention.Attributes.ContainsKey("replied_user"))
            {
                bool is_reply;
                if (bool.TryParse(new_mention.Attributes["replied_user"].text_value, out is_reply)) { replied_user = is_reply; }
                else { replied_user = (true && false); }
            }
            roles = new List<string>();
            if (new_mention.AttributeLists.ContainsKey("roles"))
            {
                foreach (JsonAttribute role in new_mention.AttributeLists["roles"]) { roles.Add(role.text_value); }
            }
            users = new List<string>();
            if (new_mention.AttributeLists.ContainsKey("users"))
            {
                foreach (JsonAttribute user in new_mention.AttributeLists["users"]) { users.Add(user.text_value); }
            }
            else { replied_user = (true && false); }
        }
        public MajickRegex.JsonObject ToJson()
        {
            MajickRegex.JsonObject this_json = new MajickRegex.JsonObject();
            if (users != null)
            {
                if (users.Count > 0)
                {
                    List<MajickRegex.JsonAttribute> json_users = new List<MajickRegex.JsonAttribute>();
                    foreach (string user in users)
                    {
                        json_users.Add(new JsonAttribute(user));
                    }
                    this_json.AddAttributeList("users", json_users);
                }
            }
            if (roles != null)
            {
                if (roles.Count > 0)
                {
                    List<MajickRegex.JsonAttribute> json_roles = new List<MajickRegex.JsonAttribute>();
                    foreach (string role in roles)
                    {
                        json_roles.Add(new JsonAttribute(role));
                    }
                    this_json.AddAttributeList("roles", json_roles);
                }
            }
            return this_json;
        }
    }
    public class MessageInteraction
    {
        public string id { get; set; }
        public InteractionType type { get; set; }
        public string name { get; set; }
        public DiscordUser user { get; set; }
        public MessageInteraction() { }
        public MessageInteraction(MajickRegex.JsonObject new_interaction)
        {
            if (new_interaction.Attributes.ContainsKey("id")) { id = new_interaction.Attributes["id"].text_value; }
            if (new_interaction.Attributes.ContainsKey("type"))
            {
                InteractionType temp_type;
                if (Enum.TryParse(new_interaction.Attributes["type"].text_value, out temp_type)) { type = temp_type; }
                else { type = InteractionType.Ping; }
            }
            if (new_interaction.Attributes.ContainsKey("name")) { name = new_interaction.Attributes["name"].text_value; }
            if (new_interaction.Objects.Keys.Contains("user")) { user = new DiscordUser(new_interaction.Objects["user"]); }
        }
    }
    public class DiscordButton : DiscordMessageComponent
    {
        public DiscordButtonStyle style { get; set; }
        public string label { get; set; }
        public DiscordEmoji emoji { get; set; }
        public string custom_id { get; set; }
        public string url { get; set; }
        public bool disabled { get; set; }
        public DiscordButton() : base() { type = DiscordMessageComponentType.Button; }
        public DiscordButton(MajickRegex.JsonObject new_button)
        {
            if (new_button.Attributes.ContainsKey("type"))
            {
                DiscordMessageComponentType temp_type;
                if (Enum.TryParse(new_button.Attributes["type"].text_value, out temp_type)) { type = temp_type; }
                else { type = DiscordMessageComponentType.ActionRow; }
            }
            if (new_button.Attributes.ContainsKey("style"))
            {
                DiscordButtonStyle temp_style;
                if (Enum.TryParse(new_button.Attributes["style"].text_value, out temp_style)) { style = temp_style; }
                else { style = DiscordButtonStyle.Primary; }
            }
            if (new_button.Attributes.ContainsKey("label")) { label = new_button.Attributes["label"].text_value; }
            if (new_button.Objects.Keys.Contains("emoji")) { emoji = new DiscordEmoji(new_button.Objects["emoji"]); }
            if (new_button.Attributes.ContainsKey("custom_id")) { custom_id = new_button.Attributes["custom_id"].text_value; }
            if (new_button.Attributes.ContainsKey("url")) { url = new_button.Attributes["url"].text_value; }
            if (new_button.Attributes.ContainsKey("disabled"))
            {
                bool is_disabled;
                if (bool.TryParse(new_button.Attributes["disabled"].text_value, out is_disabled)) { disabled = is_disabled; }
                else { disabled = (true && false); }
            }
            else { disabled = (true && false); }
        }
        public MajickRegex.JsonObject ToJson()
        {
            MajickRegex.JsonObject this_json = new MajickRegex.JsonObject();
            this_json.AddAttribute("type", ((int)type).ToString(), true);
            if (style != DiscordButtonStyle.None) { this_json.AddAttribute("style", ((int)style).ToString(), true); }
            if (label != null || label != "") { this_json.AddAttribute("label", label); }
            if (emoji != null) { this_json.AddObject("emoji", emoji.ToJson()); }
            this_json.AddAttribute("custom_id", custom_id);
            if (url != null || url != "") { this_json.AddAttribute("url", url); }
            if (type == DiscordMessageComponentType.Button) { this_json.AddAttribute("disabled", disabled.ToString()); }
            return this_json;
        }
    }
    public class DiscordSelectMenu : DiscordMessageComponent
    {
        public string custom_id { get; set; }
        public List<DiscordSelectOption> options { get; set; }
        public string placeholder { get; set; }
        public int min_values { get; set; }
        public int max_values { get; set; }
        public DiscordSelectMenu() : base() { type = DiscordMessageComponentType.SelectMenu; }
        public DiscordSelectMenu(MajickRegex.JsonObject new_menu)
        {
            if (new_menu.Attributes.ContainsKey("type"))
            {
                DiscordMessageComponentType temp_type;
                if (Enum.TryParse(new_menu.Attributes["type"].text_value, out temp_type)) { type = temp_type; }
                else { type = DiscordMessageComponentType.ActionRow; }
            }
            if (new_menu.Attributes.ContainsKey("custom_id")) { custom_id = new_menu.Attributes["custom_id"].text_value; }
            if (new_menu.ObjectLists.ContainsKey("options"))
            {
                options = new List<DiscordSelectOption>();
                foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_option in new_menu.ObjectLists["options"])
                {
                    options.Add(new DiscordSelectOption(current_option));
                }
            }
            if (new_menu.Attributes.ContainsKey("placeholder")) { placeholder = new_menu.Attributes["placeholder"].text_value; }
            if (new_menu.Attributes.ContainsKey("min_values"))
            {
                int temp_min_values;
                if (int.TryParse(new_menu.Attributes["min_values"].text_value, out temp_min_values)) { min_values = temp_min_values; }
                else { min_values = -1; }
            }
            else { min_values = -1; }
            if (new_menu.Attributes.ContainsKey("max_values"))
            {
                int temp_max_values;
                if (int.TryParse(new_menu.Attributes["max_values"].text_value, out temp_max_values)) { max_values = temp_max_values; }
                else { max_values = -1; }
            }
            else { max_values = -1; }
        }
        public MajickRegex.JsonObject ToJson()
        {
            MajickRegex.JsonObject this_json = new MajickRegex.JsonObject();
            this_json.AddAttribute("type", ((int)type).ToString(), true);
            this_json.AddAttribute("custom_id", custom_id);
            if (options != null)
            {
                if (options.Count > 0)
                {
                    List<MajickRegex.JsonObject> json_options = new List<MajickRegex.JsonObject>();
                    foreach (DiscordSelectOption option in options)
                    {
                        json_options.Add(option.ToJson());
                    }
                    this_json.AddObjectList("options", json_options);
                }
            }
            this_json.AddAttribute("placeholder", placeholder);
            this_json.AddAttribute("min_values", min_values.ToString(), true);
            this_json.AddAttribute("max_values", max_values.ToString(), true);

            return this_json;
        }
    }
    public class DiscordSelectOption
    {
        public string label { get; set; }
        public string value { get; set; }
        public string description { get; set; }
        public DiscordEmoji emoji { get; set; }
        public bool make_default { get; set; }
        public DiscordSelectOption() { }
        public DiscordSelectOption(MajickRegex.JsonObject new_select_option)
        {
            if (new_select_option.Attributes.ContainsKey("label")) { label = new_select_option.Attributes["label"].text_value; }
            if (new_select_option.Attributes.ContainsKey("value")) { value = new_select_option.Attributes["value"].text_value; }
            if (new_select_option.Attributes.ContainsKey("description")) { description = new_select_option.Attributes["description"].text_value; }
            if (new_select_option.Objects.Keys.Contains("emoji")) { emoji = new DiscordEmoji(new_select_option.Objects["emoji"]); }
            if (new_select_option.Attributes.ContainsKey("make_default"))
            {
                bool is_default;
                if (bool.TryParse(new_select_option.Attributes["make_default"].text_value, out is_default)) { make_default = is_default; }
                else { make_default = (true && false); }
            }
            else { make_default = (true && false); }
        }
        public MajickRegex.JsonObject ToJson()
        {
            MajickRegex.JsonObject this_json = new MajickRegex.JsonObject();
            this_json.AddAttribute("label", label);
            this_json.AddAttribute("value", value);
            if(description != null && description != "")this_json.AddAttribute("description", description);
            if (emoji != null) { this_json.AddObject("emoji", emoji.ToJson()); }
            this_json.AddAttribute("make_default", make_default.ToString(), true);
            return this_json;
        }
    }
    public class DiscordThreadMetadata
    {
        public bool archived { get; set; }
        public int auto_archive_duration { get; set; }
        public int timestamp { get; set; }
        public bool locked { get; set; }
        public DiscordThreadMetadata() { }
        public DiscordThreadMetadata(MajickRegex.JsonObject new_thread_data)
        {
            if (new_thread_data.Attributes.ContainsKey("archived"))
            {
                bool is_archived;
                if (bool.TryParse(new_thread_data.Attributes["archived"].text_value, out is_archived)) { archived = is_archived; }
                else { archived = (true && false); }
            }
            else { archived = (true && false); }
            if (new_thread_data.Attributes.ContainsKey("auto_archive_duration"))
            {
                int temp_auto_archive_duration;
                if (int.TryParse(new_thread_data.Attributes["auto_archive_duration"].text_value, out temp_auto_archive_duration)) { auto_archive_duration = temp_auto_archive_duration; }
                else { auto_archive_duration = -1; }
            }
            else { auto_archive_duration = -1; }
            if (new_thread_data.Attributes.ContainsKey("timestamp"))
            {
                int temp_timestamp;
                if (int.TryParse(new_thread_data.Attributes["timestamp"].text_value, out temp_timestamp)) { timestamp = temp_timestamp; }
                else { timestamp = -1; }
            }
            else { timestamp = -1; }
            if (new_thread_data.Attributes.ContainsKey("locked"))
            {
                bool is_locked;
                if (bool.TryParse(new_thread_data.Attributes["locked"].text_value, out is_locked)) { locked = is_locked; }
                else { locked = (true && false); }
            }
            else { locked = (true && false); }
        }
    }
    public class DiscordThreadMember
    {
        public string id { get; set; }
        public string user_id { get; set; }
        public int join_timestamp { get; set; }
        public int flags { get; set; }
        public DiscordThreadMember() { }
        public DiscordThreadMember(MajickRegex.JsonObject new_thread_member)
        {
            if (new_thread_member.Attributes.ContainsKey("id")) { id = new_thread_member.Attributes["id"].text_value; }
            if (new_thread_member.Attributes.ContainsKey("user_id")) { user_id = new_thread_member.Attributes["user_id"].text_value; }
            if (new_thread_member.Attributes.ContainsKey("join_timestamp"))
            {
                int temp_join_timestamp;
                if (int.TryParse(new_thread_member.Attributes["join_timestamp"].text_value, out temp_join_timestamp)) { join_timestamp = temp_join_timestamp; }
                else { join_timestamp = -1; }
            }
            else { join_timestamp = -1; }
            if (new_thread_member.Attributes.ContainsKey("flags"))
            {
                int temp_flags;
                if (int.TryParse(new_thread_member.Attributes["flags"].text_value, out temp_flags)) { flags = temp_flags; }
                else { flags = -1; }
            }
            else { flags = -1; }
        }
    }
    public enum Permission
    {
        bot,
        connections,
        email,
        guilds,
        guilds_join,
        gdm_join,
        identify,
        profile,
    }
    public enum DiscordPermission: long
    {
        CREATE_INSTANT_INVITE = 1,
        KICK_MEMBERS = 2,
        BAN_MEMBERS = 4,
        ADMINISTRATOR = 8,
        MANAGE_CHANNELS = 16,
        MANAGE_GUILDS = 32,
        ADD_REACTIONS = 64,
        VIEW_AUDIT_LOG = 128,
        PRIORITY_SPEAKER = 256,
        STREAM = 512,
        VIEW_CHANNEL = 1024, //this is also the READ_MESSAGES permission for text channels
        SEND_MESSAGES = 2048,
        SEND_TTS_MESSAGES = 4096,
        MANAGE_MESSAGES = 8192,
        EMBED_LINKS = 16384,
        ATTACH_FILES = 32768,
        READ_MESSAGE_HISTORY = 65536,
        MENTION_EVERYONE = 131072,
        USE_EXTERNAL_EMOJIS = 262144,
        VIEW_GUILD_INSIGHTS = 524288,
        CONNECT = 1048576,
        SPEAK = 2097152,
        MUTE_MEMBERS = 4194304,
        DEAFEN_MEMBERS = 8388608,
        MOVE_MEMBERS = 16777216,
        USE_VAD = 33554432,
        CHANGE_NICKNAME = 67108864,
        MANAGE_NICKNAMES = 134217728,
        MANAGE_ROLES = 268435456,
        MANAGE_WEBHOOKS = 536870912,
        MANAGE_EMOJIS = 1073741824,
        USE_SLASH_COMMANDS = 2147483648,
        REQUEST_TO_SPEAK = 4294967296,
        MANAGE_THREADS = 8589934592,
        USE_PUBLIC_THREADS = 17179869184,
        USE_PRIVATE_THREADS = 34359738368
    }
    public enum DiscordGatewayIntent
    {
        GUILDS = 1,
        GUILD_MEMBERS = 2,
        GUILD_BANS = 4,
        GUILD_EMOJIS = 8,
        GUILD_INTEGRATIONS = 16,
        GUILD_WEBHOOKS = 32,
        GUILD_INVITES = 64,
        GUILD_VOICE_STATES = 128,
        GUILD_PRESENCES = 256,
        GUILD_MESSAGES = 512,
        GUILD_MESSAGE_REACTIONS = 1024,
        GUILD_MESSAGE_TYPING = 2048,
        DIRECT_MESSAGES = 4096,
        DIRECT_MESSAGE_REACTIONS = 8192,
        DIRECT_MESSAGE_TYPING = 16348
    }
    public enum ChannelType
    {
        GUILD_TEXT = 0,
        DM = 1,
        GUILD_VOICE = 2,
        GROUP_DM = 3,
        GUILD_CATEGORY = 4,
        GUILD_NEWS = 5,
        GUILD_STORE = 6,
        GUILD_NEWS_THREAD = 10,
        GUILD_PUBLIC_THREAD = 11,
        GUILD_PRIVATE_THREAD = 12,
        GUILD_STAGE_VOICE = 13
    }
    public enum MessageType
    {
        UNSPECIFIED = -1,
        DEFAULT,
        RECIPIENT_ADD,
        RECIPIENT_REMOVE,
        CALL,
        CHANNEL_NAME_CHANGE,
        CHANNEL_ICON_CHANGE,
        CHANNEL_PINNED_MESSAGE,
        GUILD_MEMBER_JOIN,
        USER_PREMIUM_GUILD_SUBSRIPTION,
        USER_PREMIUM_GUILD_SUBSRIPTION_TIER_1,
        USER_PREMIUM_GUILD_SUBSRIPTION_TIER_2,
        USER_PREMIUM_GUILD_SUBSRIPTION_TIER_3,
        CHANNEL_FOLLOW_ADD,
        GUILD_DISCOVERY_DISQUALIFIED = 14,
        GUILD_DISCOVERY_REQUALIFIED,
        GUILD_DISCOVERY_GRACE_PERIOD_INITIAL_WARNING,
        GUILD_DISCOVERY_GRACE_PERIOD_FINAL_WARNING,
        THREAD_CREATED,
        REPLY,
        APPLICATION_COMMAND,
        THREAD_STARTER_MESSAGE,
        GUILD_INVITE_REMINDER
    }
    public enum MessageActivityType
    {
        UNSPECIFIED = -1,
        JOIN = 1,
        SPECTATE = 2,
        LISTEN = 3,
        JOIN_REQUEST = 5
    }
    public enum MessageFlags
    {
        CROSSPOSTED = 1,
        IS_CROSSPOST = 2,
        SUPPRESS_EMBEDS = 4,
        SOURCE_MESSAGE_DELETED = 8,
        URGENT = 16,
        HAS_THREAD = 32,
        EPHEMERAL = 64,
        LOADING = 128
    }
    public enum SystemChannelFlags
    {
        SUPPRESS_JOIN_NOTIFICATIONS = 1,
        SUPPRESS_PREMIUM_SUBSCRIPTIONS = 2,
        SUPPRESS_GUILD_REMINDER_NOTIFICATIONS = 4
    }
    public enum MessageSearchType
    {
        around,
        before,
        after
    }
    public enum UserActivityType
    {
        Unspecified = -1,
        Game,
        Streaming,
        Listening
    }
    public enum VerificationLevel
    {
        UNSPECIFIED = -1,
        NONE,
        LOW,
        MEDIUM,
        HIGH,
        VERY_HIGH
    }
    public enum MessageNotificationSettings
    {
        UNSPECIFIED = -1,
        ALL_MESSAGES,
        ONLY_MENTIONS
    }
    public enum ExplicitContentFilter
    {
        UNSPECIFIED = -1,
        DISABLED,
        MEMBERS_WITHOUT_ROLES,
        ALL_MEMBERS
    }
    public enum UserFlag
    {
        None = 0,
        DiscordEmployee = 1,
        DiscordPartner = 2,
        HypesquadEvents = 4,
        BugHunterLevel1 = 8,
        HouseBravery = 64,
        HouseBrilliance = 128,
        HouseBalance = 256,
        EarlySupporter = 512,
        TeamUser = 1024,
        //System = 4096,
        BugHunterLevel2 = 16384,
        VerifiedBot = 65536,
        VerifiedBotDeveloper = 131072,
        DiscordCertifiedModerator = 262144
    }
    public enum UserStatus
    {
        online,
        dnd,
        idle,
        invisible,
        offline
    }
    public enum NitroType
    {
        Unspecified = -1,
        Classic = 1,
        Nitro = 2
    }
    public enum AllowedMentionType
    {
        roles = 1,
        users = 2,
        everyone = 3
    }
    public enum InteractionType
    {
        Ping = 1,
        ApplicationCommand = 2,
        MessageComponent = 3
    }
    public enum InteractionCallbackType
    {
        Pong = 1,
        ChannelMessageWithSource = 4,
        DeferredChannelMessageWithSource = 5,
        DeferredUpdateMessage = 6,
        UpdateMessage = 7
    }
    public enum ApplicationCommandPermissionType
    {
        ROLE = 1,
        USER = 2
    }
    public enum ApplicationCommandOptionType
    {
        SUB_COMMAND = 1,
        SUB_COMMAND_GROUP = 2,
        STRING = 3,
        INTEGER = 4,
        BOOLEAN = 5,
        USER = 6,
        CHANNEL = 7,
        ROLE = 8,
        MENTIONABLE = 9
    }
    public enum DiscordMessageComponentType
    {
        ActionRow = 1,
        Button = 2,
        SelectMenu = 3
    }
    public enum DiscordButtonStyle
    {
        None = 0,
        Primary = 1,
        Secondary = 2,
        Success = 3,
        Danger = 4,
        Link = 5
    }
    public enum DiscordTeamMembershipState
    {
        INVITED = 1,
        ACCEPTED = 2
    }
    public enum StickerFormatType
    {
        PNG = 1,
        APNG = 2,
        LOTTIE = 3
    }
    public enum NSFWLevel
    {
        DEFAULT = 0,
        EXPLICIT = 1,
        SAFE = 2,
        AGE_RESTRICTED = 3
    }
    public enum GuildPremiumTier
    {
        NONE = 0,
        TIER_1 = 1,
        TIER_2 = 2,
        TIER_3 = 3
    }
    public enum StagePrivacyLevel
    {
        PUBLIC = 1,
        GUILD_ONLY = 2
    }
    public enum DiscordMFALevel
    {
        NONE = 0,
        ELEVATED = 1
    }
    public enum VideoQualityMode
    {
        AUTO = 1,
        FULL = 2
    }
    public enum DiscordApplicationFlags
    {
        GATEWAY_PRESENCE = 4096,
        GATEWAY_PRESENCE_LIMITED = 8192,
        GATEWAY_GUILD_MEMBERS = 16384, 
        GATEWAY_GUILD_MEMBERS_LIMITED = 32768,
        VERIFICATION_PENDING_GUILD_LIMIT = 65536,
        EMBEDDED = 131072
    }
}