using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Terraria;

namespace ChatAssistant
{
    [Flags]
    public enum ChannelFlags
    {
        None = 0,
        Permanent = 1,
        Passworded = 2,
        Hidden = 4,
        BlockGlobalChat = 8
    }
    public class ChannelTemplate
    {
        public String Name;
        public ChannelFlags Flags;
        public String Password;
    }
    public class Channel
    {
        public int ID;
        public String Name;
        public ChannelFlags Flags;
        public string Password;
        public ChatMessage[] Log = new ChatMessage[100];
        private int LogCounter = 0;
        public List<int> Users = new List<int>();
        public Channel(int id, string name) : this(id, name, 0, "") { }
        public Channel(int id, string name, ChannelFlags flags, string password)
        {
            this.ID = id;
            this.Name = name;
            this.Flags = flags;
            this.Password = password;
            if (password != "")
                this.Flags |= ChannelFlags.Passworded;
        }
        public void JoinChannel(CAPlayer player)
        {
            if (player == null) 
                return;
            player.Channel = this.ID;
            if (!this.Users.Contains(player.Index))
                this.Users.Add(player.Index);                           
            CAMain.DisplayLog(player, 6);
            NetMessage.SendData((int)PacketTypes.ChatText, -1, -1, String.Format("[Join] {0} has joined the channel", player.TSPlayer.Name), 255, Color.LightSalmon.R, Color.LightSalmon.G, Color.LightSalmon.B, player.Channel + 1);
        }
        public void LeaveChannel(CAPlayer player)
        {

        }
        public void AddToLog(ChatMessage msg)
        {
            this.Log[this.LogCounter] = msg;
            this.LogCounter++;
            if (this.LogCounter >= this.Log.Length)
                this.LogCounter = 0;
        }
        public List<ChatMessage> GetLog(int len)
        {
            List<ChatMessage> ReturnList = new List<ChatMessage>();
            int count = 0;
            for (int i = 1; i < Log.Length; i++)
            {
                if (count >= len)
                    break;
                int index = LogCounter - i;
                if (index < 0)
                    index = Log.Length - 1 + index;
                if (Log[index] != null)
                {
                    ReturnList.Add(Log[index]);
                    count++;
                }
            }
            //ReturnList.Reverse();
            return ReturnList;
        }


        //  ------------------ STATIC Methods ------------------------------------------
        public static int CreateChannel(string name, string password = "")
        {
            int chID = GetEmpty();
            if (chID == -1)
                return -1;
            CAMain.Channels[chID] = new Channel(chID, name, 0, password);
            return chID;
        }
        public static void DeleteChannel(int id)
        {
            if (id >= 0 && CAMain.Channels[id] != null)
            {
                foreach (CAPlayer player in CAMain.PlayerList)
                {
                    if (player.Channel == id)
                        player.Channel = 0;
                }
            }
        }
        public static int GetEmpty()
        {
            for (int i = 0; i < CAMain.Channels.Length; i++)
            {
                if (CAMain.Channels[i] == null)
                    return i;
            }
            return -1;
        }
        public static List<Channel> GetPermanent()
        {
            var ReturnList = new List<Channel>();
            for (int i = 0; i < CAMain.Channels.Length; i++)
            {
                if (CAMain.Channels[i] != null && CAMain.Channels[i].Flags.HasFlag(ChannelFlags.Permanent))
                    ReturnList.Add(CAMain.Channels[i]);
            }
            return ReturnList;
        }
        public static List<Channel> GetNonHidden()
        {
            var ReturnList = new List<Channel>();
            for (int i = 0; i < CAMain.Channels.Length; i++)
            {
                if (CAMain.Channels[i] != null && !CAMain.Channels[i].Flags.HasFlag(ChannelFlags.Hidden))
                    ReturnList.Add(CAMain.Channels[i]);
            }
            return ReturnList;
        }
    }
}
