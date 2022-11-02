using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using RestSharp;
using System.Threading.Tasks;
using MajickDiscordWrapper.Discord;
using MajickDiscordWrapper.Discord.Gateway;
using System.Runtime.CompilerServices;

namespace MajickDiscordWrapper.WizardWare
{
    public class Wizard
    {
        public string UserID { get; set; }
        private DiscordClient _client;
        private Dictionary<string, WizardGuild> _guilds;
        public IReadOnlyDictionary<string, WizardGuild> Guilds { get { return _guilds; } }
        public delegate void OnGuildCreated(object sender, GuildCreatedEventArgs e);
        public event OnGuildCreated GuildCreated;
        public delegate void OnGuildDeleted(object sender, GuildDeletedEventArgs e);
        public event OnGuildDeleted GuildDeleted;
        public delegate void OnGuildUpdate(object sender, OnGuildUpdatedEventArgs e);
        public event OnGuildUpdate GuildUpdated;
        public delegate void OnChannelCreated(object sender, OnChannelCreateEventArgs e);
        public event OnChannelCreated ChannelCreated;
        public delegate void OnChannelDeleted(object sender, OnChannelDeleteEventArgs e);
        public event OnChannelDeleted ChannelDeleted;
        public delegate void OnChannelUpdated(object sender, ChannelUpdateEventArgs e);
        public event OnChannelUpdated ChannelUpdated;
        public delegate void OnUserUpdated(object sender, OnUserUpdateEventArgs e);
        public event OnUserUpdated UserUpdated;
        public delegate void OnUserBanned(object sender, OnUserBannedEventArgs e);
        public event OnUserBanned UserBanned;
        public delegate void OnUserUnbanned(object sender, OnUserUnbannedEventArgs e);
        public event OnUserUnbanned UserUnbanned;
        public delegate void OnGuildIntegrationsUpdate(object sender, GuildIntegrationsUpdateEventArgs e);
        public event OnGuildIntegrationsUpdate GuildIntegrationsUpdate;
        public delegate void OnGuildEmojisUpdate(object sender, GuildEmojisUpdateEventArgs e);
        public event OnGuildEmojisUpdate GuildEmojisUpdate;
        public delegate void OnGuildMemberAdded(object sender, OnMemberAddedEventArgs e);
        public event OnGuildMemberAdded GuildMemberAdded;
        public delegate void OnGuildMemberRemoved(object sender, OnMemberRemovedEventArgs e);
        public event OnGuildMemberRemoved GuildMemberRemoved;
        public delegate void OnGuildMemberUpdated(object sender, OnMemberUpdatedEventArgs e);
        public event OnGuildMemberUpdated GuildMemberUpdated;
        public delegate void OnGuildMembersChunk(object sender, GuildMembersChunkEventArgs e);
        public event OnGuildMembersChunk GuildMembersChunk;
        public delegate void OnInviteCreated(object sender, OnInviteCreatedEventArgs e);
        public event OnInviteCreated InviteCreated;
        public delegate void OnInviteDeleted(object sender, OnInviteDeletedEventArgs e);
        public event OnInviteDeleted InviteDeleted;
        public delegate void OnRoleCreate(object sender, OnRoleCreateEventArgs e);
        public event OnRoleCreate RoleCreated;
        public delegate void OnRoleDelete(object sender, OnRoleDeleteEventArgs e);
        public event OnRoleDelete RoleDeleted;
        public delegate void OnRoleUpdate(object sender, OnRoleUpdateEventArgs e);
        public event OnRoleUpdate RoleUpdated;
        public delegate void OnVoiceStateUpdated(object sender, VoiceStateUpdateEventArgs e);
        public event OnVoiceStateUpdated VoiceStateUpdated;
        public delegate void OnMessageReceived(object sender, MessageReceivedEventArgs e);
        public event OnMessageReceived MessageReceived;
        public delegate void OnPinsUpdated(object sender, ChannelPinsUpdateEventArgs e);
        public delegate void OnMessageUpdate(object sender, OnMessageUpdateEventArgs e);
        public event OnMessageUpdate MessageUpdated;
        public event OnPinsUpdated PinsUpdated;
        public delegate void OnMessageReactionAdded(object sender, OnReactionAddedEventArgs e);
        public event OnMessageReactionAdded MessageReactionAdded;
        public delegate void OnMessageReactionRemoved(object sender, OnReactionRemovedEventArgs e);
        public event OnMessageReactionRemoved MessageReactionRemoved;
        public delegate void OnMessageReactionCleared(object sender, OnReactionClearedEventArgs e);
        public event OnMessageReactionCleared MessageReactionCleared;
        public delegate void OnMessageReactionRemoveEmoji(object sender, OnReactionRemoveEmojiEventArgs e);
        public event OnMessageReactionRemoveEmoji MessageReactionRemoveEmoji;
        public delegate void OnWebhooksUpdated(object sender, OnWebhooksUpdateEventArgs e);
        public event OnWebhooksUpdated WebhooksUpdated;
        public delegate void OnVoiceServerUpdated(object sender, OnVoiceServerUpdateEventArgs e);
        public event OnVoiceServerUpdated VoiceServerUpdate;
        public delegate void OnMessageDeleted(object sender, MessageDeleteEventArgs e);
        public event OnMessageDeleted MessageDeleted;
        public delegate void OnMessagesBulkDeleted(object sender, OnMessageBulkDeletedEventArgs e);
        public event OnMessagesBulkDeleted MessagesBulkDeleted;
        public delegate void OnInteractionCreated(object sender, OnInteractionCreatedEventArgs e);
        public event OnInteractionCreated InteractionCreated;
        public Wizard(string client_id, string client_secret, string bot_token, List<DiscordGatewayIntent> intents)
        {
            _guilds = new Dictionary<string, WizardGuild>();
            _client = new DiscordClient(client_id, client_secret, bot_token);
            _client.GetBotGateway(intents);
            _client.ConnectAsync();
            _client.Gateway.GUILD_CREATE += Gateway_GUILD_CREATE;
            _client.Gateway.GUILD_DELETE += Gateway_GUILD_DELETE;
            _client.Gateway.CHANNEL_CREATE += Gateway_CHANNEL_CREATE;
            _client.Gateway.CHANNEL_DELETE += Gateway_CHANNEL_DELETE;
            _client.Gateway.GUILD_BAN_ADD += Gateway_GUILD_BAN_ADD;
            _client.Gateway.GUILD_BAN_REMOVE += Gateway_GUILD_BAN_REMOVE;
            _client.Gateway.GUILD_EMOJIS_UPDATE += Gateway_GUILD_EMOJIS_UPDATE;
            _client.Gateway.GUILD_INTEGRATIONS_UPDATE += Gateway_GUILD_INTEGRATIONS_UPDATE;
            _client.Gateway.GUILD_MEMBERS_CHUNK += Gateway_GUILD_MEMBERS_CHUNK;
            _client.Gateway.GUILD_MEMBER_ADD += Gateway_GUILD_MEMBER_ADD;
            _client.Gateway.GUILD_MEMBER_REMOVE += Gateway_GUILD_MEMBER_REMOVE;
            _client.Gateway.GUILD_MEMBER_UPDATE += Gateway_GUILD_MEMBER_UPDATE;
            _client.Gateway.GUILD_ROLE_CREATE += Gateway_GUILD_ROLE_CREATE;
            _client.Gateway.GUILD_ROLE_DELETE += Gateway_GUILD_ROLE_DELETE;
            _client.Gateway.INVITE_CREATE += Gateway_INVITE_CREATE;
            _client.Gateway.INVITE_DELETE += Gateway_INVITE_DELETE;
            _client.Gateway.MESSAGE_CREATE += Gateway_MESSAGE_CREATE;
            _client.Gateway.MESSAGE_DELETE += Gateway_MESSAGE_DELETE;
            _client.Gateway.MESSAGE_DELETE_BULK += Gateway_MESSAGE_DELETE_BULK;
            _client.Gateway.MESSAGE_REACTION_ADD += Gateway_MESSAGE_REACTION_ADD;
            _client.Gateway.MESSAGE_REACTION_REMOVE += Gateway_MESSAGE_REACTION_REMOVE;
            _client.Gateway.MESSAGE_REACTION_REMOVE_ALL += Gateway_MESSAGE_REACTION_REMOVE_ALL;
            _client.Gateway.MESSAGE_REACTION_REMOVE_EMOJI += Gateway_MESSAGE_REACTION_REMOVE_EMOJI;
            _client.Gateway.GUILD_UPDATE += Gateway_GUILD_UPDATE;
            _client.Gateway.CHANNEL_UPDATE += Gateway_CHANNEL_UPDATE;
            _client.Gateway.CHANNEL_PINS_UPDATE += Gateway_CHANNEL_PINS_UPDATE;
            _client.Gateway.GUILD_ROLE_UPDATE += Gateway_GUILD_ROLE_UPDATE;
            _client.Gateway.MESSAGE_UPDATE += Gateway_MESSAGE_UPDATE;
            _client.Gateway.PRESENCE_UPDATE += Gateway_PRESENCE_UPDATE;
            _client.Gateway.TYPING_START += Gateway_TYPING_START;
            _client.Gateway.USER_UPDATE += Gateway_USER_UPDATE;
            _client.Gateway.WEBHOOKS_UPDATE += Gateway_WEBHOOKS_UPDATE;
            _client.Gateway.VOICE_STATE_UPDATE += Gateway_VOICE_STATE_UPDATE;
            _client.Gateway.VOICE_SERVER_UPDATE += Gateway_VOICE_SERVER_UPDATE;
            _client.Gateway.INTERACTION_CREATE += Gateway_INTERACTION_CREATE;
            UserID = _client.Bot.id;
        }
        public void AddGuild(string guild_id, WizardGuild new_guild)
        {
            if (!_guilds.ContainsKey(guild_id)) { _guilds.Add(guild_id, new_guild); }
        }
        public void RemoveGuild(string guild_id)
        {
            if (_guilds.ContainsKey(guild_id)) { _guilds.Remove(guild_id); }
        }
        public void UpdateGuild(string guild_id, WizardGuild new_guild)
        {
            _guilds.Remove(guild_id);
            _guilds.Add(guild_id, new_guild);
        }
        public void Reconnect()
        {
            _client.ReconnectAsync();
        }

