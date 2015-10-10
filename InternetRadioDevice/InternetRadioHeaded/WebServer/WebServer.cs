﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Threading;

namespace InternetRadio
{
    /// <summary>
    /// HttpServer class that services the content for the Security System web interface
    /// </summary>
    public sealed class HttpServer : IDisposable
    {
        private const uint BufferSize = 8192;
        private int port = 8000;
        private readonly StreamSocketListener listener;
        private WebHelper helper;
        private RadioManager radioManager;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serverPort">Port to start server on</param>
        internal HttpServer(int serverPort, RadioManager radioManager)
        {
            this.radioManager = radioManager;
            helper = new WebHelper();
            listener = new StreamSocketListener();
            port = serverPort;
            listener.ConnectionReceived += (s, e) =>
            {
                try
                {
                    // Process incoming request
                    processRequestAsync(e.Socket);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception in StreamSocketListener.ConnectionReceived(): " + ex.Message);
                }
            };
        }

        public async void StartServer()
        {
            await helper.InitializeAsync();

#pragma warning disable CS4014
            listener.BindServiceNameAsync(port.ToString());
#pragma warning restore CS4014
        }

        public void Dispose()
        {
            listener.Dispose();
        }

        /// <summary>
        /// Process the incoming request
        /// </summary>
        /// <param name="socket"></param>
        private async void processRequestAsync(StreamSocket socket)
        {
            try
            {
                StringBuilder request = new StringBuilder();
                using (IInputStream input = socket.InputStream)
                {
                    // Convert the request bytes to a string that we understand
                    byte[] data = new byte[BufferSize];
                    IBuffer buffer = data.AsBuffer();
                    uint dataRead = BufferSize;
                    while (dataRead == BufferSize)
                    {
                        await input.ReadAsync(buffer, BufferSize, InputStreamOptions.Partial);
                        request.Append(Encoding.UTF8.GetString(data, 0, data.Length));
                        dataRead = buffer.Length;
                    }
                }

                using (IOutputStream output = socket.OutputStream)
                {
                    // Parse the request
                    string[] requestParts = request.ToString().Split('\n');
                    string requestMethod = requestParts[0];
                    string[] requestMethodParts = requestMethod.Split(' ');

                    // Process the request and write a response to send back to the browser
                    if (requestMethodParts[0] == "GET")
                        await writeResponseAsync(requestMethodParts[1], output, socket.Information);
                    else
                        throw new InvalidDataException("HTTP method not supported: "
                                                       + requestMethodParts[0]);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in processRequestAsync(): " + ex.Message);

                //This exception is thrown when someone clicks on a link while the current page is still loading. This isn't really an exception worth tracking as it will be thrown a lot, but doesn't affect anything.
                /*
                // Log telemetry event about this exception
                var events = new Dictionary<string, string> { { "WebServer", ex.Message } };
                App.Controller.TelemetryClient.TrackEvent("FailedToProcessRequestAsync", events);
                */
            }
        }

        private async Task writeResponseAsync(string request, IOutputStream os, StreamSocketInformation socketInfo)
        {
            try
            {
                string[] requestParts = request.Split('/');

                // Request for the root page, so redirect to home page
                if (request.Equals("/"))
                {
                    await redirectToPage(NavConstants.HOME_PAGE, os);
                }
                // Request for the home page
                else if (request.Contains(NavConstants.HOME_PAGE))
                {
                    // Generate the default config page
                    string html = await helper.GeneratePage(NavConstants.HOME_PAGE);
                    string onState = (this.radioManager.IsOn == PowerState.Powered) ? "true" : "false";
                    string stationList = @"[" +
                        "{ 'name':'station1' , 'uri':'http://somewhere.here1' }," +
                        "{ 'name':'station2' , 'uri':'http://somewhere.here2' }," +
                        "{ 'name':'station3' , 'uri':'http://somewhere.here3' }," +
                        " ]";


                    html = html.Replace("#onState#", onState);
                    html = html.Replace("#stationListJSON#", stationList);

                    await WebHelper.WriteToStream(html, os);

                }
                // Request for the settings page
                else if (request.Contains(NavConstants.SETTINGS_PAGE))
                {
                    if (!string.IsNullOrEmpty(request))
                    {
                        IDictionary<string, string> parameters = WebHelper.ParseGetParametersFromUrl(new Uri(string.Format("http://0.0.0.0/{0}", request)));

                        if (parameters.ContainsKey("powerState"))
                        {
                            switch (parameters["powerState"])
                            {
                                case "On":
                                    this.radioManager.IsOn = PowerState.Powered;
                                    break;
                            }
                        }
                    }
                    //handle UI interaction
                    await redirectToPage(NavConstants.HOME_PAGE, os);
                }
                // Request for a file that is in the Assets\Web folder (e.g. logo, css file)
                else
                {
                    using (Stream resp = os.AsStreamForWrite())
                    {
                        bool exists = true;
                        try
                        {
                            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;

                            // Map the requested path to Assets\Web folder
                            string filePath = @"Assets\Web" + request.Replace('/', '\\');

                            // Open the file and write it to the stream
                            using (Stream fs = await folder.OpenStreamForReadAsync(filePath))
                            {
                                string header = String.Format("HTTP/1.1 200 OK\r\n" +
                                                "Content-Length: {0}\r\n{1}" +
                                                "Connection: close\r\n\r\n",
                                                fs.Length,
                                                ((request.Contains("css")) ? "Content-Type: text/css\r\n" : ""));
                                byte[] headerArray = Encoding.UTF8.GetBytes(header);
                                await resp.WriteAsync(headerArray, 0, headerArray.Length);
                                await fs.CopyToAsync(resp);
                            }
                        }
                        catch (FileNotFoundException ex)
                        {
                            exists = false;

                            // Log telemetry event about this exception
                            var events = new Dictionary<string, string> { { "WebServer", ex.Message } };
                            StartupTask.WriteTelemetryEvent("FailedToOpenStream", events);
                        }

                        // Send 404 not found if can't find file
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in writeResponseAsync(): " + ex.Message);
                Debug.WriteLine(ex.StackTrace);

                // Log telemetry event about this exception
                var events = new Dictionary<string, string> { { "WebServer", ex.Message } };
                StartupTask.WriteTelemetryEvent("FailedToWriteResponse", events);

                try
                {
                    // Try to send an error page back if there was a problem servicing the request
                    string html = helper.GenerateErrorPage("There's been an error: " + ex.Message + "<br><br>" + ex.StackTrace);
                    await WebHelper.WriteToStream(html, os);
                }
                catch (Exception e)
                {
                    StartupTask.WriteTelemetryException(e);
                }
            }
        }

        /// <summary>
        /// Redirect to a page
        /// </summary>
        /// <param name="path">Relative path to page</param>
        /// <param name="os"></param>
        /// <returns></returns>
        private async Task redirectToPage(string path, IOutputStream os)
        {
            using (Stream resp = os.AsStreamForWrite())
            {
                byte[] headerArray = Encoding.UTF8.GetBytes(
                                  "HTTP/1.1 302 Found\r\n" +
                                  "Content-Length:0\r\n" +
                                  "Location: /" + path + "\r\n" +
                                  "Connection: close\r\n\r\n");
                await resp.WriteAsync(headerArray, 0, headerArray.Length);
                await resp.FlushAsync();
            }
        }
    }
}
