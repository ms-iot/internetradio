using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Callant;
using Windows.System.Threading;
using System.Diagnostics;

namespace InternetRadio
{
    class DisplayController
    {
        private CharacterLCD lcd;
        private string currentLineOne;
        private string currentLineTwo;
        private string tempLineOne;
        private string tempLineTwo;
        private bool isUpdating;
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
            try
            {
                this.lcd = new CharacterLCD(Config.Display_RsPin,
                                            Config.Display_EnablePin, 
                                            Config.Display_D4Pin, 
                                            Config.Display_D5Pin, 
                                            Config.Display_D6Pin, 
                                            Config.Display_D7Pin);
            }
            catch(NullReferenceException)
            {
                Debug.WriteLine("Unable to initialize LCD");
            }
        }

        private async Task writeMessageToLcd(string lineOne, string lineTwo)
        {
            if (!checkInitialized())
                return;

            while (isUpdating) ;
            isUpdating = true;
            await this.lcd.WriteLCD(lineOne+"\n"+lineTwo);
            isUpdating = false;
        }

        private bool checkInitialized()
        {
            if (lcd == null)
            {
                Debug.WriteLine("LCD is not initialized");
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
