﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.Streams;

namespace InternetRadio
{
    public sealed class WebHelper
    {
        //template for all pages
        private Dictionary<string, string> htmlTemplates = new Dictionary<string, string>();
        private StorageFolder InstallFolder;

        private Dictionary<string, string> links = new Dictionary<string, string>
            {
                {"Home", "/" + NavConstants.HOME_PAGE },
                {"Settings", "/" + NavConstants.SETTINGS_PAGE },
            };

        /// <summary>
        /// Initializes the WebHelper with the default.htm template
        /// </summary>
        /// <returns></returns>
        /// 
        public async Task InitializeAsync()
        {
            this.InstallFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            await LoadHTMLTemplate(NavConstants.DEFAULT_PAGE);
        }

        /// <summary>
        /// Generates the html for the navigation bar
        /// </summary>
        /// <returns></returns>
        private string createNavBar()
        {
            // Create html for the side bar navigation using the links Dictionary
            string html = "<p>Navigation</p><ul>";
            foreach (string key in links.Keys)
            {
                html += "<li><a href='" + links[key] + "'>" + key + "</a></li>";
            }
            html += "</ul>";
            return html;
        }
        public IAsyncOperation<string> GeneratePage(string page)
        {
            return GeneratePageHelper(page).AsAsyncOperation<string>();
        }

        private async Task<string> GeneratePageHelper(string page)
        {
            if (!this.htmlTemplates.ContainsKey(page))
            {
                await LoadHTMLTemplate(page);
            }
            return this.htmlTemplates[page];
        }

        private async Task LoadHTMLTemplate(string page)
        {
            string htmlBody = "";
            string htmlHead = "";

            var filePath = string.Format("{0}{1}", NavConstants.ASSETSWEB, page);
            var file = (IStorageFile)await this.InstallFolder.TryGetItemAsync(filePath);
            if (file != null)
            {
                htmlBody = await FileIO.ReadTextAsync(file);
                this.htmlTemplates.Add(page, htmlBody);
            }
            else
            {
                string pageName = Path.GetFileNameWithoutExtension(page);

                filePath = string.Format("{0}{1}.{2}", NavConstants.ASSETSWEB, pageName, "body");
                file = (IStorageFile)await this.InstallFolder.TryGetItemAsync(filePath);
                if (file != null)
                {

                    htmlBody = await FileIO.ReadTextAsync(file);
                }
                filePath = string.Format("{0}{1}.{2}", NavConstants.ASSETSWEB, pageName, "head");
                file = (IStorageFile)await this.InstallFolder.TryGetItemAsync(filePath);
                if (file != null)
                {
                    htmlHead = await FileIO.ReadTextAsync(file);
                }

                this.htmlTemplates.Add(page, GeneratePage("Internet Radio", pageName, htmlBody, htmlHead));
            }
        }

        /// <summary>
        /// Helper function to generate page
        /// </summary>
        /// <param name="title">Title that appears on the window</param>
        /// <param name="titleBar">Title that appears on the header bar of the page</param>
        /// <param name="content">Content for the body of the page</param>
        /// <returns></returns>
        public string GenerateErrorPage(string errorMessage)
        {
            return GeneratePage("Error", "Error", errorMessage, "");
        }

        /// <summary>
        /// Helper function to generate page
        /// </summary>
        /// <param name="title">Title that appears on the window</param>
        /// <param name="titleBar">Title that appears on the header bar of the page</param>
        /// <param name="content">Content for the body of the page</param>
        /// <param name="message">A status message that will appear above the content</param>
        /// <returns></returns>
        public string GeneratePage(string title, string titleBar, string content, string head = "", string message = "")
        {
            string html = this.htmlTemplates[NavConstants.DEFAULT_PAGE];
            html = html.Replace("#content#", content);
            html = html.Replace("#title#", title);
            html = html.Replace("#titleBar#", titleBar);
            html = html.Replace("#navBar#", createNavBar());
            html = html.Replace("#message#", message);
            html = html.Replace("#head#", head);

            return html;
        }

