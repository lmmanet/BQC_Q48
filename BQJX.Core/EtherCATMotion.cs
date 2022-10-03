using BQJX.Core.Base;
using System;
using System.Threading.Tasks;
using BQJX.Core.Common;
using BQJX.Core.Interface;
using System.Collections.Generic;
using System.Threading;

namespace BQJX.Core
{
    public class EtherCATMotion : IEtherCATMotion
    {

        #region Private Members

        private ILogger _logger;

        private ushort _CardID = 0;

        private List<AxisEleGear> _eleGearList;

        private List<int> _coordinates = new List<int>();

        private readonly object _lockObj = new object();

        #endregion

        #region Construtors
        public EtherCATMotion(List<AxisEleGear> list, ILogger logger)
        {
            this._eleGearList = list;
            this._logger = logger;
        } 

        #endregion

        #region Public Methods

        public List<AxisEleGear> GetAxisInfos()
        {
            return _eleGearList;
        }

        public async Task<bool> GohomeWithCheckDone(ushort axisNo, ushort homeMode, CancellationTokenSource cts)
        {
            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            AxisEleGear ele = _eleGearList.Find(P => P.AxisNo == axisNo);
            ushort mode = homeMode;
            double lowVel = 1;
            double highVel = 10;
            double accT = 0.1;
            double decT = 0.1;
            double offset = ele.HomeOffset;
            var result = LTDMC.nmc_set_home_profile(_CardID, axisNo, mode, lowVel, highVel, accT, decT, offset);
            if (result != 0)
            {
                _logger.Error($"set_home_profile err:{result}");
                throw new EtherCATMotionException($"set_home_profile err:{result}");
            }
            result = LTDMC.nmc_home_move(_CardID, axisNo);//执行回原点运动
            if (result != 0)
            {
                _logger.Error($"home_move err:{result}");
                throw new EtherCATMotionException($"home_move err:{result}");
            }
            return await CheckDone(axisNo, cts);
        }

        public async Task<bool> GohomeWithCheckDone(ushort axisNo, ushort homeMode, double offset, CancellationTokenSource cts)
        {
            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            AxisEleGear ele = _eleGearList.Find(P => P.AxisNo == axisNo);
            ushort mode = homeMode;
            double lowVel = 1;
            double highVel = 10;
            double accT = 0.1;
            double decT = 0.1;
            var result = LTDMC.nmc_set_home_profile(_CardID, axisNo, mode, lowVel, highVel, accT, decT, offset);
            if (result != 0)
            {
                _logger.Error($"set_home_profile err:{result}");
                throw new EtherCATMotionException($"set_home_profile err:{result}");
            }
            result = LTDMC.nmc_home_move(_CardID, axisNo);//执行回原点运动
            if (result != 0)
            {
                _logger.Error($"home_move err:{result}");
                throw new EtherCATMotionException($"home_move err:{result}");
            }
            return await CheckDone(axisNo, cts);

        }


        public async Task<bool> InterPolation_2D_lineWithCheckDone(ushort[] axisNo, double[] PositionArray, double velocity, CancellationTokenSource cts)
        {
            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            ushort AxisNum = 2; //插补轴数
            ushort Crd = 0;//坐标系号

            lock (_lockObj)
            {
                for (int i = 0; i < 3; i++)
                {
                    var b = _coordinates.Contains(i);
                    if (b)
                    {
                        continue;
                    }
                    Crd = (ushort)i;
                    _coordinates.Add(Crd);
                    break;
                }
            }
         
           

            AxisEleGear ele = _eleGearList.Find(P => P.AxisNo == axisNo[0]);
            double Tacc = ele.Tacc;
            double Tdec = ele.Tdec;
            double StopVel = 0;
            double MaxVel = velocity * ele.EleGear;
            double StartVel = 0;

            var ret = LTDMC.dmc_set_vector_profile_unit(_CardID, Crd, StartVel, MaxVel, Tacc, Tdec, StopVel);
            if (ret != 0)
            {
                _logger?.Error($"InterPolation_2D_lineWithCheckDone err:{ret}");
                throw new EtherCATMotionException($"InterPolation_2D_lineWithCheckDone err:{ret}");
            }

            double[] targetPos = new double[2]
            {
                PositionArray[0]*ele.EleGear,
                PositionArray[1]*ele.EleGear
            };

            ret = LTDMC.dmc_line_unit(_CardID, Crd, AxisNum, axisNo, targetPos, 1);
            if (ret != 0)
            {
                _logger?.Error($"InterPolation_2D_lineWithCheckDone err:{ret}");
                throw new EtherCATMotionException($"InterPolation_2D_lineWithCheckDone err:{ret}");
            }

            var result = await CheckDone(axisNo[0], cts).ConfigureAwait(false);
            _coordinates.Remove(Crd);
            return result;

        }

