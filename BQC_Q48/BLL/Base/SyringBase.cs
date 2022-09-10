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

        protected readonly IIoDevice _io;
        protected readonly ILS_Motion _motion;


        protected bool _isAddSolve; //是否已经加液
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

        protected double _syringVel = 20;
        protected double _obsortVel = 10;


        #region Construtors

        public SyringBase(IIoDevice io, ILS_Motion motion)
        {
            this._motion = motion;
            this._io = io;
        
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
            //关闭全部阀口
            _io.WriteBit_DO(_port1, false);
            _io.WriteBit_DO(_port2, false);
            _io.WriteBit_DO(_port3, false);
            _io.WriteBit_DO(_port4, false);
            _io.WriteBit_DO(_port5, false);
            _io.WriteBit_DO(_port6, false);
            _io.WriteBit_DO(_port7, false);
            _io.WriteBit_DO(_port8, false);

            var result = await _motion.GoHomeWithCheckDone(_axisAddLiquid, cts);
            return result;

        }

        /// <summary>
        /// 注射器加液，Solve表示种类 用位表示
        /// </summary>
        /// <param name="solve">加液种类</param>
        /// <param name="volume">需要加液量</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> AddSolve(byte solve, double volume, CancellationTokenSource cts)
        {
            //判断是否完成加液
            if (_isAddSolve)
            {
                return true;
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
                return false;
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
                _io.WriteBit_DO(_port7, true);
            }
            if ((solve & 0x04) == 0x04)
            {
                _io.WriteBit_DO(_port3, true);
                _io.WriteBit_DO(_port6, true);
            }
            if ((solve & 0x08) == 0x08)
            {
                _io.WriteBit_DO(_port4, true);
                _io.WriteBit_DO(_port8, true);
            }

            //开始吐液
            result = await _motion.P2pMoveWithCheckDone(_axisAddLiquid, _syringHomePos, _syringVel, cts).ConfigureAwait(false);
            if (!result)
            {
                return false;
            }
            _isAddSolve = true;  //完成加液

            //开始回吸空气柱
            await Task.Delay(1000).ConfigureAwait(false);
            result = await _motion.P2pMoveWithCheckDone(_axisAddLiquid, _syringHomePos + 0.2, _syringVel, cts).ConfigureAwait(false);
            if (!result)
            {
                return false;
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
