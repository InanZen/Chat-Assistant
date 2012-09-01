using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChatAssistant
{
    public enum MsgType
    {
        Global = 0,
        Channel = 1,
        Private = 2,
        Death = 3,
        Join = 4,
        Quit = 5
    }
    public class ChatMessage
    {
        public String Text;
        public Color Color;
        public MsgType Type;
        public int Sender;
        public DateTime Timestamp;
        public ChatMessage(String txt, Color c, MsgType type = MsgType.Global, int sender = -1)
        {
            this.Color = c;
            this.Text = txt;
            this.Type = type;
            this.Sender = sender;
            this.Timestamp = DateTime.Now;
        }
        /*public ChatMessage(String txt, int r, int g, int b) : this(txt, new Color(r, g, b)) { }   */
    }
}
