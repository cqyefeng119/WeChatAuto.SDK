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


namespace WeChatAuto.Components
{
    /// <summary>
    /// 微信客户端
    /// 适用于单个微信客户端的自动化操作
    /// </summary>
    public class WeChatClient : IDisposable
    {
        private const string version = "4.1.9.55";
        private readonly AutoLogger<WeChatClient> _logger;
        private IServiceProvider serviceProvider;
        private volatile bool _disposed = false;
        private Navigation _Navigation;
        private UIThreadInvoker _MainThreadInvoker;
        private ReaderWriterLockSlim readerWriterLockSlim = new ReaderWriterLockSlim();
        internal SemaphoreSlim noticeEvent = new SemaphoreSlim(1, 1);    //有消息事件.

        #region 下面三个公开字段为比较稳定的字段，只要微信不关闭
        public readonly Window MainWindow;
        public readonly int ClientProcessId;
        public readonly WeChatClientFactory Factory;
        public readonly string NickName;
        public readonly string WxId;
        public readonly string AvatorPath;
        public readonly int WechatIndex;
        public UIThreadInvoker MainThreadInvoker => _MainThreadInvoker;
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
        public WeChatClient(int clientProcessId, IServiceProvider provider, WeChatClientFactory factory,
         Window window, UIThreadInvoker uIThreadInvoker, OwerInfo ownerInfo, int index)
        {
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
            CheckVersion();
            _Initialize();
        }

