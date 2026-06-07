using System;
using System.Drawing;
using MessagePack;
using WeAutoCommon.Enums;

namespace WeChatAuto.Models
{
    [MessagePackObject]
    public class SimpleMessageBubble
    {
        /// <summary>
        /// 微信名
        /// 如果是自己发送，则值为"我"
        /// 如果是系统发送，则值为"系统"
        /// </summary>
        [Key(1)]
        public string Who { get; set; }
        /// <summary>
        /// 消息
        /// </summary>
        [Key(2)]
        public string Message { get; set; }
        /// <summary>
        /// 发送日期，大概日期，并不准确,因为微信本身也日期也不准确
        /// </summary>
        [Key(3)]
        public DateTime SendDate { get; set; }
        /// <summary>
        /// 如果消息中有图片，此处为图片的Bitmap对象，否则为null
        /// 注意：如果消息中有图片，且MessageMonitorOptions.FetchImage=false，则此处为null
        /// 注意：如果消息中有图片，且MessageMonitorOptions.FetchImage=true，则此处为Bitmap对象
        /// 注意：如果消息中有图片，且MessageMonitorOptions.FetchImage=true，但获取图片失败，则此处为null
        /// </summary>
        [IgnoreMember]
        public Bitmap Image { get; set; }

        /// <summary>
        /// 消息类型
        /// </summary>
        [Key(5)]
        public MessageType MessageType { get; set; } = MessageType.None;

        /// <summary>
        /// 图片路径
        /// 如果消息中有图片，此处为图片的路径
        /// 注意：如果消息中有图片，且MessageMonitorOptions.FetchImage=false，则此处为null
        /// 注意：如果消息中有图片，且MessageMonitorOptions.FetchImage=true，则此处非空
        /// 注意：如果消息中有图片，且MessageMonitorOptions.FetchImage=true，但获取图片失败，则此处为null
        /// </summary>
        [Key(6)]
        public string ImageFile { get; set; }

        public override string ToString()
        {
            return $"who={this.Who} Message={this.Message} SendDate={this.SendDate.ToString("yyyy-MM-dd HH:mm")} Image={(this.Image != null ? "有图片" : "无")} MessageType={this.MessageType.ToString()} ImageFile={this.ImageFile}";
        }
    }
}