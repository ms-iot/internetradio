using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UniversalMediaEngine;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml;

namespace InternetRadio
{
    class RadioManager
    {
        private bool isInitialzed;
        private bool isAsleep;
        private bool showingFirstRun;
        private MediaEngine mediaEngine;
        private PresetManager presetManager;
        private DisplayController displayController;
        private InputManager inputManager;

        public async Task Initialize()
        {
            this.isAsleep = false;
            this.showingFirstRun = false;

            this.presetManager = new PresetManager();
            this.presetManager.PresetAdded += PresetManager_PresetAdded;

            this.displayController = new DisplayController();
            this.displayController.Initialize();

            this.inputManager = new InputManager();
            this.inputManager.InputRecieved += InputManager_InputRecieved;
            await inputManager.Initialize();

            this.mediaEngine = new MediaEngine();
            var result = await this.mediaEngine.InitializeAsync();
            if (result == MediaEngineInitializationResult.Fail)
            {
               StartupTask.WriteTelemetryEvent("MediaEngine_FailedToInitialize");
               await this.displayController.WriteMessageAsync("Fatal Error", "Failed to initialize MediaEngine", 10);
                Application.Current.Exit();
            }

            this.mediaEngine.Volume = loadVolume();
            this.mediaEngine.MediaStateChanged += MediaEngine_MediaStateChanged;

            await initializeIfNeeded();
        }

        public async Task Dispose()
        {
            if (!isInitialzed)
                return;

            await this.displayController.Dispose();
        }

        public async Task ToggleSleep()
        {
            if (isAsleep)
            {
                StartupTask.WriteTelemetryEvent("Wake");
                await initializeIfNeeded();
            }
            else
            {
                StartupTask.WriteTelemetryEvent("Sleep");
                await sleep();
            }
        }

        public void NextPreset()
        {
            playChannel(this.presetManager.NextPreset());
        }

        public void PreviousPreset()
        {
            playChannel(this.presetManager.PreviousPreset());
        }

        public async Task VolumeUp()
        {
            await changeVolume(0.1);
        }

        public async Task VolumeDown()
        {
            await changeVolume(-0.1);
        }

        private async Task wake()
        {
            await this.displayController.WriteMessageAsync(Config.Messages_StartupMessageLineOne, Config.Messages_StartupMessageLineTwo, 0);

            await Task.Delay(Config.Messages_StartupMessageDelay);
            playChannel(this.presetManager.CurrentPreset());
            this.isAsleep = false;
        }

        private async Task sleep()
        {
            await this.displayController.WriteMessageAsync(Config.Messages_ShutdownMessageLineOne, Config.Messages_ShutdownMessageLineTwo, 0);
            this.mediaEngine.Pause();
            await Task.Delay(Config.Messages_StartupMessageDelay);
            await this.displayController.WriteMessageAsync(String.Empty, String.Empty, 0);
            this.isAsleep = true;
        }
        private async Task firstRun(IPAddress address)
        {
            this.showingFirstRun = true;
            await this.displayController.WriteMessageAsync(Config.Messages_StartupMessageLineOne, Config.Messages_StartupMessageLineTwo, 5);
            await Task.Delay(3000);
            if (null != address)
            {
                await Task.Delay(3000);
                await this.displayController.WriteMessageAsync("No Presets", "", 5);
                await Task.Delay(3000);
                await this.displayController.WriteMessageAsync("Please connect", "controller application", 5);
                await Task.Delay(3000);
                await this.displayController.WriteMessageAsync("IP Address", address.ToString(), 0);
            }
            else
            {
                await this.displayController.WriteMessageAsync("No Network", "Connection", 0);
                await Task.Delay(3000);
                await this.displayController.WriteMessageAsync("Please join network", "and reboot", 0);
            }
        }

        private void playChannel(Channel channel)
        {
            Debug.WriteLine("Play Channel: " + channel.Name);
            this.mediaEngine.Play(channel.Address.ToString());
        }

