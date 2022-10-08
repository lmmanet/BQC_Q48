using BQJX.Common.Interface;
using BQJX.Core.Interface;
using System;
using System.Threading.Tasks;
using System.Threading;
using BQJX.Common;
using Q_Platform.DAL;
using Q_Platform.Logger;

namespace Q_Platform.BLL
{
    public class CapperOne : CapperBase, ICapperOne
    {

        #region Private Members

        private static ILogger logger = new MyLogger(typeof(CapperOne));

        private readonly SyringBase _syring;

        private readonly ICarrierOne _carrier; 

        private readonly IAddSolid _addSolid;

        private readonly static object _lockObj = new object();

        #endregion

        #region Constructors

        public CapperOne(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, ICapperPosDataAccess dataAccess, ICarrierOne carrier, IAddSolid addSolid) : base(io, motion, globalStatus,dataAccess, logger)
        {
            this._carrier = carrier;
            this._addSolid = addSolid;

            _axisY = 5;
            _axisC1 = 6;
            _axisC2 = 7;
            _axisZ = 12;
            _holding = 16;
            _claw = 17;
            _holdingCloseSensor = 19;  //I1.3
            _holdingOpenSensor = 20;   //I1.4

            _xOffset = 68;    //加液X偏移量

            _posData = _dataAccess.GetCapperPosData(1);
            _syring = new SyringOne(io,motion, globalStatus);



        }
        public override void UpdatePosData()
        {
            _posData = _dataAccess.GetCapperPosData(1);
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// 回零
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        public override async Task<bool> GoHome(CancellationTokenSource cts)
        {
            try
            {
                if (cts?.IsCancellationRequested == true)
                {
                    throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
                }
                _logger?.Info($"拧盖模块1回零");
                //注射器回零
                var result1 = _syring.GoHome(cts).ConfigureAwait(false);
                //拧盖回零
                var result2 = base.GoHome(cts).ConfigureAwait(false);

                if (!await result1 || !await result2)
                {
                    throw new Exception($"回零出错result1:{result1.GetAwaiter().GetResult()}，result2:{result2.GetAwaiter().GetResult()}");
                }
            
                return true;
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested == true)
                {
                    _logger?.Info($"拧盖模块1回零 停止");
                    return false;
                }
                _logger?.Warn($"{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 装盖
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public override async Task<bool> CapperOnAsync(Sample sample, CancellationTokenSource cts)
        {
            //判断样品是否有盖

           s1: var result = await CapperOn(80, 40, cts).ConfigureAwait(false);

            if (!result)
            {
                if (_globalStatus.IsPause)
                {
                    while (_globalStatus.IsPause)
                    {
                        Thread.Sleep(2000);
                    }

                    if (!_globalStatus.IsStopped)
                    {
                        goto s1;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 拆盖
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public override async Task<bool> CapperOffAsync(Sample sample, CancellationTokenSource cts)
        {
            //判断样品是否有盖
            s1: var result = await CapperOff(cts, -0.85).ConfigureAwait(false);

            if (!result)
            {
                if (_globalStatus.IsPause)
                {
                    while (_globalStatus.IsPause)
                    {
                        Thread.Sleep(2000);
                    }

                    if (!_globalStatus.IsStopped)
                    {
                        goto s1;
                    }
                }
            }

            return result;
        }


        /// <summary>
        /// 加盐提取
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool AddSaltExtract(Sample sample, CancellationTokenSource cts)
        {
            //判断是否有加盐工艺
            if (!TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSolve2) && !TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSalt2))
            {
                return true;
            }

            try
            {
                lock (_lockObj)
                {
                    bool result;

                    //加溶剂 + 加盐
                    if (sample.SubStep < 2 && !_globalStatus.IsStopped)
                    {
                        result = _addSolid.AddSaltExtract(sample, AddSolve, null, null, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            return false;
                        }
                        sample.SubStep = 2;
                    }
                   
                    //是否装盖
                    if (sample.SubStep == 2 && !_globalStatus.IsStopped)
                    {
                        //装盖 内部判断在拧盖1就执行
                        result = MoveOut(sample, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            return false;
                        }
                        sample.SubStep++;
                    }
                   
                    //下料到试管架
                    if (sample.SubStep == 3 && !_globalStatus.IsStopped)
                    {
                        //下料到试管架
                        result = _carrier.GetSampleFromCapperOneToMaterial(sample, cts);
                        if (!result)
                        {
                            return false;
                        }
                        sample.SubStep++;
                    }

                    //完成
                    if (sample.SubStep == 4)
                    {
                        return true;
                    }
                    return false;
                }

            }
            catch (Exception ex)
            {
                _logger?.Warn(ex.Message);
                return false;
            }


        }

        /// <summary>
        /// 加溶剂提取
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool AddSolveExtract(Sample sample, CancellationTokenSource cts)
        {
            //判断是否有加溶剂工艺
            if (!TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSolve1) && !TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSalt1))
            {
                sample.MainStep = 3;
                return true;
            }

            try
            {
                lock (_lockObj)
                {
                    bool result;

                    //加溶剂 
                    if (sample.SubStep == 0 && !_globalStatus.IsStopped)
                    {
                        if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSolve1))
                        {
                            result = AddSolve(sample, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                return false;
                            }
                        }
                      
                        sample.SubStep++;
                    }

                    //加盐
                    if (sample.SubStep == 1 && !_globalStatus.IsStopped)
                    {
                        if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSalt1))
                        {
                            //加固重量
                            var weight = new double[] { sample.TechParams.AddHomo[1], sample.TechParams.Solid_B[1], sample.TechParams.Solid_C[1],
                        sample.TechParams.Solid_D[1], sample.TechParams.Solid_E[1],sample.TechParams.Solid_F[1] };

                            result = _addSolid.AddSolidAsync(sample, weight, null, null, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                return false;
                            }
                        }
                        sample.SubStep++;
                    }

                    //是否装盖
                    if (sample.SubStep == 2 && !_globalStatus.IsStopped)
                    {
                        //装盖 内部判断在拧盖1就执行
                        result = MoveOut(sample, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            return false;
                        }
                        sample.SubStep++;
                    }
                   
                    //下料到试管架
                    if (sample.SubStep == 3 && !_globalStatus.IsStopped)
                    {
                        ////下料到试管架
                        result = _carrier.GetSampleFromCapperOneToMaterial(sample, cts);
                        if (!result)
                        {
                            return false;
                        }
                        sample.SubStep++;
                    }

                    //完成
                    if (sample.SubStep == 4)
                    {
                        return true;
                    }
                    return false;
                }

            

            }
            catch (Exception ex)
            {
                _logger?.Warn(ex.Message);
                return false;
            }


        }