        public List<ApplicationCommand> GetGlobalCommands(string bot_token)
        {
            List<ApplicationCommand> global_commands = new List<ApplicationCommand>();
            RestClient rcCommandClient;
            RestRequest rrCommandRequest;
            RestResponse rsCommandResponse;
            MajickRegex.JsonObject CommandRequestBody = new MajickRegex.JsonObject();
            MajickRegex.JsonObject CommandResponseContent = new MajickRegex.JsonObject();
            Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
            rcCommandClient = new RestClient("https://discord.com/api");
            rrCommandRequest = new RestRequest("/applications/" + _client.ClientID + "/commands", Method.Get);
            rrCommandRequest.RequestFormat = DataFormat.Json;
            rrCommandRequest.AddHeader("Content-Type", "application/json");
            rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
            rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
            string command_array = "{\"commands\":" + rsCommandResponse.Content + "}";
            CommandResponseContent = new MajickRegex.JsonObject(command_array);
            foreach (MajickRegex.JsonObject current_command in CommandResponseContent.ObjectLists["commands"])
            {
                global_commands.Add(new ApplicationCommand(current_command));
            }
            return global_commands;
        }
        public List<ApplicationCommand> OverwriteGlobalCommands(string bot_token, MajickRegex.JsonObject new_commands)
        {
            List<ApplicationCommand> global_commands = new List<ApplicationCommand>();
            RestClient rcCommandClient;
            RestRequest rrCommandRequest;
            RestResponse rsCommandResponse;
            MajickRegex.JsonObject CommandRequestBody = new MajickRegex.JsonObject();
            MajickRegex.JsonObject CommandResponseContent = new MajickRegex.JsonObject();
            Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
            rcCommandClient = new RestClient("https://discord.com/api");
            rrCommandRequest = new RestRequest("/applications/" + _client.ClientID + "/commands", Method.Put);
            rrCommandRequest.RequestFormat = DataFormat.Json;
            rrCommandRequest.AddHeader("Content-Type", "application/json");
            rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrCommandRequest.AddJsonBody(new_commands.ToRawText(false));
            rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
            CommandResponseContent = new MajickRegex.JsonObject(rsCommandResponse.Content);
            foreach (MajickRegex.JsonObject current_command in CommandResponseContent.ObjectLists["objects"])
            {
                global_commands.Add(new ApplicationCommand(current_command));
            }
            return global_commands;
        }
        public ApplicationCommand CreateGlobalCommand(string bot_token, MajickRegex.JsonObject new_command)
        {
            ApplicationCommand CreatedCommand = new ApplicationCommand();
            RestClient rcCommandClient;
            RestRequest rrCommandRequest;
            RestResponse rsCommandResponse;
            MajickRegex.JsonObject CommandResponseContent = new MajickRegex.JsonObject();
            Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
            rcCommandClient = new RestClient("https://discord.com/api");
            rrCommandRequest = new RestRequest("/applications/" + _client.ClientID + "/commands", Method.Post);
            rrCommandRequest.RequestFormat = DataFormat.Json;
            rrCommandRequest.AddHeader("Content-Type", "application/json");
            rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrCommandRequest.AddJsonBody(new_command.ToRawText(false));
            rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
            CommandResponseContent = new MajickRegex.JsonObject(rsCommandResponse.Content);
            CreatedCommand = new ApplicationCommand(CommandResponseContent);
            return CreatedCommand;
        }
        public ApplicationCommand GetGlobalCommand(string bot_token, string command_id)
        {
            ApplicationCommand RequestedCommand = new ApplicationCommand();
            RestClient rcCommandClient;
            RestRequest rrCommandRequest;
            RestResponse rsCommandResponse;
            MajickRegex.JsonObject CommandResponseContent = new MajickRegex.JsonObject();
            Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
            rcCommandClient = new RestClient("https://discord.com/api");
            rrCommandRequest = new RestRequest("/applications/" + _client.ClientID + "/commands/" + command_id, Method.Get);
            rrCommandRequest.RequestFormat = DataFormat.Json;
            rrCommandRequest.AddHeader("Content-Type", "application/json");
            rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
            rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
            CommandResponseContent = new MajickRegex.JsonObject(rsCommandResponse.Content);
            RequestedCommand = new ApplicationCommand(CommandResponseContent);
            return RequestedCommand;
        }
        public ApplicationCommand UpdateGlobalCommand(string bot_token, string command_id, MajickRegex.JsonObject new_command)
        {
            ApplicationCommand UpdatedCommand = new ApplicationCommand();
            RestClient rcCommandClient;
            RestRequest rrCommandRequest;
            RestResponse rsCommandResponse;
            MajickRegex.JsonObject CommandResponseContent = new MajickRegex.JsonObject();
            Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
            rcCommandClient = new RestClient("https://discord.com/api");
            rrCommandRequest = new RestRequest("/applications/" + _client.ClientID + "/commands/" + command_id, Method.Patch);
            rrCommandRequest.RequestFormat = DataFormat.Json;
            rrCommandRequest.AddHeader("Content-Type", "application/json");
            rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrCommandRequest.AddJsonBody(new_command.ToRawText(false));
            rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
            CommandResponseContent = new MajickRegex.JsonObject(rsCommandResponse.Content);
            UpdatedCommand = new ApplicationCommand(CommandResponseContent);
            return UpdatedCommand;
        }
        public bool DeleteGlobalCommand(string bot_token, string command_id)
        {
            ApplicationCommand CreatedCommand = new ApplicationCommand();
            RestClient rcCommandClient;
            RestRequest rrCommandRequest;
            RestResponse rsCommandResponse;
            MajickRegex.JsonObject CommandResponseContent = new MajickRegex.JsonObject();
            Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
            rcCommandClient = new RestClient("https://discord.com/api");
            rrCommandRequest = new RestRequest("/applications/" + _client.ClientID + "/commands/" + command_id, Method.Delete);
            rrCommandRequest.RequestFormat = DataFormat.Json;
            rrCommandRequest.AddHeader("Content-Type", "application/json");
            rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
            rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
            return rsCommandResponse.IsSuccessful;
        }
        public List<ApplicationCommand> GetGuildCommands(string bot_token, string guild_id)
        {
            List<ApplicationCommand> guild_commands = new List<ApplicationCommand>();
            RestClient rcCommandClient;
            RestRequest rrCommandRequest;
            RestResponse rsCommandResponse;
            MajickRegex.JsonObject CommandRequestBody = new MajickRegex.JsonObject();
            MajickRegex.JsonObject CommandResponseContent = new MajickRegex.JsonObject();
            Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
            rcCommandClient = new RestClient("https://discord.com/api");
            rrCommandRequest = new RestRequest("/applications/" + _client.ClientID + "/guilds/" + guild_id + "/commands", Method.Get);
            rrCommandRequest.RequestFormat = DataFormat.Json;
            rrCommandRequest.AddHeader("Content-Type", "application/json");
            rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
            rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
            string full_object = "{\"objects\":" + rsCommandResponse.Content + "}";
            CommandResponseContent = new MajickRegex.JsonObject(full_object);
            foreach (MajickRegex.JsonObject current_command in CommandResponseContent.ObjectLists["objects"])
            {
                guild_commands.Add(new ApplicationCommand(current_command));
            }
            return guild_commands;
        }
        public List<GuildApplicationCommandPermissions> GetAllGuildCommandPermissions(string bot_token, string guild_id)
        {
            List<GuildApplicationCommandPermissions> guild_command_permissions = new List<GuildApplicationCommandPermissions>();
            RestClient rcCommandClient;
            RestRequest rrCommandRequest;
            RestResponse rsCommandResponse;
            MajickRegex.JsonObject CommandRequestBody = new MajickRegex.JsonObject();
            MajickRegex.JsonObject CommandResponseContent = new MajickRegex.JsonObject();
            Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
            rcCommandClient = new RestClient("https://discord.com/api");
            rrCommandRequest = new RestRequest("/applications/" + _client.ClientID + "/guilds/" + guild_id + "/commands/permissions", Method.Get);
            rrCommandRequest.RequestFormat = DataFormat.Json;
            rrCommandRequest.AddHeader("Content-Type", "application/json");
            rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
            rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
            CommandResponseContent = new MajickRegex.JsonObject(rsCommandResponse.Content);
            foreach (MajickRegex.JsonObject current_permission in CommandResponseContent.ObjectLists["objects"])
            {
                guild_command_permissions.Add(new GuildApplicationCommandPermissions(current_permission));
            }
            return guild_command_permissions;
        }
        public List<GuildApplicationCommandPermissions> GetGuildCommandPermissions(string bot_token, string guild_id, string command_id)
        {
            List<GuildApplicationCommandPermissions> guild_command_permissions = new List<GuildApplicationCommandPermissions>();
            RestClient rcCommandClient;
            RestRequest rrCommandRequest;
            RestResponse rsCommandResponse;
            MajickRegex.JsonObject CommandRequestBody = new MajickRegex.JsonObject();
            MajickRegex.JsonObject CommandResponseContent = new MajickRegex.JsonObject();
            Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
            rcCommandClient = new RestClient("https://discord.com/api");
            rrCommandRequest = new RestRequest("/applications/" + _client.ClientID + "/guilds/" + guild_id + "/commands/" + command_id + "/permissions", Method.Get);
            rrCommandRequest.RequestFormat = DataFormat.Json;
            rrCommandRequest.AddHeader("Content-Type", "application/json");
            rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
            rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
            CommandResponseContent = new MajickRegex.JsonObject(rsCommandResponse.Content);
            foreach (MajickRegex.JsonObject current_permission in CommandResponseContent.ObjectLists["objects"])
            {
                guild_command_permissions.Add(new GuildApplicationCommandPermissions(current_permission));
            }
            return guild_command_permissions;
        }
        public List<GuildApplicationCommandPermissions> UpdateGuildCommandPermissions(string bot_token, string guild_id, string command_id, MajickRegex.JsonObject new_permissions)
        {
            List<GuildApplicationCommandPermissions> guild_command_permissions = new List<GuildApplicationCommandPermissions>();
            RestClient rcCommandClient;
            RestRequest rrCommandRequest;
            RestResponse rsCommandResponse;
            MajickRegex.JsonObject CommandRequestBody = new MajickRegex.JsonObject();
            MajickRegex.JsonObject CommandResponseContent = new MajickRegex.JsonObject();
            Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
            rcCommandClient = new RestClient("https://discord.com/api");
            rrCommandRequest = new RestRequest("/applications/" + _client.ClientID + "/guilds/" + guild_id + "/commands/" + command_id + "/permissions", Method.Put);
            rrCommandRequest.RequestFormat = DataFormat.Json;
            rrCommandRequest.AddHeader("Content-Type", "application/json");
            rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrCommandRequest.AddJsonBody(new_permissions.ToRawText(false));
            rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
            CommandResponseContent = new MajickRegex.JsonObject(rsCommandResponse.Content);
            foreach (MajickRegex.JsonObject current_permission in CommandResponseContent.ObjectLists["objects"])
            {
                guild_command_permissions.Add(new GuildApplicationCommandPermissions(current_permission));
            }
            return guild_command_permissions;
        }
        public List<GuildApplicationCommandPermissions> UpdateAllGuildCommandPermissions(string bot_token, string guild_id, string command_id, MajickRegex.JsonObject new_permissions)
        {
            List<GuildApplicationCommandPermissions> guild_command_permissions = new List<GuildApplicationCommandPermissions>();
            RestClient rcCommandClient;
            RestRequest rrCommandRequest;
            RestResponse rsCommandResponse;
            MajickRegex.JsonObject CommandRequestBody = new MajickRegex.JsonObject();
            MajickRegex.JsonObject CommandResponseContent = new MajickRegex.JsonObject();
            Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
            rcCommandClient = new RestClient("https://discord.com/api");
            rrCommandRequest = new RestRequest("/applications/" + _client.ClientID + "/guilds/" + guild_id + "/commands/permissions", Method.Put);
            rrCommandRequest.RequestFormat = DataFormat.Json;
            rrCommandRequest.AddHeader("Content-Type", "application/json");
            rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrCommandRequest.AddJsonBody(new_permissions.ToRawText(false));
            rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
            CommandResponseContent = new MajickRegex.JsonObject(rsCommandResponse.Content);
            foreach (MajickRegex.JsonObject current_permission in CommandResponseContent.ObjectLists["objects"])
            {
                guild_command_permissions.Add(new GuildApplicationCommandPermissions(current_permission));
            }
            return guild_command_permissions;
        }
        public List<ApplicationCommand> OverwriteGuildCommands(string bot_token, string guild_id, MajickRegex.JsonObject new_commands)
        {
            List<ApplicationCommand> guild_commands = new List<ApplicationCommand>();
            RestClient rcCommandClient;
            RestRequest rrCommandRequest;
            RestResponse rsCommandResponse;
            MajickRegex.JsonObject CommandRequestBody = new MajickRegex.JsonObject();
            MajickRegex.JsonObject CommandResponseContent = new MajickRegex.JsonObject();
            Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
            rcCommandClient = new RestClient("https://discord.com/api");
            rrCommandRequest = new RestRequest("/applications/" + _client.ClientID + "/guilds/" + guild_id + "/commands", Method.Put);
            rrCommandRequest.RequestFormat = DataFormat.Json;
            rrCommandRequest.AddHeader("Content-Type", "application/json");
            rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrCommandRequest.AddJsonBody(new_commands.ToRawText(false));
            rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
            CommandResponseContent = new MajickRegex.JsonObject(rsCommandResponse.Content);
            foreach (MajickRegex.JsonObject current_command in CommandResponseContent.ObjectLists["objects"])
            {
                guild_commands.Add(new ApplicationCommand(current_command));
            }
            return guild_commands;
        }
        public async Task<ApplicationCommand> CreateGuildCommandAsync(string bot_token, string guild_id, MajickRegex.JsonObject new_command) { return await Task.Run(() => CreateGuildCommand(bot_token, guild_id, new_command)); }
        public ApplicationCommand CreateGuildCommand(string bot_token, string guild_id, MajickRegex.JsonObject new_command)
        {
            ApplicationCommand CreatedCommand = new ApplicationCommand();
            RestClient rcCommandClient;
            RestRequest rrCommandRequest;
            RestResponse rsCommandResponse;
            MajickRegex.JsonObject CommandResponseContent = new MajickRegex.JsonObject();
            Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
            rcCommandClient = new RestClient("https://discord.com/api");
            rrCommandRequest = new RestRequest("/applications/" + _client.ClientID + "/guilds/" + guild_id + "/commands", Method.Post);
            rrCommandRequest.RequestFormat = DataFormat.Json;
            rrCommandRequest.AddHeader("Content-Type", "application/json");
            rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrCommandRequest.AddJsonBody(new_command.ToRawText(false));
            rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
            CommandResponseContent = new MajickRegex.JsonObject(rsCommandResponse.Content);
            CreatedCommand = new ApplicationCommand(CommandResponseContent);
            return CreatedCommand;
        }
        public async Task<ApplicationCommand> GetGuildCommandAsync(string bot_token, string guild_id, string command_id) { return await Task.Run(() => GetGuildCommand(bot_token, guild_id, command_id)); }
        public ApplicationCommand GetGuildCommand(string bot_token, string guild_id, string command_id)
        {
            ApplicationCommand RequestedCommand = new ApplicationCommand();
            RestClient rcCommandClient;
            RestRequest rrCommandRequest;
            RestResponse rsCommandResponse;
            MajickRegex.JsonObject CommandResponseContent = new MajickRegex.JsonObject();
            Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
            rcCommandClient = new RestClient("https://discord.com/api");
            rrCommandRequest = new RestRequest("/applications/" + _client.ClientID + "/guilds/" + guild_id + "/commands/" + command_id, Method.Get);
            rrCommandRequest.RequestFormat = DataFormat.Json;
            rrCommandRequest.AddHeader("Content-Type", "application/json");
            rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
            rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
            CommandResponseContent = new MajickRegex.JsonObject(rsCommandResponse.Content);
            RequestedCommand = new ApplicationCommand(CommandResponseContent);
            return RequestedCommand;
        }
        public async Task<ApplicationCommand> UpdateGuildCommandAsync(string bot_token, string guild_id, string command_id, MajickRegex.JsonObject new_command) { return await Task.Run(() => UpdateGuildCommand(bot_token, guild_id, command_id, new_command)); }
        public ApplicationCommand UpdateGuildCommand(string bot_token, string guild_id, string command_id, MajickRegex.JsonObject new_command)
        {
            ApplicationCommand UpdatedCommand = new ApplicationCommand();
            RestClient rcCommandClient;
            RestRequest rrCommandRequest;
            RestResponse rsCommandResponse;
            MajickRegex.JsonObject CommandResponseContent = new MajickRegex.JsonObject();
            Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
            rcCommandClient = new RestClient("https://discord.com/api");
            rrCommandRequest = new RestRequest("/applications/" + _client.ClientID + "/guilds/" + guild_id + "/commands/" + command_id, Method.Patch);
            rrCommandRequest.RequestFormat = DataFormat.Json;
            rrCommandRequest.AddHeader("Content-Type", "application/json");
            rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
            rrCommandRequest.AddJsonBody(new_command.ToRawText(false));
            rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
            CommandResponseContent = new MajickRegex.JsonObject(rsCommandResponse.Content);
            UpdatedCommand = new ApplicationCommand(CommandResponseContent);
            return UpdatedCommand;
        }
        public async Task<bool> DeleteGuildCommandAsync(string bot_token, string guild_id, string command_id) { return await Task.Run(() => DeleteGuildCommand(bot_token, guild_id, command_id)); }
        public bool DeleteGuildCommand(string bot_token, string guild_id, string command_id)
        {
            RestClient rcCommandClient;
            RestRequest rrCommandRequest;
            RestResponse rsCommandResponse;
            MajickRegex.JsonObject CommandResponseContent = new MajickRegex.JsonObject();
            Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
            rcCommandClient = new RestClient("https://discord.com/api");
            rrCommandRequest = new RestRequest("/applications/" + _client.ClientID + "/guilds/" + guild_id + "/commands/" + command_id, Method.Delete);
            rrCommandRequest.RequestFormat = DataFormat.Json;
            rrCommandRequest.AddHeader("Content-Type", "application/json");
            rrCommandRequest.AddHeader("Authorization", "Bot " + bot_token);
            rsCommandResponse = rcCommandClient.Execute(rrCommandRequest);
            return rsCommandResponse.IsSuccessful;
        }
        public async Task<DiscordInvite> GetInviteByCodeAsync(string bot_token, string invite_code) { return await Task.Run(() => GetInviteByCode(bot_token, invite_code)); }
        public DiscordInvite GetInviteByCode(string bot_token, string invite_code)
        {
            DiscordInvite invite;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickRegex.JsonObject GuildResponseContent = new MajickRegex.JsonObject();
            Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/invites/" + invite_code, Method.Get);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickRegex.JsonObject(rsGuildResponse.Content);
            invite = new DiscordInvite(GuildResponseContent);
            return invite;
        }
        public async Task<DiscordUser> GetUserByIDAsync(string bot_token, string user_id) { return await Task.Run(() => GetUserByID(bot_token, user_id)); }
        public DiscordUser GetUserByID(string bot_token, string user_id)
        {
            DiscordUser user;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickRegex.JsonObject GuildResponseContent = new MajickRegex.JsonObject();
            Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/users/" + user_id, Method.Get);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickRegex.JsonObject(rsGuildResponse.Content);
            user = new DiscordUser(GuildResponseContent);
            return user;
        }
        public async Task<DiscordChannel> GetChannelByIDAsync(string bot_token, string channel_id) { return await Task.Run(() => GetChannelByID(bot_token, channel_id)); }
        public DiscordChannel GetChannelByID(string bot_token, string channel_id)
        {
            DiscordChannel channel;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            RestResponse rsGuildResponse;
            MajickRegex.JsonObject GuildResponseContent = new MajickRegex.JsonObject();
            Dictionary<string, DiscordInvite> invites = new Dictionary<string, DiscordInvite>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/channels/" + channel_id, Method.Get);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + bot_token);
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            GuildResponseContent = new MajickRegex.JsonObject(rsGuildResponse.Content);
            channel = new DiscordChannel(GuildResponseContent);
            return channel;
        }
        private void Gateway_GUILD_CREATE(object sender, GuildCreateEventArgs e)
        {
            //This is where you will initially get all the data about all the guilds that your bot is on. Save this data in the Guilds Dictionary
            GuildCreatedEventArgs GuildCreatedArgs = new GuildCreatedEventArgs();
            WizardGuild created_guild = new WizardGuild(e.guild_object);
            GuildCreatedArgs.guild = created_guild;
            GuildCreatedArgs.guild_object = e.guild_object;
            if (!_guilds.ContainsKey(created_guild.id)) { _guilds.Add(created_guild.id, created_guild); }
            GuildCreated?.Invoke(this, GuildCreatedArgs);
        }

