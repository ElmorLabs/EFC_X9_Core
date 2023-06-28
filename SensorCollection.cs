using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFC_Core
{
    public class SensorCollection
    {
        public double? Temperature1;
        public double? Temperature2;
        public double? Temperature1AboveAmbient;
        public double? Temperature2AboveAmbient;
        public double? TemperatureAmbient;
        public double? Humidity;
        public double? ExternalFanDuty;
        public double? FanVoltage;
        public double? FanCurrent;
        public double? FanPower;
        public double?[] FanSpeeds = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public double?[] FanDuties = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    }
}
