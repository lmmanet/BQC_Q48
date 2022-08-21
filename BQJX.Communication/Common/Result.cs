using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Communication
{
    public class Result<T>
    {
        /// <summary>
        /// 操作成功标志
        /// </summary>
        public bool IsSuccess { get; set; }
        /// <summary>
        /// 状态码
        /// </summary>
        public int Code { get; set; }
        /// <summary>
        /// 对应的消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        public T Data { get; set; }




        public Result():this(default){}

        public Result(T data) : this(true, 0, "OK",data) { }

        public Result(bool success,int code, string msg, T data)
        {
            this.IsSuccess = success;
            this.Code = code; 
            Message = msg;
            Data = data;
        }


    }

    public class Result : Result<bool> { }
}
