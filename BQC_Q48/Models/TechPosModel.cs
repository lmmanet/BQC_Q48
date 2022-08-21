using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged;

namespace Q_Platform.Models
{
    [AddINotifyPropertyChangedInterface]
    public class TechPosModel
    {
        [DoNotNotify]
        public string PosName { get; set; }

        public double[] PosData { get; set; } = new double[3];


    }
}
