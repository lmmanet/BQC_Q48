using System;
using PropertyChanged;

namespace BQJX.Models
{
    [AddINotifyPropertyChangedInterface]
    public class AlarmMessage
    {
        public int Id { get; set; }

        public DateTime DateTime { get; set; }

        public string Message { get; set; }

        public int State { get; set; }

    }

}
