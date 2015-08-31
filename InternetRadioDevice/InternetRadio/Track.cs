using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace InternetRadio
{
    internal class Track
    {
        public string Name;
        public string Address;

        public XElement Serialize()
        {
            var xml = new XElement("Track");

            xml.Add(new XElement("Name", this.Name));
            xml.Add(new XElement("Address", this.Address));

            return xml;
        }

        public static Track Deserialize(XElement xml)
        {
            Track track = null;

            var nameElement = xml.Descendants("Name").FirstOrDefault();
            var addressElement = xml.Descendants("Address").FirstOrDefault();

            if (null != nameElement &&
                null != addressElement)
            {
                track = new Track()
                {
                    Name = nameElement.Value,
                    Address = addressElement.Value
                };
            }
            else
            {
                Debug.WriteLine("Track: Invalid track XML");
            }

            return track;
        }
    }
}
