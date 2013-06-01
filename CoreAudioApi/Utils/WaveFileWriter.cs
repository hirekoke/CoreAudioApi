using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace CoreAudioApi.Utils
{
    public class WaveFileWriter : IDisposable
    {
        private string _filePath;
        private BinaryWriter _writer;

        private int _chunkSizePosition;
        private int _dataSizePosition;
        private int _sampleNum;
        private CoreAudioApi.WAVEFORMATEXTENSIBLE _format = null;

        private object lockObj = new object();

        public string FilePath { get { return _filePath; } }

        public WaveFileWriter(CoreAudioApi.WAVEFORMATEXTENSIBLE format,
            string filePath, FileMode mode=FileMode.Create, FileAccess access=FileAccess.Write)
        {
            _filePath = filePath;
            _format = format;
            lock (lockObj)
            {
                _writer = new BinaryWriter(new FileStream(_filePath, FileMode.Create, FileAccess.Write));
            }
            writeHeader();
        }
        ~WaveFileWriter()
        {
            Dispose();
        }

        public void Close()
        {
            Dispose();
        }
        public void Dispose()
        {
            writeEnd();
        }

        private void writeHeader()
        {
            lock (lockObj)
            {
                if (_writer == null || !_writer.BaseStream.CanWrite) return;
                _writer.Write(new char[] { 'R', 'I', 'F', 'F' });

                _chunkSizePosition = (int)_writer.BaseStream.Position;
                _writer.Seek(4, SeekOrigin.Current);

                _writer.Write(new char[] { 'W', 'A', 'V', 'E' });

                // format tag
                _writer.Write(new char[] { 'f', 'm', 't', ' ' });
                // format bytes
                _writer.Write(16);
                // format tag
                _writer.Write((ushort)CoreAudioApi.WaveFormatTag.WAVE_FORMAT_PCM);
                // channels
                _writer.Write((ushort)_format.nChannels);
                // sampling rate
                _writer.Write((uint)_format.nSamplesPerSec);
                // byte/sec
                _writer.Write((uint)(_format.nSamplesPerSec/*samp*/ *
                    _format.nChannels/*channel*/ * _format.wBitsPerSample/*bit*/ / 8.0));
                // block size (byte/sample * channel)
                _writer.Write((ushort)(_format.wBitsPerSample/*bit*/ / 8.0 * _format.nChannels/*channel*/));
                // bit/sample
                _writer.Write((ushort)_format.wBitsPerSample/*bit*/);

                // data tag
                _writer.Write(new char[] { 'd', 'a', 't', 'a' });
                // data length (byte)
                _dataSizePosition = (int)_writer.BaseStream.Position;
                _writer.Seek(4, SeekOrigin.Current);

                _writer.Flush();
            }
        }

        private void writeEnd()
        {
            lock (lockObj)
            {
                if (_writer == null || !_writer.BaseStream.CanWrite) return;

                int dataSize = (int)(_sampleNum * _format.nChannels/*channel*/ * _format.wBitsPerSample/*bit*/ / 8.0);
                _writer.Seek(_chunkSizePosition, SeekOrigin.Begin);
                _writer.Write((uint)(dataSize + 36));
                _writer.Seek(_dataSizePosition, SeekOrigin.Begin);
                _writer.Write((uint)(dataSize));

                _writer.Flush();
                _writer.Close();
                _writer = null;
            }
        }

        /// <summary>
        /// 複数チャンネル
        /// </summary>
        /// <param name="data"></param>
        public void Write(double[][] data)
        {
            lock (lockObj)
            {
                if (_writer == null || !_writer.BaseStream.CanWrite) return;
                int len = 0;
                if (data.Length > 0)
                {
                    len = data[0].Length;
                    _sampleNum += len;
                }

                switch (_format.wBitsPerSample)
                {
                    case 8:
                        for (int j = 0; j < len; j++)
                        {
                            for (int i = 0; i < _format.nChannels; i++)
                            {
                                byte b = (byte)((data[i][j] * 0.5 + 0.5) * byte.MaxValue);
                                _writer.Write(b);
                            }
                        }
                        break;
                    case 16:
                        for (int j = 0; j < len; j++)
                        {
                            for (int i = 0; i < _format.nChannels; i++)
                            {
                                short s = (short)(data[i][j] * short.MaxValue);
                                _writer.Write(s);
                            }
                        }
                        break;
                }
                _writer.Flush();
            }
        }
        /// <summary>
        /// 1チャンネル
        /// </summary>
        /// <param name="data"></param>
        public void Write(double[] data)
        {
            lock (lockObj)
            {
                if (_writer == null || !_writer.BaseStream.CanWrite) return;
                _sampleNum += (int)(data.Length / (double)_format.nChannels);

                switch (_format.wBitsPerSample)
                {
                    case 8:
                        for (int j = 0; j < data.Length; j++)
                        {
                            byte b = (byte)((data[j] + 0.5) * byte.MaxValue);
                            _writer.Write(b);
                        }
                        break;

                    case 16:
                        for (int j = 0; j < data.Length; j++)
                        {
                            short s = (short)(data[j] * short.MaxValue);
                            _writer.Write(s);
                        }
                        break;
                }
                _writer.Flush();
            }
        }

        public void WriteRawData(byte[] data)
        {
            lock (lockObj)
            {
                if (_writer == null || !_writer.BaseStream.CanWrite) return;
                _sampleNum += (int)(data.Length / (double)(_format.nChannels * _format.wBitsPerSample / 8.0));

                for (int j = 0; j < data.Length; j++)
                {
                    _writer.Write(data[j]);
                }
                _writer.Flush();
            }
        }
    }
}
