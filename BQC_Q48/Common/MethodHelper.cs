using BQJX.Common;
using GalaSoft.MvvmLight.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.Common
{
    public static class MethodHelper
    {

        public static object ExcuteMethod(Sample sample, CancellationTokenSource cts)
        {
            var strs = sample.ActionCallBack.Split('@');
            string interfaceName = strs[0];
            string methodName = strs[1];
            //接口名字
            Type type = Type.GetType(interfaceName);
            var instance = SimpleIoc.Default.GetInstance(type);

            MethodInfo mi = type.GetMethod(methodName);

            if (mi != null)
            {
               return mi.Invoke(instance, new object[] { sample, cts });
            }
            return null;
        }













    }
}
