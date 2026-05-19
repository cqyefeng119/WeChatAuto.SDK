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
using WeChatAuto.Services;
using System.Text.RegularExpressions;

namespace WeChatAuto.Components
{
    /// <summary>
    /// 消息监听器
    /// </summary>
    public class NewFriendMonitor : IDisposable
    {
        private int _disposed = 0;
        private readonly WeChatClient _Client;
        private readonly Random random = new Random((int)DateTime.Now.Ticks);
        private readonly IServiceProvider serviceProvider;
        private readonly UIThreadInvoker _MainThreadInvoker;
        private readonly SemaphoreSlim noticeEvent;
        private readonly AutoLogger<MessageMonitor> _Logger;
        #region 好友监听字段
        private bool newFriendMonitorStarted = true;
        private int totalNewFriends = 0;   //新好友数量
        private readonly ConcurrentDictionary<string, bool> _MessageList = new ConcurrentDictionary<string, bool>();
        private CancellationTokenSource newFriendCts = new CancellationTokenSource();
        private Task newFriendFetchTask;
        private Action<string> UIInvoker;
        private volatile int _IsContinue = 1;
        #endregion


        /// <summary>
        /// <para>构造器，不应该自行调用</para>
        /// </summary>
        /// <param name="client"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="resetEvent"></param>
        /// <param name="_uiMainThreadInvoker"></param>
        internal NewFriendMonitor(WeChatClient client, IServiceProvider serviceProvider, UIThreadInvoker _uiMainThreadInvoker, SemaphoreSlim resetEvent)
        {
            this._Client = client;
            this.serviceProvider = serviceProvider;
            this._MainThreadInvoker = _uiMainThreadInvoker;
            this.noticeEvent = resetEvent;

            _Logger = serviceProvider.GetRequiredService<AutoLogger<MessageMonitor>>();
        }

        #region 好友监听
        
        public async Task AddNewFriendListener(OneOf<string, List<string>> nickNames, Action<MessageContext> callBack, bool IsOpenMonitor = false, CancellationTokenSource tokenSource = default, Action<string> UIInvoker = null)
        => await AddNewFriendListenerCore(nickNames, callBack, IsOpenMonitor, tokenSource, UIInvoker, new DateTimeRange());

        /// <summary>
        /// 暂停消息监听
        /// </summary>
        /// <returns></returns>
        public async Task PauseNewFriendListener()
        {
            while (Interlocked.Exchange(ref this._IsContinue, 0) == 1)
            {
                await Task.Delay(0);
            }
        }
        /// <summary>
        /// 恢复消息监听
        /// </summary>
        /// <returns></returns>
        public async Task ResumeNewFriendListener()
        {
            while(Interlocked.Exchange(ref this._IsContinue,1) == 0)
            {
                await Task.Delay(0);
            }
        }


        private async Task AddNewFriendListenerCore(OneOf<string, List<string>> nickNames, Action<MessageContext> callBack, bool IsOpenMonitor, CancellationTokenSource tokenSource, Action<string> UIInvoker, DateTimeRange range)
        {
            this.UIInvoker = UIInvoker;
            CancellationToken token;
            if (tokenSource != default)
            {
                token = CancellationTokenSource.CreateLinkedTokenSource(newFriendCts.Token, tokenSource.Token).Token;
            }
            else
            {
                token = newFriendCts.Token;
            }
            (nickNames.IsT0 ? new List<string>() { nickNames.AsT0 } : nickNames.AsT1).ForEach(u => _MessageList.TryAdd(u, false));  //赋初始值.
            try
            {
                var startTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                newFriendFetchTask = Task.Run(async () =>
                {
                    startTcs.TrySetResult(true);
                    using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(WeAutomation.Config.MonitorMessageInterval));
                    while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
                    {
                        try
                        {
                            if (_IsContinue != 1)  //如果暂停了
                                continue;
                            await noticeEvent.WaitAsync(token);
                            try
                            {
                                //await WeChatInvoker.Call(AddMessageListenerAction, callBack, token, IsOpenMonitor);
                            }
                            finally
                            {
                                noticeEvent.Release();
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            if (!startTcs.Task.IsCompleted)
                                startTcs.TrySetCanceled();
                        }
                        catch (Exception ex)
                        {
                            if (!startTcs.Task.IsCompleted)
                                startTcs.SetException(ex);
                            _Logger.Error($"监听消息发生错误：{ex.ToString()}");
                        }
                    }
                }, token);
                await startTcs.Task;
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

        /// <summary>
        /// 允许调用者做一些UI操作.
        /// </summary>
        /// <param name="token"></param>
        private void _SwtichUI(CancellationToken token)
        {
            if (this.UIInvoker != null)
            {
                token.ThrowIfCancellationRequested();
                WeAutomation.currentContext.Send((state) => this.UIInvoker.Invoke(state.ToString()), this._Client.NickName);
            }
        }

        private int _GetTotalMessage(CancellationToken token)
        {
            try
            {
                var root = this._Client.Navigation.rootElement;
                if (root == null)
                    return 0;
                var item = root.FindFirstChild(cf => cf.ByName("微信").And(cf.ByControlType(ControlType.Button)));
                if (item == null)
                    return 0;
                item.WaitUntilClickable();
                var title = item.Properties.FullDescription;
                if (string.IsNullOrEmpty(title) || title == "微信")
                    return 0;
                var pattern = @"^([\d]+)条新消息$";
                var match = Regex.Match(title, pattern);
                if (match.Success)
                {
                    return int.TryParse(match.Groups[1].Value, out var result) ? result : 0;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception ex)
            {
                _Logger.Error($"{nameof(MessageMonitor)} - {nameof(_GetTotalMessage)}发生错误:{ex.ToString()}");
                return 0;
            }
        }

        #endregion
        ~NewFriendMonitor()
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
                newFriendCts?.Cancel();
                if (newFriendFetchTask != null)
                {
                    if (!newFriendFetchTask.IsCompleted)
                    {
                        try
                        {
                            newFriendFetchTask.Wait(TimeSpan.FromSeconds(3));
                        }
                        catch (AggregateException) { }
                        catch (Exception) { }
                    }
                }

                newFriendCts?.Dispose();
                newFriendFetchTask?.Dispose();
            }
        }
    }
}