using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFC_Lib;

namespace EFC_Core.Models
{
    public class Sensor : ISensor
    {
        public string ShortName { get; }
        public string Name { get; }
        public double Value { get; }
        public SensorType Type { get; }

        public event EventHandler<SensorValueChanged>? ValueChanged;

        public Sensor(string shortName, string name, SensorType type, double value)
        {
            ShortName = shortName;
            Name = name;
            Type = type;
            Value = value;
        }

        public string Unit
        {
            get
            {
                switch (Type)
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
        }

        public int Decimals
        {
            get
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
    }
}
