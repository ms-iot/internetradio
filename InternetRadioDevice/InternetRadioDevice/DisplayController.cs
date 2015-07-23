using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Win10_LCD;
using Windows.System.Threading;

namespace InternetRadioDevice
{
    class DisplayController
    {
        private LCD lcd;
        private string currentLineOne;
        private string currentLineTwo;
        private string tempLineOne;
        private string tempLineTwo;
        private bool isUpdating;
        private bool tempMessageExecuting;
        private ThreadPoolTimer tempTimer;

        public async Task Initialize()
        {
            currentLineOne = string.Empty;
            currentLineTwo = string.Empty;
            tempLineOne = string.Empty;
            tempLineTwo = string.Empty;

            isUpdating = false;

            await initLCD();
        }

        public async Task TearDown()
        {
            currentLineOne = string.Empty;
            currentLineTwo = string.Empty;
            tempLineOne = string.Empty;
            tempLineTwo = string.Empty;

            await initLCD();
        }

        public async Task WriteMessageAsync(string lineOne, string lineTwo, uint timeout)
        {
            if (timeout == 0)
            {
                currentLineOne = lineOne;
                currentLineTwo = lineTwo;
                await writeMessageToLcd(currentLineOne, currentLineTwo);
            }
            else
            {
                if (tempTimer != null)
                {
                    tempTimer.Cancel();
                    tempTimer = null;
                }

                tempLineOne = lineOne;
                tempLineTwo = lineTwo;
                tempTimer = ThreadPoolTimer.CreateTimer(Timer_Tick, new TimeSpan(0, 0, (int)timeout));
                await writeMessageToLcd(tempLineOne, tempLineTwo);
            }
        }

        public async Task WriteAnimationAsync(List<KeyValuePair<string,string>> frames, uint framesPerSecond)
        {
            foreach (var frame in frames)
            {
                await WriteMessageAsync(frame.Key, frame.Value, 0);
                await Task.Delay(1000 / (int)framesPerSecond);
            }
        }

        private async void Timer_Tick(ThreadPoolTimer timer)
        {
            tempTimer = null;
            await writeMessageToLcd(currentLineOne, currentLineTwo);
        }

        private async Task initLCD()
        {
            lcd = new LCD(Config.Display.Width, Config.Display.Height);
            await lcd.InitAsync(Config.Display.RsPin, Config.Display.EnablePin, Config.Display.D4Pin, Config.Display.D5Pin, Config.Display.D6Pin, Config.Display.D7Pin);
            await lcd.clearAsync();
        }

        private async Task writeMessageToLcd(string lineOne, string lineTwo)
        {
            while (isUpdating) ;
            isUpdating = true;
            await lcd.clearAsync();
            lcd.setCursor(0, 0);
            lcd.write(lineOne);
            lcd.setCursor(0, 1);
            lcd.write(lineTwo);
            isUpdating = false;
        }
    }
}
