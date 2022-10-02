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
        public Enums.HardwareType Type { get; private set; } = Enums.HardwareType.EFC_X9_V1;
        public int FirmwareVersion { get; private set; } = 0;

        Random rnd = new Random();
        byte[] fan_duties = new byte[Device_EFC_X9_V1.FAN_NUM];

        private List<Models.DeviceConfigItem> _deviceConfigItems;

        public VirtualDevice() {

            // Build local config item list
            _deviceConfigItems = new List<Models.DeviceConfigItem>();

            Models.DeviceConfigItem deviceConfigItem;

            deviceConfigItem = new Models.DeviceConfigItem("Active Fan Profile", Enums.DeviceConfigItemType.List, 0, 0, Device_EFC_X9_V1.PROFILE_NUM);
            deviceConfigItem.DeviceConfigValues.Add(new Models.DeviceConfigValue("Profile 1", 0));
            deviceConfigItem.DeviceConfigValues.Add(new Models.DeviceConfigValue("Profile 2", 1));
            _deviceConfigItems.Add(deviceConfigItem);

            for(int profile = 1; profile <= Device_EFC_X9_V1.PROFILE_NUM; profile++) {
                for(int fan = 1; fan <= Device_EFC_X9_V1.FAN_NUM; fan++) {

                    deviceConfigItem = new Models.DeviceConfigItem($"Profile #{profile} Fan #{fan} Mode", Enums.DeviceConfigItemType.List, 0, 0, 2);
                    deviceConfigItem.DeviceConfigValues.Add(new Models.DeviceConfigValue("Temperature Control", 0));
                    deviceConfigItem.DeviceConfigValues.Add(new Models.DeviceConfigValue("Fixed", 1));
                    deviceConfigItem.DeviceConfigValues.Add(new Models.DeviceConfigValue("External Fan Input", 2));
                    _deviceConfigItems.Add(deviceConfigItem);

                    deviceConfigItem = new Models.DeviceConfigItem($"Profile #{profile} Fan #{fan} Temperature Source", Enums.DeviceConfigItemType.List, 0, 0, 3);
                    deviceConfigItem.DeviceConfigValues.Add(new Models.DeviceConfigValue("Auto", 0));
                    deviceConfigItem.DeviceConfigValues.Add(new Models.DeviceConfigValue("Thermistor 1", 1));
                    deviceConfigItem.DeviceConfigValues.Add(new Models.DeviceConfigValue("Thermistor 2", 2));
                    deviceConfigItem.DeviceConfigValues.Add(new Models.DeviceConfigValue("Ambient Temperature", 3));
                    _deviceConfigItems.Add(deviceConfigItem);

                    for(int point = 1; point <= Device_EFC_X9_V1.FAN_CURVE_NUM_POINTS; point++) {
                        deviceConfigItem = new Models.DeviceConfigItem($"Profile #{profile} Fan #{fan} Temperature Point {point}", Enums.DeviceConfigItemType.Float, 0, 0, 100);
                        _deviceConfigItems.Add(deviceConfigItem);
                        deviceConfigItem = new Models.DeviceConfigItem($"Profile #{profile} Fan #{fan} Duty Point {point}", Enums.DeviceConfigItemType.Integer, 0, 0, 100);
                        _deviceConfigItems.Add(deviceConfigItem);
                    }

                    deviceConfigItem = new Models.DeviceConfigItem($"Profile #{profile} Fan #{fan} Ramp Step Size", Enums.DeviceConfigItemType.Integer, 0, 1, 63);
                    _deviceConfigItems.Add(deviceConfigItem);

                    deviceConfigItem = new Models.DeviceConfigItem($"Profile #{profile} Fan #{fan} Fixed Duty", Enums.DeviceConfigItemType.Integer, 0, 0, 100);
                    _deviceConfigItems.Add(deviceConfigItem);

                    deviceConfigItem = new Models.DeviceConfigItem($"Profile #{profile} Fan #{fan} Min Duty", Enums.DeviceConfigItemType.Integer, 0, 0, 100);
                    _deviceConfigItems.Add(deviceConfigItem);

                    deviceConfigItem = new Models.DeviceConfigItem($"Profile #{profile} Fan #{fan} Max Duty", Enums.DeviceConfigItemType.Integer, 0, 0, 100);
                    _deviceConfigItems.Add(deviceConfigItem);

                }
            }
        }

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

        public bool GetSensorValues(out List<Models.SensorValue> sensorValues)
        {

            sensorValues = new List<Models.SensorValue>();

            sensorValues.Add(new Models.SensorValue("TS1", "Thermistor 1", Enums.SensorType.Temperature, (200 + rnd.Next(0, 200)) / 10.0f));
            sensorValues.Add(new Models.SensorValue("TS2", "Thermistor 2", Enums.SensorType.Temperature, (200 + rnd.Next(0, 200)) / 10.0f));
            sensorValues.Add(new Models.SensorValue("Tamb", "Ambient Temperature", Enums.SensorType.Temperature, (200 + rnd.Next(0, 20)) / 10.0f));
            sensorValues.Add(new Models.SensorValue("Hum", "Humidity", Enums.SensorType.Temperature, (300 + rnd.Next(0, 300)) / 10.0f));
            sensorValues.Add(new Models.SensorValue("FEXT", "External Fan Duty", Enums.SensorType.Duty, 255));
            
            int sim_voltage = 118 + rnd.Next(0, 4);

            sensorValues.Add(new Models.SensorValue("Vin", "Fan Voltage", Enums.SensorType.Voltage, (sim_voltage) / 10.0f));

            int sim_current = rnd.Next(0, 2);

            for (int fanId = 0; fanId < Device_EFC_X9_V1.FAN_NUM; fanId++)
            {
                sim_current += fan_duties[fanId]/10;
            }

            sensorValues.Add(new Models.SensorValue("Iin", "Fan Current", Enums.SensorType.Current, sim_current / 10.0f));
            sensorValues.Add(new Models.SensorValue("Pin", "Fan Power", Enums.SensorType.Power, (sim_voltage * sim_current) / 100.0f));

            for (int fanId = 0; fanId < Device_EFC_X9_V1.FAN_NUM; fanId++)
            {
                int sim_duty = fan_duties[fanId] * 30 + rnd.Next(0, 2) * 60;
                sensorValues.Add(new Models.SensorValue($"Fan{fanId + 1}", $"Fan Speed {fanId + 1}", Enums.SensorType.Revolutions, sim_duty));
            }

            return true;
        }

        public bool GetConfigItems(out List<Models.DeviceConfigItem> deviceConfigItems) {
            deviceConfigItems = _deviceConfigItems;
            return true;
        }

        public bool SetFanDuty(int fanId, int fanDuty)
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
