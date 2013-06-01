using System;
using System.Collections.Generic;
using System.Text;

using CoreAudioApi;

namespace CoreAudioApi.Utils
{
    public class WaveGenerator
    {
        public static byte[] GenerateSine(double freq, int sampleNum, WAVEFORMATEXTENSIBLE fmt)
        {
            if (fmt.wFormatTag != WaveFormatTag.WAVE_FORMAT_PCM)
            {
                throw new NotImplementedException("unsupported format");
            }
            double[] samples = new double[sampleNum];
            for (int i = 0; i < sampleNum; i++)
            {
                samples[i] = Math.Sin(i * 2 * Math.PI * freq / fmt.nSamplesPerSec);
            }
            List<byte> ret = new List<byte>();
            switch (fmt.wBitsPerSample)
            {
                case 8:
                    for (int i = 0; i < samples.Length; i++)
                    {
                        for (int j = 0; j < fmt.nChannels; j++)
                        {
                            ret.Add((byte)((samples[i]*0.5+0.5) * byte.MaxValue));
                        }
                    }
                    break;

                case 16:
                    for (int i = 0; i < samples.Length; i++)
                    {
                        for (int j = 0; j < fmt.nChannels; j++)
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
