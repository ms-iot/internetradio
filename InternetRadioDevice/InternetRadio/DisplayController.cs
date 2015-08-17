using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Callant;
using Windows.System.Threading;
using System.Diagnostics;
using InternetRadio.AsyncHelpers;

namespace InternetRadio
{
    class DisplayController
    {
        private CharacterLCD lcd;
        private readonly AsyncSemaphore lcdLock = new AsyncSemaphore(1);

        private string currentLineOne;
        private string currentLineTwo;
        private string tempLineOne;
        private string tempLineTwo;
        private ThreadPoolTimer tempTimer;

        public void Initialize()
        {
            currentLineOne = string.Empty;
            currentLineTwo = string.Empty;
            tempLineOne = string.Empty;
            tempLineTwo = string.Empty;

            initLCD();
        }

        public async Task Dispose()
        {
            if (tempTimer != null)
            {
                tempTimer.Cancel();
                tempTimer = null;
            }

            if (null != lcd)
            {
                await lcd.ClearLCD();
                lcd.Dispose();
            }
        }

        public async Task WriteMessageAsync(string lineOne, string lineTwo, uint timeout)
        {
            if (!checkInitialized())
                return;

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
            if (!checkInitialized())
                return;

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

        private void initLCD()
        {
            try
            {
                lcd = new CharacterLCD(Config.Display_RsPin,
                                            Config.Display_EnablePin, 
                                            Config.Display_D4Pin, 
                                            Config.Display_D5Pin, 
                                            Config.Display_D6Pin, 
                                            Config.Display_D7Pin);
            }
            catch(NullReferenceException)
            {
                Debug.WriteLine("Unable to initialize LCD");
                lcd = null;
            }
        }

        private async Task writeMessageToLcd(string lineOne, string lineTwo)
        {
            await lcdLock.WaitAsync();

            try
            { 
                await lcd.WriteLCD(lineOne + "\n" + lineTwo);
            }
            finally
            {
                lcdLock.Release();
            }
        }

        private bool checkInitialized()
        {
            if (null == lcd)
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
