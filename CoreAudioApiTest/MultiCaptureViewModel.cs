using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Diagnostics;

using CoreAudioApi;
using CoreAudioApi.Utils;

namespace CoreAudioApiTest
{
    class MultiCaptureViewModel : ViewModelBase, IDisposable
    {
        private SharedMultiAudioInput _audioInput;
        private Dictionary<string, WaveFileWriter> _wavWriters = new Dictionary<string, WaveFileWriter>();
        private Dictionary<string, bool> _selecting = new Dictionary<string, bool>();

        public MultiCaptureViewModel()
        {
            //ExclusiveAudioInput ai = new ExclusiveAudioInput();

            _audioInput = new SharedMultiAudioInput();

            _audioInput.DeviceInfoUpdated += (s, e) =>
            {
                Devices = new ObservableCollection<DeviceInfoViewModel>(
                    e.DeviceInfo.FindAll(di => true)//di.DataFlow == EDataFlow.eCapture)
                    .Select(di => new DeviceInfoViewModel(di)));
                addMessage("Devices updated");
                
                Volumes = new ObservableCollection<VolumeViewModel>(Devices.Select(di => new VolumeViewModel(di.DeviceId)));
                foreach (var di in e.DeviceInfo)
                {
                    if (!_selecting.ContainsKey(di.DeviceId))
                        _selecting.Add(di.DeviceId, false);
                }
            };

            _audioInput.DeviceSelected += (s, e) =>
            {
                addMessage("Device selected: " + e.Device.Id + "/" + e.Device.FriendlyName);
                _selecting[e.Device.Id] = true;
            };
            _audioInput.CaptureStarted += (s, e) =>
            {
                addMessage("Capture started: " + e.DeviceId);
            };
            _audioInput.CaptureStopped += (s, e) =>
            {
                addMessage("Capture stopped: " + e.DeviceId);
                lock (_wavWriters)
                {
                    if (_wavWriters.ContainsKey(e.DeviceId))
                    {
                        _wavWriters[e.DeviceId].Close();
                    }
                }
            };

            _audioInput.DataUpdated += (s, e) =>
            {
                lock (_wavWriters)
                {
                    if (_wavWriters.ContainsKey(e.DeviceId))
                    {
                        _wavWriters[e.DeviceId].Write(e.Data);
                    }
                }
            };
            _audioInput.VolumeChanged += (s, e) =>
            {
                foreach (var v in Volumes)
                {
                    if (v.DeviceId == e.DeviceId)
                    {
                        v.Master = e.Master;
                    }
                }
            };

            _audioInput.ErrorOccured += (s, e) =>
            {
                addMessage(e.Exception.Message);
            };

            #region initialize commands
            SelectCommand = new DelegateCommand<string>(
                devId => _audioInput.SelectDevice(devId),
                devId => devId != null && _selecting.ContainsKey(devId) && !_selecting[devId] && !_audioInput.Capturing(devId));
            ReleaseCommand = new DelegateCommand<string>(
                devId =>
                {
                    _audioInput.ReleaseDevice(devId);
                    _selecting[devId] = false;
                },
                devId => devId != null && _selecting.ContainsKey(devId) && _selecting[devId] && !_audioInput.Capturing(devId));

            StartCaptureCommand = new DelegateCommand<string>(
                devId =>
                {
                    lock (_wavWriters)
                    {
                        if (_wavWriters.ContainsKey(devId))
                        {
                            _wavWriters[devId].Close();
                            _wavWriters.Remove(devId);
                        }
                    }

                    WAVEFORMATEXTENSIBLE fmt = new WAVEFORMATEXTENSIBLE(_audioInput.GetCapFormat(devId));
                    fmt.wFormatTag = WaveFormatTag.WAVE_FORMAT_PCM;
                    fmt.wBitsPerSample = 16;
                    fmt.wValidBitsPerSample = 16;
                    fmt.nAvgBytesPerSec = (uint)(fmt.nChannels * fmt.nSamplesPerSec * fmt.wBitsPerSample / 8.0);

                    WaveFileWriter writer = new WaveFileWriter(fmt.nChannels, (int)fmt.nSamplesPerSec, fmt.wBitsPerSample, 
                        string.Format("{0}.wav", devId));
                    lock (_wavWriters)
                    {
                        _wavWriters.Add(devId, writer);
                    }

                    _audioInput.StartCapture(devId);
                },
                devId =>
                {
                    if (devId != null && !_audioInput.Capturing(devId))
                    {
                        DeviceInfo di = _audioInput.getDeviceInfo(devId);
                        return _selecting.ContainsKey(devId) && _selecting[devId] &&
                            di != null && di.State == DeviceState.DEVICE_STATE_ACTIVE;
                    }
                    return false;
                });

            StopCaptureCommand = new DelegateCommand<string>(
                devId => _audioInput.StopCapture(devId),
                devId =>
                {
                    return devId != null && _audioInput.Capturing(devId);
                });
            #endregion

            Devices = new ObservableCollection<DeviceInfoViewModel>();
            _audioInput.UpdateDeviceInfo();
        }

        public void Dispose()
        {
            if (_audioInput != null)
            {
                _audioInput.Dispose();
            }
        }

        private ObservableCollection<string> _msgs = new ObservableCollection<string>();
        public ObservableCollection<string> Messages
        {
            get { return _msgs; }
            private set
            {
                _msgs = value;
                RaisePropertyChanged("Messages");
            }
        }

        private ObservableCollection<DeviceInfoViewModel> _devices;
        public ObservableCollection<DeviceInfoViewModel> Devices
        {
            get { return _devices; }
            private set
            {
                _devices = value;
                RaisePropertyChanged("Devices");
            }
        }

        private ObservableCollection<VolumeViewModel> _volumes = new ObservableCollection<VolumeViewModel>();
        public ObservableCollection<VolumeViewModel> Volumes
        {
            get { return _volumes; }
            private set
            {
                _volumes = value;
                RaisePropertyChanged("Volumes");
            }
        }

        public DelegateCommand<string> SelectCommand { get; private set; }
        public DelegateCommand<string> ReleaseCommand { get; private set; }
        public DelegateCommand<string> StartCaptureCommand { get; private set; }
        public DelegateCommand<string> StopCaptureCommand { get; private set; }

        private void addMessage(string msg)
        {
            if (App.Current == null) return;
            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Messages.Insert(0, msg);
            }));
        }
    }

    class VolumeViewModel : ViewModelBase
    {
        private string _deviceId;
        public string DeviceId { get { return _deviceId; } }

        private double _master;
        public double Master
        {
            get { return _master; }
            set
            {
                _master = value;
                RaisePropertyChanged("Master");
            }
        }

        public VolumeViewModel(string devId)
        {
            _deviceId = devId;
            Master = 0;
        }
    }
}