        private void Gateway_GUILD_DELETE(object sender, GuildDeleteEventArgs e)
        {
            //This is when your bot leaves a guild. Kicked, Banned, or by you making it
            GuildDeletedEventArgs GuildDeletedArgs = new GuildDeletedEventArgs();
            GuildDeletedArgs.guild_id = e.guild.id;
            if (_guilds.ContainsKey(e.guild.id)) { _guilds.Remove(e.guild.id); }
            GuildDeleted?.Invoke(this, GuildDeletedArgs);
        }

        private void Gateway_CHANNEL_CREATE(object sender, ChannelCreateEventArgs e)
        {
            //This is when a channel is created
            OnChannelCreateEventArgs ChannelCreatedArgs = new OnChannelCreateEventArgs();
            ChannelCreatedArgs.guild_id = e.channel.guild_id;
            ChannelCreatedArgs.channel = e.channel;
            ChannelCreated?.Invoke(this, ChannelCreatedArgs);
        }
        private void Gateway_CHANNEL_DELETE(object sender, ChannelDeleteEventArgs e)
        {
            //This is when a channel is deleted
            OnChannelDeleteEventArgs ChannelDeletedArgs = new OnChannelDeleteEventArgs();
            if (e.channel.guild_id == null)
            {
                ChannelDeletedArgs.channel = e.channel;
                ChannelDeleted?.Invoke(this, ChannelDeletedArgs);
            }
            else
            {
                ChannelDeletedArgs.guild_id = e.channel.guild_id;
                ChannelDeletedArgs.channel = e.channel;
                ChannelDeleted?.Invoke(this, ChannelDeletedArgs);
            }
        }
        private void Gateway_GUILD_BAN_ADD(object sender, GuildBanAddEventArgs e)
        {
            //This is when a user is banned from a guild
            OnUserBannedEventArgs UserBannedArgs = new OnUserBannedEventArgs();
            UserBannedArgs.guild_id = e.guild_id;
            UserBannedArgs.user_id = e.user.id;
            UserBanned?.Invoke(this, UserBannedArgs);
        }

