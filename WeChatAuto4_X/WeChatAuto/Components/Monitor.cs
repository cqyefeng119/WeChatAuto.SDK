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
        private ManualResetEvent noticeEvent;
        private AutoLogger<Monitor> _Logger;


        /// <summary>
        /// <para>构造器，不应该自行调用</para>
        /// </summary>
        /// <param name="client"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="resetEvent"></param>
        /// <param name="_uiMainThreadInvoker"></param>
        internal Monitor(WeChatClient client, IServiceProvider serviceProvider, UIThreadInvoker _uiMainThreadInvoker, ManualResetEvent resetEvent)
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
        /// </summary>
        /// <param name="nickNames">好友昵称,可以是一个，也可以是多个好友/群聊 </param>
        /// <param name="callBack">回调函数,由用户提供,参数：消息上下文<see cref="MessageContext"/></param>
        public async Task AddMessageListener(OneOf<string,List<string>> nickNames, Action<MessageContext> callBack)
        {
            await Task.CompletedTask;
        }
        /// <summary>
        /// 移除被监听中的好友/群聊
        /// </summary>
        /// <param name="nickName"></param>
        /// <returns></returns>
        public async Task RemoveListeningFriend(string nickName)
        {
            
        }
        /// <summary>
        /// 暂停消息监听
        /// </summary>
        public async Task PauseMessageListener()
        {
            
        }
        /// <summary>
        /// 恢复消息监听
        /// </summary>
        /// <returns></returns>
        public async Task ResumeMessageListener()
        {
            
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