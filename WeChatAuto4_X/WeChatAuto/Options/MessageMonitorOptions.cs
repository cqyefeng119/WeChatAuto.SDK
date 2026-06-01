using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OneOf;
using WeChatAuto.Components;
using WeChatAuto.Models;

namespace WeChatAuto.Options
{
    /// <summary>
    /// 消息监听器选项.
    /// </summary>
    public class MessageMonitorOptions
    {
        /// <summary>
        /// 如果此好友在缓存中不存在，是否获取此好友的用户信息(包括wxid),并更新缓存，对于基于wxid的企业级开发很有用
        /// </summary>
        public bool FetchFriendInfo {get;set;} = false;

        /// <summary>
        /// 如果聊天记录中有图片，是否获取图片
        /// </summary>
        public bool FetchImage {get;set;} = false;
        /// <summary>
        /// 如果聊天记录中有红包、转账，是否点击
        /// </summary>
        public bool ClickRedEnvelope {get;set;} = false;
    }
}