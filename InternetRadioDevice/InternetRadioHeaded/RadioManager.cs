using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TextDisplay;
using System.Collections.Generic;
using Windows.ApplicationModel.Resources;

namespace InternetRadio
{
    internal class RadioManager 
    {
        private IPlaylistManager radioPresetManager;
        private IPlaybackManager radioPlaybackManager;
        private IDevicePowerManager radioPowerManager;
        private ITextDisplay display;
        private ResourceLoader resourceLoader;

        private AllJoynInterfaceManager allJoynInterfaceManager;
        private GpioInterfaceManager gpioInterfaceManager;
        private AppServicesInterfaceManager appServicesInterfaceManager;

        private uint playbackRetries;
        private const uint maxRetries = 3;

        public double Volume
        {
            get
            {
                return this.radioPlaybackManager.Volume;
            }
            set
            {
                this.radioPlaybackManager.Volume = value;
            }
        }

        public PlaybackState PlayState
        {
            get
            {
                return this.radioPlaybackManager.PlaybackState;
            }
            set
            {
                switch (value)
                {
                    case PlaybackState.Paused:
                        if (this.radioPlaybackManager.PlaybackState != PlaybackState.Paused)
                        {
                            this.radioPlaybackManager.Pause();
                        }
                        break;
                    case PlaybackState.Playing:
                        if (null != this.radioPresetManager.CurrentTrack && this.radioPlaybackManager.PlaybackState != PlaybackState.Playing)
                        { 
                            this.radioPlaybackManager.Play(new Uri(this.radioPresetManager.CurrentTrack.Address));
                        }
                        break;
                }
            }
        }

        internal IPlaylistManager RadioPresetManager
        {
            get
            {
                return radioPresetManager;
            }
        }

        public async Task Initialize()
        {
            this.playbackRetries = 0;

            var telemetryInitializeProperties = new Dictionary<string, string>();
#pragma warning disable CS0618 // No current view for Background task
            this.resourceLoader = new ResourceLoader("Resources");
#pragma warning restore CS0618 // No current view for Background task           

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
            //allJoynInterfaceManager = new AllJoynInterfaceManager(this.radioPlaybackManager, this.radioPresetManager, this.radioPowerManager);
            //this.allJoynInterfaceManager.Initialize();
            
            //await this.tryWriteToDisplay(this.resourceLoader.GetString("AllJoynIdMessage") +"\n" + this.allJoynInterfaceManager.GetBusId(), 0);

            this.gpioInterfaceManager = new GpioInterfaceManager(this.radioPlaybackManager, this.radioPresetManager, this.radioPowerManager);
            if (!this.gpioInterfaceManager.Initialize())
            {
                Debug.WriteLine("RadioManager: Failed to initialize GPIO");
                telemetryInitializeProperties.Add("GpioAvailable", false.ToString());
            }
            else
            {
                telemetryInitializeProperties.Add("GpioAvailable", true.ToString());
            }

            this.appServicesInterfaceManager = new AppServicesInterfaceManager(this.radioPlaybackManager, this.radioPresetManager, this.radioPowerManager);

            // Manage settings
            this.radioPlaybackManager.Volume = this.loadVolume();
            var previousPlaylist = this.loadPlaylistId();
            if (previousPlaylist.HasValue)
            {
                await this.radioPresetManager.LoadPlayList(previousPlaylist.Value);
                telemetryInitializeProperties.Add("FirstBoot", false.ToString());
            }
            else
            {
                telemetryInitializeProperties.Add("FirstBoot", true.ToString());
            }

            if (this.radioPresetManager.CurrentPlaylist == null)
            {
                var newPlaylistId = await this.radioPresetManager.StartNewPlaylist("DefaultPlaylist", new List<Track>(), true);
                this.savePlaylistId(newPlaylistId);
            }

            var displays = await TextDisplayManager.GetDisplays();
            this.display = displays.FirstOrDefault();
            if (null != this.display)
            {
                telemetryInitializeProperties.Add("DisplayAvailable", true.ToString());
                telemetryInitializeProperties.Add("DisplayHeight", this.display.Height.ToString());
                telemetryInitializeProperties.Add("DisplayWidth", this.display.Width.ToString());
            }
            else
            {
                Debug.WriteLine("RadioManager: No displays available");
                telemetryInitializeProperties.Add("DisplayAvailable", false.ToString());
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

                    await this.tryWriteToDisplay(this.resourceLoader.GetString("StartupMessageLine1") + 
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
                    await this.tryWriteToDisplay(this.resourceLoader.GetString("ShutdownMessage"), 0);
                    this.radioPlaybackManager.Pause();
                    await Task.Delay(Config.Messages_StartupMessageDelay);
                    await this.tryWriteToDisplay(String.Empty + "\n" + String.Empty, 0);
                    break;
            }
        }

        private async void RadioPlaybackManager_PlaybackStateChanged(object sender, PlaybackStateChangedEventArgs e)
        {
            switch(e.State)
            {
                case PlaybackState.Error_MediaInvalid:
                    await this.tryWriteToDisplay(this.resourceLoader.GetString("MediaErrorMessage") + "\n" + this.radioPresetManager.CurrentTrack.Name, 0);
                    break;

                case PlaybackState.Loading:
                    await this.tryWriteToDisplay(this.resourceLoader.GetString("MediaLoadingMessage") + "\n" + this.radioPresetManager.CurrentTrack.Name, 0);
                    break;

                case PlaybackState.Playing:
                    playbackRetries = 0;
                    await this.tryWriteToDisplay(this.resourceLoader.GetString("NowPlayingMessage") + "\n" + this.radioPresetManager.CurrentTrack.Name, 0);
                    break;
                case PlaybackState.Ended:
                    if (maxRetries > playbackRetries)
                    {
                        playChannel(this.radioPresetManager.CurrentTrack);
                    }
                    else
                    {
                        await this.tryWriteToDisplay(this.resourceLoader.GetString("ConnectionFailedMessage") + "\n" + this.radioPresetManager.CurrentTrack.Name, 0);
                    }

                    break;
            }
        }

        private async void RadioPlaybackManager_VolumeChanged(object sender, VolumeChangedEventArgs e)
        {
            await this.tryWriteToDisplay(this.resourceLoader.GetString("VolumeMesage") + "\n" + ((int)(e.Volume * 100)).ToString() + "%", 3);
        }

        private void RadioPresetManager_CurrentTrackChanged(object sender, PlaylistCurrentTrackChangedEventArgs e)
        {
            playChannel(e.CurrentTrack);
        }

        private void RadioPresetManager_PlaylistChanged(object sender, PlaylistChangedEventArgs e)
        {
        }

        public async Task Dispose()
        {
            if (null != this.display)
                await this.display.DisposeAsync();
        }

        private void playChannel(Track track)
        {
            Debug.WriteLine("RadioManager: Play Track - " + track.Name);
            this.radioPlaybackManager.Play(new Uri(track.Address));
        }

        private async Task tryWriteToDisplay(string message, uint timeout)
        {
            if (null != this.display)
            {
                await this.display.WriteMessageAsync(message, timeout);
            }

            Debug.WriteLine("RadioManager: Display - " + message);
        }

        internal void saveVolume(double volume)
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["volume"] = volume;
        }

        internal double loadVolume()
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
