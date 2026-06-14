using System;
using System.Globalization;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace WeChatAuto.Models
{
    public class ChatSimpleMessage
    {
        /// <summary>
        /// 微信名称
        /// </summary>
        public string Who { get; set; }
        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// 消息日期
        /// </summary>
        public string SendDateTime { get; set; }
        /// <summary>
        /// 消息日期,日期时间格式
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// 唯一字符串
        /// </summary>
        public string UniqueString { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}