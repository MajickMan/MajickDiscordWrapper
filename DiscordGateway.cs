using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using System.Net;
using WebSocketSharp;
using MajickDiscordWrapper.MajickRegex;

namespace MajickDiscordWrapper.Discord.Gateway
{
    public class Payload
    {
        public PayloadOpCode op { get; internal set; }
        //this is the opcode for Discord Payloads
        public MajickDiscordWrapper.MajickRegex.JsonObject d { get; internal set; }
        //this is the "data" for the payload
        public string s { get; internal set; }
        //this is the sequence number -- only used for opcode 0
        public string t { get; internal set; }
        //this is the event name -- ony used for opcode 0
        public Payload(PayloadOpCode new_op)
        {
            op = new_op;
            d = new MajickDiscordWrapper.MajickRegex.JsonObject();
        }
        public Payload(PayloadOpCode new_op, MajickDiscordWrapper.MajickRegex.JsonObject new_d)
        {
            op = new_op;
            d = new_d;
        }
        public Payload(PayloadOpCode new_op, MajickDiscordWrapper.MajickRegex.JsonObject new_d, string new_s, string new_t)
        {
            op = new_op;
            d = new_d;
            s = new_s;
            t = new_t;
        }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            int op_int = (int)op;
            string op_string = op_int.ToString();
            MajickDiscordWrapper.MajickRegex.JsonObject payload = new MajickDiscordWrapper.MajickRegex.JsonObject();
            payload.AddAttribute("op", op_string, true);
            payload.AddObject("d", d);
            return payload;
        }
    }
    public class DiscordGateway
    {
        private int Shards;
        private int CurrentShard;
        private int SequenceNumber;
        private string EventName;
        private string SessionID;
        private int IntentsFlag;
        private System.Timers.Timer HeartbeatTime;
        private System.Timers.Timer InternalHeartbeat;
        public DiscordClient Owner { get; internal set; }
        public WebSocket DiscordSocket { get; internal set; }
        public string GatewayURL { get; set; }
        public string CurrentData { get; internal set; }
        public DiscordGateway(string new_GatewayURL, DiscordClient new_Owner, List<DiscordGatewayIntent> Intents, int current_shard = 0, int shards = 1)
        {
            Owner = new_Owner;
            SequenceNumber = -1;
            Shards = shards;
            CurrentShard = current_shard;
            IntentsFlag = 0;
            foreach (DiscordGatewayIntent Intent in Intents)
            {
                IntentsFlag += (int)Intent;
            }
            GatewayURL = new_GatewayURL + "?v=9&encoding=json";
            DiscordSocket = new WebSocket(GatewayURL);
            DiscordSocket.OnOpen += DiscordSocket_OnOpen;
            DiscordSocket.OnClose += DiscordSocket_OnClose;
            DiscordSocket.OnMessage += DiscordSocket_OnMessage;
            DiscordSocket.OnError += DiscordSocket_OnError;
            InternalHeartbeat = new System.Timers.Timer(60000);
            InternalHeartbeat.AutoReset = true;
            InternalHeartbeat.Elapsed += InternalHeartbeat_Elapsed;
        }

        private void InternalHeartbeat_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (DiscordSocket.ReadyState == WebSocketState.Closed)
            {
                Resume();
            }
        }

        private void DiscordSocket_OnError(object sender, ErrorEventArgs e)
        {
            if (!DiscordSocket.IsAlive)
            {
                DiscordSocket.Close();
            }
        }

        private void DiscordSocket_OnMessage(object sender, MessageEventArgs e)
        {
            ReceiveMessageAsync(sender, e);
        }
        private async Task ReceiveMessageAsync(object sender, MessageEventArgs e)
        {
            bool true_or_false = true;
            DiscordUser current_user = new DiscordUser();
            PayloadOpCode op_code = PayloadOpCode.Undefined;
            Payload pl_Received;
            MajickRegex.JsonObject obj_Payload;
            CurrentData = e.Data.Substring(1, e.Data.Length - 2);
            obj_Payload = new MajickRegex.JsonObject(CurrentData);
            Enum.TryParse(obj_Payload.Attributes["op"].text_value, out op_code);
            if (op_code == PayloadOpCode.HeartbeatACK) { pl_Received = new Payload(op_code); }
            else if (op_code == PayloadOpCode.Heartbeat) { Heartbeat(); }
            else { pl_Received = new Payload(op_code, obj_Payload.Objects["d"]); }

            if (obj_Payload.Attributes.ContainsKey("s"))
            {
                if (obj_Payload.Attributes["s"].text_value != "null")
                {
                    int.TryParse(obj_Payload.Attributes["s"].text_value, out SequenceNumber);
                }
            }
            if (obj_Payload.Attributes.ContainsKey("t") && obj_Payload.Objects.ContainsKey("d"))
            {
                EventName = obj_Payload.Attributes["t"].text_value.Replace("\"", "");
                switch (EventName)
                {
                    case "READY":
                        int shard_id;
                        int num_shards;
                        ReadyEventArgs ReadyArgs = new ReadyEventArgs();
                        ReadyArgs.guilds = new List<DiscordGuild>();
                        ReadyArgs.private_channels = new List<DiscordChannel>();
                        ReadyArgs.session_id = obj_Payload.Objects["d"].Attributes["session_id"].text_value;
                        SessionID = ReadyArgs.session_id;
                        current_user = new DiscordUser(obj_Payload.Objects["d"].Objects["user"]);
                        ReadyArgs.user = current_user;
                        if (obj_Payload.Objects["d"].AttributeLists.ContainsKey("shard"))
                        {
                            if (int.TryParse(obj_Payload.Objects["d"].AttributeLists["shard"][1].text_value, out shard_id)) { ReadyArgs.shard_id = shard_id; }
                            if (int.TryParse(obj_Payload.Objects["d"].AttributeLists["shard"][1].text_value, out num_shards)) { ReadyArgs.shard_count = num_shards; }
                        }
                        READY(sender, ReadyArgs);
                        break;
                    case "INVALID_SESSION":
                        bool is_resumeable = false;
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("d"))
                        {
                            if (bool.TryParse(obj_Payload.Objects["d"].Attributes["d"].text_value, out is_resumeable))
                            {
                                if (!is_resumeable)
                                {
                                    //close the connection and start a new session
                                    DiscordSocket.Close();
                                    await ConnectAsync();
                                }
                                else
                                {
                                    await ResumeAsync();
                                }
                            }
                        }
                        break;
                    case "CHANNEL_CREATE":
                        ChannelCreateEventArgs ChannelCreateArgs = new ChannelCreateEventArgs();
                        ChannelCreateArgs.channel = new DiscordChannel(obj_Payload.Objects["d"]);
                        CHANNEL_CREATE(sender, ChannelCreateArgs);
                        break;
                    case "CHANNEL_UPDATE":
                        ChannelUpdateEventArgs ChannelUpdateArgs = new ChannelUpdateEventArgs();
                        ChannelUpdateArgs.channel = new DiscordChannel(obj_Payload.Objects["d"]);
                        CHANNEL_UPDATE(sender, ChannelUpdateArgs);
                        break;
                    case "CHANNEL_DELETE":
                        ChannelDeleteEventArgs ChannelDeleteArgs = new ChannelDeleteEventArgs();
                        ChannelDeleteArgs.channel = new DiscordChannel(obj_Payload.Objects["d"]);
                        CHANNEL_DELETE(sender, ChannelDeleteArgs);
                        break;
                    case "CHANNEL_PINS_UPDATE":
                        DateTime last_pin_update;
                        ChannelPinsUpdateEventArgs PinsArgs = new ChannelPinsUpdateEventArgs();
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("guild_id")) { PinsArgs.guild_id = obj_Payload.Objects["d"].Attributes["guild_id"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("channel_id")) { PinsArgs.channel_id = obj_Payload.Objects["d"].Attributes["channel_id"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("last_pin_timestamp"))
                        {
                            if (DateTime.TryParse(obj_Payload.Objects["d"].Attributes["last_pin_timestamp"].text_value, out last_pin_update))
                            {
                                PinsArgs.last_pin_timestamp = last_pin_update;
                            }
                        }
                        CHANNEL_PINS_UPDATE(sender, PinsArgs);
                        break;
                    case "GUILD_CREATE":
                        GuildCreateEventArgs GuildCreateArgs = new GuildCreateEventArgs();
                        GuildCreateArgs.guild_object = obj_Payload.Objects["d"];
                        RequestGuildMembers(GuildCreateArgs.guild_object.Attributes["id"].text_value);
                        GUILD_CREATE(sender, GuildCreateArgs);
                        break;
                    case "GUILD_UPDATE":
                        GuildUpdateEventArgs GuildUpdateArgs = new GuildUpdateEventArgs();
                        GuildUpdateArgs.guild = new DiscordGuild(obj_Payload.Objects["d"]);
                        GUILD_UPDATE(sender, GuildUpdateArgs);
                        break;
                    case "GUILD_DELETE":
                        GuildDeleteEventArgs GuildDeleteArgs = new GuildDeleteEventArgs();
                        GuildDeleteArgs.guild = new DiscordGuild(obj_Payload.Objects["d"]);
                        GUILD_DELETE(sender, GuildDeleteArgs);
                        break;
                    case "GUILD_BAN_ADD":
                        GuildBanAddEventArgs GuildBanAddArgs = new GuildBanAddEventArgs();
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("guild_id")) { GuildBanAddArgs.guild_id = obj_Payload.Objects["d"].Attributes["guild_id"].text_value; }
                        if (obj_Payload.Objects["d"].Objects.ContainsKey("user")) { GuildBanAddArgs.user = new DiscordUser(obj_Payload.Objects["d"].Objects["user"]); }
                        GUILD_BAN_ADD(sender, GuildBanAddArgs);
                        break;
                    case "GUILD_BAN_REMOVE":
                        GuildBanRemoveEventArgs GuildBanRemoveArgs = new GuildBanRemoveEventArgs();
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("guild_id")) { GuildBanRemoveArgs.guild_id = obj_Payload.Objects["d"].Attributes["guild_id"].text_value; }
                        if (obj_Payload.Objects["d"].Objects.ContainsKey("user")) { GuildBanRemoveArgs.user = new DiscordUser(obj_Payload.Objects["d"].Objects["user"]); }
                        GUILD_BAN_REMOVE(sender, GuildBanRemoveArgs);
                        break;
                    case "GUILD_EMOJIS_UPDATE":
                        GuildEmojisUpdateEventArgs GuildEmojisArgs = new GuildEmojisUpdateEventArgs();
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("guild_id")) { GuildEmojisArgs.guild_id = obj_Payload.Objects["d"].Attributes["guild_id"].text_value; }
                        List<DiscordEmoji> custom_emojis = new List<DiscordEmoji>();
                        if (obj_Payload.Objects["d"].ObjectLists.ContainsKey("emojis"))
                        {
                            foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_emoji in obj_Payload.Objects["d"].ObjectLists["emojis"])
                            {
                                DiscordEmoji custom_emoji = new DiscordEmoji(current_emoji);
                                custom_emojis.Add(custom_emoji);
                            }
                        }
                        GuildEmojisArgs.emojis = custom_emojis;
                        GUILD_EMOJIS_UPDATE(sender, GuildEmojisArgs);
                        break;
                    case "GUILD_INTEGRATIONS_UPDATE":
                        GuildIntegrationsUpdateEventArgs GuildIntegrationsArgs = new GuildIntegrationsUpdateEventArgs();
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("guild_id")) { GuildIntegrationsArgs.guild_id = obj_Payload.Objects["d"].Attributes["guild_id"].text_value; }
                        GUILD_INTEGRATIONS_UPDATE(sender, GuildIntegrationsArgs);
                        break;
                    case "GUILD_MEMBER_ADD":
                        GuildMemberAddEventArgs GuildMemberAddArgs = new GuildMemberAddEventArgs();
                        GuildMemberAddArgs.member = new DiscordGuildMember(obj_Payload.Objects["d"]);
                        GUILD_MEMBER_ADD(sender, GuildMemberAddArgs);
                        break;
                    case "GUILD_MEMBER_REMOVE":
                        GuildMemberRemoveEventArgs GuildMemberRemoveArgs = new GuildMemberRemoveEventArgs();
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("guild_id")) { GuildMemberRemoveArgs.guild_id = obj_Payload.Objects["d"].Attributes["guild_id"].text_value; }
                        if (obj_Payload.Objects["d"].Objects.ContainsKey("user")) { GuildMemberRemoveArgs.user = new DiscordUser(obj_Payload.Objects["d"].Objects["user"]); }
                        GUILD_MEMBER_REMOVE(sender, GuildMemberRemoveArgs);
                        break;
                    case "GUILD_MEMBER_UPDATE":
                        GuildMemberUpdateEventArgs GuildMemberUpdateArgs = new GuildMemberUpdateEventArgs();
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("guild_id")) { GuildMemberUpdateArgs.guild_id = obj_Payload.Objects["d"].Attributes["guild_id"].text_value; }
                        List<string> current_roles = new List<string>();
                        if (obj_Payload.Objects["d"].AttributeLists.ContainsKey("roles"))
                        {
                            foreach (JsonAttribute role_id in obj_Payload.Objects["d"].AttributeLists["roles"])
                            {
                                current_roles.Add(role_id.text_value);
                            }
                        }
                        GuildMemberUpdateArgs.roles = current_roles;
                        if (obj_Payload.Objects["d"].Objects.ContainsKey("user")) { GuildMemberUpdateArgs.user = new DiscordUser(obj_Payload.Objects["d"].Objects["user"]); }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("nick")) { GuildMemberUpdateArgs.nick = obj_Payload.Objects["d"].Attributes["nick"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("premium_since"))
                        {
                            DateTime created_at;
                            if (DateTime.TryParse(obj_Payload.Objects["d"].Attributes["premium_since"].text_value, out created_at))
                            {
                                GuildMemberUpdateArgs.premium_since = created_at;
                            }
                        }
                        GUILD_MEMBER_UPDATE(sender, GuildMemberUpdateArgs);
                        break;
                    case "GUILD_MEMBERS_CHUNK":
                        GuildMembersChunkEventArgs GuildMembersChunkArgs = new GuildMembersChunkEventArgs();
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("guild_id")) { GuildMembersChunkArgs.guild_id = obj_Payload.Objects["d"].Attributes["guild_id"].text_value; }
                        List<DiscordGuildMember> guild_members = new List<DiscordGuildMember>();
                        if (obj_Payload.Objects["d"].ObjectLists.ContainsKey("members"))
                        {
                            foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_member in obj_Payload.Objects["d"].ObjectLists["members"])
                            {
                                DiscordGuildMember this_member = new DiscordGuildMember(current_member, GuildMembersChunkArgs.guild_id);
                                guild_members.Add(this_member);
                            }
                        }
                        GuildMembersChunkArgs.members = guild_members;
                        List<string> not_found = new List<string>();
                        if (obj_Payload.Objects["d"].AttributeLists.ContainsKey("not_found"))
                        {
                            foreach (JsonAttribute user_id in obj_Payload.Objects["d"].AttributeLists["not_found"])
                            {
                                not_found.Add(user_id.text_value);
                            }
                        }
                        GuildMembersChunkArgs.not_found = not_found;
                        List<DiscordPresenceUpdate> presences = new List<DiscordPresenceUpdate>();
                        if (obj_Payload.Objects["d"].ObjectLists.ContainsKey("presences"))
                        {
                            foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_presence in obj_Payload.Objects["d"].ObjectLists["presences"])
                            {
                                DiscordPresenceUpdate this_presence = new DiscordPresenceUpdate(current_presence);
                                presences.Add(this_presence);
                            }
                        }
                        GuildMembersChunkArgs.presences = presences;
                        GUILD_MEMBERS_CHUNK(sender, GuildMembersChunkArgs);
                        break;
                    case "GUILD_ROLE_CREATE":
                        GuildRoleCreateEventArgs GuildRoleCreateArgs = new GuildRoleCreateEventArgs();
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("guild_id")) { GuildRoleCreateArgs.guild_id = obj_Payload.Objects["d"].Attributes["guild_id"].text_value; }
                        if (obj_Payload.Objects["d"].Objects.ContainsKey("role")) { GuildRoleCreateArgs.role = new DiscordRole(obj_Payload.Objects["d"].Objects["role"]); }
                        GUILD_ROLE_CREATE(sender, GuildRoleCreateArgs);
                        break;
                    case "GUILD_ROLE_UPDATE":
                        GuildRoleUpdateEventArgs GuildRoleUpdateArgs = new GuildRoleUpdateEventArgs();
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("guild_id")) { GuildRoleUpdateArgs.guild_id = obj_Payload.Objects["d"].Attributes["guild_id"].text_value; }
                        if (obj_Payload.Objects["d"].Objects.ContainsKey("role")) { GuildRoleUpdateArgs.role = new DiscordRole(obj_Payload.Objects["d"].Objects["role"]); }
                        GUILD_ROLE_UPDATE(sender, GuildRoleUpdateArgs);
                        break;
                    case "GUILD_ROLE_DELETE":
                        GuildRoleDeleteEventArgs GuildRoleDeleteArgs = new GuildRoleDeleteEventArgs();
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("guild_id")) { GuildRoleDeleteArgs.guild_id = obj_Payload.Objects["d"].Attributes["guild_id"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("role_id")) { GuildRoleDeleteArgs.role_id = obj_Payload.Objects["d"].Attributes["role_id"].text_value; }
                        GUILD_ROLE_DELETE(sender, GuildRoleDeleteArgs);
                        break;
                    case "INVITE_CREATE":
                        InviteCreateEventArgs InviteCreateArgs = new InviteCreateEventArgs();
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("channel_id")) { InviteCreateArgs.channel_id = obj_Payload.Objects["d"].Attributes["channel_id"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("code")) { InviteCreateArgs.invite_code = obj_Payload.Objects["d"].Attributes["code"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("created_at"))
                        {
                            DateTime created_at;
                            if (DateTime.TryParse(obj_Payload.Objects["d"].Attributes["created_at"].text_value, out created_at))
                            {
                                InviteCreateArgs.timestamp = created_at;
                            }
                        }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("guild_id")) { InviteCreateArgs.guild_id = obj_Payload.Objects["d"].Attributes["guild_id"].text_value; }
                        if (obj_Payload.Objects["d"].Objects.ContainsKey("inviter")) { InviteCreateArgs.inviter = new DiscordUser(obj_Payload.Objects["d"].Objects["inviter"]); }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("max_age"))
                        {
                            if (obj_Payload.Objects["d"].Attributes["max_age"].text_value != "null")
                            {
                                int max_age;
                                if (int.TryParse(obj_Payload.Objects["d"].Attributes["max_age"].text_value, out max_age))
                                {
                                    InviteCreateArgs.max_age = max_age;
                                }
                            }
                        }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("max_uses"))
                        {
                            if (obj_Payload.Objects["d"].Attributes["max_uses"].text_value != "null")
                            {
                                int max_uses;
                                if (int.TryParse(obj_Payload.Objects["d"].Attributes["max_uses"].text_value, out max_uses))
                                {
                                    InviteCreateArgs.max_uses = max_uses;
                                }
                            }
                        }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("temporary"))
                        {
                            if (obj_Payload.Objects["d"].Attributes["temporary"].text_value != "null")
                            {
                                if (bool.TryParse(obj_Payload.Objects["d"].Attributes["temporary"].text_value, out true_or_false))
                                {
                                    InviteCreateArgs.temporary = true_or_false;
                                }
                            }
                        }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("uses"))
                        {
                            if (obj_Payload.Objects["d"].Attributes["uses"].text_value != "null")
                            {
                                int uses;
                                if (int.TryParse(obj_Payload.Objects["d"].Attributes["uses"].text_value, out uses))
                                {
                                    InviteCreateArgs.uses = uses;
                                }
                            }
                        }
                        INVITE_CREATE(sender, InviteCreateArgs);
                        break;
                    case "INVITE_DELETE":
                        InviteDeleteEventArgs InviteDeleteArgs = new InviteDeleteEventArgs();
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("channel_id")) { InviteDeleteArgs.channel_id = obj_Payload.Objects["d"].Attributes["channel_id"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("guild_id")) { InviteDeleteArgs.guild_id = obj_Payload.Objects["d"].Attributes["guild_id"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("code")) { InviteDeleteArgs.invite_code = obj_Payload.Objects["d"].Attributes["code"].text_value; }
                        INVITE_DELETE(sender, InviteDeleteArgs);
                        break;
                    case "MESSAGE_CREATE":
                        MessageCreateEventArgs MessageCreateArgs = new MessageCreateEventArgs();
                        MessageCreateArgs.message = new DiscordMessage(obj_Payload.Objects["d"]);
                        MESSAGE_CREATE(sender, MessageCreateArgs);
                        break;
                    case "MESSAGE_UPDATE":
                        MessageUpdateEventArgs MessageUpdateArgs = new MessageUpdateEventArgs();
                        MessageUpdateArgs.message = new DiscordMessage(obj_Payload.Objects["d"]);
                        MESSAGE_UPDATE(sender, MessageUpdateArgs);
                        break;
                    case "MESSAGE_DELETE":
                        MessageDeleteEventArgs MessageDeleteArgs = new MessageDeleteEventArgs();
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("id")) { MessageDeleteArgs.id = obj_Payload.Objects["d"].Attributes["id"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("channel_id")) { MessageDeleteArgs.channel_id = obj_Payload.Objects["d"].Attributes["channel_id"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("guild_id")) { MessageDeleteArgs.guild_id = obj_Payload.Objects["d"].Attributes["guild_id"].text_value; }
                        MESSAGE_DELETE(sender, MessageDeleteArgs);
                        break;
                    case "MESSAGE_DELETE_BULK":
                        MessageDeleteBulkEventArgs MessageDeleteBulkArgs = new MessageDeleteBulkEventArgs();
                        List<string> deleted_ids = new List<string>();
                        if (obj_Payload.Objects["d"].AttributeLists.ContainsKey("ids"))
                        {
                            foreach (JsonAttribute message_id in obj_Payload.Objects["d"].AttributeLists["ids"])
                            {
                                deleted_ids.Add(message_id.text_value);
                            }
                        }
                        MessageDeleteBulkArgs.ids = deleted_ids;
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("channel_id")) { MessageDeleteBulkArgs.channel_id = obj_Payload.Objects["d"].Attributes["channel_id"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("guild_id")) { MessageDeleteBulkArgs.guild_id = obj_Payload.Objects["d"].Attributes["guild_id"].text_value; }
                        MESSAGE_DELETE_BULK(sender, MessageDeleteBulkArgs);
                        break;
                    case "MESSAGE_REACTION_ADD":
                        MessageReactionAddEventArgs MessageReactionAddArgs = new MessageReactionAddEventArgs();
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("user_id")) { MessageReactionAddArgs.user_id = obj_Payload.Objects["d"].Attributes["user_id"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("channel_id")) { MessageReactionAddArgs.channel_id = obj_Payload.Objects["d"].Attributes["channel_id"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("message_id")) { MessageReactionAddArgs.message_id = obj_Payload.Objects["d"].Attributes["message_id"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("guild_id")) { MessageReactionAddArgs.guild_id = obj_Payload.Objects["d"].Attributes["guild_id"].text_value; }
                        if (obj_Payload.Objects["d"].Objects.ContainsKey("emoji")) { MessageReactionAddArgs.emoji = new DiscordEmoji(obj_Payload.Objects["d"].Objects["emoji"]); }
                        MESSAGE_REACTION_ADD(sender, MessageReactionAddArgs);
                        break;
                    case "MESSAGE_REACTION_REMOVE":
                        MessageReactionRemoveEventArgs MessageReactionRemoveArgs = new MessageReactionRemoveEventArgs();
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("user_id")) { MessageReactionRemoveArgs.user_id = obj_Payload.Objects["d"].Attributes["user_id"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("channel_id")) { MessageReactionRemoveArgs.channel_id = obj_Payload.Objects["d"].Attributes["channel_id"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("message_id")) { MessageReactionRemoveArgs.message_id = obj_Payload.Objects["d"].Attributes["message_id"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("guild_id")) { MessageReactionRemoveArgs.guild_id = obj_Payload.Objects["d"].Attributes["guild_id"].text_value; }
                        if (obj_Payload.Objects["d"].Objects.ContainsKey("emoji")) { MessageReactionRemoveArgs.emoji = new DiscordEmoji(obj_Payload.Objects["d"].Objects["emoji"]); }
                        MESSAGE_REACTION_REMOVE(sender, MessageReactionRemoveArgs);
                        break;
                    case "MESSAGE_REACTION_REMOVE_ALL":
                        MessageReactionRemoveAllEventArgs MessageReactionRemoveAllArgs = new MessageReactionRemoveAllEventArgs();
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("channel_id")) { MessageReactionRemoveAllArgs.channel_id = obj_Payload.Objects["d"].Attributes["channel_id"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("message_id")) { MessageReactionRemoveAllArgs.message_id = obj_Payload.Objects["d"].Attributes["message_id"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("guild_id")) { MessageReactionRemoveAllArgs.guild_id = obj_Payload.Objects["d"].Attributes["guild_id"].text_value; }
                        MESSAGE_REACTION_REMOVE_ALL(sender, MessageReactionRemoveAllArgs);
                        break;
                    case "MESSAGE_REACTION_REMOVE_EMOJI":
                        MessageReactionRemoveEmojiEventArgs MessageReactionRemoveEmojiArgs = new MessageReactionRemoveEmojiEventArgs();
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("channel_id")) { MessageReactionRemoveEmojiArgs.channel_id = obj_Payload.Objects["d"].Attributes["channel_id"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("message_id")) { MessageReactionRemoveEmojiArgs.message_id = obj_Payload.Objects["d"].Attributes["message_id"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("guild_id")) { MessageReactionRemoveEmojiArgs.guild_id = obj_Payload.Objects["d"].Attributes["guild_id"].text_value; }
                        if (obj_Payload.Objects["d"].Objects.ContainsKey("emoji")) { MessageReactionRemoveEmojiArgs.emoji = new DiscordEmoji(obj_Payload.Objects["d"].Objects["emoji"]); }
                        MESSAGE_REACTION_REMOVE_EMOJI(sender, MessageReactionRemoveEmojiArgs);
                        break;
                    case "PRESENCE_UPDATE":
                        //PresenceUpdateEventArgs PresenceUpdateArgs = new PresenceUpdateEventArgs();
                        //if (obj_Payload.Objects["d"].Objects.ContainsKey("user")) { PresenceUpdateArgs.user = new DiscordUser(obj_Payload.Objects["d"].Objects["user"]); }
                        //List<string> presence_roles = new List<string>();
                        //if (obj_Payload.Objects["d"].AttributeLists.ContainsKey("roles"))
                        //{
                        //    foreach (JsonAttribute role_id in obj_Payload.Objects["d"].AttributeLists["roles"])
                        //    {
                        //        presence_roles.Add(role_id.text_value);
                        //    }
                        //}
                        //PresenceUpdateArgs.roles = presence_roles;
                        //if (obj_Payload.Objects["d"].Objects.ContainsKey("game")) { PresenceUpdateArgs.game = new UserActivity(obj_Payload.Objects["d"].Objects["game"]); }
                        //if (obj_Payload.Objects["d"].Attributes.ContainsKey("guild_id")) { PresenceUpdateArgs.guild_id = obj_Payload.Objects["d"].Attributes["guild_id"].text_value; }
                        //if (obj_Payload.Objects["d"].Attributes.ContainsKey("status")) { PresenceUpdateArgs.status = obj_Payload.Objects["d"].Attributes["status"].text_value; }
                        //List<UserActivity> user_activities = new List<UserActivity>();
                        //if (obj_Payload.Objects["d"].ObjectLists.ContainsKey("activities"))
                        //{
                        //    foreach (MajickDiscordWrapper.MajickRegex.JsonObject current_activity in obj_Payload.Objects["d"].ObjectLists["activities"])
                        //    {
                        //        UserActivity user_activity = new UserActivity(current_activity);
                        //        user_activities.Add(user_activity);
                        //    }
                        //}
                        //if (obj_Payload.Objects["d"].Objects.ContainsKey("client_status"))
                        //{
                        //    ClientStatus client_status = new ClientStatus(obj_Payload.Objects["d"].Objects["client_status"]);
                        //    PresenceUpdateArgs.client_status = client_status;
                        //}
                        //PresenceUpdateArgs.activities = user_activities;
                        //if (obj_Payload.Objects["d"].Attributes.ContainsKey("premium_since"))
                        //{
                        //    DateTime created_at;
                        //    if (DateTime.TryParse(obj_Payload.Objects["d"].Attributes["premium_since"].text_value, out created_at))
                        //    {
                        //        PresenceUpdateArgs.premium_since = created_at;
                        //    }
                        //}
                        //if (obj_Payload.Objects["d"].Objects.ContainsKey("nick")) { PresenceUpdateArgs.nick = obj_Payload.Objects["d"].Attributes["nick"].text_value; }
                        //PRESENCE_UPDATE(sender, PresenceUpdateArgs);
                        break;
                    case "TYPING_START":
                        int typing_start;
                        TypingStartEventArgs TypingStartArgs = new TypingStartEventArgs();
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("channel_id")) { TypingStartArgs.channel_id = obj_Payload.Objects["d"].Attributes["channel_id"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("user_id")) { TypingStartArgs.user_id = obj_Payload.Objects["d"].Attributes["user_id"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("guild_id")) { TypingStartArgs.guild_id = obj_Payload.Objects["d"].Attributes["guild_id"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("timestamp"))
                        {
                            if (TypingStartArgs.guild_id != null)
                            {
                                if (int.TryParse(TypingStartArgs.guild_id = obj_Payload.Objects["d"].Attributes["guild_id"].text_value, out typing_start))
                                {
                                    TypingStartArgs.timestamp = typing_start;
                                }
                            }
                        }
                        if (obj_Payload.Objects["d"].Objects.ContainsKey("member"))
                        {
                            DiscordGuildMember member = new DiscordGuildMember(obj_Payload.Objects["d"].Objects["member"]);
                            TypingStartArgs.member = member;
                        }
                        TYPING_START(sender, TypingStartArgs);
                        break;
                    case "USER_UPDATE":
                        UserUpdateEventArgs UserUpdateArgs = new UserUpdateEventArgs();
                        UserUpdateArgs.user = new DiscordUser(obj_Payload.Objects["d"]);
                        USER_UPDATE(sender, UserUpdateArgs);
                        break;
                    case "VOICE_STATE_UPDATE":
                        VoiceStateUpdateEventArgs VoiceStateUpdateArgs = new VoiceStateUpdateEventArgs();
                        VoiceStateUpdateArgs.voice_state = new DiscordVoiceState(obj_Payload.Objects["d"]);
                        VOICE_STATE_UPDATE(sender, VoiceStateUpdateArgs);
                        break;
                    case "VOICE_SERVER_UPDATE":
                        VoiceServerUpdateEventArgs VoiceServerUpdateArgs = new VoiceServerUpdateEventArgs();
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("token")) { VoiceServerUpdateArgs.token = obj_Payload.Objects["d"].Attributes["token"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("guild_id")) { VoiceServerUpdateArgs.guild_id = obj_Payload.Objects["d"].Attributes["guild_id"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("endpoint")) { VoiceServerUpdateArgs.endpoint = obj_Payload.Objects["d"].Attributes["endpoint"].text_value; }
                        VOICE_SERVER_UPDATE(sender, VoiceServerUpdateArgs);
                        break;
                    case "WEBHOOKS_UPDATE":
                        WebhooksUpdateEventArgs WebhooksUpdateArgs = new WebhooksUpdateEventArgs();
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("guild_id")) { WebhooksUpdateArgs.guild_id = obj_Payload.Objects["d"].Attributes["guild_id"].text_value; }
                        if (obj_Payload.Objects["d"].Attributes.ContainsKey("channel_id")) { WebhooksUpdateArgs.channel_id = obj_Payload.Objects["d"].Attributes["channel_id"].text_value; }
                        WEBHOOKS_UPDATE(sender, WebhooksUpdateArgs);
                        break;
                    case "INTERACTION_CREATE":
                        InteractionCreateEventArgs InteractionCreateArgs = new InteractionCreateEventArgs();
                        InteractionCreateArgs.interaction = new DiscordInteraction(obj_Payload.Objects["d"]);
                        INTERACTION_CREATE(sender, InteractionCreateArgs);
                        break;
                    default:
                        break;
                }
            }
            //else
            //{
            //    Console.WriteLine("Op Code: " + op_code.ToString());
            //    Console.WriteLine("Sequence No: " + SequenceNumber.ToString());
            //    Console.WriteLine("Data: " + obj_Payload.ToRawText());
            //}
            if (obj_Payload.Objects.ContainsKey("d"))
            {
                if (obj_Payload.Objects["d"].Attributes.ContainsKey("heartbeat_interval"))
                {
                    int heartbeat = -1;
                    int.TryParse(obj_Payload.Objects["d"].Attributes["heartbeat_interval"].text_value, out heartbeat);
                    HeartbeatTime = new System.Timers.Timer(heartbeat);
                    HeartbeatTime.AutoReset = true;
                    HeartbeatTime.Elapsed += HeartbeatTime_Elapsed;
                    HeartbeatTime.Start();
                    ConnectionProperties properties = new ConnectionProperties();
                    properties.os = "Windows";
                    properties.browser = "MajickDiscordWrapper";
                    properties.device = "MajickDiscordWrapper";
                    await IdentifyAsync(Owner.BotToken, properties, IntentsFlag, CurrentShard, Shards);
                }
            }
        }

        private void HeartbeatTime_Elapsed(object sender, ElapsedEventArgs e)
        {
            Heartbeat();
        }

        private void DiscordSocket_OnClose(object sender, CloseEventArgs e)
        {
            DiscordSocket.Connect();
            Resume();
        }
        private void DiscordSocket_OnOpen(object sender, EventArgs e)
        {
            DiscordSocket.EmitOnPing = true;
        }

        public delegate void Ready(object sender, ReadyEventArgs e);
        public delegate void ChannelCreate(object sender, ChannelCreateEventArgs e);
        public delegate void ChannelUpdate(object sender, ChannelUpdateEventArgs e);
        public delegate void ChannelDelete(object sender, ChannelDeleteEventArgs e);
        public delegate void ChannelPinsUpdate(object sender, ChannelPinsUpdateEventArgs e);
        public delegate void GuildCreate(object sender, GuildCreateEventArgs e);
        public delegate void GuildUpdate(object sender, GuildUpdateEventArgs e);
        public delegate void GuildDelete(object sender, GuildDeleteEventArgs e);
        public delegate void GuildBanAdd(object sender, GuildBanAddEventArgs e);
        public delegate void GuildBanRemove(object sender, GuildBanRemoveEventArgs e);
        public delegate void GuildEmojisUpdate(object sender, GuildEmojisUpdateEventArgs e);
        public delegate void GuildIntegrationsUpdate(object sender, GuildIntegrationsUpdateEventArgs e);
        public delegate void GuildMemberAdd(object sender, GuildMemberAddEventArgs e);
        public delegate void GuildMemberRemove(object sender, GuildMemberRemoveEventArgs e);
        public delegate void GuildMemberUpdate(object sender, GuildMemberUpdateEventArgs e);
        public delegate void GuildMembersChunk(object sender, GuildMembersChunkEventArgs e);
        public delegate void GuildRoleCreate(object sender, GuildRoleCreateEventArgs e);
        public delegate void GuildRoleUpdate(object sender, GuildRoleUpdateEventArgs e);
        public delegate void GuildRoleDelete(object sender, GuildRoleDeleteEventArgs e);
        public delegate void InviteCreate(object sender, InviteCreateEventArgs e);
        public delegate void InviteDelete(object sender, InviteDeleteEventArgs e);
        public delegate void MessageCreate(object sender, MessageCreateEventArgs e);
        public delegate void MessageUpdate(object sender, MessageUpdateEventArgs e);
        public delegate void MessageDelete(object sender, MessageDeleteEventArgs e);
        public delegate void MessageDeleteBulk(object sender, MessageDeleteBulkEventArgs e);
        public delegate void MessageReactionAdd(object sender, MessageReactionAddEventArgs e);
        public delegate void MessageReactionRemove(object sender, MessageReactionRemoveEventArgs e);
        public delegate void MessageReactionRemoveAll(object sender, MessageReactionRemoveAllEventArgs e);
        public delegate void MessageReactionRemoveEmoji(object sender, MessageReactionRemoveEmojiEventArgs e);
        public delegate void PresenceUpdate(object sender, PresenceUpdateEventArgs e);
        public delegate void TypingStart(object sender, TypingStartEventArgs e);
        public delegate void UserUpdate(object sender, UserUpdateEventArgs e);
        public delegate void VoiceStateUpdate(object sender, VoiceStateUpdateEventArgs e);
        public delegate void VoiceServerUpdate(object sender, VoiceServerUpdateEventArgs e);
        public delegate void WebhooksUpdate(object sender, WebhooksUpdateEventArgs e);
        public delegate void InteractionCreate(object sender, InteractionCreateEventArgs e);

        //These are all the events that Discord Raises and sends data for
        public event Ready READY;
        public event ChannelCreate CHANNEL_CREATE;
        public event ChannelUpdate CHANNEL_UPDATE;
        public event ChannelDelete CHANNEL_DELETE;
        public event ChannelPinsUpdate CHANNEL_PINS_UPDATE;
        public event GuildCreate GUILD_CREATE;
        public event GuildUpdate GUILD_UPDATE;
        public event GuildDelete GUILD_DELETE;
        public event GuildBanAdd GUILD_BAN_ADD;
        public event GuildBanRemove GUILD_BAN_REMOVE;
        public event GuildEmojisUpdate GUILD_EMOJIS_UPDATE;
        public event GuildIntegrationsUpdate GUILD_INTEGRATIONS_UPDATE;
        public event GuildMemberAdd GUILD_MEMBER_ADD;
        public event GuildMemberRemove GUILD_MEMBER_REMOVE;
        public event GuildMemberUpdate GUILD_MEMBER_UPDATE;
        public event GuildMembersChunk GUILD_MEMBERS_CHUNK;
        public event GuildRoleCreate GUILD_ROLE_CREATE;
        public event GuildRoleUpdate GUILD_ROLE_UPDATE;
        public event GuildRoleDelete GUILD_ROLE_DELETE;
        public event InviteCreate INVITE_CREATE;
        public event InviteDelete INVITE_DELETE;
        public event MessageCreate MESSAGE_CREATE;
        public event MessageUpdate MESSAGE_UPDATE;
        public event MessageDelete MESSAGE_DELETE;
        public event MessageDeleteBulk MESSAGE_DELETE_BULK;
        public event MessageReactionAdd MESSAGE_REACTION_ADD;
        public event MessageReactionRemove MESSAGE_REACTION_REMOVE;
        public event MessageReactionRemoveAll MESSAGE_REACTION_REMOVE_ALL;
        public event MessageReactionRemoveEmoji MESSAGE_REACTION_REMOVE_EMOJI;
        public event PresenceUpdate PRESENCE_UPDATE;
        public event TypingStart TYPING_START;
        public event UserUpdate USER_UPDATE;
        public event VoiceStateUpdate VOICE_STATE_UPDATE;
        public event VoiceServerUpdate VOICE_SERVER_UPDATE;
        public event WebhooksUpdate WEBHOOKS_UPDATE;
        public event InteractionCreate INTERACTION_CREATE;

        public async Task ConnectAsync() { await Task.Run(() => Connect()); }
        public void Connect()
        {
            DiscordSocket.Connect();
            //this is where you receive the hello
        }
        public async Task ReconnectAsync() { await Task.Run(() => Reconnect()); }
        public void Reconnect()
        {
            //this is where you receive the hello
            if (DiscordSocket.ReadyState == WebSocketState.Closed)
            {
                Resume();
            }
        }
        public async Task IdentifyAsync(string access_token, ConnectionProperties properties, int IntentsFlag = 2, int CurrentShard = 0, int Shards = 1) { await Task.Run(() => Identify(access_token, properties, IntentsFlag, CurrentShard, Shards)); }
        public void Identify(string access_token, ConnectionProperties properties, int IntentsFlag = 2, int CurrentShard = 0, int Shards = 1)
        {
            // In order to code these you have to have the correct information for the REST Requests
            // Need to build the payload object.
            MajickDiscordWrapper.MajickRegex.JsonObject d = new MajickDiscordWrapper.MajickRegex.JsonObject();
            d.Name = "d";
            d.AddAttribute("token", access_token);
            d.AddObject("properties", properties.ToJson());
            if (Shards > 1) { d.AddAttribute("shard", "[" + CurrentShard.ToString() + "," + Shards.ToString() + "]"); }
            d.AddAttribute("intents", IntentsFlag.ToString());
            Payload identify_payload = new Payload(PayloadOpCode.Identify, d);
            DiscordSocket.Send(identify_payload.ToJson().ToRawText(false));
        }
        public async Task ResumeAsync() { await Task.Run(() => Resume()); }
        public void Resume()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject d = new MajickDiscordWrapper.MajickRegex.JsonObject();
            d.Name = "d";
            d.AddAttribute("token", Owner.BotToken);
            d.AddAttribute("session_id", SessionID);
            d.AddAttribute("seq", SequenceNumber.ToString(), true);
            Payload resume_payload = new Payload(PayloadOpCode.Identify, d);
            DiscordSocket.Send(resume_payload.ToJson().ToRawText(false));
        }
        public void Heartbeat()
        {
            int HeartbeatCode = (int)PayloadOpCode.Heartbeat;
            MajickDiscordWrapper.MajickRegex.JsonObject d = new MajickDiscordWrapper.MajickRegex.JsonObject();
            d.Name = "d";
            d.AddAttribute("op", HeartbeatCode.ToString());
            if(SequenceNumber != -1) { d.AddAttribute("d", SequenceNumber.ToString()); }
            else { d.AddAttribute("d", SequenceNumber.ToString(), true); }
            Payload heartbeat_payload = new Payload(PayloadOpCode.Heartbeat, d);
            DiscordSocket.Send(heartbeat_payload.ToJson().ToRawText(false));
        }
        public async Task RequestGuildMembersAsync(string guild_id) { await Task.Run(() => RequestGuildMembers(guild_id)); }
        public void RequestGuildMembers(string guild_id) 
        {
            MajickDiscordWrapper.MajickRegex.JsonObject d = new MajickDiscordWrapper.MajickRegex.JsonObject();
            d.Name = "d";
            d.AddAttribute("guild_id", guild_id);
            d.AddAttribute("query", "");
            d.AddAttribute("limit", "500", true);
            Payload member_request = new Payload(PayloadOpCode.RequestGuildMembers, d);
            DiscordSocket.Send(member_request.ToJson().ToRawText(false));
        }
        public async Task UpdateVoiceStateAsync() { await Task.Run(() => UpdateVoiceState()); }
        public void UpdateVoiceState() { }
        public async Task UpdateStatusAsync() { await Task.Run(() => UpdateStatus()); }
        public void UpdateStatus() { }
    }
    public class SessionStartLimit
    {
        public int total { get; set; }
        public int remaining { get; set; }
        public int reset_after { get; set; }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject JsonObject = new MajickDiscordWrapper.MajickRegex.JsonObject();
            return JsonObject;
        }
    }
    public class ConnectionProperties
    {
        public string os { get; set; }
        public string browser { get; set; }
        public string device { get; set; }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject JsonObject = new MajickDiscordWrapper.MajickRegex.JsonObject();
            JsonObject.Name = "properties";
            JsonObject.AddAttribute("$os", os);
            JsonObject.AddAttribute("$browser", browser);
            JsonObject.AddAttribute("$device", device);
            return JsonObject;
        }
    }
    public class GatewayStatusUpdate
    {
        public int since { get; set; }
        public UserActivity game { get; set; }
        public UserStatus status { get; set; }
        public bool afk { get; set; }
        public MajickDiscordWrapper.MajickRegex.JsonObject ToJson()
        {
            MajickDiscordWrapper.MajickRegex.JsonObject JsonObject = new MajickDiscordWrapper.MajickRegex.JsonObject();
            return JsonObject;
        }
    }
    public class ReadyEventArgs : EventArgs
    {
        public int v { get; set; }
        public DiscordUser user { get; set; }
        public List<DiscordChannel> private_channels { get; set; }
        public List<DiscordGuild> guilds { get; set; }
        public string session_id { get; set; }
        public int shard_id { get; set; }
        public int shard_count { get; set; }
        public List<string> _trace { get; set; }
    }
    public class ChannelCreateEventArgs : EventArgs
    {
        public DiscordChannel channel { get; set; }
    }
    public class ChannelUpdateEventArgs : EventArgs
    {
        public DiscordChannel channel { get; set; }
    }
    public class ChannelDeleteEventArgs : EventArgs
    {
        public DiscordChannel channel { get; set; }
    }
    public class ChannelPinsUpdateEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public string channel_id { get; set; }
        public DateTime last_pin_timestamp { get; set; }
    }
    public class GuildCreateEventArgs : EventArgs
    {
        public MajickRegex.JsonObject guild_object { get; set; }
    }
    public class GuildUpdateEventArgs : EventArgs
    {
        public DiscordGuild guild { get; set; }
    }
    public class GuildDeleteEventArgs : EventArgs
    {
        public DiscordGuild guild { get; set; }
    }
    public class GuildBanAddEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public DiscordUser user { get; set; }
    }
    public class GuildBanRemoveEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public DiscordUser user { get; set; }
    }
    public class GuildEmojisUpdateEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public List<DiscordEmoji> emojis { get; set; }
    }
    public class GuildIntegrationsUpdateEventArgs : EventArgs
    {
        public string guild_id { get; set; }
    }
    public class GuildMemberAddEventArgs : EventArgs
    {
        public DiscordGuildMember member { get; set; }
    }
    public class GuildMemberRemoveEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public DiscordUser user { get; set; }
    }
    public class GuildMemberUpdateEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public List<string> roles { get; set; }
        public DiscordUser user { get; set; }
        public string nick { get; set; }
        public DateTime premium_since { get; set; }
    }
    public class GuildMembersChunkEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public List<DiscordGuildMember> members { get; set; }
        public List<string> not_found { get; set; }
        public List<DiscordPresenceUpdate> presences { get; set; }
    }
    public class GuildRoleCreateEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public DiscordRole role { get; set; }
    }
    public class GuildRoleUpdateEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public DiscordRole role { get; set; }
    }
    public class GuildRoleDeleteEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public string role_id { get; set; }
    }
    public class InviteCreateEventArgs : EventArgs
    {
        public string channel_id { get; set; }
        public string invite_code { get; set; }
        public DateTime timestamp { get; set; }
        public string guild_id { get; set; }
        public DiscordUser inviter { get; set; }
        public int max_age { get; set; }
        public int max_uses { get; set; }
        public bool temporary { get; set; }
        public int uses { get; set; }
    }
    public class InviteDeleteEventArgs : EventArgs
    {
        public string channel_id { get; set; }
        public string guild_id { get; set; }
        public string invite_code { get; set; }
    }
    public class MessageCreateEventArgs : EventArgs
    {
        public DiscordMessage message { get; set; }
    }
    public class MessageUpdateEventArgs : EventArgs
    {
        public DiscordMessage message { get; set; }
    }
    public class MessageDeleteEventArgs : EventArgs
    {
        public string id { get; set; }
        public string channel_id { get; set; }
        public string guild_id { get; set; }
    }
    public class MessageDeleteBulkEventArgs : EventArgs
    {
        public List<string> ids { get; set; }
        public string channel_id { get; set; }
        public string guild_id { get; set; }
    }
    public class MessageReactionAddEventArgs : EventArgs
    {
        public string user_id { get; set; }
        public string channel_id { get; set; }
        public string message_id { get; set; }
        public string guild_id { get; set; }
        public DiscordEmoji emoji { get; set; }
    }
    public class MessageReactionRemoveEventArgs : EventArgs
    {
        public string user_id { get; set; }
        public string channel_id { get; set; }
        public string message_id { get; set; }
        public string guild_id { get; set; }
        public DiscordEmoji emoji { get; set; }
    }
    public class MessageReactionRemoveAllEventArgs : EventArgs
    {
        public string channel_id { get; set; }
        public string message_id { get; set; }
        public string guild_id { get; set; }
    }
    public class MessageReactionRemoveEmojiEventArgs : EventArgs
    {
        public string channel_id { get; set; }
        public string message_id { get; set; }
        public string guild_id { get; set; }
        public DiscordEmoji emoji { get; set; }
    }
    public class PresenceUpdateEventArgs : EventArgs
    {
        public DiscordUser user { get; set; }
        public List<string> roles { get; set; }
        public UserActivity game { get; set; }
        public string guild_id { get; set; }
        public string status { get; set; }
        public List<UserActivity> activities { get; set; }
        public ClientStatus client_status { get; set; }
        public DateTime premium_since { get; set; }
        public string nick { get; set; }
    }
    public class TypingStartEventArgs : EventArgs
    {
        public string channel_id { get; set; }
        public string guild_id { get; set; }
        public string user_id { get; set; }
        public int timestamp { get; set; }
        public DiscordGuildMember member { get; set; }
    }
    public class UserUpdateEventArgs : EventArgs
    {
        public DiscordUser user { get; set; }
    }
    public class VoiceStateUpdateEventArgs : EventArgs
    {
        public DiscordVoiceState voice_state { get; set; }
    }
    public class VoiceServerUpdateEventArgs : EventArgs
    {
        public string token { get; set; }
        public string guild_id { get; set; }
        public string endpoint { get; set; }
    }
    public class WebhooksUpdateEventArgs : EventArgs
    {
        public string guild_id { get; set; }
        public string channel_id { get; set; }
    }
    public class InteractionCreateEventArgs : EventArgs
    {
        public DiscordInteraction interaction { get; set; }
    }
    public enum PayloadOpCode
    {
        Dispatch, //receive only
        Heartbeat, //send/receive
        Identify, //send only
        StatusUpdate, //send only
        VoiceStateUpdate, //send only
        Resume = 6, //send only
        Reconnect, //receive only
        RequestGuildMembers, //send only
        InvalidSession, //receive only
        Hello, //receive only
        HeartbeatACK, //receive only
        Undefined //doesn't exist. used to initialize data;
    }
}