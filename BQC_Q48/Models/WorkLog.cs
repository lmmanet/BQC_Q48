using System;
using PropertyChanged;

namespace BQJX.Models
{
    [AddINotifyPropertyChangedInterface]
    public class WorkLog
    {
        public string Content { get; set; }

        public DateTime Date { get; set; }

        public int Flag { get; set; } = 0;

    }
}
