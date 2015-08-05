using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;

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
                case "Next":
                    StartupTask.RadioManager.NextPreset();
                    messageDeferral.Complete();
                    serviceDeferral.Complete();
                    break;
                case "Previous":
                    StartupTask.RadioManager.NextPreset();
                    messageDeferral.Complete();
                    serviceDeferral.Complete();
                    break;
                case "Power":
                    await StartupTask.RadioManager.ToggleSleep();
                    messageDeferral.Complete();
                    serviceDeferral.Complete();
                    break;
            }
        }

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
        }
    }
}
