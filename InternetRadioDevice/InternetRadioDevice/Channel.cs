using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InternetRadioDevice
{
    class Channel
    {
        public Channel(string name, Uri address)
        {
            Name = name;
            Address = address;
        }

        public string Name;
        public Uri Address;
    }
}
