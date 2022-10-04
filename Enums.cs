using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace EFC_Core;

public static class Enums
{

    #region Device

    public enum DeviceStatus
    {
        DISCONNECTING,
        DISCONNECTED,
        CONNECTING,
        CONNECTED,
        ERROR
    }

    #endregion

    #region Core
    public enum HardwareType {
        EFC_X9_V1
    }

    public enum SensorType {
        Temperature,
        Humidity,
        Duty,
        Revolutions,
        Voltage,
        Current,
        Power
    }

    public enum DeviceConfigItemType {
        List,
        Integer,
        Float
    }
    #endregion
}