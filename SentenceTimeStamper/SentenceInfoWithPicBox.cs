//using System;
using System.Windows.Forms;
using NAudio.WaveFormRenderer;


namespace SentenceTimeStamper
{
    public class SentenceInfoWithPicBox:PictureBox
    {
        private long samplingPosition;
        private bool onstart;
        private bool onmanual;
        private const int ArrowWidth = 44;
        private const int ArrowHeight = 21;
        private string sentenceText;

        public SentenceInfoWithPicBox()
        {
            Width = ArrowWidth;
            Height = ArrowHeight;
        }

        public SentenceInfoWithPicBox(SentenceInfo sentInfo) 
        {
            Width = ArrowWidth;
            Height = ArrowHeight;

            this.samplingPosition = sentInfo.SamplingPosition;
            this.onstart = sentInfo.OnStart;
            this.onmanual = sentInfo.OnManual;

            ChangeImage();
        }

        public SentenceInfoWithPicBox(long Position, bool onStart, bool onManual)
        {
            Width = ArrowWidth;
            Height = ArrowHeight;
            //Console.WriteLine("ArrowWidth1={0}", Width);

            this.samplingPosition = Position;
            this.onstart = onStart;
            this.onmanual = onManual;

            ChangeImage();
        }

        private void ChangeImage()
        {
            if (this.onstart)
            {
                this.Image = Properties.Resources.StartTag;
            }
            else
            {
                this.Image = Properties.Resources.PauseTag;
            }

        }
        public long SamplingPosition { get { return samplingPosition; }  set { samplingPosition = value; } }
        public bool OnStart 
        { 
            get 
            {
                return onstart; 
            } 
            set 
            {
                onstart = value;
                ChangeImage();
            } 
        }


        public bool OnManual { get { return onmanual; }  set { onmanual = value; } }

        public string SentenceText { get { return sentenceText; } set { sentenceText = value; } }

    }
}
