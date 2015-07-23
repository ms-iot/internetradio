
/*
    Code provided by : Radek Voltr
    As part of the LCD Hackster page: https://www.hackster.io/rvoltr/basic-lcd-16x2
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace Win10_LCD
{
    class LCD
    {
        private String cleanline = "";
        // commands
        private const int LCD_CLEARDISPLAY = 0x01;
        private const int LCD_RETURNHOME = 0x02;
        private const int LCD_ENTRYMODESET = 0x04;
        private const int LCDDisplayControl = 0x08;
        private const int LCD_CURSORSHIFT = 0x10;
        private const int LCD_FUNCTIONSET = 0x20;
        private const int LCD_SETCGRAMADDR = 0x40;
        private const int LCD_SETDDRAMADDR = 0x80;

        // flags for display entry mode
        private const int LCD_ENTRYRIGHT = 0x00;
        private const int LCD_ENTRYLEFT = 0x02;
        private const int LCD_ENTRYSHIFTINCREMENT = 0x01;
        private const int LCD_ENTRYSHIFTDECREMENT = 0x00;

        // flags for display on/off control
        private const int LCD_DISPLAYON = 0x04;
        private const int LCD_DISPLAYOFF = 0x00;
        private const int LCD_CURSORON = 0x02;
        private const int LCD_CURSOROFF = 0x00;
        private const int LCD_BLINKON = 0x01;
        private const int LCD_BLINKOFF = 0x00;

        // flags for display/cursor shift
        private const int LCD_DISPLAYMOVE = 0x08;
        private const int LCD_CURSORMOVE = 0x00;
        private const int LCD_MOVERIGHT = 0x04;
        private const int LCD_MOVELEFT = 0x00;

        // flags for function set
        private const int LCD_8BITMODE = 0x10;
        private const int LCD_4BITMODE = 0x00;
        private const int LCD_2LINE = 0x08;
        private const int LCD_1LINE = 0x00;
        public const int LCD_5x10DOTS = 0x04;
        public const int LCD_5x8DOTS = 0x00;

        private GpioController controller = GpioController.GetDefault();
        private GpioPin[] DPin = new GpioPin[8];
        private GpioPin RsPin = null;
        private GpioPin EnPin = null;

        private int DisplayFunction = 0;
        private int DisplayControl = 0;
        private int DisplayMode = 0;

        private int _cols = 0;
        private int _rows = 0;
        private int _currentrow = 0;

        private String[] buffer = null;

        public bool AutoScroll = false;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void delayMicroseconds(int uS)
        {
            if (uS > 2000)
                throw new Exception("Invalid param, use Task.Delay for 2ms and more");

            if (uS < 100) //call takes more time than 100uS 
                return;

            var tick_to_reach = System.DateTime.UtcNow.Ticks + uS * 1000; //1GHz Raspi2 Clock
            while (System.DateTime.UtcNow.Ticks < tick_to_reach)
                {
                }

        }

        public LCD(int cols, int rows, int charsize = LCD_5x8DOTS)
        {
            _cols = cols;
            _rows = rows;

            buffer = new String[rows];

            for (int i = 0; i < cols; i++)
            {
                cleanline = cleanline + " ";
            }

            DisplayFunction = charsize;
            if (_rows > 1)
                DisplayFunction = DisplayFunction | LCD_2LINE;
            else
                DisplayFunction = DisplayFunction | LCD_1LINE;
        }

        private async Task begin()
        {
            await Task.Delay(50);
            // Now we pull both RS and R/W low to begin commands
            RsPin.Write(GpioPinValue.Low);
            EnPin.Write(GpioPinValue.Low);

            //put the LCD into 4 bit or 8 bit mode
            if ((DisplayFunction & LCD_8BITMODE) != LCD_8BITMODE)
            {
                // we start in 8bit mode, try to set 4 bit mode
                write4bits(0x03);
                await Task.Delay(5); // wait min 4.1ms

                // second try
                write4bits(0x03);
                await Task.Delay(5); // wait min 4.1ms

                // third go!
                write4bits(0x03);
                delayMicroseconds(150);

                // finally, set to 4-bit interface
                write4bits(0x02);
            }
            else
            {
                // Send function set command sequence
                command((byte) (LCD_FUNCTIONSET | DisplayFunction));
                await Task.Delay(5); // wait min 4.1ms

                // second try
                command((byte)(LCD_FUNCTIONSET | DisplayFunction));
                delayMicroseconds(150);

                // third go
                command((byte)(LCD_FUNCTIONSET | DisplayFunction));
            }

            command((byte)(LCD_FUNCTIONSET | DisplayFunction));
            DisplayControl = LCD_DISPLAYON | LCD_CURSOROFF | LCD_BLINKOFF;
            DisplayOn();

            await clearAsync();

            DisplayMode = LCD_ENTRYLEFT | LCD_ENTRYSHIFTDECREMENT;
            command((byte)(LCD_ENTRYMODESET | DisplayMode));

        }

        public async Task<bool> InitAsync(int rs, int enable, int d4, int d5, int d6, int d7 )
        {
            try
            {
                DisplayFunction = DisplayFunction | LCD_4BITMODE;

                RsPin = controller.OpenPin(rs);
                RsPin.SetDriveMode(GpioPinDriveMode.Output);

                EnPin = controller.OpenPin(enable);
                EnPin.SetDriveMode(GpioPinDriveMode.Output);

                DPin[0] = controller.OpenPin(d4);
                DPin[0].SetDriveMode(GpioPinDriveMode.Output);

                DPin[1] = controller.OpenPin(d5);
                DPin[1].SetDriveMode(GpioPinDriveMode.Output);

                DPin[2] = controller.OpenPin(d6);
                DPin[2].SetDriveMode(GpioPinDriveMode.Output);

                DPin[3] = controller.OpenPin(d7);
                DPin[3].SetDriveMode(GpioPinDriveMode.Output);

                await begin();

                return true;

            }
            catch ( Exception)
            {
                return false;
            }

        }

        private void pulseEnable()
        {
            EnPin.Write(GpioPinValue.Low);
           // delayMicroseconds(1);
            EnPin.Write(GpioPinValue.High);
            //delayMicroseconds(1);
            EnPin.Write(GpioPinValue.Low);
            //delayMicroseconds(50);

        }

        private void write4bits(byte value)
        {
            //String s = "Value :"+value.ToString();
            for (int i = 0; i < 4; i++)
            {
                var val = (GpioPinValue)((value >> i) & 0x01);
                DPin[i].Write(val);
              //  s = DPin[i].PinNumber.ToString()+" /"+ i.ToString() + "=" + val.ToString() + "  " + s;
            }
            //Debug.WriteLine(s);

        pulseEnable();
        }


        private void write8bits(byte value)
        {
            for (int i = 0; i < 8; i++)
            {
                var val = (GpioPinValue)((value >> 1) & 0x01);
                DPin[i].Write(val);
            }

            pulseEnable();
        }

        private void send(byte value, GpioPinValue bit8mode)
        {
            //Debug.WriteLine("send :"+value.ToString());

            RsPin.Write(bit8mode);

            if ((DisplayFunction & LCD_8BITMODE) == LCD_8BITMODE)
            {
                write8bits(value);
            }
            else
            {
                byte B = (byte)((value >> 4) & 0x0F);
                write4bits(B);
                B = (byte)(value & 0x0F);
                write4bits(B);
            }
        }

        private void write(byte value)
        {
            send(value, GpioPinValue.High);
        }

        private void command(byte value)
        {
            send(value, GpioPinValue.Low);
        }

        public async Task clearAsync()
        {
            command(LCD_CLEARDISPLAY);
            await Task.Delay(2);

            for (int i = 0; i < _rows; i++)
            {
                buffer[i] = "";
            }

            _currentrow = 0;

            await homeAsync();
        }

        public async Task homeAsync()
        {
            command(LCD_RETURNHOME);
            await Task.Delay(2);
        }

        public void write(String text)
        {
            var data = Encoding.UTF8.GetBytes(text);

            foreach (byte Ch in data)
            {
                write(Ch);
            }
        }

        public void setCursor(byte col, byte row)
        {
            var row_offsets = new int[] { 0x00, 0x40, 0x14, 0x54 };

            /*if (row >= _numlines)
            {
                row = _numlines - 1;    // we count rows starting w/0
            }
            */

            command((byte)(LCD_SETDDRAMADDR | (col + row_offsets[row])));
        }

        // Turn the display on/off (quickly)
        public void DisplayOff()
        {
            DisplayControl &= ~LCD_DISPLAYON;
            command((byte)(LCDDisplayControl | DisplayControl));
        }
        public void DisplayOn()
        {
            DisplayControl |= LCD_DISPLAYON;
            command((byte)(LCDDisplayControl | DisplayControl));
        }

        // Turns the underline cursor on/off
        public void noCursor()
        {
            DisplayControl &= ~LCD_CURSORON;
            command((byte)(LCDDisplayControl | DisplayControl));
        }
        public void cursor()
        {
            DisplayControl |= LCD_CURSORON;
            command((byte)(LCDDisplayControl | DisplayControl));
        }

        // Turn on and off the blinking cursor
        public void noBlink()
        {
            DisplayControl &= ~LCD_BLINKON;
            command((byte)(LCDDisplayControl | DisplayControl));
        }
        public void blink()
        {
            DisplayControl |= LCD_BLINKON;
            command((byte)(LCDDisplayControl | DisplayControl));
        }

        // These commands scroll the display without changing the RAM
        public void scrollDisplayLeft()
        {
            command(LCD_CURSORSHIFT | LCD_DISPLAYMOVE | LCD_MOVELEFT);
        }
        public void scrollDisplayRight()
        {
            command(LCD_CURSORSHIFT | LCD_DISPLAYMOVE | LCD_MOVERIGHT);
        }

        // This is for text that flows Left to Right
        public void leftToRight()
        {
            DisplayMode |= LCD_ENTRYLEFT;
            command((byte)(LCD_ENTRYMODESET | DisplayMode));
        }

        // This is for text that flows Right to Left
        public void rightToLeft()
        {
            DisplayMode &= ~LCD_ENTRYLEFT;
            command((byte)(LCD_ENTRYMODESET | DisplayMode));
        }

        // This will 'right justify' text from the cursor
        public void autoscroll()
        {
            DisplayMode |= LCD_ENTRYSHIFTINCREMENT;
            command((byte)(LCD_ENTRYMODESET | DisplayMode));
        }

        // This will 'left justify' text from the cursor
        public void noAutoscroll()
        {
            DisplayMode &= ~LCD_ENTRYSHIFTINCREMENT;
            command((byte)(LCD_ENTRYMODESET | DisplayMode));
        }

        // Allows us to fill the first 8 CGRAM locations
        // with custom characters
        public void createChar(byte location, byte[] charmap)
        {
            location &= 0x7; // we only have 8 locations 0-7
            command((byte)(LCD_SETCGRAMADDR | (location << 3)));
            for (int i = 0; i < 8; i++)
            {
                write(charmap[i]);
            }
        }

        public void WriteLine(string Text)
        {
            if (_currentrow >= _rows)
            {
                //let's do shift
                for (int i = 1; i<_rows;i++)
                {
                    buffer[i - 1] = buffer[i];
                    setCursor(0, (byte)(i-1));
                    write(buffer[i - 1].Substring(0, _cols));
                }
                _currentrow = _rows-1;
            }
            buffer[_currentrow] = Text+cleanline;
            setCursor(0, (byte)_currentrow);
            var cuts = buffer[_currentrow].Substring(0, _cols);
            write(cuts);
            _currentrow++;
        }
     

    }
}
