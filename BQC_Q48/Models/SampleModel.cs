using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged;

namespace BQJX.Models
{
    [AddINotifyPropertyChangedInterface]
    public class SampleModel
    {
        /// <summary>
        /// 样品位置编号1-48
        /// </summary>
        public ushort Id { get; set; }

        /// <summary>
        /// 样品编号
        /// </summary>
        public string SnNum { get; set; }

        /// <summary>
        /// 样品名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 样品创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

    }
}
