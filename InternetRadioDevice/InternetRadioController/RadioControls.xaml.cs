using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Diagnostics;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace InternetRadioController
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RadioControls : Page
    {
        public RadioControls()
        {
            this.InitializeComponent();
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
             App.ConnectionHandler.SendMessage("Nex");
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            App.ConnectionHandler.SendMessage("Pre");
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            App.ConnectionHandler.SendMessage("Vup");
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            App.ConnectionHandler.SendMessage("Vdn");
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            App.ConnectionHandler.SendMessage("Add" + PresetName.Text + ";" + PresetAddress.Text);
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            App.ConnectionHandler.SendMessage("Pwr");
        }
    }
}
