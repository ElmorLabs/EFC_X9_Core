using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFC_Lib;
using static EFC_Core.Device_EFC_X9_V1;

namespace EFC_Core
{
    public class VirtualDevice : Device_EFC_X9_V1
    {
        public new DeviceStatus Status { get; private set; } = DeviceStatus.DISCONNECTED;
        public new int FirmwareVersion { get; private set; } = 0;

        Random rnd = new Random();
        byte[] fan_duties = new byte[Device_EFC_X9_V1.FAN_NUM];

        private DeviceConfigStruct _deviceConfigStruct;
        //private List<Models.DeviceConfigItem> _deviceConfigItems;

        public VirtualDevice() {

            _deviceConfigStruct = new DeviceConfigStruct();
            _deviceConfigStruct.FanProfile = new ProfileConfigStruct[Device_EFC_X9_V1.PROFILE_NUM];

            for(int profile = 0; profile < Device_EFC_X9_V1.PROFILE_NUM; profile++) {

                _deviceConfigStruct.FanProfile[profile].FanConfig = new FanConfigStruct[Device_EFC_X9_V1.FAN_NUM];

                for(int fan = 0; fan < Device_EFC_X9_V1.FAN_NUM; fan++) {

                    _deviceConfigStruct.FanProfile[profile].FanConfig[fan].Temp = new short[Device_EFC_X9_V1.FAN_CURVE_NUM_POINTS];
                    _deviceConfigStruct.FanProfile[profile].FanConfig[fan].Duty = new byte[Device_EFC_X9_V1.FAN_CURVE_NUM_POINTS];

                    _deviceConfigStruct.FanProfile[profile].FanConfig[fan].FanMode = FAN_MODE.FAN_MODE_TEMP_CONTROL;
                    _deviceConfigStruct.FanProfile[profile].FanConfig[fan].RampStep = 2;
                    _deviceConfigStruct.FanProfile[profile].FanConfig[fan].Temp[0] = 250;
                    _deviceConfigStruct.FanProfile[profile].FanConfig[fan].Duty[0] = 20;
                    _deviceConfigStruct.FanProfile[profile].FanConfig[fan].Temp[1] = 400;
                    _deviceConfigStruct.FanProfile[profile].FanConfig[fan].Duty[1] = 100;

                    _deviceConfigStruct.FanProfile[profile].FanConfig[fan].FixedDuty = 50;
                    _deviceConfigStruct.FanProfile[profile].FanConfig[fan].MinDuty = 20;
                    _deviceConfigStruct.FanProfile[profile].FanConfig[fan].MaxDuty = 100;

                    if(profile == 1) {
                        _deviceConfigStruct.FanProfile[profile].FanConfig[fan].FanMode = FAN_MODE.FAN_MODE_FIXED;
                        _deviceConfigStruct.FanProfile[profile].FanConfig[fan].FixedDuty = 100;
                    }

                }
            }

        }

        public override bool Connect(string comPort)
        {
            Status = DeviceStatus.CONNECTED;
            return true;
        }

        public override bool Disconnect()
        {
            Status = DeviceStatus.DISCONNECTED;
            return true;
        }

        public override bool GetSensors(out List<ISensor> sensors)
        {

            sensors = new()
            {
                new Models.Sensor("TS1", "Thermistor 1", SensorType.Temperature, (200 + rnd.Next(0, 200)) / 10.0f),
                new Models.Sensor("TS2", "Thermistor 2", SensorType.Temperature, (200 + rnd.Next(0, 200)) / 10.0f),
                new Models.Sensor("Tamb", "Ambient Temperature", SensorType.Temperature, (200 + rnd.Next(0, 20)) / 10.0f),
                new Models.Sensor("Hum",  "Humidity",            SensorType.Temperature, (300 + rnd.Next(0, 300)) / 10.0f),
                new Models.Sensor("FEXT", "External Fan Duty",   SensorType.Duty,        255)
            };

            int sim_voltage = 118 + rnd.Next(0, 4);

            sensors.Add(new Models.Sensor("Vin", "Fan Voltage", SensorType.Voltage, (sim_voltage) / 10.0f));

            int sim_current = rnd.Next(0, 2);

            for (int fanId = 0; fanId < Device_EFC_X9_V1.FAN_NUM; fanId++)
            {
                sim_current += fan_duties[fanId]/10;
            }

            sensors.Add(new Models.Sensor("Iin", "Fan Current", SensorType.Current, sim_current / 10.0f));
            sensors.Add(new Models.Sensor("Pin", "Fan Power", SensorType.Power, (sim_voltage * sim_current) / 100.0f));

            for (int fanId = 0; fanId < Device_EFC_X9_V1.FAN_NUM; fanId++)
            {
                int sim_duty = fan_duties[fanId] * 30 + rnd.Next(0, 2) * 60;
                sensors.Add(new Models.Sensor($"Fan{fanId + 1}", $"Fan Speed {fanId + 1}", SensorType.Revolutions, sim_duty));
            }

            return true;
        }

        public override bool GetConfigItems(out DeviceConfigStruct deviceConfigStruct) {
            deviceConfigStruct = _deviceConfigStruct;
            return true;
        }

        public override bool SetFanDuty(int fanId, int fanDuty)
        {
            if(fanId < Device_EFC_X9_V1.FAN_NUM)
            {
                if(fanDuty > 100)
                {
                    fanDuty = 100;
                } else if(fanDuty < 0)
                {
                    fanDuty = 0;
                }
                fan_duties[fanId] = (byte)fanDuty;
                return true;
            }
            return false;
        }

    }
}
