using BQJX.Models;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q_Platform.Logger
{
    public class MyLogger : BQJX.Core.Interface.ILogger
    {

        private readonly NLog.Logger _logger;


        #region Construtors

        public MyLogger(string name)
        {
            _logger = LogManager.GetLogger(name);
        }

        public MyLogger(Type typeClass)
        {
            string name = typeClass.FullName;
            _logger = LogManager.GetLogger(name);
        }

        #endregion



        #region Public Methods 
        public void Debug(object msg)
        {
            _logger.Debug(msg);
            WorkLog log = new WorkLog()
            {
                Content = msg.ToString(),
                Date = DateTime.Now,
                Flag = 0
            };

            Messenger.Default.Send<WorkLog>(log, "logWorkLog");
        }

        public void Info(object msg)
        {
            _logger.Info(msg);
            WorkLog log = new WorkLog()
            {
                Content = msg.ToString(),
                Date = DateTime.Now,
                Flag = 0
            };

            Messenger.Default.Send<WorkLog>(log, "logWorkLog");
        }

        public void Warn(object msg)
        {
            _logger.Warn(msg);
            WorkLog log = new WorkLog()
            {
                Content = msg.ToString(),
                Date = DateTime.Now,
                Flag = 1
            };

            Messenger.Default.Send<WorkLog>(log, "logWorkLog");
        }

        public void Trace(object msg)
        {
            _logger.Trace(msg);
        }

        public void Error(object msg)
        {
            _logger.Error(msg);
            WorkLog log = new WorkLog()
            {
                Content = msg.ToString(),
                Date = DateTime.Now,
                Flag = 2
            };

            Messenger.Default.Send<WorkLog>(log, "logWorkLog");
        }

        public void Fatal(object msg)
        {
            _logger.Fatal(msg);
            WorkLog log = new WorkLog()
            {
                Content = msg.ToString(),
                Date = DateTime.Now,
                Flag = 3
            };

            Messenger.Default.Send<WorkLog>(log, "logWorkLog");
        }

        #endregion

        #region Public Methods

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
        #endregion


    }
}
