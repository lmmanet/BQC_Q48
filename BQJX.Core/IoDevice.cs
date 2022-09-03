using BQJX.Core.Base;
using BQJX.Core.Interface;
using System;

namespace BQJX.Core
{
    public class IoDevice : IIoDevice
    {

        #region Private Members

        private ushort _cardNo = 0;

        private ILogger _logger; 

        #endregion

        #region Construtors

        public IoDevice(ILogger logger)
        {
            this._logger = logger;
        }

        #endregion

        #region Public Methods

        public bool ReadBit_DI(ushort bitNO)
        {
            var ret = LTDMC.dmc_read_inbit(_cardNo, bitNO);
            return ret == 0;
        }

        public bool ReadBit_DO(ushort bitNO)
        {
            var ret = LTDMC.dmc_read_outbit(_cardNo, bitNO);
            return ret == 0;
        }

        public uint ReadByte_DI(ushort portNo)
        {
            var ret = LTDMC.dmc_read_inport(_cardNo, portNo);
            return ret;
        }

        public uint ReadByte_DO(ushort portNo)
        {
            var ret = LTDMC.dmc_read_outport(_cardNo, portNo);
            return ret;
        }

        public bool WriteByte_DO(ushort portNo, uint value)
        {
            var ret = LTDMC.dmc_write_outport(_cardNo, portNo, value);
            if (ret != 0)
            {
                _logger?.Error($"WriteByte_DO err:{ret}");
                throw new Exception($"WriteByte_DO err:{ret}");
            }
            return true;
        }

        public bool WriteBit_DO(ushort bitNO, bool value)
        {
            ushort writeValue = 0;
            if (!value)
            {
                writeValue = 1;
            }
            var ret = LTDMC.dmc_write_outbit(_cardNo, bitNO, writeValue);
            if (ret != 0)
            {
                _logger?.Error($"WriteBit_DO err:{ret}");
                throw new Exception($"WriteBit_DO err:{ret}");
            }
            return true;
        }

        public bool WriteBit_DO_Delay_Reverse(ushort bitNO, double delayTime)
        {
            //IO 输出延时  time 0为无限大

            var ret = LTDMC.dmc_reverse_outbit(_cardNo, bitNO, delayTime);
            if (ret != 0 )
            {
                _logger?.Error($"WriteBit_DO_Delay_Reverse err:{ret}");
                throw new Exception($"WriteBit_DO_Delay_Reverse err:{ret}");
            }
            return true;

        }

        public bool SetBit_DI_Count_Mode(ushort bitNO, ushort mode, double filterTime)
        {
            var ret = LTDMC.dmc_set_io_count_mode(_cardNo, bitNO, mode, filterTime);
            if (ret!=0)
            {
                _logger?.Error($"SetBit_DI_Count_Mode err:{ret}");
                throw new Exception($"SetBit_DI_Count_Mode err:{ret}");
            }
            return true;
        }

        public bool SetBit_DI_Count_Value(ushort bitNo, uint value)
        {
            var ret = LTDMC.dmc_set_io_count_value(_cardNo, bitNo, value);
            if (ret != 0)
            {
                _logger?.Error($"SetBit_DI_Count_Value err:{ret}");
                throw new Exception($"SetBit_DI_Count_Value err:{ret}");
            }
            return true;
        }

        public uint GetBit_DI_Count_Value(ushort bitNo)
        {
            uint result = 0;
            var ret = LTDMC.dmc_get_io_count_value(_cardNo, bitNo, ref result);
            if (ret != 0)
            {
                _logger?.Error($"GetBit_DI_Count_Value err:{ret}");
                throw new Exception($"GetBit_DI_Count_Value err:{ret}");
            }
            return result;
        }

        /////////////////////////////////////////////////////////////////
        public double ReadByte_AD(ushort portNo)
        {
            double result = 0;
            
            var ret = LTDMC.dmc_get_ad_input(_cardNo, portNo, ref result);
            if (ret != 0)
            {
                _logger?.Error($"ReadByte_AD err:{ret}");
                throw new Exception($"ReadByte_AD err:{ret}");
            }

            return result;
        }

