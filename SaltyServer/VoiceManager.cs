using System;
using System.Collections.Generic;
using System.Linq;
using AltV.Net;
using AltV.Net.Elements.Entities;
using System.Numerics;
using AltV.Net.Data;
using System.Globalization;

namespace SaltyServer
{
    public class VoiceManager : IScript
    {
        #region Properties
        public static VoiceManager Instance { get; private set; }

        public string ServerUniqueIdentifier { get; private set; }
        public string MinimumPluginVersion { get; private set; }
        public string SoundPack { get; private set; }
        public string IngameChannel { get; private set; }
        public string IngameChannelPassword { get; private set; }
        public ulong[] SwissChannels { get; private set; } = new ulong[0];

        public Position[] RadioTowers { get; private set; } = new Position[0];
        public VoiceClient[] VoiceClients => this._voiceClients.Values.ToArray();
        private Dictionary<IPlayer, VoiceClient> _voiceClients = new Dictionary<IPlayer, VoiceClient>();

        public RadioChannel[] RadioChannels => this._radioChannels.ToArray();
        private List<RadioChannel> _radioChannels = new List<RadioChannel>();
        #endregion

        #region CTOR
        public VoiceManager()
        {
            VoiceManager.Instance = this;
        }
        #endregion

        #region Server Events
        [ServerEvent("StartServer")]
        public void OnResourceStart()
        {
            this.ServerUniqueIdentifier = "";
            this.MinimumPluginVersion = "2.0.1";
            this.SoundPack = "default";
            this.IngameChannel = IngameChannel = "yourchannelid";
            this.IngameChannelPassword = IngameChannelPassword = "yourchannelpassword";
            //string swissChannelIds = "";
            string swissChannelIds = "";

            if (!String.IsNullOrEmpty(swissChannelIds))
            {
                this.SwissChannels = swissChannelIds.Split(',').Select(s => UInt64.Parse(s.Trim())).ToArray();
            }

            List<Position> towerPositions = new List<Position>();
            towerPositions.Add(new Position(552.8169f, -27.8083f, 94.87936f));
            towerPositions.Add(new Position(758.5276f, 1273.74f, 360.2965f));
            towerPositions.Add(new Position(1857.389f, 3694.529f, 38.9618f));
            towerPositions.Add(new Position(-448.2019f, 6019.807f, 36.62916f));

            this.RadioTowers = towerPositions.ToArray();
        }

        [ServerEvent("PlayerLoggedIn")]
        public void OnPlayerConnected(IPlayer client)
        {
            client.Emit("SaltyChat_OnConnected");
            VoiceClient voiceClient;

            lock (this._voiceClients)
            {
                if (this._voiceClients.ContainsKey(client))
                {
                    this._voiceClients.Remove(client);
                }

                voiceClient = new VoiceClient(client, this.GetTeamSpeakName(), SaltyShared.SharedData.VoiceRanges[1]);
                this._voiceClients.Add(client, voiceClient);

                client.Emit("hud:SetKeyValue", "voice", voiceClient.VoiceRange);
                client.Emit("client::updateVoiceRange", voiceClient.VoiceRange);

                client.Emit(SaltyShared.Event.SaltyChat_Initialize, voiceClient.TeamSpeakName, this.ServerUniqueIdentifier, this.SoundPack, this.IngameChannel, this.IngameChannelPassword, this.SwissChannels, this.RadioTowers);

                foreach (VoiceClient cl in this.VoiceClients)
                {
                    client.Emit(SaltyShared.Event.SaltyChat_UpdateClient, cl.Player.Id, cl.TeamSpeakName, cl.VoiceRange);
                    cl.Player.Emit(SaltyShared.Event.SaltyChat_UpdateClient, voiceClient.Player.Id, voiceClient.TeamSpeakName, voiceClient.VoiceRange);
                }
            }
        }

        [ScriptEvent(ScriptEventType.PlayerDisconnect)]
        public void OnPlayerDisconnected(IPlayer client, string reason)
        {
            VoiceClient voiceClient;

            lock (this._voiceClients)
            {
                if (!this._voiceClients.TryGetValue(client, out voiceClient))
                    return;

                this._voiceClients.Remove(client);
            }

            foreach (RadioChannel radioChannel in this.RadioChannels.Where(c => c.IsMember(voiceClient)))
            {
                radioChannel.RemoveMember(voiceClient);
            }

            foreach (VoiceClient cl in this.VoiceClients)
            {
                cl.Player.Emit(SaltyShared.Event.SaltyChat_Disconnected, voiceClient.Player.Id);
            }
        }
        #endregion

        #region Remote Events
        [ClientEvent(SaltyShared.Event.SaltyChat_CheckVersion)]
        public void OnCheckVersion(IPlayer player, string version)
        {
            if (!this.TryGetVoiceClient(player, out VoiceClient voiceClient))
                return;

            if (!this.IsVersionAccepted(version))
            {
                player.Kick($"[Salty Chat] Required Version: {this.MinimumPluginVersion}");
                return;
            }
        }

        [ClientEvent(SaltyShared.Event.SaltyChat_SetVoiceRange)]
        public void OnSetVoiceRange(IPlayer player, string voiceRange)
        {
            float cVoiceRange;
            if (!float.TryParse(voiceRange, NumberStyles.Any, CultureInfo.InvariantCulture, out cVoiceRange))
                return;

            if (!this.TryGetVoiceClient(player, out VoiceClient voiceClient))
                return;

            if (Array.IndexOf(SaltyShared.SharedData.VoiceRanges, cVoiceRange) >= 0)
            {
                voiceClient.VoiceRange = cVoiceRange;

                foreach (VoiceClient client in this.VoiceClients)
                {
                    client.Player.Emit(SaltyShared.Event.SaltyChat_UpdateClient, player.Id, voiceClient.TeamSpeakName, voiceClient.VoiceRange);
                }
            }


            player.Emit("hud:SetKeyValue", "voice", cVoiceRange);
            player.Emit("client::updateVoiceRange", cVoiceRange);
        }
        #endregion

