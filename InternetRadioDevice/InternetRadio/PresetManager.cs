using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InternetRadio
{
    internal struct PresetAddedEventArgs
    {
        public Channel Preset;
    }

    delegate void PresetAddedEventHandler(object sender, PresetAddedEventArgs e);

    class PresetManager
    {
        private List<Channel> radioPresets;
        int currentPreset;

        public event PresetAddedEventHandler PresetAdded;

        public PresetManager()
        {
            radioPresets = new List<Channel>();
            loadPresets();
        }

        public Channel CurrentPreset()
        {
            Channel? preset;

            tryGetPreset(currentPreset, out preset);

            return preset.Value;
        }

        public List<Channel> GetPresets()
        {
            return this.radioPresets;
        }

        public Channel NextPreset()
        {
            Channel? preset;

            if (currentPreset < radioPresets.Count() - 1)
            {
                currentPreset++;
            }
            else
            {
                currentPreset = 0;
            }

            savePresets();

            tryGetPreset(currentPreset, out preset);

            return preset.Value;
        }

        public Channel PreviousPreset()
        {
            Channel? preset;

            if (currentPreset != 0)
            {
                currentPreset--;
            }
            else
            {
                currentPreset = radioPresets.Count() - 1;
            }

            savePresets();

            if (!tryGetPreset(currentPreset, out preset))
                throw new Exception("");

            return preset.Value;
        }

        public bool AddPreset(Channel channel)
        {
            if (checkValidPreset(channel))
            {
                this.radioPresets.Add(channel);
                this.savePresets();
                var presetAddedArgs = new PresetAddedEventArgs()
                {
                    Preset = channel
                };

                this.PresetAdded(this, presetAddedArgs);
                return true;
            }
            return false;
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

        private bool tryGetPreset(int presetNumber, out Channel? channel)
        {
            channel = null;

            if (radioPresets.Count == 0)
            {
                return false;
            }

            channel = radioPresets[presetNumber];

            return true;
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

            localSettings.Values["radioPresets"] = seralizedPresets;
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

            if (localSettings.Values.ContainsKey("radioPresets"))
            {
                var presetData = localSettings.Values["radioPresets"].ToString().Split('|');
                foreach(var preset in presetData)
                {
                    var presetSplit = preset.Split(';');
                    if (presetSplit.Count() == 2)
                    {
                        Channel channel = new Channel();
                        channel.Name = presetSplit[0];
                        channel.Address = presetSplit[1];
                        if (checkValidPreset(channel))
                        {
                            this.radioPresets.Add(channel);
                            continue;
                        }
                    }

                    Debug.WriteLine("Invalid preset loaded");
                    StartupTask.WriteTelemetryEvent("App_InvalidPresetLoaded");
                }
            }
        }

        private bool checkValidPreset(Channel channel)
        {
            if (string.Empty == channel.Name)
            {
                Debug.WriteLine("Preset Name must not be an empty string");
                return false;
            }

            if (radioPresets.Any(c => c.Name == channel.Name))
            {
                Debug.WriteLine("Preset " + channel.Name + " already exists");
                return false;
            }

            Uri testUri;
            if (!Uri.TryCreate(channel.Address, UriKind.Absolute, out testUri))
            {
                Debug.WriteLine("Preset Address must be valid Uri");
                return false;
            }

            return true;
        }
        
    }
}
