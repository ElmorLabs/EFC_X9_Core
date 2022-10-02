using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EFC_Core;

public static class Models
{

    public class SensorValue
    {
        public string ShortName { get; set; }
        public string Name { get; set; }
        public double Value { get; set; }
        public Enums.SensorType Type { get; set; }

        public SensorValue(string short_name, string name, Enums.SensorType type, double value)
        {
            ShortName = short_name;
            Name = name;
            Type = type;
            Value = value;
        }

        public string GetUnit()
        {
            switch(Type)
            {
                case Enums.SensorType.Temperature: return "°C";
                case Enums.SensorType.Humidity:
                case Enums.SensorType.Duty: return "%";
                case Enums.SensorType.Revolutions: return "RPM";
                case Enums.SensorType.Voltage: return "V";
                case Enums.SensorType.Current: return "A";
                case Enums.SensorType.Power: return "W";
            }
            return "";
        }

        public int GetDecimals()
        {

            switch (Type)
            {
                case Enums.SensorType.Temperature:
                case Enums.SensorType.Humidity:
                case Enums.SensorType.Voltage:
                case Enums.SensorType.Current: return 1;
                case Enums.SensorType.Duty: 
                case Enums.SensorType.Revolutions:
                case Enums.SensorType.Power: return 0;
            }
            return 0;
        }
    }

    public class DeviceConfigValue {
        public string Description { get; set; }
        public int Value { get; set; }
        public DeviceConfigValue(string desc, int val) {
            Description = desc;
            Value = val;
        }
    }

    public class DeviceConfigItem {
        public string Description { get; set; }
        public Enums.DeviceConfigItemType Type { get; set; }
        public int Value;
        public int MinValue;
        public int MaxValue;

        public List<DeviceConfigValue> DeviceConfigValues = new List<DeviceConfigValue>();

        public DeviceConfigItem(string desc, Enums.DeviceConfigItemType type, int val, int min_val, int max_val) {
            Description = desc;
            Type = type;
            Value = val;
            MinValue = min_val;
            MaxValue = max_val;
        }

    }

}