        private void Gateway_GUILD_BAN_REMOVE(object sender, GuildBanRemoveEventArgs e)
        {
            //This is when a user is unbanned from a guild
            OnUserUnbannedEventArgs UserUnbannedArgs = new OnUserUnbannedEventArgs();
            UserUnbannedArgs.guild_id = e.guild_id;
            UserUnbannedArgs.user_id = e.user.id;
            if (_guilds.ContainsKey(e.guild_id))
            {
                UserUnbanned?.Invoke(this, UserUnbannedArgs);
            }
        }

        private void Gateway_GUILD_EMOJIS_UPDATE(object sender, GuildEmojisUpdateEventArgs e)
        {
            //This is when the emojis for a guild are updated
            GuildEmojisUpdate?.Invoke(this, e);
        }

        private void Gateway_GUILD_INTEGRATIONS_UPDATE(object sender, GuildIntegrationsUpdateEventArgs e)
        {
            //This is when the integrations for a guild are updated
            GuildIntegrationsUpdate?.Invoke(this, e);
        }

        private void Gateway_GUILD_MEMBERS_CHUNK(object sender, GuildMembersChunkEventArgs e)
        {
            //Only in response to RequestGuildMembers
            GuildMembersChunk?.Invoke(this, e);
            //May need to include this
        }