        public void JogMove(ushort axisNo, double velocity, ushort direction)
        {
            ushort axis = axisNo;
            AxisEleGear ele = _eleGearList.Find(P => P.AxisNo == axisNo);
            double dStartVel = ele.StartVel;                       //起始速度
            double dMaxVel = velocity * ele.EleGear;         //运行速度
            double dTacc = ele.Tacc;                         //加速时间 S
            double dTdec = ele.Tdec;                         //减速时间 S
            double dStopVel = ele.StopVel;                        //停止速度
            double dS_para = ele.S_param;                      //S段时间 s (0~1)

            var ret = LTDMC.dmc_set_profile_unit(_CardID, axis, dStartVel, dMaxVel, dTacc, dTdec, dStopVel);//设置速度参数
            if (ret != 0)
            {
                _logger?.Error($"dmc_set_profile err:{ret}");
                throw new EtherCATMotionException($"dmc_set_profile err:{ret}");
            }
            ret = LTDMC.dmc_set_s_profile(_CardID, axis, 0, dS_para);//设置S段速度参数
            if (ret != 0)
            {
                _logger?.Error($"dmc_set_s_profile err:{ret}");
                throw new EtherCATMotionException($"dmc_set_s_profile err:{ret}");
            }
            ret = LTDMC.dmc_vmove(_CardID, axis, direction);//连续运动
            if (ret != 0)
            {
                _logger?.Error($"dmc_vmove err:{ret}");
                throw new EtherCATMotionException($"dmc_vmove err:{ret}");
            }
        }

        public void JogStop(ushort axisNo)
        {
            ushort axis = axisNo; //轴号
            ushort stop_mode = 0; //制动方式，0：减速停止，1：紧急停止

            var ret = LTDMC.dmc_stop(_CardID, axis, stop_mode);
            if (ret != 0)
            {
                _logger?.Error($"StopMove axisNo{axisNo} err!");
                throw new EtherCATMotionException($"StopMove axisNo{axisNo} err!");
            }
        }

        public bool P2pMove(ushort axisNo, double offset, double velocity)
        {
            AxisEleGear ele = _eleGearList.Find(P => P.AxisNo == axisNo);
            ushort axis = axisNo;
            double dStartVel = ele.StartVel;                       //起始速度
            double dMaxVel = velocity * ele.EleGear;    //运行速度
            double dTacc = ele.Tacc;                         //加速时间 S
            double dTdec = ele.Tdec;                         //减速时间 S
            double dStopVel = ele.StopVel;                        //停止速度
            double dS_para = ele.S_param;                      //S段时间 s (0~1)
            double dDist = offset * ele.EleGear;    //目标位置
            ushort sPosi_mode = 1;                      //运动模式0：相对坐标模式，1：绝对坐标模式

            if (!IsServeOn(axis))
            {
                ServoOn(axis);
                Thread.Sleep(1000);
            }

            var ret = LTDMC.dmc_set_profile_unit(_CardID, axis, dStartVel, dMaxVel, dTacc, dTdec, dStopVel);//设置速度参数
            if (ret != 0)
            {
                _logger?.Error($"dmc_set_profile err:{ret}");
                throw new EtherCATMotionException($"dmc_set_profile err:{ret}");
            }
            ret = LTDMC.dmc_set_s_profile(_CardID, axis, 0, dS_para);//设置S段速度参数
            if (ret != 0)
            {
                _logger?.Error($"dmc_set_s_profile err:{ret}");
                throw new EtherCATMotionException($"dmc_set_s_profile err:{ret}");
            }
            ret = LTDMC.dmc_set_dec_stop_time(_CardID, axis, dTdec); //设置减速停止时间
            if (ret != 0)
            {
                _logger?.Error($"dmc_set_dec_stop_time err:{ret}");
                throw new EtherCATMotionException($"dmc_set_dec_stop_time err:{ret}");
            }
            ret = LTDMC.dmc_pmove_unit(_CardID, axis, dDist, sPosi_mode);//定长运动
            if (ret != 0)
            {
                _logger?.Error($"dmc_pmove err:{ret}");
                throw new EtherCATMotionException($"dmc_pmove err:{ret}");
            }
            return true;
        }

