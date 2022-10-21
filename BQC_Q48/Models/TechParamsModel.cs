using System;
using System.ComponentModel;
using System.Reflection;
using BQJX.Common;
using PropertyChanged;

namespace BQJX.Models
{
    [AddINotifyPropertyChangedInterface]
    public class TechParamsModel : IDataErrorInfo
    {
        /// <summary>
        /// 工艺参数名称
        /// </summary> 
        public string Name { get; set; }

        /// <summary>
        /// 加水量
        /// </summary>
        public double AddWater { get; set; }


        /// <summary>
        /// 溶剂A添加量
        /// </summary>
        [ValueLimit(20, 0.1)]
        public double Solvent_A { get; set; }    

        /// <summary>
        /// 溶剂B添加量
        /// </summary>
        public double Solvent_B { get; set; }

        /// <summary>
        /// 溶剂C添加量
        /// </summary>
        public double Solvent_C { get; set; }

        /// <summary>
        /// 加水提取时加均质子量
        /// </summary>
        public double AddHomo1 { get; set; }

        /// <summary>
        /// 涡旋时间
        /// </summary>
        public int VortexTime { get; set; }

        /// <summary>
        /// 均质子添加量
        /// </summary>
        public int Junzhizi { get; set; }

        /// <summary>
        /// 超声时间
        /// </summary>
        public int UltrasoundTime { get; set; }

        /// <summary>
        /// 均质时间
        /// </summary>
        public int HomoTime { get; set; }

        /// <summary>
        /// 均质速度
        /// </summary>
        public int HomoVelocity { get; set; }
         
        /// <summary>
        /// 均质头清洗次数
        /// </summary>
        public int WashTimes { get; set; }

        /// <summary>
        /// 均质头清洗速度
        /// </summary>
        public int WashVelocity { get; set; }

        /// <summary>
        /// 均质头清洗时间
        /// </summary>
        public int WashTime { get; set; }

        /// <summary>
        /// 离心速度
        /// </summary>
        public int CentrifugalVelocity { get; set; }

        /// <summary>
        /// 离心时间
        /// </summary>
        public int CentrifugalTime { get; set; }

        /// <summary>
        /// 上清液提取量
        /// </summary>
        public double ExtractVolume { get; set; }

        /// <summary>
        /// 工艺创建时间
        /// </summary>
        public DateTime Createtime { get; set; }

        /// <summary>
        /// 工艺更新时间
        /// </summary>
        public DateTime Updatetime { get; set; }

        /// <summary>
        /// 处理工艺（处理方法）
        /// </summary>
        public int Tech { get; set; }

        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                PropertyInfo pi = this.GetType().GetProperty(columnName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (pi.IsDefined(typeof(ValueLimitAttribute), false))
                {

                    var att = pi.GetCustomAttribute<ValueLimitAttribute>();


                    if ((double)(pi.GetValue(this)) > att.MaxValue)
                    {
                        return "大于参数最大值";
                    }
                    if ((double)(pi.GetValue(this)) < att.MinValue)
                    {
                        return "小于参数最大值";
                    }
                }
                return "";
            }
        }

        public TechParamsModel()
        {
            Name = "";
            Createtime = DateTime.Now;
        }

    }




}
