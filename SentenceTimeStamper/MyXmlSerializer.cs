using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System;
using System.Windows.Forms;

namespace SentenceTimeStamper
{
    class MyXmlSerializer
    {
        // ファイルに書き出すときに使う
        public static void Serialize<T>(string savePath, T graph)
        {

            try
            {
                using (var sw = new StreamWriter(savePath, false, Encoding.UTF8))
                {
                    var ns = new XmlSerializerNamespaces();
                    ns.Add(string.Empty, string.Empty);

                    new XmlSerializer(typeof(T)).Serialize(sw, graph, ns);
                }

            }
            catch (ArgumentNullException err)
            {
                MessageBox.Show(err.GetType().Name + ":" + err.Message);
            }
            catch (ArgumentException err)
            {
                MessageBox.Show(err.GetType().Name + ":" + err.Message);
            }
            catch (IOException err)
            {
                MessageBox.Show(err.GetType().Name + ":\r\n" + err.Message);
            }

        }

        // ファイルを読み取るときに使う
        public static T Deserialize<T>(string loadPath)
        {
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.Load(loadPath);


            //using (var sr = new StreamReader(loadPath))
            //{
            //    return (T)new XmlSerializer(typeof(T)).Deserialize(sr);
            //}

            //https://cathval.com/csharp/4263
            //c# 逆XMLシリアル化時に改行を維持する(XmlSerializer で逆シリアル化された時に、改行コードが CR+LF (\r+\n) から LF (\n) に修正されるのが原因
            using (XmlNodeReader reader = new XmlNodeReader(doc.DocumentElement))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                return (T)serializer.Deserialize(reader);
            }

        }

    }
}