        public async Task<bool> P2pMoveWithCheckDone(ushort axisNo, double offset, double velocity, CancellationTokenSource cts)
        {
            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            var result = P2pMove(axisNo, offset, velocity);
            if (!result)
            {
                return false;
            }
            return await CheckDone(axisNo, cts);
        }

        public bool RelativeMove(ushort axisNo, double offset, double velocity)
        {
            AxisEleGear ele = _eleGearList.Find(P => P.AxisNo == axisNo);
            ushort axis = axisNo;
            double dStartVel = ele.StartVel;                       //起始速度
            double dMaxVel = velocity * ele.EleGear;    //运行速度
            double dTacc = ele.Tacc;                         //加速时间 S
            double dTdec = ele.Tdec;                         //减速时间 S
            double dStopVel = ele.StopVel;                        //停止速度
            double dS_para = ele.S_param;                      //S段时间 s (0~1)
            double dDist = offset * ele.EleGear;    //目标位置
            ushort sPosi_mode = 0;                      //运动模式0：相对坐标模式，1：绝对坐标模式

            if (!IsServeOn(axis))
            {
                ServoOn(axis);
                Thread.Sleep(1000);
            }

            var ret = LTDMC.dmc_set_profile_unit(_CardID, axis, dStartVel, dMaxVel, dTacc, dTdec, dStopVel);//设置速度参数
            if (ret != 0)
            {
                _logger?.Error($"dmc_set_profile err:{ret}");
                throw new EtherCATMotionException($"dmc_set_profile err:{ret}");
            }
            ret = LTDMC.dmc_set_s_profile(_CardID, axis, 0, dS_para);//设置S段速度参数
            if (ret != 0)
            {
                _logger?.Error($"dmc_set_s_profile err:{ret}");
                throw new EtherCATMotionException($"dmc_set_s_profile err:{ret}");
            }
            ret = LTDMC.dmc_set_dec_stop_time(_CardID, axis, dTdec); //设置减速停止时间
            if (ret != 0)
            {
                _logger?.Error($"dmc_set_dec_stop_time err:{ret}");
                throw new EtherCATMotionException($"dmc_set_dec_stop_time err:{ret}");
            }
            ret = LTDMC.dmc_pmove_unit(_CardID, axis, dDist, sPosi_mode);//定长运动
            if (ret != 0)
            {
                _logger?.Error($"dmc_pmove err:{ret}");
                throw new EtherCATMotionException($"dmc_pmove err:{ret}");
            }
            return true;
        }

        public async Task<bool> RelativeMoveWithCheckDone(ushort axisNo, double offset, double velocity, CancellationTokenSource cts)
        {
            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            var result = RelativeMove(axisNo, offset, velocity);
            if (!result)
            {
                return false;
            }
            return await CheckDone(axisNo, cts);
        }

        public bool VelocityMove(ushort axisNo, double velocity, ushort direction)
        {
            AxisEleGear ele = _eleGearList.Find(P => P.AxisNo == axisNo);
            ushort axis = axisNo;                          //轴号
            double dStartVel = ele.StartVel;                          //起始速度
            double dMaxVel = velocity * ele.EleGear;       //运行速度
            double dTacc = ele.Tacc;                              //加速时间
            double dTdec = ele.Tdec;                              //减速时间
            double dStopVel = ele.StopVel;                           //停止速度
            double dS_para = ele.S_param;//0.01;                     //S段时间
            ushort sDir = direction;                       //运动方向，0：负方向，1：正方向

            if (!IsServeOn(axis))
            {
                ServoOn(axis);
                Thread.Sleep(1000);
            }

            var ret = LTDMC.dmc_set_profile_unit(_CardID, axis, dStartVel, dMaxVel, dTacc, dTdec, dStopVel);//设置速度参数
            if (ret != 0)
            {
                _logger?.Error($"dmc_set_profile err:{ret}");
                throw new EtherCATMotionException($"dmc_set_profile err:{ret}");
            }
            ret = LTDMC.dmc_set_s_profile(_CardID, axis, 0, dS_para);//设置S段速度参数
            if (ret != 0)
            {
                _logger?.Error($"dmc_set_s_profile err:{ret}");
                throw new EtherCATMotionException($"dmc_set_s_profile err:{ret}");
            }
            ret = LTDMC.dmc_set_dec_stop_time(_CardID, axis, dTdec); //设置减速停止时间
            if (ret != 0)
            {
                _logger?.Error($"dmc_set_dec_stop_time err:{ret}");
                throw new EtherCATMotionException($"dmc_set_dec_stop_time err:{ret}");
            }
            ret = LTDMC.dmc_vmove(_CardID, axis, sDir);//连续运动
            if (ret != 0)
            {
                _logger?.Error($"dmc_vmove err:{ret}");
                throw new EtherCATMotionException($"dmc_vmove err:{ret}");
            }
            return true;
        }

