using System;
using System.Collections.Generic;
using System.Text;

using CoreAudioApi;

namespace CoreAudioApi.Utils
{
    public class WaveGenerator
    {
        public static byte[] GenerateSine(double freq, int sampleNum, int sampleRate, int bps, int nChannels)
        {
            double[] samples = new double[sampleNum];
            for (int i = 0; i < sampleNum; i++)
            {
                samples[i] = Math.Sin(i * 2 * Math.PI * freq / (double)sampleRate);
            }
            List<byte> ret = new List<byte>();
            switch (bps)
            {
                case 8:
                    for (int i = 0; i < samples.Length; i++)
                    {
                        for (int j = 0; j < nChannels; j++)
                        {
                            ret.Add((byte)((samples[i]*0.5+0.5) * byte.MaxValue));
                        }
                    }
                    break;

                case 16:
                    for (int i = 0; i < samples.Length; i++)
                    {
                        for (int j = 0; j < nChannels; j++)
                        {
                            short s = (short)(samples[i] * short.MaxValue);
                            ret.AddRange(BitConverter.GetBytes(s));
                        }
                    }
                    break;
            }
            return ret.ToArray();
        }
    }
}
