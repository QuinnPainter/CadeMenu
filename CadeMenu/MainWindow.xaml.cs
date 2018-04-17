using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Threading;
using System.Diagnostics;
using SlimDX.DirectInput;
using System.Net;
using System.Collections.Specialized;
using System.Threading;

namespace CadeMenu
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        Settings settings = new Settings();
        List<console> consoles;
        List<game> currentGames;
        List<game> favourites;
        double WindowScrollSpeed = 50;
        int ScrollTarget = 0;
        int MaxScroll = 2;
        int SavedSelection = 0;
        List<int> AlphabetIndices = new List<int>();
        List<Joystick> connectedControllers = new List<Joystick>();
        List<ControllerManager> connectedControllerManagers = new List<ControllerManager>();
        //SolidColorBrush UnselectedColour = new SolidColorBrush((Color) ColorConverter.ConvertFromString("#FF252525"));
        public MainWindow()
        {
            InitializeComponent();
            settings = SettingsHandler.Read();
            consoles = ListLoader.GenerateConsoleList(settings.ConsoleListLocation);
            //double totalHeight = 0;
            foreach(console c in consoles)
            {
                TextBlock t = new TextBlock()
                {
                    Text = c.name,
                    Height = 50,
                    FontSize = 36
                };
                //totalHeight += t.Height;
                consoleList.Items.Add(t);
            }
            //consoleList.Height = totalHeight;
            connectedControllers = ListControllers();
            foreach(Joystick c in connectedControllers)
            {
                ControllerSettings cs = new ControllerSettings();
                bool settingsExist = false;
                foreach (ControllerSettings settings in settings.Controller)
                {
                    if (c.Information.InstanceGuid == settings.ID)
                    {
                        cs = settings;
                        settingsExist = true;
                    }
                }
                if (settingsExist == false)
                {
                    settings.Controller.Add(new ControllerSettings { Name = c.Information.ProductName, ID = c.Information.InstanceGuid });
                    SettingsHandler.Write(settings);
                }
                else
                {
                    //MessageBox.Show("Joystick connected : " + c.Information.ProductName + " " + c.Information.ProductGuid, "things", MessageBoxButton.OK);
                    ControllerManager con = new ControllerManager();
                    connectedControllerManagers.Add(con);
                    con.settings = cs;
                    con.Navigate += ControlNavigate;
                    con.Init(c.Information.InstanceGuid);
                }
            }
            consoleList.SelectedIndex = 0;
            alphabetList.SelectedIndex = 0;
            gameList.SelectedIndex = 0;
            LoadDetails();

            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2) };
            timer.Tick += Scroll;
            timer.Start();
            GenerateAlphabet();
        }

        public List<Joystick> ListControllers()
        {
            List<Joystick> sticks = new List<Joystick>();
            DirectInput dinput = new DirectInput();
            // Search for device
            foreach (DeviceInstance device in dinput.GetDevices(DeviceClass.GameController, DeviceEnumerationFlags.AttachedOnly))
            {
                try
                {
                    Console.WriteLine("Detected controller : " + device.InstanceName);
                    sticks.Add(new Joystick(dinput, device.InstanceGuid));
                }
                catch (DirectInputException)
                {
                    //Console.WriteLine("Not a controller : " + device.ProductName);
                }
            }
            return sticks;
        }

        void LoadDetails()
        {
            string url = consoles[consoleList.SelectedIndex].logo;
            Uri imagesource = new Uri(url, UriKind.Absolute);
            try
            {
                BitmapImage bi = new BitmapImage(imagesource);
                logo.Source = bi;
                logo.Width = bi.PixelWidth;
            }
            catch (FileNotFoundException)
            {
                logo.Source = null;
            }
            url = consoles[consoleList.SelectedIndex].image;
            imagesource = new Uri(url, UriKind.Absolute);
            try
            {
                BitmapImage bi = new BitmapImage(imagesource);
                image.Source = bi;
                image.Width = bi.PixelWidth;
            }
            catch (FileNotFoundException)
            {
                image.Source = null;
            }
        }

        public void ControlNavigate(object sender, EventArgsString args)
        {
            //(consoleList.Items[0] as TextBlock).Text = args.EventString;
            Console.WriteLine("Navigate " + args.EventString);
            if (args.EventString == "Up")
            {
                NavigateUpDown(true);
            }
            else if (args.EventString == "Down")
            {
                NavigateUpDown(false);
            }
            else if (args.EventString == "Left")
            {
                NavigateLeftRight(false);
            }
            else if (args.EventString == "Right")
            {
                NavigateLeftRight(true);
            }
            else if (args.EventString == "Favourite")
            {
                FavouriteSelectedGame();
            }
            //consoleList.ScrollIntoView(consoleList.SelectedItem);
            LoadDetails();
        }

        void GenerateAlphabet()
        {
            string alpha = "*abcdefghijklmnopqrstuvwxyz";
            foreach(char c in alpha)
            {
                TextBlock t = new TextBlock()
                {
                    Text = c.ToString(),
                    Height = 50,
                    FontSize = 36,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                alphabetList.Items.Add(t);
            }
        }

        void NavigateUpDown(bool direction)
        {
            //false goes down, true goes up
            if (direction)
            {
                switch(ScrollTarget)
                {
                    case 0:
                        if (consoleList.SelectedIndex > 0)
                        {
                            consoleList.SelectedIndex--;
                        }
                        break;
                    case 1:
                        if (alphabetList.SelectedIndex > 0)
                        {
                            alphabetList.SelectedIndex--;
                        }
                        break;
                    case 2:
                        if (gameList.SelectedIndex > 0)
                        {
                            gameList.SelectedIndex--;
                        }
                        break;
                }
            }
            else
            {
                switch (ScrollTarget)
                {
                    case 0:
                        consoleList.SelectedIndex++;
                        break;
                    case 1:
                        alphabetList.SelectedIndex++;
                        break;
                    case 2:
                        gameList.SelectedIndex++;
                        break;
                }
            }
            if (ScrollTarget == 1)
            {
                SavedSelection = AlphabetIndices[alphabetList.SelectedIndex];
                if (gameList.Items.Count > 0)
                {
                    gameList.ScrollToCenterOfView(gameList.Items[SavedSelection]);
                }
            }
            consoleList.ScrollToCenterOfView(consoleList.SelectedItem);
            alphabetList.ScrollToCenterOfView(alphabetList.SelectedItem);
            gameList.ScrollToCenterOfView(gameList.SelectedItem);
        }

        void NavigateLeftRight(bool direction)
        {
            //Console.WriteLine(gameList.SelectedIndex);
            //false goes left, true goes right
            if (ScrollTarget == 1 && direction)
            {
                gameList.SelectedIndex = SavedSelection;
                SavedSelection = alphabetList.SelectedIndex;
                alphabetList.SelectedIndex = -1;
            }
            /*
            else if (ScrollTarget == 1 && !direction)
            {
                alphabetList.SelectedIndex = 0;
                gameList.SelectedIndex = 0;
                SavedSelection = 0;
            }
            */
            //Load Games list if that is the current location
            if (ScrollTarget == 0 && direction)
            {
                LoadGamesList();
                AlphabetIndices = ListLoader.GenerateAlphabetIndices(currentGames, favourites.Count);
                alphabetList.SelectedIndex = 0;
                gameList.SelectedIndex = 0;
                SavedSelection = 0;
            }
            if (direction)
            {
                if (ScrollTarget < MaxScroll)
                {
                    ScrollTarget++;
                }
                else
                {
                    foreach (ControllerManager c in connectedControllerManagers) { c.Release(); }
                    string command = "/c " + consoles[consoleList.SelectedIndex].command;
                    //command.Replace("%ROMNAME%", System.IO.Path.GetFileNameWithoutExtension(currentGames[gameList.SelectedIndex].path));
                    //command.Replace("%ROMPATH%", @"""" + currentGames[gameList.SelectedIndex].path + @"""");
                    SendSignDataThreaded(System.IO.Path.GetFileNameWithoutExtension(currentGames[gameList.SelectedIndex].path));
                    command = @"/c """"" + consoles[consoleList.SelectedIndex].command + @""" """ + currentGames[gameList.SelectedIndex].path + @""" """ + System.IO.Path.GetFileNameWithoutExtension(currentGames[gameList.SelectedIndex].path) + @"""""";
                    //In batch file %1 is game path, %2 is game name
                    Console.WriteLine(command);
                    var processInfo = new ProcessStartInfo("cmd.exe", command);
                    var process = Process.Start(processInfo);
                    process.WaitForExit();
                    SendSignDataThreaded("clear");
                    string contIds = "";
                    foreach (ControllerManager c in connectedControllerManagers)
                    {
                        contIds += (" " + c.JoystickGuid);
                        c.Init(c.JoystickGuid);
                    }
                    //MessageBox.Show("Joystick IDs: " + contIds, "stuff", MessageBoxButton.OK);
                }
            }
            else
            {
                if (ScrollTarget > 0)
                {
                    ScrollTarget--;
                }
            }
            if (ScrollTarget == 1)
            {
                alphabetList.SelectedIndex = SavedSelection;
                SavedSelection = gameList.SelectedIndex;
                gameList.SelectedIndex = -1;
            }
        }

        void SendSignDataThreaded(string toSend)
        {
            Thread t = new Thread(() => SendSignData(toSend));
            t.Start();
        }

        void SendSignData(string toSend)
        {
            try
            {
                using (var wb = new WebClient())
                {
                    var data = new NameValueCollection();
                    data["name"] = toSend;
                    /*var response = */wb.UploadValues("http://192.168.1.16:5000", "POST", data);
                    //string responseInString = Encoding.UTF8.GetString(response);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Sign send failed: " + e.Message);
            }
        }

        void FavouriteSelectedGame()
        {
            if (favourites.Contains(currentGames[gameList.SelectedIndex]))
            {
                favourites.Remove(currentGames[gameList.SelectedIndex]);
            }
            else
            {
                favourites.Add(currentGames[gameList.SelectedIndex]);
            }
            ListLoader.WriteFavourites(favourites, consoles[consoleList.SelectedIndex].favourites);
            LoadGamesList();
        }

        void LoadGamesList()
        {
            gameList.Items.Clear();
            currentGames = ListLoader.GenerateGameList(System.IO.Path.Combine(settings.GameListLocation, consoles[consoleList.SelectedIndex].name), settings.ShowOnlyGamelistGames);
            favourites = ListLoader.ReadFavourites(consoles[consoleList.SelectedIndex].favourites, currentGames);
            foreach (game c in favourites)
            {
                currentGames.Insert(0, c);
                //TextBlock t = new TextBlock()
                //{
                //    Text = c.name,
                //    Height = 50,
                //    FontSize = 36
                //};
                //totalHeight += t.Height;
                //gameList.Items.Add(t);
            }
            foreach (game c in currentGames)
            {
                TextBlock t = new TextBlock()
                {
                    Text = c.name,
                    Height = 50,
                    FontSize = 36
                };
                //totalHeight += t.Height;
                gameList.Items.Add(t);
            }
        }

        void Scroll(object sender, EventArgs e)
        {
            //Console.WriteLine(FullWindow.Width + " " + GameMenu.Width);
            if (ScrollTarget == 0)
            {
                if (WindowScroll.HorizontalOffset > 0)
                {
                    WindowScroll.ScrollToHorizontalOffset(WindowScroll.HorizontalOffset - WindowScrollSpeed);
                }
            }
            else if (ScrollTarget == 2 || ScrollTarget == 1)
            {
                if (WindowScroll.HorizontalOffset < 1672)
                {
                    WindowScroll.ScrollToHorizontalOffset(WindowScroll.HorizontalOffset + WindowScrollSpeed);
                }
            }
        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
