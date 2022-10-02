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
    #region UART

    public enum UART_CMD
    {
        UART_CMD_WELCOME,
        UART_CMD_READ_ID,
        UART_CMD_RSVD1,
        UART_CMD_READ_SENSOR_VALUES,
        UART_CMD_READ_CONFIG,
        UART_CMD_WRITE_CONFIG,
        UART_CMD_RSVD2,
        UART_CMD_RSVD3,
        UART_CMD_RSVD4,
        UART_CMD_WRITE_FAN_DUTY,
        UART_CMD_DISPLAY_SW_DISABLE = 0xE0,
        UART_CMD_DISPLAY_SW_ENABLE = 0xE1,
        UART_CMD_DISPLAY_SW_WRITE = 0xE2,
        UART_CMD_DISPLAY_SW_UPDATE = 0xE3,
        UART_CMD_RESET = 0xF0,
        UART_CMD_BOOTLOADER = 0xF1,
        UART_CMD_NVM_CONFIG = 0xF2,
        UART_CMD_NOP = 0xFF
    }

    public static byte[] ToByteArray(this UART_CMD uartCMD, int len = 0)
    {
        byte[] returnArray = new byte[len + 1];
        returnArray[0] = (byte)uartCMD;
        return returnArray;
    }

    #endregion

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