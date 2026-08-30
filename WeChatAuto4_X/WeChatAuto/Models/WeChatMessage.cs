using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using SqlSugar;

namespace WeChatAuto.Models
{
    /// <summary>
    /// 微信消息表类    
    /// </summary>
    [SugarTable("wechat_message")]
    public class WeChatMessage
    {
        /// <summary>
        /// 数据库主键
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        /// <summary>
        /// 发送日期，形于: 2026-08-31 这种格式字符串.
        /// </summary>
        [SugarColumn(Length = 50,IsNullable = false)]
        public string SendDate {get;set;} = DateTime.Now.ToString("yyyy-MM-dd");

        /// <summary>
        /// 微信账号
        /// 表示这条消息属于哪个微信账号,应用于多微信号场景
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = false)]
        public string FromWechat { get; set; } = string.Empty;

        /// <summary>
        /// 发送者昵称
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = true)]
        public string Sender { get; set; } = string.Empty;

        /// <summary>
        /// 消息类型
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false)]
        public string MessageType { get; set; } = string.Empty;

        /// <summary>
        /// 消息内容
        /// </summary>
        [SugarColumn(ColumnDataType = "TEXT", IsNullable = true)]
        public string Content { get; set; } = "";

        /// <summary>
        /// 微信消息发生时间,仅精确到分钟 ,取自微信，可能为空！
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true)]
        public string MessageTime { get; set; }

        /// <summary>
        /// 消息是否由自己发送
        /// </summary>
        public bool IsSelf { get; set; } = false;

        /// <summary>
        /// 消息是否被Bot处理
        /// </summary>
        public bool IsBotProcessed { get; set; } = false;

        /// <summary>
        /// 消息被 SDK 写入时间.
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}