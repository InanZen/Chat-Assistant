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
        BlockGlobalChat = 8,
        BlockJoinLeaveMsg = 16
    }
    public class ChannelTemplate
    {
        public String Name;
        public String Password;
        public bool Hidden;
        public bool BlockGlobalChat;
        public bool AnnounceChannelJoinLeave;
        public ChannelFlags GetFlags()
        {
            ChannelFlags flags = (ChannelFlags)1;
            if (this.Password.Length > 0)
                flags |= ChannelFlags.Passworded;
            if (this.Hidden)
                flags |= ChannelFlags.Hidden;
            if (this.BlockGlobalChat)
                flags |= ChannelFlags.BlockGlobalChat;
            if (!this.AnnounceChannelJoinLeave)
                flags |= ChannelFlags.BlockJoinLeaveMsg;
            return flags;
        }
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
            if (player.Channel >= 0 && player.Channel != this.ID && CAMain.Channels[player.Channel] != null)
                CAMain.Channels[player.Channel].LeaveChannel(player);
            player.Channel = this.ID;
            if (!this.Users.Contains(player.Index))
            {
                this.Users.Add(player.Index);
                CAMain.DisplayLog(player, 6);
                if (!this.Flags.HasFlag(ChannelFlags.BlockJoinLeaveMsg))
                    NetMessage.SendData((int)PacketTypes.ChatText, -1, player.Index, String.Format("[Join] {0} has joined the channel", player.TSPlayer.Name), 255, Color.LightSalmon.R, Color.LightSalmon.G, Color.LightSalmon.B, this.ID + 1);
            }
        }
        public void LeaveChannel(CAPlayer player)
        {
            this.Users.Remove(player.Index);
            player.Channel = 0;
            if (!this.Flags.HasFlag(ChannelFlags.Permanent) && this.Users.Count == 0)
                DeleteChannel(this.ID);
            else if (!this.Flags.HasFlag(ChannelFlags.BlockJoinLeaveMsg))
                NetMessage.SendData((int)PacketTypes.ChatText, -1, player.Index, String.Format("[Leave] {0} has left the channel", player.TSPlayer.Name), 255, Color.LightSalmon.R, Color.LightSalmon.G, Color.LightSalmon.B, this.ID + 1);
      
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
            if (id > 0 && CAMain.Channels[id] != null)
            {
                if (CAMain.Channels[id].Flags.HasFlag(ChannelFlags.Permanent))
                    return;
                foreach (int pID in CAMain.Channels[id].Users)
                {
                    if (pID >= 0 && CAMain.PlayerList[pID] != null && CAMain.PlayerList[pID].Channel == id)
                        CAMain.PlayerList[pID].Channel = 0;
                }
                CAMain.Channels[id] = null;
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
        public static List<Channel> GetAll()
        {
            var ReturnList = new List<Channel>();
            for (int i = 0; i < CAMain.Channels.Length; i++)
            {
                if (CAMain.Channels[i] != null)
                    ReturnList.Add(CAMain.Channels[i]);
            }
            return ReturnList;
        }
    }
}