        private void Gateway_GUILD_MEMBER_ADD(object sender, GuildMemberAddEventArgs e)
        {
            //This is when a member joins a guild that your bot is on
            OnMemberAddedEventArgs MemberAddedArgs = new OnMemberAddedEventArgs();
            MemberAddedArgs.guild_id = e.member.guild_id;
            MemberAddedArgs.member = e.member;
            GuildMemberAdded?.Invoke(this, MemberAddedArgs);
        }

        private void Gateway_GUILD_MEMBER_REMOVE(object sender, GuildMemberRemoveEventArgs e)
        {
            //This is when a member leaves a guild that your bot is on
            OnMemberRemovedEventArgs MemberRemovedArgs = new OnMemberRemovedEventArgs();
            MemberRemovedArgs.guild_id = e.guild_id;
            MemberRemovedArgs.member = new DiscordGuildMember(e.user.ToJson());
            GuildMemberRemoved?.Invoke(this, MemberRemovedArgs);
        }
        private void Gateway_GUILD_MEMBER_UPDATE(object sender, GuildMemberUpdateEventArgs e)
        {
            //This is when the roles or nickname of a member of a guild your bot is on are changed
            OnMemberUpdatedEventArgs MemberUpdatedArgs = new OnMemberUpdatedEventArgs();
            if (_guilds[e.guild_id].roles.Count == 0) { _guilds[e.guild_id].GetRoles(_client.BotToken); }
            MemberUpdatedArgs.guild_id = e.guild_id;
            MemberUpdatedArgs.nick = e.nick;
            if (_guilds[e.guild_id].members.ContainsKey(e.user.id)) { MemberUpdatedArgs.before_roles = _guilds[e.guild_id].members[e.user.id].roles; }
            MemberUpdatedArgs.role_ids = e.roles;
            MemberUpdatedArgs.user_id = e.user.id;
            GuildMemberUpdated?.Invoke(this, MemberUpdatedArgs);
        }

        private void Gateway_GUILD_ROLE_CREATE(object sender, GuildRoleCreateEventArgs e)
        {
            //This is when a new role is created on a guild your bot is on
            //This will always be a colorless role named "new role" and changes will be fired in a new role update event
            OnRoleCreateEventArgs RoleCreateArgs = new OnRoleCreateEventArgs();
            RoleCreateArgs.role =e.role;
            RoleCreateArgs.guild_id = e.guild_id;
            RoleCreated(this, RoleCreateArgs);
        }

        private void Gateway_GUILD_ROLE_DELETE(object sender, GuildRoleDeleteEventArgs e)
        {
            //This is when a role is deleted on a guild your bot is on
            OnRoleDeleteEventArgs RoleDeleteArgs = new OnRoleDeleteEventArgs();
            RoleDeleteArgs.guild_id = e.guild_id;
            RoleDeleteArgs.role_id = e.role_id;
            RoleDeleted?.Invoke(this, RoleDeleteArgs);
        }

        private void Gateway_INVITE_CREATE(object sender, InviteCreateEventArgs e)
        {
            //This is when an invite is created on a guild your bot is on
            OnInviteCreatedEventArgs InviteCreatedArgs = new OnInviteCreatedEventArgs();
            InviteCreatedArgs.guild_id = e.guild_id;
            InviteMetadata meta_data = new InviteMetadata(e.inviter, e.temporary, e.max_uses, e.max_age, e.timestamp);
            DiscordInvite new_invite = new DiscordInvite(e.invite_code, e.guild_id, e.channel_id, meta_data);
            InviteCreatedArgs.invite = new_invite;
            InviteCreated?.Invoke(this, InviteCreatedArgs);
        }

        private void Gateway_INVITE_DELETE(object sender, InviteDeleteEventArgs e)
        {
            //This is when an invite is deleted on a guild your bot is on
            OnInviteDeletedEventArgs InviteDeletedArgs = new OnInviteDeletedEventArgs();
            InviteDeletedArgs.guild_id = e.guild_id;
            InviteDeletedArgs.channel_id = e.channel_id;
            InviteDeletedArgs.code = e.invite_code;
            InviteDeleted?.Invoke(this, InviteDeletedArgs);
        }

        private void Gateway_MESSAGE_CREATE(object sender, MessageCreateEventArgs e)
        {
            //This is the watcher for all message sends
            //move all the message events to be invoked, if they exist in a guild, from the guild to the channel to the message
            //also provide a separate guild level message received option to watch all messages from the guild level
            MessageReceivedEventArgs MessageEventArgs = new MessageReceivedEventArgs();
            MessageEventArgs.ChannelID = e.message.channel_id;
            MessageEventArgs.Author = e.message.member;
            MessageEventArgs.TaggedRoles = e.message.mention_roles;
            MessageEventArgs.MessageID = e.message.id;
            if (e.message.attachments.Count > 0) { MessageEventArgs.AttachmentURL = e.message.attachments[0].url; }
            else { MessageEventArgs.AttachmentURL = ""; }
            List<string> TaggedUserList = new List<string>();
            if (e.message.channel_id != null)
            {
                foreach (DiscordGuildMember mention in e.message.mentions.Values)
                {
                    TaggedUserList.Add(mention.user.id);
                }
                MessageEventArgs.TaggedUsers = TaggedUserList.ToArray();
                MessageEventArgs.MessageText = e.message.content;
                if (e.message.guild_id == null)
                {
                    //This is for private messages
                    MessageEventArgs.IsPrivate = true;
                    MessageEventArgs.ChannelID = e.message.channel_id;
                    MessageReceived?.Invoke(this, MessageEventArgs);
                }
                else 
                {
                    //This is for guild messages
                    MessageEventArgs.IsPrivate = false;
                    MessageEventArgs.GuildID = e.message.guild_id;
                    MessageReceived?.Invoke(this, MessageEventArgs);
                }
            }
        }

        private void Gateway_MESSAGE_DELETE(object sender, MessageDeleteEventArgs e)
        {
            MessageDeleted?.Invoke(this, e);
        }

        private void Gateway_MESSAGE_DELETE_BULK(object sender, MessageDeleteBulkEventArgs e)
        {
            OnMessageBulkDeletedEventArgs BulkDeletedArgs = new OnMessageBulkDeletedEventArgs();
            BulkDeletedArgs.guild_id = e.guild_id;
            BulkDeletedArgs.id_list = e.ids;
            MessagesBulkDeleted?.Invoke(this, BulkDeletedArgs);
        }