        public bool WriteByte_DA(ushort portNo, double value)
        {
            var ret = LTDMC.dmc_set_da_output(_cardNo, portNo, value);
            if (ret != 0)
            {
                _logger?.Error($"WriteByte_DA err:{ret}");
                throw new Exception($"WriteByte_DA err:{ret}");
            }
            return true;
        }

        public double ReadDoubleDA(ushort portNo)
        {
            double result = 0;
            var ret = LTDMC.dmc_get_da_output(_cardNo,portNo,ref result);
            if (ret != 0)
            {
                _logger?.Error($"ReadDoubleDA err:{ret}");
                throw new Exception($"ReadDoubleDA err:{ret}");
            }

            return result;
        }

        public bool Config_AD(ushort slaveId, ushort slot, ushort channel, int mode, int filterTime)
        {
            ushort portNum = 2;
            ushort index_config = (ushort)(0x8000 + (16 * slot));
            ushort index_filter = (ushort)(0x8001 + (16 * slot));
            ushort ad_len = 16;
            var ret = LTDMC.nmc_set_node_od(_cardNo, portNum, slaveId, index_config, channel, ad_len, mode);
            if (ret != 0)
            {
                _logger?.Error($"Config_AD err{ret}");
                throw new Exception($"Config_AD err:{ret}");
            }

            ret = LTDMC.nmc_set_node_od(_cardNo, portNum, slaveId, index_filter, channel, ad_len, filterTime);
            if (ret != 0)
            {
                _logger?.Error($"Config_AD err{ret}");
                throw new Exception($"Config_AD err:{ret}");
            }

            return true;
        }

        public bool Config_DA(ushort slaveId, ushort slot, ushort channel, int mode, int enable)
        {
            ushort portNum = 2;
            ushort index_config = (ushort)(0x8000 + (16 * slot));
            ushort index_filter = (ushort)(0x8001 + (16 * slot));
            ushort ad_len = 16;
            var ret = LTDMC.nmc_set_node_od(_cardNo, portNum, slaveId, index_config, channel, ad_len, mode);
            if (ret != 0)
            {
                _logger?.Error($"Config_DA err{ret}");
                throw new Exception($"Config_DA err:{ret}");
            }

            ret = LTDMC.nmc_set_node_od(_cardNo, portNum, slaveId, index_filter, channel, ad_len, enable);
            if (ret != 0)
            {
                _logger?.Error($"Config_DA err{ret}");
                throw new Exception($"Config_DA err:{ret}");
            }

            return true;
        }


        #endregion

        #region MyDefine 

        public bool ReadBit_DI(string str)
        {
            ushort bitNo = CalaIoAddress(str);
            return ReadBit_DI(bitNo);
        }


        public bool ReadBit_DO(string str)
        {
            ushort bitNo = CalaIoAddress(str);
            return ReadBit_DI(bitNo);
        }

        public bool ResetBit_DO(string str)
        {
            ushort bitNo = CalaIoAddress(str);
            return WriteBit_DO(bitNo, false);
        }

        public bool SetBit_DO(string str)
        {
            ushort bitNo = CalaIoAddress(str);
            return WriteBit_DO(bitNo, true);
        }

      

        #endregion

        #region Private Methods

        private ushort CalaIoAddress(string ioString)
        {
            //Q0.0 Q10.0
            if (ioString.Length < 4)
            {
                _logger?.Error($"IO格式错误:{ioString } ");
                throw new InvalidOperationException($"IO格式错误:{ioString } ");
            }
            ushort main = ushort.Parse(ioString.Substring(1, ioString.Length - 3));
            ushort sub = ushort.Parse(ioString.Substring(ioString.Length - 1, 1));

            return (ushort)(main * 8 + sub);
        } 

        #endregion

    }
}
