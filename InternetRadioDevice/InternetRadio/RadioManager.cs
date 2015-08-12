using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalMediaEngine;

namespace InternetRadio
{
    class RadioManager
    {
        private bool isAsleep;
        private MediaEngine mediaEngine;
        private PresetManager presetManager;
        private DisplayController displayController;
        private InputManager inputManager;

        public RadioManager(PresetManager presetManager)
        {
            this.presetManager = presetManager;
        }

        public async Task Initialize()
        {
            this.isAsleep = false;

            this.displayController = new DisplayController();
            await this.displayController.Initialize();

            this.inputManager = new InputManager();
            inputManager.InputRecieved += InputManager_InputRecieved;
            await inputManager.Initialize();

            this.mediaEngine = new MediaEngine();
            var result = await this.mediaEngine.InitializeAsync();
            if (result == MediaEngineInitializationResult.Fail)
            {
                StartupTask.WriteTelemetryEvent("MediaEngine_FailedToInitialize");
                await this.displayController.WriteMessageAsync("Fatal Error", "Failed to initialize MediaEngine", 0);
            }

            this.mediaEngine.Volume = (0.05);
            this.mediaEngine.MediaStateChanged += MediaEngine_MediaStateChanged;

            var packagefn = Windows.ApplicationModel.Package.Current.Id.FamilyName;

            await wake();
        }

        public async Task ToggleSleep()
        {
            if (isAsleep)
            {
                StartupTask.WriteTelemetryEvent("Wake");
                await wake();
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

        private async void playChannel(Channel channel)
        {
            Debug.WriteLine("Play Channel: " + channel.Name);
            this.mediaEngine.Play(channel.Address.ToString());
            await this.displayController.WriteMessageAsync("Now Playing: ", channel.Name, 0);
        }

        private async Task changeVolume(double volumeChange)
        {
            if ((volumeChange > 0 && this.mediaEngine.Volume < 1) || (volumeChange < 0 && this.mediaEngine.Volume > 0))
            {
                this.mediaEngine.Volume += volumeChange;
            }

            await this.displayController.WriteMessageAsync("Volume: ", ((int)(this.mediaEngine.Volume * 100)).ToString() + "%", 3);

        }

        private async void MediaEngine_MediaStateChanged(MediaState state)
        {
            switch (state)
            {
                case MediaState.Loading:
                    await this.displayController.WriteMessageAsync("Loading Media...", string.Empty, 0);
                    break;

                case MediaState.Playing:
                    await this.displayController.WriteMessageAsync("Now Playing: ", this.presetManager.CurrentPreset().Name, 0);
                    break;

                case MediaState.Error:
                    break;
            }
        }


        private async void InputManager_InputRecieved(object sender, InputRecievedEventArgs e)
        {
            switch (e.Action)
            {
                case InputAction.AddChannel:
                    Channel newPreset;
                    if (checkAndDeserialzePreset(e.Message, out newPreset))
                    {
                        this.presetManager.AddPreset(newPreset);
                        await this.displayController.WriteMessageAsync("Preset Added:", newPreset.Name, 3);
                        break;
                    }
                    else
                    {
                        Debug.WriteLine("Preset added from network was invalid, ignoring");
                    }

                    break;
                case InputAction.DeleteChannel:

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
        private bool checkAndDeserialzePreset(string serializedPreset, out Channel preset)
        {
            preset = new Channel();
            var presetData = serializedPreset.Split(';');

            if (presetData.Count() == 2)
            {
                Uri channelUri;

                if (Uri.TryCreate(presetData[1], UriKind.Absolute, out channelUri))
                {
                    preset = new Channel() { Name = presetData[0], Address = channelUri.ToString() };
                    return true;
                }
            }


            return false;
        }
    }
}