        private void Gateway_MESSAGE_REACTION_ADD(object sender, MessageReactionAddEventArgs e)
        {
            //move all the message events to be invoked, from the guild to the channel to the message
            //Pass the data through for updates and raise the necessary event
            OnReactionAddedEventArgs ReactionAddedArgs = new OnReactionAddedEventArgs();
            ReactionAddedArgs.user_id = e.user_id;
            ReactionAddedArgs.guild_id = e.guild_id;
            ReactionAddedArgs.channel_id = e.channel_id;
            ReactionAddedArgs.message_id = e.message_id;
            ReactionAddedArgs.emoji = e.emoji;
            MessageReactionAdded?.Invoke(this, ReactionAddedArgs);
        }

        private void Gateway_MESSAGE_REACTION_REMOVE(object sender, MessageReactionRemoveEventArgs e)
        {
            //move all the message events to be invoked, from the guild to the channel to the message
            //Pass the data through for updates and raise the necessary event
            OnReactionRemovedEventArgs ReactionRemovedArgs = new OnReactionRemovedEventArgs();
            ReactionRemovedArgs.guild_id = e.guild_id;
            ReactionRemovedArgs.channel_id = e.channel_id;
            ReactionRemovedArgs.message_id = e.message_id;
            ReactionRemovedArgs.user_id = e.user_id;
            ReactionRemovedArgs.emoji = e.emoji;
            MessageReactionRemoved?.Invoke(this, ReactionRemovedArgs);
        }

        private void Gateway_MESSAGE_REACTION_REMOVE_ALL(object sender, MessageReactionRemoveAllEventArgs e)
        {
            //move all the message events to be invoked, from the guild to the channel to the message
            //Pass the data through for updates and raise the necessary event
            OnReactionClearedEventArgs ReactionClearedArgs = new OnReactionClearedEventArgs();
            MessageReactionCleared?.Invoke(this, ReactionClearedArgs);
        }

        private void Gateway_MESSAGE_REACTION_REMOVE_EMOJI(object sender, MessageReactionRemoveEmojiEventArgs e)
        {
            //move all the message events to be invoked, from the guild to the channel to the message
            //never got this to fire. might need to have multiple reactions made by users other than the remover
            OnReactionRemoveEmojiEventArgs ReactionRemoveEmojiArgs = new OnReactionRemoveEmojiEventArgs();
            ReactionRemoveEmojiArgs.guild_id = e.guild_id;
            ReactionRemoveEmojiArgs.channel_id = e.channel_id;
            ReactionRemoveEmojiArgs.message_id = e.message_id;
            MessageReactionRemoveEmoji(this, ReactionRemoveEmojiArgs);
        }

        private void Gateway_GUILD_UPDATE(object sender, GuildUpdateEventArgs e)
        {
            //This is when a guild that your bot is on is updated
            OnGuildUpdatedEventArgs GuildUpdatedArgs = new OnGuildUpdatedEventArgs();
            GuildUpdatedArgs.after = new WizardGuild(e.guild.ToJson());
            GuildUpdated?.Invoke(this, GuildUpdatedArgs);
        }

        private void Gateway_CHANNEL_UPDATE(object sender, ChannelUpdateEventArgs e)
        {
            //This is when any channel is updated
            ChannelUpdated?.Invoke(this, e);
        }

        private void Gateway_CHANNEL_PINS_UPDATE(object sender, ChannelPinsUpdateEventArgs e)
        {
            //This is when a message is pinned to any channel
            //this also fired a channel update that failed could have been the list of 1
            PinsUpdated?.Invoke(this, e);
        }
        private void Gateway_GUILD_ROLE_UPDATE(object sender, GuildRoleUpdateEventArgs e)
        {
            //This will fire for every role affected anytime the positions are changed
            OnRoleUpdateEventArgs RoleUpdatedArgs = new OnRoleUpdateEventArgs();
            RoleUpdatedArgs.guild_id = e.guild_id;
            RoleUpdatedArgs.after = new DiscordRole(e.role.ToJson());
            RoleUpdated?.Invoke(this, RoleUpdatedArgs);
        }
        private void Gateway_MESSAGE_UPDATE(object sender, MessageUpdateEventArgs e)
        {
            //still needs tested
            OnMessageUpdateEventArgs MessageUpdateArgs = new OnMessageUpdateEventArgs();
            MessageUpdateArgs.guild_id = e.message.guild_id;
            MessageUpdateArgs.channel_id = e.message.channel_id;
            MessageUpdateArgs.after = e.message;
            MessageUpdated?.Invoke(this, MessageUpdateArgs);
        }
        private void Gateway_PRESENCE_UPDATE(object sender, PresenceUpdateEventArgs e)
        {
            //will probably ignore this as it's a privileged intent
        }

        private void Gateway_TYPING_START(object sender, TypingStartEventArgs e)
        {
            //DetectTyping - will probably ignore this
        }

        private void Gateway_USER_UPDATE(object sender, UserUpdateEventArgs e)
        {
            //This is when any user that shares an open channel with the bot updates their profile
            //WizardUser user_updated = new WizardUser(e.user);
        }

        private void Gateway_WEBHOOKS_UPDATE(object sender, WebhooksUpdateEventArgs e)
        {
            //This is when the webhooks of a guild your bot is on are changed
            OnWebhooksUpdateEventArgs WebhooksUpdatedArgs = new OnWebhooksUpdateEventArgs();
            WebhooksUpdatedArgs.guild_id = e.guild_id;
            WebhooksUpdatedArgs.channel_id = e.channel_id;
            if (_guilds.ContainsKey(e.guild_id))
            {
                WebhooksUpdated?.Invoke(this, WebhooksUpdatedArgs);
            }
        }

        private void Gateway_VOICE_STATE_UPDATE(object sender, VoiceStateUpdateEventArgs e)
        {
            //No idea what this means
            VoiceStateUpdated?.Invoke(this, e);
        }

