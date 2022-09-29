using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFC_Core;

public static class Models
{
    public class SensorValues
    {
        public float? ThermalSensor1 { get; set; } = null;
        public float? ThermalSensor2 { get; set; } = null;
        public float? AmbientThermalSensor { get; set; } = null;
        public float? HumiditySensor { get; set; } = null;
        public float? ExternalFanSpeed { get; set; } = null;
        public float? VoltageIn { get; set; } = null;
        public float? CurrentIn { get; set; } = null;
        public int?[] FanSpeed { get; set; } = { null, null, null, null, null, null, null, null, null };
    }
}