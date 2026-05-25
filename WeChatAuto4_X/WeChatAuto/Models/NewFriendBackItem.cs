namespace WeChatAuto.Models
{
    /// <summary>
    /// 自动加好友返回信息
    /// </summary>
    public class NewFriendBackItem
    {
        /// <summary>
        /// 新增加好友昵称
        /// </summary>
        public string Who { get; set; }
        /// <summary>
        /// 新增加好友从哪个关键词过来
        /// </summary>
        public string FromKeyword {get;set;}
    }
}