        #region Commands (Radio)
#if DEBUG
        [ClientEvent("speaker")]
        public void OnSetRadioSpeaker(IPlayer player, string toggleString)
        {
            bool toggle = String.Equals(toggleString, "true", StringComparison.OrdinalIgnoreCase);

            this.SetRadioSpeaker(player, toggle);
        }

        [ClientEvent("joinradio")]
        public void OnJoinRadioChannel(IPlayer player, string channelName)
        {
            this.JoinRadioChannel(player, channelName);
        }

        [ClientEvent("leaveradio")]
        public void OnLeaveRadioChannel(IPlayer player, string channelName)
        {
            this.LeaveRadioChannel(player, channelName);
        }
#endif
        #endregion
        #region Remote Events (Radio)
        [ClientEvent(SaltyShared.Event.SaltyChat_IsSending)]
        public void OnSendingOnRadio(IPlayer player, string radioChannelName, bool isSending)
        {
            if (!this.TryGetVoiceClient(player, out VoiceClient voiceClient))
            {
                return;
            }

            RadioChannel radioChannel = this.GetRadioChannel(radioChannelName, false);

            if (radioChannel == null || !radioChannel.IsMember(voiceClient))
            {
                return;
            }
            radioChannel.Send(voiceClient, isSending);
        }
        #endregion

        #region Methods (Radio)
        public RadioChannel GetRadioChannel(string name, bool create)
        {
            RadioChannel radioChannel;

            lock (this._radioChannels)
            {
                radioChannel = this.RadioChannels.FirstOrDefault(r => r.Name == name);

                if (radioChannel == null && create)
                {
                    radioChannel = new RadioChannel(name);

                    this._radioChannels.Add(radioChannel);
                }
            }

            return radioChannel;
        }

        public void SetRadioSpeaker(IPlayer player, bool toggle)
        {
            if (!this.TryGetVoiceClient(player, out VoiceClient voiceClient))
                return;

            voiceClient.RadioSpeaker = toggle;
        }

        public void JoinRadioChannel(IPlayer player, string radioChannelName)
        {
            if (!this.TryGetVoiceClient(player, out VoiceClient voiceClient))
                return;

            foreach (RadioChannel channel in this.RadioChannels)
            {
                if (channel.IsMember(voiceClient))
                    return;
            }

            RadioChannel radioChannel = this.GetRadioChannel(radioChannelName, true);

            radioChannel.AddMember(voiceClient);
        }

        public void LeaveRadioChannel(IPlayer player)
        {
            if (!this.TryGetVoiceClient(player, out VoiceClient voiceClient))
                return;

            foreach (RadioChannel radioChannel in this.RadioChannels.Where(r => r.IsMember(voiceClient)))
            {
                this.LeaveRadioChannel(player, radioChannel.Name);
            }
        }

        public void LeaveRadioChannel(IPlayer player, string radioChannelName)
        {
            if (!this.TryGetVoiceClient(player, out VoiceClient voiceClient))
                return;

            RadioChannel radioChannel = this.GetRadioChannel(radioChannelName, false);

            if (radioChannel != null)
            {
                radioChannel.RemoveMember(voiceClient);

                if (radioChannel.Members.Length == 0)
                {
                    this._radioChannels.Remove(radioChannel);
                }
            }
        }

        public void SendingOnRadio(IPlayer player, string radioChannelName, bool isSending)
        {
            if (!this.TryGetVoiceClient(player, out VoiceClient voiceClient))
                return;

            RadioChannel radioChannel = this.GetRadioChannel(radioChannelName, false);

            if (radioChannel == null || !radioChannel.IsMember(voiceClient))
                return;

            radioChannel.Send(voiceClient, isSending);
        }
        #endregion

        #region Methods
        internal string GetTeamSpeakName()
        {
            string name;

            do
            {
                name = Guid.NewGuid().ToString().Replace("-", "");

                if (name.Length > 30)
                {
                    name = name.Remove(29, name.Length - 30);
                }
            }
            while (this._voiceClients.Values.Any(c => c.TeamSpeakName == name));

            return name;
        }

        public bool IsVersionAccepted(string version)
        {
            if (!String.IsNullOrWhiteSpace(this.MinimumPluginVersion))
            {
                try
                {
                    string[] minimumVersionArray = this.MinimumPluginVersion.Split('.');
                    string[] versionArray = version.Split('.');

                    int lengthCounter = 0;

                    if (versionArray.Length >= minimumVersionArray.Length)
                    {
                        lengthCounter = minimumVersionArray.Length;
                    }
                    else
                    {
                        lengthCounter = versionArray.Length;
                    }

                    for (int i = 0; i < lengthCounter; i++)
                    {
                        int min = Convert.ToInt32(minimumVersionArray[i]);
                        int cur = Convert.ToInt32(versionArray[i]);

                        if (cur > min)
                        {
                            return true;
                        }
                        else if (min > cur)
                        {
                            return false;
                        }
                    }
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }
        #endregion

        #region Helper
        public bool TryGetVoiceClient(IPlayer client, out VoiceClient voiceClient)
        {
            lock (this._voiceClients)
            {
                if (this._voiceClients.TryGetValue(client, out voiceClient))
                    return true;
            }

            voiceClient = null;
            return false;
        }
        #endregion
    }
}
