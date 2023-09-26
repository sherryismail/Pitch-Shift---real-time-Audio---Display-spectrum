using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSCore;
using CSCore.SoundIn;
using CSCore.SoundOut;
using CSCore.CoreAudioAPI;
using CSCore.Streams;
using CSCore.Codecs;
using System.IO;
using System.Media;
using CSCore.Codecs.WAV;


namespace OpenC_Demo
{
    public partial class Form1 : Form
    {
        
        private MMDeviceCollection inputDevices, outputDevices;
        //WaveIn wavein;
        //WaveOut waveout;
        WasapiCapture wavein;
        WasapiOut waveout;
        EfxProcs efxProcs;
        static int N = 512;//used for FFT
        public int numSampsToProcess = 128;
        int Fs = 48000;
        float[] audioForChart;
        float axisMax = float.MinValue;
        float axisMin = float.MaxValue;
        Fourier myfourier = new Fourier();
        FourierArrays farray = new FourierArrays(N);
        float linearGain = 1;
        float pitchShift = 1;
        SoundInSource source;

        public Form1()
        {
            InitializeComponent();
            chart1.ChartAreas[0].AxisX.MajorGrid.LineWidth = 0;
            chart1.ChartAreas[0].AxisY.MajorGrid.LineWidth = 0;
            chart1.ChartAreas[0].CursorY.IsUserEnabled = true;
            chart1.ChartAreas[0].CursorY.IsUserSelectionEnabled = true;
            chart1.ChartAreas[0].AxisY.ScrollBar.Enabled = true;
            chart1.ChartAreas[0].AxisY.ScaleView.Zoomable = true;
            chart1.ChartAreas[0].AxisY.ScrollBar.IsPositionedInside = true;
            chart1.ChartAreas[0].CursorX.IsUserEnabled = true;
            chart1.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
            chart1.ChartAreas[0].AxisX.ScrollBar.Enabled = true;
            chart1.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
            chart1.ChartAreas[0].AxisX.ScrollBar.IsPositionedInside = true;
            chart1.ChartAreas[0].AxisY.Maximum = 2;
            chart1.ChartAreas[0].AxisY.Minimum = -2;
            chart2.ChartAreas[0].AxisY.Maximum = 10;
            chart2.ChartAreas[0].AxisY.Minimum = -10;
            for (int i = 0; i < 10; i++)
            {
                chart1.Series[0].Points.Add(0);
                chart2.Series[0].Points.Add(2);
            }

            chart2.ChartAreas[0].AxisX.MajorGrid.Interval = 1000;
            chart2.ChartAreas[0].AxisX.MajorGrid.LineWidth = 0;
            chart2.ChartAreas[0].AxisY.MajorGrid.LineWidth = 0;
            chart2.ChartAreas[0].AxisX.Minimum = 0;
            chart2.ChartAreas[0].AxisX.Maximum = Fs / 2;//Nyquist
            //chart2.ChartAreas[0].AxisY.IsLogarithmic = true;//This is only chart display option, Take log() of linear value instead
            chart2.ChartAreas[0].CursorY.IsUserEnabled = true;
            chart2.ChartAreas[0].CursorY.IsUserSelectionEnabled = true;
            chart2.ChartAreas[0].AxisY.ScrollBar.Enabled = true;
            chart2.ChartAreas[0].AxisY.ScaleView.Zoomable = true;
            chart2.ChartAreas[0].AxisY.ScrollBar.IsPositionedInside = true;
            chart2.ChartAreas[0].CursorX.IsUserEnabled = true;
            chart2.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
            chart2.ChartAreas[0].AxisX.ScrollBar.Enabled = true;
            chart2.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
            chart2.ChartAreas[0].AxisX.ScrollBar.IsPositionedInside = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            detectDevices();
            //device manager: mic and speaker settings set for 48kHz, 16-bit PCM
        }

        private void detectDevices()
        {
            MMDeviceEnumerator deviceEnum = new MMDeviceEnumerator();
            //get audio capturing devices
            inputDevices = deviceEnum.EnumAudioEndpoints(DataFlow.Capture, DeviceState.Active);
            MMDevice activeDevice = deviceEnum.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            foreach (MMDevice device in inputDevices)
            {
                comboBox_mic.Items.Add(device.FriendlyName);
                if (device.DeviceID == activeDevice.DeviceID)
                    comboBox_mic.SelectedIndex = comboBox_mic.Items.Count - 1;
            }

            //Find speakers or headphones
            activeDevice = deviceEnum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            outputDevices = deviceEnum.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active);
            foreach (MMDevice device in outputDevices)
            {
                comboBox_speaker.Items.Add(device.FriendlyName);
                if (device.DeviceID == activeDevice.DeviceID)
                    comboBox_speaker.SelectedIndex = comboBox_speaker.Items.Count - 1;
            }
        }

