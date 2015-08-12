using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InternetRadio
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

        public Channel CurrentPreset()
        {
            return getPreset(currentPreset);
        }

        public List<Channel> GetPresets()
        {
            return this.radioPresets;
        }

        public Channel NextPreset()
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

        public Channel PreviousPreset()
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

        public void AddPreset(Channel channel)
        {
            radioPresets.Add(channel);
            savePresets();
        }

        public void DeletePreset(string channelName)
        {
            if (radioPresets.Any(channel => channel.Name == channelName))
            {
                radioPresets.RemoveAll(x => x.Name == channelName);
            }
            else
            {
                Debug.WriteLine("Channel: " + channelName + "does not exist and therefore cannot be deleted");
            }
        }

        private Channel getPreset(int presetNumber)
        {
            if (radioPresets.Count == 0)
            {
                var channel = new Channel() { Address = "", Name = "" };
                return channel;
            }

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
                        radioPresets.Add(new Channel() { Name = presetPart[0], Address = presetPart[1] });
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
