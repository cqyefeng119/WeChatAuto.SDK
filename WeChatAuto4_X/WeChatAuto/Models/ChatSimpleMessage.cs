namespace WeChatAuto.Models
{
    public class ChatSimpleMessage
    {
        /// <summary>
        /// 好友/群聊昵称
        /// </summary>
        public string Who { get; set; }
        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; set; }

        public override string ToString()
        {
            return $"{Who}: {Message}";
        }
    }
}