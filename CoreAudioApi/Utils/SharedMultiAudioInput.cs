using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace CoreAudioApi.Utils
{
    /// <summary>
    /// 共有モードで複数のソースから音声取得をする
    /// <remarks>中でSharedAudioInputを複数用意しているだけ、デバイス情報のイベントは1個だけ採用</remarks>
    /// </summary>
    public class SharedMultiAudioInput : IDisposable
    {
        public event EventHandler<DeviceInfoUpdatedEventArgs> DeviceInfoUpdated;
        public event EventHandler<InputDisposedEventArgs> InputDisposed;
        public event EventHandler<DeviceSelectedEventArgs> DeviceSelected;
        public event EventHandler<CaptureStartedEventArgs> CaptureStarted;
        public event EventHandler<CaptureStoppedEventArgs> CaptureStopped;
        public event EventHandler<DataUpdatedEventArgs> DataUpdated;
        public event EventHandler<VolumeChangedEventArgs> VolumeChanged;
        public event EventHandler<ErrorEventArgs> ErrorOccured;

        private List<SharedAudioInput> _inputs = new List<SharedAudioInput>();
        private SharedAudioInput _defaultInput;

        // デバイス情報
        private List<DeviceInfo> _deviceInfos = new List<DeviceInfo>();
        public List<DeviceInfo> DeviceInfos { get { return new List<DeviceInfo>(_deviceInfos); } }

        public SharedMultiAudioInput()
        {
            _defaultInput = new SharedAudioInput();

            _defaultInput.DeviceInfoUpdated += (s, e) =>
            {
                _deviceInfos = e.DeviceInfo;

                // イベント発火
                var del = DeviceInfoUpdated;
                if (del != null)
                {
                    List<DeviceInfo> info = _deviceInfos;
                    del.Invoke(this, new DeviceInfoUpdatedEventArgs(info));
                }
            };

            setupInput(_defaultInput);
        }

        public void Dispose()
        {
            lock (_inputs)
            {
                foreach (var input in _inputs)
                {
                    if (input != null) input.Dispose();
                }
            }
            if (_defaultInput != null) _defaultInput.Dispose();
        }

        private SharedAudioInput getInput(string devId)
        {
            SharedAudioInput input = null;
            lock (_inputs)
            {
                input = _inputs.Find(di => di.SelectedDeviceId == devId);
            }
            return input;
        }

        public DeviceInfo getDeviceInfo(string devId)
        {
            return _deviceInfos.Find(di => di.DeviceId == devId);
        }

        public WAVEFORMATEXTENSIBLE GetCapFormat(string devId)
        {
            SharedAudioInput input = getInput(devId);
            if(input != null) return input.CapFormat;
            return null;
        }

        public bool Capturing(string devId)
        {
            SharedAudioInput input = getInput(devId);
            if (input != null) return input.Capturing;
            else return false;
        }

        public void UpdateDeviceInfo(bool needStop = false)
        {
            _defaultInput.UpdateDeviceInfo(needStop);
        }

        public void SelectDevice(string devId)
        {
            lock (_inputs)
            {
                if (_inputs.Count == 0)
                {
                    _defaultInput.SelectDevice(devId);
                    _inputs.Add(_defaultInput);
                }
                else
                {
                    SharedAudioInput input = new SharedAudioInput();
                    _inputs.Add(input);
                    setupInput(input);
                    input.SelectDevice(devId);
                }
            }
        }

        public void ReleaseDevice(string devId)
        {
            SharedAudioInput input = getInput(devId);
            if (input != null) input.ReleaseDevice();
            lock (_inputs)
            {
                if (input != _defaultInput)
                {
                    input.Dispose();
                    _inputs.Remove(input);
                }
            }
        }

        public void StopCapture(string devId)
        {
            SharedAudioInput input = getInput(devId);
            if (input != null) input.StopCapture();
        }

        public void StartCapture(string devId)
        {
            SharedAudioInput input = getInput(devId);
            if (input != null) input.StartCapture();
        }

        private void setupInput(SharedAudioInput input)
        {
            input.InputDisposed += (s, e) =>
            {
                var h = InputDisposed;
                if (h != null)
                {
                    h(this, new InputDisposedEventArgs(e.DeviceId));
                }
            };

            input.DeviceSelected += (s, e) =>
            {
                var h = DeviceSelected;
                if (h != null)
                {
                    h(this, new DeviceSelectedEventArgs(e.Device, e.Index));
                }
            };
            input.CaptureStarted += (s, e) =>
            {
                var h = CaptureStarted;
                if (h != null)
                {
                    h(this, new CaptureStartedEventArgs(e.DeviceId));
                }
            };
            input.CaptureStopped += (s, e) =>
            {
                var h = CaptureStopped;
                if (h != null)
                {
                    h(this, new CaptureStoppedEventArgs(e.DeviceId));
                }
            };

            input.DataUpdated += (s, e) =>
            {
                var h = DataUpdated;
                if (h != null)
                {
                    byte[] rawData = new byte[e.RawData.Length];
                    Array.Copy(e.RawData, rawData, e.RawData.Length);
                    List<double[]> data = new List<double[]>();
                    foreach (var d in e.Data)
                    {
                        double[] ds = new double[d.Length];
                        Array.Copy(d, ds, d.Length);
                        data.Add(ds);
                    }
                    h(this, new DataUpdatedEventArgs(e.DeviceId, e.SampleNum, rawData, data.ToArray()));
                }
            };
            input.VolumeChanged += (s, e) =>
            {
                var h = VolumeChanged;
                if (h != null)
                {
                    h(this, new VolumeChangedEventArgs(e.DeviceId, e.Master, e.Channels));
                }
            };

            input.ErrorOccured += (s, e) =>
            {
                var h = ErrorOccured;
                if (h != null)
                {
                    h(this, new ErrorEventArgs(e.DeviceId, e.Exception));
                }
            };
        }
    }
}
