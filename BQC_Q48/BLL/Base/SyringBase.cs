using BQJX.Common.Interface;
using BQJX.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public abstract class SyringBase
    {
        #region Protected Members

        protected readonly IIoDevice _io;
        protected readonly ILS_Motion _motion;
        protected readonly ILogger _logger;
        protected readonly IGlobalStatus _globalStauts;
        #endregion

        #region Protected Variants

        protected bool _isAddSolve; //是否已经加液
        protected bool _isAbsorb;   //是否已经完成吸液
        protected ushort _axisAddLiquid; //加液注射器
        protected ushort _port1; //加液口1
        protected ushort _port2; //加液口2
        protected ushort _port3; //加液口3
        protected ushort _port4; //加液口4
        protected ushort _port5; //加液口5
        protected ushort _port6; //加液口6
        protected ushort _port7; //加液口7
        protected ushort _port8; //加液口8
        protected double _syringHomePos = 0; //注射器原点位置

        protected double _syringVel = 80;
        protected double _obsortVel = 50;

        protected int _step = 1;

        #endregion

        #region Construtors

        public SyringBase(IIoDevice io, ILS_Motion motion,ILogger logger, IGlobalStatus globalStauts)
        {
            this._motion = motion;
            this._io = io;
            this._logger = logger;
            this._globalStauts = globalStauts;
            _globalStauts.StopProgramEventArgs += StopMove;
        }


        #endregion

        #region Public Methods

        /// <summary>
        /// 注射器回零
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> GoHome(CancellationTokenSource cts)
        {
            try
            {
                _logger.Debug("Syring GoHome");
                //关闭全部阀口
                _io.WriteBit_DO(_port1, false);
                _io.WriteBit_DO(_port2, false);
                _io.WriteBit_DO(_port3, false);
                _io.WriteBit_DO(_port4, false);
                _io.WriteBit_DO(_port5, false);
                _io.WriteBit_DO(_port6, false);
                _io.WriteBit_DO(_port7, false);
                _io.WriteBit_DO(_port8, false);

                //使能轴
                await _motion.ServoOn(_axisAddLiquid).ConfigureAwait(false);

                var result = await _motion.GoHomeWithCheckDone(_axisAddLiquid, cts);
                _step = 1;
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
        }

        public bool StopMove()
        {
            _motion.StopMove(_axisAddLiquid);
            return true;
        }

        /// <summary>
        /// 注射器加液，Solve表示种类 用位表示
        /// </summary>
        /// <param name="solve">加液种类</param>
        /// <param name="volume">需要加液量0~40ml</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> AddSolve(byte solve, double volume, CancellationTokenSource cts)
        {
            _logger?.Debug($"AddSolve-{solve}-{volume},step{_step}");
            if (volume <= 10 && !_globalStauts.IsStopped)
            {
                return await AddSolveSub(solve, volume, null).ConfigureAwait(false);
            }
            if (volume >10 && volume <= 20 &&cts?.IsCancellationRequested != true)
            {
                if (_step == 1 && !_globalStauts.IsStopped)
                {
                    var ret1 = await AddSolveSub(solve, 10, null).ConfigureAwait(false);
                    if (!ret1)
                    {
                        throw new Exception("加液失败!");
                    }
                    _step++;
                }
                if (_step == 2 && !_globalStauts.IsStopped)
                {
                    var ret1 = await AddSolveSub(solve, volume -10, null).ConfigureAwait(false);
                    if (!ret1)
                    {
                        throw new Exception("加液失败!");
                    }
                    _step = 1;
                    return true;
                }
            }
            if (volume > 20 && volume <= 30 && !_globalStauts.IsStopped)
            {
                if (_step == 1 && !_globalStauts.IsStopped)
                {
                    var ret1 = await AddSolveSub(solve, 10, null).ConfigureAwait(false);
                    if (!ret1)
                    {
                        throw new Exception("加液失败!");
                    }
                    _step++;
                }
                if (_step == 2 && !_globalStauts.IsStopped)
                {
                    var ret1 = await AddSolveSub(solve, 10, null).ConfigureAwait(false);
                    if (!ret1)
                    {
                        throw new Exception("加液失败!");
                    }
                    _step++;
                }
                if (_step == 3 && !_globalStauts.IsStopped)
                {
                    var ret1 = await AddSolveSub(solve, volume - 20, null).ConfigureAwait(false);
                    if (!ret1)
                    {
                        throw new Exception("加液失败!");
                    }
                    _step = 1;
                    return true;
                }
            }
            if (volume >30 && volume <= 40 && !_globalStauts.IsStopped)
            {
                if (_step == 1 && !_globalStauts.IsStopped)
                {
                    var ret1 = await AddSolveSub(solve, 10, null).ConfigureAwait(false);
                    if (!ret1)
                    {
                        throw new Exception("加液失败!");
                    }
                    _step++;
                }
                if (_step == 2 && !_globalStauts.IsStopped)
                {
                    var ret1 = await AddSolveSub(solve, 10, null).ConfigureAwait(false);
                    if (!ret1)
                    {
                        throw new Exception("加液失败!");
                    }
                    _step++;
                }
                if (_step == 3 && !_globalStauts.IsStopped)
                {
                    var ret1 = await AddSolveSub(solve, 10, null).ConfigureAwait(false);
                    if (!ret1)
                    {
                        throw new Exception("加液失败!");
                    }
                    _step++;
                }
                if (_step == 4 && !_globalStauts.IsStopped)
                {
                    var ret1 = await AddSolveSub(solve, volume - 30, null).ConfigureAwait(false);
                    if (!ret1)
                    {
                        throw new Exception("加液失败!");
                    }
                    _step = 1;
                    return true;
                }
            }
            throw new Exception("加液失败!");
        }


        #endregion

        #region Protected Methods

        /// <summary>
        /// 加液
        /// </summary>
        /// <param name="solve">溶剂种类</param>
        /// <param name="volume"> 1- 10ml</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> AddSolveSub(byte solve, double volume, CancellationTokenSource cts = null)
        {
            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger?.Debug($"AddSolveSub-{solve}-{volume},isAddSolve{_isAddSolve},isAbsorb{_isAbsorb}");
            //判断是否完成加液,进行吸取空气复位阀
            if (_isAddSolve)
            {
                goto air;
            }
            //是否完成吸液
            if (_isAbsorb)
            {
                goto inject;
            }

            //关闭全部阀口
            _io.WriteBit_DO(_port1, false);
            _io.WriteBit_DO(_port2, false);
            _io.WriteBit_DO(_port3, false);
            _io.WriteBit_DO(_port4, false);
            _io.WriteBit_DO(_port5, false);
            _io.WriteBit_DO(_port6, false);
            _io.WriteBit_DO(_port7, false);
            _io.WriteBit_DO(_port8, false);

            //开始吸液
            var result = await _motion.P2pMoveWithCheckDone(_axisAddLiquid, volume, _obsortVel, cts).ConfigureAwait(false);
            if (!result)
            {
                throw new Exception("吸液失败");
            }

            //切换阀口
            if ((solve & 0x01) == 0x01)
            {
                _io.WriteBit_DO(_port1, true);
                _io.WriteBit_DO(_port5, true);
            }
            if ((solve & 0x02) == 0x02)
            {
                _io.WriteBit_DO(_port2, true);
                _io.WriteBit_DO(_port6, true);
            }
            if ((solve & 0x04) == 0x04)
            {
                _io.WriteBit_DO(_port3, true);
                _io.WriteBit_DO(_port7, true);
            }
            if ((solve & 0x08) == 0x08)
            {
                _io.WriteBit_DO(_port4, true);
                _io.WriteBit_DO(_port8, true);
            }
            //完成吸液
            _isAbsorb = true;

        //开始吐液
        inject: result = await _motion.P2pMoveWithCheckDone(_axisAddLiquid, _syringHomePos, _syringVel, cts).ConfigureAwait(false);
            if (!result)
            {
                throw new Exception("吐液失败");
            }
            _isAddSolve = true;  //完成加液
            _isAbsorb = false;   //复位完成吸液

            //开始回吸空气柱
            await Task.Delay(1000).ConfigureAwait(false);
        air: result = await _motion.P2pMoveWithCheckDone(_axisAddLiquid, _syringHomePos + 0.2, _obsortVel, cts).ConfigureAwait(false);
            if (!result)
            {
                throw new Exception("回吸空气柱失败");
            }

            //关闭全部阀口
            _io.WriteBit_DO(_port1, false);
            _io.WriteBit_DO(_port2, false);
            _io.WriteBit_DO(_port3, false);
            _io.WriteBit_DO(_port4, false);
            _io.WriteBit_DO(_port5, false);
            _io.WriteBit_DO(_port6, false);
            _io.WriteBit_DO(_port7, false);
            _io.WriteBit_DO(_port8, false);

            _isAddSolve = false;  //复位完成加液
            return true;


        }

        #endregion
    }
}
