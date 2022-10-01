using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace EFC_Core
{

    public class EFC_Def
    {
        public const int VENDOR_ID = 0xEE;
        public const int PRODUCT_ID = 0x0E;
        public const int FIRMWARE_VERSION = 0x01;

        public const int CONFIG_VERSION = 0x00;

        public const int PROFILE_NUM = 2;

        public const int FAN_NUM = 9;
        public const int FAN_CURVE_NUM_POINTS = 2;

        public const int TS_NUM = 2;
    }

    public struct VendorDataStruct
    {
        public byte VendorId;
        public byte ProductId;
        public byte FwVersion;
    };

    public struct SensorStruct
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=EFC_Def.TS_NUM)] public Int16[] Ts;
        public Int16 Tamb;
        public Int16 Hum;
        public byte FanExt;
        public UInt16 Vin;
        public UInt16 Iin;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = EFC_Def.FAN_NUM)] public UInt16[] FanTach;
    };

    public enum FAN_MODE {
        FAN_MODE_TEMP_CONTROL,
        FAN_MODE_FIXED,
        FAN_MODE_EXT
    };

    public enum TEMP_SRC
    {
        TEMP_SRC_AUTO,
        TEMP_SRC_TS1,
        TEMP_SRC_TS2,
        TEMP_SRC_TAMB
    };

    public struct FanConfigStruct
    {
        public FAN_MODE FanMode;
        public TEMP_SRC TempSource;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = EFC_Def.FAN_CURVE_NUM_POINTS)] public Int16[] Temp;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = EFC_Def.FAN_CURVE_NUM_POINTS)] public byte[] Duty;
        public byte RampStep;
        public byte FixedDuty;
        public byte MinDuty;
        public byte MaxDuty;
    };

    public struct DeviceConfigStruct
    {
        public UInt16 Crc;
        public byte ActiveFanProfileId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = EFC_Def.PROFILE_NUM)] public ProfileConfigStruct[] FanProfile;
    };

    public struct ProfileConfigStruct
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = EFC_Def.FAN_NUM)] public FanConfigStruct[] FanConfig;
    }

}
