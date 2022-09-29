using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFC_Core;

public interface IDevice
{
    public Enums.DeviceStatus Status { get; }
    public int FirmwareVersion { get; }

    #region Connection

    public bool Connect(string comPort);
    public bool Disconnect();

    #endregion

    #region Sensors

    public bool GetSensorValues(out Models.SensorValues sensorValues);

    #endregion

    #region Controller

    public bool SetFanDuty(int fanId, int fanDuty);

    #endregion
}