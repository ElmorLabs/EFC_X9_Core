using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;

namespace EFC_Core;

public class Device : IDevice
{
    #region Variables
    
    public Enums.DeviceStatus Status { get; private set; } = Enums.DeviceStatus.DISCONNECTED;
    public int FirmwareVersion { get; private set; } = 0;

    private static readonly List<byte> RxData = new();
    private static SerialPort? _serialPort;

    #endregion

    #region Public Methods

    #region Connection

    public bool Connect(string comPort = "COM34")
    {
        Status = Enums.DeviceStatus.CONNECTING;

        try
        {
            _serialPort = new SerialPort(comPort)
            {
                BaudRate = 115200,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = 500,
                WriteTimeout = 500,
                RtsEnable = true,
                DtrEnable = true
            };

            _serialPort.DataReceived += SerialPortOnDataReceived;
        }
        catch
        {
            Status = Enums.DeviceStatus.ERROR;
            return false;
        }

        try
        {
            _serialPort.Open();
        }
        catch
        {
            Status = Enums.DeviceStatus.ERROR;
            return false;
        }

        if (Test())
        {
            Status = Enums.DeviceStatus.CONNECTED;
            return true;
        }
        else
        {
            Status = Enums.DeviceStatus.ERROR;
            return false;
        }
    }

    public bool Disconnect()
    {
        Status = Enums.DeviceStatus.DISCONNECTING;

        if (_serialPort != null)
        {
            try
            {
                _serialPort.Dispose();
                _serialPort = null;
            }
            catch
            {
                Status = Enums.DeviceStatus.ERROR;
                return false;
            }
        }
        else
        {
            return false;
        }

        Status = Enums.DeviceStatus.DISCONNECTED;
        return true;
    }

    #endregion

    #region Functionality

    public bool GetSensorValues(out List<Models.SensorValue> sensorValues)
    {
        byte[] txBuffer = Enums.UART_CMD.UART_CMD_READ_SENSOR_VALUES.ToByteArray();
        SendCommand(txBuffer, out byte[] rxBuffer, 32);

        SensorStruct sensorStruct = new SensorStruct();

        int size = Marshal.SizeOf(sensorStruct);
        IntPtr ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(txBuffer, 0, ptr, size);

            sensorStruct = (SensorStruct)Marshal.PtrToStructure(ptr, typeof(SensorStruct));
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        sensorValues = new List<Models.SensorValue>();

        sensorValues.Add(new Models.SensorValue("TS1", "Thermistor 1", Models.SensorType.Temperature, sensorStruct.Ts[0] / 10.0f));
        sensorValues.Add(new Models.SensorValue("TS2", "Thermistor 2", Models.SensorType.Temperature, sensorStruct.Ts[1] / 10.0f));
        sensorValues.Add(new Models.SensorValue("Tamb", "Ambient Temperature", Models.SensorType.Temperature, sensorStruct.Ts[1] / 10.0f));
        sensorValues.Add(new Models.SensorValue("Hum", "Humidity", Models.SensorType.Temperature, sensorStruct.Ts[1] / 10.0f));
        sensorValues.Add(new Models.SensorValue("FEXT", "External fan duty", Models.SensorType.Duty, sensorStruct.FanExt));
        sensorValues.Add(new Models.SensorValue("Vin", "Fan Voltage", Models.SensorType.Voltage, sensorStruct.Vin / 10.0f));
        sensorValues.Add(new Models.SensorValue("Iin", "Fan Current", Models.SensorType.Current, sensorStruct.Iin / 10.0f));
        sensorValues.Add(new Models.SensorValue("Pin", "Fan Power", Models.SensorType.Power, (sensorStruct.Vin * sensorStruct.Iin) / 100.0f));

        for (int fanId = 0; fanId < EFC_Def.FAN_NUM; fanId++)
        {
            sensorValues.Add(new Models.SensorValue($"Fan{fanId + 1}", $"Fan Speed {fanId + 1}", Models.SensorType.Revolutions, sensorStruct.FanTach[fanId]));
        }

        return true;
    }

    public bool SetFanDuty(int fanId, int fanDuty)
    {
        // Duty - txBuffer[2]
        // 0~100 (0x00~0x64) for duty control in percentage
        // 255 (0xFF) for MCU embedded control

        byte[] txBuffer = Enums.UART_CMD.UART_CMD_WRITE_FAN_DUTY.ToByteArray(2);
        txBuffer[1] = (byte)fanId;
        txBuffer[2] = (byte)fanDuty;

        try
        {
            SendCommand(txBuffer, out _, 0);
        }
        catch
        {
            return false;
        }

        return true;
    }

    #endregion

    #endregion

    #region Private Methods

    private bool Test()
    {
        byte[] txBuffer = Enums.UART_CMD.UART_CMD_READ_ID.ToByteArray();
        SendCommand(txBuffer, out byte[] rxBuffer, 3);

        if (rxBuffer[0] != 0xEE || rxBuffer[1] != 0x0E) return false;

        FirmwareVersion = rxBuffer[2];
        return true;
    }

    private void SerialPortOnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        SerialPort serialPort = (SerialPort)sender;
        byte[] data = new byte[serialPort.BytesToRead];
        serialPort.Read(data, 0, data.Length);
        RxData.AddRange(data.ToList());
    }

    private bool SendCommand(byte[] txBuffer, out byte[] rxBuffer, int rxLen)
    {
        rxBuffer = new byte[rxLen];

        if (_serialPort == null) return false;

        try
        {
            RxData.Clear();
            _serialPort.Write(txBuffer, 0, txBuffer.Length);
            int timeout = 50;
            
            while (timeout-- > 0 && RxData.Count != rxLen)
            {
                Thread.Sleep(10);
            }

            if (RxData.Count != rxBuffer.Length)
            {
                return false;
            }

            rxBuffer = RxData.ToArray();
        }
        catch
        {
            return false;
        }

        return true;
    }

    #endregion
}