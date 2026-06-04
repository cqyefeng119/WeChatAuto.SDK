using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using OneOf;
using WeAutoCommon.Utils;
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
        public bool FetchFriendInfo { get; set; } = false;

        /// <summary>
        /// 如果聊天记录中有图片，是否获取图片
        /// </summary>
        public bool FetchImage { get; set; } = false;
        /// <summary>
        /// 如果聊天记录中有红包、转账，是否点击
        /// </summary>
        public bool ClickRedEnvelope { get; set; } = false;

        /// <summary>
        /// 手动处理消息，SDK只默认处理了文字消息、图片消息、红包/转账消息，其他的消息可以自行处理，如：自行处理打开链接抓取链接内容等.
        /// </summary>
        public Action<AutomationElement> CustomProcessMessageAction = null;

        /// <summary>
        /// 是否预防风控,如果待监控的群不多，建议设置为False,如果监测的群/好友很多，并且聊天很频繁，建议将设置为True.
        /// 因为人不可能一天24小时进行操作的,否则极易被微信风控退出。
        /// </summary>
        public bool IsRiskPrevention { get; set; } = false;

        /// <summary>
        /// 预防风控方法
        /// 如果上面IsRiskPrevention设置为True,则预防风控方法生效，预设预防风控行为是等候一段时间，你也可以覆盖此方法，加入更多不可预测行为.
        /// 如：你可以加入随机与某人聊一句，或者运行其他的方法，甚至晚上一段时间停止等
        /// 触发时间：运4行6-10分钟之内的某个随机时间触发
        /// 预防风控方法运行时，消息监听会暂停，预防风控方法运行结束，消息监听继续.
        /// </summary>
        public Func<WeChatClient,Task> RiskPreventionAction { get; set; } = async client =>
        {
            await RandomWait.WaitAsync(60 * 1_000, 3 * 60 * 1_000);  //随机等候1..3分钟.
        };
    }
}