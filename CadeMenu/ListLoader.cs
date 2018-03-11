using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using System.Xml;

namespace CadeMenu
{
    public class game
    {
        public string path { get; set; }
        public string name { get; set; }
        public string image { get; set; }
        public string bannerImage { get; set; }
        public string releasedate { get; set; }
        public string publisher { get; set; }
    }

    public class console
    {
        public string name;
        public string logo;
        public string image;
        public string command;
        public string favourites;
    }

    public static class ListLoader
    {
        public static List<console> GenerateConsoleList(string dir)
        {
            List<console> consoles = new List<console>();
            List<string> list = new List<string>();
            list = Directory.GetDirectories(dir, "*", SearchOption.TopDirectoryOnly).ToList();
            foreach(string s in list)
            {
                string consoleimagetype = "console.jpg";
                if (File.Exists(Path.Combine(s, "console.png")))
                {
                    consoleimagetype = "console.png";
                }
                consoles.Add(new console {
                    name = Path.GetFileName(s),
                    logo = Path.Combine(s, "logo.png"),
                    image = Path.Combine(s, consoleimagetype),
                    command = Path.Combine(s, "command.bat"),
                    favourites = Path.Combine(s, "favourites.txt")
                });
            }
            return consoles;
        }

        public static List<game> GenerateGameList(string dir, bool OnlyGamelistGames)
        {
            List<game> games = new List<game>();
            List<string> list = new List<string>();
            try
            {
                list = Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly).ToList();
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine("No games found in " + dir);
                return new List<game>();
            }
            bool gamelist = false;
            string gamelistdir = Path.Combine(dir, "gamelist.xml");
            List<game> xmlGamelist = new List<game>();
            if (list.Contains(gamelistdir))
            {
                gamelist = true;
                xmlGamelist = ParseGamelistXml(gamelistdir, dir);
                list.Remove(gamelistdir);
            }
            foreach (string s in list)
            {
                game xmlData = xmlGamelist.Find(g => g.path == s);
                if (gamelist == true && xmlData != null)
                {
                    games.Add(xmlData);
                }
                else if (gamelist == true && OnlyGamelistGames == true)
                {
                    continue; //Don't add the game
                }
                else
                {
                    games.Add(new game {
                        path = s/*"./" + Path.GetFileName(s)*/,
                        name = Path.GetFileNameWithoutExtension(s),
                        image = null,
                        bannerImage = null,
                        releasedate = null,
                        publisher = null
                    });
                }
            }
            games.Sort((x, y) => string.Compare(x.name, y.name));
            return games;
        }

        public static List<game> ParseGamelistXml(string xmlPath, string gamePath)
        {
            List<game> xmlGames = new List<game>();
            try
            {
                //XDocument doc = XDocument.Load(path);
                //var d = doc.Descendants();
                XmlDocument doc = new XmlDocument();
                doc.Load(xmlPath);
                XmlNodeList nodeList = doc.DocumentElement.SelectNodes("/gameList/game");
                foreach (XmlNode node in nodeList)
                {
                    xmlGames.Add(new game
                    {
                        name = node.SelectSingleNode("name").InnerText,
                        publisher = node.SelectSingleNode("publisher").InnerText,
                        releasedate = node.SelectSingleNode("releasedate").InnerText,
                        image = Path.Combine(gamePath, "images", Path.GetFileName(node.SelectSingleNode("image").InnerText)),
                        bannerImage = Path.Combine(gamePath, "banners", Path.GetFileName(node.SelectSingleNode("image").InnerText)),
                        path = Path.Combine(gamePath, Path.GetFileName(node.SelectSingleNode("path").InnerText))
                    });
                    //Console.WriteLine(node.SelectSingleNode("name").InnerText);
                }
            }
            catch (FileNotFoundException ex)
            {
                throw new Exception("NoGamelist", ex);
            }
            return xmlGames;
        }

        public static List<int> GenerateAlphabetIndices(List<game> games, int NumberOfFavourites)
        {
            string alphabet = "abcdefghijklmnopqrstuvwxyz";
            List<int> indices = new List<int>();
            indices.Add(0);//add first for favourite
            foreach(char c in alphabet)
            {
                int i = games.FindIndex(NumberOfFavourites - 1, s => s.name.ToLowerInvariant().StartsWith(c.ToString()));
                if (i == -1)
                {
                    indices.Add(indices.Last());
                }
                else
                {
                    indices.Add(i);
                }
            }
            //for (int i = 0; i < games.Count; i++)
            //{
            //    if (alphabet == "")
            //    {
            //        return indices;
            //    }
            //    if (games[i].name.StartsWith(alphabet[0].ToString()))
            //    {
            //        indices.Add(i);
            //        alphabet.Remove(0, 1);
            //        continue;
            //    }
            //}
            return indices;
        }

        public static List<game> ReadFavourites(string fileLocation, List<game> games)
        {
            List<string> favs = new List<string>();
            List<game> favGames = new List<game>();
            StreamReader file = new StreamReader(fileLocation);
            string line;
            while ((line = file.ReadLine()) != null)
            {
                favs.Add(line);
            }
            file.Close();
            foreach(game g in games)
            {
                if (favs.Contains(Path.GetFileName(g.path)))
                {
                    favGames.Add(g);
                }
            }
            return favGames;
        }

        public static void WriteFavourites(List<game> favs, string fileLocation)
        {
            StreamWriter file = new StreamWriter(fileLocation);
            foreach (game g in favs)
            {
                string s = Path.GetFileName(g.path);
                file.WriteLine(s);
            }
            file.Close();
        }
    }
}
