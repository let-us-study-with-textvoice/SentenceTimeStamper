/*
 * The Sentence Time Stamper designed by guijiu
 * 
 * Original Code
 * https://qiita.com/siy1121/items/dd06a5e700dcf9543af7
 * 
 * NAudio components
 * https://github.com/naudio
 * https://github.com/naudio/NAudio.WaveFormRenderer
 */

using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using NAudio.WaveFormRenderer;


namespace SentenceTimeStamper
{
    public partial class Form1 : Form
    {
        private WaveOutEvent outputDevice;
        private AudioFileReader audioFile;
        private WaveFormArrangement waveFormArrangement = new WaveFormArrangement();

        private ContextMenuStrip contextMenuStripOnArrow;       // 矢印のコンテキストメニュー
        private ContextMenuStrip contextMenuStripOnPanel2;      // panel2上でのコンテキスメニュー（矢印の追加）
        private string voiceFilePath;   //音声ファイルのパス
        private string voiceFileBaseName;   //音声ファイルのファイル名（拡張子を除く）
        private string voiceFileDirectory;  //音声ファイルのファイルディレクトリ
        private int bytePerSec; //一秒あたりのバイト数
        private int length;     //曲の長さ（秒）
        private int position;   //再生位置（秒）
        private bool mouseDownFlag; //ドラッグ時に使うフラグ　MouseDown中にtrue

        private const double INITIALRENDERINGSCALE = 7.6;//波形イメージのスケール（初期値7.6）
        private double renderingScale = INITIALRENDERINGSCALE;
        private const float AMPLITUDESCALE = 3.0f;  // 固定値
        private float magnification = 1.0f;     // 波形の振幅の倍率
        private int timeRise; //波形の立ち上がり時間
        private int timeFall;　//波形の立下り時間
        private long currentPosition = 0; // 現在の再生位置を記録


        public Form1()
        {
            InitializeComponent();

            // コンソールの立ち上げ方は、次のサイトの（その２）の方法による
            //https://www.wareko.jp/blog/output-text-string-to-console-window-with-windows-form-application-in-c-sharp
            //（プロジェクトーー＞プロパティーーー＞アプリケーションーー＞出力の種類(U)ーー＞コンソールアプリケーション

            //イベントの設定　共通にしたいので手動で
            pictureBox1.MouseDown += PictureBox_MouseDown;
            pictureBox2.MouseDown += PictureBox_MouseDown;
            pictureBox1.MouseMove += PictureBox_MouseMove;
            pictureBox2.MouseMove += PictureBox_MouseMove;
            pictureBox1.MouseUp += PictureBox_MouseUp;
            pictureBox2.MouseUp += PictureBox_MouseUp;

            //黒い太い波形の設定
            soundCloudDarkSticks.Width = pictureBox1.Height;//生成する画像の幅
            soundCloudDarkSticks.TopHeight = pictureBox1.Width;// 上に伸びるバーの高さ
            soundCloudDarkSticks.BottomHeight = 0; //下に伸びるバーの長さ
            soundCloudDarkSticks.BackgroundColor = Color.Transparent;//生成される画像の背景色　今回は透明
            soundCloudDarkSticks.PixelsPerPeak = 1;//バーの幅
            soundCloudDarkSticks.SpacerPixels = 1; //バーの間に挟まる細いバーの幅

            //オレンジの太い波形の設定
            soundCloudOrangeSticks.Width = pictureBox1.Height;
            soundCloudOrangeSticks.TopHeight = pictureBox1.Width;
            soundCloudOrangeSticks.BottomHeight = 0;
            soundCloudOrangeSticks.BackgroundColor = Color.Transparent;
            soundCloudOrangeSticks.PixelsPerPeak = 1;

            // 矢印の右クリック　コンテキストメニュー
            contextMenuStripOnArrow = new ContextMenuStrip();
            ToolStripMenuItem tsmiDeleteArrow = new ToolStripMenuItem("削除(&D)");   // コンテキストメニューで表示される項目
            tsmiDeleteArrow.Click += new EventHandler(tsmiDeleteOnArrow_Click);             // コンテキストメニューの中で「削除」を選択した時のデリゲート
            contextMenuStripOnArrow.Items.Add(tsmiDeleteArrow);                      // コンテキストメニューにtsmiDelete(削除)を追加する

            // panel2上で右クリック　コンテキストメニュー
            ToolStripMenuItem tsmiAddArrow = new ToolStripMenuItem("追加(&A)");   // コンテキストメニューに表示される項目
            tsmiAddArrow.Click += tsmiAddOnPanel2_Click; // コンテキストメニューの中で「追加」を選択した時のデリゲート

            contextMenuStripOnPanel2 = new ContextMenuStrip();
            contextMenuStripOnPanel2.Items.Add(tsmiAddArrow);// コンテキストメニューにtsmiAddArrow(追加)を加える
            contextMenuStripOnPanel2.Opening += new CancelEventHandler(csmOnPanel2_Opening);
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            // 縮小拡大波形表示部分、基準はpanel1
            pictureBox1.Location = new Point(0, 0);
            pictureBox1.Size = new Size(120, panel1.Height - 2);

            pictureBox2.Location = new Point(0, 0);
            pictureBox2.Size = pictureBox1.Size;

            redLinePictureBox4.Size = new Size(1, pictureBox1.Height);
            redLinePosition();

            panel2.Location = new Point(pictureBox1.Width + 1, 0);
            panel2.Size = new Size(45, pictureBox1.Height);
            panel2.ContextMenuStrip = contextMenuStripOnPanel2;


            // 全体波形表示部分、基準はpanel1
            panel3.Size = new Size(60, panel1.Height);

            pictureBox10.Location = new Point(0, 0);
            pictureBox10.Size = new Size(panel3.Width - 2, panel3.Height - 2);

            pictureBox11.Location = pictureBox10.Location;
            pictureBox11.Size = pictureBox10.Size;
        }

