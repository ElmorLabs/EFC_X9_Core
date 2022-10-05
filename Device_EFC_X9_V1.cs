using System;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using EFC_Lib;

namespace EFC_Core;

public class Device_EFC_X9_V1 : IDevice
{
    #region Device-specific Consts
    public const int VENDOR_ID = 0xEE;
    public const int PRODUCT_ID = 0x0E;
    public const int FIRMWARE_VERSION = 0x01;

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

    public struct SensorStruct_V1 {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = TS_NUM)] public Int16[] Ts;
        public Int16 Tamb;
        public Int16 Hum;
        public byte FanExt;
        public UInt16 Vin;
        public UInt16 Iin;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = FAN_NUM)] public UInt16[] FanTach;
    };

    public struct SensorStruct_V2 {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = TS_NUM)] public Int16[] Ts;
        public Int16 Tamb;
        public Int16 Hum;
        public byte FanExt;
        public UInt16 Vin;
        public UInt16 Iin;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = FAN_NUM)] public UInt16[] FanTach;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = FAN_NUM)] public byte[] FanDuty;
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

    public enum FAN_MODE : byte {
        FAN_MODE_TEMP_CONTROL,
        FAN_MODE_FIXED,
        FAN_MODE_EXT
    };

    public enum TEMP_SRC : byte {
        TEMP_SRC_AUTO,
        TEMP_SRC_TS1,
        TEMP_SRC_TS2,
        TEMP_SRC_TAMB
    };

    private enum UART_CMD : byte {
        UART_CMD_WELCOME,
        UART_CMD_READ_ID,
        UART_CMD_RSVD1,
        UART_CMD_READ_SENSOR_VALUES,
        UART_CMD_READ_CONFIG,
        UART_CMD_WRITE_CONFIG,
        UART_CMD_RSVD2,
        UART_CMD_RSVD3,
        UART_CMD_RSVD4,
        UART_CMD_WRITE_FAN_DUTY,
        UART_CMD_DISPLAY_SW_DISABLE = 0xE0,
        UART_CMD_DISPLAY_SW_ENABLE = 0xE1,
        UART_CMD_DISPLAY_SW_WRITE = 0xE2,
        UART_CMD_DISPLAY_SW_UPDATE = 0xE3,
        UART_CMD_RESET = 0xF0,
        UART_CMD_BOOTLOADER = 0xF1,
        UART_CMD_NVM_CONFIG = 0xF2,
        UART_CMD_NOP = 0xFF
    }

    private static byte[] ToByteArray(UART_CMD uartCMD, int len = 0) {
        byte[] returnArray = new byte[len + 1];
        returnArray[0] = (byte)uartCMD;
        return returnArray;
    }

    private enum UART_NVM_CMD : byte {
        CONFIG_SAVE,
        CONFIG_LOAD,
        CONFIG_RESET
    }


    #endregion

    #region Variables

    public string Name { get; } = "EFC X9 V1";
    public Guid DeviceId { get; } = new("AE4A2B8E-492B-4310-AB73-E0CF701D4EAB");
    public DeviceStatus Status { get; private set; } = DeviceStatus.DISCONNECTED;
    public int FirmwareVersion { get; private set; } = 0;

    private static readonly List<byte> RxData = new();
    private static SerialPort? _serialPort;

    #endregion

    #region Public Methods

    #region Connection

    public virtual bool Connect(string comPort = "COM34")
    {
        Status = DeviceStatus.CONNECTING;

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
            Status = DeviceStatus.ERROR;
            return false;
        }

        try
        {
            _serialPort.Open();
        }
        catch
        {
            Status = DeviceStatus.ERROR;
            return false;
        }

        if (CheckVendorData())
        {
            Status = DeviceStatus.CONNECTED;
            return true;
        }
        else
        {
            Status = DeviceStatus.ERROR;
            return false;
        }
    }

    public virtual bool Disconnect()
    {
        Status = DeviceStatus.DISCONNECTING;

        if (_serialPort != null)
        {
            try
            {
                _serialPort.Dispose();
                _serialPort = null;
            }
            catch
            {
                Status = DeviceStatus.ERROR;
                return false;
            }
        }
        else
        {
            return false;
        }

        Status = DeviceStatus.DISCONNECTED;
        return true;
    }

    public virtual bool Reset(bool bootloaderMode = false)
    {

        byte[] txBuffer = ToByteArray(bootloaderMode ? UART_CMD.UART_CMD_BOOTLOADER : UART_CMD.UART_CMD_RESET);

        // Send command to device
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

    #region Functionality

    public virtual bool GetSensors(out List<ISensor> sensors)
    {
        byte[] txBuffer = ToByteArray(UART_CMD.UART_CMD_READ_SENSOR_VALUES);
        byte[] rxBuffer;

        if(FirmwareVersion == 0x01) {

            SensorStruct_V1 sensorStruct = new()
            {
                // Prevent null possibility
                Ts = new short[TS_NUM],
                FanTach = new ushort[FAN_NUM]
            };

            // Get struct size
            int size = Marshal.SizeOf(sensorStruct);

            // Get values from device
            try {
                SendCommand(txBuffer, out rxBuffer, size);
            } catch {
                sensors = new List<ISensor>();
                return false;
            }

            // Convert byte array to struct
            IntPtr ptr = IntPtr.Zero;
            try {
                ptr = Marshal.AllocHGlobal(size);

                Marshal.Copy(rxBuffer, 0, ptr, size);

                object? structObj = Marshal.PtrToStructure(ptr, typeof(SensorStruct_V1));
                if(structObj != null) {
                    sensorStruct = (SensorStruct_V1)structObj;
                }
            } finally {
                Marshal.FreeHGlobal(ptr);
            }

            sensors = new()
            {
                new Models.Sensor("TS1",  "Thermistor 1",        SensorType.Temperature, sensorStruct.Ts[0] / 10.0f),
                new Models.Sensor("TS2",  "Thermistor 2",        SensorType.Temperature, sensorStruct.Ts[1] / 10.0f),
                new Models.Sensor("Tamb", "Ambient Temperature", SensorType.Temperature, sensorStruct.Ts[1] / 10.0f),
                new Models.Sensor("Hum",  "Humidity",            SensorType.Temperature, sensorStruct.Ts[1] / 10.0f),
                new Models.Sensor("FEXT", "External fan duty",   SensorType.Duty,        sensorStruct.FanExt),
                new Models.Sensor("Vin",  "Fan Voltage",         SensorType.Voltage,     sensorStruct.Vin / 10.0f),
                new Models.Sensor("Iin",  "Fan Current",         SensorType.Current,     sensorStruct.Iin / 10.0f),
                new Models.Sensor("Pin",  "Fan Power",           SensorType.Power,       (sensorStruct.Vin * sensorStruct.Iin) / 100.0f)
            };

            for(int fanId = 0; fanId < FAN_NUM; fanId++) {
                sensors.Add(new Models.Sensor($"Fan{fanId + 1}", $"Fan Speed {fanId + 1}", SensorType.Revolutions, sensorStruct.FanTach[fanId]));
            }
        } else {
            SensorStruct_V2 sensorStruct = new()
            {
                // Prevent null possibility
                Ts = new short[TS_NUM],
                FanTach = new ushort[FAN_NUM],
                FanDuty = new byte[FAN_NUM]
            };

            // Get struct size
            int size = Marshal.SizeOf(sensorStruct);

            // Get values from device
            try {
                SendCommand(txBuffer, out rxBuffer, size);
            } catch {
                sensors = new List<ISensor>();
                return false;
            }

            // Convert byte array to struct

            IntPtr ptr = IntPtr.Zero;
            try {
                ptr = Marshal.AllocHGlobal(size);

                Marshal.Copy(rxBuffer, 0, ptr, size);

                object? structObj = Marshal.PtrToStructure(ptr, typeof(SensorStruct_V2));
                if(structObj != null) {
                    sensorStruct = (SensorStruct_V2)structObj;
                }
            } finally {
                Marshal.FreeHGlobal(ptr);
            }

            sensors = new()
            {
                new Models.Sensor("TS1",  "Thermistor 1",        SensorType.Temperature, sensorStruct.Ts[0] / 10.0f),
                new Models.Sensor("TS2",  "Thermistor 2",        SensorType.Temperature, sensorStruct.Ts[1] / 10.0f),
                new Models.Sensor("Tamb", "Ambient Temperature", SensorType.Temperature, sensorStruct.Ts[1] / 10.0f),
                new Models.Sensor("Hum",  "Humidity",            SensorType.Temperature, sensorStruct.Ts[1] / 10.0f),
                new Models.Sensor("FEXT", "External fan duty",   SensorType.Duty,        sensorStruct.FanExt),
                new Models.Sensor("Vin",  "Fan Voltage",         SensorType.Voltage,     sensorStruct.Vin / 10.0f),
                new Models.Sensor("Iin",  "Fan Current",         SensorType.Current,     sensorStruct.Iin / 10.0f),
                new Models.Sensor("Pin",  "Fan Power",           SensorType.Power,       (sensorStruct.Vin * sensorStruct.Iin) / 100.0f)
            };

            for(int fanId = 0; fanId < FAN_NUM; fanId++) {
                sensors.Add(new Models.Sensor($"Fan{fanId + 1}", $"Fan Speed {fanId + 1}", SensorType.Revolutions, sensorStruct.FanTach[fanId]));
            }

            for(int fanId = 0; fanId < FAN_NUM; fanId++) {
                sensors.Add(new Models.Sensor($"Fan{fanId + 1}", $"Fan Duty {fanId + 1}", SensorType.Duty, sensorStruct.FanDuty[fanId]));
            }
        }

        return true;
    }

    public virtual bool SetFanDuty(int fanId, int fanDuty)
    {
        // Duty - txBuffer[2]
        // 0~100 (0x00~0x64) for duty control in percentage
        // 255 (0xFF) for MCU embedded control

        byte[] txBuffer = ToByteArray(UART_CMD.UART_CMD_WRITE_FAN_DUTY, 2);
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

    // Get device configuration
    public virtual bool GetConfigItems(out DeviceConfigStruct deviceConfigStruct) {

        byte[] txBuffer = ToByteArray(UART_CMD.UART_CMD_READ_CONFIG);
        byte[] rxBuffer;

        deviceConfigStruct = new DeviceConfigStruct();

        // Get data from device
        try {
            SendCommand(txBuffer, out rxBuffer, 220);
        } catch {
            return false;
        }

        // Calc CRC16
        UInt16 crc16Calc = Helper.CRC16_Calc(rxBuffer, 2, rxBuffer.Length - 2);

        // Convert byte array to struct
        int size = Marshal.SizeOf(deviceConfigStruct);
        IntPtr ptr = IntPtr.Zero;
        try {
            ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(rxBuffer, 0, ptr, size);

            object? structObj = Marshal.PtrToStructure(ptr, typeof(DeviceConfigStruct));
            if(structObj != null) {
                deviceConfigStruct = (DeviceConfigStruct)structObj;
            }
        } finally {
            Marshal.FreeHGlobal(ptr);
        }

        // Firmware version 01 reports Crc = 0 if not loaded from NVM
        if(FirmwareVersion != 0x01 && crc16Calc != deviceConfigStruct.Crc) {
            return false;
        }

        return true;
    }

    public virtual bool SetConfigItems(DeviceConfigStruct deviceConfigStruct) {

        // Firmware version 01 write config is bugged, so fail directly
        if(FirmwareVersion == 0x01) {
            return false;
        }

        byte[] txBuffer = ToByteArray(UART_CMD.UART_CMD_WRITE_CONFIG);

        // Send initial command
        try {
            SendCommand(txBuffer, out _, 0);
        } catch {
            return false;
        }

        // Convert struct to byte array
        int size = Marshal.SizeOf(deviceConfigStruct);
        txBuffer = new byte[size];
        IntPtr ptr = IntPtr.Zero;
        try {
            ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(deviceConfigStruct, ptr, true);
            Marshal.Copy(ptr, txBuffer, 0, size);
        } finally {
            Marshal.FreeHGlobal(ptr);
        }

        // Calc CRC16
        UInt16 crc16Calc = Helper.CRC16_Calc(txBuffer, 2, txBuffer.Length - 2);
        txBuffer[0] = (byte)crc16Calc;
        txBuffer[1] = (byte)(crc16Calc >> 8);

        // Send config data to device
        try {
            SendCommand(txBuffer, out _, 0);
        } catch {
            return false;
        }

        return true;
    }

    public virtual bool SaveConfig() {
        return SendNvmCommand(UART_NVM_CMD.CONFIG_SAVE);
    }
    public virtual bool LoadConfig() {
        return SendNvmCommand(UART_NVM_CMD.CONFIG_LOAD);
    }
    public virtual bool ResetConfig() {
        return SendNvmCommand(UART_NVM_CMD.CONFIG_RESET);
    }

    public virtual bool SetOledSoftwareControl(bool enable) {

        byte[] txBuffer = ToByteArray(enable ? UART_CMD.UART_CMD_DISPLAY_SW_ENABLE : UART_CMD.UART_CMD_DISPLAY_SW_DISABLE);

        // Send command to device
        try {
            SendCommand(txBuffer, out _, 0);
        } catch {
            return false;
        }

        return true;

    }

    public virtual bool SendOledFramebuffer(byte[] frameBuffer) {

        if(frameBuffer.Length != 128*64/8) {
            return false;
        }

        // Send write framebuffer command to device
        byte[] txBuffer = ToByteArray(UART_CMD.UART_CMD_DISPLAY_SW_WRITE);
        try {
            SendCommand(txBuffer, out _, 0);
        } catch {
            return false;
        }

        // Send framebuffer
        try {
            SendCommand(frameBuffer, out _, 0);
        } catch {
            return false;
        }

        // Send framebuffer update command
        txBuffer = ToByteArray(UART_CMD.UART_CMD_DISPLAY_SW_UPDATE);
        try {
            SendCommand(txBuffer, out _, 0);
        } catch {
            return false;
        }

        return true;

    }


    #endregion

    #endregion

    #region Private Methods

    // Compare device id and store firmware version
    private bool CheckVendorData()
    {
        byte[] txBuffer = ToByteArray(UART_CMD.UART_CMD_READ_ID);
        SendCommand(txBuffer, out byte[] rxBuffer, 3);

        if (rxBuffer[0] != 0xEE || rxBuffer[1] != 0x0E) return false;

        FirmwareVersion = rxBuffer[2];
        return true;
    }

    // Data reception event
    private void SerialPortOnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        SerialPort serialPort = (SerialPort)sender;
        byte[] data = new byte[serialPort.BytesToRead];
        serialPort.Read(data, 0, data.Length);
        RxData.AddRange(data.ToList());
    }

    // Send command to EFC-X9
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

    private bool SendNvmCommand(UART_NVM_CMD cmd) {

        byte[] txBuffer = ToByteArray(UART_CMD.UART_CMD_NVM_CONFIG, 3);

        // Write key
        txBuffer[1] = 0x34;
        txBuffer[2] = 0x12;
        txBuffer[3] = (byte)cmd;

        // Send command
        try {
            SendCommand(txBuffer, out _, 0);
        } catch {
            return false;
        }

        return true;

    }

    #endregion
}