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
    /// 他有群管理
    /// </summary>
    public class OuterGroup : Group
    {
        internal OuterGroup(WeChatClient client, UIThreadInvoker uiThreadInvoker, IServiceProvider serviceProvider) :
            base(client, uiThreadInvoker, serviceProvider)
        {

        }

        /// <summary>
        /// 邀请群聊成员,适用于外部群
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <param name="memberName">成员名称</param>
        /// <param name="helloText">打招呼文本</param>
        /// <returns>微信响应结果<see cref="ChatResponse"/></returns>
        public async Task<ChatResponse> InviteChatGroupMember(string groupName, OneOf<string, string[]> memberName, string helloText = "") => throw new Exception("正在完成中");

        /// <summary>
        /// 添加群聊里面的好友为自己的好友,适用于从外部群中添加好友为自己的好友
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <param name="memberName">成员名称</param>
        /// <param name="intervalSecond">间隔时间</param>
        /// <param name="helloText">打招呼文本</param>
        /// <param name="label">好友标签,方便归类管理</param>
        /// <returns>微信响应结果<see cref="ChatResponse"/></returns>
        public async Task<ChatResponse> AddChatGroupMemberToFriends(string groupName, OneOf<string, string[]> memberName, int intervalSecond = 5, string helloText = "", string label = "")
          => throw new Exception("正在完成中");

        /// <summary>
        /// 添加群聊里面的所有好友为自己的好友,适用于从外部群中添加所有好友为自己的好友
        /// 风控提醒：
        /// 1、此方法容易触发微信风控机制，建议使用分页添加，并使用键鼠模拟器的方式增加好友。
        /// 1、微信对于加好友每天有数量的限制，实际测试一天只能加20多个，超出数量会返回[操作过于频繁，请稍后再试。]消息.
        /// 2、实际测试:使用键鼠模拟器的方式增加好友，只会受上述的增加好友数量限制，不会被风控退出。
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <param name="exceptList">排除列表</param>
        /// <param name="intervalSecond">间隔时间</param>
        /// <param name="helloText">打招呼文本</param>
        /// <param name="label">好友标签,方便归类管理</param>
        /// <param name="pageNo">起始页码,从1开始,如果从0开始，表示不使用分页，全部添加好友，但容易触发微信风控机制，建议使用分页添加</param>
        /// <param name="pageSize">每页数量</param>
        /// <returns>微信响应结果<see cref="ChatResponse"/></returns>
        public async Task<ChatResponse> AddAllChatGroupMemberToFriends(string groupName, List<string> exceptList = null, int intervalSecond = 3,
            string helloText = "", string label = "", int pageNo = 1, int pageSize = 15)
          => throw new Exception("正在完成中");
        // /// <summary>
        // /// 添加群聊里面的所有好友为自己的好友,适用于从外部群中添加所有好友为自己的好友
        // /// 注意：此方法容易触发微信风控机制，建议使用分页添加，并使用键鼠模拟器的方式增加好友。
        // /// </summary>
        // /// <param name="groupName">群聊名称</param>
        // /// <param name="options">添加群聊成员为好友的选项<see cref="AddGroupMemberOptions"/></param>
        // /// <returns>微信响应结果<see cref="ChatResponse"/></returns>
        // public async Task<ChatResponse> AddAllChatGroupMemberToFriends(string groupName, Action<AddGroupMemberOptions> options)
        //   => await WxMainWindow.AddAllChatGroupMemberToFriends(groupName, options);

    }
}