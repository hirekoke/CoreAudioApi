using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace CoreAudioApi.Utils
{
    /// <summary>
    /// 未実装
    /// </summary>
    public class ExclusiveAudioInput : IDisposable
    {
        // CoreAudioApi
        private AudioClient _audioClient;
        private AudioCaptureClient _capClient;
        private MMDeviceEnumerator _devices = null;
        private MMDevice _capDevice;
        private string _capDeviceId;
        internal string SelectedDeviceId { get { return _capDeviceId; } }

        private WAVEFORMATEXTENSIBLE _capFormat;

        public MMDevice CapDevice { get { return _capDevice; } }
        public WAVEFORMATEXTENSIBLE CapFormat { get { return _capFormat; } }

        public ExclusiveAudioInput()
        {
            _devices = new MMDeviceEnumerator();

            MMDevice dev = null;
            if (_devices != null)
            {
                foreach (MMDevice device in _devices.EnumerateAudioEndPoints(EDataFlow.eCapture, DeviceState.DEVICE_STATE_ACTIVE))
                {
                    dev = device;
                    break;
                }
            }

            if (dev != null)
            {
                SelectDevice(dev.Id);
            }
        }

        public void Dispose()
        {
        }

        public void UpdateDeviceInfo()
        {
        }

        public void SelectDevice(string devId)
        {
            _capDevice = _devices.GetDevice(devId.Trim());

            if (_capDevice == null)
            {
                _audioClient = null;
                _capClient = null;
                return;
            }

            _capDeviceId = _capDevice.Id;
            
            // モード
            AudioClientShareMode shareMode = AudioClientShareMode.Exclusive;
            AudioClientStreamFlags streamFlags = AudioClientStreamFlags.NoPersist;

            if (_audioClient != null)
                _capDevice.ReleaseAudioClient();

            try
            {
                _audioClient = _capDevice.AudioClient;
                _capFormat = _audioClient.MixFormat;

                _capFormat.wFormatTag = WaveFormatTag.WAVE_FORMAT_EXTENSIBLE;
                _capFormat.nChannels = 2;
                _capFormat.nSamplesPerSec = 16000;
                _capFormat.wBitsPerSample = 16;
                _capFormat.SubFormat = CoreAudioApi.AudioMediaSubtypes.MEDIASUBTYPE_PCM;

                _capFormat.wValidBitsPerSample = _capFormat.wBitsPerSample;
                _capFormat.nBlockAlign = (ushort)(_capFormat.wBitsPerSample / 8.0 * _capFormat.nChannels);
                _capFormat.nAvgBytesPerSec = _capFormat.nSamplesPerSec * _capFormat.nBlockAlign;

                long tmp1; long tmp2;
                _audioClient.GetDevicePeriod(out tmp1, out tmp2);

                // 初期化
                try
                {
                    WAVEFORMATEXTENSIBLE tmpFmt = new WAVEFORMATEXTENSIBLE();
                    if (!_audioClient.IsFormatSupported(shareMode, _capFormat, ref tmpFmt))
                        _capFormat = tmpFmt;
                    _audioClient.Initialize(shareMode, streamFlags,
                        tmp2, tmp2,
                        _capFormat, Guid.Empty);
                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    try
                    {
                        AudioClientError error = (AudioClientError)ex.ErrorCode;
                        switch (error)
                        {
                            case AudioClientError.BufferSizeNotAligned:
                                uint bufSize = _audioClient.BufferSize;
                                tmp2 = (long)((10000.0 * 1000 / _capFormat.nSamplesPerSec * bufSize) + 0.5);
                                _audioClient.Initialize(shareMode,
                                    streamFlags, tmp2, tmp2, _capFormat, Guid.Empty);
                                break;

                            case AudioClientError.UnsupportedFormat:

                                break;
                        }
                    }
                    catch (InvalidCastException)
                    {

                    }
                }

                _capClient = _audioClient.AudioCaptureClient;

            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                _audioClient = null;
                _capClient = null;
                throw ex;
            }
        }

    }
}
