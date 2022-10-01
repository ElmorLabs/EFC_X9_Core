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

        public bool GetSensorValues(out List<Models.SensorValue> sensorValues)
        {


            sensorValues = new List<Models.SensorValue>();

            sensorValues.Add(new Models.SensorValue("TS1", "Thermistor 1", Models.SensorType.Temperature, (200 + rnd.Next(0, 200)) / 10.0f));
            sensorValues.Add(new Models.SensorValue("TS2", "Thermistor 2", Models.SensorType.Temperature, (200 + rnd.Next(0, 200)) / 10.0f));
            sensorValues.Add(new Models.SensorValue("Tamb", "Ambient Temperature", Models.SensorType.Temperature, (200 + rnd.Next(0, 20)) / 10.0f));
            sensorValues.Add(new Models.SensorValue("Hum", "Humidity", Models.SensorType.Temperature, (300 + rnd.Next(0, 300)) / 10.0f));
            sensorValues.Add(new Models.SensorValue("FEXT", "External Fan Duty", Models.SensorType.Duty, 255));
            
            int sim_voltage = 118 + rnd.Next(0, 4);

            sensorValues.Add(new Models.SensorValue("Vin", "Fan Voltage", Models.SensorType.Voltage, (sim_voltage) / 10.0f));

            int sim_current = rnd.Next(0, 2);

            for (int fanId = 0; fanId < EFC_Def.FAN_NUM; fanId++)
            {
                sim_current += fan_duties[fanId]/10;
            }

            sensorValues.Add(new Models.SensorValue("Iin", "Fan Current", Models.SensorType.Current, sim_current / 10.0f));
            sensorValues.Add(new Models.SensorValue("Pin", "Fan Power", Models.SensorType.Power, (sim_voltage * sim_current) / 100.0f));

            for (int fanId = 0; fanId < EFC_Def.FAN_NUM; fanId++)
            {
                int sim_duty = fan_duties[fanId] * 30 + rnd.Next(0, 2) * 60;
                sensorValues.Add(new Models.SensorValue($"Fan{fanId + 1}", $"Fan Speed {fanId + 1}", Models.SensorType.Revolutions, sim_duty));
            }

            return true;
        }

        public bool SetFanDuty(int fanId, int fanDuty)
        {
            if(fanId < EFC_Def.FAN_NUM)
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