        public bool StopMove(ushort axisNo)
        {
            ushort axis = axisNo; //轴号
            ushort stop_mode = 0; //制动方式，0：减速停止，1：紧急停止

            var ret = LTDMC.dmc_stop(_CardID, axis, stop_mode);
            if (ret != 0)
            {
                _logger?.Error($"StopMove axisNo{axisNo} err!");
                throw new EtherCATMotionException($"StopMove axisNo{axisNo} err!");
            }

            return true;
        }

        public bool Emg_stop(ushort axisNo)
        {
            ushort axis = (ushort)axisNo; //轴号
            ushort stop_mode = 1; //制动方式，0：减速停止，1：紧急停止

            var ret = LTDMC.dmc_stop(_CardID, axis, stop_mode);
            if (ret != 0)
            {
                _logger?.Error($"Emg_stop axisNo{axisNo} err!");
                throw new EtherCATMotionException($"Emg_stop axisNo{axisNo} err!");
            }

            return true;
        }

        public bool ServoOn(ushort axisNo)
        {
            var ret = LTDMC.nmc_set_axis_enable(_CardID, axisNo);
            if (ret != 0)
            {
                _logger?.Error($"ServoOn err:{ret}");
                throw new EtherCATMotionException($"ServoOn err:{ret}");
            }
            return true;
        }

        public bool ServoOff(ushort axisNo)
        {
            var ret = LTDMC.nmc_set_axis_disable(_CardID, axisNo);
            if (ret != 0)
            {
                _logger?.Error($"ServeOff err:{ret}");
                throw new EtherCATMotionException($"ServeOff err:{ret}");
            }
            return true;
        }

        public bool ResetAxisAlm(ushort axisNo)
        {
            var ret = LTDMC.nmc_clear_axis_errcode(_CardID, axisNo);
            if (ret != 0)
            {
                _logger?.Error($"ResetAxisAlm err:{ret}");
                throw new EtherCATMotionException($"ResetAxisAlm err:{ret}");
            }
            return true;
        }

        public bool ConfigSoftLimit(ushort axisNo, ushort enableLimit, double nLimit, double pLimit)
        {
            AxisEleGear ele = _eleGearList.Find(P => P.AxisNo == axisNo);
            ushort axis = axisNo;                                      // 指定轴号
            ushort enable = enableLimit;                               // 使能状态，0：禁止，1：允许
            ushort source_sel = 1;                                     // 计数器选择，0：指令位置计数器，1：编码器计数器
            ushort SL_action = 0;                                      // 限位停止方式，0：立即停止 1：减速停止
            int N_limit = (int)(nLimit * ele.EleGear);                 // 负限位位置，单位：uint
            int P_limit = (int)(pLimit * ele.EleGear);                 // 正限位位置，单位：uint
            var ret = LTDMC.dmc_set_softlimit(_CardID, axis, enable, source_sel, SL_action, N_limit, P_limit);
            if (ret != 0)
            {
                _logger?.Error($"ConfigSoftLimit err:{ret}");
                throw new EtherCATMotionException($"ConfigSoftLimit err:{ret}");
            }
            return true;
        }

        public bool ConfigSoftLimit(ushort axisNo, ushort enableLimit)
        {
            AxisEleGear ele = _eleGearList.Find(P => P.AxisNo == axisNo);
            ushort axis = axisNo;                                      // 指定轴号
            ushort enable = enableLimit;                               // 使能状态，0：禁止，1：允许
            ushort source_sel = 1;                                     // 计数器选择，0：指令位置计数器，1：编码器计数器
            ushort SL_action = 0;                                      // 限位停止方式，0：立即停止 1：减速停止
            int N_limit = (int)(ele.nLimit * ele.EleGear);             // 负限位位置，单位：uint
            int P_limit = (int)(ele.pLimit * ele.EleGear);             // 正限位位置，单位：uint
            var ret = LTDMC.dmc_set_softlimit(_CardID, axis, enable, source_sel, SL_action, N_limit, P_limit);
            if (ret != 0)
            {
                _logger?.Error($"ConfigSoftLimitWithOutValue err:{ret}");
                throw new EtherCATMotionException($"ConfigSoftLimitWithOutValue err:{ret}");
            }
            return true;
        }

