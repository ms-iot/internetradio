using System.Collections.Generic;
using Windows.ApplicationModel.Background;
using Microsoft.ApplicationInsights;
using com.microsoft.maker.InternetRadio;
using Windows.Devices.AllJoyn;
using System;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace InternetRadio
{
    public sealed class StartupTask : IBackgroundTask
    {
        private static TelemetryClient s_telemetryClient;
        internal static RadioManager s_radioManager;
        private BackgroundTaskDeferral deferral;
        private static HttpServer s_webServer;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            WriteTelemetryEvent("App_Startup");

            deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += TaskInstance_Canceled;

            if (null == s_radioManager)
            {
                s_radioManager = new RadioManager();
                await s_radioManager.Initialize();
            }

            if (null == s_webServer)
            {
                s_webServer = new HttpServer(8000);
                s_webServer.StartServer();
            }

        }

        public static void WriteTelemetryEvent(string eventName)
        {
            WriteTelemetryEvent(eventName, new Dictionary<string, string>());
        }

        public static void WriteTelemetryEvent(string eventName, IDictionary<string, string> properties)
        {
            if (null == s_telemetryClient)
            {
                s_telemetryClient = new TelemetryClient();
            }

            s_telemetryClient.TrackEvent(eventName, properties);
        }

        public static void WriteTelemetryException(Exception e)
        {
            WriteTelemetryException(e, new Dictionary<string, string>());
        }

        public static void WriteTelemetryException(Exception e, IDictionary<string, string> properties)
        {
            if (null == s_telemetryClient)
            {
                s_telemetryClient = new TelemetryClient();
            }

            s_telemetryClient.TrackException(e, properties);
        }

        private async void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            var properties = new Dictionary<string, string>();
            properties.Add("Reason", reason.ToString());
            WriteTelemetryEvent("App_Shutdown", properties);

            await s_radioManager.Dispose();

            deferral.Complete();
        }
    }
}
