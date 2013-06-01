using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi
{
    /// <summary>
    /// 波形オーディオ データのフォーマット
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class WAVEFORMATEXTENSIBLE
    {
        /// <summary>
        /// 波形オーディオのフォーマット タイプ
        /// </summary>
        public WaveFormatTag wFormatTag;
        /// <summary>
        /// 波形オーディオ データに含まれるチャンネル数
        /// </summary>
        public ushort nChannels;
        /// <summary>
        /// サンプル/秒で表すサンプル レート (単位 Hz)
        /// </summary>
        public uint nSamplesPerSec;
        /// <summary>
        /// フォーマット タグに必要な平均データ転送レート (単位 バイト/秒)
        /// </summary>
        public uint nAvgBytesPerSec;
        /// <summary>
        /// ブロック アラインメント (単位 バイト)。ブロック アラインメントとは、wFormatTag フォーマット タイプのデータの最小構成単位。
        /// <remarks>
        /// ソフトウェアは、一度に複数の nBlockAlign バイトのデータを処理する必要がある
        /// </remarks>
        /// </summary>
        public ushort nBlockAlign;
        /// <summary>
        /// wFormatTag フォーマット タイプの 1 サンプルあたりのビット数
        /// </summary>
        public ushort wBitsPerSample;
        /// <summary>
        /// WAVEFORMATEX 構造体の最後に追加される追加フォーマット情報のサイズ (単位バイト)。非 PCM フォーマットは、この情報を使って wFormatTag の追加属性を格納できる。wFormatTag に追加情報が必要ない場合は、このメンバはゼロに設定しなければならない。
        /// </summary>
        public ushort cbSize;

        /// <summary>
        /// 信号の精度のビット数。通常は WAVEFORMATEX.wBitsPerSample と等しい。
        /// </summary>
        public ushort wValidBitsPerSample;
        /// <summary>
        /// オーディオ データの 1 つの圧縮ブロックに含まれるサンプル数。
        /// </summary>
        public ChannelMask dwChannelMask;
        /// <summary>
        /// スピーカ位置へのストリーム内のチャンネル割り当てを指定するビットマスク。
        /// </summary>
        public Guid SubFormat;

        public WAVEFORMATEXTENSIBLE()
        {
        }
        public WAVEFORMATEXTENSIBLE(WAVEFORMATEXTENSIBLE o)
        {
            wFormatTag = o.wFormatTag;
            nChannels = o.nChannels;
            nSamplesPerSec = o.nSamplesPerSec;
            nAvgBytesPerSec = o.nAvgBytesPerSec;
            nBlockAlign = o.nBlockAlign;
            wBitsPerSample = o.wBitsPerSample;
            cbSize = o.cbSize;
            wValidBitsPerSample = o.wValidBitsPerSample;
            dwChannelMask = o.dwChannelMask;
            SubFormat = o.SubFormat;
        }
    }

    public enum ChannelMask : uint
    {
        SPEAKER_FRONT_LEFT = 0x1,
        SPEAKER_FRONT_RIGHT = 0x2,
        SPEAKER_FRONT_CENTER = 0x4,
        SPEAKER_LOW_FREQUENCY = 0x8,
        SPEAKER_BACK_LEFT = 0x10,
        SPEAKER_BACK_RIGHT = 0x20,
        SPEAKER_FRONT_LEFT_OF_CENTER = 0x40,
        SPEAKER_FRONT_RIGHT_OF_CENTER = 0x80,
        SPEAKER_BACK_CENTER = 0x100,
        SPEAKER_SIDE_LEFT = 0x200,
        SPEAKER_SIDE_RIGHT = 0x400,
        SPEAKER_TOP_CENTER = 0x800,
        SPEAKER_TOP_FRONT_LEFT = 0x1000,
        SPEAKER_TOP_FRONT_CENTER = 0x2000,
        SPEAKER_TOP_FRONT_RIGHT = 0x4000,
        SPEAKER_TOP_BACK_LEFT = 0x8000,
        SPEAKER_TOP_BACK_CENTER = 0x10000,
        SPEAKER_TOP_BACK_RIGHT = 0x20000,
    }

    public enum WaveFormatTag : ushort
    {
        WAVE_FORMAT_UNKNOWN = 0x0000,
        WAVE_FORMAT_PCM = 0x0001,
        WAVE_FORMAT_MS_ADPCM = 0x0002,
        WAVE_FORMAT_IEEE_FLOAT = 0x0003,
        WAVE_FORMAT_VSELP = 0x0004,
        WAVE_FORMAT_IBM_CVSD = 0x0005,
        WAVE_FORMAT_ALAW = 0x0006,
        WAVE_FORMAT_MULAW = 0x0007,
        WAVE_FORMAT_OKI_ADPCM = 0x0010,
        WAVE_FORMAT_IMA_ADPCM = 0x0011,
        WAVE_FORMAT_MEDIASPACE_ADPCM = 0x0012,
        WAVE_FORMAT_SIERRA_ADPCM = 0x0013,
        WAVE_FORMAT_G723_ADPCM = 0x0014,
        WAVE_FORMAT_DIGISTD = 0x0015,
        WAVE_FORMAT_DIGIFIX = 0x0016,
        WAVE_FORMAT_DIALOGIC_OKI_ADPCM = 0x0017,
        WAVE_FORMAT_MEDIAVISION_ADPCM = 0x118,
        WAVE_FORMAT_CU_CODEC = 0x0019,
        WAVE_FORMAT_YAMAHA_ADPCM = 0x0020,
        WAVE_FORMAT_SONARC = 0x0021,
        WAVE_FORMAT_DSPGROUP_TRUESPEECH = 0x0022,
        WAVE_FORMAT_ECHOSC1 = 0x0023,
        WAVE_FORMAT_AUDIOFILE_AF36 = 0x0024,
        WAVE_FORMAT_APTX = 0x0025,
        WAVE_FORMAT_AUDIOFILE_AF10 = 0x0026,
        WAVE_FORMAT_PROSODY_1612 = 0x0027,
        WAVE_FORMAT_LRC = 0x0028,
        WAVE_FORMAT_DOLBY_AC2 = 0x0030,
        WAVE_FORMAT_GSM610 = 0x0031,
        WAVE_FORMAT_MSNAUDIO = 0x0032,
        WAVE_FORMAT_ANTEX_ADPCME = 0x0033,
        WAVE_FORMAT_CONTROL_RES_VQLPC = 0x0034,
        WAVE_FORMAT_DIGIREAL = 0x0035,
        WAVE_FORMAT_DIGIADPCM = 0x0036,
        WAVE_FORMAT_CONTROL_RES_CR10 = 0x0037,
        WAVE_FORMAT_NMS_VBXADPCM = 0x0038,
        WAVE_FORMAT_ROLAND_RDAC = 0x0039,
        WAVE_FORMAT_ECHOSC3 = 0x003A,
        WAVE_FORMAT_ROCKWELL_ADPCM = 0x003B,
        WAVE_FORMAT_ROCKWELL_DIGITALK = 0x003C,
        WAVE_FORMAT_XEBEC = 0x003D,
        WAVE_FORMAT_G721_ADPCM = 0x0040,
        WAVE_FORMAT_G728_CELP = 0x0041,
        WAVE_FORMAT_MSG723 = 0x0042,
        WAVE_FORMAT_MPEG = 0x0050,
        WAVE_FORMAT_RT24 = 0x0052,
        WAVE_FORMAT_PAC = 0x0053,
        WAVE_FORMAT_MPEGLAYER3 = 0x0055,
        WAVE_FORMAT_LUCENT_G723 = 0x0059,
        WAVE_FORMAT_CIRRUS = 0x0060,
        WAVE_FORMAT_ESPCM = 0x0061,
        WAVE_FORMAT_VOXWARE = 0x0062,
        WAVE_FORMAT_CANOPUS_ATRAC = 0x0063,
        WAVE_FORMAT_G726_ADPCM = 0x0064,
        WAVE_FORMAT_G722_ADPCM = 0x0065,
        WAVE_FORMAT_DSAT = 0x0066,
        WAVE_FORMAT_DSAT_DISPLAY = 0x0067,
        WAVE_FORMAT_VOXWARE_BYTE_ALIGNED = 0x0069,
        WAVE_FORMAT_VOXWARE_AC8 = 0x0070,
        WAVE_FORMAT_VOXWARE_AC10 = 0x0071,
        WAVE_FORMAT_VOXWARE_AC16 = 0x0072,
        WAVE_FORMAT_VOXWARE_AC20 = 0x0073,
        WAVE_FORMAT_VOXWARE_RT24 = 0x0074,
        WAVE_FORMAT_VOXWARE_RT29 = 0x0075,
        WAVE_FORMAT_VOXWARE_RT29HW = 0x0076,
        WAVE_FORMAT_VOXWARE_VR12 = 0x0077,
        WAVE_FORMAT_VOXWARE_VR18 = 0x0078,
        WAVE_FORMAT_VOXWARE_TQ40 = 0x0079,
        WAVE_FORMAT_SOFTSOUND = 0x0080,
        WAVE_FORMAT_VOXARE_TQ60 = 0x0081,
        WAVE_FORMAT_MSRT24 = 0x0082,
        WAVE_FORMAT_G729A = 0x0083,
        WAVE_FORMAT_MVI_MV12 = 0x0084,
        WAVE_FORMAT_DF_G726 = 0x0085,
        WAVE_FORMAT_DF_GSM610 = 0x0086,
        WAVE_FORMAT_ONLIVE = 0x0089,
        WAVE_FORMAT_SBC24 = 0x0091,
        WAVE_FORMAT_DOLBY_AC3_SPDIF = 0x0092,
        WAVE_FORMAT_ZYXEL_ADPCM = 0x0097,
        WAVE_FORMAT_PHILIPS_LPCBB = 0x0098,
        WAVE_FORMAT_PACKED = 0x0099,
        WAVE_FORMAT_RHETOREX_ADPCM = 0x0100,
        IBM_FORMAT_MULAW = 0x0101,
        IBM_FORMAT_ALAW = 0x0102,
        IBM_FORMAT_ADPCM = 0x0103,
        WAVE_FORMAT_VIVO_G723 = 0x0111,
        WAVE_FORMAT_VIVO_SIREN = 0x0112,
        WAVE_FORMAT_DIGITAL_G723 = 0x0123,
        WAVE_FORMAT_CREATIVE_ADPCM = 0x0200,
        WAVE_FORMAT_CREATIVE_FASTSPEECH8 = 0x0202,
        WAVE_FORMAT_CREATIVE_FASTSPEECH10 = 0x0203,
        WAVE_FORMAT_QUARTERDECK = 0x0220,
        WAVE_FORMAT_FM_TOWNS_SND = 0x0300,
        WAVE_FORMAT_BZV_DIGITAL = 0x0400,
        WAVE_FORMAT_VME_VMPCM = 0x0680,
        WAVE_FORMAT_OLIGSM = 0x1000,
        WAVE_FORMAT_OLIADPCM = 0x1001,
        WAVE_FORMAT_OLICELP = 0x1002,
        WAVE_FORMAT_OLISBC = 0x1003,
        WAVE_FORMAT_OLIOPR = 0x1004,
        WAVE_FORMAT_LH_CODEC = 0x1100,
        WAVE_FORMAT_NORRIS = 0x1400,
        WAVE_FORMAT_SOUNDSPACE_MUSICOMPRESS = 0x1500,
        WAVE_FORMAT_DVM = 0x2000,
        WAVE_FORMAT_INTERWAV_VSC112 = 0x7150,
        WAVE_FORMAT_EXTENSIBLE = 0xFFFE,
    }
}
