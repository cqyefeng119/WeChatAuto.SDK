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
        /// <summary>
        /// 获取群聊成员列表
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <returns>群聊成员列表</returns>
        public async Task<List<string>> GetChatGroupMemberList(string groupName) => null;

        /// <summary>
        /// 是否是群聊成员
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <param name="memberName">成员名称</param>
        /// <returns>是否是群聊成员</returns>
        public async Task<bool> IsChatGroupMember(string groupName, string memberName) => false;

        /// <summary>
        /// 是否是自有群
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <returns>是否是自有群</returns>
        public async Task<bool> IsOwnerChatGroup(string groupName) => false;

        /// <summary>
        /// 获取群主
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <returns>群主昵称</returns>
        public async Task<string> GetGroupOwner(string groupName) => null;

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