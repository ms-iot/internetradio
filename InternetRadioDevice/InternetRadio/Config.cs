using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InternetRadio
{
    struct Config
    {
        public const int Api_Port = 3011;

        public const int Messages_StartupMessageDelay = 3000;

        public const string Messages_StartupMessageLineOne = "Windows Powered";
        public const string Messages_StartupMessageLineTwo = "Internet Radio";

        public const string Messages_ShutdownMessageLineOne = "Goodbye!";
        public const string Messages_ShutdownMessageLineTwo = "";

        public const int Buttons_Debounce = 1000;
        public static Dictionary<int, InputAction> Buttons_Pins = new Dictionary<int, InputAction>()
            {
                { 27, InputAction.Sleep },
                { 22, InputAction.NextChannel },
                { 16, InputAction.PreviousChannel },
                { 12, InputAction.VolumeDown },
                { 4, InputAction.VolumeUp }
            };

        public static int Display_Width = 16;
        public static int Display_Height = 2;
        public static int Display_RsPin = 18;
        public static int Display_EnablePin = 23;
        public static int Display_D4Pin = 24;
        public static int Display_D5Pin = 5;
        public static int Display_D6Pin = 6;
        public static int Display_D7Pin = 13;
    }

}
