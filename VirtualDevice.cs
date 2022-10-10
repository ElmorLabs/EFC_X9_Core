using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFC_Lib.Classes;
using static EFC_Core.Device_EFC_X9_V1;

namespace EFC_Core
{
    public class VirtualDevice : Device_EFC_X9_V1
    {
        public new DeviceStatus Status { get; private set; } = DeviceStatus.DISCONNECTED;
        public new int FirmwareVersion => 0;

        private readonly Random _rnd = new();
        private readonly byte[] _fanDuties = new byte[FAN_NUM];

        private readonly DeviceConfigStruct _deviceConfigStruct;

        public VirtualDevice()
        {

            _deviceConfigStruct = new DeviceConfigStruct();
            _deviceConfigStruct.FanProfile = new ProfileConfigStruct[PROFILE_NUM];

            for (int profile = 0; profile < PROFILE_NUM; profile++)
            {

                _deviceConfigStruct.FanProfile[profile].FanConfig = new FanConfigStruct[FAN_NUM];

                for (int fan = 0; fan < FAN_NUM; fan++)
                {

                    _deviceConfigStruct.FanProfile[profile].FanConfig[fan].Temp = new short[FAN_CURVE_NUM_POINTS];
                    _deviceConfigStruct.FanProfile[profile].FanConfig[fan].Duty = new byte[FAN_CURVE_NUM_POINTS];

                    _deviceConfigStruct.FanProfile[profile].FanConfig[fan].FanMode = FAN_MODE.FAN_MODE_TEMP_CONTROL;
                    _deviceConfigStruct.FanProfile[profile].FanConfig[fan].RampStep = 2;
                    _deviceConfigStruct.FanProfile[profile].FanConfig[fan].Temp[0] = 250;
                    _deviceConfigStruct.FanProfile[profile].FanConfig[fan].Duty[0] = 20;
                    _deviceConfigStruct.FanProfile[profile].FanConfig[fan].Temp[1] = 400;
                    _deviceConfigStruct.FanProfile[profile].FanConfig[fan].Duty[1] = 100;

                    _deviceConfigStruct.FanProfile[profile].FanConfig[fan].FixedDuty = 50;
                    _deviceConfigStruct.FanProfile[profile].FanConfig[fan].MinDuty = 20;
                    _deviceConfigStruct.FanProfile[profile].FanConfig[fan].MaxDuty = 100;

                    if (profile == 1)
                    {
                        _deviceConfigStruct.FanProfile[profile].FanConfig[fan].FanMode = FAN_MODE.FAN_MODE_FIXED;
                        _deviceConfigStruct.FanProfile[profile].FanConfig[fan].FixedDuty = 100;
                    }

                }
            }

        }

        public override bool Connect(string comPort = "COM34")
        {
            Status = DeviceStatus.CONNECTED;
            return true;
        }

        public override bool Disconnect()
        {
            Status = DeviceStatus.DISCONNECTED;
            return true;
        }

        public override bool Reset(bool bootloaderMode)
        {
            return true;
        }

        public override bool UpdateSensors()
        {

            Sensors.Temperature1 = (200 + _rnd.Next(0, 200)) / 10.0f;
            Sensors.Temperature2 = (200 + _rnd.Next(0, 200)) / 10.0f;
            Sensors.TemperatureAmbient = (200 + _rnd.Next(0, 20)) / 10.0f;
            Sensors.Humidity = (300 + _rnd.Next(0, 300)) / 10.0f;
            Sensors.ExternalFanDuty = 255;

            int sim_voltage = 118 + _rnd.Next(0, 4);
            Sensors.FanVoltage = sim_voltage / 10.0f;

            int sim_current = _rnd.Next(0, 2);

            for (int fanId = 0; fanId < FAN_NUM; fanId++)
            {
                sim_current += _fanDuties[fanId] / 10;
            }

            Sensors.FanCurrent = sim_current / 10.0f;
            Sensors.FanPower = sim_voltage * sim_current / 100.0f;

            for (int fanId = 0; fanId < FAN_NUM; fanId++)
            {
                int sim_duty = _fanDuties[fanId] * 30 + _rnd.Next(0, 2) * 60;
                Sensors.FanSpeeds[fanId] = sim_duty;
            }

            return true;
        }

        public override bool GetConfigItems(out DeviceConfigStruct deviceConfigStruct)
        {
            deviceConfigStruct = _deviceConfigStruct;
            return true;
        }

        public override bool SetFanDuty(int fanId, int fanDuty)
        {
            if (fanId < FAN_NUM)
            {
                if (fanDuty > 100)
                {
                    fanDuty = 100;
                }
                else if (fanDuty < 0)
                {
                    fanDuty = 0;
                }
                _fanDuties[fanId] = (byte)fanDuty;
                return true;
            }
            return false;
        }

    }
}
