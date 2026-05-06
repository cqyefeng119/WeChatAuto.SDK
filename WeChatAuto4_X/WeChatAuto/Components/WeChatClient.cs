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
            this.Conversations = new ConversationList(this,this._MainThreadInvoker,serviceProvider);
        }

        #region POM对象
        //导航栏
        public Navigation Navigation => GetNavigation();
        //ToolBar
        public ToolBar ToolBar;
        //会话管理对象
        public ConversationList Conversations;

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
        #endregion

        #region 消息管理
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