        private void Gateway_VOICE_SERVER_UPDATE(object sender, VoiceServerUpdateEventArgs e)
        {
            //This is when the server region of a guild your bot is on is changed
            OnVoiceServerUpdateEventArgs VoiceServerUpdateArgs = new OnVoiceServerUpdateEventArgs();
            VoiceServerUpdateArgs.guild_id = e.guild_id;
            VoiceServerUpdateArgs.endpoint = e.endpoint;
            VoiceServerUpdateArgs.token = e.token;
            if (_guilds.ContainsKey(e.guild_id))
            {
                VoiceServerUpdate?.Invoke(this, VoiceServerUpdateArgs);
            }
        }
        private void Gateway_INTERACTION_CREATE(object sender, InteractionCreateEventArgs e)
        {
            OnInteractionCreatedEventArgs InteractionCreatedEventArgs = new OnInteractionCreatedEventArgs();
            InteractionCreatedEventArgs.new_interaction = e.interaction;
            InteractionCreated?.Invoke(this, InteractionCreatedEventArgs);
        }
    }
    public class WizardGuild : DiscordGuild
    {
        public string CommandPrefix { get; set; }
        public string OwnerID { get; set; }
        public bool IsPremium { get; set; }
        public Dictionary<string, WizardRoleGroup> RoleGroups { get; set; }
        public bool IsDeleted { get; set; }
        public WizardGuildTimer MakeRoleTimer { get; set; }
        public int MakeRoleColor { get; set; }
        public string MakeRoleName { get; set; }
        public string MakeRoleAuthorID { get; set; }
        public string MakeRoleMessageID { get; set; }
        public WizardLinkup Linkup { get; set; }
        public WizardSecurity Security { get; set; }
        public List<string> PluginNames { get; set; }
        public Dictionary<int, WizardPlugin> Plugins { get; set; }
        public WizardGuild(MajickRegex.JsonObject guild_object) : base(guild_object)
        {
            
            RoleGroups = new Dictionary<string, WizardRoleGroup>();
            IsDeleted = false;
            IsPremium = false;            
            MakeRoleTimer = new WizardGuildTimer(300000, id);
            MakeRoleTimer.AutoReset = false;
            MakeRoleName = "";
            MakeRoleAuthorID = "";
            MakeRoleMessageID = "";
            MakeRoleColor = 0;
            Linkup = new WizardLinkup();
            Security = new WizardSecurity();
            PluginNames = new List<string>();
            Plugins = new Dictionary<int, WizardPlugin>();
        }
    }
    public class WizardSecurity
    {
        public string MutedRoleID { get; set; }
        public string WizardCategoryID { get; set; }
        public string LockoutRoleID { get; set; }
        public string LockoutChannelID { get; set; }
        public string LoggingChannelID { get; set; }
        public string LoggingWebhookID { get; set; }
        public bool LogModeration { get; set; }
        public bool LogTimers { get; set; }
        public bool LogRoles { get; set; }
        public bool LogDashboard { get; set; }
        public bool SnipeConflicts { get; set; }
        public List<string> CommandRoles { get; set; }
        public List<string> RejectedRoles { get; set; }
        public List<string> ImmunityRoles { get; set; }
        public List<string> DashboardRoles { get; set; }
        public List<string> SnipedRoles { get; set; }
        public List<string> FlaggedMutedRoles { get; set; }
        public List<string> FlaggedLockoutRoles { get; set; }
        public List<WizardPlugin> Plugins { get; set; }
        public WizardSecurity()
        {
            //Instantiate all the properties
            LogModeration = false;
            LogTimers = false;
            LogRoles = false;
            LogDashboard = false;
            SnipeConflicts = false;
            CommandRoles = new List<string>();
            RejectedRoles = new List<string>();
            ImmunityRoles = new List<string>();
            DashboardRoles = new List<string>();
            SnipedRoles = new List<string>();
            FlaggedMutedRoles = new List<string>();
            FlaggedLockoutRoles = new List<string>();
            Plugins = new List<WizardPlugin>();
        }
    }
    public class WizardPlugin
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string HelpText { get; set; }
        public string Description { get; set; }
        public List<string> Aliases { get; set; }
        public List<string> AllowedRoles { get; set; }
        public List<string> IgnoredRoles { get; set; }
        public List<string> CommandNames { get; set; }
        public Dictionary<int, WizardCommand> Commands { get; set; }
        public WizardPlugin() 
        {
            ID = -1;
            Name = "";
            HelpText = "";
            Description = "";
            Aliases = new List<string>();
            AllowedRoles = new List<string>();
            IgnoredRoles = new List<string>();
            CommandNames = new List<string>();
            Commands = new Dictionary<int, WizardCommand>();
        }
        public WizardPlugin(int new_id, string new_name)
        {
            ID = new_id;
            Name = new_name;
            HelpText = "";
            Description = "";
            Aliases = new List<string>();
            AllowedRoles = new List<string>();
            IgnoredRoles = new List<string>();
            CommandNames = new List<string>();
            Commands = new Dictionary<int, WizardCommand>();
        }
    }
    public class WizardCommand
    {
        //Build this for Command Level allowances and refusals
        public int ID { get; set; }
        public int PluginID { get; set; }
        public string Name { get; set; }
        public string Help { get; set; }
        public string Usage { get; set; }
        public string Description { get; set; }
        public int PermissionFlag { get; set; }
        public bool IsAvailable { get; set; }
        public CommandActionType ActionType { get; set; }
        public List<string> Aliases { get; set; }
        public List<string> AllowedRoles { get; set; }
        public List<string> IgnoredRoles { get; set; }
        public List<WizardCommandArgument> Arguments { get; set; }
        public WizardCommand() 
        {
            ID = -1;
            PluginID = -1;
            Name = "";
            Help = "";
            Usage = "";
            Description = "";
            PermissionFlag = -1;
            IsAvailable = false;
            ActionType = CommandActionType.None;
            Aliases = new List<string>();
            AllowedRoles = new List<string>();
            IgnoredRoles = new List<string>();
            Arguments = new List<WizardCommandArgument>();
        }
        public WizardCommand(int new_id, int new_plugin, string new_name)
        {
            ID = new_id;
            PluginID = new_plugin;
            Name = new_name;
            Help = "";
            Usage = "";
            Description = "";
            PermissionFlag = -1;
            IsAvailable = false;
            ActionType = CommandActionType.None;
            Aliases = new List<string>();
            AllowedRoles = new List<string>();
            IgnoredRoles = new List<string>();
            Arguments = new List<WizardCommandArgument>();
        }
    }
    public class WizardCommandArgument
    {
        public int Order { get; set; }
        public bool IsList { get; set; }
        public bool IsNumeric { get; set; }
        public bool IsRequired { get; set; }
        public bool IsProvided { get; set; }
        public string TextValue { get; set; }
        public string DefaultValue { get; set; }
        public CommandArgumentType ArgumentType { get; set; }
        public CommandArgumentObject ArgumentObject { get; set; }
        public List<string> ListValue { get; set; }
        public WizardCommandArgument() 
        {
            Order = 0;
            IsList = false;
            TextValue = "";
            DefaultValue = "";
            IsNumeric = false;
            IsRequired = false;
            IsProvided = false;
            ArgumentType = CommandArgumentType.Text;
            ArgumentObject = CommandArgumentObject.None;
            ListValue = new List<string>();
        }
    }
    public class WizardLinkup
    {
        public List<string> LinkupRoles { get; set; }
        public string HelpdeskChannelID { get; set; }
        public string HelpdeskWebhookID { get; set; }
        public string LinkupChannelID { get; set; }
        public string LinkupWebhookID { get; set; }
        public string PremiumLinkupChannelID { get; set; }
        public string PremiumLinkupWebhookID { get; set; }
        public string PendingConfirmationID { get; set; }
        public string LinkupConfirmMessageID { get; set; }
        public bool LinkupChannelIsOpen { get; set; }
        public List<string> LinkupConnections { get; set; }
        public bool PremiumLinkupChannelIsOpen { get; set; }
        public List<string> PremiumLinkupConnections { get; set; }
        public bool HelpdeskMessageAllowed { get; set; }
        private System.Timers.Timer HelpdeskCooldown;
        public WizardLinkup()
        {
            LinkupRoles = new List<string>();
            LinkupConnections = new List<string>();
            PremiumLinkupConnections = new List<string>();
            LinkupChannelIsOpen = false;
            PremiumLinkupChannelIsOpen = false;
            PendingConfirmationID = "";
            LinkupConfirmMessageID = "";
            HelpdeskMessageAllowed = true;
            HelpdeskCooldown = new System.Timers.Timer(1200000);
            HelpdeskCooldown.AutoReset = false;
            HelpdeskCooldown.Elapsed += HelpdeskCooldown_Elapsed;
        }
        private void HelpdeskCooldown_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            HelpdeskMessageAllowed = true;
        }
        public void CooldownHelpdesk()
        {
            HelpdeskMessageAllowed = false;
            HelpdeskCooldown.Start();
        }
    }
    public static class DiscordObjectExtensions
    {
        public static int GetColorFromHex(this RoleUpdateObject role_object, string hex_color)
        {
            int iNdx = 0;
            int color_code = 0;
            int current_value = 0;
            char[] code_array = hex_color.ToCharArray();
            if (code_array.Length == 6)
            {
                for (int i = 5; i >= 0; i--)
                {
                    char current_code = code_array[i];
                    if (!int.TryParse(current_code.ToString(), out current_value))
                    {
                        switch (current_code)
                        {
                            case 'A':
                            case 'a':
                                current_value = 10;
                                break;
                            case 'B':
                            case 'b':
                                current_value = 11;
                                break;
                            case 'C':
                            case 'c':
                                current_value = 12;
                                break;
                            case 'D':
                            case 'd':
                                current_value = 13;
                                break;
                            case 'E':
                            case 'e':
                                current_value = 14;
                                break;
                            case 'F':
                            case 'f':
                                current_value = 15;
                                break;
                        }
                    }
                    if (iNdx != 0) { color_code += (int)(Math.Pow(16, iNdx)) * current_value; }
                    else { color_code = current_value; }
                    iNdx += 1;
                }
            }
            return color_code;
        }
        public static int ConvertHexColor(this Embed embed_object, string hex_color)
        {
            int iNdx = 0;
            int color_code = -1;
            int current_value = 0;
            char[] code_array = hex_color.ToCharArray();
            for (int i = 5; i >= 0; i--)
            {
                char current_code = code_array[i];
                if (!int.TryParse(current_code.ToString(), out current_value))
                {
                    switch (current_code)
                    {
                        case 'A':
                        case 'a':
                            current_value = 10;
                            break;
                        case 'B':
                        case 'b':
                            current_value = 11;
                            break;
                        case 'C':
                        case 'c':
                            current_value = 12;
                            break;
                        case 'D':
                        case 'd':
                            current_value = 13;
                            break;
                        case 'E':
                        case 'e':
                            current_value = 14;
                            break;
                        case 'F':
                        case 'f':
                            current_value = 15;
                            break;
                    }
                }
                if (iNdx != 0) { color_code += (int)(Math.Pow(16, iNdx)) * current_value; }
                else { color_code = current_value; }
                iNdx += 1;
            }
            return color_code;
        }
    }
    public class WizardRoleGroup
    {
        public bool SwapSet { get; set; }
        public string BaseRoleID { get; set; }
        public List<string> GroupedRoles { get; set; }
        public List<string> RejectedRoles { get; set; }
        public List<string> RequiredRoles { get; set; }
        public bool RequireAll { get; set; }
        public WizardRoleGroup()
        {
            BaseRoleID = "";
            SwapSet = false;
            GroupedRoles = new List<string>();
            RejectedRoles = new List<string>();
            RequiredRoles = new List<string>();
            RequireAll = false;
        }
        public WizardRoleGroup(string new_base_id)
        {
            BaseRoleID = new_base_id;
            GroupedRoles = new List<string>();
            RejectedRoles = new List<string>();
            RequiredRoles = new List<string>();
            RequireAll = false;
            SwapSet = false;
        }
    }
    public class WizardGuildTimer : System.Timers.Timer 
    {
        public int MessageCount { get; set; }
        public string RoleID { get; set; }
        public string GuildID { get; set; }
        public string MemberID { get; set; }
        public string ChannelID { get; set; }
        public string WebhookID { get; set; }
        public string MessageID { get; set; }
        public string Description { get; set; }
        public WizardGuildTimer() : base()
        {
            RoleID = "";
            GuildID = "";
            MemberID = "";
            ChannelID = "";
            WebhookID = "";
            MessageID = "";
            Description = "";
            MessageCount = 0;
        }
        public WizardGuildTimer(double new_interval) : base(new_interval)
        {
            RoleID = "";
            GuildID = "";
            MemberID = "";
            ChannelID = "";
            WebhookID = "";
            MessageID = "";
            Description = "";
            MessageCount = 0;
        }
        public WizardGuildTimer(double new_interval, string guild_id, string channel_id = "", string message_id = "") : base(new_interval)
        {
            RoleID = "";
            GuildID = guild_id;
            MemberID = "";
            ChannelID = channel_id;
            WebhookID = "";
            MessageID = message_id;
            Description = "";
            MessageCount = 0;
        }
    }
    public class GuildCreatedEventArgs : EventArgs
    {
        public WizardGuild guild { get; set; }
        public MajickRegex.JsonObject guild_object { get; set; }
    }
    public class GuildDeletedEventArgs : EventArgs
    {
        public string guild_id { get; set; }
    }
    public class MessageReceivedEventArgs : EventArgs
    {
        public bool IsPrivate { get; set; }
        public string GuildID { get; set; }
        public string MessageID { get; set; }
        public string MessageText { get; set; }
        public string ChannelID { get; set; }
        public string AttachmentURL { get; set; }
        public DiscordGuildMember Author { get; set; }
        public List<string> TaggedRoles { get; set; }
        public string[] TaggedUsers { get; set; }
    }
    public class OnMessageBulkDeletedEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public List<string> id_list { get; set; }
    }
    public class OnGuildUpdatedEventArgs : EventArgs
    {
        public WizardGuild before { get; set; }
        public WizardGuild after { get; set; }
    }
    public class OnMemberAddedEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public DiscordGuildMember member { get; set; }
    }
    public class OnMemberRemovedEventArgs
    {
        public string guild_id { get; set; }
        public DiscordGuildMember member { get; set; }
    }
    public class OnChannelCreateEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public DiscordChannel channel { get; set; }
    }
    public class OnChannelDeleteEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public DiscordChannel channel { get; set; }
    }
    public class OnRoleCreateEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public DiscordRole role { get; set; }
    }
    public class OnRoleDeleteEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public string role_id { get; set; }
    }
    public class OnMemberUpdatedEventArgs : EventArgs
    {
        public string user_id { get; set; }
        public string guild_id { get; set; }
        public List<string> before_roles { get; set; }
        public List<string> role_ids { get; set; }
        public string nick { get; set; }
    }
    public class OnUserBannedEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public string user_id { get; set; }
        public string reason { get; set; }
    }
    public class OnInviteCreatedEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public DiscordInvite invite { get; set; }
    }
    public class OnInviteDeletedEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public string code { get; set; }
        public string channel_id { get; set; }
    }
    public class OnEmojisUpdateEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public List<DiscordEmoji> before { get; set; }
        public List<DiscordEmoji> after { get; set; }
    }
    public class OnWebhooksUpdateEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public string channel_id { get; set; }
    }
    public class OnVoiceServerUpdateEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public string token { get; set; }
        public string endpoint { get; set; }
    }
    public class OnUserUnbannedEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public string user_id { get; set; }
    }
    public class OnUserUpdateEventArgs : EventArgs
    {
        public DiscordUser before { get; set; }
        public DiscordUser after { get; set; }
    }
    public class OnReactionAddedEventArgs : EventArgs
    {
        public string user_id { get; set; }
        public string guild_id { get; set; }
        public string channel_id { get; set; }
        public string message_id { get; set; }
        public DiscordEmoji emoji { get; set; }
    }
    public class OnReactionRemovedEventArgs : EventArgs
    {
        public string user_id { get; set; }
        public string guild_id { get; set; }
        public string channel_id { get; set; }
        public string message_id { get; set; }
        public DiscordEmoji emoji { get; set; }
    }
    public class OnReactionClearedEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public string channel_id { get; set; }
        public string message_id { get; set; }
    }
    public class OnReactionRemoveEmojiEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public string channel_id { get; set; }
        public string message_id { get; set; }
        public DiscordEmoji emoji { get; set; }
    }
    public class OnMessageUpdateEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public string channel_id { get; set; }
        public DiscordMessage before { get; set; }
        public DiscordMessage after { get; set; }
    }
    public class OnRoleUpdateEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public DiscordRole before { get; set; }
        public DiscordRole after { get; set; }
    }
    public class OnChannelTypingDetectedEventArgs : EventArgs
    {
        public string user_id { get; set; }
        public string channel_id { get; set; }
        public string guild_id { get; set; }
    }
    public class OnInteractionCreatedEventArgs : EventArgs
    {
        public DiscordInteraction new_interaction { get; set; }
    }
    public enum TimeInterval
    {
        second = 1000,
        minute = 60000,
        hour = 3600000,
        day = 30800000
    }
    public enum CommandActionType
    {
        None = 0,
        SendMessage = 1,
        DeleteMessage = 2,
        CreateRole = 3,
        AddRole = 4,
        RemoveRole = 5,
        SetRoles = 6,
        SortRoles = 7,
        ToggleRoles = 8,
        CreateChannel = 9,
        DeleteChannel = 10,
        CreateWebhook = 11,
        ExecuteWebhook = 12,
        SetSlowmode = 13,
        SlowmodeLonger = 14,
        SlowmodeShorter = 15,
        JoinServer = 16,
        SetChannelTopic = 17,
        SetRoleName = 18,
        SetRoleColor = 19,
        ToggleRoleHoist = 20,
        ToggleRoleMention = 21,
        Advanced = 22
    }
    public enum CommandArgumentType
    {
        Text = 0,
        Number = 1,
        ID = 2,
        Name = 3,
        Mention = 4,
        ReactionOn = 5,
        ReactionOff = 6,
        Timespan = 7
    }
    public enum CommandArgumentObject
    {
        None = 0,
        User = 1,
        Role = 2,
        Channel = 3,
        Webhook = 4,
        Emoji = 5,
        Plugin = 6,
        Command = 7
    }
}