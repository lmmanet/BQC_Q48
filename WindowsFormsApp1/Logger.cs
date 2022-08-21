using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public class Logger : BQJX.Core.Interface.ILogger
    {
        private ILog _logger;

        public Logger()
        {
            XmlConfigurator.Configure();
            _logger = LogManager.GetLogger(typeof(Logger));
        }
        public void Debug(object message)
        {
            _logger.Debug(message);
            WriteLog(message.ToString());
        }

        public void Error(object message)
        {
            _logger.Error(message);
            WriteLog(message.ToString());
        }

        public void Fatal(object message)
        {
            _logger.Fatal(message);
            WriteLog(message.ToString());
        }

        public void Info(object message)
        {
            _logger.Info(message);
            WriteLog(message.ToString());
        }

        public void Warn(object message)
        {
            _logger.Warn(message);
            WriteLog(message.ToString());
        }


        private void WriteLog(string mes)
        {
            Form1.WriteLog(mes);
        }


    }
}