        public int Get_stop_code(ushort axisNo)
        {
            ushort axis = axisNo;
            int stopCode = 0;
            var ret = LTDMC.dmc_get_stop_reason(_CardID, axis, ref stopCode);
            if (ret != 0)
            {
                _logger?.Error($"Get_stop_code err:{ret}");
                throw new EtherCATMotionException($"Get_stop_code err:{ret}");
            }
            return stopCode;
        }

        public double GetCurrentPos(ushort axisNo)
        {
            AxisEleGear ele = _eleGearList.Find(p => p.AxisNo == axisNo);
            ushort axis = axisNo;
            double pos = 0;
            var ret = LTDMC.dmc_get_encoder_unit(_CardID, axis, ref pos); //读取指定轴指令位置值
            int pii = LTDMC.dmc_get_encoder(_CardID, axis);
            if (ret != 0)
            {
                _logger?.Error($"GetCurrentPos err:{ret}");
                throw new EtherCATMotionException($"GetCurrentPos err:{ret}");
            }
            double currentPos = Math.Round(pos / ele.EleGear, 3);
            return currentPos;
        }

        public double GetCurrentVel(ushort axisNo)
        {
            AxisEleGear ele = _eleGearList.Find(p => p.AxisNo == axisNo);
            ushort axis = axisNo;
            double speed = 0;
            var ret = LTDMC.dmc_read_current_speed_unit(_CardID, axis, ref speed); // 读取轴当前速度
            if (ret != 0)
            {
                _logger?.Error($"GetCurrentVel err:{ret}");
                throw new EtherCATMotionException($"GetCurrentVel err:{ret}");
            }
            double currentPos = Math.Round(speed / ele.EleGear, 3);
            return currentPos;
        }

        public uint GetMotionIoStatus(ushort axisNo)
        {
            var result = LTDMC.dmc_axis_io_status(_CardID, axisNo);

            return result;
        }

        public ushort GetMotionStatus(ushort axisNo)
        {
            ushort axis = axisNo; //轴号
            ushort status = 0;
            var ret = LTDMC.nmc_get_axis_state_machine(_CardID, axis, ref status);
            if (ret != 0)
            {
                _logger?.Error($"GetMotionStatus err:{ret}");
                throw new EtherCATMotionException($"GetMotionStatus err:{ret}");
            }

            return status;
        }

        public bool AbsSysClear(ushort axisNo)
        {
            ushort homeMode = 35;
            double lowVel = 500;
            double highVel = 1000;
            double accT = 0.1;
            double decT = 0.1;
            double offset = 0;
            var result = LTDMC.nmc_set_home_profile(_CardID, axisNo, homeMode, lowVel, highVel, accT, decT, offset);
            if (result != 0)
            {
                _logger.Error($"set_home_profile err:{result}");
                throw new EtherCATMotionException($"set_home_profile err:{result}");
            }
            result = LTDMC.nmc_home_move(_CardID, axisNo);//执行回原点运动
            if (result != 0)
            {
                _logger.Error($"home_move err:{result}");
                throw new EtherCATMotionException($"home_move err:{result}");
            }
            return true;
        }

        public async Task<bool> CheckDone(ushort axisNo, CancellationTokenSource cts)
        {
            //double factor = 1; //编码器系数
            //int error = 1000;  //位置误差带 Pulse
            //LTDMC.dmc_set_factor_error(_CardID,axisNo, factor,error);
      
            ushort axis = axisNo; //轴号
            return await Task.Run(() =>
            {
                short status = 0;
                do
                {
                    Thread.Sleep(500);
                    status = LTDMC.dmc_check_done(_CardID, axis);
                    if (status != 0) // 读取指定轴运动状态
                    {
                        var ret = LTDMC.dmc_check_success_pulse(_CardID, axis);//检测指令到位
                        //ret = LTDMC.dmc_check_success_encoder(_CardID, axis); // 检测编码器到位
                        if (ret != 1)
                        {
                            _logger?.Error($"CheckDone 指令未到位");
                            break;
                        }
                        return true;
                    }
                } while (cts?.IsCancellationRequested != true);
                if (cts?.IsCancellationRequested == true)
                {
                    throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
                }
                return false;
            }).ConfigureAwait(false);

        }

