using System;
using System.Collections.Generic;
using System.Text;
using AltV.Net;
using AltV.Net.Elements.Entities;

namespace SaltyServer
{
    public class VoiceClient
    {
        public IPlayer Player { get; set; }
        public string TeamSpeakName { get; set; }
        public float VoiceRange { get; set; }
        public bool PhoneSpeaker { get; set; }
        public bool RadioSpeaker { get; set; }

        public VoiceClient(IPlayer player, string teamSpeakName, float voiceRange)
        {
            this.Player = player;
            this.TeamSpeakName = teamSpeakName;
            this.VoiceRange = voiceRange;
        }
    }
}
