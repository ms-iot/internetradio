using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;

namespace InternetRadio
{
    public sealed class AppServicesBackgroundTask : IBackgroundTask
    {
        private BackgroundTaskDeferral serviceDeferral;
        private AppServiceConnection appServiceCOnnection;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            taskInstance.Canceled += TaskInstance_Canceled;

            serviceDeferral = taskInstance.GetDeferral();

            var appService = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            if (appService.Name == "InternetRadio.RadioManager")
            {
                appServiceCOnnection = appService.AppServiceConnection;
                appServiceCOnnection.RequestReceived += AppServiceConnection_RequestReceived;
            }
        }

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            var properties = new Dictionary<string, string>();
            properties.Add("Reason", reason.ToString());
            StartupTask.WriteTelemetryEvent("AppServicesBackgroundTask_Canceled", properties);
            serviceDeferral.Complete();
        }

        private async void AppServiceConnection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            if (null == StartupTask.s_radioManager)
            {
                StartupTask.WriteTelemetryEvent("AppServicesBackgroundTask_RadioNotRunning");
                return;
            }

            await StartupTask.s_radioManager.GetAppServicesInterfaceManager().HandleDeferral(args.GetDeferral(), args.Request);
        }
    }
}
