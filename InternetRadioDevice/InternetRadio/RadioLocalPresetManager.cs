using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using Windows.Storage;

namespace InternetRadio
{
    class RadioLocalPresetManager : IPlaylistManager
    {
        private Playlist playlist;
        int currentPreset;

        public RadioLocalPresetManager()
        {
            currentPreset = 0;
        }

        public Playlist CurrentPlaylist
        {
            get
            {
                return playlist;
            }
            internal set
            {
                this.playlist = value;
                PlaylistChanged(this, new PlaylistChangedEventArgs() { Playlist = this.playlist });
            }
        }

        public Track CurrentTrack
        {
            get
            {
                if (this.playlist != null && 0 < this.playlist.Tracks.Count)
                {
                    return this.playlist.Tracks[currentPreset];
                }
                else
                {
                    return null;
                }
            }
        }

        public event PlaylistChangedEventHandler PlaylistChanged;
        public event PlaylistCurrentTrackChangedEventHandler CurrentTrackChanged;

        public async Task LoadPlayList(Guid playlistId)
        {
            await saveCurrentPlaylistToFile();

            var playlist = await loadPlaylistFromFile(playlistId);
            if (null != playlist)
            {
                this.CurrentPlaylist = playlist;
                this.CurrentPlaylist.Tracks.CollectionChanged += Tracks_CollectionChanged;
            }
            else
            {
                Debug.WriteLine("RadioLocalPresetManager: Playlist "+ playlistId.ToString() + " was not avaliable");
            }
        }

        private async void Tracks_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if ((e.OldStartingIndex == -1 && e.NewStartingIndex == 0) ||
                (e.OldStartingIndex == 0 && e.NewStartingIndex == -1))
            {
                this.CurrentTrackChanged(this, new PlaylistCurrentTrackChangedEventArgs() { CurrentTrack = this.CurrentTrack });
            }

            await this.saveCurrentPlaylistToFile();
        }

        public async Task<Guid> StartNewPlaylist(string name, List<Track> tracks, bool persist)
        {
            await this.saveCurrentPlaylistToFile();
            var playlistId = Guid.NewGuid();
            var newPlaylist = new Playlist(name, playlistId);
            newPlaylist.Tracks = new ObservableCollection<Track>(tracks);

            this.CurrentPlaylist = newPlaylist;
            this.CurrentPlaylist.Tracks.CollectionChanged += Tracks_CollectionChanged;

            return newPlaylist.Id;
        }

        public bool PlayTrack(string trackName)
        {
            var track = this.playlist.Tracks.FirstOrDefault(t => t.Name == trackName);
            if (track != null)
            {
                this.currentPreset = this.playlist.Tracks.IndexOf(track);
                this.CurrentTrackChanged(this, new PlaylistCurrentTrackChangedEventArgs() { CurrentTrack = this.CurrentTrack });
            }
            return false;
        }

        public Track NextTrack()
        {
            if (this.CurrentPlaylist.Tracks.Count -1 > this.currentPreset)
            {
                this.currentPreset++;
                this.CurrentTrackChanged(this, new PlaylistCurrentTrackChangedEventArgs() { CurrentTrack = this.CurrentTrack });
            }
            else if (this.currentPreset == this.CurrentPlaylist.Tracks.Count -1)
            {
                this.currentPreset = 0;
                this.CurrentTrackChanged(this, new PlaylistCurrentTrackChangedEventArgs() { CurrentTrack = this.CurrentTrack });
            }

            return this.CurrentTrack;
        }

        public Track PreviousTrack()
        {
            if (this.currentPreset > 0)
            {
                this.currentPreset--;
                this.CurrentTrackChanged(this, new PlaylistCurrentTrackChangedEventArgs() { CurrentTrack = this.CurrentTrack });
            }
            else if (this.currentPreset == 0 && this.CurrentPlaylist.Tracks.Count > 0)
            {
                this.currentPreset = this.CurrentPlaylist.Tracks.Count - 1;
                this.CurrentTrackChanged(this, new PlaylistCurrentTrackChangedEventArgs() { CurrentTrack = this.CurrentTrack });
            }

            return this.CurrentTrack;
        }

        private async Task saveCurrentPlaylistToFile()
        {
            if (null == this.CurrentPlaylist)
            {
                Debug.WriteLine("Cannot save null playlist");
                return;
            }

            var playlistXml = this.CurrentPlaylist.Serialize();

            var fileName = this.CurrentPlaylist.Id.ToString() + ".playlist";
            var folder = Windows.Storage.ApplicationData.Current.LocalFolder;
            var playlistFile = await folder.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(playlistFile, playlistXml.ToString());
        }

        private async Task<Playlist> loadPlaylistFromFile(Guid playlistId)
        {
            Playlist playlist = null;
            var fileName = playlistId.ToString() + ".playlist";

            var folder = Windows.Storage.ApplicationData.Current.LocalFolder;
            try
            {
                var playlistFile = await folder.GetFileAsync(fileName);
                
                var xmlString = await FileIO.ReadTextAsync(playlistFile);
                var xml = XElement.Parse(xmlString);
                playlist = Playlist.Deserialize(xml);

            }
            catch(FileNotFoundException)
            {
                Debug.WriteLine("RadioLocalPresetManager: Playlist file not found - " + fileName);
            }
            catch (System.Xml.XmlException)
            {
                Debug.WriteLine("RadioLocalPresetManager: Playlist file not in correct format - " + fileName);
            }

            return playlist;
        }
    }
}
