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
    class AppServicesInterfaceManager
    {
        private IPlaylistManager playlistManager;
        private IPlaybackManager playbackManager;
        private IDevicePowerManager powerManager;

        public AppServicesInterfaceManager(IPlaybackManager playbackManager, IPlaylistManager playlistManager, IDevicePowerManager powerManager)
        {
            this.playbackManager = playbackManager;
            this.playlistManager = playlistManager;
            this.powerManager = powerManager;
        }

        public async Task HandleDeferral(AppServiceDeferral deferral, AppServiceRequest request)
        {
            StartupTask.WriteTelemetryEvent("Action_AppService");

            ValueSet returnMessage = new ValueSet();
            await request.SendResponseAsync(returnMessage);

            deferral.Complete();
        }
    }
}
