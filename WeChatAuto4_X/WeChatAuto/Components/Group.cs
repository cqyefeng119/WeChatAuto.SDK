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
using System.Reflection.Metadata.Ecma335;
using Emgu.CV;

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
        internal AutomationElement RootPane => _GetChatRootPane();   //root,包括搜索group与内容group.
        internal AutomationElement PaneRoot => _GetGoupPaneRoot();
        internal AutomationElement SearchEdit => _GetSearEdit();
        internal ListBoxItem[] ChatMemberList => _GetChatMemberList();
        internal Group(WeChatClient client, UIThreadInvoker uiThreadInvoker, IServiceProvider serviceProvider)
        {
            this._Client = client;
            this.uiThreadInvoker = uiThreadInvoker;
            this.serviceProvider = serviceProvider;
        }
        /// <summary>
        /// 关闭信息窗口
        /// </summary>
        internal void CloseChatInfoPane()
        {
            var path = "/Group/Custom/Group/Group/Group/Custom/Custom/Custom/Group/Custom/Custom/Group/Group/Group[@ClassName='mmui::ChatRoomMemberInfoView']";
            var groupRetry = Retry.WhileNull(() => this._Client.MainWindow.FindFirstByXPath(path), TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(200)); ;
            if (groupRetry.Success)
            {
                //已经存在.
                var button = RootBotton;
                if (button != null)
                {
                    var point = button.GetClickablePoint();
                    Mouse.Position = point.Confusion(5, 3);
                    RandomWait.Wait(100, 300);
                    SupperMouseKey.MoveTo(point.Confusion(5, 3));
                    RandomWait.Wait(300, 900);
                    SupperMouseKey.LeftClick();
                    RandomWait.Wait(1000, 1500);
                    groupRetry = Retry.WhileNull(() => this._Client.MainWindow.FindFirstByXPath(path), TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(200));
                }
            }
        }

        internal AutomationElement _GetChatRootPane()
        {
            var path = "/Group/Custom/Group/Group/Group/Custom/Custom/Custom/Group/Custom/Custom/Group/Group/Group[@ClassName='mmui::ChatRoomMemberInfoView']";
            var groupRetry = Retry.WhileNull(() => this._Client.MainWindow.FindFirstByXPath(path), TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(200)); ;
            if (groupRetry.Success)
            {
                //已经存在.
                return groupRetry.Result;
            }
            else
            {
                //点击按钮
                var button = RootBotton;
                if (button != null)
                {
                    var point = button.GetClickablePoint();
                    Mouse.Position = point.Confusion(5, 3);
                    RandomWait.Wait(100, 300);
                    SupperMouseKey.MoveTo(point.Confusion(5, 3));
                    RandomWait.Wait(300, 900);
                    SupperMouseKey.LeftClick();
                    RandomWait.Wait(1000, 1500);
                    groupRetry = Retry.WhileNull(() => this._Client.MainWindow.FindFirstByXPath(path), TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(200));
                    return groupRetry.Success ? groupRetry.Result : null;
                }
                return null;
            }
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
            return await WeChatInvoker.Call(GetGroupOwnerCore, groupName);
        }

        private string GetGroupOwnerCore(UIA3Automation automation, string groupName)
        {
            if (!CheckGroup(automation, groupName))
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

        /// <summary>
        /// 修改自己在群中的昵称
        /// </summary>
        /// <param name="groupName">群名</param>
        /// <param name="nickName">昵称，如果为空，则删除自己在本群中的昵称</param>
        /// <returns>微信响应结果<see cref="Result"/></returns>
        public async Task<Result> ChangeChatGroupNickName(string groupName, string nickName)
        {
            var find = await _Client.Conversations.Search(groupName);
            if (!find)
                return Result.Fail($"错误：没有发现groupName={groupName}的群");
            return await WeChatInvoker.Call(ChangeChatGroupNickNameCore, nickName);
        }
        /// <summary>
        /// 修改自己在群中的昵称,本方法适用于当前窗口是群聊的昵称修改
        /// </summary>
        /// <param name="nickName">昵称，如果为空，则删除自己在本群中的昵称</param>
        /// <returns>微信响应结果<see cref="Result"/></returns>
        public async Task<Result> ChangeChatGroupNickName(string nickName)
        {
            var headInfo = await this._Client.GetTitle();
            if (!headInfo.CanTalk() || headInfo.HeaderType != ChatType.群聊)
                return Result.Fail("错误：本窗口不是群聊窗口，修改群聊昵称动作终止!");
            return await WeChatInvoker.Call(ChangeChatGroupNickNameCore, nickName);
        }

        private Result ChangeChatGroupNickNameCore(UIA3Automation automation, string nickName)
        {
            var rootPane = this._GetChatRootPane();
            var pane = rootPane.FindFirstByXPath("/Group[2]");
            if (pane == null)
                return Result.Fail("没有找到聊天信息的根Pane");
            using var bitmap = pane.Capture();
            using var mat = this._Client.OcrEngee.GetMatFromBitmap(bitmap);
            //扩大两倍好识别
            using var mat2 = new Mat();
            CvInvoke.Resize(mat, mat2, Size.Empty, 2, 2, Emgu.CV.CvEnum.Inter.Cubic);
            using var srcImg = mat2.ToBitmap();
            var ocrResult = this._Client.OcrEngee.Detect(srcImg, 0, mat2.Width > mat2.Height ? mat2.Width : mat2.Height, 0.5f, 0.3f, 1.5f, false, false, false);
            var item = ocrResult.TextBlocks.Where(x => x.Text.Trim().Equals("我在本群的昵称")).FirstOrDefault();
            if (item == null)
                return Result.Fail("OCR识别 我在本群的昵称 失败");
            var srcPoint = new Point((int)(item.BoxPoints[0].X / 2 + (item.BoxPoints[2].X - item.BoxPoints[0].X) / 4), item.BoxPoints[0].Y / 2 + (int)((item.BoxPoints[2].Y - item.BoxPoints[0].Y) / 4));
            var point = new Point(pane.BoundingRectangle.X + srcPoint.X, pane.BoundingRectangle.Y + srcPoint.Y);
            var baseStep = 25;
            var ratio = DpiHelper.GetScaleForWindow(this._Client.MainWindow.Properties.NativeWindowHandle);
            var destPoint = new Point(point.X + 90, point.Y + (int)(baseStep * ratio));
            Mouse.Position = destPoint;
            RandomWait.Wait(100, 300);
            SupperMouseKey.MoveTo(destPoint.Confusion(10, 0));
            RandomWait.Wait(300, 900);
            SupperMouseKey.LeftClick();
            RandomWait.Wait(300, 1200);
            SupperMouseKey.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
            RandomWait.Wait(100, 300);
            SupperMouseKey.TypeSimultaneously(VirtualKeyShort.BACK);
            ClipboardHelper.SetText(nickName);
            RandomWait.Wait(300, 1200);
            SupperMouseKey.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);
            RandomWait.Wait(300, 1200);
            SupperMouseKey.Enter();
            RandomWait.Wait(300, 1200);
            var path = "/Window[@Name='Weixin'][@ClassName='mmui::XDialog']/Group/Text[@Name='修改我在本群的昵称？']";
            var confirmRetry = Retry.WhileNull(() => this._Client.MainWindow.FindFirstByXPath(path), TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(200));
            if (confirmRetry.Success)
            {
                var confirmGroup = confirmRetry.Result.GetParent();
                var button = confirmGroup.FindFirstByXPath("/Group/Group/Button[@Name='修改']");
                if (button != null)
                {
                    button.Click();
                    this.CloseChatInfoPane();
                    RandomWait.Wait(900, 1500);
                    return Result.Ok();
                }
            }

            RandomWait.Wait(800, 1500);
            return Result.Fail("错误：修改群昵称失败!");
        }

        /// <summary>
        /// 改变群备注,群备注仅自己可见.
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <param name="newMemo">新备注</param>
        /// <returns>微信响应结果<see cref="Result"/></returns>
        public async Task<Result> ChangeChatGroupMemo(string groupName, string newMemo)
        {
            var find = await _Client.Conversations.Search(groupName);
            if (!find)
                return Result.Fail($"错误：没有发现groupName={groupName}的群");
            return await WeChatInvoker.Call(ChangeOwnerChatGroupMemoCore, newMemo);
        }

        /// <summary>
        /// 改变本聊天窗口的群备注,群聊备注仅自己可见.
        /// </summary>
        /// <param name="newMemo">新备注</param>
        /// <returns>微信响应结果<see cref="Result"/></returns>
        public async Task<Result> ChangeChatGroupMemo(string newMemo)
        {
            var headInfo = await this._Client.GetTitle();
            if (!headInfo.CanTalk() || headInfo.HeaderType != ChatType.群聊)
                return Result.Fail("错误：本窗口不是群聊窗口，修改群聊备注动作终止!");
            return await WeChatInvoker.Call(ChangeOwnerChatGroupMemoCore, newMemo);
        }


        private Result ChangeOwnerChatGroupMemoCore(UIA3Automation automation, string newMemo)
        {
            var rootPane = this._GetChatRootPane();
            var pane = rootPane.FindFirstByXPath("/Group[2]");
            if (pane == null)
                return Result.Fail("没有找到聊天信息的根Pane");
            using var bitmap = pane.Capture();
            using var mat = this._Client.OcrEngee.GetMatFromBitmap(bitmap);
            //扩大两倍好识别
            using var mat2 = new Mat();
            CvInvoke.Resize(mat, mat2, Size.Empty, 2, 2, Emgu.CV.CvEnum.Inter.Cubic);
            using var srcImg = mat2.ToBitmap();
            var ocrResult = this._Client.OcrEngee.Detect(srcImg, 0, mat2.Width > mat2.Height ? mat2.Width : mat2.Height, 0.5f, 0.3f, 1.5f, false, false, false);
            var item = ocrResult.TextBlocks.Where(x => x.Text.Trim().Equals("备注")).FirstOrDefault();
            if (item == null)
                return Result.Fail("OCR识别 备注 失败");
            var srcPoint = new Point((int)(item.BoxPoints[0].X / 2 + (item.BoxPoints[2].X - item.BoxPoints[0].X) / 4), item.BoxPoints[0].Y / 2 + (int)((item.BoxPoints[2].Y - item.BoxPoints[0].Y) / 4));
            var point = new Point(pane.BoundingRectangle.X + srcPoint.X, pane.BoundingRectangle.Y + srcPoint.Y);
            var baseStep = 25;
            var ratio = DpiHelper.GetScaleForWindow(this._Client.MainWindow.Properties.NativeWindowHandle);
            var destPoint = new Point(point.X + 90, point.Y + (int)(baseStep * ratio));
            Mouse.Position = destPoint;
            RandomWait.Wait(100, 300);
            SupperMouseKey.MoveTo(destPoint.Confusion(10, 0));
            RandomWait.Wait(300, 900);
            SupperMouseKey.LeftClick();
            RandomWait.Wait(300, 1200);
            SupperMouseKey.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
            RandomWait.Wait(100, 300);
            SupperMouseKey.TypeSimultaneously(VirtualKeyShort.BACK);
            ClipboardHelper.SetText(newMemo);
            RandomWait.Wait(300, 1200);
            SupperMouseKey.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);
            RandomWait.Wait(300, 1200);
            SupperMouseKey.Enter();
            RandomWait.Wait(300, 1200);
            this.CloseChatInfoPane();

            RandomWait.Wait(800, 1500);

            return Result.Ok();
        }
    }
}