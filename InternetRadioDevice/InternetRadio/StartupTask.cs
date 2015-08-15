using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using UniversalMediaEngine;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Windows.UI.Xaml;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace InternetRadio
{
    public sealed class StartupTask : IBackgroundTask
    {
        private static TelemetryClient TelemetryClient;

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
            if (null == TelemetryClient)
            {
                TelemetryClient = new TelemetryClient();
            }

            TelemetryClient.TrackEvent(eventName, properties);
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
