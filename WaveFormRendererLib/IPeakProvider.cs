using NAudio.Wave;

namespace WaveFormRendererLib2
{
    public interface IPeakProvider
    {
        void Init(ISampleProvider reader, int samplesPerPixel);
        PeakInfo GetNextPeak();
    }
}