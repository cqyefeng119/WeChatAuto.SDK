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


namespace WeChatAuto.Components
{
    /// <summary>
    /// 微信客户端
    /// 适用于单个微信客户端的自动化操作
    /// </summary>
    public class WeChatClient : IDisposable
    {
        private const string version = "4.1.9.30";
        private readonly AutoLogger<WeChatClient> _logger;
        private IServiceProvider serviceProvider;
        private volatile bool _disposed = false;
        private Navigation _Navigation;
        private UIThreadInvoker _MainThreadInvoker;
        #region 下面三个公开字段为比较稳定的字段，只要微信不关闭
        public readonly Window MainWindow;
        public readonly int ClientProcessId;
        public readonly WeChatClientFactory Factory;
        public readonly string NickName;
        public readonly string WxId;
        public readonly string AvatorPath;
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
        public WeChatClient(int clientProcessId, IServiceProvider provider, WeChatClientFactory factory,
         Window window, UIThreadInvoker uIThreadInvoker, OwerInfo ownerInfo)
        {
            this._MainThreadInvoker = uIThreadInvoker;
            this.MainWindow = window;
            this.serviceProvider = provider;
            this.Factory = factory;
            this.ClientProcessId = clientProcessId;
            this.NickName = ownerInfo.NickName;
            this.WxId = ownerInfo.WxId;
            this.AvatorPath = ownerInfo.AvatorPath;
            _logger = provider.GetRequiredService<AutoLogger<WeChatClient>>();
            CheckVersion();
            _Initialize();
        }

        private void _Initialize()
        {
            this.Navigation.SwitchNavigationCore(NavigationType.微信);
            this.ToolBar = new ToolBar(this.MainWindow, this.MainThreadInvoker, serviceProvider);
            this.Conversations = new ConversationList(this, this._MainThreadInvoker, serviceProvider);
            this.ChatContent = new ChatContent(this, this._MainThreadInvoker, serviceProvider);
        }

        #region POM对象
        /// <summary>
        /// 导航栏, 参考: <see cref="Navigation"/>
        /// </summary>
        internal Navigation Navigation => GetNavigation();
        /// <summary>
        /// 微信ToolBar,参考:<see cref="ToolBar"/>
        /// </summary>
        internal ToolBar ToolBar;
        /// <summary>
        /// 会话管理对象,参考:<see cref="ConversationList"/>
        /// </summary>
        internal ConversationList Conversations;
        /// <summary>
        /// ChatContent对象,参考:<see cref="ChatContent"/>
        /// </summary>
        internal ChatContent ChatContent;

        #endregion
        private Navigation GetNavigation()
        {
            if (_Navigation != null)
            {
                _Navigation.Dispose();
            }
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

        #endregion

        #region  监听管理
        #endregion

        #region 好友/群聊管理
        #endregion

        #region 通讯录管理
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
                _Navigation?.Dispose();
            }
        }
        #endregion
    }
}