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

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace InternetRadio
{
    public sealed class StartupTask : IBackgroundTask
    {
        internal static RadioManager RadioManager;

        private BackgroundTaskDeferral defferal;
        private static TelemetryClient TelemetryClient = null;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            WriteTelemetryEvent("Startup");

            defferal = taskInstance.GetDeferral();
            taskInstance.Canceled += TaskInstance_Canceled;

            if (null == RadioManager)
            {
                RadioManager = new RadioManager();
                await RadioManager.Initialize();
            }
        }

        public static void WriteTelemetryEvent(string eventName)
        {
            if (null == TelemetryClient)
            {
                //var telemetryConfig = new TelemetryConfiguration();
                //telemetryConfig.InstrumentationKey = "30c9af2e-6da0-4733-93a0-de9a4c94f2ab";
                TelemetryClient = new TelemetryClient();
            }

            TelemetryClient.TrackEvent(eventName);
        }

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            //a few reasons that you may be interested in.
            switch (reason)
            {
                case BackgroundTaskCancellationReason.Abort:
                    //app unregistered background task (amoung other reasons).
                    break;
                case BackgroundTaskCancellationReason.Terminating:
                    break;
                case BackgroundTaskCancellationReason.ConditionLoss:
                    break;
                case BackgroundTaskCancellationReason.SystemPolicy:
                    break;
            }
            defferal.Complete();
        }
    }
}
