using System.IO.Ports;
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
                ReadTimeout = 2000,
                WriteTimeout = 2000,
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

    public bool GetSensorValues(out Models.SensorValues sensorValues)
    {
        byte[] txBuffer = Enums.UART_CMD.UART_CMD_READ_SENSOR_VALUES.ToByteArray();
        SendCommand(txBuffer, out byte[] rxBuffer, 32);

        sensorValues = new Models.SensorValues()
        {
            ThermalSensor1       = (Int16)(rxBuffer[1] << 8 | rxBuffer[0]) / 10.0f,
            ThermalSensor2       = (Int16)(rxBuffer[3] << 8 | rxBuffer[2]) / 10.0f,
            AmbientThermalSensor = (Int16)(rxBuffer[5] << 8 | rxBuffer[4]) / 10.0f,
            HumiditySensor       = (Int16)(rxBuffer[7] << 8 | rxBuffer[6]) / 10.0f,
            ExternalFanSpeed     = rxBuffer[8],
            VoltageIn            = (UInt16)(rxBuffer[11] << 8 | rxBuffer[10]) / 100.0f,
            CurrentIn            = (UInt16)(rxBuffer[13] << 8 | rxBuffer[12]) / 10.0f
        };

        for (int fanId = 0; fanId < 9; fanId++)
        {
            sensorValues.FanSpeed[fanId] = (UInt16)(rxBuffer[15 + fanId * 2] << 8 | rxBuffer[14 + fanId * 2]);
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