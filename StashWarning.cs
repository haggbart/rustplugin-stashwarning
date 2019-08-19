using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Stash Warning", "haggbart", "1.2.0")]
    [Description("Logs suspicious stash activity and reports to admins ingame and on discord")]
    class StashWarning : RustPlugin
    {
        [PluginReference] Plugin DiscordMessages;
        
        private const string WEBHOOK_URL = "Webhook URL";
        private const string MESSAGE = "Message";
        private const string IGNORE_SAME_TEAM = "Ignore players in the same team";
        
        private string _webhookUrl;
        private bool _discordEnabled;
        
        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new configuration file");
            Config[WEBHOOK_URL] = "";
            Config[IGNORE_SAME_TEAM] = true;
        }
        
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [MESSAGE] = "{0} ({1}) found a stash that belongs to {2} ({3}) in {4}"
            }, this);
        }
        
        private void Init()
        {
            _webhookUrl = Config[WEBHOOK_URL].ToString();
            if (!string.IsNullOrEmpty(_webhookUrl))
            {
                _discordEnabled = true;
            }
        }
        
        private void CanSeeStash(BasePlayer player, StashContainer stash)
        {
            if (stash.inventory.itemList.Count == 0) return;
            if (player.userID == stash.OwnerID || IsTeamMember(player, stash)) return;
            var iPlayerOwner = covalence.Players.FindPlayerById(stash.OwnerID.ToString());
            var formattedMessage = GetFormattedMessage(player, iPlayerOwner);
            foreach (var target in BasePlayer.activePlayerList.Where(x => x.IsAdmin))
            {
                SendReply(target, GetFormattedMessage(player, iPlayerOwner, target));
            }
            LogToFile(string.Empty, formattedMessage, this); 
            if (_discordEnabled)
                DiscordMessages?.Call("API_SendTextMessage", _webhookUrl, formattedMessage);
        }

        private bool IsTeamMember(BasePlayer player, StashContainer stash)
        {
            if ((bool)Config[IGNORE_SAME_TEAM]) return false;
            var team = RelationshipManager.Instance.FindTeam(player.currentTeam);
            if (team == null) return false;
            foreach (var member in team.members)
            {
                if (member == player.userID)
                {
                    continue;
                }
                if (stash.OwnerID == member)
                {
                    return true;
                }
            }
            return false;
        }

        private string GetFormattedMessage(BasePlayer player, IPlayer iPlayerOwner, BasePlayer target = null)
        {
            var message = target == null ? lang.GetMessage(string.Format(MESSAGE), this) : lang.GetMessage(string.Format(MESSAGE), this, target.UserIDString);
            return string.Format(message, player.displayName, player.userID, iPlayerOwner.Name, iPlayerOwner.Id, GridReference(player.transform.position));
        }

        private static string GridReference(Vector3 pos)
        {
            var worldSize = ConVar.Server.worldsize;
            const float scale = 150f;
            var x = pos.x + worldSize/2f;
            var z = pos.z + worldSize/2f;
            var lat = (int)(x / scale);
            var latChar = (char)('A' + lat);
            var lon = (int)(worldSize/scale - z/scale);
            return $"{latChar}{lon}";
        }
    }
}


