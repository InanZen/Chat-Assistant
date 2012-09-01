using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Data;
using System.ComponentModel;
using Terraria;
using Hooks;
using TShockAPI;
using TShockAPI.DB;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace ChatAssistant
{
    [APIVersion(1, 12)]
    public class CAMain : TerrariaPlugin
    {        

        public static CAPlayer[] PlayerList = new CAPlayer[256];
        public static ChatMessage[] GlobalLog = new ChatMessage[100];
        private static int LogCounter = 0;
        public delegate void MenuAction(Object sender, MenuEventArgs args);
        public static Channel[] Channels = new Channel[256];
        public static String Savepath = Path.Combine(TShock.SavePath, "Chat Assistant/");
        internal static CAconfig config;
        public static String[] DeathMessages = new String[] 
        {
            "face was torn off",
            "entrails were ripped out",
            "skull was crushed",
            "extremities were detached",
            "body was mangled",
            "vital organs were ruptured",
            "plead for death was finally answered",
            "meat was ripped off the bone",
            "flailing about was finally stopped",
            "got massacred",
            "got impaled",
            "got snapped in half",
            "got melted",
            "had their head removed",
            "fell to their death",
            "didn't bounce",
            "forgot to breathe",
            "is sleeping with the fish",
            "is shark food",
            "drowned",
            "tried to swim in lava",
            "likes to play in magma", 
            "couldn't put out the fire",
            "couldn't find the antidote", 
            "tried to escape",
            "let their arms get torn off",
            "watched their innards become outards",
            "was slain",
            "was eviscerated",
            "was murdered",
            "was licked",
            "was incinerated",
            "was cut down the middle",
            "was chopped up",
            "was turned into a pile of flesh",
            "was removed from",
            "was torn in half",
            "was decapitated",
            "was brutally dissected",
            "was destroyed"
        };

        public override string Name
        {
            get { return "ChatAssistant"; }
        }
        public override string Author
        {
            get { return "by InanZen"; }
        }
        public override string Description
        {
            get { return "Provides Various Chat Feauters and Utilities"; }
        }
        public override Version Version
        {
            get { return new Version("0.42"); }
        }
        public CAMain(Main game)
            : base(game)
        {
            Order = -1;
        }
        public override void Initialize()
        {
            NetHooks.GetData += GetData;
            NetHooks.SendData += SendData;
            ServerHooks.Join += OnJoin;
            ServerHooks.Leave += OnLeave;
            ServerHooks.Chat += OnChat;
            Commands.ChatCommands.Add(new Command("CA.channel.cmd", ChannelCommand, "ch", "channel"));
            Commands.ChatCommands.Add(new Command("CA.ignore.cmd", IgnoreCommand, "ignore"));


            if (!Directory.Exists(Savepath))
                Directory.CreateDirectory(Savepath);
            var permChanPath = Path.Combine(Savepath, "PermChannels.conf");
            if (!File.Exists(permChanPath))
            {
                List<ChannelTemplate> defaultChannels = new List<ChannelTemplate>();
                defaultChannels.Add(new ChannelTemplate { Name = "Global", Password = "", AnnounceChannelJoinLeave = false, BlockGlobalChat = false, Hidden = false });
                /*var defaultChannel = new Channel(0, "Global", 1);
                List<Object> newlist = new List<Object>() { new { defaultChannel.Name, defaultChannel.Flags, defaultChannel.Password } };*/

                using (var stream = new FileStream(permChanPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
                {
                    using (var sr = new StreamWriter(stream))
                    {
                        sr.Write(JsonConvert.SerializeObject(defaultChannels, Formatting.Indented));
                    }
                    stream.Close();
                }
            }
                     
            using (var stream = new FileStream(permChanPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var sr = new StreamReader(stream))
                {
                    var permChanList = JsonConvert.DeserializeObject<List<ChannelTemplate>>(sr.ReadToEnd());
                    for (int i = 0; i < permChanList.Count; i++)
                    {
                        Channels[i] = new Channel(i, permChanList[i].Name, permChanList[i].GetFlags(), permChanList[i].Password);
                    }                        
                }
                stream.Close();
            }
            config = CAconfig.Load();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                NetHooks.GetData -= GetData;
                NetHooks.SendData -= SendData;
                ServerHooks.Leave -= OnLeave;
                ServerHooks.Join -= OnJoin;
                ServerHooks.Chat -= OnChat;
            }
            base.Dispose(disposing);
        }
        private static void OnJoin(int who, HandledEventArgs args)
        {
            try
            {
                lock (PlayerList)
                {
                    PlayerList[who] = new CAPlayer(who);
                }
                if (Channels[0] != null)
                    Channels[0].JoinChannel(PlayerList[who]);
            }
            catch (Exception ex)
            {
                Log.ConsoleError(ex.ToString());
            }

        }
        private static void OnLeave(int who)
        {
            try
            {
                if (who >= 0 && PlayerList[who] != null)
                {
                    lock (PlayerList)
                    {
                        PlayerList[who].quitting = true;
                        if (PlayerList[who].InMenu)
                            PlayerList[who].Menu.Close(true);
                        if (PlayerList[who].Channel >= 0)
                        {
                            var chan = Channels[PlayerList[who].Channel];
                            if (chan != null)
                                chan.LeaveChannel(PlayerList[who]);
                        }
                        PlayerList[who] = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ConsoleError(ex.ToString());
            }
        }
        void ChannelCommand(CommandArgs args)
        {
            try
            {
                var player = PlayerList[args.Player.Index];
                if (player == null)
                    return;
                if (args.Parameters.Count > 0)
                {
                    int j = -1;
                    for (int i = 0; i < Channels.Length; i++)
                    {
                        if (Channels[i] != null)
                        {
                            if (Channels[i].Name.ToLower() == args.Parameters[0].ToLower())
                            {
                                if (Channels[i].ID == player.Channel)
                                {
                                    args.Player.SendMessage("You are already in this channel", Color.Red);
                                    return;
                                }
                                if (Channels[i].Password != "" && !args.Player.Group.HasPermission("CA.channel.bypass.password") && (args.Parameters.Count < 2 || args.Parameters[1] != Channels[i].Password)) // incorrect password
                                {
                                    args.Player.SendMessage("This channel is locked, please provide the correct password.", Color.Red);
                                    return;
                                }
                                Channels[i].JoinChannel(player);
                                return;
                            }
                        }
                        else if (j == -1)
                            j = i;
                    }
                    if (j != -1) // channel not found
                    {
                        if (args.Player.Group.HasPermission("CA.channel.create")) //create new channel
                        {
                            var newchannel = new Channel(j, args.Parameters[0]);
                            if (args.Parameters.Count > 1)
                                newchannel.Password = args.Parameters[1];
                            Channels[j] = newchannel;
                            NetMessage.SendData((int)PacketTypes.ChatText, -1, -1, "New channel created", 255, Color.LightSalmon.R, Color.LightSalmon.G, Color.LightSalmon.B, j + 1);
                            Channels[j].JoinChannel(player);
                            return;
                        }
                        args.Player.SendMessage("Channel not found and you do not have permission to create a new channel", Color.LightSalmon);
                        return;
                    }
                    args.Player.SendMessage("Channel not found and all channel slots are full", Color.Red);
                    return;
                }
                args.Player.SendMessage("Syntax: /ch <channel name> [<password>]", Color.Red);
            }
            catch (Exception ex)
            {
                Log.ConsoleError(ex.ToString());
            }
        }
        void IgnoreCommand(CommandArgs args)
        {
            try
            {
                var player = PlayerList[args.Player.Index];
                if (player != null)
                {
                    if (args.Parameters.Count > 0)
                    {
                        string plyName = string.Join(" ", args.Parameters.GetRange(0, args.Parameters.Count));
                        var ignorePlayer = CAPlayer.GetPlayerByName(plyName);
                        if (ignorePlayer != null)
                        {
                            if (ignorePlayer.TSPlayer.Name == player.TSPlayer.Name)
                                args.Player.SendMessage("You cannot ignore yourself..", Color.Red);
                            else if (ignorePlayer.TSPlayer.Group.HasPermission("CA.ignore.bypass"))
                                args.Player.SendMessage(String.Format("Player {0} cannot be ignored", ignorePlayer.TSPlayer.Name), Color.Red);
                            else if (player.Ignores.Remove(ignorePlayer.Index))
                                args.Player.SendMessage(String.Format("Player {0} removed from your Ignore list", ignorePlayer.TSPlayer.Name), Color.DarkGreen);
                            else
                            {
                                player.Ignores.Add(ignorePlayer.Index);
                                args.Player.SendMessage(String.Format("Player {0} added to your Ignore list", ignorePlayer.TSPlayer.Name), Color.DarkGreen);
                            }
                        }
                        else
                            args.Player.SendMessage(String.Format("Player {0} is not on this server", plyName), Color.Red);
                    }
                    else
                        args.Player.SendMessage("Syntax: /ignore <player name> - toggle ignore player messages", Color.Red);
                }
            }
            catch (Exception ex)
            {
                Log.ConsoleError(ex.ToString());
            }
        }
        void OnChat(messageBuffer buf, int who, string text, HandledEventArgs args)
        {
            if (text[0] == '/')
                return;
            var player = PlayerList[who];
            if (player != null)
            {
                if (player.InMenu)
                {
                    if (player.Menu.contents[player.Menu.index].Writable)
                        player.Menu.OnInput(text);
                    args.Handled = true;
                }
                else if (!player.TSPlayer.mute)
                {
                    var playerGroup = player.TSPlayer.Group;
                    NetMessage.SendData((int)PacketTypes.ChatText, -1, who, String.Format(TShock.Config.ChatFormat, playerGroup.Name, playerGroup.Prefix, player.TSPlayer.Name, playerGroup.Suffix, text), 255, playerGroup.R, playerGroup.G, playerGroup.B, player.Channel + 1);
                    args.Handled = true;
                }
            }
        }
        public static void GetData(GetDataEventArgs e)
        {
            try
            {
                if (e.MsgID == PacketTypes.PlayerUpdate)
                {
                    byte plyID = e.Msg.readBuffer[e.Index];
                    byte flags = e.Msg.readBuffer[e.Index + 1]; 
                    bool up = false;
                    bool down = false;
                    bool space = false;
                    if ((flags & 1) == 1)
                        up = true;
                    if ((flags & 2) == 2)
                        down = true;
                    if ((flags & 16) == 16)
                        space = true;
                    var player = PlayerList[plyID];
                    if (player != null)
                    {
                        if (player.InMenu)  // HANDLE MENU NAVIGATION
                        {
                            if (up && down)
                            {
                                player.Menu.Close();
                                e.Handled = true;
                                return;
                            }
                            if (up)
                            {
                                player.Menu.MoveUp();
                                e.Handled = true;
                            }
                            if (down)
                            {
                                player.Menu.MoveDown();
                                e.Handled = true;
                            }
                            if (space)
                            {
                                player.Menu.Select();
                                e.Handled = true;
                            }
                        }
                        else
                        {
                            if (up && down) // Show main menu
                            {
                                Menu.DisplayMainMenu(player);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Log.ConsoleError(ex.ToString()); }
        }
        public static void SendData(SendDataEventArgs e)
        {
            if (e.Handled)
                return;
            try
            {
                if (e.MsgID == PacketTypes.ChatText)
                {
                     //Log.ConsoleInfo(String.Format("ChatText> 1: {0}, 2: {4}, 3: {5}, 4: {6}, 5: {1}, remote: {2}, ignore: {3}", e.number, e.number5, e.remoteClient, e.ignoreClient, e.number2, e.number3, e.number4));
                    int sender = e.ignoreClient; // -1 = system
                    if (e.remoteClient == -1) // message to all players
                    {
                        int channel = e.number5 - 1;
                        MsgType msgType = MsgType.Global;
                        if (channel >= 0)
                        {
                            msgType = MsgType.Channel;
                            Channels[channel].AddToLog(new ChatMessage(e.text, new Color((byte)e.number2, (byte)e.number3, (byte)e.number4), msgType, sender)); // add to channel log
                        }
                        else
                        {
                            AddLogItem(new ChatMessage(e.text, new Color((byte)e.number2, (byte)e.number3, (byte)e.number4), msgType, sender)); // add to global log           
                            if (config.EnableDeathMsgFilter || config.EnableJoinQuitFilter)
                            {
                                var plyName = StartsWithPlayerName(e.text);
                                if (plyName != "")
                                {
                                    if (config.EnableJoinQuitFilter && e.text == String.Format("{0} has joined.", plyName))
                                        msgType = MsgType.Join;
                                    else if (config.EnableJoinQuitFilter && e.text == String.Format("{0} left", plyName))
                                        msgType = MsgType.Quit;
                                    else if (config.EnableDeathMsgFilter && IsDeathMsg(e.text, plyName))
                                        msgType = MsgType.Death;
                                }
                            }
                        }
                        for (int i = 0; i < PlayerList.Length; i++)
                        {
                            if (PlayerList[i] != null && !PlayerList[i].InMenu && !PlayerList[i].Ignores.Contains(sender) && (msgType == MsgType.Global || channel == PlayerList[i].Channel || (msgType == MsgType.Death && !PlayerList[i].Flags.HasFlag(PlayerSettings.HideDeathMsg)) || ((msgType == MsgType.Join || msgType == MsgType.Quit) && !PlayerList[i].Flags.HasFlag(PlayerSettings.HideJoinQuitMsg))))
                                NetMessage.SendData((int)PacketTypes.ChatText, PlayerList[i].Index, sender, e.text, 255, e.number2, e.number3, e.number4, 1); // custom message > e.number5 = 1, e.ignoreClient = sender
                        }
                        String logMessage = String.Format("[Chat][{1}]{2} {0}", e.text, msgType.ToString(), (msgType == MsgType.Channel && Channels[channel] != null) ? String.Format("[{0}]", Channels[channel].Name) : "");
                        Console.WriteLine(logMessage);
                        Log.Data(logMessage);
                        e.Handled = true;
                    }
                    else // message for player id = e.remoteClient
                    {
                        var player = PlayerList[e.remoteClient];
                        if (player == null)
                            return;
                        if (e.number5 == 0) // default message
                        {
                            if (e.text.StartsWith("(Whisper From)<"))
                            {
                                var senderName = e.text.Substring(15, e.text.IndexOf('>', 14) - 15);
                                var wPly = CAPlayer.GetPlayerByName(senderName);
                                if (wPly != null && player.Ignores.Contains(wPly.Index))  //  Private message from ignored player
                                {
                                    e.Handled = true;
                                    return;
                                }
                            }
                            player.AddToLog(new ChatMessage(e.text, new Color((byte)e.number2, (byte)e.number3, (byte)e.number4), MsgType.Private, sender));
                            if (player.InMenu)
                                e.Handled = true;
                        }
                    }                                        
                }
            }
            catch (Exception ex) { Log.ConsoleError(ex.ToString()); }
        }
        private static void AddLogItem(ChatMessage msg)
        {
            GlobalLog[LogCounter] = msg;
            LogCounter++;
            if (LogCounter >= GlobalLog.Length)
                LogCounter = 0;
        }
        public static void DisplayLog(CAPlayer player, int offset)
        {
            if (player == null)
                return;
            var log = player.GetCombinedLog(offset);
            foreach (ChatMessage msg in log)
            {
                player.TSPlayer.SendData(PacketTypes.ChatText, msg.Text, 255, msg.Color.R, msg.Color.G, msg.Color.B, 1);
            }
        }
        public static List<ChatMessage> GetLog(int len)
        {
            List<ChatMessage> ReturnList = new List<ChatMessage>();
            int count = 0;
            for (int i = 1; i < GlobalLog.Length; i++)
            {
                if (count >= len)
                    break;
                int index = LogCounter - i;
                if (index < 0)
                    index = GlobalLog.Length - 1 + index;
                if (GlobalLog[index] != null)
                {
                    ReturnList.Add(GlobalLog[index]);
                    count++;
                }
            }
            //ReturnList.Reverse();
            return ReturnList;
        }

        public static bool IsDeathMsg(string text, string playerName)
        {
            if (text.StartsWith(playerName))
            {
                if (text.Length == playerName.Length)
                    return true;
                try
                {
                    for (int i = 0; i < DeathMessages.Length; i++)
                    {
                        if (text.Contains(DeathMessages[i]))
                            return true;
                    }                    
                }
                catch (Exception ex) { Log.ConsoleError(ex.ToString()); }
            }
            return false;
        }
        public static string StartsWithPlayerName(string text)
        {
            var split = text.Split(' ');
            if (split.Length > 0)
            {
                foreach (Player ply in Main.player)
                {
                    if (ply != null && ply.name == split[0])
                        return ply.name;
                }
            }
            return "";
        }
    }   

}
