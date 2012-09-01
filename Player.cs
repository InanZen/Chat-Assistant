using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;

namespace ChatAssistant
{
    [Flags]
    public enum PlayerSettings
    {
        HideJoinQuitMsg = 1,
        HideDeathMsg = 2        
    }
    public class CAPlayer
    {
        public int Index = -1;
        public TSPlayer TSPlayer;
        public bool quitting = false;
        public bool InMenu = false;
        public Menu Menu;
        public int Channel = -1;
        public ChatMessage[] Log = new ChatMessage[50];
        private int LogCounter = 0;
        public List<int> Ignores = new List<int>();
        public PlayerSettings Flags = 0;
        public CAPlayer(int id = -1)
        {
            this.Index = id;
            if (id != -1)
            {
                this.TSPlayer = TShock.Players[id];
            }
            if (CAMain.Channels[0] != null)
                this.Channel = 0;
            if (CAMain.config.FilterDeathMsgByDefault)
                this.Flags |= PlayerSettings.HideDeathMsg;
            if (CAMain.config.FilterJoinQuitByDefault)
                this.Flags |= PlayerSettings.HideJoinQuitMsg;
        }
        public void AddToLog(ChatMessage msg)
        {
            this.Log[this.LogCounter] = msg;
            this.LogCounter++;
            if (this.LogCounter >= this.Log.Length)
                this.LogCounter = 0;
        }
        public List<ChatMessage> GetPrivateLog(int len)
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
        public List<ChatMessage> GetCombinedLog(int len = 50)
        {
            var playerlog = this.GetPrivateLog(len);
            var globallog = CAMain.GetLog(len);
            var channellog = CAMain.Channels[this.Channel].GetLog(len);
            var combinedLog = playerlog.Union(globallog).Union(channellog).OrderByDescending(m => m.Timestamp).ToList(); // combine 3 logs + sort by timestamp descending (largest = latest on top)
            for (int i = combinedLog.Count - 1; i >= 0; i--)
            {
                if (this.Ignores.Contains(combinedLog[i].Sender))
                    combinedLog.RemoveAt(i);
            }
            if (combinedLog.Count > len) // take required len from the top
                combinedLog = combinedLog.Take(len).ToList();
            combinedLog.Reverse(); // reverse the result
            return combinedLog;
        }

        // ------------------------------ Static -----------------------------
        public static CAPlayer GetPlayerByName(string name, bool caseSensitive = false)
        {
            for (int i = 0; i < CAMain.PlayerList.Length; i++)
            {
                if (CAMain.PlayerList[i] != null && (CAMain.PlayerList[i].TSPlayer.Name == name || (!caseSensitive && CAMain.PlayerList[i].TSPlayer.Name.ToLower() == name.ToLower())))
                    return CAMain.PlayerList[i];
            }
            return null;
        }
    }
}
