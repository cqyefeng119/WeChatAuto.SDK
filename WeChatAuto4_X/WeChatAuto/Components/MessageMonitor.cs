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
    public class MessageMonitor : IDisposable
    {
        private int _disposed = 0;
        private readonly WeChatClient _Client;
        private readonly Random random = new Random((int)DateTime.Now.Ticks);
        private readonly IServiceProvider serviceProvider;
        private readonly UIThreadInvoker _MainThreadInvoker;
        private readonly SemaphoreSlim noticeEvent;
        private readonly AutoLogger<MessageMonitor> _Logger;
        #region 消息监听字段
        private int messageListnerStartedFlag = 0;   //消息监听启用标识
        private bool messageStarted = true;
        private int totalNewMessage = 0;   //新消息数量
        private readonly ConcurrentDictionary<string, bool> _MessageList = new ConcurrentDictionary<string, bool>();
        private CancellationTokenSource messageCts = new CancellationTokenSource();
        private Task messageRunningTask;
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
        internal MessageMonitor(WeChatClient client, IServiceProvider serviceProvider, UIThreadInvoker _uiMainThreadInvoker, SemaphoreSlim resetEvent)
        {
            this._Client = client;
            this.serviceProvider = serviceProvider;
            this._MainThreadInvoker = _uiMainThreadInvoker;
            this.noticeEvent = resetEvent;

            _Logger = serviceProvider.GetRequiredService<AutoLogger<MessageMonitor>>();
        }

        #region 消息监听
        /// <summary>
        /// 添加消息监听，用户需要提供一个回调函数，当有消息时，会调用此回调函数
        /// 参考<see cref="MessageContext"/>
        /// 
        /// <para>使用规则：</para>
        /// <para>1. 仅能监听“允许消息通知”的好友/群聊,所以需要监听的好友/群聊不要设置为“消息免打扰”;</para>
        /// <para>2. 如果要避免太多消息影响，请将不监听的好友/群设置为“消息免打扰”，以提高监听性能;</para>
        /// <para>3. 此方法可以多次添加，第一次添加时会启动消息监听，第二次（类似的第三次等）添加效果等同<see cref="AddListeningFriend"/>方法</para>
        /// <para>4. 为了减少会话窗口的滚动，建议不要添加太多消息“置顶”，以提高监听效率.</para>
        /// <para>执行逻辑:</para>
        /// <para>1. 启动消息监听器时做做一次全量扫描，即：SDK会将会话列表整个循环一编，会自动点击并回调用户设定的方法，以防止遗漏消息</para>
        /// <para>2. 以后的监听过程会增量监听，以提高效率.</para>
        /// </summary>
        /// <param name="nickNames">好友昵称,可以是一个，也可以是多个好友/群聊 </param>
        /// <param name="callBack">回调函数,由用户提供,参数：消息上下文<see cref="MessageContext"/></param>
        /// <param name="IsOpenMonitor">是否开启开放式监听，默认不开放(值为false）,如果开启开放式监听，前面的nickNames可以为空，所谓的开放式监听的含义是：无须固定好友/群监听，只要此好友/群没有设置“消息免打挠”就可以监听</param>
        /// <param name="tokenSource">取消令牌,请参考<see cref="CancellationTokenSource"/>,可以自行取消消息监听</param>
        /// <param name="UIInvoker">UI的调度器，适用于把微信嵌入UI的场景使用，如：多微信切换Tab页等,SDK会给调用者注入一个微信名称</param>
        public async Task AddMessageListener(OneOf<string, List<string>> nickNames, Action<MessageContext> callBack, bool IsOpenMonitor = false, CancellationTokenSource tokenSource = default, Action<string> UIInvoker = null)
        => await AddMessageListenerCore(nickNames, callBack, IsOpenMonitor, tokenSource, UIInvoker, new DateTimeRange());

        /// <summary>
        /// 添加一个从什么时候开始，什么时候结束的消息监听，用户需要提供一个回调函数，当有消息时，会调用此回调函数
        /// 参考<see cref="MessageContext"/>
        /// 
        /// <para>使用规则：</para>
        /// <para>1. 仅能监听“允许消息通知”的好友/群聊,所以需要监听的好友/群聊不要设置为“消息免打扰”;</para>
        /// <para>2. 如果要避免太多消息影响，请将不监听的好友/群设置为“消息免打扰”，以提高监听性能;</para>
        /// <para>3. 此方法可以多次添加，第一次添加时会启动消息监听，第二次（类似的第三次等）添加效果等同<see cref="AddListeningFriend"/>方法</para>
        /// <para>4. 为了减少会话窗口的滚动，建议不要添加太多消息“置顶”，以提高监听效率.</para>
        /// <para>执行逻辑:</para>
        /// <para>1. 启动消息监听器时做做一次全量扫描，即：SDK会将会话列表整个循环一编，会自动点击并回调用户设定的方法，以防止遗漏消息</para>
        /// <para>2. 以后的监听过程会增量监听，以提高效率.</para>
        /// </summary>
        /// <param name="nickNames">好友昵称,可以是一个，也可以是多个好友/群聊 </param>
        /// <param name="callBack">回调函数,由用户提供,参数：消息上下文<see cref="MessageContext"/></param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param> 
        /// <param name="IsOpenMonitor">是否开启开放式监听，默认不开放(值为false）,如果开启开放式监听，前面的nickNames可以为空，所谓的开放式监听的含义是：无须固定好友/群监听，只要此好友/群没有设置“消息免打挠”就可以监听</param>
        /// <param name="tokenSource">取消令牌,请参考<see cref="CancellationTokenSource"/>,可以自行取消消息监听</param>
        /// <param name="UIInvoker">UI的调度器，适用于把微信嵌入UI的场景使用，如：多微信切换Tab页等,SDK会给调用者注入一个微信名称</param>
        public async Task AddMessageListener(OneOf<string, List<string>> nickNames, Action<MessageContext> callBack, TimeOnly startTime, TimeOnly endTime, bool IsOpenMonitor = false, CancellationTokenSource tokenSource = default, Action<string> UIInvoker = null)
        => await AddMessageListenerCore(nickNames, callBack, IsOpenMonitor, tokenSource, UIInvoker, new DateTimeRange()
        {
            IsCheckDate = true,
            TimeList = new List<TimeOnlyRange>(){
                new TimeOnlyRange()
                {
                    StarTime = startTime,
                    EndTime = endTime,
                }
            }
        });
        /// <summary>
        /// 添加一天中多个时间段的消息监听，用户需要提供一个回调函数，当有消息时，会调用此回调函数
        /// 参考<see cref="MessageContext"/>
        /// 
        /// <para>使用规则：</para>
        /// <para>1. 仅能监听“允许消息通知”的好友/群聊,所以需要监听的好友/群聊不要设置为“消息免打扰”;</para>
        /// <para>2. 如果要避免太多消息影响，请将不监听的好友/群设置为“消息免打扰”，以提高监听性能;</para>
        /// <para>3. 此方法可以多次添加，第一次添加时会启动消息监听，第二次（类似的第三次等）添加效果等同<see cref="AddListeningFriend"/>方法</para>
        /// <para>4. 为了减少会话窗口的滚动，建议不要添加太多消息“置顶”，以提高监听效率.</para>
        /// <para>执行逻辑:</para>
        /// <para>1. 启动消息监听器时做做一次全量扫描，即：SDK会将会话列表整个循环一编，会自动点击并回调用户设定的方法，以防止遗漏消息</para>
        /// <para>2. 以后的监听过程会增量监听，以提高效率.</para>
        /// </summary>
        /// <param name="nickNames">好友昵称,可以是一个，也可以是多个好友/群聊 </param>
        /// <param name="callBack">回调函数,由用户提供,参数：消息上下文<see cref="MessageContext"/></param>
        /// <param name="range">一天中的多个时间段,如果设定多个时间段，监听器在这些时间段内开始/结束监听,时间段类请参考:<see cref="TimeOnlyRange"/>,另注意：可以跨天，如设置为:23:00 ~ 02:00</param>
        /// <param name="IsOpenMonitor">是否开启开放式监听，默认不开放(值为false）,如果开启开放式监听，前面的nickNames可以为空，所谓的开放式监听的含义是：无须固定好友/群监听，只要此好友/群没有设置“消息免打挠”就可以监听</param>
        /// <param name="tokenSource">取消令牌,请参考<see cref="CancellationTokenSource"/>,可以自行取消消息监听</param>
        /// <param name="UIInvoker">UI的调度器，适用于把微信嵌入UI的场景使用，如：多微信切换Tab页等,SDK会给调用者注入一个微信名称</param>
        public async Task AddMessageListener(OneOf<string, List<string>> nickNames, Action<MessageContext> callBack, List<TimeOnlyRange> range, bool IsOpenMonitor = false, CancellationTokenSource tokenSource = default, Action<string> UIInvoker = null)
        => await AddMessageListenerCore(nickNames, callBack, IsOpenMonitor, tokenSource, UIInvoker, new DateTimeRange()
        {
            IsCheckDate = true,
            TimeList = range,
        });

        /// <summary>
        /// 暂停消息监听
        /// </summary>
        /// <returns></returns>
        public async Task PauseMessageListener()
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
        public async Task ResumeMessageListener()
        {
            while(Interlocked.Exchange(ref this._IsContinue,1) == 0)
            {
                await Task.Delay(0);
            }
        }

        /// <summary>
        /// 监听过程中添加好友
        /// </summary>
        /// <param name="who">好友名称</param>
        /// <returns></returns>
        public async Task AddListeningFriend(string who)
        {
            if (messageListnerStartedFlag != 1)
                throw new Exception("错误：请先启动消息监听器");
            if (!_MessageList.Keys.Contains(who))
            {
                _MessageList.TryAdd(who, false);
            }
            await Task.CompletedTask;
        }
        /// <summary>
        /// 监听过程中移除被监听中的好友/群聊
        /// </summary>
        /// <param name="who"></param>
        /// <returns></returns>
        public async Task RemoveListeningFriend(string who)
        {
            if (messageListnerStartedFlag != 1)
                throw new Exception("错误：请先启动消息监听器");
            _MessageList.TryRemove(who, out _);
            await Task.CompletedTask;
        }

        private async Task AddMessageListenerCore(OneOf<string, List<string>> nickNames, Action<MessageContext> callBack, bool IsOpenMonitor, CancellationTokenSource tokenSource, Action<string> UIInvoker, DateTimeRange range)
        {
            if (Interlocked.CompareExchange(ref messageListnerStartedFlag, 1, 0) == 1)
            {
                List<string> list = nickNames.IsT0 ? new List<string>() { nickNames.AsT0 } : nickNames.AsT1;
                foreach (var item in list)
                {
                    await AddListeningFriend(item);
                }
                return;
            }
            this.UIInvoker = UIInvoker;
            CancellationToken token;
            if (tokenSource != default)
            {
                token = CancellationTokenSource.CreateLinkedTokenSource(messageCts.Token, tokenSource.Token).Token;
            }
            else
            {
                token = messageCts.Token;
            }
            (nickNames.IsT0 ? new List<string>() { nickNames.AsT0 } : nickNames.AsT1).ForEach(u => _MessageList.TryAdd(u, false));  //赋初始值.
            try
            {
                var startTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                messageRunningTask = Task.Run(async () =>
                {
                    startTcs.TrySetResult(true);
                    using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(WeAutomation.Config.MonitorMessageInterval));
                    while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
                    {
                        try
                        {
                            if (_IsContinue != 1)  //如果暂停了
                                continue;
                            if (range.IsCheckDate) //时间段检查
                            {
                                var flag = _CheckRuntime(range.TimeList);
                                if (!flag)
                                    continue;
                            }
                            await noticeEvent.WaitAsync(token);
                            try
                            {
                                await WeChatInvoker.Call(AddMessageListenerAction, callBack, token, IsOpenMonitor);
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
        private void AddMessageListenerAction(UIA3Automation automation, Action<MessageContext> callBack, CancellationToken token, bool IsOpenMonitor)
        {
            var root = this._Client.Conversations.ConversationRoot;
            token.ThrowIfCancellationRequested();
            if (messageStarted)
            {
                //一开始对会话遍历一次.
                messageStarted = false;
                _SwtichUI(token);
                _TravelConversationList(automation, callBack, IsOpenMonitor, token, root);
                return;
            }
            #region 鼠标方案,比较有意思，但是不够优雅，暂时取消
            // _TryPopupNoticeMenu(automation, token);
            // if (!_CheckExistNotice(automation, token))
            // {
            //     var point = this._Client.MainWindow.BoundingRectangle.SafeRandomPoint();
            //     Mouse.Position = point;
            //     return;
            // }
            #endregion
            #region UI Tree方案
            this.totalNewMessage = _GetTotalMessage(token);
            if (this.totalNewMessage == 0)
            {
                return;
            }
            #endregion
            //增量执行监听
            _SwtichUI(token);
            token.ThrowIfCancellationRequested();
            _MessageClickScheduling(automation, callBack, token, IsOpenMonitor, root);
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

        private void _TravelConversationList(UIA3Automation automation, Action<MessageContext> callBack, bool IsOpenMonitor, CancellationToken token, ListBox root)
        {
            //当前的消息列表处理
            _ProcessVisibleConversation(automation, callBack, IsOpenMonitor, root.BoundingRectangle, token);
            //先往上翻到顶.
            this._Client.Conversations.UpCore(automation, (els, rootRect) =>
            {
                _ProcessVisibleConversation(automation, callBack, IsOpenMonitor, rootRect, token);
                return true;
            });
            //再往下翻到底
            this._Client.Conversations.DownCore(automation, (el, rootRect) =>
            {
                _ProcessVisibleConversation(automation, callBack, IsOpenMonitor, rootRect, token);
                return true;
            });
            //再往上翻到顶
            this._Client.Conversations.UpCore(automation, (els, rootRect) => true);
        }

        private void _MessageClickScheduling(UIA3Automation automation, Action<MessageContext> callBack, CancellationToken token, bool IsOpenMonitor, ListBox root)
        {
            this._Client.MainWindow.Focus();
            Mouse.Position = _Client.MainWindow.BoundingRectangle.Center();

            var index = 0;
            token.ThrowIfCancellationRequested();

            //当前的消息列表处理
            _ProcessVisibleConversation(automation, callBack, IsOpenMonitor, root.BoundingRectangle, token);

            //先往下滚动
            this._Client.Conversations.DownCore(automation, (el, rootRect) =>
            {
                index++;
                _ProcessVisibleConversation(automation, callBack, IsOpenMonitor, rootRect, token);
                if (index <= WeAutomation.Config.MonitorMessageMaxDownInterval)
                {
                    return true;
                }
                return false;
            });
            //再往上翻到顶.
            this._Client.Conversations.UpCore(automation, (els, rootRect) =>
            {
                _ProcessVisibleConversation(automation, callBack, IsOpenMonitor, rootRect, token);
                return true;
            });

            token.ThrowIfCancellationRequested();
        }

        private void _ProcessVisibleConversation(UIA3Automation automation, Action<MessageContext> callBack, bool IsOpenMonitor, Rectangle rootRect, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var dontCheckItems = new string[] { "服务号", "服务通知", "文件传输助手", "公众号", "元宝", "微信团队" };
            List<SimpleConversation> fullList = _Client.Conversations.GetVisibleConversationsCore(automation);
            RandomWait.Wait(100, 350);
            var filterObjList = fullList.Where(item => !item.IsDoNotDisturb && item.NotReadNumbr > 0).ToList();  //取出没有设置“免打扰”的好友,并且未读数>0
            var elementList = _Client.Conversations.GetVisibleConversationElements(automation);
            List<AutomationElement> list = new List<AutomationElement>();
            if (IsOpenMonitor)
            {
                var tmpList = filterObjList.Select(x => x.ConversationTitle);
                foreach (var item in elementList)
                {
                    string[] aryItems = item.Name.Split('\n');
                    var name = aryItems[0].Trim();
                    if (tmpList.Contains(name))
                    {
                        list.Add(item);
                    }
                }
            }
            else
            {
                var tmpList = filterObjList.Select(x => x.ConversationTitle).ToList().Intersect(this._MessageList.Keys).ToList();  //与设定监听的集合做交集.
                foreach (var item in elementList)
                {
                    string[] aryItems = item.Name.Split('\n');
                    var name = aryItems[0].Trim();
                    if (tmpList.Contains(name))
                    {
                        list.Add(item);
                    }
                }
            }
            foreach (var item in list)
            {
                token.ThrowIfCancellationRequested();
                if (item.BoundingRectangle.IsClickSafe(rootRect))
                {
                    var serviceBackClick = item.Name.StartsWith("服务号\n") ? true : false;
                    var point = item.BoundingRectangle.SafeRandomPoint();
                    Mouse.Position = point;
                    Mouse.Click();
                    RandomWait.Wait(300, 900);
                    _ProcessClickServiceNumber(serviceBackClick);
                    _ParserMessaageCore(automation, callBack, token, item);
                }
            }
        }

        private void _ProcessClickServiceNumber(bool serviceBackClick)
        {
            if (serviceBackClick)
            {
                var backButtonRetry = Retry.WhileNull(() => _Client.MainWindow.FindFirstDescendant(cf => cf.ByName("返回").And(cf.ByAutomationId("button")).And(cf.ByClassName("mmui::ChatBackwardView")).And(cf.ByControlType(ControlType.Button).And(cf.ByProcessId(this._Client.MainWindow.Properties.ProcessId)))), timeout: TimeSpan.FromSeconds(1), interval: TimeSpan.FromMilliseconds(200));
                if (backButtonRetry.Success)
                {
                    var button = backButtonRetry.Result;
                    button.WaitUntilClickable();
                    button.Click();
                }
            }
        }

        private void _ParserMessaageCore(UIA3Automation automation, Action<MessageContext> callBack, CancellationToken token, AutomationElement el)
        {
            _Logger.Debug($"自动点击了:{el.Name.Trim()}");
        }

        /// <summary>
        /// 仅是了解有没有消息，但是并不做解读数量等操作.
        /// </summary>
        /// <param name="automation"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [Obsolete("此方法可用，但是不够优雅，暂时取消")]
        private bool _CheckExistNotice(UIA3Automation automation, CancellationToken token)
        {
            var desktop = automation.GetDesktop();
            token.ThrowIfCancellationRequested();
            var winRetry = Retry.WhileNull(() => desktop.FindFirstChild(cf => cf.ByName("Weixin").And(cf.ByControlType(ControlType.Window)).And(cf.ByClassName("mmui::UnreadMessageHoverWindow")).And(cf.ByProcessId(this._Client.MainWindow.Properties.ProcessId))), timeout: TimeSpan.FromSeconds(1), interval: TimeSpan.FromMilliseconds(200));
            token.ThrowIfCancellationRequested();
            return winRetry.Success;
        }

        [Obsolete("这个方法可用，但是不够优雅，暂时出消")]
        private void _TryPopupNoticeMenu(UIA3Automation automation, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Button button = _Client.NotifyIcon.GetButtonCore(automation);
            Point point1 = button.BoundingRectangle.SafeRandomPoint();
            Point point2 = button.BoundingRectangle.SafeRandomPoint();
            Mouse.Position = point1;
            RandomWait.Wait(100, 500);
            token.ThrowIfCancellationRequested();
            Mouse.MoveTo(point2);  //弹出菜单
        }
        #endregion

        #region 会话切换监听
        #endregion

        private bool _CheckRuntime(List<TimeOnlyRange> timeList)
        {
            var now = TimeOnly.FromDateTime(DateTime.Now);

            foreach (var time in timeList)
            {
                if (_IsInRange(now, time.StarTime, time.EndTime))
                    return true;
            }

            return false;
        }

        private bool _IsInRange(TimeOnly now, TimeOnly start, TimeOnly end)
        {
            // 普通区间
            if (start <= end)
            {
                return now >= start && now <= end;
            }

            // 跨天区间
            return now >= start || now <= end;
        }

        #region 释放器
        ~MessageMonitor()
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
                messageCts?.Cancel();
                if (messageRunningTask != null)
                {
                    if (!messageRunningTask.IsCompleted)
                    {
                        try
                        {
                            messageRunningTask.Wait(TimeSpan.FromSeconds(3));
                        }
                        catch (AggregateException) { }
                        catch (Exception) { }
                    }
                }

                messageCts?.Dispose();
                messageRunningTask?.Dispose();
            }
        }
        #endregion
    }
}