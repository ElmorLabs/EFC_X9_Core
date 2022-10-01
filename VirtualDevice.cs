using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFC_Core
{
    public class VirtualDevice : IDevice
    {
        public Enums.DeviceStatus Status { get; private set; } = Enums.DeviceStatus.DISCONNECTED;
        public int FirmwareVersion { get; private set; } = 0;

        Random rnd = new Random();
        byte[] fan_duties = new byte[EFC_Def.FAN_NUM];

        public bool Connect(string comPort)
        {
            Status = Enums.DeviceStatus.CONNECTED;
            return true;
        }

        public bool Disconnect()
        {
            Status = Enums.DeviceStatus.DISCONNECTED;
            return true;
        }

        public bool GetSensorValues(out Models.SensorValues sensorValues)
        {

            sensorValues = new Models.SensorValues()
            {
                ThermalSensor1 = (200 + rnd.Next(0, 200))/10.0f,
                ThermalSensor2 = (200 + rnd.Next(0, 200)) / 10.0f,
                AmbientThermalSensor = (200 + rnd.Next(0, 20)) / 10.0f,
                HumiditySensor = (300 + rnd.Next(0, 300)) / 10.0f,
                ExternalFanSpeed = 255,
                VoltageIn = (118 + rnd.Next(0, 4)) / 10.0f,
                CurrentIn = 0
            };

            for (int fanId = 0; fanId < EFC_Def.FAN_NUM; fanId++)
            {
                sensorValues.CurrentIn += fan_duties[fanId] / 10;
                sensorValues.FanSpeed[fanId] = fan_duties[fanId]*30 - 60 + rnd.Next(0,2)*60;
            }
            return true;
        }

        public bool SetFanDuty(int fanId, int fanDuty)
        {
            if(fanId < EFC_Def.FAN_NUM)
            {
                fan_duties[fanId] = (byte)fanDuty;
                return true;
            }
            return false;
        }
    }
}