        /// <summary>
        /// 加水提取
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool AddWaterExtract(Sample sample, CancellationTokenSource cts)
        {
            //判断是否有加水工艺
            if (!TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddWater) && !TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddHomo)) 
            {
                sample.MainStep = 2;
                return true;
            }

            try
            {
                lock (_lockObj)
                {
                    bool result;

                    //加水  
                    if (sample.SubStep == 0 && !_globalStatus.IsStopped)
                    {
                        if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddWater))
                        {
                            result = AddWater(sample, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                return false;
                            }
                        }
                        
                        sample.SubStep = 1;
                    }

                    //加固
                    if ( sample.SubStep == 1 && !_globalStatus.IsStopped)
                    {
                        if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddHomo))
                        {
                            //加固重量
                            var weight = new double[] { sample.TechParams.AddHomo[0], sample.TechParams.Solid_B[0], sample.TechParams.Solid_C[0],
                        sample.TechParams.Solid_D[0], sample.TechParams.Solid_E[0],sample.TechParams.Solid_F[0] };

                            result = _addSolid.AddSolidAsync(sample, weight, null, null, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                return false;
                            }
                        }
                       
                        sample.SubStep = 2;
                    }

                    //是否装盖  =》 到上下料位
                    if (sample.SubStep == 2 && !_globalStatus.IsStopped)
                    {
                        //装盖 内部判断在拧盖1就执行
                        result = MoveOut(sample, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            return false;
                        }
                        sample.SubStep = 3;
                    }

                    //下料到试管架
                    if (sample.SubStep == 3 && !_globalStatus.IsStopped)
                    {
                        ////下料到试管架
                        result = _carrier.GetSampleFromCapperOneToMaterial(sample, cts);
                        if (!result)
                        {
                            return false;
                        }
                        sample.SubStep++;
                    }

                    //完成
                    if (sample.SubStep == 4)
                    {
                        return true;
                    }
                    return false;
                }

            }
            catch (Exception ex)
            {
                _logger?.Warn(ex.Message);
                return false;
            }

        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// 加液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> AddSolve(Sample sample, CancellationTokenSource cts)
        {
            try
            {

                ushort sampleId = sample.Id;
                _logger?.Info($"样品{sampleId}加液");
                //拧盖移动到上下料位
                var result = await MovePutGetPos(cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("拧盖移动到上下料位 出错") ;
                }

                //搬运试管到拧盖1  内部判断试管位置
                result = _carrier.GetSampleFromMaterialToCapperOne(sample,cts);
                if (!result)
                {
                    throw new Exception("搬运试管到拧盖1 出错");
                }

                //判断试管是否有盖 拆盖
                if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsUnCapped))
                {
                    result = await CapperOffAsync(sample,cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception("拆盖 出错");
                    }
                    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsUnCapped);
                }

                //加溶剂A   内部判断是否有盖  需要修改
                if (sample.TechParams.Solvent_A != 0)
                {
                    double volume = sample.TechParams.Solvent_A;
                    result = await AddSolve(0x02, volume, cts).ConfigureAwait(false);
                }
                //加溶剂B
                if (sample.TechParams.Solvent_B != 0)
                {
                    double volume = sample.TechParams.Solvent_B;
                    result = await AddSolve(0x04, volume, cts).ConfigureAwait(false);
                }
                //加溶剂C
                if (sample.TechParams.Solvent_C != 0)
                {
                    double volume = sample.TechParams.Solvent_C;
                    result = await AddSolve(0x08, volume, cts).ConfigureAwait(false);
                }
        
                //判断是否有后续动作
                if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSalt1)|| TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSalt2)|| TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddHomo))
                {
                    result = await MovePutGetPos(cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }
                }
             
                //完成
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Warn(ex.Message);
                throw ex;
            }
          
        }


         /// <summary>
        /// 加水
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> AddWater(Sample sample, CancellationTokenSource cts)
        {
            try
            {
                if (cts?.IsCancellationRequested == true)
                {
                    throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
                }
                ushort sampleId = sample.Id;
                _logger?.Info($"样品{sampleId}加液");
                //拧盖移动到上下料位
                var result = await MovePutGetPos(cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("拧盖移动到上下料位 出错") ;
                }

                //搬运试管到拧盖1  内部判断试管位置
                result = _carrier.GetSampleFromMaterialToCapperOne(sample,cts);
                if (!result)
                {
                    throw new Exception("搬运试管到拧盖1 出错");
                }

                //判断试管是否有盖 拆盖
                if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsUnCapped))
                {
                    result = await CapperOffAsync(sample,cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception("拆盖 出错");
                    }
                    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsUnCapped);
                }

                //加溶剂A   内部判断是否有盖  需要修改
                if (sample.TechParams.AddWater != 0)
                {
                    double volume = sample.TechParams.AddWater;  //加水量
                    result = await AddSolve(0x01, volume, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                }

                //判断是否有后续动作
                if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSalt1)|| TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSalt2)|| TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddHomo))
                {
                    result = await MovePutGetPos(cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }
                }
             
                //完成
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Warn(ex.Message);
                throw ex;
            }
          
        }


        /// <summary>
        /// 加液
        /// </summary>
        /// <param name="solve"></param>
        /// <param name="volume"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> AddSolve(byte solve,double volume, CancellationTokenSource cts)
        {
            try
            {

                if (cts?.IsCancellationRequested == true)
                {
                    throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
                }
                _logger?.Debug($"AddSolve-{solve}-{volume}");
                //抱夹夹紧
                _io.WriteBit_DO(_holding, true);

                //Y轴移动到加液位
                var result = await _motion.P2pMoveWithCheckDone(_axisY, _posData.AddLiquidPos, _yMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //开始加液
                result = await _syring.AddSolve(solve, volume, cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                return true;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 装盖及下料
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="getToMaterial"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> MoveOut(Sample sample,CancellationTokenSource cts)
        {
            try
            {
                if (cts?.IsCancellationRequested == true)
                {
                    throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
                }
                bool result;

                //判断是否在拧盖1
                if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperOne))
                {
                    return true;
                }

                //判断试管是否有盖 装盖
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsUnCapped))
                {
                    result = await CapperOnAsync(sample, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    SampleStatusHelper.ResetBit(sample, SampleStatus.IsUnCapped);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Warn(ex.Message);
                throw ex;
            }
        }
        

        #endregion

     



    }
}