        private void button_start_Click(object sender, EventArgs e)
        {
            wavein = null;
            wavein = new WasapiCapture(false, AudioClientShareMode.Exclusive, 5);
            wavein.Device = inputDevices[comboBox_mic.SelectedIndex];
            wavein.Initialize();
            wavein.Start();

            source = new SoundInSource(wavein) { FillWithZeros = true };
            //add my special effects in the chain
            efxProcs = new EfxProcs(source.ToSampleSource().ToMono());
            efxProcs.gain = linearGain;//keep track of this changing value
            efxProcs.pitchFactor = pitchShift;//keep track of pitch

            waveout = null;
            waveout = new WasapiOut(false, AudioClientShareMode.Exclusive, 5);
            waveout.Device = outputDevices[comboBox_speaker.SelectedIndex];
            waveout.Initialize(efxProcs.ToWaveSource()); //source.ToSampleSource().ToWaveSource());// 
            waveout.Play();
            //CSCore.Streams.SampleConverter.SampleToIeeeFloat32 sampleToIeee = new CSCore.Streams.SampleConverter.SampleToIeeeFloat32(source.ToSampleSource());
            timer1.Enabled = true;
        }

        private void updateGraphs(float[] time)
        {
            chart1.Series[0].Points.Clear();
            chart2.Series[0].Points.Clear();
            chart1.ChartAreas[0].AxisX.Maximum = time.Length;
            for (int i=0; i<time.Length;i++)
            {
                chart1.Series[0].Points.AddY(time[i]);
            }
            Array.Copy(time, farray.time_re, N);//extra filled with zeros
            myfourier.fft("forward", "hann", farray.time_re, farray.time_im, farray.freq_re, farray.freq_im, N);
            float[] spectrum = new float[N/2];
            for (int i = 0; i < N / 2; i++)
            {
                //apply scaling factor of N
                spectrum[i] = N * (float)Math.Sqrt(farray.freq_re[i] * farray.freq_re[i] + farray.freq_im[i] * farray.freq_im[i]);
                chart2.Series[0].Points.AddXY((i * Fs) / N, Math.Log((spectrum[i] + 0.001), 2));
            }
            axisMax = ((double)audioForChart.Max() > axisMax) ? audioForChart.Max() : axisMax;
            axisMin = ((double)audioForChart.Min() < axisMin) ? audioForChart.Min() : axisMin;
            chart1.ChartAreas[0].AxisY.Maximum = Math.Round(axisMax, 2);
            chart1.ChartAreas[0].AxisY.Minimum = Math.Round(axisMin, 2);
            chart1.ChartAreas[0].AxisX.Maximum = 200;
           // chart1.ChartAreas[0].RecalculateAxesScale();
        }

        
        private void timer1_Tick(object sender, EventArgs e)
        {
            audioForChart = efxProcs.DataToDisplay();
            updateGraphs(audioForChart);//will do fft and display in a single for-loop
            //short temp;
            //for (int i = 0, j = 0; i < outbytes.Length; i += bytesPerSample, j++)//captured are 16-bit
            //{
            //      temp = (short)((outbytes[i + 1] << 8) | outbytes[i]);
            //      audioForChart[j] = (float)temp / 32768f;
            //}
        }

        private void knobControl1_ValueChanged(object Sender)
        {
            if (wavein == null)//&& knobControl1.Value != 0
            {
                MessageBox.Show("Please press the Start button first", "Invalid input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else if (efxProcs != null)
            {
                linearGain = (float)(Math.Pow(10.0, 20.0));//dB volume to linear
                efxProcs.gain = linearGain;
            }
            
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (wavein == null)
            {
                MessageBox.Show("Please press the Start button first","Invalid input",MessageBoxButtons.OK, MessageBoxIcon.Error);
                trackBar1.Value = 0;
                return;
            }
            else
            {
                pitchShift = (float)Math.Pow(2, trackBar1.Value / 10f);
                efxProcs.pitchFactor = pitchShift;
            }    
        }

        private void comboBox_mic_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_mic.SelectedIndex != 0)
            {
                wavein.Device = inputDevices[comboBox_mic.SelectedIndex];
                wavein.Initialize();
                wavein.Start();
            }
        }

        private void comboBox_speaker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_speaker.SelectedIndex != 0)
            {
                if (waveout.PlaybackState != PlaybackState.Stopped)
                {
                    waveout.Stop();
                    waveout.Dispose();
                }
                waveout.Device = outputDevices[comboBox_speaker.SelectedIndex];
                var source = new SoundInSource(wavein) { FillWithZeros = true };
                waveout = new WasapiOut(false, AudioClientShareMode.Exclusive, 5);
                waveout.Device = outputDevices[comboBox_speaker.SelectedIndex];
                waveout.Initialize(source);
                waveout.Play();
            }
        }

        private void button_stop_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            waveout.Stop(); waveout.Dispose();
            wavein.Stop(); wavein.Dispose();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (waveout != null && waveout.PlaybackState != PlaybackState.Stopped)
            {
                waveout.Stop(); waveout.Dispose();
            }
            if (wavein != null && wavein.RecordingState != RecordingState.Stopped)
            {
                wavein.Stop(); wavein.Dispose();
            }               
        }   
    }
}
