using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace CoreAudioApi.Utils
{
    /// <summary>
    /// 共有モードで音声取得をする
    /// </summary>
    public class SharedAudioInput : IDisposable
    {
        const int MSIN100NS = 10000;

        // 定数
        private const int RESETLIMIT = 500; // AudioClientをリセットするまでの無音時間のリミット(msec)
        private const int SENDSPAN = 300; // 解析をする間隔(msec)
        private const int CAPLEN = 300; // データを取得する長さ(msec)
        private const int POLLSPAN = 10; // データ更新のポーリング間隔(msec)

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

        // スレッド制御用
        private object _lockObj = new object();
        private Thread _thread = null;
        private TimeSpan _sleepTime = new TimeSpan(POLLSPAN * MSIN100NS);

        // 制御命令キュー
        private Queue<Operation> _opQueue = new Queue<Operation>();
        private bool _capturing = false;
        public bool Capturing
        {
            get { return _capturing; }
        }

        // データ制御用
        private long _prevResetTime = 0;
        private bool _reset = false;

        // データ格納バッファ
        private long _prevSendTime = 0;
        private List<byte> _bytebuf = new List<byte>();
        private List<double> _buffer = new List<double>();
        private List<List<double>> _channelBuffer = new List<List<double>>();
        private long _curSamples = 0;

        // ボリュームデータ
        private double _masterVolume = 0;
        public double MasterVolume { get { return _masterVolume; } }
        private double[] _channelVolumes = new double[] { 0 };
        public double[] ChannelVolumes { 
            get {
                double[] v = new double[_channelVolumes.Length];
                _channelVolumes.CopyTo(v, 0);
                return v;
            } 
        }

        // デバイス情報
        private List<DeviceInfo> _deviceInfos = new List<DeviceInfo>();
        public List<DeviceInfo> DeviceInfos
        {
            get
            {
                lock (_lockObj)
                {
                    return new List<DeviceInfo>(_deviceInfos);
                }
            }
        }

        // event
        public event EventHandler<InputDisposedEventArgs> InputDisposed;
        public event EventHandler<DeviceInfoUpdatedEventArgs> DeviceInfoUpdated;
        public event EventHandler<DeviceSelectedEventArgs> DeviceSelected;
        public event EventHandler<CaptureStartedEventArgs> CaptureStarted;
        public event EventHandler<CaptureStoppedEventArgs> CaptureStopped;
        public event EventHandler<DataUpdatedEventArgs> DataUpdated;
        public event EventHandler<VolumeChangedEventArgs> VolumeChanged;
        public event EventHandler<ErrorEventArgs> ErrorOccured;

        public SharedAudioInput()
        {
            _devices = new MMDeviceEnumerator();
            _devices.OnDeviceAdded += (s, e) => { UpdateDeviceInfo(); };
            _devices.OnDeviceRemoved += (s, e) => { UpdateDeviceInfo(true); };
            _devices.OnDeviceStateChanged += (s, e) => { UpdateDeviceInfo(); };
            _devices.OnPropertyValueChanged += (s, e) => { UpdateDeviceInfo(); };
            _devices.OnDefaultDeviceChanged += (s, e) => { UpdateDeviceInfo(); };

            _thread = new Thread(new ThreadStart(mainThread));
            _thread.Start();
        }

        public void Dispose()
        {
            lock (_opQueue)
            {
                if(Capturing) _opQueue.Enqueue(new Operation(Operation.OperationType.StopCapture));
                _opQueue.Enqueue(new Operation(Operation.OperationType.Dispose));
            }
        }

        public void UpdateDeviceInfo(bool needStop = false)
        {
            lock (_opQueue)
            {
                _opQueue.Enqueue(new Operation(Operation.OperationType.UpdateDevices, needStop));
            }
        }
        public void SelectDevice(string devId)
        {
            lock (_opQueue)
            {
                _opQueue.Enqueue(new Operation(Operation.OperationType.SelectDevice, devId));
            }
        }
        internal void ReleaseDevice()
        {
            lock (_opQueue)
            {
                _opQueue.Enqueue(new Operation(Operation.OperationType.ReleaseDevice));
            }
        }
        public void StopCapture()
        {
            lock (_opQueue)
            {
                _opQueue.Enqueue(new Operation(Operation.OperationType.StopCapture));
            }
        }
        public void StartCapture()
        {
            lock (_opQueue)
            {
                _opQueue.Enqueue(new Operation(Operation.OperationType.StartCapture));
            }
        }

        #region 内部実装
        private void disposeImpl()
        {
            if (_devices != null) _devices.Dispose();

            // イベント発火
            var del = InputDisposed;
            if (del != null)
            {
                del.Invoke(this, new InputDisposedEventArgs(_capDeviceId));
            }
        }

        private void releaseDeviceImpl()
        {
            if (_capDevice != null)
            {
                if (_capturing) stopCaptureImpl();
                _capDevice.Dispose();
            }
            _capDevice = null;
            if (_capClient != null) _capClient.Dispose();
            _capClient = null;
            if (_audioClient != null) _audioClient.Dispose();
            _audioClient = null;
        }

        private void updateDeviceInfoImpl(bool needStop)
        {
            if (needStop)
            {
                stopCaptureImpl();
            }
            lock (_lockObj)
            {
                _deviceInfos.Clear();
                if (_devices != null)
                {
                    foreach (MMDevice device in _devices.EnumerateAudioEndPoints(EDataFlow.eAll, DeviceState.DEVICE_STATE_ACTIVE))
                    {
                        AudioEndpointVolume vol = device.AudioEndpointVolume;
                        DeviceInfo info = new DeviceInfo(device.Id, device.FriendlyName, device.DataFlow, device.State, device.DeviceFormat);
                        _deviceInfos.Add(info);
                    }
                }
            }

            // イベント発火
            var del = DeviceInfoUpdated;
            if (del != null)
            {
                List<DeviceInfo> info = this.DeviceInfos;
                del.Invoke(this, new DeviceInfoUpdatedEventArgs(info));
            }
        }

        private void selectDeviceImpl(string devId)
        {
            if (_capDevice != null && _capDevice.Id == devId)
            {
                return;
            }

            releaseDeviceImpl();

            _capDevice = _devices.GetDevice(devId.Trim());
            int idx = _deviceInfos.FindIndex((di) => { return di.DeviceId == devId; });
            if (_capDevice == null)
            {
#warning 例外
                _audioClient = null;
                _capClient = null;
                return;
            }
            _capDeviceId = _capDevice.Id;

            // モード
            AudioClientShareMode shareMode = AudioClientShareMode.Shared;

            // デバイスに適した初期化方法を決定
            AudioClientStreamFlags streamFlags = AudioClientStreamFlags.NoPersist;
            switch (shareMode)
            {
                case AudioClientShareMode.Shared:
                    switch (_capDevice.DataFlow)
                    {
                        case EDataFlow.eCapture:
                            streamFlags = 0;
                            break;
                        case EDataFlow.eRender:
                            streamFlags = AudioClientStreamFlags.Loopback;
                            break;
                    }
                    break;
                case AudioClientShareMode.Exclusive:
                    streamFlags = AudioClientStreamFlags.NoPersist;
                    break;
            }

            // フォーマット
            if (_audioClient != null) _capDevice.ReleaseAudioClient();

            // ボリューム
            _masterVolume = 0;
            _channelVolumes = new double[_capDevice.AudioMeterInformation.PeakValues.Count];
            var h = VolumeChanged;
            if (h != null)
            {
                h(this, new VolumeChangedEventArgs(_capDeviceId, _masterVolume, _channelVolumes));
            }

            try
            {
                _audioClient = _capDevice.AudioClient;
                _capFormat = _audioClient.MixFormat;

                if (shareMode == AudioClientShareMode.Exclusive)
                {
                    _capFormat.wFormatTag = WaveFormatTag.WAVE_FORMAT_EXTENSIBLE;
                    _capFormat.nChannels = 2;
                    _capFormat.nSamplesPerSec = 44100;
                    _capFormat.wBitsPerSample = 16;
                    _capFormat.SubFormat = CoreAudioApi.AudioMediaSubtypes.MEDIASUBTYPE_PCM;

                    _capFormat.wValidBitsPerSample = _capFormat.wBitsPerSample;
                    _capFormat.nBlockAlign = (ushort)(_capFormat.wBitsPerSample / 8.0 * _capFormat.nChannels);
                    _capFormat.nAvgBytesPerSec = _capFormat.nSamplesPerSec * _capFormat.nBlockAlign;
                }

                long tmp1; long tmp2;
                _audioClient.GetDevicePeriod(out tmp1, out tmp2);

                // 初期化

                try
                {
                    WAVEFORMATEXTENSIBLE tmpFmt = new WAVEFORMATEXTENSIBLE();
                    if (!_audioClient.IsFormatSupported(shareMode, _capFormat, ref tmpFmt)) _capFormat = tmpFmt;
                    _audioClient.Initialize(shareMode,
                            streamFlags, tmp2, tmp2, _capFormat, Guid.Empty);
                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    if ((uint)ex.ErrorCode == 0x88890019)
                    {
                        uint bufSize = _audioClient.BufferSize;
                        tmp2 = (long)((10000.0 * 1000 / _capFormat.nSamplesPerSec * bufSize) + 0.5);
                        _audioClient.Initialize(shareMode,
                            streamFlags, tmp2, tmp2, _capFormat, Guid.Empty);
                    }
                }
                clearBuffer();

                _capClient = _audioClient.AudioCaptureClient;

                // イベント発火
                var del = DeviceSelected;
                if (del != null)
                {
                    del.Invoke(this, new DeviceSelectedEventArgs(_capDevice, idx));
                }
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                _audioClient = null;
                _capClient = null;
                throw ex;
            }
        }

        private void stopCaptureImpl()
        {

            try
            {
                if (_audioClient != null && _capClient != null && _audioClient.IsStarted)
                {
                    _audioClient.Stop();

                    // イベント発火
                    var del1 = CaptureStopped;
                    if (del1 != null)
                    {
                        del1.Invoke(this, new CaptureStoppedEventArgs(_capDeviceId));
                    }
                    raiseDataUpdated();
                }
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                throw ex;
            }
        }

        private void startCaptureImpl()
        {
            stopCaptureImpl();

            if (_audioClient == null || _capClient == null)
            {
                return;
            }

            long defaultDp; long minimumDp;

            try
            {
                _audioClient.GetDevicePeriod(out defaultDp, out minimumDp);
                _sleepTime = new TimeSpan((long)(defaultDp / 4.0));

                clearBuffer();

                _prevResetTime = Environment.TickCount;
                _prevSendTime = Environment.TickCount;
                _reset = true;

                _audioClient.Start();

                // イベント発火
                var del = CaptureStarted;
                if (del != null)
                {
                    del.Invoke(this, new CaptureStartedEventArgs(_capDeviceId));
                }
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                throw ex;
            }
        }

        // 今溜まっている分を解析する
        private void sendBuffer()
        {
            _prevSendTime = Environment.TickCount;
            if (_buffer.Count == 0) return;

            double[] buf = _buffer.ToArray();
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] /= _capFormat.nChannels;
            }

            // イベント発火
            raiseDataUpdated();
        }
        // バッファをクリアする
        private void clearBuffer()
        {
            _bytebuf.Clear();
            _buffer.Clear();
            _channelBuffer.Clear();
            _curSamples = 0;
            for (int i = 0; i < _capFormat.nChannels; i++)
            {
                _channelBuffer.Add(new List<double>());
            }
        }

        private void resetImpl()
        {
            if (!string.IsNullOrEmpty(_capDeviceId))
                selectDeviceImpl(_capDeviceId);
            stopCaptureImpl();
            startCaptureImpl();
            _reset = true;
        }
        #endregion

        #region メインスレッド
        private void mainThread()
        {
            try
            {
                while (true)
                {
                    Operation op = new Operation(Operation.OperationType.None);
                    lock (_opQueue)
                    {
                        if (_opQueue.Count > 0)
                        {
                            op = _opQueue.Dequeue();
                        }
                    }

                    try
                    {
                        switch (op.OpType)
                        {
                            case Operation.OperationType.Dispose: // 終了
                                releaseDeviceImpl();
                                disposeImpl();
                                _capturing = false;
                                return;
                            case Operation.OperationType.UpdateDevices: // Device一覧アップデート
                                updateDeviceInfoImpl((bool)op.OpArgs[0]);
                                break;
                            case Operation.OperationType.SelectDevice: // Device選択
                                selectDeviceImpl(op.OpArgs[0] as string);
                                _capturing = false;
                                break;
                            case Operation.OperationType.ReleaseDevice: // Device解放
                                releaseDeviceImpl();
                                _capturing = false;
                                break;
                            case Operation.OperationType.StartCapture: // 開始
                                startCaptureImpl();
                                _capturing = true;
                                break;
                            case Operation.OperationType.StopCapture: // 解放(stop)
                                stopCaptureImpl();
                                _capturing = false;
                                break;
                            default: // その他
                                {
                                    #region 音量
                                    if (_capDevice == null)
                                    {
                                        _masterVolume = 0;
                                        _channelVolumes = new double[] { 0 };
                                    }
                                    else
                                    {
                                        _masterVolume = _capDevice.AudioMeterInformation.MasterPeakValue;
                                        for (int i = 0; i < _capDevice.AudioMeterInformation.PeakValues.Count; i++)
                                            _channelVolumes[i] = _capDevice.AudioMeterInformation.PeakValues[i];
                                    }
                                    var h = VolumeChanged;
                                    if (h != null)
                                    {
                                        h(this, new VolumeChangedEventArgs(_capDeviceId, _masterVolume, _channelVolumes));
                                    }
                                    #endregion

                                    #region キャプチャ
                                    if (_capturing)
                                    {
                                        if (_capClient != null)
                                        {
                                            long curTick = Environment.TickCount;
                                            uint size = 0;

                                            try
                                            {
                                                size = _capClient.NextPacketSize;

                                                if (size == 0) // 音が無い or バッファが一定量溜まっていない
                                                {
                                                    if (!_reset)
                                                    {
                                                        // データが来なくなってから一定時間 -> リセット
                                                        if (curTick - _prevResetTime > RESETLIMIT)
                                                        {
                                                            resetImpl();
                                                            _prevResetTime = curTick;

                                                            raiseDataUpdated();
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    _reset = false;
                                                    _prevResetTime = curTick;

                                                    try
                                                    {
                                                        while (_capClient.NextPacketSize > 0)
                                                        {
                                                            byte[] bytes; uint numFrames;
                                                            AudioClientBufferFlags flags;
                                                            UInt64 devicePosition; UInt64 qpcPosition;

                                                            _capClient.GetBuffer(out bytes, out numFrames, out flags,
                                                                out devicePosition, out qpcPosition);
                                                            //if ((flags & AudioClientBufferFlags.DataDiscontinuity) != 0)
                                                            //    clearBuffer();

                                                            switch (_capFormat.wBitsPerSample)
                                                            {
                                                                case 8:
                                                                    get8bitBuf(bytes);
                                                                    break;
                                                                case 16:
                                                                    get16bitBuf(bytes);
                                                                    break;
                                                                case 24:
                                                                    get24bitBuf(bytes);
                                                                    break;
                                                                case 32:
                                                                    get32bitBuf(bytes);
                                                                    break;
                                                            }
                                                        }
                                                    }
                                                    catch (Exception) { }
                                                    if (curTick - _prevSendTime > SENDSPAN) sendBuffer();
                                                }
                                            }
                                            catch (System.Runtime.InteropServices.COMException ex)
                                            {
                                                throw ex;
                                            }
                                        }
                                    }
                                    #endregion
                                }
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        raiseError(ex);
                    }
                    Thread.Sleep(_sleepTime);
                }
            }
            catch (ThreadAbortException threadEx)
            {
                Console.WriteLine(threadEx.Message);
            }
        }
        #endregion

        #region byte列からdouble列に変換し、バッファに書き込む
        private void get8bitBuf(byte[] bytes)
        {
            int idx = 0; int sample = 0;
            _bytebuf.AddRange(bytes);
            while (idx < bytes.Length)
            {
                double d = (bytes[idx] + 256) / (double)(255 + 256);

                int c = sample % _capFormat.nChannels;
                _channelBuffer[c].Add(d);

                if (c == 0) _buffer.Add(d);
                else _buffer[_buffer.Count - 1] += d;

                idx++;
                sample++;
            }
            _curSamples += sample;
            Debug.Assert(idx == bytes.Length, "byte配列の長さがブロック境界と一致しない");
        }
        private void get16bitBuf(byte[] bytes)
        {
            int idx = 0; int sample = 0;
            _bytebuf.AddRange(bytes);
            while (idx < bytes.Length)
            {
                double d = BitConverter.ToInt16(bytes, idx) / (double)(short.MaxValue);

                int c = sample % _capFormat.nChannels;
                _channelBuffer[c].Add(d);

                if (c == 0) _buffer.Add(d);
                else _buffer[_buffer.Count - 1] += d;

                idx += 2;
                sample++;
            }
            _curSamples += sample;
            Debug.Assert(idx == bytes.Length, "byte配列の長さがブロック境界と一致しない");
        }
        private void get24bitBuf(byte[] bytes)
        {
            int idx = 0; int sample = 0;
            _bytebuf.AddRange(bytes);
            while (idx < bytes.Length)
            {
                char c0 = BitConverter.ToChar(bytes, idx++);
                char c1 = BitConverter.ToChar(bytes, idx++);
                char c2 = BitConverter.ToChar(bytes, idx++);
                double d = (c0 + c1 * 256 + c2 * 256 * 256) / 8388608.0;

                int c = sample % _capFormat.nChannels;
                _channelBuffer[c].Add(d);

                if (c == 0) _buffer.Add(d);
                else _buffer[_buffer.Count - 1] += d;

                sample++;
            }
            _curSamples += sample;
            Debug.Assert(idx == bytes.Length, "byte配列の長さがブロック境界と一致しない");
        }
        private void get32bitBuf(byte[] bytes)
        {
            int idx = 0; int sample = 0;
            _bytebuf.AddRange(bytes);
            if (_capFormat.SubFormat == CoreAudioApi.AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT)
            {
                while (idx < bytes.Length)
                {
                    double d = (double)BitConverter.ToSingle(bytes, idx);

                    int c = sample % _capFormat.nChannels;
                    _channelBuffer[c].Add(d);

                    if (c == 0) _buffer.Add(d);
                    else _buffer[_buffer.Count - 1] += d;

                    idx += 4;
                    sample++;
                }
                _curSamples += sample;
                Debug.Assert(idx == bytes.Length, "byte配列の長さがブロック境界と一致しない");
            }
        }
        #endregion

        protected void raiseDataUpdated()
        {
            var h = DataUpdated;
            double[][] b = new double[_channelBuffer.Count][];
            for (int i = 0; i < b.Length; i++)
            {
                b[i] = _channelBuffer[i].ToArray();
            }
            if(h != null)
                h.Invoke(this, new DataUpdatedEventArgs(_capDeviceId, (int)_curSamples, _bytebuf.ToArray(), b));
            clearBuffer();
        }

        protected void raiseError(Exception ex)
        {
            var h = ErrorOccured;
            if (h != null)
            {
                h(this, new ErrorEventArgs(_capDeviceId, ex));
            }
        }

        internal class Operation
        {
            public enum OperationType
            {
                Dispose,
                StopCapture,
                UpdateDevices,
                StartCapture,
                SelectDevice,
                ReleaseDevice,
                None,
            }

            private OperationType opType;
            private object[] opArgs;
            public OperationType OpType { get { return opType; } }
            public object[] OpArgs { get { return opArgs; } }

            public Operation(OperationType type, params object[] args)
            {
                opType = type;
                opArgs = new object[args.Length];
                Array.Copy(args, opArgs, args.Length);
            }
        }

    }

    #region delegate and event args for AudioInput

    public class InputDisposedEventArgs : EventArgs
    {
        public readonly string DeviceId;
        public InputDisposedEventArgs(string devId)
        {
            DeviceId = devId;
        }
    }
    public class DeviceInfoUpdatedEventArgs : EventArgs
    {
        public readonly List<DeviceInfo> DeviceInfo;
        public DeviceInfoUpdatedEventArgs(List<DeviceInfo> info)
        {
            DeviceInfo = info;
        }
    }
    public class DeviceSelectedEventArgs : EventArgs
    {
        public readonly MMDevice Device;
        public readonly int Index;
        public DeviceSelectedEventArgs(MMDevice dev, int index)
        {
            Device = dev;
            Index = index;
        }
    }

    public class CaptureStartedEventArgs : EventArgs
    {
        public readonly string DeviceId;
        public CaptureStartedEventArgs(string devId)
        {
            DeviceId = devId;
        }
    }
    public class CaptureStoppedEventArgs : EventArgs
    {
        public readonly string DeviceId;
        public CaptureStoppedEventArgs(string devId)
        {
            DeviceId = devId;
        }
    }

    public class DataUpdatedEventArgs : EventArgs
    {
        public readonly string DeviceId;
        private readonly int _sampleNum;
        private readonly byte[] _bytes;
        private readonly double[][] _data;
        public DataUpdatedEventArgs(string devId, int sampleNum, byte[] bytes, double[][] data)
        {
            DeviceId = devId;
            _sampleNum = sampleNum;
            _bytes = bytes;
            _data = data;
        }
        /// <summary>サンプル数</summary>
        public int SampleNum { get { return _sampleNum; } }
        /// <summary>生のバイトデータ</summary>
        public byte[] RawData { get { return _bytes; } }
        /// <summary>チャンネルごと、-1～1に正規化した浮動小数点数データ</summary>
        public double[][] Data { get { return _data; } }
    }

    public class VolumeChangedEventArgs : EventArgs
    {
        public readonly string DeviceId;
        public readonly double Master;
        public readonly double[] Channels;
        public VolumeChangedEventArgs(string devId, double master, double[] channels)
        {
            DeviceId = devId;
            Master = master;
            Channels = new double[channels.Length];
            Array.Copy(channels, Channels, channels.Length);
        }
    }

    public class ErrorEventArgs : EventArgs
    {
        public readonly string DeviceId;
        private readonly Exception _exception;
        public ErrorEventArgs(string devId, Exception ex)
        {
            DeviceId = devId;
            _exception = ex;
        }
        public Exception Exception { get { return _exception; } }
    }
    #endregion

    public class DeviceInfo
    {
        private readonly string _friendlyName;
        public string FriendlyName { get { return _friendlyName; } }

        private readonly string _deviceId;
        public string DeviceId { get { return _deviceId; } }

        private readonly EDataFlow _dataFlow;
        public EDataFlow DataFlow { get { return _dataFlow; } }

        private readonly DeviceState _state;
        public DeviceState State { get { return _state; } }

        private readonly WAVEFORMATEXTENSIBLE _format = null;
        public WAVEFORMATEXTENSIBLE Format { get { return _format; } }

        internal DeviceInfo(string deviceId, string name, EDataFlow flow, DeviceState state, WAVEFORMATEXTENSIBLE format)
        {
            _deviceId = deviceId;
            _friendlyName = name;
            _dataFlow = flow;
            _state = state;
            _format = new WAVEFORMATEXTENSIBLE(format);
        }

        public override string ToString()
        {
            return string.Format("<{0}> {1} : {2} ({3})",
                _dataFlow == EDataFlow.eCapture ? "録音" : "再生",
                _friendlyName, _state, _deviceId);
        }
    }

    public class SessionInfo
    {
        private string _displayName;
        public string DisplayName { get { return _displayName; } }

        private uint _processId;
        public uint ProcessId { get { return _processId; } }

        private Guid _guid;
        public Guid Guid { get { return _guid; } }

        public SessionInfo(uint procId, Guid guid, string name)
        {
            _processId = procId;
            _guid = guid;
            _displayName = name;
        }

        public override string ToString()
        {
            return string.Format("[{0}]{1}({2})",
                _processId, _displayName, _guid);
        }
    }
}
