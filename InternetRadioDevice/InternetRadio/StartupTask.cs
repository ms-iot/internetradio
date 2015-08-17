using System.Collections.Generic;
using Windows.ApplicationModel.Background;
using Microsoft.ApplicationInsights;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace InternetRadio
{
    public sealed class StartupTask : IBackgroundTask
    {
        private static TelemetryClient telemetryClient;

        private BackgroundTaskDeferral deferral;
        private RadioManager radioManager;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            WriteTelemetryEvent("App_Startup");

            deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += TaskInstance_Canceled;

            radioManager = new RadioManager();
            await radioManager.Initialize();
        }

        public static void WriteTelemetryEvent(string eventName)
        {
            WriteTelemetryEvent(eventName, new Dictionary<string, string>());
        }

        public static void WriteTelemetryEvent(string eventName, IDictionary<string, string> properties)
        {
            if (null == telemetryClient)
            {
                telemetryClient = new TelemetryClient();
            }

            telemetryClient.TrackEvent(eventName, properties);
        }

        private async void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            var properties = new Dictionary<string, string>();
            properties.Add("Reason", reason.ToString());
            WriteTelemetryEvent("App_Shutdown", properties);

            await this.radioManager.Dispose();

            deferral.Complete();
        }
    }
}
