using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace CadeMenu
{
    [Serializable]
    public class ControllerSettings
    {
        public string Name { get; set; }
        public Guid ID { get; set; }
        public int[] Select { get; set; }
        public int[] Back { get; set; }
        public int[] Favourite { get; set; }
        public bool InvertY { get; set; }
        public int Deadzone { get; set; }
    }

    [Serializable]
    public class Settings
    {
        internal string consoleListLocation;
        public string ConsoleListLocation
        {
            get { return consoleListLocation; }
            set { consoleListLocation = value; }
        }
        internal string gameListLocation;
        public string GameListLocation
        {
            get { return gameListLocation; }
            set { gameListLocation = value; }
        }
        public List<ControllerSettings> Controller { get; set; }
        public bool ShowOnlyGamelistGames { get; set; }
    }

    public class SettingsHandler
    {
        public static Settings Read()
        {
            XmlSerializer s = new XmlSerializer(typeof(Settings));
            FileStream fs = new FileStream("settings.xml", FileMode.Open);
            Settings settings = (Settings)s.Deserialize(fs);
            fs.Flush();
            fs.Dispose();
            return settings;
        }

        public static void Write(Settings settings)
        {
            XmlSerializer s = new XmlSerializer(typeof(Settings));
            StreamWriter sw = new StreamWriter("settings.xml");
            s.Serialize(sw, settings);
            sw.Close();
        }
    }
}
