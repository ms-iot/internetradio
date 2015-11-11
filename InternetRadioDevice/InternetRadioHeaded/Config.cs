﻿using System;
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
                { 4, InputAction.Sleep },
                { 16, InputAction.NextChannel },
                { 27, InputAction.PreviousChannel },
                { 12, InputAction.VolumeDown },
                { 22, InputAction.VolumeUp }
            };
    }

}
