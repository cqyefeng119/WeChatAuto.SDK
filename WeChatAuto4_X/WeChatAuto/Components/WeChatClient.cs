using FlaUI.Core.Definitions;
using FlaUI.Core.AutomationElements;
using System.Collections.Generic;
using WeAutoCommon.Utils;
using System;
using WeAutoCommon.Models;
using OneOf;
using WeChatAuto.Utils;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using WeChatAuto.Services;
using FlaUI.Core.Tools;
using WeAutoCommon.Exceptions;
using FlaUI.UIA3;
using WeAutoCommon.Simulator;
using System.Threading.Tasks;
using FlaUI.Core.Capturing;
using WeAutoCommon.Enums;
using WeChatAuto.Extentions;
using WeChatAuto.Models;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.IO;
using WeChatAuto.Options;
using RapidOCRLib;
using System.Threading.Channels;


namespace WeChatAuto.Components
{
    /// <summary>
    /// 微信客户端
    /// 适用于单个微信客户端的自动化操作
    /// </summary>
    public class WeChatClient : IDisposable
    {
        private readonly AutoLogger<WeChatClient> _logger;
        private IServiceProvider serviceProvider;
        private volatile bool _disposed = false;
        private Navigation _Navigation;
        private UIThreadInvoker _MainThreadInvoker;
        private ReaderWriterLockSlim readerWriterLockSlim = new ReaderWriterLockSlim();
        private SemaphoreSlim monitorEvent;

        #region 比较稳定的字段
        public readonly Window MainWindow;
        public readonly int ClientProcessId;
        public readonly WeChatClientFactory Factory;
        public readonly string NickName;
        public readonly string WxId;
        public readonly string AvatorPath;
        public readonly int WechatIndex;
        public UIThreadInvoker MainThreadInvoker => _MainThreadInvoker;
        #endregion

        #region 系统消息监听Channel
        private readonly Channel<(SystemMonitorOption option, List<string> messaages)> _SystemMonitorChannel = Channel.CreateBounded<(SystemMonitorOption option, List<string> messaages)>(new BoundedChannelOptions(100)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait,
        });
        public Channel<(SystemMonitorOption option, List<string> messaages)> SystemMonitorChannel => _SystemMonitorChannel;
        private int _SystemMonitorChannelStarted = 0;
        private readonly CancellationTokenSource _ActionTokenSource = new CancellationTokenSource();
        private Task _SystemMonitorTask;
        #endregion

        /// <summary>
        /// 构造器
        /// 不应该自行调用,应该通过<see cref="WeChatClientFactory.GetWeChatClient"/>方法来获取
        /// </summary>
        /// <param name="clientProcessId"></param>
        /// <param name="provider"></param>
        /// <param name="factory"></param>
        /// <param name="window"></param>
        /// <param name="uIThreadInvoker"></param>
        /// <param name="ownerInfo">个人信息</param>
        /// <param name="index">微信在任务栏的索引</param>
        /// <param name="monitorEvent">任务统一器</param>
        public WeChatClient(int clientProcessId, IServiceProvider provider, WeChatClientFactory factory,
         Window window, UIThreadInvoker uIThreadInvoker, OwerInfo ownerInfo, int index, SemaphoreSlim monitorEvent)
        {
            this.monitorEvent = monitorEvent;
            this._MainThreadInvoker = uIThreadInvoker;
            this.MainWindow = window;
            this.serviceProvider = provider;
            this.Factory = factory;
            this.ClientProcessId = clientProcessId;
            this.NickName = ownerInfo.NickName;
            this.WxId = ownerInfo.WxId;
            this.AvatorPath = ownerInfo.AvatorPath;
            this.WechatIndex = index;
            _logger = provider.GetRequiredService<AutoLogger<WeChatClient>>();
            _Initialize();
        }

        private void _Initialize()
        {
            this.Navigation.SwitchNavigationCore(null, NavigationType.微信);
            this.ToolBar = new ToolBar(this.MainWindow, this.MainThreadInvoker, serviceProvider);
            this.Conversations = new ConversationList(this, this._MainThreadInvoker, serviceProvider);
            this.ChatContent = new ChatContent(this, this._MainThreadInvoker, serviceProvider);
            this.AddressBookList = new AddressBookList(this, this._MainThreadInvoker, serviceProvider);
            this.MessageMonitor = new MessageMonitor(this, serviceProvider, _MainThreadInvoker, monitorEvent);
            this.NewFriendMonitor = new NewFriendMonitor(this, serviceProvider, _MainThreadInvoker, monitorEvent);
            this.NotifyIcon = new ShellNotifyIcon(this, serviceProvider, WechatIndex);
            this.OwnerGroup = new OwnerGroup(this, _MainThreadInvoker, serviceProvider);
            this.OuterGroup = new OuterGroup(this, _MainThreadInvoker, serviceProvider);
            this.Moments = new Moments(this, _MainThreadInvoker, serviceProvider);
            this.Search = new Search(this, _MainThreadInvoker, serviceProvider);
            this.CacheManager = new CacheManager(this);
            _RunCheckAddressBook();
        }

