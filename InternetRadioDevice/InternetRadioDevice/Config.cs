using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InternetRadioDevice
{
    namespace Config
    {
        public static class Messages
        {

            public static string StartupMessageLineOne = "Windows Powered";
            public static string StartupMessageLineTwo = "Internet Radio";
            public static int StartupMessageDelay = 3000;

            public static string ShutdownMessageLineOne = "Goodbye!";
            public static string ShutdownMessageLineTwo = "";
        }

        public static class Buttons
        {
            public static int ButtonDebounce = 1000;
            public static Dictionary<int, InputAction> ButtonPins = new Dictionary<int, InputAction>()
            {
                { 27, InputAction.Sleep },
                { 22, InputAction.NextChannel },
                { 16, InputAction.PreviousChannel },
                { 12, InputAction.VolumeDown },
                { 4, InputAction.VolumeUp }
            };
        }

        public static class Display
        {
            public static int Width = 16;
            public static int Height = 2;
            public static int RsPin = 18;
            public static int EnablePin = 23;
            public static int D4Pin = 24;
            public static int D5Pin = 5;
            public static int D6Pin = 6;
            public static int D7Pin = 13;
        }
    }
}