        private void _Initialize()
        {
            this.Navigation.SwitchNavigationCore(null, NavigationType.微信);
            this.ToolBar = new ToolBar(this.MainWindow, this.MainThreadInvoker, serviceProvider);
            this.Conversations = new ConversationList(this, this._MainThreadInvoker, serviceProvider);
            this.ChatContent = new ChatContent(this, this._MainThreadInvoker, serviceProvider);
            this.AddressBookList = new AddressBookList(this, this._MainThreadInvoker, serviceProvider);
            this.MessageMonitor = new MessageMonitor(this, serviceProvider, _MainThreadInvoker, noticeEvent);
            this.NewFriendMonitor = new NewFriendMonitor(this, serviceProvider, _MainThreadInvoker, noticeEvent);
            this.NotifyIcon = new ShellNotifyIcon(this, serviceProvider, WechatIndex);
            this.OwnerGroup = new OwnerGroup(this, _MainThreadInvoker, serviceProvider);
            this.OuterGroup = new OuterGroup(this, _MainThreadInvoker, serviceProvider);
            _RunCheckAddressBook();
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


        #endregion
        private Navigation GetNavigation()
        {
            _Navigation = new Navigation(this, this.MainThreadInvoker, this.serviceProvider);
            return _Navigation;
        }
        private void CheckVersion()
        {
            if (WeAutomation.Config.WxVersion != version)
            {
                throw new Exception("错误：配置参数错误！请检查：1.微信客户端是否是最新版本,2.是否正确设置参数.");
            }
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
        /// 如此置顶微信窗口
        /// </summary>
        /// <returns></returns>
        public async Task UnPinned() => await ToolBar.Top(false);

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
        /// 点击任务栏图标
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
        /// <returns></returns>
        public async Task<List<string>> GetAllConversations() => await this.Conversations.GetAllConversations();

        /// <summary>
        /// 获取会话列表可见会话标题
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetVisibleConversationTitles() => await this.Conversations.GetVisibleConversationTitles();

        /// <summary>
        /// 获取可见会话列表
        /// 会话信息包含：会话名称、会话未读消息数、会话头像等具体信息，请参考<see cref="SimpleConversation"/>
        /// </summary>
        /// <returns>返回<see cref="Conversation"/>列表</returns>
        public async Task<List<SimpleConversation>> GetVisibleConversations() => await this.Conversations.GetVisibleConversations();

        /// <summary>
        /// 搜索好友/群聊
        /// </summary>
        /// <param name="who">待搜索的好友/群聊昵称,who - 微信会话列表肉眼可见的名称,如果群有备注，则这个who即为备注名</param>
        /// <returns>如果找到，返回true,如果没有找到，则返回false.</returns>
        public async Task<bool> Search(string who) => await this.Conversations.Search(who);

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
        public async Task FcouseSenderInput() => await this.ChatContent.FcouseSenderInput();

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
        public async Task SendMessage(string who, string message, OneOf<string, string[], List<string>> atUser = default) => await ChatContent.SendMessage(who, message, atUser);

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
        /// 根据日期获取当前聊天窗口的聊天历史
        /// </summary>
        /// <param name="dates">查询日期列表</param>
        /// <returns>返回<see cref="ChatSimpleMessage"/>列表</returns>
        public async Task<List<ChatSimpleMessage>> GetChatHistory(List<DateTime> dates = default) => await ChatContent.GetChatHistory(dates);

        /// <summary>
        /// 根据日期获取聊天历史
        /// </summary>
        /// <param name="who">微信名称，可以是好友/群聊的微信名称,可以为空，如果为空，则获取当前聊天窗口的历史记录</param>
        /// <param name="date">查询日期,如果不传，则是当天日期</param>
        /// <returns>返回<see cref="ChatSimpleMessage"/>列表</returns>
        public async Task<List<ChatSimpleMessage>> GetChatHistory(string who, DateTime date = default) => await ChatContent.GetChatHistory(who, date);

        /// <summary>
        /// 获取一段时间的聊天历史记录
        /// </summary>
        /// <param name="who">微信名称，可以是好友/群聊的微信名称,可以为空，如果为空，则获取当前聊天窗口的历史记录</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns></returns>
        public async Task<List<ChatSimpleMessage>> GetChatHistory(string who, DateTime startDate, DateTime endDate) => await ChatContent.GetChatHistory(who, startDate, endDate);

        /// <summary>
        /// 获取多个指定日期的聊天历史记录
        /// </summary>
        /// <param name="who">微信名称，可以是好友/群聊的微信名称,可以为空，如果为空，则获取当前聊天窗口的历史记录</param>
        /// <param name="range">指定的多个日期</param>
        /// <returns></returns>
        public async Task<List<ChatSimpleMessage>> GetChatHistory(string who, List<DateTime> range) => await ChatContent.GetChatHistory(who, range);

        #endregion

        #region  监听管理
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
        public async Task AddMessageListener(OneOf<string, List<string>, string[]> nickNames, Action<MessageContext> callBack, bool IsOpenMonitor = false, CancellationToken userToken = default, Action<string> UIInvoker = null)
            => await this.MessageMonitor.AddMessageListener(nickNames, callBack, IsOpenMonitor, userToken, UIInvoker);
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
        /// <param name="userToken">取消令牌,请参考<see cref="CancellationToken"/>,可以自行取消消息监听</param>
        /// <param name="UIInvoker">UI的调度器，适用于把微信嵌入UI的场景使用，如：多微信切换Tab页等,SDK会给调用者注入一个微信名称</param>
        public async Task AddMessageListener(OneOf<string, List<string>, string[]> nickNames, Action<MessageContext> callBack, TimeOnly startTime, TimeOnly endTime, bool IsOpenMonitor = false, CancellationToken userToken = default, Action<string> UIInvoker = null)
          => await this.MessageMonitor.AddMessageListener(nickNames, callBack, startTime, endTime, IsOpenMonitor, userToken, UIInvoker);
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
        /// <param name="range">一天中的多个时间段,如果设定多个时间段，监听器在这些时间段内开始/结束监听,时间段类请参考:<see cref="TimeOnlyRange"/>,另注意：可以跨天，如设置为:23:00 ~ 02:00,则表示当天23:00至明天02:00</param>
        /// <param name="IsOpenMonitor">是否开启开放式监听，默认不开放(值为false）,如果开启开放式监听，前面的nickNames可以为空，所谓的开放式监听的含义是：无须固定好友/群监听，只要此好友/群没有设置“消息免打挠”就可以监听</param>
        /// <param name="userToken">取消令牌,请参考<see cref="CancellationToken"/>,可以自行取消消息监听</param>
        /// <param name="UIInvoker">UI的调度器，适用于把微信嵌入UI的场景使用，如：多微信切换Tab页等,SDK会给调用者注入一个微信名称</param>
        public async Task AddMessageListener(OneOf<string, List<string>, string[]> nickNames, Action<MessageContext> callBack, List<TimeOnlyRange> range, bool IsOpenMonitor = false, CancellationToken userToken = default, Action<string> UIInvoker = null)
            => await this.MessageMonitor.AddMessageListener(nickNames, callBack, range, IsOpenMonitor, userToken, UIInvoker);
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
        /// <param name="groupName">群聊名称</param>
        /// <param name="memberName">成员名称</param>
        /// <returns>微信响应结果<see cref="ChatResponse"/></returns>
        public async Task AddOwnerChatGroupMember(string groupName, OneOf<string, string[]> memberName) => await OwnerGroup.AddOwnerChatGroupMember(groupName, memberName);

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
                MessageMonitor?.Dispose();
                NewFriendMonitor?.Dispose();
            }
        }
        #endregion
    }
}