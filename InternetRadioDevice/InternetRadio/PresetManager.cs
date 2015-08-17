using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

            if (!tryGetPreset(currentPreset, out preset))
                throw new Exception("Unable to get preset");

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

            if (!tryGetPreset(currentPreset, out preset))
                throw new Exception("Unable to get preset");

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
                throw new Exception("Unable to get preset");

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
            if (0 == radioPresets.RemoveAll(x => x.Name == channelName))
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
            // Save the currently playing preset number to app storage
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["currentPreset"] = currentPreset;

            // Serialize the list of presets and save to app storage.
            // Presets are delimited by a '|' character and the name/address
            // values are delimited by a ';' character
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

            // Load the previously stored preset number or use the default '0' value
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

            // These characters are used as delimiters in serialization
            // and therefore are not valid characters for use in the name
            if (channel.Name.Contains(';') || channel.Name.Contains('|'))
            {
                Debug.WriteLine("Preset Name cannot contain ; or | characters");
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
