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
        /// 乙腈添加量
        /// </summary>
        [ValueLimit(20, 0)]
        public double ACE { get; set; }

        /// <summary>
        /// 醋酸添加量
        /// </summary>
        public double Acid { get; set; }

        /// <summary>
        /// 甲酸添加量
        /// </summary>
        public double Formic { get; set; }

        /// <summary>
        /// 加均质子量
        /// </summary>
        public double Homo { get; set; }

        /// <summary>
        /// 硫酸镁
        /// </summary>
        public double MgSO4 { get; set; }

        /// <summary>
        /// 氯化钠/硫酸钠
        /// </summary>
        public double NaCl { get; set; }

        /// <summary>
        /// 柠檬酸钠
        /// </summary>
        public double Trisodium { get; set; }

        /// <summary>
        /// 柠檬酸氢二钠
        /// </summary>
        public double Monosodium { get; set; }

        /// <summary>
        /// 乙酸钠
        /// </summary>
        public double Sodium { get; set; }

        /// <summary>
        /// 涡旋时间
        /// </summary>
        public int VortexTime { get; set; }

        /// <summary>
        /// 涡旋速度
        /// </summary>
        public int VortexVel { get; set; }

        /// <summary>
        /// 振荡时间
        /// </summary>
        public int VibrationTime { get; set; }

        /// <summary>
        /// 涡旋速度
        /// </summary>
        public int VibrationVel { get; set; }

        /// <summary>
        /// 离心时间
        /// </summary>
        public int CentrifugalTime { get; set; }

        /// <summary>
        /// 离心速度
        /// </summary>
        public int CentrifugalVel { get; set; }

        /// <summary>
        /// 上清液提取量
        /// </summary>
        public double ExtractVolume { get; set; }

        /// <summary>
        /// 浓缩时间
        /// </summary>
        public int ConcentrationTime { get; set; }

        /// <summary>
        /// 浓缩速度
        /// </summary>
        public int ConcentrationVel { get; set; }

        /// <summary>
        /// 工艺创建时间
        /// </summary>
        public DateTime Createtime { get; set; }

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
