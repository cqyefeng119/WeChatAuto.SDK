using FlaUI.Core;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.Core.EventHandlers;
using FlaUI.Core.AutomationElements;
using WeAutoCommon.Utils;
using WeChatAuto.Utils;
using WeAutoCommon.Interface;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using WeAutoCommon.Configs;
using WeAutoCommon.Enums;
using WeChatAuto.Services;
using Microsoft.Extensions.DependencyInjection;
using WeChatAuto.Models;
using WeChatAuto.Extentions;
using System.Text.RegularExpressions;

namespace WeChatAuto.Components
{
    [Obsolete("废弃,层次太深了")]
    public class ChatBody : IDisposable
    {
        private readonly AutoLogger<ChatBody> _logger;
        private readonly IServiceProvider _serviceProvider;
        private UIThreadInvoker _uiMainThreadInvoker;
        private volatile bool _disposed = false;


        /// <summary>
        /// 添加消息监听
        /// 注意：消息回调函数会在新线程中执行，请注意线程安全，如果在回调函数中操作UI，请切换到UI线程.
        /// </summary>
        /// <param name="callBack">回调函数,参数：消息上下文<see cref="MessageContext"/></param>
        /// <param name="firstMessageAction">适用于当开始消息监听时,发送一些信息（如：发送文字、表情、文件等）给好友的场景,参数：发送者<see cref="Sender"/></param>
        // public void AddListener(Action<MessageContext> callBack, Action<Sender> firstMessageAction = null)
        // {
        //     //StartMessagePolling(callBack); //启动消息轮询检测
        //     //firstMessageAction?.Invoke(Sender); //执行第一次消息发送
        // }


        /// <summary>
        /// 停止消息监听
        /// </summary>
        public void StopListener() => this.Dispose();

        /// <summary>
        /// 获取聊天内容区可见气泡列表
        /// </summary>
        /// <returns>聊天内容区可见气泡列表对象<see cref="Components.MessageBubbleList"/></returns>
        // public MessageBubbleList GetBubbleListObject()
        // {
        //     // var xPath = $"/Pane/Pane/List[@Name='{WeChatConstant.WECHAT_CHAT_BOX_MESSAGE}']";
        //     // _ChatBodyRoot = _GetChatBodyRoot_();
        //     // var bubbleListRoot = _uiMainThreadInvoker.Run(automation => _ChatBodyRoot.FindFirstByXPath(xPath)).GetAwaiter().GetResult();
        //     // MessageBubbleList bubbleListObject = new MessageBubbleList(_Window, bubbleListRoot, _WxWindow, _FullTitle, _uiMainThreadInvoker, this, _serviceProvider);
        //     // return bubbleListObject;
        //     return null;
        // }
        /// <summary>
        /// 获取聊天内容区所有气泡列表,如果消息没有显示全，则会滚动消息至最顶部，然后获取所有气泡标题
        /// 速度会比较快
        /// </summary>
        /// <param name="pageCount">获取的气泡数量，默认是10页,可以指定获取的页数，如果指定为-1，则获取所有气泡</param>
        /// <returns>聊天内容区所有气泡列表,仅返回气泡标题</returns>
        public List<ChatSimpleMessage> GetAllChatHistory(int pageCount = 10)
        {
            return null;
        }
        /// <summary>
        /// 获取聊天内容区发送者
        /// </summary>
        /// <returns>聊天内容区发送者<see cref="Sender"/></returns>
        // public Sender GetSender()
        // {
        //     // var xPath = "/Pane[2]";
        //     // _ChatBodyRoot = _GetChatBodyRoot_();
        //     // var senderRoot = _uiMainThreadInvoker.Run(automation =>
        //     // {
        //     //     var result = Retry.WhileNull(() => _ChatBodyRoot.FindFirstByXPath(xPath), timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200));
        //     //     return result.Success ? result.Result : null;
        //     // }).GetAwaiter().GetResult();
        //     // DrawHightlightHelper.DrawHightlight(senderRoot, _uiMainThreadInvoker);
        //     // var sender = new Sender(_Window, senderRoot, _WxWindow, _FullTitle, _uiMainThreadInvoker, _serviceProvider);
        //     // return sender;
        //     return null;
        // }

        public void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            if (disposing)
            {

            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~ChatBody()
        {
            Dispose(false);
        }

    }
}