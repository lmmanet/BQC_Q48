using BQJX.Models;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using NLog.Common;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.Logger
{
    public class LoggerHelper : BQJX.Core.Interface.ILogger
    {
        private readonly NLog.Logger _logger = LogManager.GetCurrentClassLogger();
        

        private static LoggerHelper _obj;

        public static LoggerHelper Logger
        {
            get => _obj ?? (new LoggerHelper());
            set=> _obj = value;
        }

        //=========================================================================================//

        public void Debug(object msg)
        {
            
            _logger.Debug(msg);
        }

        public void Info(object msg)
        {
            _logger.Info(msg);
        }

        public void Warn(object msg)
        {
            _logger.Warn(msg);
        }

        public void Trace(object msg)
        {
            _logger.Trace(msg);
        }

        public void Error(object msg)
        {
            _logger.Error(msg);
            AlarmMessage alarm = new AlarmMessage()
            {
                Message = msg.ToString(),
                DateTime = DateTime.Now,
                State = 0
            };
            Messenger.Default.Send<AlarmMessage>(alarm, "AlarmNotification");
        }

        public void Fatal(object msg)
        {
            _logger.Fatal(msg);
        }


        public void Debug(string msg, Exception err)
        {
            _logger.Debug(err, msg);
        }
        public void Info(string msg, Exception err)
        {
            _logger.Info(err, msg);
        }
        public void Warn(string msg, Exception err)
        {
            _logger.Warn(err, msg);
        }
        public void Trace(string msg, Exception err)
        {
            _logger.Trace(err, msg);
        }
        public void Error(string msg, Exception err)
        {
            _logger.Error(err, msg);
        }
        public void Fatal(string msg, Exception err)
        {
            _logger.Fatal(err, msg);
        }
    }
}