        /// <summary>
        /// Parses the GET parameters from the URL and returns the parameters and values in a Dictionary
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static IDictionary<string, string> ParseGetParametersFromUrl(Uri uri)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(uri.Query))
            {
                var decoder = new WwwFormUrlDecoder(uri.Query);
                foreach (WwwFormUrlDecoderEntry entry in decoder)
                {
                    parameters.Add(entry.Name, entry.Value);
                }
            }
            return parameters;
        }

        ///// <summary>
        ///// Parses the GET parameters from the URL and loads them into the settings
        ///// </summary>
        ///// <param name="uri"></param>
        //public void ParseUriIntoSettings(Uri uri)
        //{
        //    var decoder = new WwwFormUrlDecoder(uri.Query);

        //    // Take the parameters from the URL and put it into Settings
        //    foreach (WwwFormUrlDecoderEntry entry in decoder)
        //    {
        //        try
        //        {
        //            var field = typeof(AppSettings).GetField(entry.Name);
        //            if (field.FieldType == typeof(int))
        //            {
        //                field.SetValue(App.Controller.XmlSettings, Convert.ToInt32(entry.Value));
        //            }
        //            else if (field.FieldType == typeof(CameraType) ||
        //                    field.FieldType == typeof(StorageProvider))
        //            {
        //                field.SetValue(App.Controller.XmlSettings, Enum.Parse(field.FieldType, entry.Value));
        //            }
        //            else
        //            {
        //                //if the field being saved is the alias, and the alias has changed, send a telemetry event
        //                if (0 == field.Name.CompareTo("MicrosoftAlias") &&
        //                   0 != entry.Value.CompareTo(App.Controller.XmlSettings.MicrosoftAlias))
        //                {
        //                    Dictionary<string, string> properties = new Dictionary<string, string> { { "Alias", entry.Value } };
        //                    App.Controller.TelemetryClient.TrackEvent("Alias Changed", properties);
        //                }
        //                field.SetValue(App.Controller.XmlSettings, entry.Value);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.WriteLine(ex.Message);

        //            // Log telemetry event about this exception
        //            var events = new Dictionary<string, string> { { "WebHelper", ex.Message } };
        //            StartupTask.WriteTelemetryEvent("FailedToParseUriIntoSettings", events);
        //        }
        //    }
        //}

        /// <summary>
        /// Writes html data to the stream
        /// </summary>
        /// <param name="data"></param>
        /// <param name="os"></param>
        /// <returns></returns>
        /// 
        public static IAsyncAction WriteToStream(string data, IOutputStream os)
        {
            return WriteToStreamHelper(data, os).AsAsyncAction();
        }

        private static async Task WriteToStreamHelper(string data, IOutputStream os)
        {
            using (Stream resp = os.AsStreamForWrite())
            {
                // Look in the Data subdirectory of the app package
                byte[] bodyArray = Encoding.UTF8.GetBytes(data);
                MemoryStream stream = new MemoryStream(bodyArray);
                string header = String.Format("HTTP/1.1 200 OK\r\n" +
                                  "Content-Length: {0}\r\n" +
                                  "Connection: close\r\n\r\n",
                                  stream.Length);
                byte[] headerArray = Encoding.UTF8.GetBytes(header);
                await resp.WriteAsync(headerArray, 0, headerArray.Length);
                await stream.CopyToAsync(resp);
                await resp.FlushAsync();
            }
        }

        /// <summary>
        /// Writes a file to the stream
        /// </summary>
        /// <param name="file"></param>
        /// <param name="os"></param>
        /// <returns></returns>
        /// 
        public IAsyncAction WriteFileToStream(StorageFile file, IOutputStream os)
        {
            return WriteFileToStreamHelper(file, os).AsAsyncAction();
        }

        private static async Task WriteFileToStreamHelper(StorageFile file, IOutputStream os)
        {
            using (Stream resp = os.AsStreamForWrite())
            {
                bool exists = true;
                try
                {
                    using (Stream fs = await file.OpenStreamForReadAsync())
                    {
                        string header = String.Format("HTTP/1.1 200 OK\r\n" +
                                        "Content-Length: {0}\r\n" +
                                        "Connection: close\r\n\r\n",
                                        fs.Length);
                        byte[] headerArray = Encoding.UTF8.GetBytes(header);
                        await resp.WriteAsync(headerArray, 0, headerArray.Length);
                        await fs.CopyToAsync(resp);
                    }
                }
                catch (FileNotFoundException ex)
                {
                    exists = false;

                    // Log telemetry event about this exception
                    var events = new Dictionary<string, string> { { "WebHelper", ex.Message } };
                    StartupTask.WriteTelemetryEvent("FailedToWriteFileToStream", events);
                }

                if (!exists)
                {
                    byte[] headerArray = Encoding.UTF8.GetBytes(
                                          "HTTP/1.1 404 Not Found\r\n" +
                                          "Content-Length:0\r\n" +
                                          "Connection: close\r\n\r\n");
                    await resp.WriteAsync(headerArray, 0, headerArray.Length);
                }

                await resp.FlushAsync();
            }
        }

        /// <summary>
        /// Makes a html hyperlink
        /// </summary>
        /// <param name="text">Hyperlink text</param>
        /// <param name="url">Hyperlink URL</param>
        /// <param name="newWindow">Should the link open in a new window</param>
        /// <returns></returns>
        public static string MakeHyperlink(string text, string url, bool newWindow)
        {
            return "<a href='" + url + "' " + ((newWindow) ? "target='_blank'" : "") + ">" + text + "</a>";
        }
        
    }
}
