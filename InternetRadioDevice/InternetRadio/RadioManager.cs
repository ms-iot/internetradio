using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TextDisplay;
using com.microsoft.maker.InternetRadio;
using Windows.Devices.AllJoyn;
using System.Collections.Generic;
using Windows.ApplicationModel.Resources;

namespace InternetRadio
{
    class RadioManager 
    {
        private IPlaylistManager radioPresetManager;
        private IPlaybackManager radioPlaybackManager;
        private IDevicePowerManager radioPowerManager;
        private ITextDisplay display;
        private ResourceLoader resourceLoader;

        private AllJoynInterfaceManager allJoynInterfaceManager;
        private InternetRadioProducer internetRadioProducer;
        private GpioInterfaceManager gpioInterfaceManager;
        private AppServicesInterfaceManager appServicesInterfaceManager;

        public async Task Initialize()
        {
            var telemetryInitializeProperties = new Dictionary<string, string>();
#pragma warning disable CS0618 // No current view for Background task
            this.resourceLoader = new ResourceLoader("Resources");
#pragma warning restore CS0618 // No current view for Background task
            var displays = await TextDisplay.TextDisplayManager.GetDisplays();
            this.display = displays.FirstOrDefault();
            if (null != this.display)
            {
                await this.display.InitializeAsync();
                telemetryInitializeProperties.Add("DisplayAvailable", true.ToString());
                telemetryInitializeProperties.Add("DisplayHeight", this.display.Height.ToString());
                telemetryInitializeProperties.Add("DisplayWidth", this.display.Width.ToString());
            }
            else
            {
                Debug.WriteLine("RadioManager: No displays available");
                telemetryInitializeProperties.Add("DisplayAvailable", false.ToString());
            }

            await this.display.WriteMessageAsync("Booting...", 0);

            this.radioPowerManager = new RadioPowerManager();
            this.radioPowerManager.PowerStateChanged += RadioPowerManager_PowerStateChanged;

            this.radioPresetManager = new RadioLocalPresetManager();
            this.radioPresetManager.PlaylistChanged += RadioPresetManager_PlaylistChanged;
            this.radioPresetManager.CurrentTrackChanged += RadioPresetManager_CurrentTrackChanged;

            this.radioPlaybackManager = new MediaEnginePlaybackManager();
            this.radioPlaybackManager.VolumeChanged += RadioPlaybackManager_VolumeChanged;
            this.radioPlaybackManager.PlaybackStateChanged += RadioPlaybackManager_PlaybackStateChanged;
            await this.radioPlaybackManager.InitialzeAsync();

            // Initialize the input managers
            AllJoynBusAttachment radioAlljoynBusAttachment = new AllJoynBusAttachment();
            internetRadioProducer = new InternetRadioProducer(radioAlljoynBusAttachment);
            radioAlljoynBusAttachment.AboutData.DefaultAppName = "Internet Radio";
            radioAlljoynBusAttachment.AboutData.DefaultDescription = "Internet Radio Device";
            radioAlljoynBusAttachment.AboutData.DefaultManufacturer = "Microsoft Corporation";
            radioAlljoynBusAttachment.AboutData.IsEnabled = true;
            allJoynInterfaceManager = new AllJoynInterfaceManager(internetRadioProducer, this.radioPlaybackManager, this.radioPresetManager, this.radioPowerManager);
            internetRadioProducer.Service = allJoynInterfaceManager;
            internetRadioProducer.Start();

            await this.display.WriteMessageAsync("AllJoyn Device ID:\n" + radioAlljoynBusAttachment.UniqueName,0);

            this.gpioInterfaceManager = new GpioInterfaceManager(this.radioPlaybackManager, this.radioPresetManager, this.radioPowerManager);
            this.gpioInterfaceManager.Initialize();

            this.appServicesInterfaceManager = new AppServicesInterfaceManager(this.radioPlaybackManager, this.radioPresetManager, this.radioPowerManager);

            // Manage settings
            this.radioPlaybackManager.Volume = this.loadVolume();
            var previousPlaylist = this.loadPlaylistId();
            if (previousPlaylist.HasValue)
            {
                await this.radioPresetManager.LoadPlayList(previousPlaylist.Value);
                if (this.radioPresetManager.CurrentPlaylist == null)
                {
                    var newPlaylistId = await this.radioPresetManager.StartNewPlaylist("DefaultPlaylist", new List<Track>(), true);
                    this.savePlaylistId(newPlaylistId);
                }
            }
            else
            {
                var newPlaylistId = await this.radioPresetManager.StartNewPlaylist("DefaultPlaylist", new List<Track>(), true);
                this.savePlaylistId(newPlaylistId);
            }

            // Wake up the radio
            this.radioPowerManager.PowerState = PowerState.Powered;

            StartupTask.WriteTelemetryEvent("App_Initialize", telemetryInitializeProperties);
        }

