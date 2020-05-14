using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SentenceTimeStamper
{
    public class DummySentenceInfoList
    {
        [XmlElement(ElementName = "sentenceinfo")]
        public List<DummySentenceInfo> DummySentenceInfo { get; set; } // 複数要素の場合リスト
    }
}