        //バーの色（黒）の設定
        private SoundCloudStickWaveFormSettings soundCloudDarkSticks
            = new SoundCloudStickWaveFormSettings
                                (
                                    Color.FromArgb(52, 52, 52),
                                    Color.FromArgb(55, 55, 55),
                                    Color.FromArgb(154, 154, 154),
                                    Color.FromArgb(204, 204, 204)
                                );

        //バーの色（オレンジ）の設定
        private SoundCloudStickWaveFormSettings soundCloudOrangeSticks
             = new SoundCloudStickWaveFormSettings
                                 (
                                    Color.FromArgb(255, 76, 0),
                                    Color.FromArgb(255, 52, 2),
                                    Color.FromArgb(255, 171, 141),
                                    Color.FromArgb(255, 213, 199)
                                 );

        private WaveFormRenderer renderer = new WaveFormRenderer(); //波形レンダラの生成
        private AveragePeakProvider averagePeakProvider = new AveragePeakProvider(AMPLITUDESCALE); //波形レンダラ内部で使用されるもの

        // 基本パラメータの初期化
        private void initialParameter()
        {
            renderingScale = INITIALRENDERINGSCALE;   // 波形の拡大率
            sentenceNumber = 0;     // センテンス番号

            // ２回目以降音声ファイルを読み込んだ時のpictureBoxの初期設定
            // pictureBox1.Width(=800)の高さをpanel1(=802)の高さから２を引いた値に合わせる
            pictureBox1.Height = panel1.Height - 2;

            // pictureBox1,2の位置と大きさをそろえる
            pictureBox2.Size = pictureBox1.Size;
            redLinePictureBox4.Size = new Size(1, pictureBox1.Height);

            // listArrowを初期化
            listArrow.Clear();
            panel2.Controls.Clear();

        }

        // 音声フィアルを開く
        private void openVoiceFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 基本パラメータの初期化
            initialParameter();


            // フォルダーMYMusicにある音声ファイルを開く
            openFileDialog.InitialDirectory
                = System.Environment.GetFolderPath(Environment.SpecialFolder.MyMusic); //openFileDialog1がイニシャルでMyMusicを開くようにデフォルトで設定
            openFileDialog.Filter
                = "Voice_file(*.mp3;*.wav)|*.mp3;*.wav|MPEG_Layer-3_file(*.mp3)|*.mp3|wave_file(*.wav" + ")|*.wav"; //音声ファイルのフィルタ

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {

                clearAudioDevice(); // 使用中のオーディオデバイスなどを破棄する

                voiceFilePath = openFileDialog.FileName;
                setVoiceFile();     // Ｖｏｉｃｅファイルをセットする 

                makeSentence();     // センテンスを生成する

                redLinePictureBox4.Size = new Size(1, pictureBox1.Height);
            }


            label12.Text = Math.Round(renderingScale, 1, MidpointRounding.AwayFromZero).ToString();
            label8.Text = Math.Round(magnification, 1, MidpointRounding.AwayFromZero).ToString();

        }


        // 使用中のオーディオデバイスなどを破棄する
        private void clearAudioDevice()
        {
            if (audioFile != null)//再生中に音声ファイルを選択できてしまい、多重再生できてしまうための対策。再生中に新しいファイルを選んだときは、再生を停止し、デバイスとファイルをdisposeする。（初期設定）
            {
                outputDevice.Stop();
                outputDevice.Dispose();
                outputDevice = null;
                audioFile.Dispose();
                audioFile = null;
            }

        }

        // Ｖｏｉｃｅファイルをセットする
        private void setVoiceFile()
        {
            voiceFileBaseName = Path.GetFileNameWithoutExtension(voiceFilePath);
            voiceFileDirectory = Path.GetDirectoryName(voiceFilePath);

            // Label1のファイル名の表示の処理：ファイル名が長すぎるときに、label1の表示部分が赤くなるのを防ぐ処理(拡張子を含むFileNameが２４文字以上の時、ファイル名のベース部分を切り出し、１０文字目に~~記号を挿入し、ベース部分の後ろから１０を加え、置き換える
            string fileNameLabel1 = Path.GetFileName(voiceFilePath);
            if (fileNameLabel1.Length > 24)
            {
                fileNameLabel1 = fileNameLabel1.Substring(0, 10)
                    + "~~" + fileNameLabel1.Substring(fileNameLabel1.IndexOf(".") - 10, 10)
                    + Path.GetExtension(voiceFilePath);
            }
            label1.Text = fileNameLabel1;

            //レンダリングした画像をPictureBoxに設定
            renderingSoundFile();
            pictureBox2.Height = 0;

            currentPosition = 0;

            outputDevice = new WaveOutEvent();
            audioFile = new AudioFileReader(voiceFilePath);
            audioFile.Position = currentPosition;
            outputDevice.Init(audioFile);
            playButton.Image = Properties.Resources.play;

            //必要な値を求める
            bytePerSec = audioFile.WaveFormat.BitsPerSample / 8 * audioFile.WaveFormat.SampleRate * audioFile.WaveFormat.Channels;
            length = (int)audioFile.Length / bytePerSec;



        }

        private string textFilePath;//音声ファイルのパス

