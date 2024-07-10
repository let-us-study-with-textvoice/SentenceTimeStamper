using System;
//using System.Windows.Forms;


namespace NAudio.WaveFormRenderer
{
    public class SentenceInfo //:PictureBox
    {
        private long samplingPosition;
        private bool onstart;
        private bool onmanual;
        //private const int ArrowWidth = 44;
        //private const int ArrowHeight = 21;
        //private string sentenceText;


        //public SentenceInfo() 
        //{
        //    //Width = ArrowWidth;
        //    //Height = ArrowHeight;
        //}

        public SentenceInfo(long Position, bool onStart, bool onManual)
        {
            //Width = ArrowWidth;
            //Height = ArrowHeight;
            //Console.WriteLine("ArrowWidth1={0}", Width);

            this.samplingPosition = Position;
            this.onstart = onStart;
            this.onmanual = onManual;
            //changImage();
        }

        //private void changImage()
        //{
        //    if (this.onstart)
        //    {
        //        //this.Image = WaveFormRendererLib.Properties.Resources.StartTag;
        //    }
        //    else
        //    {
        //        //this.Image = WaveFormRendererLib.Properties.Resources.PauseTag;
        //    }
        //}

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
                //changImage();
            } 
        }


        public bool OnManual { get { return onmanual; }  set { onmanual = value; } }

        //public string SentenceText { get { return sentenceText; } set { sentenceText = value; } }

    }
}
