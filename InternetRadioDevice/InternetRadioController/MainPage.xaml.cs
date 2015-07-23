using System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace InternetRadioController
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await App.ConnectionHandler.Connect(this.addressTextBox.Text, this.portTextPox.Text);
                this.Frame.Navigate(typeof(RadioControls));
            }
            catch (Exception)
            {
                var dialog = new MessageDialog("Unable to connect to device");
                await dialog.ShowAsync();
            }
        }
    }
}
