using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSCore;

namespace OpenC_Demo
{
    class EfxProcs : ISampleSource
    {
        ISampleSource audiostream;
        public float pitchFactor { get; set; }
        public float gain { get; set; }
        public float[] audioThisTime { get; set; }
        public Form1 mainform { get; set; }
        delegate void updateChartDelegate(int position, int N, float[] data);
        public EfxProcs(ISampleSource source)
        {
            audiostream = source;
            pitchFactor = 1;
            gain = 1;//0dB
        }
        public int Read(float[] buffer, int offset, int count)
        {
            int status = audiostream.Read(buffer, offset, count);
            audioThisTime = buffer;
            for (int i = offset; i < offset + status; i++)
            {
                buffer[i] = buffer[i] * gain;
            }
            PitchProcessing.PitchShift(pitchFactor, offset, count, 2048, 4, audiostream.WaveFormat.SampleRate, buffer);
            return status;
        }
        public float[] DataToDisplay()
        {
            return audioThisTime;
        }
        public bool CanSeek
        {
            get
            {
                return audiostream.CanSeek;
            }
        }

        public WaveFormat WaveFormat
        {
            get
            {
                return audiostream.WaveFormat;
            }
        }

        public long Position
        {
            get
            {
                return audiostream.Position;
            }
            set
            {
                audiostream.Position = value;
            }
        }

        public long Length
        {
            get { return audiostream.Length; }
        }

        public void Dispose()
        {
            if (audiostream != null) audiostream.Dispose();
        }

        
    }
}
