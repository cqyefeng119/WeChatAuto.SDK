using FlaUI.Core;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.Core.EventHandlers;
using FlaUI.Core.AutomationElements;
using System.Collections.Generic;
using System.Linq;
using WeAutoCommon.Enums;
using WeAutoCommon.Utils;
using WeChatAuto.Utils;
using FlaUI.UIA3.Converters;
using FlaUI.Core.WindowsAPI;
using System;
using WeChatAuto.Extentions;
using WeAutoCommon.Interface;
using System.Text;
using OneOf;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Windows.Controls.Primitives;
using FlaUI.Core.Patterns;
using System.Drawing;
using System.Threading.Tasks;
using WeAutoCommon.Extentions;
using System.Windows;
using FlaUI.UIA3.Patterns;
using FlaUI.UIA3;
using WeAutoCommon.Models;

namespace WeChatAuto.Components
{
    /// <summary>
    /// 群聊管理
    /// </summary>
    public class Group
    {
        protected readonly WeChatClient _Client;
        protected readonly UIThreadInvoker uiThreadInvoker;
        protected readonly IServiceProvider serviceProvider;
        internal Group(WeChatClient client, UIThreadInvoker uiThreadInvoker, IServiceProvider serviceProvider)
        {
            this._Client = client;
            this.uiThreadInvoker = uiThreadInvoker;
            this.serviceProvider = serviceProvider;
        }

        internal Button RootBotton
        {
            get
            {
                var path = $"/Group/Custom/Group/Group/Group/Custom/Custom/Custom/Group/Custom/Custom/Group/Group/Group/Group/Group/Group/Group/Group/Group/Group/Button[@Name='聊天信息'][@AutomationId='content_view.top_content_view.title_h_view.right_v_view.right_content_h_view.right_content_v_view.right_ui_.more_button'][@ClassName='mmui::XButton']";
                var index = 0;
                while (index <= 1)
                {
                    var buttonRetry = Retry.WhileNull(() => this._Client.MainWindow.FindFirstByXPath(path), timeout: TimeSpan.FromSeconds(1), interval: TimeSpan.FromMilliseconds(200));
                    if (buttonRetry.Success)
                    {
                        return buttonRetry.Result.AsButton();
                    }
                    else
                    {
                        this._Client.Navigation.SwitchNavigationCore(uiThreadInvoker.Automation, NavigationType.微信);
                        index++;
                    }
                }
                return null;
            }
        }
        internal AutomationElement PaneRoot => _GetGoupPaneRoot();
        internal AutomationElement SearchEdit => _GetSearEdit();
        internal ListBoxItem[] ChatMemberList => _GetChatMemberList();

        private ListBoxItem[] _GetChatMemberList()
        {
            var paneRoot = PaneRoot;
            if (paneRoot == null)
                return null;
            return paneRoot.FindFirstDescendant(cf => cf.ByControlType(ControlType.List).And(cf.ByAutomationId("chat_member_list")).And(cf.ByClassName("QFReuseGridWidget")))?.AsListBox().Items;
        }

        private AutomationElement _GetGoupPaneRoot()
        {
            var rootButton = RootBotton;
            if (rootButton == null)
                return null;
            var index = 0;
            while (index <= 1)
            {
                //可能没有打开
                var paneRetry = Retry.WhileNull(() => this._Client.MainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Group).And(cf.ByClassName("mmui::ChatRoomMemberInfoView"))), timeout: TimeSpan.FromSeconds(1), interval: TimeSpan.FromMilliseconds(200));
                if (paneRetry.Success)
                {
                    return paneRetry.Result;
                }
                else
                {
                    index++;
                    rootButton.ClickEnhance(this._Client.MainWindow);
                }
            }

            return null;
        }

        private AutomationElement _GetSearEdit()
        {
            var paneRoot = PaneRoot;
            if (paneRoot == null)
                return null;
            return paneRoot.FindFirstDescendant(cf => cf.ByName("搜索").And(cf.ByClassName("mmui::XValidatorTextEdit")));
        }

        //微信风控，提供不全ui tree,暂时不提供此api,日后想办法
        // /// <summary>
        // /// 获取群聊成员列表
        // /// </summary>
        // /// <param name="groupName">群聊名称</param>
        // /// <returns>群聊成员列表</returns>
        // public async Task<List<string>> GetChatGroupMemberList(string groupName)
        // {
        //     return await WeChatInvoker.Call(GetChatGroupMemberListCore, groupName);
        // }

        internal bool CheckGroup(UIA3Automation automation, string groupName)
        {
            var search = this._Client.Conversations.SearchWhoCore(automation, groupName);
            if (!search)
                return false;
            var headerInfo = this._Client.ChatContent.ChatHeader.GetTitleCore(automation);
            if (headerInfo == null)
                return false;
            if (!headerInfo.CanTalk())
                return false;
            if (headerInfo.HeaderType != ChatType.群聊)
                return false;
            if (headerInfo.Title != groupName)
                return false;
            return true;
        }

        // 微信风控，提供不全ui tree,日后想办法.
        // internal List<string> GetChatGroupMemberListCore(UIA3Automation automation, string groupName)
        // {
        //     if (!CheckGroup(automation, groupName))
        //         return new List<string>();
        //     var rootButton = RootBotton;
        //     rootButton.ClickEnhance(this._Client.MainWindow);


        // }

        // /// <summary>
        // /// 是否是群聊成员
        // /// </summary>
        // /// <param name="groupName">群聊名称</param>
        // /// <param name="memberName">成员名称</param>
        // /// <returns>是否是群聊成员</returns>
        // public async Task<bool> IsChatGroupMember(string groupName, string memberName) => false;

        /// <summary>
        /// 是否是自有群
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <returns>是否是自有群</returns>
        public async Task<bool> IsOwnerChatGroup(string groupName)
        {
            var name = await GetGroupOwner(groupName);
            return name == this._Client.NickName ? true : false;
        }

        /// <summary>
        /// 获取群主
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <returns>群主昵称</returns>
        public async Task<string> GetGroupOwner(string groupName)
        {
            return await WeChatInvoker.Call(GetGroupOwnerCore,groupName);
        }

        private string GetGroupOwnerCore(UIA3Automation automation, string groupName)
        {
            if (!CheckGroup(automation,groupName))
                return "";
            var list = _GetChatMemberList();
            if (list.Length > 0)
            {
                this._Client.ChatContent.Sender.FcouseSenderCore(automation);
                return list[0].Name.Trim();
            }
            return "";
        }

        /// <summary>
        /// 清空群聊历史聊天记录
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        public async Task ClearChatGroupHistory(string groupName) => await Task.CompletedTask;

        /// <summary>
        /// 退出群聊
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        public async Task QuitChatGroup(string groupName) => await Task.CompletedTask;

        /// <summary>
        /// 设置保存到通讯录
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <param name="isSaveToAddress">是否保存到通讯录,默认是True:保存,False:取消保存</param>
        /// <returns>微信响应结果<see cref="ChatResponse"/></returns>
        public ChatResponse SetSaveToAddress(string groupName, bool isSaveToAddress = true) => null;
    }
}