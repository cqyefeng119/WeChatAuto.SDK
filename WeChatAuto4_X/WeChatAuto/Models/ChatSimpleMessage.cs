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
        /// 消息日期，有些api不携带此信息，请注意选择api选择
        /// </summary>
        public string SendDateTime { get; set; }
        /// <summary>
        /// 唯一字符串，有些api不携带此信息，请注意选择api选择
        /// </summary>
        public string UniqueString { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}