        internal async Task _InitializeSystemMonitorConsumption()
        {
            if (Interlocked.CompareExchange(ref _SystemMonitorChannelStarted, 1, 0) == 1)
                return;
            TaskCompletionSource tcs = new TaskCompletionSource();
            _SystemMonitorTask = Task.Run(async () =>
            {
                try
                {
                    tcs.SetResult();
                    var token = _ActionTokenSource.Token;
                    await foreach (var message in _SystemMonitorChannel.Reader.ReadAllAsync(token))
                    {
                        try
                        {
                            await monitorEvent.WaitAsync(token);
                            try
                            {
                                if (message.option.CallBack != null)
                                {
                                    await SystemMonitorConsumptionActionCore(message);
                                }
                            }
                            finally
                            {
                                monitorEvent.Release();
                            }
                        }
                        catch (OperationCanceledException)
                        {
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"系统消息消费者发生错误，错误原因:{ex.ToString()}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    tcs?.TrySetCanceled();
                }
                catch (Exception ex)
                {
                    _logger.Error($"系统消息消费者发生错误，错误原因:{ex.ToString()}");
                    tcs?.TrySetException(ex);
                }
            });
            await tcs.Task;
        }

        private async Task SystemMonitorConsumptionActionCore((SystemMonitorOption option, List<string> messaages) message)
        {
            SystemMessageContext context = new SystemMessageContext(message.messaages, this, this.serviceProvider, message.option.Who);
            await message.option.CallBack.Invoke(context);
        }

        private void _RunCheckAddressBook()
        {
            if (WeAutomation.Config.InitAdressBook)
            {
                var path = Path.Combine(AppContext.BaseDirectory, this.WxId + "_cache.dat");
                if (File.Exists(path))
                    return;
                this.GetAllFriends(false).GetAwaiter().GetResult();
            }
        }

        #region POM对象
        /// <summary>
        /// 导航栏, 参考: <see cref="Navigation"/>
        /// </summary>
        public Navigation Navigation => GetNavigation();
        /// <summary>
        /// 微信ToolBar,参考:<see cref="ToolBar"/>
        /// </summary>
        public ToolBar ToolBar;
        /// <summary>
        /// 会话管理对象,参考:<see cref="ConversationList"/>
        /// </summary>
        public ConversationList Conversations;
        /// <summary>
        /// ChatContent对象,参考:<see cref="ChatContent"/>
        /// </summary>
        public ChatContent ChatContent;
        /// <summary>
        /// 通讯录对象，参考:<see cref="AddressBookList"/>
        /// </summary>
        public AddressBookList AddressBookList;
        /// <summary>
        /// 消息监听器对象，参考:<see cref="MessageMonitor"/>
        /// </summary>
        public MessageMonitor MessageMonitor;
        /// <summary>
        /// 任务栏操作对象，参考<see cref="ShellNotifyIcon"/>
        /// </summary>
        public ShellNotifyIcon NotifyIcon;
        /// <summary>
        /// 通过新好友添加好友监听器
        /// </summary>
        public NewFriendMonitor NewFriendMonitor;

        public Search Search;

        /// <summary>
        /// 得到OCR引擎
        /// </summary>
        public OCRService OcrEngee => serviceProvider.GetRequiredService<OCRService>();
        /// <summary>
        /// 自有群
        /// </summary>
        public OwnerGroup OwnerGroup;

        /// <summary>
        /// 外部群
        /// </summary>
        public OuterGroup OuterGroup;

        /// <summary>
        /// 朋友圈
        /// </summary>
        public Moments Moments;

        /// <summary>
        /// cache管理器
        /// </summary>
        public CacheManager CacheManager;


        #endregion
        private Navigation GetNavigation()
        {
            _Navigation = new Navigation(this, this.MainThreadInvoker, this.serviceProvider);
            return _Navigation;
        }


        #region 个人信息
        /// <summary>
        /// 获取本微信的个人信息，包括头像文件位置，wxid,昵称
        /// </summary>
        /// <returns>返回<see cref="OwerInfo"/></returns>
        public OwerInfo GetOwerInfo()
        {
            return new OwerInfo
            {
                AvatorPath = this.AvatorPath,
                WxId = this.WxId,
                NickName = this.NickName,
            };
        }
        /// <summary>
        /// 保存头像至其他路径
        /// </summary>
        /// <param name="path">待保存的头像路径</param>
        /// <returns></returns>
        public async Task SaveOwnerAvator(string path) => await this.Navigation.SaveOwnerAvator(path);
        #endregion

        #region 窗口管理
        /// <summary>
        /// 最大化微信窗口
        /// </summary>
        public async Task Max() => await ToolBar.Max();
        /// <summary>
        /// 还原微信窗口
        /// </summary>
        public async Task Restore() => await ToolBar.Restore();
        /// <summary>
        /// 置顶微信窗口
        /// </summary>
        /// <returns></returns>
        public async Task Pinned() => await ToolBar.Top(true);
        /// <summary>
        /// 取消置顶微信窗口
        /// </summary>
        /// <returns></returns>
        public async Task UnPinned() => await ToolBar.Top(false);
        /// <summary>
        /// 使主窗口获取焦点
        /// </summary>
        /// <returns></returns>
        public async Task Focus()
        {
            this.MainWindow.Focus();
            await Task.CompletedTask;
        }

        /// <summary>
        /// 关闭查询窗口,如果查询窗口打开则关闭，如果查询窗口没有打开，则不作动作
        /// </summary>
        /// <param name="who">关闭谁的查询窗口</param>
        /// <returns></returns>
        public async Task CloseSearchWindow(string who) => await this.ChatContent.CloseSearchWindow(who);

        /// <summary>
        /// 移动窗口至主窗口的中间
        /// </summary>
        /// <param name="window"></param>
        public void MoveWinToMainCenter(Window window)
        {
            if (window == null)
                return;
            window.Focus();
            RandomWait.Wait(100, 600);
            window.Move(
                this.MainWindow.BoundingRectangle.X + (int)((this.MainWindow.BoundingRectangle.Width - window.BoundingRectangle.Width) / 2),
                this.MainWindow.BoundingRectangle.Y + (int)((this.MainWindow.BoundingRectangle.Height - window.BoundingRectangle.Height) / 2)
            );
            RandomWait.Wait(300, 900);
        }

        /// <summary>
        /// 打开who指定的子窗口
        /// </summary>
        /// <param name="who"></param>
        /// <returns></returns>
        public async Task<Window> OpenSubWin(string who) => await this.Conversations.OpenSubWin(who);
        /// <summary>
        /// 得到本微信窗口句柄
        /// </summary>
        /// <returns></returns>
        public nint GetHandler() => this.MainWindow.Properties.NativeWindowHandle.Value;
        /// <summary>
        /// 得到本微信窗口的进程id
        /// </summary>
        /// <returns></returns>
        public nint GetProcessId() => this.MainWindow.Properties.ProcessId.Value;

        #endregion

        #region Navigator管理
        /// <summary>
        /// 切换导航栏
        /// </summary>
        /// <param name="navigationType">导航栏类型,请参见枚举类型<seealso cref="NavigationType"/></param>
        public async Task SwitchNavigation(NavigationType navigationType) => await this.Navigation.SwitchNavigation(navigationType);

        /// <summary>
        /// 关闭通过导航栏打开的窗口.
        /// 仅支持聊天文件、朋友圈、视频号、看一看、搜一搜、小程序面板等窗口
        /// </summary>
        /// <param name="navigationType">导航栏类型,请参见枚举类型<seealso cref="NavigationType"/></param>
        public async Task CloseNavWin(NavigationType navigationType) => await this.Navigation.CloseNavWin(navigationType);
        /// <summary>
        /// 点击任务栏微信图标
        /// </summary>
        /// <param name="index">图标索引，从1开始,索引范围不能越界</param>
        /// <returns></returns>
        public async Task ClickNotifyIcon(int index) => (await this.NotifyIcon.GetButtons())[index - 1].Click();
        /// <summary>
        /// 点击指定微信名称的任务栏图标
        /// </summary>
        /// <param name="WechatName">微信名称</param>
        /// <returns></returns>
        public async Task ClickNotifyIcon(string WechatName)
        {
            var client = this.Factory.GetWeChatClient(WechatName);
            var button = await client.NotifyIcon.GetButton();
            button.Click();
        }
        #endregion

        #region 会话管理
        /// <summary>
        /// 获取会话列表所有会话的标题
        /// 考虑到效率，只返回名称列表
        /// </summary>
        /// <returns>所有会话列表的标题</returns>
        public async Task<List<string>> GetAllConversations() => await this.Conversations.GetAllConversations();

        /// <summary>
        /// 获取会话列表可见会话标题
        /// </summary>
        /// <returns>可见的会话列表的标题列表</returns>
        public async Task<List<string>> GetVisibleConversationTitles() => await this.Conversations.GetVisibleConversationTitles();

        /// <summary>
        /// 获取可见会话列表
        /// 会话信息包含：会话名称、会话未读消息数、会话头像等具体信息，请参考<see cref="SimpleConversation"/>
        /// </summary>
        /// <returns>返回<see cref="SimpleConversation"/>列表</returns>
        public async Task<List<SimpleConversation>> GetVisibleConversations() => await this.Conversations.GetVisibleConversations();

        /// <summary>
        /// 搜索好友/群聊
        /// </summary>
        /// <param name="who">待搜索的好友/群聊昵称,who - 微信会话列表肉眼可见的名称,如果群有备注，则这个who即为备注名</param>
        /// <returns>如果找到，返回true,如果没有找到，则返回false.</returns>
        public async Task<bool> SearchFriend(string who) => await this.Conversations.Search(who);

        /// <summary>
        /// 定位会话
        /// 定位会话的用途：可以将会话列表滚动到指定会话的位置，使指定会话可见
        /// </summary>
        /// <param name="title">会话标题</param>
        /// <returns>如果找到会话，则返回true，否则返回false</returns>
        public async Task<bool> LocateConversation(string title) => await this.Conversations.LocateConversation(title);

        /// <summary>
        /// 会话列表向上滚动，并且执行需要的业务逻辑
        /// </summary>
        /// <param name="callBack">
        /// <para>滚动过程中的回调，建议：如果处理业务结束，返回false,意味着不向上滚动，如果业务没有处理到，则返回true,继续滚动</para>
        /// <para>参数中的Rectangle为会话列表容器的<see cref="Rectangle"/>,如果超出容器Rectangle范围，应该返回true继续滚动，直到可以点击为止</para>
        /// </param>
        /// <returns></returns>
        public async Task Up(Func<AutomationElement[], Rectangle, bool> callBack) => await this.Conversations.Up(callBack);

        /// <summary>
        /// 会话列表滚动向下滚动，并且执行需要的业务逻辑
        /// <param name="callBack">
        /// <para>滚动过程中的回调，建议：如果处理业务结束，返回false,意味着不向上滚动，如果业务没有处理到，则返回true,继续滚动</para>
        /// <para>参数中的Rectangle为会话列表容器的<see cref="Rectangle"/>,如果超出容器Rectangle范围，应该返回true继续滚动，直到可以点击为止</para>
        /// </param>
        /// </summary>
        /// <returns></returns>
        public async Task Down(Func<AutomationElement[], Rectangle, bool> callBack) => await this.Conversations.Down(callBack);

        /// <summary>
        /// 设置会话消息免打扰
        /// </summary>
        /// <param name="setting">如果为:true,则设置会话消息免打扰，如果为:false,则：允许消息通知</param>
        /// <param name="who">要设置的 好友/群聊 名称,可以为空,如果为空，则为当前窗口设置免打扰</param>
        /// <returns>执行消息免打扰结果</returns>
        public async Task<bool> SetDoNotDisturb(string who, bool setting = true) => await this.Conversations.SetDoNotDisturb(who, setting);
        /// <summary>
        /// 设置会话置顶
        /// </summary>
        /// <param name="setting">true:聊天置顶;false:取消聊天置顶</param>
        /// <param name="who">要设置的 好友/群聊 名称,可以为空,如果为空，则为当前窗口设置置顶</param>
        /// <returns>执行会话置顶结果</returns>
        public async Task<bool> SetTopMost(string who, bool setting = true) => await this.Conversations.SetTopMost(who, setting);

        #endregion

        #region 消息管理
        /// <summary>
        /// 获取当前窗口的标题对象
        /// </summary>
        /// <returns>标题对象，请参考:<see cref="HeaderInfo"/></returns>
        public async Task<HeaderInfo> GetTitle() => await this.ChatContent.ChatHeader.GetTitle();

        /// <summary>
        /// 当前窗口的Sender输入区域点击，以获得焦点，也可以取消系统的消息提醒或者关闭右侧Pane等作用
        /// </summary>
        /// <returns></returns>
        public async Task FocuseSenderInput() => await this.ChatContent.FocuseSenderInput();

        /// <summary>
        /// 获取当前标窗的标题
        /// </summary>
        /// <returns>当前窗口的标题名称</returns>
        public async Task<string> GetOnlyTitle() => await this.ChatContent.ChatHeader.GetOnlyTitle();
        /// <summary>
        /// 发送文本消息,可以是群聊名称或者好友名称，名称可以为空，如果为空，则给当前聊天窗口发送消息
        /// </summary>
        /// <param name="who">好名/群聊的名称,也就是肉眼所见的标题</param>
        /// <param name="message">消息内容，文本消息内容</param>
        /// <param name="atUser">被@的好友,可以多个</param>
        /// <param name="refer">引用的对话内容,请参考<see cref="ChatRefer"/></param>
        public async Task SendMessage(string who, string message, OneOf<string, string[], List<string>> atUser = default, ChatRefer refer = null)
            => await ChatContent.SendMessage(who, message, atUser, refer);
        /// <summary>
        /// <para>发送消息,给当前窗口发送消息</para>
        /// <para>注意：仅给当前窗口发送消息，要注意当前聊天窗口可用</para>
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="atUser">被@的好友</param>
        /// <param name="refer">引用的对话内容,请参考<see cref="ChatRefer"/></param>
        public async Task SendMessage(string message, OneOf<string, string[], List<string>> atUser = default, ChatRefer refer = null)
            => await ChatContent.SendMessage(message, atUser, refer);

        /// <summary>
        /// 发送文件
        /// </summary>
        /// <param name="who">好友/群聊，可以为空,如果为空，则发送到当前聊天窗口</param>
        /// <param name="files">文件路径列表</param>
        public async Task SendFile(string who, string[] files) => await ChatContent.SendFile(who, files);

        /// <summary>
        /// 发送表情    
        /// </summary>  
        /// <param name="who">被发送消息的好友名称/群聊名称</param>
        /// <param name="emoji">表情名称或者描述或者索引,具体请参见<see cref="EmojiListHelper"/></param>
        /// <param name="atUserList">被@的好友列表</param>
        public async Task SendEmoji(string who, OneOf<int, string> emoji, List<string> atUserList = null) => await ChatContent.SendEmoji(who, emoji, atUserList);

        /// <summary>
        /// 发起单人语音聊天
        /// </summary>
        /// <param name="who">好友昵称,可以为空，如果为空，则发送到当前聊天窗口</param>
        public async Task SendVoiceChat(string who) => await ChatContent.SendVoiceChat(who);

        /// <summary>
        /// 发起单人视频聊天
        /// </summary>
        /// <param name="who">好友昵称,可以为空，如果为空，则发送到当前聊天窗口</param>
        public async Task SendVedioChat(string who) => await ChatContent.SendVedioChat(who);
        /// <summary>
        /// 发起多人语音聊天，适用于群聊发起语音聊天
        /// </summary>
        /// <param name="who">群聊名称,可以为空，如果为空，则发送到当前聊天窗口</param>
        /// <param name="partner">参与者，好友昵称列表,必须是群聊成员</param>
        public async Task SendVoiceChats(string who, string[] partner) => await ChatContent.SendVoiceChats(who, partner);
        /// <summary>
        /// 发送语音消息,此功能依赖虚拟声卡：Cable input/Cable output
        /// 请在声音-->设置-->将输入设备改成: Cable output
        /// 如果没有安装虚拟声卡，请在:https://github.com/alexzhao189/wechatautosdk/blob/main/Resources/VBCABLE_Driver_Pack45.zip下载
        /// </summary>
        /// <param name="who">好友昵称或群聊名称</param>
        /// <param name="filePath">语音文件路径</param>
        public async Task SendVoiceMessage(string who, string filePath) => await ChatContent.SendVoiceMessage(who, filePath);
        /// <summary>
        /// 给本聊天窗口发送语音消息，请确保本聊天窗口可用.
        /// 请在声音-->设置-->将输入设备改成: Cable output
        /// 如果没有安装虚拟声卡，请在:https://github.com/alexzhao189/wechatautosdk/blob/main/Resources/VBCABLE_Driver_Pack45.zip下载
        /// </summary>
        /// <param name="filePath">语音文件路径</param>
        /// <returns></returns>
        public async Task SendVoiceMessage(string filePath) => await ChatContent.SendVoiceMessage(filePath);
        /// <summary>
        /// 根据日期获取当前聊天窗口的聊天历史
        /// </summary>
        /// <param name="date">查询日期,如果为空，则为当天日期</param>
        /// <returns>返回<see cref="ChatSimpleMessage"/>列表</returns>
        public async Task<List<ChatSimpleMessage>> GetChatHistory(DateTime date = default) => await ChatContent.GetChatHistory(date);

        /// <summary>
        /// 根据日期获取聊天历史
        /// </summary>
        /// <param name="who">微信名称，可以是好友/群聊的微信名称,可以为空，如果为空，则获取当前聊天窗口的历史记录</param>
        /// <param name="date">查询日期,如果不传，则是当天日期</param>
        /// <returns>返回<see cref="ChatSimpleMessage"/>列表</returns>
        public async Task<List<ChatSimpleMessage>> GetChatHistory(string who, DateTime date = default) => await ChatContent.GetChatHistory(who, date);

        /// <summary>
        /// 获取一段时间的(开始时间与结束时间)聊天历史记录
        /// 适用于历史消息比较多，分段获取的场景
        /// </summary>
        /// <param name="who">微信名称，可以是好友/群聊的微信名称</param>
        /// <param name="startDate">开始日期,支持时、分、秒</param>
        /// <param name="endDate">结束日期，支持时、分、秒</param>
        /// <returns></returns>
        public async Task<List<ChatSimpleMessage>> GetChatHistory(string who, DateTime startDate, DateTime endDate) => await ChatContent.GetChatHistory(who, startDate, endDate);

        /// <summary>
        /// 拍一拍
        /// 注意：只能拍一拍当前聊天窗口的好友,一般结合消息监听或者<seealso cref="SearchFriend"/>使用.
        /// 只有两个地方可以拍一拍：一个是群聊中，一个是好友聊天窗口（非企业微信,企业微信聊天不能拍一拍).
        /// </summary>
        /// <param name="who">要拍一拍的好友昵称</param>
        /// <param name="prevScrollNumber">如果当前页找不到，往前滚动的次数</param>
        /// <returns>是否成功拍一拍</returns>
        public async Task<bool> TapWho(string who, int prevScrollNumber = 30) => await this.ChatContent.MessageBubbleList.TapWho(who, prevScrollNumber);

        /// <summary>
        /// 引用消息
        /// 注意：引用消息只能是当前窗口的好友消息，一般结合消息监听或者<seealso cref="SearchFriend"/>使用.
        /// </summary>
        /// <param name="chatSimpleMessage">要引用的消息<see cref="ChatSimpleMessage"/></param>
        /// <param name="prevScrollNumber">如果当前页找不到，往前滚动的次数</param>
        public async Task<bool> ReferencedMessage(ChatSimpleMessage chatSimpleMessage, int prevScrollNumber = 30) => await this.ChatContent.MessageBubbleList.ReferencedMessage(chatSimpleMessage, prevScrollNumber);

        /// <summary>
        /// 引用消息
        /// 注意：引用消息只能是当前窗口的好友消息，一般结合消息监听或者<seealso cref="SearchFriend"/>使用.        
        /// </summary>
        /// <param name="who">要引用的好友昵称</param>
        /// <param name="message">要引用的消息内容</param>
        /// <param name="prevScrollNumber">如果当前页找不到，往前滚动的次数</param>
        public async Task<bool> ReferencedMessage(string who, string message, int prevScrollNumber = 30) => await this.ChatContent.MessageBubbleList.ReferencedMessage(who, message, prevScrollNumber);

        /// <summary>
        /// 引用最后一条消息
        /// 注意：引用消息只能是当前窗口的好友消息，一般结合消息监听或者<seealso cref="SearchFriend"/>使用.    
        /// 注意，只能引用有的消息，不会翻页，如果消息不在当前页，则不会引用
        /// </summary>
        public async Task<bool> ReferencedLastMessage() => await this.ChatContent.MessageBubbleList.ReferencedLastMessage();

        /// <summary>
        /// 转发多条消息,默认转发最后5条消息，可以自行指定转发多少条消息
        /// </summary>
        /// <param name="who">被转发消息的好友/群聊,可以为空，则转发本窗口的消息</param>
        /// <param name="to">要转发给谁</param>
        /// <param name="fType">消息转发类型，详情请参见<see cref="ForwardMessageTypeEnums"/></param>
        /// <param name="rowCount">要转发多少条消息，默认是最后的5条消息,如果当前没有5条，则转发所有消息</param>
        public async Task<bool> ForwardMultipleMessage(string who, OneOf<string, string[]> to, ForwardMessageTypeEnums fType = ForwardMessageTypeEnums.ForwardMerge, int rowCount = 5) => await this.ChatContent.MessageBubbleList.ForwardMultipleMessage(who, to, fType, rowCount);


        /// <summary>
        /// 转发单条消息
        /// 流程：
        /// 1. 找到这一条消息,倒序找，这里注意一点，如果找不到消息，自动往前滚动，如果找不到，则不会转发此消息,日志显示错误，但不会报错.
        /// 2. 右键点击这一条消息
        /// 3. 找到菜单
        /// 4. 找到发送人
        /// </summary>
        /// <param name="to">要转发给谁,可以多人/群</param>
        /// <param name="chatSimpleMessage">要转发的消息<see cref="ChatSimpleMessage"/></param>
        /// <param name="prevScrollNumber">如果当前页找不到，往前翻页的次数</param>
        public async Task<bool> ForwardSingleMessage(ChatSimpleMessage chatSimpleMessage, OneOf<string, string[]> to, int prevScrollNumber = 30) => await this.ChatContent.MessageBubbleList.ForwardSingleMessage(chatSimpleMessage, to, prevScrollNumber);

        /// <summary>
        /// 转发单条消息
        /// </summary>
        /// <param name="who">要转发的好友昵称</param>
        /// <param name="message">要转发的消息内容</param>
        /// <param name="to">要转发给谁</param>
        /// <param name="prevScrollNumber">如果当前页找不到，往前滚动的次数</param>
        public async Task<bool> ForwardSingleMessage(string who, string message, OneOf<string, string[]> to, int prevScrollNumber = 30) => await this.ChatContent.MessageBubbleList.ForwardSingleMessage(who, message, to, prevScrollNumber);

        #endregion

        #region  监听管理
        /// <summary>
        /// <para>添加系统消息监听，以实现如： 检测到群主邀请好友后发送欢迎消息等功能</para>
        /// <para>注意：仅适用于群聊，不适用个人,个人请使用下面的开放式/固定式监听，另外，不支持注册监听后再新增待监听的群聊</para>
        /// 多线程监听变化，但是操作等，还得在微信单线程中执行.
        /// </summary>
        /// <param name="nickNames">群聊昵称，可以多个</param>
        /// <param name="callBack">回调函数,由用户提供,参数：消息上下文<see cref="SystemMessageContext"/></param>
        /// <param name="userToken">取消令牌,请参考<see cref="CancellationToken"/>,可以自行取消消息监听</param>
        public async Task AddGroupSystemMessageListener(OneOf<string, List<string>, string[]> nickNames, Func<SystemMessageContext, Task> callBack, CancellationToken userToken = default) => await this.MessageMonitor.AddGroupSystemMessageListener(nickNames, callBack, userToken);
        /// <summary>
        /// 添加消息监听，用户需要提供一个回调函数，当有消息时，会调用此回调函数
        /// 参考<see cref="MessageContext"/>
        /// 消息监听最主要以事件触发的方式，当消息过来的时候，监听才会运行.
        /// <para>使用规则：</para>
        /// <para>1. 仅能监听“允许消息通知”的好友/群聊,所以需要监听的好友/群聊不要设置为“消息免打扰”;</para>
        /// <para>2. 如果要避免太多消息影响，请将不监听的好友/群设置为“消息免打扰”，以提高监听性能;</para>
        /// <para>3. 此方法可以多次添加，第一次添加时会启动消息监听，第二次（类似的:第三次等）添加效果等同<see cref="AddListeningFriend"/>方法</para>
        /// <para>4. 为了减少会话窗口的滚动，建议不要添加太多消息“置顶”，以提高监听效率.</para>
        /// <para>执行逻辑:</para>
        /// <para>1. 启动消息监听器时，SDK会将会话列表整个循环一遍，会自动点击并回调用户设定的方法，以防止遗漏消息</para>
        /// <para>2. 以后的监听过程会增量监听，以提高效率.</para>
        /// </summary>
        /// <param name="nickNames">好友昵称,可以是一个，也可以是多个好友/群聊 </param>
        /// <param name="callBack">回调函数,由用户提供,参数：消息上下文<see cref="MessageContext"/></param>
        /// <param name="IsOpenMonitor">是否开启开放式监听，默认不开放(值为false）,如果开启开放式监听，前面的nickNames可以为空，所谓的开放式监听的含义是：无须固定好友/群监听，只要此好友/群没有设置“消息免打挠”就可以监听</param>
        /// <param name="userToken">取消令牌,请参考<see cref="CancellationToken"/>,可以自行取消消息监听</param>
        /// <param name="UIInvoker">UI的调度器，适用于把微信嵌入UI的场景使用，如：多微信切换Tab页等,SDK会给调用者注入一个微信名称</param>
        /// <param name="options">监听选项，具体请参见<see cref="MessageMonitorOptions"/></param>
        public async Task AddMessageListener(OneOf<string, List<string>, string[]> nickNames, Action<MessageContext> callBack, bool IsOpenMonitor = false, CancellationToken userToken = default, Action<string> UIInvoker = null, MessageMonitorOptions options = null)
            => await this.MessageMonitor.AddMessageListener(nickNames, callBack, IsOpenMonitor, userToken, UIInvoker, options);
        /// <summary>
        /// 添加一个从什么时候开始，什么时候结束的消息监听，用户需要提供一个回调函数，当有消息时，会调用此回调函数
        /// 参考<see cref="MessageContext"/>
        /// 适用于固定时间自动化的场景
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
          => await this.MessageMonitor.AddMessageListener(nickNames, callBack, startTime, endTime, IsOpenMonitor, userToken, UIInvoker, options);
        /// <summary>
        /// 添加一天中多个时间段的消息监听，用户需要提供一个回调函数，当有消息时，会调用此回调函数
        /// 参考<see cref="MessageContext"/>
        /// 适用于一天内多次固定时间进行自动化操作的场景
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
        /// <param name="range">一天中的多个时间段,如果设定多个时间段，监听器在这些时间段内开始/结束监听,时间段类请参考:<see cref="TimeOnlyRange"/>,另注意：可以跨天，如设置为:23:00 ~ 02:00,则表示当天23:00至明天02:00</param>
        /// <param name="IsOpenMonitor">是否开启开放式监听，默认不开放(值为false）,如果开启开放式监听，前面的nickNames可以为空，所谓的开放式监听的含义是：无须固定好友/群监听，只要此好友/群没有设置“消息免打挠”就可以监听</param>
        /// <param name="userToken">取消令牌,请参考<see cref="CancellationToken"/>,可以自行取消消息监听</param>
        /// <param name="UIInvoker">UI的调度器，适用于把微信嵌入UI的场景使用，如：多微信切换Tab页等,SDK会给调用者注入一个微信名称</param>
        /// <param name="options">监听选项，具体请参见<see cref="MessageMonitorOptions"/></param>
        public async Task AddMessageListener(OneOf<string, List<string>, string[]> nickNames, Action<MessageContext> callBack, List<TimeOnlyRange> range, bool IsOpenMonitor = false, CancellationToken userToken = default, Action<string> UIInvoker = null, MessageMonitorOptions options = null)
            => await this.MessageMonitor.AddMessageListener(nickNames, callBack, range, IsOpenMonitor, userToken, UIInvoker, options);
        /// <summary>
        /// 暂停消息监听
        /// </summary>
        /// <returns></returns>
        public async Task PauseMessageListener() => await this.MessageMonitor.PauseMessageListener();
        /// <summary>
        /// 恢复消息监听
        /// </summary>
        /// <returns></returns>
        public async Task ResumeMessageListener() => await this.MessageMonitor.ResumeMessageListener();
        /// <summary>
        /// 监听过程中添加好友
        /// </summary>
        /// <param name="who">好友名称</param>
        /// <returns></returns>
        public async Task AddListeningFriend(string who) => await this.MessageMonitor.AddListeningFriend(who);
        /// <summary>
        /// 监听过程中移除被监听中的好友/群聊
        /// </summary>
        /// <param name="who"></param>
        /// <returns></returns>
        public async Task RemoveListeningFriend(string who) => await this.MessageMonitor.RemoveListeningFriend(who);

        /// <summary>
        /// <para>新好友添加监听</para>
        /// <para>实现的功能</para>
        /// <para>1. 通过好友申请</para>
        /// <para>2. 根据设定的关键词过滤好友申请的打招呼文本，只有包含关键词的打招呼内容才会被通过</para>
        /// <para>3. 通过好友申请时，可以设置后缀,以区分不同类型的好友,方便后续的自动化实现</para>
        /// <para>4. 通过好友申请时，可以设置特定的微信标签，以方便后续的自动化与好友管理</para>
        /// <para>5. 也可以通过好友申请后，删除申请记录</para>
        /// </summary>
        /// <param name="options">配置选项，请参考<see cref="FriendRequestAutoAcceptOptions"/>类</param>
        /// <param name="token">取消令版</param>
        /// <param name="UIInvoker">UI线程调度器,适用于把微信嵌入UI的场景使用，如：多微信切换Tab页等,SDK会给调用者注入一个微信名称</param>
        /// <returns></returns>
        public async Task AddFriendRequestAutoAcceptListener(FriendRequestAutoAcceptOptions options, CancellationToken token = default, Action<string> UIInvoker = null)
          => await this.NewFriendMonitor.AddFriendRequestAutoAcceptListener(options, token, UIInvoker);
        /// <summary>
        /// 暂停好友申请监听
        /// </summary>
        /// <returns></returns>
        public async Task PauseNewFriendListener() => await this.NewFriendMonitor.PauseNewFriendListener();
        /// <summary>
        /// 恢复好友申请监听
        /// </summary>
        /// <returns></returns>
        public async Task ResumeNewFriendListener() => await this.NewFriendMonitor.ResumeNewFriendListener();
        #endregion

        #region 好友/群聊管理
        /// <summary>
        /// 打开新增朋友窗口
        /// </summary>
        /// <returns></returns>
        public async Task<Window> OpenAddFriensWin() => await this.Search.OpenAddFriensWin();
        /// <summary>
        /// 关闭新增朋友窗口
        /// </summary>
        /// <returns></returns>
        public async Task CloseAddFriendWin() => await this.Search.CloseAddFriendWin();
        /// <summary>
        /// 通过手机号码、微信号查找并添加好友
        /// </summary>
        /// <param name="friends">手机号码或者微信号列表</param>
        /// <param name="options">增加朋友选项，具体请参考<see cref="AddFriendsOptions"/></param>
        /// <param name="token">取消令版</param>
        /// <returns>添加好友结果列表，详情请参见<see cref="FriendAddResult"/></returns>
        public async Task<IDictionary<string, FriendAddResult>> AddFriends(OneOf<string, string[]> friends, AddFriendsOptions options = null, CancellationToken token = default)
            => await this.Search.AddFriends(friends, options, token);
        #region 缓存中好友信息管理
        /// <summary>
        /// 显示缓存中存储的好友信息.
        /// </summary>
        /// <returns>好友信息列表，请参考<see cref="FriendInfo"/></returns>
        public List<FriendInfo> GetFriendListFromCache() => CacheManager.GetFriendListFromCache();

        /// <summary>
        /// 显示缓存中存储的好友信息,异步方法
        /// </summary>
        /// <returns>好友信息列表，请参考<see cref="FriendInfo"/></returns>
        public async Task<List<FriendInfo>> GetFriendListFromCacheAsync() => await CacheManager.GetFriendListFromCacheAsync();

        /// <summary>
        /// 从缓存中得到一个好友的信息
        /// </summary>
        /// <param name="who">好友名称</param>
        /// <returns>好友对象，请参考:<see cref="FriendInfo"/></returns>
        public FriendInfo GetFriendFromCache(string who) => CacheManager.GetFriendFromCache(who);

        /// <summary>
        /// 从缓存中得到一个好友的信息,因为名字可能重复，而wxid永远不重复
        /// </summary>
        /// <param name="wxid">微信号</param>
        /// <returns>好友对象，请参考:<see cref="FriendInfo"/></returns>
        public FriendInfo GetFriendWithWxIDFromCache(string wxid) => CacheManager.GetFriendWithWxIDFromCache(wxid);

        /// <summary>
        /// 从缓存中得到一个好友的信息,异步方法
        /// </summary>
        /// <param name="who">好友名称</param>
        /// <returns>好友对象，请参考:<see cref="FriendInfo"/></returns>
        public async Task<FriendInfo> GetFriendFromCacheAsync(string who) => await CacheManager.GetFriendFromCacheAsync(who);

        /// <summary>
        /// 从缓存中得到一个好友的信息,通过wxid来获取，因为名字可能重复,而微信id号永不重复
        /// </summary>
        /// <param name="wxid">微信号</param>
        /// <returns>好友对象，请参考:<see cref="FriendInfo"/></returns>
        public async Task<FriendInfo> GetFriendWithWxIDFromCacheAsync(string wxid) => await CacheManager.GetFriendWithWxIDFromCacheAsync(wxid);

        /// <summary>
        /// 从缓存中移除一个好友
        /// </summary>
        /// <param name="who"></param>
        public void RemoveFriendFromCache(string who) => CacheManager.RemoveFriendFromCache(who);
        /// <summary>
        /// 从缓存中移除一个好友,异步方法
        /// </summary>
        /// <param name="who"></param>
        public async Task RemoveFriendFromCacheAsync(string who) => await CacheManager.RemoveFriendFromCacheAsync(who);

        /// <summary>
        /// 从缓存中移除一个好友,通过微信id，因为通过微信名可能会重复
        /// </summary>
        /// <param name="wxid">微信号</param>
        public void RemoveFriendWithWxIDFromCache(string wxid) => CacheManager.RemoveFriendWithWxIDFromCache(wxid);

        /// <summary>
        /// 从缓存中移除一个好友,通过wxid,异步方法
        /// </summary>
        /// <param name="wxid">微信号</param>
        public async Task RemoveFriendWithWxIDFromCacheAsync(string wxid) => await CacheManager.RemoveFriendWithWxIDFromCacheAsync(wxid);

        /// <summary>
        /// 手动增加或者修改一个好友对象
        /// </summary>
        /// <param name="friend">好友对象，请参考<see cref="FriendInfo"/></param>
        public void AddOrUpdateFriendFromCache(FriendInfo friend) => CacheManager.AddOrUpdateFriendFromCache(friend);

        /// <summary>
        /// 手动增加或者修改一个好友对象，使用异步方法
        /// </summary>
        /// <param name="friend">好友对象，请参考<see cref="FriendInfo"/></param>
        public async Task AddOrUpdateFriendFromCacheAsync(FriendInfo friend) => await CacheManager.AddOrUpdateFriendFromCacheAsync(friend);

        #endregion
        /// <summary>
        /// 是否是自有群
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <returns>是否是自有群</returns>
        public async Task<bool> IsOwnerChatGroup(string groupName) => await OwnerGroup.IsOwnerChatGroup(groupName);

        /// <summary>
        /// 获取群主
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <returns>群主昵称</returns>
        public async Task<string> GetGroupOwner(string groupName) => await OwnerGroup.GetGroupOwner(groupName);

        /// <summary>
        /// 添加群聊成员，适用于自有群
        /// </summary>
        /// <param name="groupName">群聊名称,可以为空，则在焦点聊天群聊中添加群聊成员</param>
        /// <param name="memberName">成员名称</param>
        /// <returns>微信响应结果<see cref="ChatResponse"/></returns>
        public async Task AddOwnerChatGroupMember(string groupName, OneOf<string, string[]> memberName) => await OwnerGroup.AddOwnerChatGroupMember(groupName, memberName);

        /// <summary>
        /// 创建群聊
        /// 如果存在，则打开群聊，否则创建一个新群聊
        /// </summary>
        /// <param name="groupName">群聊名称,不能与之前的群聊名称重复</param>
        /// <param name="firstWho">首个成员名称，必须是好友，不能是群聊名称，用来创建群聊定位,可以为空，如果为空，则以当前聊天的好友为基准创建群聊</param>
        /// <param name="memberName">成员名称,成员数量要大于0</param>
        /// <returns>是否创建成功,如果创建失败，则显示原因,具体请参考<see cref="Result"/></returns>
        public async Task<Result> CreateOwnerChatGroup(string groupName, string firstWho, string[] memberName) => await this.OwnerGroup.CreateOwnerChatGroup(groupName, firstWho, memberName);

        /// <summary>
        /// 修改群名，适用于自有群群名修改
        /// </summary>
        /// <param name="oldGroupName">旧群名称</param>
        /// <param name="newGroupName">新群名称</param>
        /// <returns>微信响应结果</returns>
        public async Task<Result> ChangeOwnerChatGroupName(string oldGroupName, string newGroupName) => await this.OwnerGroup.ChangeOwnerChatGroupName(oldGroupName, newGroupName);

        /// <summary>
        /// 修改自己在群中的昵称
        /// </summary>
        /// <param name="groupName">群名,可以为空，如果为空，则修改焦点群聊的自己在群中的昵称</param>
        /// <param name="nickName">昵称，如果为空，则删除自己在本群中的昵称</param>
        /// <returns>微信响应结果<see cref="Result"/></returns>
        public async Task<Result> ChangeChatGroupNickName(string groupName, string nickName) => await this.OwnerGroup.ChangeChatGroupNickName(groupName, nickName);


        /// <summary>
        /// 改变群备注,群备注仅自己可见.
        /// </summary>
        /// <param name="groupName">群聊名称,可以为空，如果为空，则改变焦点聊天群的备注</param>
        /// <param name="newMemo">新备注，可以为空，如果为空，则删除本群备注</param>
        /// <returns>微信响应结果<see cref="Result"/></returns>
        public async Task<Result> ChangeChatGroupMemo(string groupName, string newMemo) => await this.OwnerGroup.ChangeChatGroupMemo(groupName, newMemo);

        /// <summary>
        /// 更新群聊公告,仅适用于自有群
        /// </summary>
        /// <param name="groupName">群聊名称，可以为空字符串，如果为空，则更新焦点聊天群聊窗口的公告</param>
        /// <param name="groupNotice">群聊公告</param>
        /// <returns>微信操作响应结果<see cref="ChatResponse"/></returns>
        public async Task<Result> UpdateGroupNotice(string groupName, string groupNotice) => await this.OwnerGroup.UpdateGroupNotice(groupName, groupNotice);


        /// <summary>
        /// 获取群聊成员列表
        /// </summary>
        /// <param name="groupName">群聊名称,可以为空，如果为空，则获取的是焦点聊天群聊的成员列表</param>
        /// <returns>群聊成员列表</returns>
        public async Task<List<string>> GetChatGroupMemberList(string groupName) => await this.OwnerGroup.GetChatGroupMemberList(groupName);

        /// <summary>
        /// 移除群聊成员,适用于自有群
        /// </summary>
        /// <param name="groupName">群聊名称,可以为空，如果为空，则从焦点聊天群聊中移除好友</param>
        /// <param name="memberName">成员名称</param>
        /// <returns>微信响应结果<see cref="Result"/></returns>
        public async Task<Result> RemoveOwnerChatGroupMember(string groupName, OneOf<string, string[]> memberName) => await this.OwnerGroup.RemoveOwnerChatGroupMember(groupName, memberName);

        /// <summary>
        /// 退出群聊
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <param name="clearHistory">是否清除历史消</param>
        public async Task QuitChatGroup(string groupName, bool clearHistory = true) => await this.OwnerGroup.QuitChatGroup(groupName, clearHistory);

        /// <summary>
        /// 邀请群聊成员,适用于外部群
        /// </summary>
        /// <param name="groupName">群聊名称,可以为空，如果为空，则在本焦点群聊窗口邀请好友</param>
        /// <param name="members">被邀请的成员名称列表,要求在自己的通讯录中</param>
        /// <param name="inviteReasonIfNeed">邀请原因，只在群管理员开启了 进群需要群主或者管理员确认 功能时有效，可以为空</param>
        /// <returns>微信响应结果<see cref="Result"/></returns>
        public async Task<Result> InviteChatGroupMember(string groupName, List<string> members, string inviteReasonIfNeed = "") => await this.OuterGroup.InviteChatGroupMember(groupName, members, inviteReasonIfNeed);

        /// <summary>
        /// 添加群聊里面的好友为自己的好友,适用于从外部群中添加好友为自己的好友
        /// 此操作为微信严风控操作，因为微信对于一天加好友应该有数量限定，建议分批次加，一次不要超过20-30个，时间延长为4小时或者一天后
        /// </summary>
        /// <param name="groupName">群聊名称,可以为空，如果为空，则在本焦点群聊窗口邀请好友</param>
        /// <param name="memberName">成员名称列表,考虑风控,建议先运行<see cref="Group.GetChatGroupMemberList(string)"/>获取群聊的成员列表，然后分批增加</param>
        /// <param name="options">好友选项，可以增加好友时设置备注后缀、打招呼内容及标签等，方便分类管理</param>
        /// <returns></returns>
        public async Task<IDictionary<string, FriendAddResult>> AddChatGroupMemberToFriends(string groupName, List<string> memberName, AddFriendsOptions options = null) => await this.OuterGroup.AddChatGroupMemberToFriends(groupName, memberName, options);

        #endregion

        #region 通讯录管理
        /// <summary>
        ///<para> 获取所有好友的信息列表,具体请考<see cref="FriendInfo"/>类说明.</para>
        ///<para> 注意：只会获取通讯录中的联系人、企业微信联系人和群聊的记录,公众号，服务号，我的企业等特殊账号不会获取.</para>
        ///<para> 1.如果是企业微信，会剔除@xxxx后缀，以保持一致性.</para>
        ///<para> 2.如果好友/群聊/企业微信联系人等有备注，则备注会覆盖昵称显示.</para>
        ///<para> 3.注意：如果微信联系人有重名，此方法会仅获取/保存一个联系人，所以运行此方法前:建议好友/群聊/企业微信联系人有重名时，通过手工的方式添加备注，以保持区分.</para>
        ///<para> 4.普通联系人可以获取wxid,其他的如：群聊/企业微信联系人无法获取wxid.</para>
        ///<para> 5. 此方法运行结果会保存在cache中,默认为true,从cache中获取数据，如果设置为false,则重新刷新一遍通讯录,cache也会同步更新，建议实际开发过程中运行一遍从通讯录获取好友信息的操作,并且做好添加好友时的同步工作（在一些监听的场景，如果读取到此好友没有wxid,也会自动获取,并同步更新cache）</para>
        /// <para>6. 不必太过于担心cache过期的问题，因为实际需要识别wxid的业务场景中，如果碰到新好友在cache中没有数据，会自动获取好友的信息并更新cahce，所以也不必太担心cache的过时问题</para>
        /// </summary>
        /// <param name="fromCache">是否从cahce中获取数据</param>
        /// <returns>好友列表</returns>
        public async Task<List<FriendInfo>> GetAllFriends(bool fromCache = true) => await AddressBookList.GetAllFriends(fromCache);
        /// <summary>
        /// 获取所有好友名称列表.（通过通讯录）
        /// 如果好友有昵称与备注，优先选择备注名
        /// 注意：如果是企业微信，会剔除@xxxx后缀，以保持一致性.
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetAllFriendNames() => await AddressBookList.GetAllFriendNames();

        /// <summary>
        /// 通过加好友添加申请
        /// </summary>
        /// <param name="options">配置对象，具体参见<see cref="FriendRequestAutoAcceptOptions"/></param>
        /// <param name="token">取消今牌</param>
        /// <returns>返回加成功的好友昵称列表</returns>
        public async Task<List<NewFriendBackItem>> PassedAllNewFriend(FriendRequestAutoAcceptOptions options, CancellationToken token = default)
          => await this.AddressBookList.PassedAllNewFriend(options, token);

        #endregion

        #region 朋友圈管理
        /// <summary>
        /// 打开朋友圈,如果未打开，则打开朋友圈，如果已经打开了，则窗口提前到顶端
        /// </summary>
        /// <returns>返回朋友圈窗口对象|</returns>
        public async Task<Window> OpenMoments() => await this.Moments.OpenMoments();
        /// <summary>
        /// 关闭朋友圈
        /// </summary>
        /// <returns></returns>
        public async Task CloseMoments() => await this.Moments.CloseMoments();
        /// <summary>
        /// 发送朋友圈
        /// </summary>
        /// <param name="imageFiles">图片列表，可以一个，也可以多个,如果是多个文件，要求在同一个目录中</param>
        /// <param name="content">朋友圈内容</param>
        /// <param name="options">发送选项，请参考<see cref="MomentsOptions"/></param>
        /// <returns>成功还是失败</returns>
        public async Task<bool> AddMoments(List<string> imageFiles, string content, MomentsOptions options = null)
          => await this.Moments.AddMoments(imageFiles, content, options);
        /// <summary>
        /// 移除自己发送的朋友圈
        /// </summary>
        /// <param name="content">朋友圈文字内容</param>
        /// <returns>是否成功删除</returns>
        public async Task<bool> RemoveMoments(string content)
          => await this.Moments.RemoveMoments(content);
        #endregion

        #region 释放资源
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~WeChatClient()
        {
            Dispose(false);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            _disposed = true;
            if (disposing)
            {
                SystemMonitorChannel.Writer.Complete();
                _ActionTokenSource?.Cancel();
                if (_SystemMonitorTask != null && !_SystemMonitorTask.IsCompleted)
                {
                    try
                    {
                        _SystemMonitorTask.Wait(TimeSpan.FromSeconds(3));
                    }
                    catch (AggregateException) { }
                    catch (Exception) { }
                }

                MessageMonitor?.Dispose();
                NewFriendMonitor?.Dispose();
            }
        }
        #endregion
    }
}