using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenC_Demo
{
    public class Fourier
    {
        public Fourier() { }
        int fftn;
        int fftnd2;
        int j;
        int k;
        int l;
        //int m;
        int n;
        int bf;
        int bw;
        int kt;
        int kb;
        int normalize;
        //int lf;
        //int hf;
        int fft_order;
        int signer;
        double twiddle;
        double p;
        double r;
        double t;
        double wi;
        double wr;
        double ti;
        double tr;
        double slope;
        ushort[] bin = { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768 };
        public void fft(string direction, string window, float[] input_re, float[] input_im, float[] out_re, float[] out_im, int size)
        {
            fftn = size;
            fftnd2 = fftn / 2;
            fft_order = (int)Math.Round(Math.Log(fftn) / Math.Log(2));
            signer = 1;
            normalize = 1;
            /* determine if its forward transform FFT or reverse (IFFT)
             * 
             * */
            switch (direction)
            {
                case "forward":
                case "FFT":
                case "fft":
                case "frequency":
                    normalize = fftnd2;
                    signer = 1;
                    break;
                case "inverse":
                case "reverse":
                case "IFFT":
                case "ifft":
                case "time":
                    normalize = 2;
                    signer = -1;
                    break;
                default:
                    normalize = fftnd2;
                    signer = 1;
                    Console.WriteLine("Direction of FFT was not chosen correctly. Forward direction i.e. Temporal to Fourier domain is assumed.", "Fourier Direction");
                    break;
            }
            /* determine what type of truncating window is assumed
             * 
             * */
            switch (window)
            {
                case "bart":
                    slope = fftnd2;
                    slope = 1 / slope;
                    for (int j = 0; j <= fftnd2; j++) // what to use,< or <= ??
                    {
                        input_re[j] = input_re[j] * (j * (float)slope);
                        input_re[fftn - 1 - j] = input_re[fftn - 1 - j] * (j * (float)slope);
                    }
                    break;
                case "blac":
                    int n = 0;
                    for (j = -fftnd2; j < fftnd2; j++) // between -512 to 511
                    {
                        input_re[n] = input_re[n] * (float)(0.42 + (0.5 * Math.Cos(j / (fftn - 1) * 2 * Math.PI)) + (0.08 * Math.Cos(j / (fftn - 1) * 4 * Math.PI)));
                        n++;
                    }
                    break;
                case "hann":
                    n = 0;
                    for (j = -fftnd2; j < fftnd2; j++)
                    {
                        input_re[n] = input_re[n] * (float)(0.5 + 0.5 * Math.Cos(j / (fftn) * 2 * Math.PI));
                        n++;
                    }
                    break;
                case "hamm":
                    n = 0;
                    for (j = -fftnd2; j < fftnd2; j++)
                    {
                        input_re[n] = input_re[n] * (float)(0.54 + 0.46 * Math.Cos(j / (fftn) * 2 * Math.PI));
                        n++;
                    }
                    break;
                case "rect":
                default:
                    break;
            }
            /* bit reversal for rearraging data
             * */
            for (k = 0; k <= fftn - 1; k++)
            {
                twiddle = 0;
                l = k;
                for (j = fft_order - 1; j >= 0; j--)
                {
                    twiddle = twiddle + (l % 2) * bin[j];
                    l = l / 2;
                }
                out_re[k] = input_re[(int)Math.Round(twiddle)] / normalize;
                out_im[k] = input_im[(int)Math.Round(twiddle)] / normalize;
            }
            /* perform FFT or IFFT depending on variables chosen
             * */
            for (n = 1; n <= fft_order; n++)
            {
                bw = bin[n - 1];
                bf = bw * 2;
                p = fftn / bf;
                for (j = 0; j <= bw - 1; j++)
                {
                    r = p * j;
                    t = 2 * Math.PI * r / fftn;
                    wr = Math.Cos(t);
                    wi = Math.Sin(t) * signer;
                    kt = j;
                    while (kt <= (fftn - 1))
                    {
                        kb = kt + bw;
                        tr = (wr * out_re[kb]) + (wi * out_im[kb]);
                        ti = (wr * out_im[kb]) - (wi * out_re[kb]);
                        out_re[kb] = out_re[kt] - (float)tr;
                        out_im[kb] = out_im[kt] - (float)ti;
                        out_re[kt] = out_re[kt] + (float)tr;
                        out_im[kt] = out_im[kt] + (float)ti;
                        kt = kt + bf;
                    }
                }
            }

        }
    }
    public struct FourierArrays
    {
        public float[] time_re;
        public float[] time_im;
        public float[] freq_re;
        public float[] freq_im;
        public FourierArrays(int N)
        {
            time_re = new float[N];
            time_im = new float[N];
            freq_re = new float[N];
            freq_im = new float[N];
        }
    }
}
