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
using WeChatAuto.Options;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Drawing.Imaging;

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
        //消息监听
        private int messageListnerStartedFlag = 0;   //消息监听启用标识
        private bool messageStarted = true;
        private int totalNewMessage = 0;   //新消息数量
        private readonly ConcurrentDictionary<string, bool> _MonitorList = new ConcurrentDictionary<string, bool>();
        private CancellationTokenSource messageCts = new CancellationTokenSource();
        private Task messageRunningTask;
        private Action<string> UIInvoker;
        private int _IsContinue = 1;
        //系统消息监听
        private int systemListnerStartedFlag = 0;   //系统消息监听启用标识
        private readonly ConcurrentDictionary<string, bool> _SystemMonitorList = new ConcurrentDictionary<string, bool>();
        private readonly List<UIThreadInvoker> _SystemMonitorTasks = new List<UIThreadInvoker>();
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
        /// <para>添加系统消息监听，以实现如： 检测到群主邀请好友后发送欢迎消息等功能</para>
        /// <para>注意：仅适用于群聊，不适用个人，另外，不支持注册监听后再新增待监听的群聊</para>
        /// 多线程监听变化，但是操作等，还得在微信单线程中执行.
        /// </summary>
        /// <param name="nickNames">群聊昵称</param>
        /// <param name="callBack">回调函数,由用户提供,参数：消息上下文<see cref="MessageContext"/></param>
        /// <param name="userToken">取消令牌,请参考<see cref="CancellationToken"/>,可以自行取消消息监听</param>
        /// <returns></returns>
        public async Task AddGroupSystemMessageListener(OneOf<string, List<string>, string[]> nickNames, Action<MessageContext> callBack, CancellationToken userToken)
        {
            AddGroupSystemMessageListenerCore(nickNames, callBack, userToken);
            await Task.CompletedTask;
        }
        private void AddGroupSystemMessageListenerCore(OneOf<string, List<string>, string[]> nickNames, Action<MessageContext> callBack, CancellationToken userToken)
        {
            if (Interlocked.CompareExchange(ref this.systemListnerStartedFlag, 1, 0) == 1)
            {
                return;
            }

            try
            {

            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                _Logger.Error($"监听系统消息发生错误：{ex.ToString()}");
            }
        }

        /// <summary>
        /// <para>添加消息监听，用户需要提供一个回调函数，当有消息时，会调用此回调函数</para>
        /// <para>参考<see cref="MessageContext"/></para>
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
        /// <param name="IsOpenMonitor">是否开启开放式监听，默认不开放(值为false）,如果开启开放式监听，前面的nickNames可以为空，所谓的开放式监听的含义是：无须固定好友/群监听，只要此好友/群没有设置“消息免打挠”就可以监听,而固定式监听为只监听设定的好友/群</param>
        /// <param name="userToken">取消令牌,请参考<see cref="CancellationToken"/>,可以自行取消消息监听</param>
        /// <param name="UIInvoker">UI的调度器，适用于把微信嵌入UI的场景使用，如：多微信切换Tab页等,SDK会给调用者注入一个微信名称</param>
        /// <param name="options">监听选项，具体请参见<see cref="MessageMonitorOptions"/></param>
        public async Task AddMessageListener(OneOf<string, List<string>, string[]> nickNames, Action<MessageContext> callBack, bool IsOpenMonitor = false, CancellationToken userToken = default, Action<string> UIInvoker = null, MessageMonitorOptions options = null)
        => await AddMessageListenerCore(nickNames, callBack, IsOpenMonitor, userToken, UIInvoker, new DateTimeRange(), options);

        /// <summary>
        /// 添加一个从什么时候开始，什么时候结束的消息监听，用户需要提供一个回调函数，当有消息时，会调用此回调函数
        /// 参考<see cref="MessageContext"/>
        /// 添加此监听，在设定的开始时间开始监听，并且在设定的结束时间暂停监听
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
        /// <param name="userToken">取消令牌,请参考<see cref="CancellationToken"/>,可以自行取消消息监听</param>
        /// <param name="UIInvoker">UI的调度器，适用于把微信嵌入UI的场景使用，如：多微信切换Tab页等,SDK会给调用者注入一个微信名称</param>
        /// <param name="options">监听选项，具体请参见<see cref="MessageMonitorOptions"/></param>
        public async Task AddMessageListener(OneOf<string, List<string>, string[]> nickNames, Action<MessageContext> callBack, TimeOnly startTime, TimeOnly endTime, bool IsOpenMonitor = false, CancellationToken userToken = default, Action<string> UIInvoker = null, MessageMonitorOptions options = null)
        => await AddMessageListenerCore(nickNames, callBack, IsOpenMonitor, userToken, UIInvoker, new DateTimeRange()
        {
            IsCheckDate = true,
            TimeList = new List<TimeOnlyRange>(){
                new TimeOnlyRange()
                {
                    StarTime = startTime,
                    EndTime = endTime,
                }
            }
        }, options);
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
        /// <param name="userToken">取消令牌,请参考<see cref="CancellationToken"/>,可以自行取消消息监听</param>
        /// <param name="UIInvoker">UI的调度器，适用于把微信嵌入UI的场景使用，如：多微信切换Tab页等,SDK会给调用者注入一个微信名称</param>
        /// <param name="options">监听选项，具体请参见<see cref="MessageMonitorOptions"/></param>
        public async Task AddMessageListener(OneOf<string, List<string>, string[]> nickNames, Action<MessageContext> callBack, List<TimeOnlyRange> range, bool IsOpenMonitor = false, CancellationToken userToken = default, Action<string> UIInvoker = null, MessageMonitorOptions options = null)
        => await AddMessageListenerCore(nickNames, callBack, IsOpenMonitor, userToken, UIInvoker, new DateTimeRange()
        {
            IsCheckDate = true,
            TimeList = range,
        }, options);

        /// <summary>
        /// 暂停消息监听
        /// </summary>
        /// <returns></returns>
        public async Task PauseMessageListener()
        {
            Volatile.Write(ref _IsContinue, 0);
            await Task.CompletedTask;
        }
        /// <summary>
        /// 恢复消息监听
        /// </summary>
        /// <returns></returns>
        public async Task ResumeMessageListener()
        {
            Volatile.Write(ref _IsContinue, 1);
            await Task.CompletedTask;
        }

        /// <summary>
        /// 监听过程中动态添加待监听的好友/群聊
        /// </summary>
        /// <param name="who">好友/群聊名称</param>
        /// <returns></returns>
        public async Task AddListeningFriend(string who)
        {
            if (messageListnerStartedFlag != 1)
                throw new Exception("错误：请先启动消息监听器");
            if (!_MonitorList.Keys.Contains(who))
            {
                _MonitorList.TryAdd(who, false);
            }
            await Task.CompletedTask;
        }
        /// <summary>
        /// 监听过程中动态移除被监听的好友/群聊
        /// </summary>
        /// <param name="who">好友/群聊</param>
        /// <returns></returns>
        public async Task RemoveListeningFriend(string who)
        {
            if (messageListnerStartedFlag != 1)
                throw new Exception("错误：请先启动消息监听器");
            _MonitorList.TryRemove(who, out _);
            await Task.CompletedTask;
        }

        private async Task AddMessageListenerCore(OneOf<string, List<string>, string[]> nickNames, Action<MessageContext> callBack, bool IsOpenMonitor, CancellationToken userToken, Action<string> UIInvoker, DateTimeRange range, MessageMonitorOptions options)
        {
            if (Interlocked.CompareExchange(ref messageListnerStartedFlag, 1, 0) == 1)
            {
                List<string> list = nickNames.IsT0 ? new List<string>() { nickNames.AsT0 } : nickNames.IsT1 ? nickNames.AsT1 : nickNames.AsT2.ToList();
                foreach (var item in list)
                {
                    await AddListeningFriend(item);
                }
                return;
            }
            this.UIInvoker = UIInvoker;
            CancellationToken token;
            CancellationTokenSource linkedCts = default;
            if (userToken != default)
            {
                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(messageCts.Token, userToken);
                token = linkedCts.Token;
            }
            else
            {
                token = messageCts.Token;
            }
            (nickNames.IsT0 ? new List<string>() { nickNames.AsT0 } : nickNames.IsT1 ? nickNames.AsT1.ToHashSet().ToList() : nickNames.AsT2.ToHashSet().ToList()).ForEach(u => _MonitorList.TryAdd(u, false));  //赋初始值.
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
                                await WeChatInvoker.Call(AddMessageListenerAction, callBack, token, IsOpenMonitor, options);
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
            finally
            {
                if (linkedCts != default)
                {
                    linkedCts?.Dispose();
                }
            }
        }
        private void AddMessageListenerAction(UIA3Automation automation, Action<MessageContext> callBack, CancellationToken token, bool IsOpenMonitor, MessageMonitorOptions options)
        {
            var root = this._Client.Conversations.ConversationRoot;
            token.ThrowIfCancellationRequested();
            if (messageStarted)
            {
                //一开始对会话列表遍历一次.
                messageStarted = false;
                _SwtichUI(token);
                _TravelConversationList(automation, callBack, IsOpenMonitor, token, root, options);
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
            _MessageClickScheduling(automation, callBack, token, IsOpenMonitor, root, options);
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

        private void _TravelConversationList(UIA3Automation automation, Action<MessageContext> callBack, bool IsOpenMonitor, CancellationToken token, ListBox root, MessageMonitorOptions options)
        {
            //当前的消息列表处理
            _ProcessVisibleConversation(automation, callBack, IsOpenMonitor, root.BoundingRectangle, token, options);
            //先往上翻到顶.
            this._Client.Conversations.UpCore(automation, (els, rootRect) =>
            {
                _ProcessVisibleConversation(automation, callBack, IsOpenMonitor, rootRect, token, options);
                return true;
            });
            //再往下翻到底
            this._Client.Conversations.DownCore(automation, (el, rootRect) =>
            {
                _ProcessVisibleConversation(automation, callBack, IsOpenMonitor, rootRect, token, options);
                return true;
            });
            //再往上翻到顶
            this._Client.Conversations.UpCore(automation, (els, rootRect) => true);
        }

        private void _MessageClickScheduling(UIA3Automation automation, Action<MessageContext> callBack, CancellationToken token, bool IsOpenMonitor, ListBox root, MessageMonitorOptions options)
        {
            this._Client.MainWindow.Focus();
            Mouse.Position = _Client.MainWindow.BoundingRectangle.Center();

            var index = 0;
            token.ThrowIfCancellationRequested();

            //当前的消息列表处理
            _ProcessVisibleConversation(automation, callBack, IsOpenMonitor, root.BoundingRectangle, token, options);
            if (_GetTotalMessage(token) == 0)
                return;

            //先往下滚动
            this._Client.Conversations.DownCore(automation, (el, rootRect) =>
            {
                index++;
                _ProcessVisibleConversation(automation, callBack, IsOpenMonitor, rootRect, token, options);
                if (index <= WeAutomation.Config.MonitorMessageMaxDownInterval)
                {
                    return true;
                }
                return false;
            });
            //再往上翻到顶.
            this._Client.Conversations.UpCore(automation, (els, rootRect) =>
            {
                _ProcessVisibleConversation(automation, callBack, IsOpenMonitor, rootRect, token, options);
                return true;
            });

            token.ThrowIfCancellationRequested();
        }

        //未设置“免打扰”,有未读消息数字
        private void _ProcessVisibleConversation(UIA3Automation automation, Action<MessageContext> callBack, bool IsOpenMonitor, Rectangle rootRect, CancellationToken token, MessageMonitorOptions options)
        {
            List<SimpleConversation> fullList = _Client.Conversations.GetVisibleConversationsCore(automation);
            RandomWait.Wait(100, 350);
            token.ThrowIfCancellationRequested();
            var filterObjList = fullList.Where(item => !item.IsDoNotDisturb && item.NotReadNumbr > 0).ToList();  //取出没有设置“免打扰”的好友,并且未读数>0
            var elementList = _Client.Conversations.GetVisibleConversationElements(automation);  //取所有可见的会话元素
            List<AutomationElement> clickList = new List<AutomationElement>();  //待点击的列表
            var tmpList = filterObjList.Select(x => x.ConversationTitle);  //可点击的元素标题 列表
            foreach (var item in elementList)
            {
                string[] aryItems = item.Name.Split('\n');
                var name = aryItems[0].Trim();
                if (tmpList.Contains(name))
                {
                    clickList.Add(item);
                }
            }
            var willParseList = new List<string>();  //待获取元素并要求返回消息回调s
            if (IsOpenMonitor)
            {
                willParseList.AddRange(clickList.Select(u => u.GetName()));
            }
            else
            {
                var l1 = clickList.Select(u => u.GetName()).ToList(); //本次获取的对象。
                var l2 = this._MonitorList.Keys;  //所有监听对象
                willParseList.AddRange(l2.Intersect(l1));
            }

            var messageListRoot = this._Client.MainWindow.FindFirstByXPath("/Group/Custom/Group/Group/Group/Custom/Custom/Custom/Group/Custom/Custom/Group/Custom/Group/List[@AutomationId='chat_message_list'][@ClassName='mmui::RecyclerListView'][@Name='消息']");
            if (messageListRoot == null)
            {
                _Logger.Error("未找到消息列表根元素，无法获取消息");
                return;
            }

            foreach (var item in clickList)   //丢失了点击没有关系，下一轮又会点击，关键不能失去返回消息的能力.
            {
                token.ThrowIfCancellationRequested();
                if (item.BoundingRectangle.IsClickSafe(rootRect))
                {
                    //int count = _GetCurrentNotReadNumbr(item, token);  //获取未读消息数量
                    var serviceBackClickFlag = item.Name.StartsWith("服务号\n") ? true : false;
                    var point = item.BoundingRectangle.SafeRandomPoint();
                    Mouse.Position = point;
                    RandomWait.Wait(100, 300);
                    SupperMouseKey.LeftClick();   //点击
                    RandomWait.Wait(300, 900);
                    _ProcessClickServiceNumber(serviceBackClickFlag);  //点击了服务号需要进行返回
                    if (!serviceBackClickFlag && willParseList.Contains(item.GetName()))
                    {
                        _ParserMessaageCore(automation, callBack, token, item, options, messageListRoot);
                    }
                }
            }
        }

        //item 为会话列表AutomationElement
        private int _GetCurrentNotReadNumbr(AutomationElement item, CancellationToken token)
        {
            var result = 0;
            var aryContent = item.Name.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var i in aryContent)
            {
                var pattern = @"\[(\d+)条\]";
                var match = Regex.Match(i, pattern);
                if (match.Success)
                {
                    result = int.TryParse(match.Groups[1].Value, out var r) ? r : 0;
                    break;
                }
            }
            return result;
        }

        private void _ParserMessaageCore(UIA3Automation automation, Action<MessageContext> callBack, CancellationToken token, AutomationElement clickConversionItem, MessageMonitorOptions options, AutomationElement messageListRoot)
        {
            var title = this._Client.ChatContent.ChatHeader.GetTitleCore(automation);
            if (options != null && options.FetchFriendInfo)
            {
                //先获取用户信息后再获取消息
                if (title.HeaderType == ChatType.好友)  //只有普通好友才有wxid.
                {
                    _FetchFriendInfo(automation, token, title);
                }
            }
            //获取消息
            //获取消息核心逻辑
            _ProcessUpBlanceMessage(automation, token, clickConversionItem, messageListRoot);  //如果有未读消息自动点击
            var popResult = _ProcessPopMenu(automation, messageListRoot);  //弹出并点击“多选”菜单
            if (!popResult)
            {
                _Logger.Error("解析消息时，右键菜单解析失败，可能会导致消息监听失败");
                return;
            }
            List<SimpleMessageBubble> newMessages = _FetchMessageCore(automation, token, clickConversionItem, messageListRoot, title.Title); //本次最新的消息列表
            MessageCacheHelper.AddTodayMessageCaches(title.Title, newMessages);  //增加进缓存.
            //回调用户提供的方法
        }

        private List<SimpleMessageBubble> _FetchMessageCore(UIA3Automation automation, CancellationToken token, AutomationElement clickConversionItem, AutomationElement messageListRoot, string who)
        {
            List<SimpleMessageBubble> result = new List<SimpleMessageBubble>();  //本次最新的消息列表
            this._Client.MainWindow.Focus();
            var point = messageListRoot.BoundingRectangle.SafeRandomPoint();
            Mouse.Position = point;
            RandomWait.Wait(100, 300);
            SupperMouseKey.MoveTo(point.Confusion(5, 10));
            RandomWait.Wait(100, 900);
            var lastItems = new List<AutomationElement>();
            var firstFlag = true;
            var index = 3;  //循环取3次.
            while (index < 3)
            {
                var items = messageListRoot.FindAllChildren().ToList();  //全部本次滚动全部子ListItem.
                var exceptList = items.Except(lastItems, new AutomationRuntimeComparer()).ToList();
                if (exceptList.Count() == 0)
                {
                    index++;
                }
                else
                {
                    lastItems = items;
                    var newList = _GenerateSimpleMessageBubbles(exceptList.ToArray(), messageListRoot, ref firstFlag, who, automation);
                    result.AddRange(newList);  //增加进入结果列表
                    index = 0;
                }
                var mousePosition = point.Confusion(5, 10);
                SupperMouseKey.MoveTo(mousePosition);
                RandomWait.Wait(300, 900);
                MouseScrollHelper.DownStep(mousePosition, 2);
            }
            return result;
        }

        private List<SimpleMessageBubble> _GenerateSimpleMessageBubbles(AutomationElement[] newItems, AutomationElement messageListRoot, ref bool firstFlag, string title, UIA3Automation automation)
        {
            var newList = new List<SimpleMessageBubble>();
            foreach (var item in newItems)
            {
                if (item.BoundingRectangle.Y < messageListRoot.BoundingRectangle.Y || item.BoundingRectangle.Y + 85 > messageListRoot.BoundingRectangle.Y + messageListRoot.BoundingRectangle.Height)  //因为不在处理范围，处理不了,事实之前循环已经处理.
                    continue;
                if (item is CheckBox)
                {
                    //真正的消息
                    //如果有图片消息，或者红包/转账消息，则取消-->处理-->再勾选.
                    SimpleMessageBubble message = new SimpleMessageBubble();
                    string[] aryMessage = item.Name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    string who = aryMessage[0];
                    if (who == this._Client.NickName)
                    {
                        who = "我";
                    }
                    message.Who = who;
                    message.SendDate = DateTime.Now;
                    string maybeType = aryMessage.Length > 1 ? aryMessage[1] : "";
                    string content = item.Name.Trim().Substring(who.Length).Trim();
                    __ProcessMessageType(maybeType, message, content, item.Name.Trim());
                    if (message.MessageType == MessageType.图片)
                    {
                        __ProcessImage(item, messageListRoot, automation, message);
                    }
                    if (message.MessageType == MessageType.红包)
                    {
                        __ProcessRedEnvelope(item, messageListRoot, automation);
                    }
                    if (message.MessageType == MessageType.微信转账)
                    {
                        __ProcessTransfer(item, messageListRoot, automation);
                    }
                    //alexzhao 1234
                    newList.Add(message);
                }
                if (item is ListBoxItem)
                {
                    //其他消息，如：系统消息
                    //时间
                    //收取红包
                    //拒绝红包
                    //拍一拍
                    //加入群聊
                    //移出群聊
                    //撤回了一条消息
                    //其他消息
                    //剔除时间
                    string pattern = @"\s(?:[01]\d|2[0-3]):[0-5]\d$";
                    if (Regex.IsMatch(item.Name, pattern))
                        continue;
                    SimpleMessageBubble message = new SimpleMessageBubble();
                    message.Who = "系统";
                    message.message = item.Name.Trim();
                    message.MessageType = MessageType.其他;   //先统一为其他，以后再分出来.
                    message.SendDate = DateTime.Now;
                    newList.Add(message);
                }
            }

            if (firstFlag)
            {
                var historyList = MessageCacheHelper.GetTodayLastMessages(title, 5);  //历史消息中最后5条，因为首次可能会与历史重复.
                newList = newList.Where(x => !historyList.Contains(x, new MessageComparer())).ToList();

                firstFlag = false;
            }
            return newList;
        }

        private void __ProcessImage(AutomationElement item, AutomationElement messageListRoot, UIA3Automation automation, SimpleMessageBubble message)
        {
            int[] runtimeId = item.Properties.RuntimeId.Value;
            var path = "/Group/Custom/Group/Group/Group/Custom/Custom/Custom/Group/Custom/Custom/Group/Custom/Group/ToolBar/Group/Button[@Name='取消多选'][@ClassName='mmui::XButton']";
            var buttonRetry = Retry.WhileNull(() => this._Client.MainWindow.FindFirstByXPath(path), TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(200));
            if (buttonRetry.Success)
            {
                var button = buttonRetry.Result;
                var point = button.BoundingRectangle.Center().Confusion(3, 5);
                Mouse.Position = point;
                RandomWait.Wait(100, 300);
                SupperMouseKey.MoveTo(point.Confusion(3, 5));
                RandomWait.Wait(300, 900);
                SupperMouseKey.LeftClick();
                RandomWait.Wait(300, 900);
            }
            //点击图片
            var index = 3;
            var items = messageListRoot.FindAllChildren();
            var clickItem = items.FirstOrDefault(x => x.Properties.RuntimeId.Value.SequenceEqual(runtimeId));
            if (clickItem == null)
            {
                _Logger.Error("处理图片数据时发生错误,请检查原因！");
                return;
            }
            while (index < 3)
            {
                this._Client.MainWindow.Focus();
                __ProcessPopMenuCore(automation, clickItem);
                var imageWinRetry = Retry.WhileNull(() => automation.GetDesktop().FindFirstChild(cf => cf.ByProcessId(this._Client.MainWindow.Properties.ProcessId).And(cf.ByName("图片和视频")).And(cf.ByClassName("mmui::PreviewWindow"))), TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(200));
                if (imageWinRetry.Success)
                {
                    var imageWin = imageWinRetry.Result.AsWindow();
                    imageWin.Focus();
                    var point = imageWin.BoundingRectangle.Center();
                    point = new Point(point.X, point.Y + 50);
                    Mouse.Position = point.Confusion(10, 10);
                    RandomWait.Wait(100, 300);
                    SupperMouseKey.MoveTo(point.Confusion(10, 10));
                    RandomWait.Wait(300, 900);
                    SupperMouseKey.RightClick();
                    RandomWait.Wait(300, 900);
                    var menuWinRetry = Retry.WhileNull(() => imageWin.FindFirstChild(cf => cf.ByControlType(ControlType.Window).And(cf.ByName("Weixin").And(cf.ByClassName("mmui::XMenu")))), TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(200));
                    if (!menuWinRetry.Success)
                    {
                        index++;
                        continue;
                    }
                    var copyItem = menuWinRetry.Result.FindFirstChild(cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName("复制")));
                    copyItem.Click();
                    RandomWait.Wait(300, 900);
                    var isImage = System.Windows.Clipboard.ContainsImage();
                    if (isImage)
                    {
                        Bitmap bitmap = (Bitmap)System.Windows.Forms.Clipboard.GetImage();
                        message.Image = bitmap;
                        path = Path.Combine(AppContext.BaseDirectory, "ImageCaches");
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }
                        path = Path.Combine(path, $"{Guid.NewGuid().ToString("N")}.png");
                        bitmap.Save(path, ImageFormat.Png);
                        message.ImageFile = path;
                    }

                    RandomWait.Wait(300, 900);
                    imageWin.Close();
                    break;
                }
                index++;
            }

            //处理后，继续变成checkbox状态
            this._Client.MainWindow.Focus();
            __ProcessPopMenuCore(automation, clickItem);
        }

        private void __ProcessRedEnvelope(AutomationElement item, AutomationElement messageListRoot, UIA3Automation automation)
        {
            int[] runtimeId = item.Properties.RuntimeId.Value;
        }

        private void __ProcessTransfer(AutomationElement item, AutomationElement messageListRoot, UIA3Automation automation)
        {

        }

        private void __ProcessMessageType(string maybeType, SimpleMessageBubble message, string content, string allContent)
        {
            maybeType = maybeType.Trim();
            message.message = content;
            if (string.IsNullOrWhiteSpace(maybeType))
            {
                message.MessageType = MessageType.文字;
                return;
            }
            if (maybeType == "图片")
            {
                message.MessageType = MessageType.图片;
                return;
            }
            //处理语音
            var pattern = @"^语音\d+""秒(.+)$";
            var match = Regex.Match(maybeType, pattern);
            if (match.Success)
            {
                message.MessageType = MessageType.语音;
                message.message = match.Groups[1].Value;
                return;
            }
            //处理视频 消息
            if (maybeType == "视频")
            {
                message.MessageType = MessageType.视频;
                return;
            }
            if (maybeType == "文件")
            {
                message.MessageType = MessageType.文件;
                return;
            }
            if (maybeType.StartsWith("聊天记录") && maybeType.Contains("的聊天记录"))
            {
                message.MessageType = MessageType.聊天记录;
                return;
            }
            if (maybeType.StartsWith("位置"))
            {
                message.MessageType = MessageType.位置;
                return;
            }
            if (maybeType.EndsWith("个人名片"))
            {
                message.MessageType = MessageType.个人名片;
                message.message = content;
                return;
            }
            if (maybeType.StartsWith("视频号") && !maybeType.EndsWith("直播中"))
            {
                message.MessageType = MessageType.视频号;
                return;
            }
            if (maybeType.StartsWith("视频号") && maybeType.EndsWith("直播中"))
            {
                message.MessageType = MessageType.视频号直播;
                return;
            }

            if (allContent.EndsWith("微信红包"))
            {
                message.Who = "_系统_";
                message.MessageType = MessageType.红包;
                message.message = allContent;
                return;
            }
            if (allContent.EndsWith("微信转账"))
            {
                message.Who = "_系统_";
                message.MessageType = MessageType.微信转账;
                message.message = allContent;
                return;
            }
            if (maybeType.StartsWith("小程序"))
            {
                message.MessageType = MessageType.小程序;
                return;
            }
            if (maybeType.StartsWith("[链接]"))
            {
                message.MessageType = MessageType.链接;
                return;
            }
            if (maybeType.StartsWith("动画表情") || maybeType.EndsWith("表情"))
            {
                message.MessageType = MessageType.表情;
                return;
            }
            if (maybeType.StartsWith("笔记"))
            {
                message.MessageType = MessageType.笔记;
                return;
            }
            message.MessageType = MessageType.文字;  //以上皆不是，归为文本.
        }

        //如果聊天窗口顶部有未读消息按钮，则点击获取消息.
        private void _ProcessUpBlanceMessage(UIA3Automation automation, CancellationToken token, AutomationElement clickConversionItem, AutomationElement messageListRoot)
        {
            token.ThrowIfCancellationRequested();
            var blanceButton = messageListRoot.GetSibling(1);
            if (blanceButton != null && blanceButton.ControlType == ControlType.Button)
            {
                var centerPoint = messageListRoot.BoundingRectangle.Center();
                var point = blanceButton.BoundingRectangle.Center();
                if (point.Y < centerPoint.Y)  //如果未读消息按钮在消息列表上方，则点击它
                {
                    Mouse.Position = point.Confusion(10, 3);
                    RandomWait.Wait(100, 300);
                    SupperMouseKey.MoveTo(point.Confusion(5, 3));
                    RandomWait.Wait(300, 900);
                    SupperMouseKey.LeftClick();
                    RandomWait.Wait(300, 900);
                }
            }
        }

        internal bool _ProcessPopMenu(UIA3Automation automation, AutomationElement messageListRoot)
        {
            var messages = messageListRoot.FindAllChildren(cf => cf.ByControlType(ControlType.ListItem));
            foreach (var item in messages)
            {
                if (ExcludeMessage(item))
                    continue;
                if ((item.BoundingRectangle.Y >= messageListRoot.BoundingRectangle.Y) &&
                    (item.BoundingRectangle.Y + 85 <= messageListRoot.BoundingRectangle.Y + messageListRoot.BoundingRectangle.Height))
                {
                    var result = __ProcessPopMenuCore(automation, item);
                    if (result)
                        return true;
                }
            }
            return false;
        }

        private bool __ProcessPopMenuCore(UIA3Automation automation, AutomationElement item)
        {
            var title = this._Client.ChatContent.ChatHeader.GetTitleCore(automation);
            if (!title.CanTalk())
                return false;
            var point = Point.Empty;
            if (title.HeaderType == ChatType.群聊)
            {
                //群聊
                var rect = item.BoundingRectangle;
                point = new Point(rect.X + 90 + 15, rect.Y + 35 + 22);  //中心
            }
            else
            {
                //好友或者微信好友
                var rect = item.BoundingRectangle;
                point = new Point(rect.X + 90 + 24, rect.Y + 10 + 22);  //中心
            }
            //尝试左边
            Mouse.Position = point.Confusion(5, 5);
            RandomWait.Wait(100, 300);
            SupperMouseKey.MoveTo(point.Confusion(5, 5));
            RandomWait.Wait(300, 900);
            SupperMouseKey.RightClick();
            RandomWait.Wait(300, 900);
            var path = "/Window[@Name='Weixin'][@ClassName='mmui::XMenu']";
            var menuWinRetry = Retry.WhileNull(() => this._Client.MainWindow.FindFirstByXPath(path), TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(200));
            if (menuWinRetry.Success)
            {
                var menuWin = menuWinRetry.Result.AsWindow();
                var menuItem = menuWin.FindFirstChild(cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName("多选")));
                if (menuItem != null)
                {
                    menuItem.Click();
                    RandomWait.Wait(300, 900);
                    return true;
                }
            }
            else
            {
                //如果左边不行，再尝试右边
                return __TryRightSideClick(automation, item);
            }

            return false;
        }

        private bool __TryRightSideClick(UIA3Automation automation, AutomationElement item)
        {
            var rect = item.BoundingRectangle;
            var point = new Point(rect.X + rect.Width - 90 - 24, rect.Y + 10 + 22);  //中心
            Mouse.Position = point.Confusion(5, 5);
            RandomWait.Wait(100, 300);
            SupperMouseKey.MoveTo(point.Confusion(5, 5));
            RandomWait.Wait(300, 900);
            SupperMouseKey.RightClick();
            RandomWait.Wait(300, 900);
            var path = "/Window[@Name='Weixin'][@ClassName='mmui::XMenu']";
            var menuWinRetry = Retry.WhileNull(() => this._Client.MainWindow.FindFirstByXPath(path), TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(200));
            if (menuWinRetry.Success)
            {
                var menuWin = menuWinRetry.Result.AsWindow();
                var menuItem = menuWin.FindFirstChild(cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName("多选")));
                if (menuItem != null)
                {
                    menuItem.Click();
                    RandomWait.Wait(300, 900);
                    return true;
                }
            }
            return false;
        }

        private bool ExcludeMessage(AutomationElement item)
        {
            var message = item.Name;
            if (message.Contains("邀请") && message.Contains("加入群聊"))
                return true;
            if (message.Contains("你将") && message.Contains("移出了群聊"))
                return true;
            if (message.Contains("你被") && message.Contains("移出了群聊"))
                return true;
            if (message.Contains("与群里其他人都不是朋友关系，请注意隐私安全"))
                return true;
            if (message.Contains("撤回了一条消息"))
                return true;
            string pattern = @"\s(?:[01]\d|2[0-3]):[0-5]\d$";
            if (Regex.IsMatch(message, pattern))
                return true;
            if (message.Contains("拍了拍"))
                return true;
            if (message.Contains("领取了你的红包"))
                return true;
            if (message.Contains("你领取了") && message.Contains("的红包"))
                return true;
            return false;
        }



        //仅用于测试.
        public async Task FetchFriendInfo(string who)
        {
            await this._Client.SearchFriend(who);
            RandomWait.Wait(500, 1200);
            var chatInfo = await this._Client.ChatContent.ChatHeader.GetTitle();
            await WeChatInvoker.Call(_FetchFriendInfo, CancellationToken.None, chatInfo);
        }
        /// <summary>
        /// 点击聊天窗口右上角的“聊天信息”按钮，进入好友信息界面获取好友信息，获取完后会自动关闭好友信息界面
        /// </summary>
        /// <param name="automation"></param>
        /// <param name="token"></param>
        /// <param name="headerInfo"></param>
        internal void _FetchFriendInfo(UIA3Automation automation, CancellationToken token, HeaderInfo headerInfo)
        {
            //首先判断在缓存中有没有此用户
            var result = this._Client.GetFriendFromCache(headerInfo.Title);
            token.ThrowIfCancellationRequested();
            if (result != null)
            {
                return;
            }
            //如果没有此用户，则取用户wxid
            var path = "/Group/Custom/Group/Group/Group/Custom/Custom/Custom/Group/Custom/Custom/Group/Group/Group/Group/Group/Group/Group/Group/Group/Group/Button[@Name='聊天信息'][@AutomationId='content_view.top_content_view.title_h_view.right_v_view.right_content_h_view.right_content_v_view.right_ui_.more_button']";
            var buttonRetry = Retry.WhileNull(() => this._Client.MainWindow.FindFirstByXPath(path), TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(200));
            if (!buttonRetry.Success)
                return;
            var button = buttonRetry.Result; //点击侧边栏按钮.
            var point = button.BoundingRectangle.Center();
            var point1 = point.Confusion(5, 3);
            Mouse.Position = point1;
            RandomWait.Wait(100, 300);
            point1 = point.Confusion(5, 3);
            SupperMouseKey.MoveTo(point1);
            RandomWait.Wait(200, 800);
            SupperMouseKey.LeftClick();
            //点击后找到右边侧图标
            path = "/Group/Custom/Group/Group/Group/Custom/Custom/Custom/Group/Custom/Custom/Group/Group[2]/Group[2]/Group/Group/Group/Group[1]/Button[1]";
            buttonRetry = Retry.WhileNull(() => this._Client.MainWindow.FindFirstByXPath(path), TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(200));
            if (!buttonRetry.Success)
                return;
            token.ThrowIfCancellationRequested();
            button = buttonRetry.Result;
            button = button.FindFirstChild(cf => cf.ByControlType(ControlType.Button).And(cf.ByClassName("mmui::ContactHeadView")));
            point = button.BoundingRectangle.Center();
            point1 = point.Confusion(5, 5);
            Mouse.Position = point1;
            RandomWait.Wait(100, 300);
            point1 = point.Confusion(5, 3);
            SupperMouseKey.MoveTo(point1);
            RandomWait.Wait(200, 800);
            SupperMouseKey.LeftClick();
            token.ThrowIfCancellationRequested();

            //获取信息
            FriendInfo friendInfo = GetFriendInfoCore(automation);
            if (friendInfo != null)
            {
                this._Client.AddOrUpdateFriendFromCache(friendInfo);
            }
            token.ThrowIfCancellationRequested();
            this._Client.ChatContent.Sender.FcouseSenderCore(automation);  //获取完信息后把焦点切回消息输入框
            RandomWait.Wait(100, 300);

            //再次检查一下是否关闭侧边栏，如果没有关闭，则关闭一下，避免影响下一次获取信息
            var addButtonRetry = Retry.WhileNotNull(() => this._Client.MainWindow.FindFirstByXPath("/Group/Custom/Group/Group/Group/Custom/Custom/Custom/Group/Custom/Custom/Group/Group/Group/Group/Group/Group/Group/Button[@Name='添加'][@AutomationId='single_chat_member_add'][@ClassName='mmui::ChatMemberActionView']"), TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(200));
            if (!addButtonRetry.Result)
            {
                this._Client.ChatContent.Sender.FcouseSenderCore(automation);  //获取完信息后把焦点切回消息输入框
                RandomWait.Wait(100, 300);
            }
        }

        /// <summary>
        /// 单纯获取好友信息
        /// </summary>
        /// <param name="automation"></param>
        /// <returns></returns>
        internal FriendInfo GetFriendInfoCore(UIA3Automation automation)
        {
            FriendInfo friendInfo = new FriendInfo();
            var winRetry = Retry.WhileNull(() => automation.GetDesktop().FindFirstChild(cf => cf.ByName("Weixin").And(cf.ByClassName("mmui::ProfileUniquePop").And(cf.ByProcessId(this._Client.MainWindow.Properties.ProcessId)))), TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(200));
            if (!winRetry.Success)
                return null;
            var win = winRetry.Result.AsWindow();
            var path = "/Group/Group/Group/Group/Group/Group/Group/Text[@Name='微信号：'][@AutomationId='right_v_view.user_info_center_view.basic_line_view.basic_line.key_text']";
            var textRetry = Retry.WhileNull(() => win.FindFirstByXPath(path), TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(200));
            if (!textRetry.Success)
                return null;
            //微信号
            var text = textRetry.Result.GetSibling(1);
            friendInfo.WxId = text.Name;
            //名称、昵称、备注
            path = "/Group/Group/Group/Group/Group/Group/Text[@AutomationId='right_v_view.nickname_button_view.display_name_text']";
            text = win.FindFirstByXPath(path);
            friendInfo.NickName = text.Name;
            friendInfo.MemoName = text.Name;
            //昵称
            path = "/Group/Group/Group/Group/Group/Group/Group/Text[@Name='昵称：'][@AutomationId='right_v_view.user_info_center_view.basic_line_view.basic_line.key_text']";
            text = win.FindFirstByXPath(path)?.GetSibling(1);
            if (text != null)
            {
                friendInfo.NickName = text.Name;
            }
            //地区
            path = "/Group/Group/Group/Group/Group/Group/Group/Text[@Name='地区：'][@AutomationId='right_v_view.user_info_center_view.basic_line_view.basic_line.key_text']";
            text = win.FindFirstByXPath(path)?.GetSibling(1);
            if (text != null)
            {
                friendInfo.Area = text.Name;
            }
            //共同群聊
            path = "/Group/Group/Group/Group/Group/Group/Group/Group/Group/Group/Group/Group/Group/Group/Button/Group/Group/Text[@AutomationId='content_v_view.ProfileResizeVBoxView.detail_scroll_view.gradient_mask_stacked_view.default_scroll_area.qt_scrollarea_viewport.detail_content_host.detail_center_v_view.detail_derived_content_view.section_shell.more_line_v_view.chatroom_intersection.value_normal_view.content_view.value_reader_view.value_reader_'][@ClassName='mmui::ProfileTextView']";
            text = win.FindFirstByXPath(path);
            if (text != null)
            {
                friendInfo.SameGroupNumber = text.Name;
            }
            //签名
            path = "/Group/Group/Group/Group/Group/Group/Group/Group/Group/Group/Group/Group/Group/Group/Button/Group/Group/Text[@AutomationId='content_v_view.ProfileResizeVBoxView.detail_scroll_view.gradient_mask_stacked_view.default_scroll_area.qt_scrollarea_viewport.detail_content_host.detail_center_v_view.detail_derived_content_view.section_shell.more_line_v_view.sign.value_normal_view.content_view.value_reader_view.value_reader_'][@ClassName='mmui::ProfileTextView']";
            text = win.FindFirstByXPath(path);
            if (text != null)
            {
                friendInfo.Signature = text.Name;
            }
            //来源与添加时间略掉，因为没啥意义.
            //标签
            path = "/Group/Group/Group/Group/Group/Group/Group/Group/Group/Group/Group/Group/Group/Group/Button/Group/Group/Text[@AutomationId='content_v_view.ProfileResizeVBoxView.detail_scroll_view.gradient_mask_stacked_view.default_scroll_area.qt_scrollarea_viewport.detail_content_host.detail_center_v_view.detail_derived_content_view.section_shell.main_line_v_view.tag_line.value_normal_view.content_view.value_reader_view.value_reader_'][@ClassName='mmui::ProfileTextView']";
            text = win.FindFirstByXPath(path);
            if (text != null)
            {
                string[] tags = text.Name.Split(',', StringSplitOptions.RemoveEmptyEntries);
                friendInfo.Lable = tags.ToList();
            }
            path = "/Group/Group/Group/Group/Group/Button[@AutomationId='head_image_v_view.head_view_'][@ClassName='mmui::ContactHeadView']";
            var button = win.FindFirstByXPath(path);
            if (button != null)
            {
                path = Path.Combine(AppContext.BaseDirectory, "Avator", $"{friendInfo.WxId}.png");
                button.CaptureToFile(path);
                friendInfo.AvatarPath = path;
            }
            win?.Close();
            return friendInfo;
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
                    RandomWait.Wait(300, 900);
                }
            }
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