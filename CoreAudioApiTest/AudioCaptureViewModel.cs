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
    class AudioCaptureViewModel : ViewModelBase, IDisposable
    {
        private SharedAudioInput _audioInput = null;
        private WaveFileWriter[] _wavWriters;
        private WaveFileWriter _wavRawWriter;
        private System.IO.BinaryWriter _writer;

        public AudioCaptureViewModel()
        {
            _audioInput = new SharedAudioInput();

            _audioInput.DeviceInfoUpdated += (s, e) =>
            {
                Devices = new ObservableCollection<DeviceInfoViewModel>(
                    e.DeviceInfo.FindAll(di => true)//di.DataFlow == EDataFlow.eCapture)
                    .Select(di => new DeviceInfoViewModel(di)));
                addMessage("Devices updated");
            };

            _audioInput.DeviceSelected += (s, e) =>
            {
                addMessage("Device selected: " + e.Device.FriendlyName);
            };
            _audioInput.CaptureStarted += (s, e) =>
            {
                addMessage("Capture started");
            };
            _audioInput.CaptureStopped += (s, e) =>
            {
                addMessage("Capture stopped");

                if (_wavWriters != null)
                {
                    foreach (var wr in _wavWriters) wr.Close();
                }
                _wavWriters = null;
                if (_wavRawWriter != null)
                {
                    _wavRawWriter.Close(); _wavRawWriter = null;
                }
                if (_writer != null) { _writer.Close(); _writer = null; }
            };

            _audioInput.DataUpdated += (s, e) =>
            {
                if (_wavWriters != null)
                {
                    for (int i = 0; i < _wavWriters.Length; i++)
                        _wavWriters[i].Write(e.Data[i]);
                }
                if (_wavRawWriter != null)
                {
                    _wavRawWriter.WriteRawData(e.RawData);
                }
                if (_writer != null) _writer.Write(e.RawData);
            };
            _audioInput.VolumeChanged += (s, e) =>
            {
                Volume = e.Master;
            };

            _audioInput.ErrorOccured += (s, e) =>
            {
                addMessage(e.Exception.Message);
            };

            #region initialize commands
            StartCaptureCommand = new DelegateCommand(
                () =>
                {
                    if (_wavWriters != null)
                    {
                        foreach (var wr in _wavWriters) wr.Close();
                    }

                    _wavWriters = new WaveFileWriter[_audioInput.CapFormat.nChannels];
                    WAVEFORMATEXTENSIBLE fmt = new WAVEFORMATEXTENSIBLE(_audioInput.CapFormat);
                    fmt.wFormatTag = WaveFormatTag.WAVE_FORMAT_PCM;
                    fmt.nChannels = 1;
                    fmt.wBitsPerSample = 16;
                    fmt.wValidBitsPerSample = 16;
                    fmt.nAvgBytesPerSec = (uint)(fmt.nChannels * fmt.nSamplesPerSec * fmt.wBitsPerSample / 8.0);

                    for (int i = 0; i < _wavWriters.Length; i++)
                    {
                        _wavWriters[i] = new WaveFileWriter(fmt,
                            DateTime.Now.ToString("yyyyMMdd-HHmmss") + string.Format("_{0}.wav", i));
                    }

                    WAVEFORMATEXTENSIBLE rawFmt = new WAVEFORMATEXTENSIBLE(_audioInput.CapFormat);
                    _wavRawWriter = new WaveFileWriter(rawFmt, DateTime.Now.ToString("yyyyMMdd-HHmmss") + "_raw.wav");

                    _writer = new System.IO.BinaryWriter(new System.IO.FileStream("test.wav", System.IO.FileMode.Create));

                    _audioInput.StartCapture();
                },
                () => { return _selectedDev != null && !_audioInput.Capturing 
                    && _selectedDev.State == DeviceState.DEVICE_STATE_ACTIVE; });

            StopCaptureCommand = new DelegateCommand(
                () =>
                {
                    _audioInput.StopCapture();
                },
                () => { return SelectedDev != null && _audioInput.Capturing; });
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

        private double _volume = 0;
        public double Volume
        {
            get { return _volume; }
            set
            {
                _volume = value;
                RaisePropertyChanged("Volume");
            }
        }

        private DeviceInfoViewModel _selectedDev = null;
        public DeviceInfoViewModel SelectedDev
        {
            get { return _selectedDev; }
            set
            {
                _selectedDev = value;
                RaisePropertyChanged("SelectedDev");
                if (_selectedDev != null)
                {
                    _audioInput.SelectDevice(_selectedDev.DeviceId);
                }
            }
        }

        private DelegateCommand _startCapCommand = null;
        public DelegateCommand StartCaptureCommand
        {
            get { return _startCapCommand; }
            private set
            {
                _startCapCommand = value;
                RaisePropertyChanged("StartCaptureCommand");
            }
        }
        private DelegateCommand _stopCapCommand = null;
        public DelegateCommand StopCaptureCommand
        {
            get { return _stopCapCommand; }
            private set
            {
                _stopCapCommand = value;
                RaisePropertyChanged("StopCaptureCommand");
            }
        }

        private void addMessage(string msg)
        {
            if (App.Current == null) return;
            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Messages.Insert(0, msg);
            }));
        }
    }

    class DeviceInfoViewModel : ViewModelBase
    {
        private DeviceInfo _dev;
        public DeviceInfo DeviceInfo
        {
            get { return _dev; }
            private set
            {
                _dev = value;
                RaisePropertyChanged("DeviceInfo");
            }
        }

        private bool _capturing;
        public bool IsCapturing
        {
            get { return _capturing; }
            private set
            {
                _capturing = value;
                RaisePropertyChanged("IsCapturing");
            }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            private set
            {
                _name = value;
                RaisePropertyChanged("Name");
            }
        }

        private string _devId;
        public string DeviceId
        {
            get { return _devId; }
            private set
            {
                _devId = value;
                RaisePropertyChanged("DeviceId");
            }
        }

        private DeviceState _state;
        public DeviceState State
        {
            get { return _state; }
            private set
            {
                _state = value;
                RaisePropertyChanged("State");
            }
        }

        private int _channels;
        public int Channels
        {
            get { return _channels; }
            private set
            {
                _channels = value;
                RaisePropertyChanged("Channels");
            }
        }

        private uint _samplesPerSec;
        public uint SamplesPerSec
        {
            get { return _samplesPerSec; }
            set
            {
                _samplesPerSec = value;
                RaisePropertyChanged("SamplesPerSec");
            }
        }

        public DeviceInfoViewModel(DeviceInfo di)
        {
            DeviceInfo = di;
            Name = di.FriendlyName;
            DeviceId = di.DeviceId;
            State = di.State;
            IsCapturing = false;
            Channels = di.Format.nChannels;
            SamplesPerSec = di.Format.nSamplesPerSec;
        }
    }
}
