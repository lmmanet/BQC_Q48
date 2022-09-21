using BQJX.Common;
using BQJX.Common.Common;
using BQJX.Common.Interface;
using BQJX.Core.Interface;
using Q_Platform.DAL;
using Q_Platform.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class CapperTwo : CapperBase, ICapperTwo
    {

        private static ILogger logger = new MyLogger(typeof(CapperTwo));

        private readonly ICarrierOne _carrier;

        #region Construtors

        public CapperTwo(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, ICapperPosDataAccess dataAccess, ICarrierOne carrier) : base(io, motion, globalStatus, dataAccess, logger)
        {
            this._carrier = carrier;
            _axisY = 9;
            _axisC1 = 10;
            _axisC2 = 11;
            _axisZ = 13;
            _holding = 19;
            _claw = 20;
            _holdingCloseSensor = 22;  //I1.6
            _holdingOpenSensor = 23;   //I1.7

            _xOffset = 60;    //拧盖X偏移量
            _capperTorque = 80;  //拧盖力度
            _capperTimeout = 40;  //拧盖超时时间 S 
            _posData = _dataAccess.GetCapperPosData(2);
        }

        #endregion



        public override Task<bool> CapperOffAsync(Sample sample, CancellationTokenSource cts)
        {
            return base.CapperOffAsync(sample, cts);
        }


        public override Task<bool> CapperOnAsync(Sample sample, CancellationTokenSource cts)
        {
            return base.CapperOnAsync(sample, cts); 
        }




        public bool GetSampleFromCapperTwoToTransfer(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            try
            {
                _logger?.Info($"从试管架取{sample.Id}样品移液管");

                //拧盖移动到上下料位
                var result = MovePutGetPos(cts).GetAwaiter().GetResult();
                if (!result)
                {
                    throw new Exception("拧盖移动到上下料位 出错");
                }

                //取空管到拧盖2
                result = _carrier.GetSampleFromMaterialToCapperTwo(sample, cts);
                if (!result)
                {
                    throw new Exception($"从试管架取{sample.Id}样品移液管 失败！ TubeStatus-{sample.TubeStatus}");
                }

                //拆盖
                if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsUnCapped))
                {
                    result = CapperOff(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception($"{sample.Id}样品移液管拆盖 失败！ TubeStatus-{sample.TubeStatus}");
                    }
                    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsUnCapped);
                }

                //移动到上下料位
                result =  MovePutGetPos(cts).GetAwaiter().GetResult();
                if (!result)
                {
                    throw new Exception("拧盖移动到上下料位 出错");
                }

                //搬运到移栽
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsUnCapped) && SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperTwo))
                {
                    result = _carrier.GetSampleFromCapperTwoToTransfer(sample, func,cts);
                    if (!result)
                    {
                        throw new Exception($"{sample.Id}样品移液管搬运到移栽 失败！ TubeStatus-{sample.TubeStatus}");
                    }
                }

                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInTransfer))
                {
                    return true;
                }

                throw new Exception($"从试管架取{sample.Id}样品移液管到移栽失败,SampleStatus-{sample.Status}");
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从移栽取出{sample.Id}样品（50ml）空管 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        public bool GetSampleFromColdToTransfer(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            //增加处理逻辑  冰浴缓存 
            //查询冰浴列表是否有料

            //

            return _carrier.GetSampleFromColdToTransfer(sample,1,func, cts);
        }

        public bool GetSampleFromTransferToCapperTwo(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            try
            {
                _logger?.Info($"从移栽取回{sample.Id}样品移液管");

                //拧盖移动到上下料位
                var result = MovePutGetPos(cts).GetAwaiter().GetResult();
                if (!result)
                {
                    throw new Exception("拧盖移动到上下料位 出错");
                }

                //取空管到拧盖2
                result = _carrier.GetSampleFromTransferToCapperTwo(sample,func, cts);
                if (!result)
                {
                    throw new Exception($"从移栽取{sample.Id}样品移液管到拧盖2 失败！ TubeStatus-{sample.TubeStatus}");
                }

                //装盖
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsUnCapped) && SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperTwo))
                {
                    result = CapperOn(_capperTorque, 40, cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception($"{sample.Id}样品移液管装盖 失败！ TubeStatus-{sample.TubeStatus}");
                    }
                    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsUnCapped);
                }

                //移动到上下料位
                result = MovePutGetPos(cts).GetAwaiter().GetResult();
                if (!result)
                {
                    throw new Exception("拧盖移动到上下料位 出错");
                }

                //搬运到移栽
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsUnCapped) && SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperTwo))
                {
                    result = _carrier.GetSampleFromCapperTwoToMaterial(sample, cts);
                    if (!result)
                    {
                        throw new Exception($"{sample.Id}样品移液管搬运到试管架 失败！ TubeStatus-{sample.TubeStatus}");
                    }
                }

                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInShelf))
                {
                    return true;
                }

                throw new Exception($"从移栽取{sample.Id}样品移液管到试管架失败,SampleStatus-{sample.Status}");
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从移栽取{sample.Id}样品移液管到试管架 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }

        }

        public bool GetSampleFromTransferToMaterial(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            return _carrier.GetSampleFromTransferToMaterial(sample, func, cts);
        }

  







    }
}
