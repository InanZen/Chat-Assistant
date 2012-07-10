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
    public class MenuEventArgs : HandledEventArgs
    {
        public MenuEventArgs(List<MenuItem> contents, int playerID) : this(contents, playerID, -1, 0) { }
        public MenuEventArgs(List<MenuItem> contents, int playerID, int selection) : this(contents, playerID, selection, 1) { }            
        public MenuEventArgs(List<MenuItem> contents, int playerID, int selection, byte status)
        {
            this.Data = contents;
            this.Selected = selection;
            this.PlayerID = playerID;
            this.Status = status;
        }
        public int Selected;
        public byte Status;
        public List<MenuItem> Data;
        public int PlayerID;
    }
    public class MenuItem
    {
        public String Text;
        public Color Color;
        public int Value;
        public bool Selectable;
        public bool Writable;
        public String Input;
        public MenuItem(MenuItem menuitem)
        {
            this.Text = menuitem.Text;
            this.Value = menuitem.Value;
            this.Selectable = menuitem.Selectable;
            this.Writable = menuitem.Writable;
            this.Color = menuitem.Color;
            this.Input = menuitem.Input;
        }
        public MenuItem(String text) : this(text, -1) { }
        public MenuItem(String text, int value) : this(text, value, true, false) { }
        public MenuItem(String text, int value, Color color) : this(text, value, true, false, color) { }
        public MenuItem(String text, int value, bool selectable, Color color) : this(text, value, selectable, false, color) { }
        public MenuItem(String text, int value, bool selectable, bool writable) : this(text, value, selectable, writable, Color.White) { }
        public MenuItem(String text, int value, bool selectable, bool writable, Color color)
        {
            this.Text = text;
            this.Color = color;
            this.Value = value;
            this.Selectable = selectable;
            this.Writable = writable;
            this.Input = "";
        }
    }
    struct ChatMessage
    {
        public String Text;
        public Color Color;
        public int Recipient;
        public int Channel;
        public ChatMessage(String txt, Color c, int rec = -1, int chan = 0 )
        {
            this.Color = c;
            this.Text = txt;
            this.Recipient = rec;
            this.Channel = chan;
        }
        /*public ChatMessage(String txt, int r, int g, int b) : this(txt, new Color(r, g, b)) { }   */     
    }
    class Channel
    {
        public int ID;
        public String Name;
        public byte Flags;
        public string Password;
        public Channel(int id, string name) : this(id, name, 0, "") { }
        public Channel(int id, string name, byte flags) : this(id, name, flags, "") { }
        [JsonConstructor]
        public Channel(int id, string name, byte flags, string password)
        {
            this.ID = id;
            this.Name = name;
            this.Flags = flags;
            this.Password = password;
        }

    }
    [APIVersion(1, 12)]
    public class Chat : TerrariaPlugin
    {

        public class Menu
        {
            public int PlayerID;
            public string title;
            public List<MenuItem> contents;
            public int index = 0;
            public bool header = true;
            public MenuAction MenuActionHandler;

            public Menu(int playerid, String title, List<MenuItem> contents, MenuAction del = null)
            {
                this.contents = contents;
                this.PlayerID = playerid;
                this.title = title;
                if (del != null)
                    this.MenuActionHandler = del;
                if (PlayerList[playerid] != null)
                    PlayerList[playerid].InMenu = true;
                this.DisplayMenu();
            }
            public void DisplayMenu()
            {
                var player = TShock.Players[this.PlayerID];
                if (player != null)
                {
                    int j = -2;
                    if (this.header)
                        player.SendData(PacketTypes.ChatText, String.Format("{0}: (Move: [up,down] - Select: [spacebar] - Exit: [up+down])", this.title), 255, Color.DarkSalmon.R, Color.DarkSalmon.G, Color.DarkSalmon.B, 1);
                    else
                        j = -3;
                     for (int i = j; i <= 3; i++)
                    {
                        if (i == 0)
                        {                             
                            if (this.contents[this.index].Writable)
                                player.SendData(PacketTypes.ChatText, (this.contents[this.index + i].Text.Contains("@0")) ? this.contents[this.index + i].Text.Replace("@0", String.Format(">{0}<",this.contents[this.index + i].Input)) : String.Format("{0}>{1}<", this.contents[this.index].Text, this.contents[this.index].Input), 255, this.contents[this.index].Color.R, this.contents[this.index].Color.G, this.contents[this.index].Color.B, 1);               
                            else if (this.contents[this.index].Selectable)
                                player.SendData(PacketTypes.ChatText, (this.contents[this.index + i].Text.Contains("@0")) ? String.Format("> {0} <", this.contents[this.index + i].Text.Replace("@0", this.contents[this.index + i].Input)) : String.Format("> {0}{1} <", this.contents[this.index].Text, this.contents[this.index].Input), 255, this.contents[this.index].Color.R, this.contents[this.index].Color.G, this.contents[this.index].Color.B, 1);
                            else
                                player.SendData(PacketTypes.ChatText, (this.contents[this.index + i].Text.Contains("@0")) ? this.contents[this.index + i].Text.Replace("@0", this.contents[this.index + i].Input) : this.contents[this.index].Text, 255, this.contents[this.index].Color.R, this.contents[this.index].Color.G, this.contents[this.index].Color.B, 1);
                        }
                        else if (this.index + i < 0 || this.index + i >= this.contents.Count)
                            player.SendData(PacketTypes.ChatText, "", 255, 0f, 0f, 0f, 1);
                        else
                            player.SendData(PacketTypes.ChatText, (this.contents[this.index + i].Text.Contains("@0")) ? this.contents[this.index + i].Text.Replace("@0",this.contents[this.index + i].Input) : String.Format("{0}{1}", this.contents[this.index + i].Text, this.contents[this.index + i].Input), 255, this.contents[this.index + i].Color.R, this.contents[this.index + i].Color.G, this.contents[this.index + i].Color.B, 1);
                         
                     }
                }
            }
            public void MoveDown()
            {
                if (this.index + 1 < this.contents.Count)
                {
                    this.index++;
                    this.DisplayMenu();
                }
            }
            public void MoveUp()
            {
                if (this.index - 1 >= 0)
                {
                    this.index--;
                    this.DisplayMenu();
                }
            }
            public void Close(int value = -1)
            {
                var player = PlayerList[this.PlayerID];
                if (player != null)
                {
                    MenuEventArgs args = new MenuEventArgs(this.contents, this.PlayerID, value, (value == -1) ? (byte)0 : (byte)1);
                    if (this.MenuActionHandler != null)
                        this.MenuActionHandler(this, args);
                    if (!args.Handled)
                    {
                        player.InMenu = false;
                        player.Menu = null;
                        DisplayLog(player, 7);
                    }
                }
            }
            public void Select()
            {
                if (this.contents[this.index].Selectable)
                    this.Close(this.index);
            }
            public void OnInput(String text)
            {
                var player = PlayerList[this.PlayerID];
                if (player != null)
                {
                    string oldinput = this.contents[this.index].Input;
                    this.contents[this.index].Input = text;
                    MenuEventArgs args = new MenuEventArgs(this.contents, this.PlayerID, this.index, 2);
                    if (this.MenuActionHandler != null)
                        this.MenuActionHandler(this, args);
                    if (!args.Handled)                    
                        DisplayMenu();                    
                    else
                        this.contents[this.index].Input = oldinput;
                }
            }
            public MenuItem GetItemByValue(int value)
            {
                foreach (MenuItem item in this.contents)
                {
                    if (item.Value == value)
                        return item;
                }
                return null;
            }
        }
        private class CAPlayer
        {
            public int Index = -1;
            public TSPlayer TSPlayer;
            public bool InMenu = false;
            public Menu Menu;
            public int Channel = 0;
            public CAPlayer(int id = -1)
            {
                this.Index = id;
                if (id != -1)
                {
                    this.TSPlayer = TShock.Players[id];
                }
                if (Channels[0] != null)
                    this.Channel = 1;
            }

        }
        private static CAPlayer[] PlayerList = new CAPlayer[256];
        private static ChatMessage[] ChatLog = new ChatMessage[200];
        private static int LogCounter = 0;
        public delegate void MenuAction(Object sender, MenuEventArgs args);
        private static Channel[] Channels = new Channel[256];
        private static String Savepath = Path.Combine(TShock.SavePath, "Chat Assistant/");

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
            get { return new Version("0.25"); }
        }
        public Chat(Main game)
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
            Commands.ChatCommands.Add(new Command("channel", ChannelCommand, "ch", "channel"));

            if (!Directory.Exists(Savepath))
            {
                Directory.CreateDirectory(Savepath);
                List<Channel> defaultChannels = new List<Channel>();
                defaultChannels.Add(new Channel(1, "Global", 1));
                using (var stream = new FileStream(Path.Combine(Savepath, "PermChannels.json"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
                {
                    using (var sr = new StreamWriter(stream))
                    {
                        sr.Write(JsonConvert.SerializeObject(defaultChannels, Formatting.Indented));
                    }
                    stream.Close();
                }                
            }
            using (var stream = new FileStream(Path.Combine(Savepath, "PermChannels.json"), FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var sr = new StreamReader(stream))
                {
                    var permChanList = JsonConvert.DeserializeObject<List<Channel>>(sr.ReadToEnd());
                    int index = 0;
                    for (int i = 0; i < Channels.Length; i++)
                    {
                        if (index >= permChanList.Count)
                            break;
                        if (Channels[i] == null)
                        {
                            Channels[i] = new Channel(i + 1, permChanList[index].Name, permChanList[index].Flags, permChanList[index].Password);
                            index++;
                        }
                    }                        
                }
                stream.Close();
            }

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
                lock (PlayerList)
                {
                    PlayerList[who] = null;
                }
            }
            catch (Exception ex)
            {
                Log.ConsoleError(ex.ToString());
            }
        }
        void ChannelCommand(CommandArgs args)
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
                            if (Channels[i].Password != "" && (args.Parameters.Count < 2 || args.Parameters[1] != Channels[i].Password)) // incorrect password
                            {
                                args.Player.SendMessage("This channel is locked, please provide the correct password.", Color.Red);
                                return;
                            }
                            player.Channel = Channels[i].ID; // join channel
                            NetMessage.SendData((int)PacketTypes.ChatText, -1, -1, String.Format("{0} has joined the channel", player.TSPlayer.Name), 255, Color.LightSalmon.R, Color.LightSalmon.G, Color.LightSalmon.B, player.Channel);
                            return;                            
                        }
                    }
                    else if (j == -1)
                        j = i;                    
                }
                if (j != -1) // channel not found
                {
                    if (args.Player.Group.HasPermission("channelcreate")) //create new channel
                    {
                        var newchannel = new Channel(j+1, args.Parameters[0]);
                        if (args.Parameters.Count > 1)
                            newchannel.Password = args.Parameters[1];
                        Channels[j] = newchannel;
                        player.Channel = j+1;
                        NetMessage.SendData((int)PacketTypes.ChatText, -1, -1, "New channel created", 255, Color.LightSalmon.R, Color.LightSalmon.G, Color.LightSalmon.B, player.Channel);
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
                }
                else
                {
                    var playerGroup = player.TSPlayer.Group;
                    NetMessage.SendData((int)PacketTypes.ChatText, -1, -1, String.Format(TShock.Config.ChatFormat, playerGroup.Name, playerGroup.Prefix, player.TSPlayer.Name, playerGroup.Suffix, text), 255, playerGroup.R, playerGroup.G, playerGroup.B, player.Channel); 
                }
                args.Handled = true;
            }
        }
        public static void GetData(GetDataEventArgs e)
        {
            try
            {
                if (e.MsgID == PacketTypes.PlayerUpdate)
                {
                    byte plyID, flags;
                    using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
                    {
                        var reader = new BinaryReader(data);
                        plyID = reader.ReadByte();
                        flags = reader.ReadByte();
                        reader.Close();
                        reader.Dispose();
                    }
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
                        if (player.InMenu)
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
                            if (up && down)
                            {     
                                player.Menu = new Menu(player.Index, String.Format("Log [{0}]",DateTime.Now.ToString()), GetLog(player.Index, player.Channel));
                                player.Menu.index = player.Menu.contents.Count - 1;
                                player.Menu.DisplayMenu();
                            }
                        }

                    }
                }
                /*else if (e.MsgID == PacketTypes.ChatText)
                {
                    byte PlayerID = e.Msg.readBuffer[e.Index];
                    var player = PlayerList[PlayerID];
                    if (player != null)
                    {
                        Console.WriteLine("Player {0} chat text", player.TSPlayer.Name);
                    }
                */
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
                   // Console.WriteLine("1: {0}, 2: {4}, 3: {5}, 4: {6}, 5: {1}, remote: {2}, ignore: {3}", e.number, e.number5, e.remoteClient, e.ignoreClient, e.number2, e.number3, e.number4);

                    if (e.remoteClient == -1) // message to all players
                    {
                        int channel = e.number5;
                        foreach (TSPlayer tsply in TShock.Players)
                        {
                            if (tsply != null && tsply.Index >= 0)
                            {
                                var player = PlayerList[tsply.Index];
                                if (player != null && (channel == 0 || channel == player.Channel) && !player.InMenu)                                
                                    player.TSPlayer.SendData(PacketTypes.ChatText, e.text, 255, e.number2, e.number3, e.number4, 1);
                                
                            }
                        }
                        AddLogItem(new ChatMessage(e.text, new Color((byte)e.number2, (byte)e.number3, (byte)e.number4), -1, channel));
                        e.Handled = true;
                    }
                    else // message for player id = e.remoteClient
                    {
                        var player = PlayerList[e.remoteClient];
                        if (player == null)
                            return;
                        if (e.number5 == 0) // default message
                        {
                            AddLogItem(new ChatMessage(e.text, new Color((byte)e.number2, (byte)e.number3, (byte)e.number4), e.remoteClient));
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
            ChatLog[LogCounter] = msg;
            LogCounter++;
            if (LogCounter >= ChatLog.Length)
                LogCounter = 0;
        }
        private static void DisplayLog(CAPlayer player, int offset)
        {
            if (player == null)
                return;
            var log = GetLog(player.Index, player.Channel, offset);
            foreach (MenuItem logItem in log)
            {
                player.TSPlayer.SendData(PacketTypes.ChatText, logItem.Text, 255, logItem.Color.R, logItem.Color.G, logItem.Color.B, 1);
            }  
        }
        public static List<MenuItem> GetLog(int playerID = -1, int channelID = 0, int offset = 199)
        {
            List<MenuItem> ReturnList = new List<MenuItem>();
            int count = 0;
            for (int i = 1; i < ChatLog.Length; i++)
            {
                if (count >= offset)
                    break;
                int index = LogCounter - i;
                if (index < 0)
                    index = ChatLog.Length - 1 + index;
                if (ChatLog[index].Text != null && (ChatLog[index].Channel == 0 || ChatLog[index].Channel == channelID) && (ChatLog[index].Recipient == -1 || ChatLog[index].Recipient == playerID))
                {
                    ReturnList.Add(new MenuItem(ChatLog[index].Text, -1, false, false, ChatLog[index].Color));
                    count++;
                }
            }
            ReturnList.Reverse();
            return ReturnList;            

            /*for (int i = offset; i > 0; i--)
            {
                int index = LogCounter - i;
                if (index < 0)
                    index = ChatLog.Length - 1 + index;
                if (ChatLog[index].Text != null && (ChatLog[index].Channel == 0 || ChatLog[index].Channel == channelID) && (ChatLog[index].Recipient == -1 ||ChatLog[index].Recipient == playerID))
                    ReturnList.Add(new MenuItem(ChatLog[index].Text, -1, false, false, ChatLog[index].Color));
            }
            return ReturnList;*/
        }
        public static Menu CreateMenu(int playerID, string title, List<MenuItem> data, MenuAction callback)
        {
            var player = PlayerList[playerID];
            try
            {
                if (player != null && !player.InMenu)
                {
                    player.Menu = new Menu(playerID, title, data, callback);
                    return player.Menu;
                }
            }
            catch (Exception ex) { Log.ConsoleError(ex.ToString()); }
            return null;
        }

    }   

}
