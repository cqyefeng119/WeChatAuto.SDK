using System;

namespace WeChatAuto.Models
{
    public class SimpleMessageBubble
    {
        /// <summary>
        /// 微信名
        /// 如果是自己发送，则值为"我"
        /// </summary>
        public string Who { get; set; }
        /// <summary>
        /// 消息
        /// </summary>
        public string message { get; set; }
        /// <summary>
        /// 发送日期，大概日期，并不准确,因为微信本身也日期也不准确
        /// </summary>
        public DateTime SendDate { get; set; }
    }
}