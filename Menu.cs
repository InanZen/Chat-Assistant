using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using TShockAPI;
namespace ChatAssistant
{
    public enum MenuStatus
    {        
        ForceExit = -1,
        Exit = 0,
        Select = 1,
        Input = 2
    }
    public class MenuEventArgs : HandledEventArgs
    {
        public MenuEventArgs(List<MenuItem> contents, int playerID) : this(contents, playerID, -1, MenuStatus.Exit) { }
        public MenuEventArgs(List<MenuItem> contents, int playerID, int selection) : this(contents, playerID, selection, MenuStatus.Select) { }
        public MenuEventArgs(List<MenuItem> contents, int playerID, int selection, MenuStatus status)
        {
            this.Data = contents;
            this.Selected = selection;
            this.PlayerID = playerID;
            this.Status = status;
        }
        public int Selected;
        public MenuStatus Status;
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
    public class Menu
    {
        public int PlayerID;
        public string title;
        public List<MenuItem> contents;
        public int index = 0;
        public bool header = true;
        public CAMain.MenuAction MenuActionHandler;

        public Menu(int playerid, String title, List<MenuItem> contents, CAMain.MenuAction del = null)
        {
            this.contents = contents;
            this.PlayerID = playerid;
            this.title = title;
            if (del != null)
                this.MenuActionHandler = del;
            if (CAMain.PlayerList[playerid] != null)
                CAMain.PlayerList[playerid].InMenu = true;
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
                            player.SendData(PacketTypes.ChatText, (this.contents[this.index + i].Text.Contains("@0")) ? this.contents[this.index + i].Text.Replace("@0", String.Format(">{0}<", this.contents[this.index + i].Input)) : String.Format("{0} >{1}<", this.contents[this.index].Text, this.contents[this.index].Input), 255, this.contents[this.index].Color.R, this.contents[this.index].Color.G, this.contents[this.index].Color.B, 1);
                        else if (this.contents[this.index].Selectable)
                            player.SendData(PacketTypes.ChatText, String.Format("> {0} <", this.contents[this.index + i].Text.Replace("@0", this.contents[this.index + i].Input)), 255, this.contents[this.index].Color.R, this.contents[this.index].Color.G, this.contents[this.index].Color.B, 1);
                        else
                            player.SendData(PacketTypes.ChatText, this.contents[this.index + i].Text.Replace("@0", this.contents[this.index + i].Input), 255, this.contents[this.index].Color.R, this.contents[this.index].Color.G, this.contents[this.index].Color.B, 1);
                    }
                    else if (this.index + i < 0 || this.index + i >= this.contents.Count)
                        player.SendData(PacketTypes.ChatText, "", 255, 0f, 0f, 0f, 1);
                    else
                        player.SendData(PacketTypes.ChatText, this.contents[this.index + i].Text.Replace("@0", this.contents[this.index + i].Input), 255, this.contents[this.index + i].Color.R, this.contents[this.index + i].Color.G, this.contents[this.index + i].Color.B, 1);

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
        public void Close(bool force =  false)
        {
            try
            {
                var player = CAMain.PlayerList[this.PlayerID];
                if (player != null)
                {
                    MenuEventArgs args = new MenuEventArgs(this.contents, this.PlayerID, -1, (force)?MenuStatus.ForceExit:MenuStatus.Exit);
                    if (this.MenuActionHandler != null)
                        this.MenuActionHandler(this, args);
                    if (force || !args.Handled)
                    {
                        player.InMenu = false;
                        player.Menu = null;
                        if (!player.quitting)
                            CAMain.DisplayLog(player, 7);
                    }
                }
            }
            catch (Exception ex) { Log.ConsoleError(ex.ToString()); }
        }
        public void Select()
        {
            if (this.contents[this.index].Selectable)
            {
                MenuEventArgs args = new MenuEventArgs(this.contents, this.PlayerID, this.index, MenuStatus.Select);
                if (this.MenuActionHandler != null)
                    this.MenuActionHandler(this, args);
            }
        }
        public void OnInput(String text)
        {
            var player = CAMain.PlayerList[this.PlayerID];
            if (player != null)
            {
                string oldinput = this.contents[this.index].Input;
                this.contents[this.index].Input = text;
                MenuEventArgs args = new MenuEventArgs(this.contents, this.PlayerID, this.index, MenuStatus.Input);
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

        // --------------------------- STATIC METHODS -------------------------
        public static Menu CreateMenu(int playerID, string title, List<MenuItem> data, CAMain.MenuAction callback)
        {
            var player = CAMain.PlayerList[playerID];
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
        public static void DisplayMainMenu(CAPlayer player)
        {
            if (player.InMenu)
                player.Menu.Close(true);
            player.Menu = new Menu(player.Index, "Chat Assistant menu", MainMenuBuilder.GetContentsMain(), new CAMain.MenuAction(MainMenuBuilder.MainMenuCallback));
        }
    }
    internal static class MainMenuBuilder
    {
        internal static void MainMenuCallback(Object m, MenuEventArgs args)
        {
            // var player = CAMain.PlayerList[args.PlayerID];
            var menu = (Menu)m;
            if (args.Status == MenuStatus.Select)
            {
                var selected = args.Data[args.Selected];
                menu.index = 0;
                switch (selected.Value)
                {
                    case -1:
                        {
                            menu.Close(true);
                            return;
                        }
                    case 1: // Main menu
                        {
                            menu.title = "Chat Assistant menu";
                            menu.contents = GetContentsMain();
                            break;
                        }
                    case 2: // Channel list
                        {
                            menu.title = "Channel list";
                            menu.contents = GetContentsChannelList((TShock.Players[menu.PlayerID].Group.HasPermission("CA.channel.bypass.hidden")) ? true : false);
                            break;
                        }
                    case 3: // channel chat log
                        {
                            var player = CAMain.PlayerList[menu.PlayerID];
                            menu.contents = GetContentsLog(player);
                            menu.title = String.Format("Log [{0}]", DateTime.Now.ToString());
                            menu.index = menu.contents.Count - 1;                            
                            break;
                        }
                    case 4: // Personal settings
                        {
                            var player = CAMain.PlayerList[menu.PlayerID];
                            menu.contents = GetContentsPersonalSettings(player);
                            menu.title = "Personal settings";
                            break;
                        }
                    case 41: // toggle display join/quit msgs
                        {
                            var player = CAMain.PlayerList[menu.PlayerID];
                            player.Flags ^= PlayerSettings.HideJoinQuitMsg;
                            menu.contents = GetContentsPersonalSettings(player);
                            break;
                        }
                    case 42: // toggle display death msgs
                        {
                            var player = CAMain.PlayerList[menu.PlayerID];
                            player.Flags ^= PlayerSettings.HideDeathMsg;
                            menu.contents = GetContentsPersonalSettings(player);
                            menu.index = 1;
                            break;
                        }
                    case 43: // display ignore list
                        {
                            var player = CAMain.PlayerList[menu.PlayerID];
                            menu.contents = GetContentsIgnoreList(player);
                            menu.title = "My ignore list";
                            break;
                        }
                    default:
                        return;
                }
                menu.DisplayMenu();
            }
        }
        internal static List<MenuItem> GetContentsMain()
        {
            List<MenuItem> returnList = new List<MenuItem>();
            returnList.Add(new MenuItem("[ Channel list ]", 2, Color.AntiqueWhite));
            returnList.Add(new MenuItem("[ Chat log ]", 3, Color.AntiqueWhite));
            returnList.Add(new MenuItem("[ Personal settings ]", 4, Color.AntiqueWhite));
            returnList.Add(new MenuItem("[ Exit ]", -1, Color.LightGray));
            return returnList;
        }
        internal static List<MenuItem> GetContentsChannelList(bool hidden = false)
        {
            List<MenuItem> returnList = new List<MenuItem>();
            List<Channel> chList;
            if (hidden)
                chList = Channel.GetAll();
            else
                chList = Channel.GetNonHidden();
            foreach (Channel ch in chList)
            {
                returnList.Add(new MenuItem(String.Format("{0} [{1} in channel] [{2}]", ch.Name, ch.Users.Count, ch.Flags.ToString()), 0, false, Color.AntiqueWhite));
            }
            returnList.Add(new MenuItem("[ Back ]", 1, Color.LightGray));
            return returnList;
        }
        internal static List<MenuItem> GetContentsLog(CAPlayer player)
        {
            var log = player.GetCombinedLog(50);
            List<MenuItem> logMenu = new List<MenuItem>();
            foreach (ChatMessage msg in log)
            {
                logMenu.Add(new MenuItem(msg.Text, 0, false, false, msg.Color));
            }
            return logMenu;
        }
        internal static List<MenuItem> GetContentsPersonalSettings(CAPlayer player)
        {
            List<MenuItem> returnList = new List<MenuItem>();
            if (CAMain.config.EnableJoinQuitFilter)
                returnList.Add(new MenuItem(String.Format("Display join/quit messages [{0}]", (player.Flags.HasFlag(PlayerSettings.HideJoinQuitMsg)) ? "OFF" : "ON"), 41, Color.AntiqueWhite));
            else
                returnList.Add(new MenuItem(String.Format("Display join/quit messages [{0}] <option disabled>", (player.Flags.HasFlag(PlayerSettings.HideJoinQuitMsg)) ? "OFF" : "ON"), 0, false, Color.AntiqueWhite));
            if (CAMain.config.EnableDeathMsgFilter)
                returnList.Add(new MenuItem(String.Format("Display death messages [{0}]", (player.Flags.HasFlag(PlayerSettings.HideDeathMsg)) ? "OFF" : "ON"), 42, Color.AntiqueWhite));
            else
                returnList.Add(new MenuItem(String.Format("Display death messages [{0}] <option disabled>", (player.Flags.HasFlag(PlayerSettings.HideDeathMsg)) ? "OFF" : "ON"), 0, false, Color.AntiqueWhite));
            if (player.Ignores.Count == 0)
                returnList.Add(new MenuItem("Ignore list [empty]", 0, false, Color.AntiqueWhite));
            else
                returnList.Add(new MenuItem(String.Format("Ignore list [{0}]", player.Ignores.Count), 43, Color.AntiqueWhite));
            returnList.Add(new MenuItem("[ Back ]", 1, Color.LightGray));
            return returnList;
        }
        internal static List<MenuItem> GetContentsIgnoreList(CAPlayer player)
        {
            List<MenuItem> returnList = new List<MenuItem>();
            foreach (int i in player.Ignores)
            {
                returnList.Add(new MenuItem(CAMain.PlayerList[i].TSPlayer.Name, 0, false, Color.AntiqueWhite));
            }
            returnList.Add(new MenuItem("[ Back ]", 4, Color.LightGray));
            return returnList;
        }

    }
}
