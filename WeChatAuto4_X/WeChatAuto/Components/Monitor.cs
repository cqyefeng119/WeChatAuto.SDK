using System;
using System.Collections.Generic;
using System.Linq;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using WeAutoCommon.Enums;
using WeAutoCommon.Interface;
using WeAutoCommon.Utils;
using WeChatAuto.Extentions;
using WeChatAuto.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using WeAutoCommon.Models;
using System.Threading.Tasks;
using System.Drawing;
using FlaUI.Core;
using FlaUI.Core.Identifiers;
using FlaUI.Core.Conditions;
using System.IO;
using FlaUI.UIA3;
using WeAutoCommon.Extentions;
using System.Threading;
using System.Diagnostics;
using WeChatAuto.Models;
using OneOf;
using System.Collections.Concurrent;

namespace WeChatAuto.Components
{
    /// <summary>
    /// 监听器
    /// </summary>
    public class Monitor : IDisposable
    {
        private int _disposed = 0;
        private WeChatClient _Client;
        private IServiceProvider serviceProvider;
        private UIThreadInvoker _MainThreadInvoker;
        private AutoResetEvent noticeEvent;
        private AutoLogger<Monitor> _Logger;
        private int messageListnerFlag = 0;
        private ConcurrentBag<string> _MessageList = new ConcurrentBag<string>();
        private CancellationTokenSource messageCts = new CancellationTokenSource();


        /// <summary>
        /// <para>构造器，不应该自行调用</para>
        /// </summary>
        /// <param name="client"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="resetEvent"></param>
        /// <param name="_uiMainThreadInvoker"></param>
        internal Monitor(WeChatClient client, IServiceProvider serviceProvider, UIThreadInvoker _uiMainThreadInvoker, AutoResetEvent resetEvent)
        {
            this._Client = client;
            this.serviceProvider = serviceProvider;
            this._MainThreadInvoker = _uiMainThreadInvoker;
            this.noticeEvent = resetEvent;

            _Logger = serviceProvider.GetRequiredService<AutoLogger<Monitor>>();
        }


        private AutomationElement _GetToolBarRoot(Window window)
        {
            var toolBarRetry = Retry.WhileNull(() => window.FindFirstDescendant(cf => cf.ByAutomationId("MainView.main_tabbar").And(cf.ByControlType(ControlType.ToolBar).And(cf.ByName("导航")))), timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200));
            return toolBarRetry.Success ? toolBarRetry.Result : null;
        }

        #region 消息监听
        /// <summary>
        /// 添加消息监听，用户需要提供一个回调函数，当有消息时，会调用回调函数
        /// 参考<see cref="MessageContext"/>
        /// 
        /// 使用规则：
        /// 1. 仅能监听“允许消息通知”的好友/群聊,所以需要监听的好友/群聊不要设置为“消息免打扰”;
        /// 2. 如果要避免太多消息影响，请将不监听的好友/群设置为“消息免打扰”，以提高监听性能;
        /// 3. 此方法可以多次添加，第一次添加时会启动消息监听，第二次（类似的第三次等）添加效果等同<see cref="AddListeningFriend"/>方法
        /// 4. 为了减少会话窗口的滚动，建议不要添加太多消息“置顶”，以提高监听效率.
        /// </summary>
        /// <param name="nickNames">好友昵称,可以是一个，也可以是多个好友/群聊 </param>
        /// <param name="callBack">回调函数,由用户提供,参数：消息上下文<see cref="MessageContext"/></param>
        public async Task AddMessageListener(OneOf<string, List<string>> nickNames, Action<MessageContext> callBack)
        {
            if (Interlocked.CompareExchange(ref messageListnerFlag, 1, 0) == 1)
            {
                List<string> list = nickNames.IsT0 ? new List<string>() { nickNames.AsT0 } : nickNames.AsT1;
                list.ForEach(async item => await AddListeningFriend(item));
                return;
            }
            await WeChatInvoker.Call(AddMessageListenerCore, nickNames, callBack);
        }

        private void AddMessageListenerCore(UIA3Automation automation, OneOf<string, List<string>> nickNames, Action<MessageContext> callBack)
        {
            _MessageList = new ConcurrentBag<string>(nickNames.IsT0 ? new List<string>() { nickNames.AsT0 } : nickNames.AsT1);  //赋初始值.
            while (!messageCts.Token.IsCancellationRequested)
            {
                try
                {
                    noticeEvent.WaitOne();


                    noticeEvent.Set();
                    messageCts.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(3));
                }
                catch (OperationCanceledException)
                {
                    //do nothing.
                }
                catch (Exception ex)
                {
                    _Logger.Error($"监听消息发生错误：{ex.ToString()}");
                }
            }
        }

        /// <summary>
        /// 监听过程中添加好友
        /// </summary>
        /// <param name="who">好友名称</param>
        /// <returns></returns>
        public async Task AddListeningFriend(string who)
        {
            if (messageListnerFlag != 1)
                throw new Exception("错误：请先启动消息监听器");
            if (!_MessageList.Contains(who))
            {
                _MessageList.Add(who);
            }
        }
        /// <summary>
        /// 监听过程中移除被监听中的好友/群聊
        /// </summary>
        /// <param name="who"></param>
        /// <returns></returns>
        public async Task RemoveListeningFriend(string who)
        {
            if (messageListnerFlag != 1)
                throw new Exception("错误：请先启动消息监听器");
            _MessageList = new ConcurrentBag<string>(_MessageList.Where(x => x != who));
            await Task.CompletedTask;
        }

        #endregion

        #region 会话切换监听
        #endregion

        #region 新好友申请监听

        #endregion

        #region 添加朋友圈监听
        #endregion

        #region 释放器
        ~Monitor()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
                return;
            if (disposing)
            {

            }
        }
        #endregion
    }
}