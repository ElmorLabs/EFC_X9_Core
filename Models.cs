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

}