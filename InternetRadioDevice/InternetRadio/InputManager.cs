using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace InternetRadio
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

    internal struct InputRecievedEventArgs
    {
        public InputAction Action;
        public Channel Channel;
    }

    // A delegate type for hooking up change notifications.
    delegate void InputRecievedEventHandler(object sender, InputRecievedEventArgs e);

    class InputManager
    {
        public event InputRecievedEventHandler InputRecieved;

        private Dictionary<InputAction, GpioPin> actionButtons;

        private StreamSocketListener listener = new StreamSocketListener();
        private List<StreamSocket> connections = new List<StreamSocket>();

        public async Task Initialize()
        {
            listener.ConnectionReceived += connectionReceived;
            await listener.BindServiceNameAsync(Config.Api_Port.ToString());

            SetupGpio();
        }

        private void SetupGpio()
        {
            actionButtons = new Dictionary<InputAction, GpioPin>();
            var gpio = GpioController.GetDefault();

            foreach (var pinSetting in Config.Buttons_Pins)
            {
                GpioPin button;
                GpioOpenStatus status;

                if (gpio.TryOpenPin(pinSetting.Key, GpioSharingMode.Exclusive, out button, out status))
                {
                    if (status == GpioOpenStatus.PinOpened)
                    {
                        button.DebounceTimeout = new TimeSpan(Config.Buttons_Debounce);
                        button.SetDriveMode(GpioPinDriveMode.Input);
                        button.ValueChanged += handleButton;
                        Debug.WriteLine("Button on pin " + pinSetting.Value + " successfully bound for action: " + pinSetting.Key.ToString());
                        actionButtons.Add(pinSetting.Value, button);
                        button = null;
                        continue;
                    }
                }

                Debug.WriteLine("Error: Button on pin " + pinSetting.Value + " was unable to be bound because: " + status.ToString());
            }
        }


        private void connectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            connections.Add(args.Socket);

            waitForData(args.Socket);
        }

        async private void waitForData(StreamSocket socket)
        {
            var dr = new DataReader(socket.InputStream);
            uint stringHeader;
            try
            {
                stringHeader = await dr.LoadAsync(4);
            }
            catch (Exception)
            {
                Debug.WriteLine("Lost connection to remote");
                return;
            }

            if (stringHeader == 0)
            {
                return;
            }

            int strLength = dr.ReadInt32();

            uint numStrBytes = await dr.LoadAsync((uint)strLength);
            string command = dr.ReadString(3);

            StartupTask.WriteTelemetryEvent("Action_NetworkCommand");

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
                    string newPresetStr = dr.ReadString((uint)strLength - 3);
                    Channel newPreset = deserialzePreset(newPresetStr);
                    InputRecieved(this, new InputRecievedEventArgs { Action = InputAction.AddChannel, Channel = newPreset });
                    break;
                case "Del":
                    string presetToDeleteStr = dr.ReadString((uint)strLength - 3);
                    Channel presetToDelete = deserialzePreset(presetToDeleteStr);
                    InputRecieved(this, new InputRecievedEventArgs { Action = InputAction.DeleteChannel, Channel = presetToDelete });
                    break;
                default:
                    StartupTask.WriteTelemetryEvent("Network_InvalidCommand");
                    break;
            }

            dr.DetachStream();

            waitForData(socket);
        }

        private void handleButton(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            Debug.WriteLine("Value Change on pin:" + sender.PinNumber +" : " + args.Edge);
            StartupTask.WriteTelemetryEvent("Action_PhysicalButton");
            if (args.Edge == GpioPinEdge.RisingEdge)
            {
                InputRecieved(this, new InputRecievedEventArgs { Action = Config.Buttons_Pins[sender.PinNumber] });
            }
        }

        private Channel deserialzePreset(string serializedPreset)
        {
            var preset = new Channel();
            var presetData = serializedPreset.Split(';');

            preset.Name = presetData.ElementAtOrDefault(0);

            Uri channelUri;
            if (Uri.TryCreate(presetData.ElementAtOrDefault(1), UriKind.Absolute, out channelUri))
            {
                preset.Address = channelUri.ToString();
            }

            return preset;
        }
    }
}
