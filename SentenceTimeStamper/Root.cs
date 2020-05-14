using System.Xml.Serialization;

namespace SentenceTimeStamper
{
    [XmlRoot(ElementName = "root")]
    public class Root
    {
        [XmlElement(ElementName = "usedfiles")]
        public UsedFiles UsedFiles { get; set; } // 複数要素の場合リスト

        [XmlElement(ElementName = "sentenceinfolist")]
        public DummySentenceInfoList DummySentenceInfoList { get; set; }
    }
}
