using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace InternetRadioDevice
{
    public enum InputAction
    {
        NextChannel,
        PreviousChannel,
        VolumeUp,
        VolumeDown,
        Sleep,
        AddChannel,
        DeleteChannel
    }

    public class InputRecievedEventArgs
    {
        public InputAction Action;
        public string Message;
    }

    // A delegate type for hooking up change notifications.
    public delegate void InputRecievedEventHandler(object sender, InputRecievedEventArgs e);


        public class InputManager
    {
        public event InputRecievedEventHandler InputRecieved;

        private Dictionary<InputAction, GpioPin> actionButtons;

        private StreamSocketListener listener = new StreamSocketListener();
        private List<StreamSocket> connections = new List<StreamSocket>();

        public async Task Initialize()
        {
            actionButtons = new Dictionary<InputAction, GpioPin>();
            var gpio = GpioController.GetDefault();

            foreach(var pinSetting in Config.Buttons.ButtonPins)
            {
                GpioPin button;
                GpioOpenStatus status;

                if (gpio.TryOpenPin(pinSetting.Key, GpioSharingMode.Exclusive, out button, out status))
                {
                    if (status == GpioOpenStatus.PinOpened)
                    {
                        button.DebounceTimeout = new TimeSpan(Config.Buttons.ButtonDebounce);
                        button.SetDriveMode(GpioPinDriveMode.Input);
                        button.ValueChanged += HandleButton;
                        Debug.WriteLine("Button on pin "+ pinSetting.Value +" successfully bound for action: " + pinSetting.Key.ToString());
                        actionButtons.Add(pinSetting.Value, button);
                        button = null;
                        continue;
                    }
                }

                Debug.WriteLine("Error: Button on pin " + pinSetting.Value + " was unable to be bound becuase: " + status.ToString());
            }

            listener.ConnectionReceived += connectionReceived;
            await listener.BindServiceNameAsync(Config.Api.Port.ToString());
        }


        private void connectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            connections.Add(args.Socket);

            waitForData(args.Socket);
        }

        async private void waitForData(StreamSocket socket)
        {
            var dr = new DataReader(socket.InputStream);
            var stringHeader = await dr.LoadAsync(4);

            if (stringHeader == 0)
            {
                return;
            }

            int strLength = dr.ReadInt32();

            uint numStrBytes = await dr.LoadAsync((uint)strLength);
            string command = dr.ReadString(3);

            App.TelemetryClient.TrackEvent("Action_NetworkCommand");

            switch (command)
            {
                case "Pre":
                    InputRecieved(this, new InputRecievedEventArgs { Action = InputAction.PreviousChannel });
                    break;
                case "Nex":
                    InputRecieved(this, new InputRecievedEventArgs { Action = InputAction.NextChannel });
                    break;
                case "Vup":
                    InputRecieved(this, new InputRecievedEventArgs { Action = InputAction.VolumeUp });
                    break;
                case "Vdn":
                    InputRecieved(this, new InputRecievedEventArgs { Action = InputAction.VolumeDown });
                    break;
                case "Pwr":
                    InputRecieved(this, new InputRecievedEventArgs { Action = InputAction.Sleep });
                    break;
                case "Add":
                    string newPreset = dr.ReadString((uint)strLength - 3);
                    InputRecieved(this, new InputRecievedEventArgs { Action = InputAction.AddChannel, Message = newPreset });
                    break;
                case "Del":
                    string presetToDelete = dr.ReadString((uint)strLength - 3);
                    InputRecieved(this, new InputRecievedEventArgs { Action = InputAction.DeleteChannel, Message = presetToDelete });
                    break;
                default:
                    App.TelemetryClient.TrackEvent("Network_InvalidCommand");
                    break;
            }

            dr.DetachStream();

            waitForData(socket);
        }

        private void HandleButton(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            Debug.WriteLine("Value Change on pin:" + sender.PinNumber +" : " + args.Edge);
            App.TelemetryClient.TrackEvent("Action_PhysicalButton");
            if (args.Edge == GpioPinEdge.RisingEdge)
            {
                InputRecieved(this, new InputRecievedEventArgs { Action = Config.Buttons.ButtonPins[sender.PinNumber], Message="" });
            }
        }
    }
}