        private async Task changeVolume(double volumeChange)
        {
            if ((volumeChange > 0 && this.mediaEngine.Volume < 1) || (volumeChange < 0 && this.mediaEngine.Volume > 0))
            {
                this.mediaEngine.Volume += volumeChange;
            }

            await this.displayController.WriteMessageAsync("Volume: ", ((int)(this.mediaEngine.Volume * 100)).ToString() + "%", 3);
            saveVolume(this.mediaEngine.Volume);
        }

        private async void MediaEngine_MediaStateChanged(MediaState state)
        {
            switch (state)
            {
                case MediaState.Loading:
                    await this.displayController.WriteMessageAsync("Loading Media:", this.presetManager.CurrentPreset().Name, 0);
                    break;

                case MediaState.Playing:
                    await this.displayController.WriteMessageAsync("Now Playing: ", this.presetManager.CurrentPreset().Name, 0);
                    break;

                case MediaState.Error:
                    await this.displayController.WriteMessageAsync("Media Error:", this.presetManager.CurrentPreset().Name, 0);
                    break;
            }
        }

        private async void PresetManager_PresetAdded(object sender, PresetAddedEventArgs e)
        {
            if (!await initializeIfNeeded())
                return;
            await this.displayController.WriteMessageAsync("Preset Added:", e.Preset.Name, 3);
        }

        private async void InputManager_InputRecieved(object sender, InputRecievedEventArgs e)
        {           
            if (!await initializeIfNeeded())
                return;

            switch (e.Action)
            {
                case InputAction.AddChannel:
                    if (!this.presetManager.AddPreset(e.Channel))
                    {
                        await this.displayController.WriteMessageAsync("Failed to add preset:", e.Channel.Name, 3);
                    }
                    break;
                case InputAction.DeleteChannel:
                    this.presetManager.DeletePreset(e.Channel.Name);
                    await this.displayController.WriteMessageAsync("Preset Deleted:", e.Channel.Name, 3);
                    break;
                case InputAction.NextChannel:
                    NextPreset();
                    break;
                case InputAction.PreviousChannel:
                    PreviousPreset();
                    break;
                case InputAction.VolumeDown:
                    await VolumeDown();
                    break;
                case InputAction.VolumeUp:
                    await VolumeUp();
                    break;
                case InputAction.Sleep:
                    await ToggleSleep();
                    break;
            }
        }
        private void saveVolume(double volume)
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["volume"] = volume;
        }

        private double loadVolume()
        {
            double volume = 0;
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            if (localSettings.Values.ContainsKey("volume"))
            {
                volume = Convert.ToDouble(localSettings.Values["volume"]);
            }

            return volume;
        }

        private async Task<bool> initializeIfNeeded()
        {
            if (!isInitialzed)
            {
                if (this.showingFirstRun)
                    return false;

                IPAddress radioAddress = null;

                var icp = NetworkInformation.GetInternetConnectionProfile();

                if (icp != null && icp.NetworkAdapter != null)
                {
                    var hostname =
                        NetworkInformation.GetHostNames()
                            .FirstOrDefault(
                                hn =>
                                hn.IPInformation != null && hn.IPInformation.NetworkAdapter != null
                                && hn.IPInformation.NetworkAdapter.NetworkAdapterId
                                == icp.NetworkAdapter.NetworkAdapterId && hn.Type == Windows.Networking.HostNameType.Ipv4);

                    if (null != hostname)
                    {
                        radioAddress = IPAddress.Parse(hostname.CanonicalName);
                    }
                }


                if ((this.presetManager != null &&
                this.presetManager.GetPresets().Count == 0)
                || null == radioAddress)
                {
                    await firstRun(radioAddress);
                }
                else
                {
                    this.showingFirstRun = false;
                    this.isInitialzed = true;
                    await wake();
                }
            }

            return isInitialzed;
        }
    }
}
