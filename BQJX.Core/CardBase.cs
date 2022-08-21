using BQJX.Core.Base;
using BQJX.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Core
{
    public class CardBase : ICardBase
    {

        #region Private Members

        private bool _isInitialed;

        private ILogger _logger;

        private ushort[] _cardIdList = new ushort[8];

        private uint TotalAxes;

        private string _filePath;

        #endregion

        #region Constructors

        public CardBase(ILogger logger)
        {
            this._logger = logger;
        }

        #endregion

        #region Public Methods

        public bool GetCaraIsIni()
        {
            return _isInitialed;
        }

        public async Task<int> Initialize(string filePath)
        {
            _filePath = filePath;

            if (_isInitialed)
            {
                return 0;
            }

            return await Task.Run(() =>
            {
                //初始化卡资源
                bool result = InitiCard();
                if (!result)
                {
                    return -1;
                }
                //下载配置数据
                result = DownLoadConfigFile(0, filePath);
                if (!result)
                {
                    _logger?.Error("下载配置文件失败！");
                    return -2;
                }

                //获取总线轴数
                LTDMC.nmc_get_total_axes(0, ref TotalAxes);

                //初始化绝对坐标
                int totalAxis = (int)TotalAxes;
                result = SetOffsetZero(0, totalAxis);

                return 0;
            }).ConfigureAwait(false);

        }

        public async Task<bool> Close()
        {
            if (!_isInitialed)
            {
                return false;
            }

            return await Task.Run(() =>
            {
                var ret = LTDMC.dmc_board_close();//控制卡关闭函数，释放系统资源
                if (ret != 0)
                {
                    _logger?.Error($"Close err:{ret}");
                }
                _isInitialed = false;
                return ret == 0;

            }).ConfigureAwait(false);

        }

        public async Task<bool> ResetFieldBus(int card)
        {
            var cardId = _cardIdList[card];
            var result = LTDMC.dmc_soft_reset(cardId);
            if (result != 0)
            {
                _logger?.Error($"ResetFieldBus err:{result}");
                return false;
            }
            await Task.Delay(15000).ConfigureAwait(false);
            await Close().ConfigureAwait(false);
            var ret = await Initialize(_filePath).ConfigureAwait(false);
            return ret == 0;

        }

        public bool SetOffsetZero(ushort cardNo, int axisCount)
        {
            double pos = 0.0;
            for (int i = 0; i < axisCount; i++)
            {
                ushort axis = (ushort)i;
                var ret = LTDMC.nmc_set_offset_pos(cardNo, axis, pos);
                if (ret != 0)
                {
                    return false;
                }
            }
            return true;

        }

        public bool DownLoadConfigFile(ushort cardNo, string filePath)
        {
            var ret = LTDMC.dmc_download_configfile(cardNo, filePath);
            return ret == 0;
        }

        public bool InitiCard()
        {
            if (_isInitialed)
            {
                return true;
            }

            short num = LTDMC.dmc_board_init();//获取卡数量
            if (num <= 0 || num > 8)
            {
                _logger?.Error($"初始卡失败! CardCount:{num}");
                _isInitialed = false;
                return false;
            }
            ushort _num = 0;
            uint[] cardtypes = new uint[8];
            short res = LTDMC.dmc_get_CardInfList(ref _num, cardtypes, _cardIdList);
            if (res != 0)
            {
                _logger?.Error($"获取卡信息失败! ret:{res}");
                _isInitialed = false;
                return false;
            }

            _isInitialed = true;
            return true;
        }

        public int GetFieldBusErrorCode(ushort cardNo)
        {
            ushort errCode = 0;
            var ret = LTDMC.nmc_get_errcode(cardNo, 2, ref errCode);
            if (ret != 0)
            {
                _logger?.Error($"GetFieldBusErrorCode err:{ret}");
                return -1;
            }
            return errCode;
        }

        public int GetFieldBusTotalSlaves(ushort cardNo)
        {
            ushort portNum = 2;
            ushort totalSlaves = 0;
            var ret = LTDMC.nmc_get_total_slaves(cardNo, portNum, ref totalSlaves);
            if (ret != 0 )
            {
                _logger?.Error($"GetFieldBusTotalSlaves err:{ret}");
            }
            return (int)totalSlaves;
        }

        public int GetFieldBusAxes(ushort cardNo)
        {
            uint totalAxes = 0;
            //获取总线轴数
            var ret =  LTDMC.nmc_get_total_axes(cardNo, ref totalAxes);
            if (ret != 0)
            {
                _logger?.Error($"GetFieldBusAxes err:{ret}");
            }
            return (int)totalAxes;
        }

        public bool ClearFieldBusError(ushort cardNo)
        {
            ushort portNum = 2;
            var ret = LTDMC.nmc_clear_errcode(cardNo, portNum);
            if (ret != 0)
            {
                _logger?.Error($"ClearFieldBusError err:{ret}");
                return false;
            }
            return true;
        }

        public bool StopFieldBus(ushort cardNo)
        {
            ushort state = 0;
            var ret = LTDMC.nmc_stop_etc(cardNo,ref state);
            if (ret != 0)
            {
                _logger?.Error($"StopFieldBus err:{ret}");
                return false;
            }
            return state == 0;
        }

        public int GetPDO(ushort cardNo, ushort modNo, ushort index, ushort subIndex, ushort valueLength)
        {
            ushort portNum = 2;
            int result = 0;
            var ret = LTDMC.nmc_get_node_od(cardNo, portNum, modNo, index, subIndex, valueLength, ref result);
            if (ret != 0)
            {
                _logger?.Error($"GetPDO err:{ret}");
            }
            return result;
        }

        public bool SetPDO(ushort cardNo,ushort modNo, ushort index, ushort subIndex, ushort valueLength, int value)
        {
            ushort portNum = 2;
            var ret = LTDMC.nmc_set_node_od(cardNo, portNum, modNo, index, subIndex, valueLength, value);
            if (ret != 0)
            {
                _logger?.Error($"SetPDO err:{ret}");
                return false;
            }
            return true;
        }

        //*******************************************************************************************************************//

        public bool Write_rxPDO_Extra(ushort cardNo,ushort address,ushort dataLen,int value)
        {
            ushort portNum = 2;
            var ret =  LTDMC.nmc_write_rxpdo_extra(cardNo, portNum, address, dataLen, value);
            if (ret != 0)
            {
                _logger?.Error($"Write_rxPDO_Extra err:{ret}");
                return false;
            }
            return true;
        }

        public int Read_rxPDO_Extra(ushort cardNo, ushort address, ushort dataLen)
        {
            ushort portNum = 2;
            int result = 0;
            var ret = LTDMC.nmc_read_rxpdo_extra(cardNo, portNum, address, dataLen, ref result);
            if (ret!=0)
            {
                _logger?.Error($"Read_rxPDO_Extra err:{ret}");
                return -1000;
            }
            return result;
        }

        public int Read_txPDO_Extra(ushort cardNo, ushort address, ushort dataLen)
        {
            ushort portNum = 2;
            int result = 0;
            var ret = LTDMC.nmc_read_txpdo_extra(cardNo, portNum, address, dataLen, ref result);
            if (ret != 0)
            {
                _logger?.Error($"Read_txPDO_Extra err:{ret}");
                return -1000;
            }
            return result;
        }

        public bool Write_rxPDO_Extra_uint(ushort cardNo, ushort address, ushort dataLen, uint value)
        {
            ushort portNum = 2;
            var ret = LTDMC.nmc_write_rxpdo_extra_uint(cardNo, portNum, address, dataLen, value);
            if (ret != 0)
            {
                _logger?.Error($"Write_rxPDO_Extra_uint err:{ret}");
                return false;
            }
            return true;
        }

        public uint Read_rxPDO_Extra_uint(ushort cardNo, ushort address, ushort dataLen)
        {
            ushort portNum = 2;
            uint result = 0;
            var ret = LTDMC.nmc_read_rxpdo_extra_uint(cardNo, portNum, address, dataLen, ref result);
            if (ret != 0)
            {
                _logger?.Error($"Read_rxPDO_Extra_uint err:{ret}");
                return 0;
            }
            return result;
        }

        public uint Read_txPDO_Extra_uint(ushort cardNo, ushort address, ushort dataLen)
        {
            ushort portNum = 2;
            uint result = 0;
            var ret = LTDMC.nmc_read_txpdo_extra_uint(cardNo, portNum, address, dataLen, ref result);
            if (ret != 0)
            {
                _logger?.Error($"Read_txPDO_Extra_uint err:{ret}");
                return 0;
            }
            return result;
        }

        #endregion

    }
}
