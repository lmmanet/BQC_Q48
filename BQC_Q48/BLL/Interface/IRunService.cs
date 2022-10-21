using System;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface IRunService
    {
        /// <summary>
        /// 报警事件
        /// </summary>
        event Action<string> AlmOccuCallBack;

        /// <summary>
        /// 加盐暂停事件
        /// </summary>
        event Action<string> AddSaltPauseEventHandler;

        /// <summary>
        /// 加盐继续事件
        /// </summary>
        event Action<string> AddSaltContinueEventHandler;

        /// <summary>
        /// 搬运1暂停事件
        /// </summary>
        event Action<string> CarrierOnePauseEventHandler;

        /// <summary>
        /// 搬运1继续事件
        /// </summary>
        event Action<string> CarrierOneContinueEventHandler;

        /// <summary>
        /// 搬运2暂停事件
        /// </summary>
        event Action<string> CarrierTwoPauseEventHandler;

        /// <summary>
        /// 搬运2继续事件
        /// </summary>
        event Action<string> CarrierTwoContinueEventHandler;



        double AD_Value1 { get; set; }
        double AD_Value5 { get; set; }
        double AD_Value6 { get; set; }
        bool IsTemperatureCtl { get; set; }
        double SetTemperature1 { get; set; }
        double SetTemperature2 { get; set; }
        double SetTemperature3 { get; set; }


        void ResetAlm();
        Task Run();
        void StopPro();
    }
}