using System;
using System.Collections.Generic;
using System.Text;

namespace CoreAudioApi
{
    [Flags]
    public enum DeviceState : uint
    {
        /// <summary>
        /// The audio endpoint device is active. That is, the audio adapter that connects to the endpoint device is present and enabled. In addition, if the endpoint device plugs into a jack on the adapter, then the endpoint device is plugged in.
        /// </summary>
        DEVICE_STATE_ACTIVE = 0x00000001,
        /// <summary>
        /// The audio endpoint device is disabled. The user has disabled the device in the Windows multimedia control panel, Mmsys.cpl. For more information, see Remarks.
        /// </summary>
        DEVICE_STATE_DISABLED = 0x00000002,
        /// <summary>
        /// The audio endpoint device is not present because the audio adapter that connects to the endpoint device has been removed from the system, or the user has disabled the adapter device in Device Manager.
        /// </summary>
        DEVICE_STATE_NOTPRESENT = 0x00000004,
        /// <summary>
        /// The audio endpoint device is unplugged. The audio adapter that contains the jack for the endpoint device is present and enabled, but the endpoint device is not plugged into the jack. Only a device with jack-presence detection can be in this state. 
        /// </summary>
        DEVICE_STATE_UNPLUGGED = 0x00000008,
        /// <summary>
        /// Includes audio endpoint devices in all states: active, disabled, not present, and unplugged.
        /// </summary>
        DEVICE_STATEMASK_ALL = 0x0000000F,
    }
}
