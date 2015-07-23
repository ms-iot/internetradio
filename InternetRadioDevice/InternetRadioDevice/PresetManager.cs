using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            radioPresets = new List<Channel>();
            loadPresets();
        }

        public Channel CurrentChannel()
        {
            return getPreset(currentPreset);
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
            return getPreset(currentPreset);
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
            return getPreset(currentPreset);
        }

        public void AddChannel(Channel channel)
        {
            radioPresets.Add(channel);
            savePresets();
        }

        private Channel getPreset(int presetNumber)
        {
            if (radioPresets.Count == 0)
                return new Channel("", new Uri("http://localhost"));

            return radioPresets[presetNumber];
        }

        private void savePresets()
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["currentPreset"] = currentPreset;

            string seralizedPresets = "";

            foreach (var preset in radioPresets)
            {
                seralizedPresets += preset.Name + ";" + preset.Address + "|";
            }

            localSettings.Values["presets"] = seralizedPresets;
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

            if (localSettings.Values.ContainsKey("presets"))
            {
                foreach (var preset in (localSettings.Values["presets"] as string).Split('|'))
                {
                    var presetPart = preset.Split(';');

                    if (presetPart.Length == 2)
                    {
                        radioPresets.Add(new Channel(presetPart[0], new Uri(presetPart[1])));
                    }
                    else
                    {
                        Debug.WriteLine("Invlaid Preset loaded from settings");
                    }
                }
            }
        }
    }
}
