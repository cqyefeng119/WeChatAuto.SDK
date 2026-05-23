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
    /// 自有群管理
    /// </summary>
    public class OwnerGroup : Group
    {
        internal OwnerGroup(WeChatClient client, UIThreadInvoker uiThreadInvoker, IServiceProvider serviceProvider) :
            base(client, uiThreadInvoker, serviceProvider)
        {

        }
        /// <summary>
        /// 改变自有群群备注
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <param name="newMemo">新备注</param>
        /// <returns>微信响应结果<see cref="ChatResponse"/></returns>
        public ChatResponse ChangeOwnerChatGroupMemo(string groupName, string newMemo) => throw new Exception("待完成");
        //   => WxMainWindow.ChangeOwnerChatGroupMemo(groupName, newMemo);
        /// <summary>
        /// 修改群名，适用于自有群群名
        /// </summary>
        /// <param name="oldGroupName">旧群名称</param>
        /// <param name="newGroupName">新群名称</param>
        /// <returns>微信响应结果</returns>
        public ChatResponse ChangeOwnerChatGroupName(string oldGroupName, string newGroupName) => throw new Exception("待完成");
        //   => WxMainWindow.ChangeOwnerChatGroupName(oldGroupName, newGroupName);
        /// <summary>
        /// 更新群聊公告
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <param name="groupNotice">群聊公告</param>
        /// <returns>微信响应结果</returns>
        public async Task<ChatResponse> UpdateGroupNotice(string groupName, string groupNotice) => throw new Exception("待完成");
        //   => await WxMainWindow.UpdateGroupNotice(groupName, groupNotice);
        /// <summary>
        /// 创建群聊
        /// 如果存在，则打开它，否则创建一个新群聊
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <param name="memberName">成员名称</param>
        /// <returns>微信响应结果<see cref="ChatResponse"/></returns>
        public ChatResponse CreateOrUpdateOwnerChatGroup(string groupName, OneOf<string, string[]> memberName) => throw new Exception("待完成");


        /// <summary>
        /// 添加群聊成员，适用于自有群
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <param name="memberName">成员名称</param>
        /// <returns>微信响应结果<see cref="ChatResponse"/></returns>
        public async Task<ChatResponse> AddOwnerChatGroupMember(string groupName, OneOf<string, string[]> memberName) => throw new Exception("待完成");
        //   => await WxMainWindow.AddOwnerChatGroupMember(groupName, memberName);
        /// <summary>
        /// 删除群聊，适用于自有群,与退出群聊不同，退出群聊是退出群聊，删除群聊会删除自有群的所有好友，然后退出群聊
        /// willdo: 这里有一个问题，如果删除群的好友很多，则需要滚屏才能全部选中。
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <returns>微信响应结果<see cref="ChatResponse"/></returns>
        public async Task<ChatResponse> DeleteOwnerChatGroup(string groupName) => throw new Exception("待完成");
        //=> await WxMainWindow.DeleteOwnerChatGroup(groupName);
        /// <summary>
        /// 移除群聊成员,适用于自有群
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <param name="memberName">成员名称</param>
        /// <returns>微信响应结果<see cref="ChatResponse"/></returns>
        public async Task<ChatResponse> RemoveOwnerChatGroupMember(string groupName, OneOf<string, string[]> memberName) => throw new Exception("待完成");
        //=> await WxMainWindow.RemoveOwnerChatGroupMember(groupName, memberName);
    }
}