using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InternetRadioDevice
{
    class PresetManager
    {
        private List<Channel> radioPresets;
        int currentPreset;

        public PresetManager()
        {
            loadPresets();

            radioPresets = new List<Channel>();
            radioPresets.Add(new Channel("KEXP", new Uri("http://192.168.1.100:8080/kexp.mp3")));
            radioPresets.Add(new Channel("NPR Car Talk", new Uri("http://192.168.1.100:8081/car.mp3")));
        }

        public Channel CurrentChannel()
        {
            return radioPresets[currentPreset];
        }

        public Channel NextChannel()
        {
            if (currentPreset < radioPresets.Count() - 1)
            {
                currentPreset++;
            }
            else
            {
                currentPreset = 0;
            }

            savePresets();
            return radioPresets[currentPreset];
        }

        public Channel PreviousChannel()
        {
            if (currentPreset != 0)
            {
                currentPreset--;
            }
            else
            {
                currentPreset = radioPresets.Count() - 1;
            }

            savePresets();
            return radioPresets[currentPreset];
        }

        public void AddChannel(Channel channel)
        {
            radioPresets.Add(channel);
        }

        private void savePresets()
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["currentPreset"] = currentPreset;
        }

        private void loadPresets()
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            if (localSettings.Values.ContainsKey("currentPreset"))
            {
                currentPreset = Convert.ToInt32(localSettings.Values["currentPreset"]);
            }
            else
            {
                currentPreset = 0;
            }
        }
    }
}
