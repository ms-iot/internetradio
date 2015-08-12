using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;

namespace InternetRadio
{
    public sealed class RadioManagerTask : IBackgroundTask
    {
        private static BackgroundTaskDeferral serviceDeferral;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            taskInstance.Canceled += TaskInstance_Canceled;

            serviceDeferral = taskInstance.GetDeferral();

            var appService = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            if (appService.Name == "InternetRadio.RadioManager")
            {
                appService.AppServiceConnection.RequestReceived += AppServiceConnection_RequestReceived;
            }
        }

        private async void AppServiceConnection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            if (null == StartupTask.RadioManager)
            {
                StartupTask.WriteTelemetryEvent("MediaEngineManagerTask_RadioNotRunning");
                return;
            }

            var messageDeferral = args.GetDeferral();
            var message = args.Request.Message;
            string command = message["Command"] as string;

            switch (command)
            {
                case "Power":
                    await StartupTask.RadioManager.ToggleSleep();
                    messageDeferral.Complete();
                    sender.Dispose();
                    serviceDeferral.Complete();
                    break;

                    // Media Commands
                case "Next":
                    StartupTask.RadioManager.NextPreset();
                    messageDeferral.Complete();
                    break;
                case "Previous":
                    StartupTask.RadioManager.NextPreset();
                    messageDeferral.Complete();
                    break;
                case "VolumeUp":
                    await StartupTask.RadioManager.VolumeUp();
                    messageDeferral.Complete();
                    break;
                case "VolumeDown":
                    await StartupTask.RadioManager.VolumeDown();
                    messageDeferral.Complete();
                    break;

                    // Preset commands
                case "GetPresets":
                    var presets = StartupTask.PresetManager.GetPresets();

                    var returnMessage = new ValueSet();
                    returnMessage.Add("Presets", presets);

                    var responseStatus = args.Request.SendResponseAsync(returnMessage);
                    messageDeferral.Complete();

                    break;
                case "AddPreset":
                    string presetName = message["PresetName"] as string;
                    string presetAddress = message["PresetAddress"] as string;
                    var newPreset = new Channel() { Name = presetName, Address = presetAddress };
                    StartupTask.PresetManager.AddPreset(newPreset);
                    messageDeferral.Complete();
                    break;
                case "DeletePreset":
                    string presetToDeleteName = message["PresetName"] as string;
                    StartupTask.PresetManager.DeletePreset(presetToDeleteName);
                    messageDeferral.Complete();
                    break;

            }
        }

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
        }
    }
}