        // テキストファイルを開く
        private void openTextFileToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            openTextFileDialog.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); // MyDocumentを開くようにデフォルトで設定

            openTextFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";

            if (openTextFileDialog.ShowDialog() == DialogResult.OK)
            {
                textFilePath = openTextFileDialog.FileName;
                string textFileName = Path.GetFileName(textFilePath);
                if (textFileName.Length > 24)
                {
                    textFileName = textFileName.Substring(0, 10)
                        + "~~" + textFileName.Substring(textFileName.IndexOf(".") - 10, 10)
                        + Path.GetExtension(voiceFilePath);
                }
                label11.Text = textFileName;

                //Read the contents of the file into a stream
                var fileStream = openTextFileDialog.OpenFile();

                using (StreamReader reader = new StreamReader(fileStream))
                {
                    textBox1.Text = reader.ReadToEnd();
                }
            }
        }


        private void renderingSoundFile()
        {
            //Console.WriteLine("renderingScale={0}", renderingScale);
            soundCloudDarkSticks.Width = (int)((panel1.Height - 2) * renderingScale); //生成する画像の幅
            soundCloudOrangeSticks.Width = (int)((panel1.Height - 2) * renderingScale); //生成する画像の幅

            Image i1 = null;
            Image i2 = null;

            using (var voiceStream = new AudioFileReader(voiceFilePath))
            {
                i1 = renderer.Render(voiceStream, averagePeakProvider, soundCloudDarkSticks);
            }

            using (var voiceStream = new AudioFileReader(voiceFilePath))
            {
                i2 = renderer.Render(voiceStream, averagePeakProvider, soundCloudOrangeSticks);
            }

            i1.RotateFlip(RotateFlipType.Rotate90FlipX);
            i2.RotateFlip(RotateFlipType.Rotate90FlipX);

            pictureBox1.Image = i1;
            pictureBox2.Image = i2;

            pictureBox2.Height = (int)(((double)position / length) * pictureBox1.Height);
        }


        // 再生ボタンを押したときの処理
        private void playButton_Click(object sender, EventArgs e)
        {
            // 選曲なしでプレーボタンを押したときの処理
            if (audioFile != null) playState();
        }


        // 再生の状態により、再生開始・一時停止をトグルに切り替える。
        private void playState()
        {
            //Console.WriteLine("PlayState={0}   position={1}", outputDevice.PlaybackState, audioFile.Position);
            // 選曲後の処理
            switch (outputDevice.PlaybackState)
            {
                case PlaybackState.Stopped://ファイルが読み込まれてまだ一度も再生されていない場合

                    label3.Text = new TimeSpan(0, 0, length).ToString(); //音源の長さ（時間）を表示

                    if (currentPosition >= audioFile.Length) currentPosition = 0;
                    // 現在の位置を代入
                    audioFile.Position = currentPosition;

                    timer1.Start();
                    //Console.WriteLine("AutoScroleReset");

                    if (!mouseDownFlag)
                    {
                        panel1.AutoScrollPosition = new Point(0, 0);
                        pictureBox2.Height = 0;
                    }

                    outputDevice.Init(audioFile);
                    outputDevice.Play();
                    playButton.Image = SentenceTimeStamper.Properties.Resources.pause;
                    break;

                case PlaybackState.Paused://一時停止時の場合
                    outputDevice.Dispose();
                    audioFile.Position = currentPosition;
                    outputDevice.Init(audioFile);

                    timer1.Start();

                    outputDevice.Play();

                    playButton.Image = SentenceTimeStamper.Properties.Resources.pause;
                    break;

                case PlaybackState.Playing://再生中の場合
                    outputDevice.Pause();
                    currentPosition = audioFile.Position;
                    playButton.Image = SentenceTimeStamper.Properties.Resources.play;
                    break;
            }
        }

        // Timerでの処理
        private void timer1_Tick(object sender, EventArgs e)
        {
            //再生位置（秒）を計算して表示
            currentPosition = audioFile.Position;

            position = (int)currentPosition / bytePerSec;
            label2.Text = new TimeSpan(0, 0, position).ToString();

            if (!mouseDownFlag)//ドラッグ時に幅を変更するとチカチカするのを防止
                //再生位置からオレンジ波形をすすめる
                pictureBox2.Height = (int)((double)currentPosition / audioFile.Length * pictureBox1.Height);
            //Console.WriteLine((double)currentPosition / audioFile.Length * pictureBox1.Height);
            //Console.WriteLine("pictureBox2.Height={0}   currentPosition={1}", pictureBox2.Height, currentPosition);

            //再生位置が終了の位置になったときに、停止状態にする
            if (outputDevice.PlaybackState == PlaybackState.Stopped)
            {
                outputDevice.Stop();
                playButton.Image = SentenceTimeStamper.Properties.Resources.play;
                audioFile.Position = 0;
                timer1.Stop();
            }

            // Pauseの時
            if (outputDevice.PlaybackState == PlaybackState.Paused)
            {
                playButton.Image = SentenceTimeStamper.Properties.Resources.play;
            }

            // オレンジ色の波形がpanel1の右側からはみ出した時に、左にずらす
            if (pictureBox2.Height + panel1.AutoScrollPosition.Y > panel1.Height * 0.9)
            {
                panel1.AutoScrollPosition = new Point(0, pictureBox2.Height - (int)(panel1.Height * 0.9));
            }

            // オレンジ色の波形がpanel1の左からはみ出した時に、右にずらす
            if (pictureBox2.Height + panel1.AutoScrollPosition.Y < panel1.Height * 0.1)
            {
                panel1.AutoScrollPosition = new Point(0, pictureBox2.Height - (int)(panel1.Height * 0.1));
            }
        }


        // ドラッグしてシークする処理
        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)  // 左クリックをした時
            {
                if (audioFile == null) return; //audioFileが空のとき、pictureBoxをクリックするとPictureBox_MouseUp()で誤動作するため、その防止

                mouseDownFlag = true;//ドラッグ時のフラグをtrueに
                pictureBox2.Height = e.Y;
            }
        }
        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)  // 左クリックをした時
            {
                if (mouseDownFlag && e.Y <= pictureBox1.Height) pictureBox2.Height = e.Y;//ドラッグ中にオレンジの波形の幅を変更
            }
        }

        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)  // 左クリックをした時
            {
                if (!mouseDownFlag) return;
                if (audioFile == null) return;

                // マウスの位置がpictureBox1の範囲外のとき、その中に入れ込む
                int y = e.Y;
                if (y > pictureBox1.Height) y = pictureBox1.Height - 1;
                else if (y < 0) y = 0;

                //ドラッグが終了した場所から曲の再生位置を計算して設定
                outputDevice.Pause();
                outputDevice.Dispose();

                currentPosition = (int)((double)(y) / pictureBox1.Height * audioFile.Length);
                audioFile.Position = currentPosition;
                outputDevice.Init(audioFile);

                // 再生していない状態（Stopped or Paused）で、PictureBoxの上でクリックしたときに再生を開始
                if (outputDevice.PlaybackState == PlaybackState.Stopped
                    || outputDevice.PlaybackState == PlaybackState.Paused)
                    playState();

                outputDevice.Play();

                mouseDownFlag = false;

                timer1.Start();
            }
        }


        // メニュー・fileからcloseを選択したとき
        private void closeXToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();        // フォームを閉じる処理


        }

        // フォームを閉じる処理

        // このソフトウエアについて
        private void aboutAToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Sentence Time Stamper Ver. 0.07\n" +
                "\n" +
                "Special Thanks\n" +
                "NAudio and NAudio_WaveFormRendererLib\n" +
                "siy1121@Qitta\n" +
                "\n" +
                "Original Souce Code:" +
                "https://qiita.com/siy1121/items/dd06a5e700dcf9543af7 \n" +
                "\n" +
                "Modified by guijiu\n" +
                "https://github.com/lets-study-with-textvoice" +
                "\n" +
                "\n" +
                "12.May.2020 guijiu",
                "About this application");
        }


        //　ライセンス
        private void lisenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.Show();

        }

        //　免責
        private void escapeClauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form3 form3 = new Form3();
            form3.Show();

        }


        // 波形イメージを時間軸方向に拡大
        private void timeExpoionButton_Click(object sender, EventArgs e)
        {
            // 選曲ありのときのみイメージを拡大する。
            if (audioFile != null)
            {
                renderingScale = renderingScale * 1.5;
                renderingSoundFile();
                label12.Text = Math.Round(renderingScale, 1, MidpointRounding.AwayFromZero).ToString();

                //panel2.Size = new Size(45, pictureBox1.Height);
                CalculateArrowPosition();
                redLinePictureBox4.Height = pictureBox1.Height;

            }
        }

        private void CalculateArrowPosition()
        {
            panel2.Size = new Size(45, pictureBox1.Height);

            foreach (SentenceInfoWithPicBox x in listArrow)
            {
                SentenceInfoWithPicBox arrow;
                arrow = x;
                arrow.Location = new Point(0, (int)(panel2.Height * x.SamplingPosition * audioFile.WaveFormat.BlockAlign / audioFile.Length) - ArrowCenterY);
            }
        }

        //// 波形イメージを時間軸方向に縮小
        private void timeReductionButton_Click(object sender, EventArgs e)
        {
            if (audioFile != null)
            {
                renderingScale = renderingScale / 1.5;
                if (renderingScale < 1.0) renderingScale = 1.0;
                renderingSoundFile();
                label12.Text = Math.Round(renderingScale, 1, MidpointRounding.AwayFromZero).ToString();

                //panel2.Size = new Size(45, pictureBox1.Height);

                CalculateArrowPosition();
                redLinePictureBox4.Size = new Size(1, pictureBox1.Height);


            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            redLinePosition();

            //Console.WriteLine("trackBar1.Value={0}", trackBar1.Value);
        }

        // 
        private void redLinePosition()
        {
            redLinePictureBox4.Location = new Point((int)((1.0 - (double)trackBar1.Value / (trackBar1.Maximum - trackBar1.Minimum)) * pictureBox1.Width), panel1.AutoScrollPosition.Y);
        }

        // 波形振幅拡大
        private void pictureBox6_Click(object sender, EventArgs e)
        {
            if (audioFile != null)
            {
                magnification = magnification * 1.5f;
                averagePeakProvider = new AveragePeakProvider(AMPLITUDESCALE * magnification); //波形レンダラ内部で使用されるもの
                renderingSoundFile();
                label8.Text = Math.Round(magnification, 1, MidpointRounding.AwayFromZero).ToString();
            }
        }

        // 波形振幅縮小
        private void pictureBox7_Click(object sender, EventArgs e)
        {
            if (audioFile != null)
            {
                magnification = magnification / 1.5f;
                averagePeakProvider = new AveragePeakProvider(AMPLITUDESCALE * magnification); //波形レンダラ内部で使用されるもの
                renderingSoundFile();
                label8.Text = Math.Round(magnification, 1, MidpointRounding.AwayFromZero).ToString();
            }
        }


        public List<SentenceInfoWithPicBox> listArrow = new List<SentenceInfoWithPicBox>();

        // FindSentenceボタンの処理
        private void pictureBox5_Click(object sender, EventArgs e)
        {
            makeSentence();
        }

        private void makeSentence()
        {
            //listArrow = null;
            listArrow.Clear();
            panel2.Controls.Clear();


            if (audioFile != null)
            {
                using (var wavestream = new AudioFileReader(voiceFilePath))
                {
                    waveFormArrangement.ArrangeWF(wavestream);
                }
                

                timeRise = int.Parse(rizeTimeTextBox.Text);
                timeFall = int.Parse(fallTimeTextBox.Text);


                // 音声波形からセンテンスを区切ったリストを生成し、（　waveFormArrangement.FindSectence()　）
                // SentenceInfoのリストからSentenceInfoWithPicBoxのリストlistArrowに追加
                foreach ( SentenceInfo sentInfo in waveFormArrangement.FindSectence((float)trackBar1.Value / (trackBar1.Maximum - trackBar1.Minimum) / magnification, timeRise, timeFall))
                {
                    // SentenceInfoWithPicBoxのリストをlistArrowに加える
                    listArrow.Add(new SentenceInfoWithPicBox(sentInfo));
                }

                //listArrow.AddRange(waveFormArrangement.FindSectence((float)trackBar1.Value / (trackBar1.Maximum - trackBar1.Minimum) / magnification, timeRise, timeFall));

                MakeArrow();    // 矢印を作って、panel2に登録する
                CalculateArrowPosition();

                showText();     // センテンスを表示する

            }
        }

        // 矢印を作って、panel2に登録する
        private void MakeArrow()
        {
            foreach (SentenceInfoWithPicBox x in listArrow)
            {
                SentenceInfoWithPicBox arrow;
                arrow = x;
                ToArrowGiveEventHandler(arrow);

                arrow.Parent = panel2;
            }
        }

        // 矢印にイベントハンドラを与える
        private void ToArrowGiveEventHandler(SentenceInfoWithPicBox arrow)
        {
            arrow.DoubleClick += arrow_DoubleClick;     // 重なったarrowの順番を変える
            arrow.MouseEnter += arrow_MouseEnter;       // 矢印が示す指示値を表示する
            arrow.MouseLeave += arrow_MouseLeave;       // 矢印が示す指示値を隠す
            arrow.MouseDown += arrow_MouseDown;         // 矢印の位置を変えるためにドラッグする
            arrow.MouseMove += arrow_MouseMove;         // 矢印の位置を変える
            arrow.MouseUp += arrow_MouseUp;             // 矢印の位置を定める


            arrow.ContextMenuStrip = contextMenuStripOnArrow;   // 矢印にコンテキストメニューを追加
        }


        // 隠れている矢印を前面に移すためのイベントハンドラ
        private void arrow_DoubleClick(object sender, EventArgs e)
        {
            this.ActiveControl = null;
            panel2.Controls.Remove((SentenceInfoWithPicBox)sender);
            panel2.Controls.Add((SentenceInfoWithPicBox)sender);
        }


        // マウスポインタが矢印の上に入った時に、その矢印が前面になるようにするイベントハンドラ
        private void arrow_MouseEnter(object sender, EventArgs e)
        {
            panel2.Controls.SetChildIndex((SentenceInfoWithPicBox)sender, 0);    // マウスが矢印の中に入ったら、その矢印を最前面に移動する。
            //Console.WriteLine("listArrow.IndexOf((SentenceInfoWithPicBox)sender)={0}", listArrow.IndexOf((SentenceInfoWithPicBox)sender));
        }



        private void arrow_MouseLeave(object sender, EventArgs e)
        {

        }



        private int slipOffset;     // ドラッグしたときにマウスポインタと矢印の中心線がずれていることによっておこる矢印のスリップを補正ためのオフセット
        private const int ArrowWidth = 44;              // 矢印の幅
        private const int ArrowHeight = 21;             // 矢印の高さ
        //private int ArrowCenterX = ArrowWidth / 2;      // 矢印の幅(X軸)の中心の値
        private int ArrowCenterY = ArrowHeight / 2;     // 矢印の幅(Y軸)の中心の値
        bool isDragPictureBox = false;                  // 矢印がドラッグされているかを判定する変数

        //
        // 矢印をドラッグするメソッド群　arrow_MouseDown()から_MouseUp()まで
        private void arrow_MouseDown(Object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)  // 左クリックをした時
            {
                Point p = new Point();
                p.Y = e.Y;
                p = ((SentenceInfoWithPicBox)sender).PointToScreen(p);
                p = panel2.PointToClient(p);
                slipOffset = p.Y - (((SentenceInfoWithPicBox)sender).Location).Y;// ドラッグしたときに矢印がスリップすることを補正ためのオフセット、arrow_MouseMove()で補正
                isDragPictureBox = true;
            }
        }

        //
        // マウスの移動に伴い、矢印を移動する
        private void arrow_MouseMove(Object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)  // 左クリックをした時
            {
                Point p = new Point();

                if (isDragPictureBox)
                {
                    p.Y = e.Y - slipOffset;     // slipOffsetはドラッグしたときに矢印がスリップすることを補正
                    p.X = 0;                    // 矢印の位置を矢印配置パネル(panel2)のtopに固定する。

                    p = ((SentenceInfoWithPicBox)sender).PointToScreen(p);
                    p = panel2.PointToClient(p);

                    // ひとつ上の矢印を飛び越さないようにする
                    if (listArrow.IndexOf((SentenceInfoWithPicBox)sender) < listArrow.Count - 1
                        && p.Y >= listArrow[listArrow.IndexOf((SentenceInfoWithPicBox)sender) + 1].Location.Y)
                    {
                        p.Y = listArrow[listArrow.IndexOf((SentenceInfoWithPicBox)sender) + 1].Location.Y - 3;
                    }

                    // 一つ下の矢印を飛び越さないようにする
                    if (listArrow.IndexOf((SentenceInfoWithPicBox)sender) > 0
                        && p.Y <= listArrow[listArrow.IndexOf((SentenceInfoWithPicBox)sender) - 1].Location.Y)
                    {
                        p.Y = listArrow[listArrow.IndexOf((SentenceInfoWithPicBox)sender) - 1].Location.Y + 3;
                    }

                    // 矢印の先端がpanel2の外に出ないようにする。
                    if (p.Y < 0 - ArrowCenterY) p.Y = 0 - ArrowCenterY;
                    if (p.Y > panel2.Height - ArrowCenterY) p.Y = panel2.Height - ArrowCenterY - 3;

                    ((SentenceInfoWithPicBox)sender).Location = p;
                }
            }
        }

        //
        // 矢印をドロップする
        private void arrow_MouseUp(Object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)  // 左クリックをした時
            {
                isDragPictureBox = false;

                long y = LocationToSamplingPosition(((SentenceInfoWithPicBox)sender).Location);

                ((SentenceInfoWithPicBox)sender).SamplingPosition = y;

                showPlaySentenceTime(); // 再生するセンテンスの開始・終了時刻の表示
            }
        }


        // 立ち上がり時間の入力用textBox5の入力文字の制限（半角数字とバックスペースのみ）
        private void textBox5_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((e.KeyChar < '0' || '9' < e.KeyChar) && e.KeyChar != '\b')
            {
                //押されたキーが 0～9でない場合は、イベントをキャンセルする
                e.Handled = true;
            }
        }

        // 立下り時間の入力用textBox6の入力文字の制限（半角数字とバックスペースのみ）
        private void textBox6_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((e.KeyChar < '0' || '9' < e.KeyChar) && e.KeyChar != '\b')
            {
                //押されたキーが 0～9でない場合は、イベントをキャンセルする
                e.Handled = true;
            }
        }



        private int sentenceNumber = 0;     // センテンスの通し番号


        // ひとつ前のセンテンスへ
        private void playBeforeSentencePictureBox_Click(object sender, EventArgs e)
        {
            if (audioFile == null) return;
            outputDevice.Pause();

            sentenceNumber = sentenceNumber - 2;
            if (sentenceNumber < 0) sentenceNumber = 0;
            audioFile.Position = listArrow[sentenceNumber].SamplingPosition * audioFile.WaveFormat.BlockAlign;
            timer1.Start();

            showText();

            playThisSentence();

        }



        // 一つ後のセンテンスへ
        private void playNextSentencePictureBox_Click(object sender, EventArgs e)
        {

            if (audioFile == null) return;
            outputDevice.Pause();

            sentenceNumber = sentenceNumber + 2;
            if (sentenceNumber > listArrow.Count - 2) sentenceNumber = listArrow.Count - 2;
            audioFile.Position = listArrow[sentenceNumber].SamplingPosition * audioFile.WaveFormat.BlockAlign;
            timer1.Start();

            showText();     // テキストのセンテンスを表示する

            playThisSentence();     // このセンテンスを再生する
        }

        // テキストのセンテンスを表示する
        private void showText()
        {
            // 本文を表示
            if (sentenceNumber > 0)
            {
                textBox2.Text = listArrow[sentenceNumber - 2].SentenceText;
            }
            else textBox2.Text = "";

            textBox3.Text = listArrow[sentenceNumber].SentenceText;

            if (sentenceNumber < listArrow.Count - 2)
            {
                textBox4.Text = listArrow[sentenceNumber + 2].SentenceText;
            }
            else textBox4.Text = "";

            if (sentenceNumber < listArrow.Count - 4)
            {
                textBox5.Text = listArrow[sentenceNumber + 4].SentenceText;
            }
            else textBox5.Text = "";

            if (sentenceNumber < listArrow.Count - 6)
            {
                textBox6.Text = listArrow[sentenceNumber + 6].SentenceText;
            }
            else textBox6.Text = "";

            if (sentenceNumber < listArrow.Count - 8)
            {
                textBox7.Text = listArrow[sentenceNumber + 8].SentenceText;
            }
            else textBox7.Text = "";

            showPlaySentenceTime(); // 再生するセンテンスの開始・終了時刻の表示

        }

        // 再生するセンテンスの開始・終了時刻の表示
        private void showPlaySentenceTime()
        {
            label4.Text = samplingPositionToTime(listArrow[sentenceNumber].SamplingPosition);
            label5.Text = samplingPositionToTime(listArrow[sentenceNumber + 1].SamplingPosition);

        }


        private string samplingPositionToTime(long samplingPosition)
        {
            double t0 = (double)samplingPosition * audioFile.WaveFormat.BlockAlign / bytePerSec;

            int t1 = (int)(t0 * 100);
            int t2 = (int)t0 * 100;

            int milisec = t1 - t2;

            int ss = (int)t0;

            int min = ss / 60;

            int sec = ss - min * 60;

            string time = min.ToString("00") + ":" + sec.ToString("00") + "." + milisec.ToString("00");

            return time;

        }

        // このセンテンスを再生する
        private void playThisSentencePictureBox_Click(object sender, EventArgs e)
        {
            playThisSentence(); // このセンテンスを再生する
        }


        // センテンスを再生の本体
        private void playThisSentence()
        {
            if (audioFile == null) return;
            if (listArrow.Count == 0) return;

            outputDevice.Pause();
            outputDevice.Dispose();

            audioFile.Position = listArrow[sentenceNumber].SamplingPosition * audioFile.WaveFormat.BlockAlign; ;

            OffsetSampleProvider offsetSample = new OffsetSampleProvider(audioFile);
            offsetSample.TakeSamples = ((int)listArrow[sentenceNumber + 1].SamplingPosition - (int)listArrow[sentenceNumber].SamplingPosition) * audioFile.WaveFormat.Channels;

            outputDevice.Init(offsetSample);

            outputDevice.Play();

            timer1.Start();

            playButton.Image = SentenceTimeStamper.Properties.Resources.pause;
        }


        // センテンスの内容を変更した時の処理
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (audioFile != null && sentenceNumber > 2)
            {
                listArrow[sentenceNumber - 2].SentenceText = textBox2.Text;
            }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            if (audioFile != null)
            {
                listArrow[sentenceNumber].SentenceText = textBox3.Text;
            }
        }


        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            //Console.WriteLine("textBox4_TextChanged  sentenceNumber={0}", sentenceNumber);

            if (audioFile != null && sentenceNumber < listArrow.Count - 2)
            {
                listArrow[sentenceNumber + 2].SentenceText = textBox4.Text;
            }
        }


        private Point mousePositionInSouceControl;
        private Point arrowPoint;

        private void csmOnPanel2_Opening(object sender, CancelEventArgs e)
        {
            //Console.WriteLine("現在csmOnPanel2_Opening");

            ContextMenuStrip menuOnPanel2 = (ContextMenuStrip)sender;

            mousePositionInSouceControl = MousePosition;

            Point mousePositionInPanel2;

            mousePositionInPanel2 = menuOnPanel2.SourceControl.PointToClient(mousePositionInSouceControl);

            arrowPoint = mousePositionInPanel2;
            arrowPoint.X = 0;
            arrowPoint.Y = mousePositionInPanel2.Y - ArrowCenterY;

            //Console.WriteLine("mousePosition.Y={0}   mousePositionInPanel2.Y={1}   arrowPoin.Y={2}", mousePositionInSouceControl.Y, mousePositionInPanel2.Y, arrowPoint.Y);

            if (audioFile == null) e.Cancel = true;     // もしaudioFileがなければ、このイベントをキャンセルする。
        }


        // 
        private void tsmiAddOnPanel2_Click(object sender, EventArgs e)
        {
            if (audioFile == null) return;

            SentenceInfoWithPicBox arrow1 = new SentenceInfoWithPicBox(0, false, true);     // Arrow
            SentenceInfoWithPicBox arrow2 = new SentenceInfoWithPicBox(0, false, true);     // arrow
            ToArrowGiveEventHandler(arrow1);
            ToArrowGiveEventHandler(arrow2);

            arrow1.Location = arrowPoint;
            //Console.WriteLine("arrowPoint.Y={0}", arrow1.Location.Y);

            arrow1.SamplingPosition = LocationToSamplingPosition(arrow1.Location);

            // arrow1がlistArrow[]のどの位置に当たるかを検索する
            for (int i = 0; i < listArrow.Count; i++)
            {

                if (listArrow[i].Location.Y > arrowPoint.Y)
                {
                    arrow1.OnStart = listArrow[i].OnStart;
                    arrow2.OnStart = !listArrow[i].OnStart;

                    arrow2.Location = new Point(0, (arrow1.Location.Y + listArrow[i].Location.Y) / 2);
                    arrow2.SamplingPosition = (arrow1.SamplingPosition + listArrow[i].SamplingPosition) / 2;

                    arrow1.Parent = panel2;
                    arrow2.Parent = panel2;

                    listArrow.Insert(i, arrow1);
                    listArrow.Insert(i + 1, arrow2);

                    //foreach (SentenceInfoWithPicBox x in listArrow)
                    //{
                    //    Console.WriteLine("x.SamplingPosition={0}", x.SamplingPosition);
                    //}

                    showText();

                    return;
                }
            }

            // 追加したArrowがlistArrowの最後に追加されるときの処理
            arrow1.OnStart = true;
            arrow2.OnStart = false;

            arrow2.Location = new Point(0, (arrow1.Location.Y + panel2.Height) / 2);
            arrow2.SamplingPosition = LocationToSamplingPosition(arrow2.Location);


            arrow1.Parent = panel2;
            arrow2.Parent = panel2;

            listArrow.Add(arrow1);
            listArrow.Add(arrow2);

        }


        // 矢印の位置から再生ポイントに変換
        private long LocationToSamplingPosition(Point p)
        {
            return (long)((double)(p.Y + ArrowCenterY) / panel2.Height * audioFile.Length / audioFile.WaveFormat.BlockAlign);
        }



        // Arrowの上でコンテキストメニューの「削除」を選択した時のイベントハンドラ
        private void tsmiDeleteOnArrow_Click(object sender, EventArgs e)
        {
            SentenceInfoWithPicBox deletingArrow = contextMenuStripOnArrow.SourceControl as SentenceInfoWithPicBox;// コンテキストメニューを開いて削除を選択した矢印をdeletingArrowに代入する。as Arrowにより、deletingArrowはArrow型以外の時nullになる

            if (deletingArrow != null)      // deletingArrowはSentenceInfo型以外の時nullになる
            {
                int arrowIndex = listArrow.IndexOf(deletingArrow);
                //Console.WriteLine("arrowIndex={0}", arrowIndex);

                if (arrowIndex > 1 && arrowIndex < listArrow.Count - 1)
                {
                    // テキストを移し替える
                    listArrow[arrowIndex - 1].SentenceText = listArrow[arrowIndex - 1].SentenceText + listArrow[arrowIndex + 1].SentenceText;

                    listArrow[arrowIndex - 2].SentenceText = listArrow[arrowIndex - 2].SentenceText + deletingArrow.SentenceText;
                }
                else if (arrowIndex == 0)
                {
                    listArrow[2].SentenceText = listArrow[0].SentenceText + listArrow[2].SentenceText;
                }
                else if (arrowIndex == 1)
                {
                    listArrow[0].SentenceText = listArrow[0].SentenceText + listArrow[2].SentenceText;
                }

                if (arrowIndex < listArrow.Count - 1)
                {

                    //
                    panel2.Controls.Remove(listArrow[arrowIndex + 1]);
                    listArrow.Remove(listArrow[arrowIndex + 1]);

                    panel2.Controls.Remove(deletingArrow);  // panel2に登録されたArrow型のオブジェクトを消す
                    listArrow.Remove(deletingArrow);        // listArrowから削除

                    showText();

                }

                //Console.WriteLine("現在のliatArrowの総数：{0}", listArrow.Count);
                //Console.WriteLine("現在のpanel2に含まれるコントロールの総数：{0}", panel2.Controls.Count);

            }
            else MessageBox.Show("選択したのはArrow型ではありません！");
        }

        private void lyricFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = voiceFileDirectory;
            sfd.Filter = "Lyric File|*.lrc";
            sfd.FileName = voiceFileBaseName;

            if (sfd.ShowDialog() == DialogResult.OK)
            {

                try
                {
                    using (StreamWriter sw = new StreamWriter(sfd.FileName))
                    {

                        string text = textBox10.Text;

                        for (int i = 0; i < listArrow.Count; i = i + 2)
                        {
                            string starttime = samplingPositionToTime(listArrow[i].SamplingPosition);
                            text = text + "[" + starttime + "]" + listArrow[i].SentenceText;
                        }

                        sw.WriteLine(text);
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
        }




        private void sentenceTimeStamperFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = voiceFileDirectory;

            sfd.Filter = " File|*.sts";

            sfd.FileName = voiceFileBaseName;

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (StreamWriter sw = new StreamWriter(sfd.FileName))
                    {

                        string text = textBox10.Text;

                        for (int i = 0; i < listArrow.Count; i = i + 2)
                        {

                            string starttime = samplingPositionToTime(listArrow[i].SamplingPosition);
                            string pausetime = samplingPositionToTime(listArrow[i + 1].SamplingPosition);

                            bool iLinefeed = false;
                            bool iReturn = false;

                            string x = listArrow[i].SentenceText;

                            if (x != null && x.Length > 1)
                            {
                                // 文末に改行コード\nがあれば削除
                                if (x[x.Length - 1] == '\n')
                                {
                                    x = x.Remove(x.Length - 1);
                                    iLinefeed = true;
                                }

                                // 文末に改行コード\rがあれば削除
                                if (x[x.Length - 1] == '\r')
                                {
                                    x = x.Remove(x.Length - 1);
                                    iReturn = true;
                                }
                            }
                            // センテンスの頭と尻にタイムスタンプを付加
                            text = text + "[" + starttime + "]" + x + "[/" + pausetime + "]";

                            // もともと文末に改行コードがあった場合、改行コード（\rもしくは\n）を加える
                            if (iReturn) text = text + '\r';
                            if (iLinefeed) text = text + '\n';

                        }

                        sw.WriteLine(text);
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
        }

        private void fontDialog1_Apply(object sender, EventArgs e)
        {

        }

        private void font_Button_Click(object sender, EventArgs e)
        {
            // テキストボックスに設定されているフォントを初期選択させる
            fontDialog1.Font = textBox1.Font;
            if (DialogResult.OK == fontDialog1.ShowDialog())
            {
                // 「OK」ボタンがクリックされた場合にフォント設定する
                textBox1.Font = fontDialog1.Font;
                textBox2.Font = fontDialog1.Font;
                textBox3.Font = fontDialog1.Font;
                textBox4.Font = fontDialog1.Font;
                textBox5.Font = fontDialog1.Font;

                label11.Font = new Font(fontDialog1.Font.Name, label1.Font.Size);
                label1.Font = label11.Font;
            }
        }


        // プロジェクトを書き込む
        private void projectFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveProject();  // プロジェクトをセーブ
        }


        private void saveProject()
        {
            FormClosingEventArgs e = new FormClosingEventArgs(CloseReason.None,false);

            saveProject(e);
        }



        // プロジェクトをセーブするルーチン
        private void saveProject(FormClosingEventArgs e)
        {
            SaveFileDialog projsfd = new SaveFileDialog();
            projsfd.InitialDirectory = voiceFileDirectory;

            projsfd.Filter = " TextVoiceSplitterProjectFile|*.tvsprojctxml";

            projsfd.FileName = voiceFileBaseName;


            if (projsfd.ShowDialog() == DialogResult.OK)
            {
                // 源泉のファイル名を書き込む
                UsedFiles usedFiles = new UsedFiles()
                {
                    TextFilePath = textFilePath,
                    VoiceFilePath = voiceFilePath,
                    TextBox10Text = textBox10.Text,
                    TextBox1Text = textBox1.Text
                };


                // SentencInfoがpictureBoxを継承しているため、Xmlにする際にエラーになることを避けるために、pictureBoxを継承していないDummySentenceInfoに代入して、ｘｍｌ化する
                List<DummySentenceInfo> dummySentenceInfoList = new List<DummySentenceInfo>();

                foreach (var member in listArrow)
                {
                    DummySentenceInfo dummySentenceInfo = new DummySentenceInfo();
                    dummySentenceInfo.SamplingPosition = member.SamplingPosition;
                    dummySentenceInfo.OnStart = member.OnStart;
                    dummySentenceInfo.OnManual = member.OnManual;
                    dummySentenceInfo.SentenceText = member.SentenceText;
                    dummySentenceInfoList.Add(dummySentenceInfo);
                }


                // ＸＭＬファイルを作る準備
                DummySentenceInfoList dummyMemberList = new DummySentenceInfoList();
                dummyMemberList.DummySentenceInfo = dummySentenceInfoList;

                Root serializedObect = new Root();
                serializedObect.DummySentenceInfoList = dummyMemberList;
                serializedObect.UsedFiles = usedFiles;


                // 書き込む
                MyXmlSerializer.Serialize(projsfd.FileName, serializedObect);

            }
            else 
            {
                e.Cancel = true;
            }
            
        }

        // プロジェクトを読み込む
        private void projectPToolStripMenuItem_Click(object sender, EventArgs e)
        {

            initialParameter();     // 基本パラメータの初期化

            // フォルダーMYMusicにある音声ファイルを開く
            openFileDialog.InitialDirectory
                = System.Environment.GetFolderPath(Environment.SpecialFolder.MyMusic); //openFileDialog1がイニシャルでMyMusicを開くようにデフォルトで設定
            openFileDialog.Filter
                = " TextVoiceSplitterProjectFile|*.tvsprojctxml"; //プロジェクトファイルのフィルタ

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // 以前作成したプロジェクトファイル(XML)を読み取る
                var deserializedObect = MyXmlSerializer.Deserialize<Root>(openFileDialog.FileName);

                DummySentenceInfoList dummySentenceInfoListRead = deserializedObect.DummySentenceInfoList;

                UsedFiles subRead = deserializedObect.UsedFiles;

                textFilePath = subRead.TextFilePath;

                clearAudioDevice(); // 使用中のオーディオデバイスなどを破棄する
                voiceFilePath = subRead.VoiceFilePath;
                setVoiceFile();     // Ｖｏｉｃｅファイルをセットする 

                textBox10.Text = subRead.TextBox10Text; // タイトルをセットする

                string str = subRead.TextBox1Text;

                textBox1.Text = str;
                //Console.WriteLine("TextBox1Text: " + str);

                foreach (DummySentenceInfo dummySentenceInfoRead in dummySentenceInfoListRead.DummySentenceInfo)
                {
                    SentenceInfoWithPicBox sentenceInfoRead = new SentenceInfoWithPicBox();
                    sentenceInfoRead.SamplingPosition = dummySentenceInfoRead.SamplingPosition;
                    sentenceInfoRead.OnStart = dummySentenceInfoRead.OnStart;
                    sentenceInfoRead.OnManual = dummySentenceInfoRead.OnManual;
                    sentenceInfoRead.SentenceText = dummySentenceInfoRead.SentenceText;
                    listArrow.Add(sentenceInfoRead);
                }

                MakeArrow();    // 矢印を作って、panel2に登録する

                CalculateArrowPosition();   // 矢印を表示する

                showText();     // テキストを表示する

                label12.Text = Math.Round(renderingScale, 1, MidpointRounding.AwayFromZero).ToString();
                label8.Text = Math.Round(magnification, 1, MidpointRounding.AwayFromZero).ToString();

            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //　formclose();        // フォームを閉じる処理
            DialogResult dr = MessageBox.Show("Finish this application!\n\rDo you need Save Project?", "Confirm!",
MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            if (dr == DialogResult.Yes)
            {
                saveProject(e);  // プロジェクトをセーブ
            }
            else if (dr == DialogResult.No)
            {
                

            }
            else e.Cancel = true;

        }

        private void textFileToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}

