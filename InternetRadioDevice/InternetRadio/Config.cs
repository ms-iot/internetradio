using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InternetRadio
{
    struct Config
    {
        public const int Messages_StartupMessageDelay = 3000;

        public const int Buttons_Debounce = 1000;
        public static Dictionary<int, InputAction> Buttons_Pins = new Dictionary<int, InputAction>()
            {
                { 27, InputAction.Sleep },
                { 22, InputAction.NextChannel },
                { 16, InputAction.PreviousChannel },
                { 12, InputAction.VolumeDown },
                { 4, InputAction.VolumeUp }
            };
    }

}