        public AppServicesInterfaceManager GetAppServicesInterfaceManager()
        {
            return this.appServicesInterfaceManager;
        }

        private async void RadioPowerManager_PowerStateChanged(object sender, PowerStateChangedEventArgs e)
        {
            switch(e.PowerState)
            {
                case PowerState.Powered:

                    await this.display.WriteMessageAsync(this.resourceLoader.GetString("StartupMessageLine1") + 
                                                        "\n" + 
                                                        this.resourceLoader.GetString("StartupMessageLine2"), 
                                                        0);

                    await Task.Delay(Config.Messages_StartupMessageDelay);
                    if (null != this.radioPresetManager.CurrentTrack)
                    {
                        playChannel(this.radioPresetManager.CurrentTrack);
                    }
                    break;
                case PowerState.Standby:
                    await this.display.WriteMessageAsync(this.resourceLoader.GetString("ShutdownMessage"), 0);
                    this.radioPlaybackManager.Pause();
                    await Task.Delay(Config.Messages_StartupMessageDelay);
                    await this.display.WriteMessageAsync(String.Empty + "\n" + String.Empty, 0);
                    break;
            }
        }

        private async void RadioPlaybackManager_PlaybackStateChanged(object sender, PlaybackStateChangedEventArgs e)
        {
            switch(e.State)
            {
                case PlaybackState.Error_MediaInvalid:
                    await this.display.WriteMessageAsync(this.resourceLoader.GetString("MediaErrorMessage") + "\n" + this.radioPresetManager.CurrentTrack.Name, 0);
                    break;

                case PlaybackState.Loading:
                    await this.display.WriteMessageAsync(this.resourceLoader.GetString("MediaLoadingMessage") + "\n" + this.radioPresetManager.CurrentTrack.Name, 0);
                    break;

                case PlaybackState.Playing:
                    await this.display.WriteMessageAsync(this.resourceLoader.GetString("NowPlayingMessage") + "\n" + this.radioPresetManager.CurrentTrack.Name, 0);
                    break;

            }
        }

        private async void RadioPlaybackManager_VolumeChanged(object sender, VolumeChangedEventArgs e)
        {
            await this.display.WriteMessageAsync(this.resourceLoader.GetString("VolumeMesage") + "\n" + ((int)(e.Volume * 100)).ToString() + "%", 3);
        }

        private void RadioPresetManager_CurrentTrackChanged(object sender, PlaylistCurrentTrackChangedEventArgs e)
        {
            playChannel(e.CurrentTrack);
        }

        private async void RadioPresetManager_PlaylistChanged(object sender, PlaylistChangedEventArgs e)
        {
            await this.display.WriteMessageAsync(this.resourceLoader.GetString("PlaylistLoadedMessage") + "\n" + e.Playlist.Name, 3);
        }

        public async Task Dispose()
        {
            await this.display.DisposeAsync();
        }

        private void playChannel(Track channel)
        {
            Debug.WriteLine("Play Channel: " + channel.Name);
            this.radioPlaybackManager.Play(new Uri(channel.Address));
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

        private void savePlaylistId(Guid id)
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["playlist"] = id;
        }

        private Guid? loadPlaylistId()
        {
            Guid? playlistId = null;
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            if (localSettings.Values.ContainsKey("playlist"))
            {
                playlistId = localSettings.Values["playlist"] as Guid?;
            }

            return playlistId;
        }
    }
}
