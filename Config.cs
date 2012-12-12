using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;

namespace ChatAssistant
{
    class CAconfig
    {
        public bool EnableJoinQuitFilter = true;
        public bool FilterJoinQuitByDefault = false;
        public bool EnableDeathMsgFilter = true;
        public bool FilterDeathMsgByDefault = false;
        public bool UsingVault = false;
        public int ChanelCreatingCost = 10000;

   /*     [JsonConstructor]
        public CAconfig(bool joinquitfilter, bool deathmsgfilter, bool joinquitdefault, bool deathmsgdefault)
        {
            this.EnableJoinQuitFilter = joinquitfilter;
            this.EnableDeathMsgFilter = deathmsgfilter;
            this.FilterDeathMsgByDefault = deathmsgdefault;
            this.FilterJoinQuitByDefault = joinquitdefault;
        }*/
        // -------------------------------  Static save/load methods -----------------------
        public static CAconfig Load()
        {
            var savepath = Path.Combine(CAMain.Savepath, "ChatAssistant.conf");
            CAconfig returnconf = null;
            try
            {
                if (!File.Exists(savepath))
                {
                    returnconf = new CAconfig();
                    CAconfig.Save(returnconf);
                }
                else
                {
                    using (var stream = new FileStream(savepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            returnconf = JsonConvert.DeserializeObject<CAconfig>(sr.ReadToEnd());
                        }
                        stream.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                TShockAPI.Log.ConsoleError(ex.ToString());
                return new CAconfig();
            }
            if (returnconf == null)
                returnconf = new CAconfig();
            return returnconf;
        }
        private static void Save(CAconfig conf)
        {
            try
            {
                using (var stream = new FileStream(Path.Combine(CAMain.Savepath, "ChatAssistant.conf"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
                {
                    using (var sr = new StreamWriter(stream))
                    {
                        sr.Write(JsonConvert.SerializeObject(conf, Formatting.Indented));
                    }
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                TShockAPI.Log.ConsoleError(ex.ToString());
            }
        }
    }
}
