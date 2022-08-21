using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Communication.Moons
{
    public class Result<T>
    {
        /// <summary>
        /// 操作成功标志
        /// </summary>
        public bool IsSuccess { get; set; }
        /// <summary>
        /// 错误码
        /// </summary>
        public int Code { get; set; }
        /// <summary>
        /// 对应的消息
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// 数据列表
        /// </summary>
        public T Data { get; set; }
        public Result(T t):this(true,0,"OK",t)
        {

        }
        public Result(bool success, int code, string msg,T data)
        {
            this.IsSuccess = success;
            this.Code = code;
            Message = msg;
            Data = data;
        }
    }
    public class Result : Result<int>
    {
        public Result() : this(true, 0, "OK",0)
        {

        }
        public Result(bool success, int code, string msg,int i):base(success,code,msg,i)
        {
           
        }
    }
    public class BoolResult :Result<bool>
    {
        public BoolResult():this(true, 0, "OK", false)
        {

        }
        public BoolResult(bool success, int code, string msg, bool b) :base(success, code, msg, b)
        {

        }
    }

}
