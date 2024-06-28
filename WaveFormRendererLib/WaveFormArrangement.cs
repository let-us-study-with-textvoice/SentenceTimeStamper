using System;
using System.Collections.Generic;
using System.Drawing;
using NAudio.Wave;

namespace WaveFormRendererLib
{
    public class WaveFormArrangement
    {
        private const int BLOCKTIMEms = 10;     // 音声ファイルをmSecごとに等間隔に区切るための定数
        private static long samples;            // 音声ファイルのサンプリングの数
        private int samplePerBlock;             // １Blockあたりのサンプリング数
        private float[] waveValuePerBlock;      // Blockの中のサンプリング値を代表値（peakProviderによって決まる値、平均値(AveragePeakProvider())、最大値(MaxPeakProvider())など）
        private int sampleRate;
        private int blockAlign;

        public float[] ArrangeWF(string selectedFile)
        {
            //Console.WriteLine(selectedFile);  // デバッグ用
            return ArrangeWF(selectedFile, new AveragePeakProvider(3));
        }

        public float[] ArrangeWF(string selectedFile, IPeakProvider peakProvider)
        {
            using (var reader = new AudioFileReader(selectedFile))
            {
                sampleRate = (reader.WaveFormat.SampleRate);
                samplePerBlock = sampleRate * BLOCKTIMEms / 1000;
                int bytesPerSample = (reader.WaveFormat.BitsPerSample / 8);
                samples = reader.Length / reader.WaveFormat.BlockAlign;
                waveValuePerBlock = new float[(samples / samplePerBlock) + 1];
                blockAlign = reader.WaveFormat.BlockAlign;
                //// デバッグ用
                //Console.WriteLine("BlockAlign=             {0}", reader.WaveFormat.BlockAlign);
                //Console.WriteLine("AverageBytesPerSecond = {0}", reader.WaveFormat.AverageBytesPerSecond);
                //Console.WriteLine("SampleRate              {0}", reader.WaveFormat.SampleRate);
                //Console.WriteLine("Channels                {0}", reader.WaveFormat.Channels);
                //Console.WriteLine("ExtraSize               {0}", reader.WaveFormat.ExtraSize);
                //Console.WriteLine("BitsPerSample           {0}", reader.WaveFormat.BitsPerSample);
                //Console.WriteLine("Length                  {0}", reader.Length);

                peakProvider.Init(reader, samplePerBlock * reader.WaveFormat.Channels);
                return ArrangeWF(peakProvider);
            }
        }

        private float[] ArrangeWF(IPeakProvider peakProvider)
        {
            long x = 0;
            long i = 0;
            var currentPeak = peakProvider.GetNextPeak();
            while (x < samples)
            {
                //Console.WriteLine("i={0}   x={1}  ", i, x);   //デバッグ用
                var nextPeak = peakProvider.GetNextPeak();
                waveValuePerBlock[i] = currentPeak.Max;
                i++;
                x = i * samplePerBlock;
                currentPeak = nextPeak;
            }
            return waveValuePerBlock;
        }

        // 波形を描画する
        public Image Render(int width, int height, double wScale)
        {
            // width:　要求側の基本的なビットマップの幅
            // height: 要求側の基本的なビットマップの高さ
            // hScale:　ｗ方向の拡大倍率

            Bitmap b = new Bitmap((int)(width * wScale), height);
            float sum, ave, curr;


            using (Graphics g = Graphics.FromImage(b))
            {
                g.FillRectangle(new SolidBrush(Color.Yellow), 0, 0, b.Width, b.Height);

                long j = (waveValuePerBlock.Length / b.Width) + 1; //ひとつのピクセルに入れるデータの数
                long j_;

                for (int x = 0; x < b.Width; x++)
                {
                    j_ = j;
                    sum = 0.0f;
                    curr = 0.0f;
                    for (long k = 0; k < j; k++)
                    {
                        if (x * j + k < waveValuePerBlock.Length)
                        {
                            //if (curr < waveValuePerBlock[x*j+k])
                            //    curr = waveValuePerBlock[x*j+k];
                            sum = sum + waveValuePerBlock[x * j + k];
                            //Console.WriteLine("k={0}   curr={1}  ", k, curr);
                        }
                        else
                        {
                            j_ = k - 1;
                            break;
                        }
                    }
                    ave = sum / j_;
                    //Console.WriteLine("i={0}  curr={1}  wfa[]={2}", x, curr, waveValuePerBlock.Length);

                    g.DrawLine(new Pen(Color.Blue, 1.0f), new Point(x, b.Height), new Point(x, (int)(b.Height * (1.0f - ave))));
                    x++;
                }
            }
            return b;
        }

        // センテンスの開始終了のポジションを探すメソッド
        public List<SentenceInfo> FindSectence(float thresholdLevel, int raiseUpHoldTime, int fallDownHoldTime)
        {
            //Console.WriteLine("****\nFindSentence\n******************");

            var list = new List<SentenceInfo>();

            if (raiseUpHoldTime < 10) raiseUpHoldTime = 10;
            int raiseUpHoldBlocks = raiseUpHoldTime / BLOCKTIMEms;
            int fallDownHoldBlocks = fallDownHoldTime / BLOCKTIMEms;

            int count = 0;

            for (long i = 0; i < waveValuePerBlock.Length; i++)
            {
                //Console.WriteLine("i={0}   wVPB={1}  count={2}", i, waveValuePerBlock[i], count);

                if (waveValuePerBlock[i] > thresholdLevel)
                {
                    count++;
                    //Console.WriteLine("count++={0}", count);
                }
                else
                {
                    count = 0;
                }

                if (count > raiseUpHoldBlocks)
                {
                    //Console.WriteLine("raiseUpのカウントが３を超えた ");
                    //Console.WriteLine("count={0}   raiseUpHoldBlocks={1}  ", count, raiseUpHoldBlocks);
                    // listに要素を追加
                    list.Add(new SentenceInfo((i - raiseUpHoldBlocks - 1) * BLOCKTIMEms * sampleRate /  1000, true, false));
                    count = 0;
                    i++;
                    while (count < fallDownHoldBlocks && i < waveValuePerBlock.Length)
                    {
                        //Console.WriteLine("i={0}   wVPB={1}  count={2}　＊＊＊＊＊", i, waveValuePerBlock[i], count);
                        if (waveValuePerBlock[i] < thresholdLevel)
                        {
                            count++;
                        }
                        else
                        {
                            count = 0;
                        }
                        i++;
                    }
                    //Console.WriteLine("fallDownのカウントが１０を超えた ");
                    //Console.WriteLine("count={0}   fallDownHoldBlocks={1}  ", count, fallDownHoldBlocks);
                    // listに要素を追加
                    list.Add(new SentenceInfo((i - fallDownHoldBlocks + 1) * BLOCKTIMEms * sampleRate / 1000, false, false));
                    count = 0;

                }
            }

            foreach (SentenceInfo x in list)
            {
                //Console.WriteLine("Position={0}  OnStart:{1}  OnManual:{2}", x.SamplingPosition, x.OnStart, x.OnManual);
            }
            //Console.WriteLine();

            return list;

        }

        public int BlockTimems
        {
            get { return BLOCKTIMEms; }
            private set { }
        }

    }
}
