using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace CoreAudioApiTest
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private AudioCaptureViewModel avm;
        private MultiCaptureViewModel mvm;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //avm = new AudioCaptureViewModel();
            //AudioCaptureWindow mw = new AudioCaptureWindow();
            //mw.DataContext = avm;
            //mw.Closed += (s, ev) =>
            //{
            //    avm.Dispose();
            //};
            //mw.Show();

            mvm = new MultiCaptureViewModel();
            MultiCaptureWindow mw = new MultiCaptureWindow();
            mw.DataContext = mvm;
            mw.Closed += (s, ev) =>
            {
                mvm.Dispose();
            };
            mw.Show();
        }
    }
}
