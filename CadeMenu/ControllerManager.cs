using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX.DirectInput;
using System.Windows.Threading;
using System.Windows;

namespace CadeMenu
{
    public class EventArgsString : EventArgs
    {
        public string EventString { get; set; }
        public EventArgsString(string eventString)
        {
            EventString = eventString;
        }
    }

    public class ControllerManager
    {
        public Guid JoystickGuid;

        public event EventHandler<EventArgsString> Navigate;
        public ControllerSettings settings;
        DispatcherTimer timer;
        SimpleJoystick s;
        JoystickState state;
        string JoystickState;
        bool[] LocalButtons;
        string GoingDirection;
        readonly int NavigateBaseSpeed = 300; //delay in milliseconds between up/down navigates, lower is faster
        readonly int NavigateAcceleration = 30;
        readonly int MaxNavigateSpeed = 40;
        int NavigateSpeed;
        public void Init(Guid g = default(Guid))
        {
            JoystickGuid = g;
            s = new SimpleJoystick(g);
            state = s.State;
            LocalButtons = s.State.GetButtons();
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(5) };
            timer.Tick += GetControllerInput;
            timer.Start();
        }

        public void Release()
        {
            timer.Stop();
            s.Release();
        }

        public void GetControllerInput(object sender, EventArgs e)
        {
            if (state != s.State)
            {
                bool[] ChangedButtons = s.State.GetButtons();
                if(LocalButtons != ChangedButtons)
                {
                    for(int i = 0; i < LocalButtons.Length; i++)
                    {
                        if(LocalButtons[i] != ChangedButtons[i])
                        {
                            ButtonChanged(i, ChangedButtons[i]);
                        }
                    }
                }
                string currentJoystickState = "Idle";
                //Console.WriteLine("x = " + s.State.X);
                //Console.WriteLine("y = " + s.State.Y);
                if (s.State.X > settings.Deadzone)
                {
                    currentJoystickState = "Right";
                }
                else if (s.State.X < -settings.Deadzone)
                {
                    currentJoystickState = "Left";
                }
                else if (s.State.Y > settings.Deadzone)
                {
                    if (settings.InvertY)
                    {
                        currentJoystickState = "Down";
                    }
                    else
                    {
                        currentJoystickState = "Up";
                    }
                }
                else if (s.State.Y < -settings.Deadzone)
                {
                    if (settings.InvertY)
                    {
                        currentJoystickState = "Up";
                    }
                    else
                    {
                        currentJoystickState = "Down";
                    }
                }
                if (currentJoystickState != JoystickState)
                {
                    JoystickState = currentJoystickState;
                    JoystickChanged(JoystickState);
                }
                state = s.State;
                LocalButtons = state.GetButtons();
            }
        }

        void ButtonChanged(int button, bool pressed)
        {
            //MessageBox.Show("Button " + button + " pressed for " + JoystickGuid.ToString(), "Error", MessageBoxButton.OK);
            EventHandler<EventArgsString> handler = Navigate;
            if (handler == null || pressed)//only trigger event on button release
            {
                if (handler == null)
                {
                    Console.WriteLine("Handler is null");
                }
                return;
            }
            //If bool pressed is false, button was released
            if (settings.Back.Contains(button))
            {
                handler(this, new EventArgsString("Left"));
                //Console.WriteLine("Back button " + pressed);
            }
            else if (settings.Select.Contains(button))
            {
                handler(this, new EventArgsString("Right"));
                //Console.WriteLine("Select button " + pressed);
            }
            else if (settings.Favourite.Contains(button))
            {
                handler(this, new EventArgsString("Favourite"));
            }
            else
            {
                Console.WriteLine("Unregistered button " + button + " " + pressed);
            }
        }

        async void JoystickChanged(string state, bool repeat = false)
        {
            EventHandler<EventArgsString> handler = Navigate;
            if (handler == null)
            {
                Console.WriteLine("Handler is null");
                return;
            }
            if (!repeat)
            {
                GoingDirection = state;
                if (state != "Idle" && state != "Left" && state != "Right")
                {
                    //Console.WriteLine("go");
                    NavigateSpeed = NavigateBaseSpeed;
                    JoystickChanged(state, true);
                }
            }
            else
            {
                await Task.Delay(NavigateSpeed);
                NavigateSpeed -= NavigateAcceleration;
                if (NavigateSpeed < MaxNavigateSpeed)
                {
                    NavigateSpeed = MaxNavigateSpeed;
                }
                if (GoingDirection == state)
                {
                    JoystickChanged(state, true);
                }
                else
                {
                    return;
                }
            }
            handler(this, new EventArgsString(state));
            //Console.WriteLine("Joystick state is: " + state);
        }
    }

    public class SimpleJoystick
    {
        /// 
        /// Joystick handle
        /// 
        private Joystick Joystick;

        /// 
        /// Get the state of the joystick
        /// 
        public JoystickState State
        {
            get
            {

                if (Joystick.Acquire().IsFailure)
                    throw new Exception("Joystick failure");

                if (Joystick.Poll().IsFailure)
                    throw new Exception("Joystick failure");

                return Joystick.GetCurrentState();
            }
        }

        /// 
        /// Construct, attach the joystick
        /// 
        public SimpleJoystick(Guid instance = default(Guid))
        {
            DirectInput dinput = new DirectInput();

            if (instance == default(Guid))
            {
                // Search for device
                foreach (DeviceInstance device in dinput.GetDevices(DeviceClass.GameController, DeviceEnumerationFlags.AttachedOnly))
                {
                    // Create device
                    try
                    {
                        //Console.WriteLine("Detected controller : " + device.InstanceName);
                        Joystick = new Joystick(dinput, device.InstanceGuid);
                        break;
                    }
                    catch (DirectInputException)
                    {
                        //Console.WriteLine("Not a controller : " + device.ProductName);
                    }
                }
            }
            else
            {
                //No need to search
                try
                {
                    Joystick = new Joystick(dinput, instance);
                }
                catch (DirectInputException)
                {

                }
            }

            if (Joystick == null)
            {
                MessageBox.Show("No joystick found! Using keyboard input instead", "Error", MessageBoxButton.OK);
                throw new Exception("No joystick found");
                //TODO: keyboard input
                //ControllerManager.UsingKeyboard = true;
                //return;
            }

            foreach (DeviceObjectInstance deviceObject in Joystick.GetObjects())
            {
                if ((deviceObject.ObjectType & ObjectDeviceType.Axis) != 0)
                    Joystick.GetObjectPropertiesById((int)deviceObject.ObjectType).SetRange(-100, 100);
            }

            // Acquire sdevice
            Joystick.Acquire();
        }

        /// 
        /// Release joystick
        /// 
        public void Release()
        {
            if (Joystick != null)
            {
                Joystick.Unacquire();
                Joystick.Dispose();
            }

            Joystick = null;
        }
    }
}
