using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFC_Core;

public static class Models
{

    public enum SensorType
    {
        Temperature,
        Humidity,
        Duty,
        Revolutions,
        Voltage,
        Current,
        Power
    }

    public class SensorValue
    {
        public string ShortName { get; set; }
        public string Name { get; set; }
        public double Value { get; set; }
        public SensorType Type { get; set; }

        public SensorValue(string short_name, string name, SensorType type, double value)
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
                case SensorType.Temperature: return "°C";
                case SensorType.Humidity:
                case SensorType.Duty: return "%";
                case SensorType.Revolutions: return "RPM";
                case SensorType.Voltage: return "V";
                case SensorType.Current: return "A";
                case SensorType.Power: return "W";
            }
            return "";
        }

        public int GetDecimals()
        {

            switch (Type)
            {
                case SensorType.Temperature:
                case SensorType.Humidity:
                case SensorType.Voltage:
                case SensorType.Current: return 1;
                case SensorType.Duty: 
                case SensorType.Revolutions:
                case SensorType.Power: return 0;
            }
            return 0;
        }
    }

    /*public class SensorValues
    {
        public float? ThermalSensor1 { get; set; } = null;
        public float? ThermalSensor2 { get; set; } = null;
        public float? AmbientThermalSensor { get; set; } = null;
        public float? HumiditySensor { get; set; } = null;
        public float? ExternalFanSpeed { get; set; } = null;
        public float? VoltageIn { get; set; } = null;
        public float? CurrentIn { get; set; } = null;
        public int?[] FanSpeed { get; set; } = { null, null, null, null, null, null, null, null, null };
    }*/
}