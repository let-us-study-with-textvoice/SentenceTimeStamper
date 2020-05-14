using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SentenceTimeStamper
{
    public class DummySentenceInfo
    {
        [XmlElement(ElementName = "samplingposition")]
        public long SamplingPosition { get; set; }


        [XmlElement(ElementName = "onstart")]
        public bool OnStart { get; set; }


        [XmlElement(ElementName = "onmanual")]
        public bool OnManual { get; set; }

        [XmlElement(ElementName = "sentencetext")]
        public string SentenceText { get; set; }
    }
}
