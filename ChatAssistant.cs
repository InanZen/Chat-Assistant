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

namespace ChatAssistant
{
    public class MenuEventArgs : HandledEventArgs
    {
        public MenuEventArgs(List<MenuItem> contents, int selection)
        {
            this.Data = contents;
            this.Selected = selection;
        }
        public int Selected;
        public List<MenuItem> Data;
    }
    public struct MenuItem
    {
        public String text;
        public int value;
        public MenuItem(String text, int val = -1)
        {
            this.text = text;
            this.value = val;
        }
    }
    struct ChatMessage
    {
        public String text;
        public Color color;
        public ChatMessage(String txt, Color c)
        {
            this.color = c;
            this.text = txt;
        }
        public ChatMessage(String txt, int r, int g, int b)
        {
            this.color = new Color(r, g, b);
            this.text = txt;
        }
    }
    [APIVersion(1, 12)]
    public class Chat : TerrariaPlugin
    {

        private class Menu
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
            public Menu(int playerid, String title, List<String> text, List<int> values = null, MenuAction del = null)
            {                
                this.contents = new List<MenuItem>();
                for (int i = 0; i < text.Count; i++)
                {
                    if (values != null && text.Count == values.Count)
                        this.contents.Add(new MenuItem(text[i], values[i]));
                    else
                        this.contents.Add(new MenuItem(text[i]));
                }
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
                            player.SendData(PacketTypes.ChatText, String.Format("> {0} <", this.contents[this.index].text), 255, Color.LightGray.R, Color.LightGray.G, Color.LightGray.B, 1);
                        else if (this.index + i < 0 || this.index + i >= this.contents.Count)
                            player.SendData(PacketTypes.ChatText, "", 255, 0f, 0f, 0f, 1);
                        else
                            player.SendData(PacketTypes.ChatText, String.Format("{0}", this.contents[this.index + i].text), 255, 255, 255, 255, 1);
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
                    MenuEventArgs args = new MenuEventArgs(this.contents, value);
                    if (this.MenuActionHandler != null)
                        this.MenuActionHandler(this, args);
                    if (!args.Handled)
                    {
                        player.InMenu = false;
                        player.Menu = null;
                        DisplayLog(player.TSPlayer, 7);
                    }
                }
            }
            public void Select()
            {
                this.Close(this.index);
            }
        }
        private class CAPlayer
        {
            public int Index = -1;
            public TSPlayer TSPlayer;
            public bool InMenu = false;
            public Menu Menu;
            public CAPlayer(int id = -1)
            {
                this.Index = id;
                if (id != -1)
                {
                    this.TSPlayer = TShock.Players[id];
                }
            }

        }
        private static CAPlayer[] PlayerList = new CAPlayer[256];
        private static ChatMessage[] ChatLog = new ChatMessage[50];
        private static int LogCounter = 0;
        public delegate void MenuAction(Object sender, MenuEventArgs args);

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
            get { return new Version("0.1"); }
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
        void OnChat(messageBuffer buf, int who, string text, HandledEventArgs args)
        {
            var player = PlayerList[who];
            if (player != null)
            {
                if (player.InMenu)
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
                                return;
                            }
                            if (up)
                                player.Menu.MoveUp();
                            if (down)
                                player.Menu.MoveDown();
                            if (space)
                                player.Menu.Select();
                        }
                        else
                        {
                            if (up && down)
                            {                                
                                player.Menu = new Menu(player.Index, String.Format("Log [{0}]",DateTime.Now.ToString()), GetLog());
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
                    //Console.WriteLine("1: {0}, 5: {1}, remote: {2}, ignore: {3}", e.number, e.number5, e.remoteClient, e.ignoreClient);
                    if (e.remoteClient != -1)
                    {
                        var player = PlayerList[e.remoteClient];
                        if (player != null && player.InMenu && e.number5 == 0)
                            e.Handled = true;
                    }
                    else
                    {
                        foreach (TSPlayer tsply in TShock.Players)
                        {
                            if (tsply != null && tsply.Index >= 0)
                            {
                                var player = PlayerList[tsply.Index];
                                if (player != null && !player.InMenu)
                                {
                                    player.TSPlayer.SendMessage(e.text, (byte)e.number2, (byte)e.number3, (byte)e.number4);
                                }
                            }
                        }
                        ChatLog[LogCounter] = new ChatMessage(e.text, (int)e.number2, (int)e.number3, (int)e.number4);
                        LogCounter++;
                        if (LogCounter > 49)
                            LogCounter = 0;
                        e.Handled = true;
                    }
                                        
                }
            }
            catch (Exception ex) { Log.ConsoleError(ex.ToString()); }
        }
        public static void DisplayLog(TSPlayer player, int offset)
        {
            for (int i = offset; i > 0; i--)
            {
                if (i < offset - 6)
                    break;
                int index = LogCounter - i;
                if (index < 0)                
                    index = ChatLog.Length - 1 + index; 
                if (ChatLog[index].text != null)
                    player.SendMessage(ChatLog[index].text, ChatLog[index].color);
            }
        }
        public static List<String> GetLog(int offset = 49)
        {
            List<String> ReturnList = new List<string>();
            for (int i = offset; i > 0; i--)
            {
                int index = LogCounter - i;
                if (index < 0)
                    index = ChatLog.Length - 1 + index;
                if (ChatLog[index].text != null)
                    ReturnList.Add(ChatLog[index].text);
            }
            return ReturnList;
        }
        public static bool CreateMenu(int PlayerID, string Title, List<String> Text, List<int> Values, MenuAction Callback)
        {
            var player = PlayerList[PlayerID];
            try
            {
                if (player != null && !player.InMenu)
                {
                    player.Menu = new Menu(PlayerID, Title, Text, Values, Callback);
                    return true;
                }
            }
            catch (Exception ex) { Log.ConsoleError(ex.ToString()); }
            return false;
        }

    }   

}
