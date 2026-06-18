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
        /// <param name="groupName">群聊名称,可以为空，如果为空，则在本焦点群聊窗口邀请好友</param>
        /// <param name="members">被邀请的成员名称列表,要求在自己的通讯录中</param>
        /// <param name="inviteReasonIfNeed">邀请原因，只在群管理员开启了 进群需要群主或者管理员确认 功能时有效，可以为空</param>
        /// <returns>微信响应结果<see cref="Result"/></returns>
        public async Task<Result> InviteChatGroupMember(string groupName, List<string> members, string inviteReasonIfNeed = "")
        {
            if (!string.IsNullOrWhiteSpace(groupName))
            {
                var find = await _Client.Conversations.Search(groupName);
                if (!find)
                    return Result.Fail($"错误:未找名为 {groupName} 的群聊!");
            }
            var headInfo = await this._Client.GetTitle();
            if (!headInfo.CanTalk() || headInfo.HeaderType != ChatType.群聊)
                return Result.Fail($"错误：群聊窗口状态错误，或者不是群聊");
            return await WeChatInvoker.Call(InviteChatGroupMemberCore, headInfo.Title, members, inviteReasonIfNeed);
        }

        private Result InviteChatGroupMemberCore(UIA3Automation automation, string groupName, List<string> members, string reason)
        {
            Result result = _PopupInviteWindow(automation, groupName);  //弹出邀请好友窗口
            if (!result.Success) return result;
            result = _SelectInviteMembers(automation, groupName, members);
            if (!result.Success) return result;
            result = _ClickAddButton(automation, groupName);
            if (!result.Success)
            {
                SupperMouseKey.TypeSimultaneously(VirtualKeyShort.ESC);
                CloseChatInfoPane();
                return result;
            }
            result = _ClickOwnerConfirm(automation, groupName, reason);
            result = _ClickConfirmButtonIfExist(automation, groupName);
            this.CloseChatInfoPane();
            return result;
        }

        private Result _SelectInviteMembers(UIA3Automation automation, string groupName, List<string> members)
        {
            var path = "/Window[@Name='微信添加群成员'][@ClassName='mmui::SessionPickerWindow']/Group/Group/Group/Edit[@Name='搜索'][@ClassName='mmui::XValidatorTextEdit']";
            var editRetry = Retry.WhileNull(() => this._Client.MainWindow.FindFirstByXPath(path), TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(200));
            if (editRetry.Success)
            {
                foreach (var m in members)
                {
                    var edit = editRetry.Result.AsTextBox();
                    var parent = edit.GetParent();
                    var clearButton = parent.FindFirstChild(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("清空")));
                    if (clearButton != null)
                    {
                        clearButton.Click();
                        RandomWait.Wait(300, 900);
                    }
                    edit.Focus();
                    edit.Text = m;
                    RandomWait.Wait(1000, 3000);
                    path = "/Window[@Name='微信添加群成员'][@ClassName='mmui::SessionPickerWindow']/Group/Group/List[@AutomationId='sp_search_result_list'][@Name='请勾选需要添加的联系人']";
                    var items = this._Client.MainWindow.FindFirstByXPath(path).AsListBox();
                    var item = items.FindAllChildren(cf => cf.ByControlType(ControlType.CheckBox)).FirstOrDefault(x => x.Name.Trim().Equals(m.Trim()));
                    if (item != null)
                    {
                        if (item.Patterns.Toggle.IsSupported)
                        {
                            //可以点击，否则不能点击
                            var point = item.BoundingRectangle.Center();
                            Mouse.Position = point.Confusion(10, 4);
                            RandomWait.Wait(100, 300);
                            SupperMouseKey.MoveTo(point.Confusion(10, 4));
                            RandomWait.Wait(300, 900);
                            SupperMouseKey.LeftClick();
                        }
                    }

                    RandomWait.Wait(1000, 4000);
                }
                return Result.Ok();
            }
            return Result.Fail("错误：选择被邀请成员时出错！");
        }

        private Result _ClickAddButton(UIA3Automation automation, string groupName)
        {
            var path = "/Window[@Name='微信添加群成员'][@ClassName='mmui::SessionPickerWindow']/Group/Group/Button[@Name='添加'][@AutomationId='confirm_btn']";
            var buttonRetry = Retry.WhileNull(() => this._Client.MainWindow.FindFirstByXPath(path), TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(200));
            if (buttonRetry.Success)
            {
                var button = buttonRetry.Result;
                if (button.IsEnabled)
                {
                    var point = button.BoundingRectangle.Center();
                    Mouse.Position = point.Confusion(10, 2);
                    RandomWait.Wait(100, 300);
                    SupperMouseKey.MoveTo(point.Confusion(10, 2));
                    RandomWait.Wait(300, 900);
                    SupperMouseKey.LeftClick();
                    RandomWait.Wait(1000, 4000);
                    return Result.Ok();
                }
            }
            return Result.Fail("错误：点击 增加 按钮出错，可能增加按钮不可点击！");
        }
        private Result _ClickOwnerConfirm(UIA3Automation automation, string groupName, string reason)
        {
            var path = "/Window[@Name='Weixin']/Group/Group/Group/Button[@Name='邀请']";
            var inviteButtonRetry = Retry.WhileNull(() => this._Client.MainWindow.FindFirstByXPath(path), TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(200));
            if (inviteButtonRetry.Success)
            {
                var inviteButton = inviteButtonRetry.Result.AsButton();
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    path = "/Window[@Name='Weixin']/Group/Group/Group/Edit/Group[@AutomationId='qt_scrollarea_viewport']";
                    var group = this._Client.MainWindow.FindFirstByXPath(path);
                    if (group != null)
                    {
                        var edit = group.GetParent().AsTextBox();
                        edit.Focus();
                        RandomWait.Wait(300, 900);
                        edit.Text = reason;
                        RandomWait.Wait(1000, 2000);
                    }
                }
                inviteButton.Click();
                RandomWait.Wait(1000, 4000);
            }
            return Result.Ok();
        }
        private Result _ClickConfirmButtonIfExist(UIA3Automation automation, string groupName)
        {
            var path = "/Window[@Name='Weixin']/Group/Group/Group/Button[@Name='邀请']";
            var inviteButtonRetry = Retry.WhileNull(() => this._Client.MainWindow.FindFirstByXPath(path), TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(200));
            if (inviteButtonRetry.Success)
            {
                var inviteButton = inviteButtonRetry.Result.AsButton();
                inviteButton.Click();
                RandomWait.Wait(1000, 4000);
            }
            return Result.Ok();
        }

        private Result _PopupInviteWindow(UIA3Automation automation, string groupName)
        {
            var paneRoot = PaneRoot;
            var point = this._Client.OcrEngee.OCRVerticalDetect(paneRoot, 0.5f, "添加");
            if (point.IsEmpty)
                return Result.Fail("错误: OCR 添加 按钮失败!");
            this._Client.MainWindow.Focus();
            Mouse.Position = paneRoot.BoundingRectangle.Center();
            RandomWait.Wait(600, 1200);
            var point2 = new Point(point.X, point.Y - 30).Confusion(10, 5);
            SupperMouseKey.MoveTo(point2);
            RandomWait.Wait(300, 1200);
            SupperMouseKey.LeftClick();
            RandomWait.Wait(800, 1500);
            return Result.Ok();
        }


        /// <summary>
        /// 添加群聊里面的好友为自己的好友,适用于从外部群中添加好友为自己的好友
        /// </summary>
        /// <param name="groupName">群聊名称,可以为空，如果为空，则在本焦点群聊窗口邀请好友</param>
        /// <param name="memberName">成员名称列表,考虑风控,建议先运行<see cref="Group.GetChatGroupMemberList()"/>获取群聊的成员列表，然后分批增加</param>
        /// <returns></returns>
        public async Task<IDictionary<string, FriendAddResult>> AddChatGroupMemberToFriends(string groupName, List<string> memberName)
        {
            if (!string.IsNullOrWhiteSpace(groupName))
            {
                var find = await _Client.Conversations.Search(groupName);
                if (!find)
                    return new Dictionary<string, FriendAddResult>();
            }
            var headInfo = await this._Client.GetTitle();
            if (!headInfo.CanTalk() || headInfo.HeaderType != ChatType.群聊)
                return new Dictionary<string, FriendAddResult>();
            return await WeChatInvoker.Call(AddChatGroupMemberToFriendsCore, headInfo.Title, memberName);
        }

        private IDictionary<string, FriendAddResult> AddChatGroupMemberToFriendsCore(UIA3Automation automation, string groupName, List<string> memberName)
        {
            return null;
        }
    }
}