using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Gpio;

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

    class GpioInterfaceManager
    {
        private Dictionary<InputAction, GpioPin> actionButtons;
        private IPlaybackManager playbackManager;
        private IPlaylistManager playlistManager;
        private IDevicePowerManager powerManager;

        public GpioInterfaceManager(IPlaybackManager playbackManager, IPlaylistManager playlistManager, IDevicePowerManager powerManager)
        {
            this.playbackManager = playbackManager;
            this.playlistManager = playlistManager;
            this.powerManager = powerManager;
        }

        public bool Initialize()
        {
            actionButtons = new Dictionary<InputAction, GpioPin>();
            var gpio = GpioController.GetDefault();

            if (null == gpio)
            {
                Debug.WriteLine("GpioInterfaceManager: No GPIO controller found");
                return false;
            }

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

            return true;
        }

        private void handleButton(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            Debug.WriteLine("Value Change on pin:" + sender.PinNumber +" : " + args.Edge);
            StartupTask.WriteTelemetryEvent("Action_PhysicalButton");
            if (args.Edge == GpioPinEdge.RisingEdge)
            {
                switch(Config.Buttons_Pins[sender.PinNumber])
                {
                    case InputAction.NextChannel:
                        this.playlistManager.NextTrack();
                        break;
                    case InputAction.PreviousChannel:
                        this.playlistManager.PreviousTrack();
                        break;
                    case InputAction.VolumeUp:
                        this.playbackManager.Volume += 0.1;
                        break;
                    case InputAction.VolumeDown:
                        this.playbackManager.Volume -= 0.1;
                        break;
                    case InputAction.Sleep:
                        if (PowerState.Powered == this.powerManager.PowerState)
                        {
                            this.powerManager.PowerState = PowerState.Standby;
                        }
                        else
                        {
                            this.powerManager.PowerState = PowerState.Powered;
                        }
                        break;
                }
            }
        }

        private Track deserialzePreset(string serializedPreset)
        {
            var preset = new Track();
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
