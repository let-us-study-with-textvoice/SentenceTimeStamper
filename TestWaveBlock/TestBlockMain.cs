using WaveFormRendererLib2;
using System;
using System.Drawing;

namespace TestWaveBlock
{
    class TestBlockMain
    {
        static void Main(string[] args)
        {
            // WaveFormArrangementクラスのオブジェクトを作る
            WaveFormArrangement waveFormArrangement = new WaveFormArrangement();

            // 初期設定
            float[] wfa;    // 音声ファイルをBlockTemems（１０ｍSec)に区切った値を入れる変数配列

            string selectedFile = @"C:\Users\takakuwa\Music\test_sound\burst_0sec95.mp3";
            //Console.WriteLine(selectedFile);

            // WaveFormArrangementクラスのオブジェクトに音声ファイルを入れて、10mSecごとに区切った配列を作る
            wfa = waveFormArrangement.ArrangeWF(selectedFile);

            // デバッグ用のコード
            //for (long i = 0; i < wfa.Length; i++)
                //Console.WriteLine("i={0}   t={1}  Amp={2} ", i, i / 0.1f, wfa[i]);
            //Console.WriteLine(waveFormArrangement.BlockTimems);


            // ビットマップに描画する
            Bitmap bitmap = (Bitmap)waveFormArrangement.Render(640, 400, 1.0);

            // 音声波形を描画したビットマップをイメージファイルに保存する
            bitmap.Save(@"C:\materials\picture\mypicture5.jpg");

            // ビットマップファイルを廃棄する
            bitmap.Dispose();

            //
            waveFormArrangement.FindSectence(0.02f, 30, 100);

        }
    }
}
