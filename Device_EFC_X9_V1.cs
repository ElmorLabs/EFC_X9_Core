using System;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using static EFC_Core.Models;

namespace EFC_Core;

public class Device_EFC_X9_V1 : IDevice
{
    #region Device-specific Consts
    public const int VENDOR_ID = 0xEE;
    public const int PRODUCT_ID = 0x0E;
    public const int FIRMWARE_VERSION = 0x01;

    public const int CONFIG_VERSION = 0x00;

    public const int PROFILE_NUM = 2;

    public const int FAN_NUM = 9;
    public const int FAN_CURVE_NUM_POINTS = 2;

    public const int TS_NUM = 2;
    #endregion

    #region Device-specific Structs
    public struct VendorDataStruct {
        public byte VendorId;
        public byte ProductId;
        public byte FwVersion;
    };

    public struct SensorStruct {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = TS_NUM)] public Int16[] Ts;
        public Int16 Tamb;
        public Int16 Hum;
        public byte FanExt;
        public UInt16 Vin;
        public UInt16 Iin;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = FAN_NUM)] public UInt16[] FanTach;
    };


    public struct FanConfigStruct {
        public FAN_MODE FanMode;
        public TEMP_SRC TempSource;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = FAN_CURVE_NUM_POINTS)] public Int16[] Temp;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = FAN_CURVE_NUM_POINTS)] public byte[] Duty;
        public byte RampStep;
        public byte FixedDuty;
        public byte MinDuty;
        public byte MaxDuty;
    };

    public struct DeviceConfigStruct {
        public UInt16 Crc;
        public byte ActiveFanProfileId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = PROFILE_NUM)] public ProfileConfigStruct[] FanProfile;
    };

    public struct ProfileConfigStruct {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = FAN_NUM)] public FanConfigStruct[] FanConfig;
    }
    #endregion

    #region Device-specific Enums

    public enum FAN_MODE {
        FAN_MODE_TEMP_CONTROL,
        FAN_MODE_FIXED,
        FAN_MODE_EXT
    };

    public enum TEMP_SRC {
        TEMP_SRC_AUTO,
        TEMP_SRC_TS1,
        TEMP_SRC_TS2,
        TEMP_SRC_TAMB
    };
    #endregion

    #region Variables

    public Enums.DeviceStatus Status { get; private set; } = Enums.DeviceStatus.DISCONNECTED;
    public Enums.HardwareType Type { get; private set; } = Enums.HardwareType.EFC_X9_V1;
    public int FirmwareVersion { get; private set; } = 0;

    private static readonly List<byte> RxData = new();
    private static SerialPort? _serialPort;
    //private List<Models.DeviceConfigItem> _deviceConfigItems;

    #endregion

    #region Public Methods

    public Device_EFC_X9_V1() {

        // Build local config item list
        /*_deviceConfigItems = new List<Models.DeviceConfigItem>();

        Models.DeviceConfigItem deviceConfigItem;

        deviceConfigItem = new Models.DeviceConfigItem("Active Fan Profile", Enums.DeviceConfigItemType.List, 0, 0, PROFILE_NUM);
        deviceConfigItem.DeviceConfigValues.Add(new Models.DeviceConfigValue("Profile 1", 0));
        deviceConfigItem.DeviceConfigValues.Add(new Models.DeviceConfigValue("Profile 2", 1));
        _deviceConfigItems.Add(deviceConfigItem);

        for(int profile = 1; profile <= PROFILE_NUM; profile++) {
            for(int fan = 1; fan <= FAN_NUM; fan++) {

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

                for(int point = 1; point <= FAN_CURVE_NUM_POINTS; point++) {
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
        }*/
    }

    #region Connection

    public virtual bool Connect(string comPort = "COM34")
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

    public virtual bool Disconnect()
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

    public virtual bool GetSensorValues(out List<Models.SensorValue> sensorValues)
    {
        byte[] txBuffer = Enums.UART_CMD.UART_CMD_READ_SENSOR_VALUES.ToByteArray();
        byte[] rxBuffer;

        // Get values from device
        try {
            SendCommand(txBuffer, out rxBuffer, 32);
        } catch {
            sensorValues = new List<Models.SensorValue>();
            return false;
        }

        // Convert to struct
        SensorStruct sensorStruct = new SensorStruct();
        int size = Marshal.SizeOf(sensorStruct);
        IntPtr ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(rxBuffer, 0, ptr, size);

            object? struct_obj = Marshal.PtrToStructure(ptr, typeof(SensorStruct));
            if(struct_obj != null) {
                sensorStruct = (SensorStruct)struct_obj;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        sensorValues = new List<Models.SensorValue>();

        sensorValues.Add(new Models.SensorValue("TS1", "Thermistor 1", Enums.SensorType.Temperature, sensorStruct.Ts[0] / 10.0f));
        sensorValues.Add(new Models.SensorValue("TS2", "Thermistor 2", Enums.SensorType.Temperature, sensorStruct.Ts[1] / 10.0f));
        sensorValues.Add(new Models.SensorValue("Tamb", "Ambient Temperature", Enums.SensorType.Temperature, sensorStruct.Ts[1] / 10.0f));
        sensorValues.Add(new Models.SensorValue("Hum", "Humidity", Enums.SensorType.Temperature, sensorStruct.Ts[1] / 10.0f));
        sensorValues.Add(new Models.SensorValue("FEXT", "External fan duty", Enums.SensorType.Duty, sensorStruct.FanExt));
        sensorValues.Add(new Models.SensorValue("Vin", "Fan Voltage", Enums.SensorType.Voltage, sensorStruct.Vin / 10.0f));
        sensorValues.Add(new Models.SensorValue("Iin", "Fan Current", Enums.SensorType.Current, sensorStruct.Iin / 10.0f));
        sensorValues.Add(new Models.SensorValue("Pin", "Fan Power", Enums.SensorType.Power, (sensorStruct.Vin * sensorStruct.Iin) / 100.0f));

        for (int fanId = 0; fanId < FAN_NUM; fanId++)
        {
            sensorValues.Add(new Models.SensorValue($"Fan{fanId + 1}", $"Fan Speed {fanId + 1}", Enums.SensorType.Revolutions, sensorStruct.FanTach[fanId]));
        }

        return true;
    }

    public virtual bool SetFanDuty(int fanId, int fanDuty)
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

    public virtual bool GetConfigItems(out DeviceConfigStruct deviceConfigStruct) {

        byte[] txBuffer = Enums.UART_CMD.UART_CMD_READ_CONFIG.ToByteArray(0);
        byte[] rxBuffer;

        deviceConfigStruct = new DeviceConfigStruct();

        // Get data from device
        try {
            SendCommand(txBuffer, out rxBuffer, 220);
        } catch {
            return false;
        }

        // Calc CRC16
        UInt16 crc16_calc = Helper.CRC16_Calc(rxBuffer, 2, rxBuffer.Length - 2);

        // Convert to struct
        int size = Marshal.SizeOf(deviceConfigStruct);
        IntPtr ptr = IntPtr.Zero;
        try {
            ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(rxBuffer, 0, ptr, size);

            object? struct_obj = Marshal.PtrToStructure(ptr, typeof(DeviceConfigStruct));
            if(struct_obj != null) {
                deviceConfigStruct = (DeviceConfigStruct)struct_obj;
            }
        } finally {
            Marshal.FreeHGlobal(ptr);
        }

        // Check CRC
        if(crc16_calc != deviceConfigStruct.Crc) {
            return false;
        }

        return true;
    }

    /*public bool GetConfigItems(out List<Models.DeviceConfigItem> deviceConfigItems) {

        byte[] txBuffer = Enums.UART_CMD.UART_CMD_READ_CONFIG.ToByteArray(0);
        byte[] rxBuffer;

        // Get data from device
        try {
            SendCommand(txBuffer, out rxBuffer, 220);
        } catch {
            deviceConfigItems = new List<Models.DeviceConfigItem>();
            return false;
        }

        // Calc CRC16
        UInt16 crc16_calc = Helper.CRC16_Calc(rxBuffer, 2, rxBuffer.Length - 2);

        // Convert to struct
        DeviceConfigStruct deviceConfigStruct = new DeviceConfigStruct();
        int size = Marshal.SizeOf(deviceConfigStruct);
        IntPtr ptr = IntPtr.Zero;
        try {
            ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(rxBuffer, 0, ptr, size);

            object? struct_obj = Marshal.PtrToStructure(ptr, typeof(DeviceConfigStruct));
            if(struct_obj != null) {
                deviceConfigStruct = (DeviceConfigStruct)struct_obj;
            }
        } finally {
            Marshal.FreeHGlobal(ptr);
        }

        // Check CRC
        if(crc16_calc != deviceConfigStruct.Crc) {
            deviceConfigItems = new List<Models.DeviceConfigItem>();
            return false;
        }

        // Assign values
        _deviceConfigItems[0].Value = deviceConfigStruct.ActiveFanProfileId;

        int i = 1;
        for(int profile = 1; profile <= PROFILE_NUM; profile++) {
            for(int fan = 1; fan <= FAN_NUM; fan++) {
                _deviceConfigItems[i++].Value = (int)deviceConfigStruct.FanProfile[profile].FanConfig[fan - 1].FanMode;
                _deviceConfigItems[i++].Value = (int)deviceConfigStruct.FanProfile[profile].FanConfig[fan - 1].TempSource;

                for(int point = 1; point <= FAN_CURVE_NUM_POINTS; point++) {
                    _deviceConfigItems[i++].Value = (int)deviceConfigStruct.FanProfile[profile].FanConfig[fan - 1].Temp[point - 1];
                    _deviceConfigItems[i++].Value = (int)deviceConfigStruct.FanProfile[profile].FanConfig[fan - 1].Duty[point - 1];
                }

                _deviceConfigItems[i++].Value = (int)deviceConfigStruct.FanProfile[profile].FanConfig[fan - 1].RampStep;
                _deviceConfigItems[i++].Value = (int)deviceConfigStruct.FanProfile[profile].FanConfig[fan - 1].FixedDuty;
                _deviceConfigItems[i++].Value = (int)deviceConfigStruct.FanProfile[profile].FanConfig[fan - 1].MinDuty;
                _deviceConfigItems[i++].Value = (int)deviceConfigStruct.FanProfile[profile].FanConfig[fan - 1].MaxDuty;

            }
        }

        deviceConfigItems = _deviceConfigItems;

        return true;
    }*/

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