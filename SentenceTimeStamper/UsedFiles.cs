using System.Xml.Serialization;

namespace SentenceTimeStamper
{
    public class UsedFiles
    {

        // 子要素の定義
        [XmlElement(ElementName = "voicefilepath")]
        public string VoiceFilePath { get; set; }   // 音声ファイルパス

        [XmlElement(ElementName = "textfilepath")]
        public string TextFilePath { get; set; }    // テキストファイルパス


        [XmlElement(ElementName = "title")]  // タイトル
        public string TextBox10Text { get; set; }
        
        [XmlElement(ElementName = "text")]  // 本文テキストエリア
        public string TextBox1Text { get; set; }

    }
}