        public async Task<bool> CheckDoneMulti(ushort cardNo, ushort coordinate, CancellationTokenSource cts)
        {
            return await Task.Run(() =>
            {
                short status = 0;
                do
                {
                    status = LTDMC.dmc_check_done_multicoor(cardNo, coordinate);
                    if (status == 0)
                    {
                        return true;
                    }
                    Thread.Sleep(1000);
                } while (cts?.IsCancellationRequested == false);
                if (cts?.IsCancellationRequested == true)
                {
                    throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
                }
                return false;
              
            }).ConfigureAwait(false);
        }

        public void SetOffset(ushort axisNo)
        {
            ushort axis = axisNo;
            double offset = 0;
            var ret = LTDMC.nmc_set_offset_pos(_CardID, axis, offset);
            if (ret != 0)
            {
                _logger?.Error($"SetOffset err:{ret}");
                throw new EtherCATMotionException($"SetOffset err:{ret}");
            }
        }

        public bool SetPositonToSystem(ushort axisNo, double pos)
        {
            ushort axis = axisNo; //轴号
            AxisEleGear ele = _eleGearList.Find(p => p.AxisNo == axisNo);
            int dpos = (int)(pos * ele.EleGear);// 当前位置

            var ret = LTDMC.dmc_set_position(_CardID, axis, dpos); //设置指定轴的当前指令位置值
            if (ret != 0)
            {
                _logger?.Error($"SetPositonToSystem err:{ret}!");
                throw new EtherCATMotionException($"SetPositonToSystem err:{ret}!");
            }
            return true;
        }

        public void WriteDebugLog(string fileName)
        {
            ushort mode = 1;//0:值打印报错函数  1：全部打印  2：全部不打印
            var ret = LTDMC.dmc_set_debug_mode(mode, fileName);
            if (ret != 0)
            {
                _logger?.Error($"WriteDebugLog err:{ret}");
                throw new EtherCATMotionException($"WriteDebugLog err:{ret}");
            }
        }

        public uint GetAxisAlmCode(ushort axisNo)
        {
            ushort errCode = 0;
            var ret = LTDMC.nmc_get_axis_errcode(_CardID, axisNo, ref errCode);
            if (ret != 0)
            {
                _logger?.Error($"GetAxisAlmCode err:{ret}");
                throw new EtherCATMotionException($"GetAxisAlmCode err:{ret}");
            }
            return errCode;
        }

        public bool SetAxisIoMap(ushort axisNo,ushort ioType,ushort mapIoIndex ,int filterTime)
        {
            var ret = LTDMC.dmc_set_axis_io_map(_CardID, axisNo, ioType, ioType, mapIoIndex, filterTime);
            if (ret != 0)
            {
                _logger?.Error($"SetAxisIoMap err:{ret}");
                throw new EtherCATMotionException($"SetAxisIoMap err:{ret}");
            }
            return true;
        }






        #endregion


        public int Get_actual_torque(ushort axisNo)
        {
            //0x6077
            throw new NotImplementedException();
        }
        public async Task<bool> TorqueMoveWithCheckDone(ushort axisNo, short targetTorque, uint torqueSlope, uint velocity, double maxOffset, CancellationTokenSource cts)
        {
            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            throw new NotImplementedException();
        }

        public bool StopTorqueMove(ushort axisNo)
        {
            throw new NotImplementedException();
        }

        public bool TorqueMove(ushort axisNo, short targetTorque, uint torqueSlope, uint velocity)
        {
            throw new NotImplementedException();
        }

        public bool IsServeOn(ushort axisNo)
        {
            var status = GetMotionIoStatus(axisNo);
            if ((status & 4) == 4)
            {
                return true;
            }
            return false;
        }

        #region Private Methods


        private void change_speed(ushort axisNo, double velocity)
        {
            AxisEleGear ele = _eleGearList.Find(p => p.AxisNo == axisNo);
            ushort axis = axisNo; //轴号
            double dNewVel = velocity * ele.EleGear;// 新的运行速度
            double dTaccDec = 0.5;//变速时间 S

            LTDMC.dmc_change_speed(_CardID, axis, dNewVel, dTaccDec); //在线变速

        }

        private void change_target_position(ushort axisNo, double target)
        {
            AxisEleGear ele = _eleGearList.Find(p => p.AxisNo == axisNo);
            ushort axis = axisNo; //轴号
            int dNewPos = (int)(target * ele.EleGear);// 新的目标位置

            LTDMC.dmc_reset_target_position(_CardID, axis, dNewPos, 1);//在线变位
        } 

        #endregion

    }

}
