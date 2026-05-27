using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace WeChatAuto.Models
{
    /// <summary>
    /// 消息引用
    /// </summary>
    public class ChatRefer
    {
        /// <summary>
        /// 日期,如果不设置则为当天
        /// </summary>
        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        public ChatSimpleMessage Message {get;set;}
    }
}