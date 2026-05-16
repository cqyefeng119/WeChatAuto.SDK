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

namespace WeChatAuto.Components
{
    /// <summary>
    /// 聊天内容区发送者
    /// </summary>
    internal class Sender
    {
        private readonly AutoLogger<Sender> _logger;
        private UIThreadInvoker _uiThreadInvoker;
        private readonly IServiceProvider _serviceProvider;
        private WeChatClient _Client;
        private ChatContent content;
        /// <summary>
        /// 聊天内容区发送者构造函数
        /// </summary>
        internal Sender(WeChatClient client, UIThreadInvoker uiThreadInvoker, IServiceProvider serviceProvider, ChatContent content)
        {
            _uiThreadInvoker = uiThreadInvoker;
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<AutoLogger<Sender>>();
            this._Client = client;
            this.content = content;
        }

        /// <summary>
        /// 发起单人语音聊天
        /// </summary>
        /// <param name="who">好友昵称</param>
        public async Task SendVoiceChat(string who)
        {
            await WeChatInvoker.Call(SendVoiceChatCore, who);
        }

        internal void SendVoiceChatCore(UIA3Automation automation, string who)
        {
            if (string.IsNullOrWhiteSpace(who))
            {
                var chatInfo = _Client.ChatContent.ChatHeader.GetTitleCore(automation);
                if (!chatInfo.CanTalk())
                {
                    return;
                }
            }
            else
            {
                _Client.Conversations.SearchWhoCore(automation, who);
            }
            RandomWait.Wait(300, 1200);
            var root = this.content.Root;
            if (root == null)
                return;
            var button = root.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("语音通话").And(cf.ByAutomationId("voip_button"))))?.AsButton();
            if (button == null)
                return;
            button.DrawHighlightExt();
            // var point = button.BoundingRectangle.SafeRandomPoint();
            button.ClickEnhance(_Client.MainWindow);
            //等候语音通话窗口出现，最多等候2秒钟
            var windowResult = Retry.WhileNull(() => _Client.MainWindow.FindFirstByXPath("/Window[@Name='Weixin']/MenuItem[@Name='语音通话']"), timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200));
            if (windowResult.Success)
            {
                var menuItem = windowResult.Result.AsMenuItem();
                menuItem.DrawHighlightExt();
                menuItem.ClickEnhance(_Client.MainWindow);
            }
        }

        /// <summary>
        /// 发起单人视频聊天
        /// </summary>
        /// <param name="who">好友昵称,可以为空，如果为空，则发送到当前聊天窗口</param>
        public async Task SendVedioChat(string who)
        {
            await WeChatInvoker.Call(SendVedioChatCore, who);
        }

        internal void SendVedioChatCore(UIA3Automation automation, string who)
        {
            if (string.IsNullOrWhiteSpace(who))
            {
                var chatInfo = _Client.ChatContent.ChatHeader.GetTitleCore(automation);
                if (!chatInfo.CanTalk())
                {
                    return;
                }
            }
            else
            {
                _Client.Conversations.SearchWhoCore(automation, who);
            }
            RandomWait.Wait(300, 1200);
            var root = this.content.Root;
            if (root == null)
                return;
            var button = root.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("语音通话").And(cf.ByAutomationId("voip_button"))))?.AsButton();
            if (button == null)
                return;
            button.DrawHighlightExt();
            // var point = button.BoundingRectangle.SafeRandomPoint();
            button.ClickEnhance(_Client.MainWindow);
            //等候语音通话窗口出现，最多等候2秒钟
            var windowResult = Retry.WhileNull(() => _Client.MainWindow.FindFirstByXPath("/Window[@Name='Weixin']/MenuItem[@Name='视频通话']"), timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200));
            if (windowResult.Success)
            {
                var menuItem = windowResult.Result.AsMenuItem();
                menuItem.DrawHighlightExt();
                menuItem.ClickEnhance(_Client.MainWindow);
            }
        }

        /// <summary>
        /// 发起多人语音聊天，适用于群聊发起语音聊天
        /// </summary>
        /// <param name="who">群聊名称,可以为空，如果为空，则发送到当前聊天窗口</param>
        /// <param name="partner">参与者，好友昵称列表,必须是群聊成员</param>
        public async Task SendVoiceChats(string who, string[] partner)
        {
            await WeChatInvoker.Call(SendVoiceChatsCore, who, partner);
        }

        private void SendVoiceChatsCore(UIA3Automation automation, string who, string[] partner)
        {
            if (string.IsNullOrWhiteSpace(who))
            {
                //可能没有选择聊天对象，如果没有选择聊天对象，则不发送.
                // if (unSelectChatItem())
                //     return;
                var chatInfo = _Client.ChatContent.ChatHeader.GetTitleCore(automation);
                if (!chatInfo.CanTalk())
                {
                    return;
                }
            }
            else
            {
                _Client.Conversations.SearchWhoCore(automation, who);
            }
            RandomWait.Wait(300, 1200);
            var filter = partner.Where(u => u != _Client.NickName).ToList().ToArray();
            var root = this.content.Root;
            if (root == null)
                return;
            var button = root.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("语音通话").And(cf.ByAutomationId("voip_button"))))?.AsButton();
            if (button == null)
                return;
            button.DrawHighlightExt();
            // var point = button.BoundingRectangle.SafeRandomPoint();
            button.ClickEnhance(_Client.MainWindow);
            //等候语音通话窗口出现，最多等候2秒钟
            var windowResult = Retry.WhileNull(() => _Client.MainWindow.FindFirstByXPath("/Window[@Name='微信选择成员']"), timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200));
            if (windowResult.Success)
            {
                var selectWindow = windowResult.Result;
                selectWindow.DrawHighlightExt();
                var listBox = selectWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.List).And(cf.ByAutomationId("sp_to_select_contact_list")).And(cf.ByName("请勾选需要添加的联系人")))?.AsListBox();
                listBox?.DrawHighlightExt();
                if (listBox != null)
                {
                    var lastItemName = string.Empty;
                    var point = listBox.BoundingRectangle.SafeRandomPoint();
                    Mouse.Position = point;
                    var index = 0;
                    var oldItems = new AutomationElement[] { };
                    var compare = new AutomationElementComparer();
                    while (index < 2)
                    {
                        var items = listBox.FindAllChildren(cf => cf.ByControlType(ControlType.CheckBox));
                        var exceptItems = items.Except(oldItems, new AutomationElementComparer()).ToArray();
                        if (exceptItems.Length > 0)
                        {
                            oldItems = items;
                        }
                        else
                        {
                            break;
                        }
                        foreach (var item in items)
                        {
                            var name = item.Name;
                            if (filter.Contains(name))
                            {
                                var checkBox = item.AsCheckBox();
                                if (checkBox.IsPatternSupported(checkBox.Automation.PatternLibrary.TogglePattern))
                                {
                                    var pattern = checkBox.Patterns.Toggle.Pattern;
                                    if (pattern.ToggleState != ToggleState.On)
                                    {
                                        //可能有些项目超出底部可视范围了，需要滚动才能看到，滚动到该项目位置
                                        if (item.BoundingRectangle.Y + item.BoundingRectangle.Height > listBox.BoundingRectangle.Y + listBox.BoundingRectangle.Height)
                                        {
                                            Mouse.Position = point;
                                            Mouse.Scroll(-1);
                                            RandomWait.Wait(200, 500);
                                        }
                                        if (item.BoundingRectangle.Y < listBox.BoundingRectangle.Y)
                                        {
                                            Mouse.Position = point;
                                            Mouse.Scroll(1);
                                            RandomWait.Wait(200, 500);
                                        }
                                        item.DrawHighlightExt();
                                        var itemPoint = item.BoundingRectangle.SafeRandomPoint();
                                        Mouse.Position = itemPoint;
                                        Mouse.Click();
                                        RandomWait.Wait(200, 500);
                                    }
                                }
                            }
                        }
                        if (items.LastOrDefault() != null)
                        {
                            if (lastItemName.Equals(items.LastOrDefault().Name))
                            {
                                index++;
                                break;
                            }
                            else
                            {
                                index = 0;
                            }
                            lastItemName = items.LastOrDefault().Name;
                        }
                        else
                        {
                            break;
                        }
                        Mouse.Position = point;
                        Mouse.Scroll(-3);
                        RandomWait.Wait(50, 500);
                    }

                    //点击确定按钮
                    var path = @"/Window/Group/Group/Button[@AutomationId='confirm_btn'][@Name='完成']";
                    var confirmButtonResult = Retry.WhileNull(() => _Client.MainWindow.FindFirstByXPath(path)?.AsButton(), timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200));
                    if (confirmButtonResult.Success)
                    {
                        var confirmButton = confirmButtonResult.Result;
                        confirmButton.DrawHighlightExt();
                        confirmButton.ClickEnhance(_Client.MainWindow);
                    }
                }
            }
        }

        /// <summary>
        /// 发送语音消息
        /// </summary>
        /// <param name="who">好友昵称或群聊名称</param>
        /// <param name="filePath">语音文件路径</param>
        internal async Task SendVoiceMessage(string who, string filePath)
        {

        }
        /// <summary>
        /// 通过文本发送语音消息，需要下载whisper模型并配置好环境，文本转语音后发送
        /// </summary>
        /// <param name="who">好友昵称或群聊名称</param>
        /// <param name="message">要转换为语音的文本消息</param>
        /// <param name="textToVoiceFunc">文本转语音的函数，参数为要转换的文本，返回值为生成的语音文件路径,如果不提供，则使用默认的文本转语音功能(默认使用whisper)，当然你也可以提供自己的实现（如连接到外部平台的API）</param>
        /// <returns></returns>
        internal async Task SendVoiceMessage(string who, string message, Func<string, Task<string>> textToVoiceFunc = default)
        {

        }


        internal TextBox _GetContentArea(AutomationElement root)
        {
            var text = root.FindFirstByXPath(@"/Custom/Group/Group/Group/Group/Edit[@AutomationId='chat_input_field']").AsTextBox();
            return text;
        }
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="who">被发送消息的好友名称/群聊名称</param>
        /// <param name="message">文本消息内容</param>
        /// <param name="atUser">@的好友，可以多个，在群聊中使用</param>
        /// <returns></returns>
        public async Task SendMessage(string who, string message, OneOf<string, string[], List<string>> atUser = default)
        {
            await WeChatInvoker.Call(SendMessageExt, who, message, atUser);
        }
        private void SendMessageExt(UIA3Automation automation, string who, string message, OneOf<string, string[], List<string>> atUser = default)
        {
            if (string.IsNullOrWhiteSpace(who))
            {
                //可能没有选择聊天对象，如果没有选择聊天对象，则不发送.
                // if (unSelectChatItem())
                //     return;
                var chatInfo = _Client.ChatContent.ChatHeader.GetTitleCore(automation);
                if (!chatInfo.CanTalk())
                {
                    return;
                }
            }
            else
            {
                _Client.Conversations.SearchWhoCore(automation, who);
            }
            RandomWait.Wait(100, 600);
            SendMessageCore(automation, message, atUser);
        }
        public async Task SendFile(string who, string[] files)
        {
            await WeChatInvoker.Call(SendFileExt, who, files);
        }
        private void SendFileExt(UIA3Automation automation, string who, string[] files)
        {
            if (string.IsNullOrWhiteSpace(who))
            {
                //可能没有选择聊天对象，如果没有选择聊天对象，则不发送.
                // if (unSelectChatItem())
                //     return;
                var chatInfo = _Client.ChatContent.ChatHeader.GetTitleCore(automation);
                if (!chatInfo.CanTalk())
                {
                    return;
                }
            }
            else
            {
                _Client.Conversations.SearchWhoCore(automation, who);
            }
            RandomWait.Wait(100, 600);
            SendFileCore(automation, files);
        }
        /// <summary>
        /// 检查是否是选中状态.
        /// </summary>
        /// <returns></returns>
        internal bool unSelectChatItem() => !this._Client.Conversations.CheckSelectState();

        /// <summary>
        /// 发送消息,给当前窗口发送消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="atUser">被@的好友</param>
        public async Task SendMessage(string message, OneOf<string, string[], List<string>> atUser = default)
        {
            await WeChatInvoker.Call(SendMessageCore, message, atUser);
        }

        internal void SendMessageCore(UIA3Automation automation, string message, OneOf<string, string[], List<string>> atUser)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            var root = this.content.Root;
            if (root == null)
                return;
            root.DrawHighlightExt();
            var ContentArea = _GetContentArea(root);
            if (ContentArea == null)
                return;
            ContentArea.DrawHighlightExt();


            if (atUser.Value == default)
            {
                _Client.MainWindow.Focus();
                __InputText(ContentArea, message);
            }
            else
            {
                var atUserList = atUser.IsT0 ? new List<string> { atUser.AsT0 } : atUser.IsT1 ?
                    atUser.AsT1.ToList() : atUser.AsT2;
                __AtUserInputText(atUserList, ContentArea, message);
            }
        }

        private void __AtUserInputText(List<string> atUsers, TextBox textBox, string message)
        {
            textBox.Focus();
            Clipboard.SetText(message);
            var point = textBox.BoundingRectangle.SafeRandomPoint();
            Mouse.Position = point;
            Mouse.Click();
            __AtUserList(atUsers, textBox);
            textBox.Focus();
            Clipboard.SetText(message);
            RandomWait.Wait(50, 300);
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);
            RandomWait.Wait(50, 800);
            Keyboard.TypeSimultaneously(VirtualKeyShort.ENTER);
        }

        private void __AtUserList(List<string> atUsers, TextBox textBox)
        {
            RandomWait.Wait(50, 300);
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
            RandomWait.Wait(50, 300);
            Keyboard.TypeSimultaneously(VirtualKeyShort.BACK);
            RandomWait.Wait(50, 300);

            var path = "/Window[@Name='Weixin']/Group/List[@AutomationId='chat_mention_list']";
            foreach (var name in atUsers)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                var point = textBox.BoundingRectangle.SafeRandomPoint();
                Mouse.Position = point;
                Mouse.Click();
                Keyboard.Type("@");
                var popWinResult = Retry.WhileNull(() => _Client.MainWindow.FindFirstByXPath(path),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200));
                if (popWinResult.Success)
                {
                    var listBox = popWinResult.Result.AsListBox();
                    listBox.DrawHighlightExt();
                    point = listBox.BoundingRectangle.SafeRandomPoint();
                    Mouse.Position = point;
                    var isEnd = false;
                    var lastItemName = string.Empty;
                    while (!isEnd)
                    {
                        var children = listBox.FindAllChildren(cf => cf.ByControlType(ControlType.ListItem));
                        var item = children.FirstOrDefault(u => u.Name.Equals(name));
                        if (item != null)
                        {
                            if (item.BoundingRectangle.Y + item.BoundingRectangle.Height > listBox.BoundingRectangle.Y + listBox.BoundingRectangle.Height)
                            {
                                Mouse.Scroll(-1);
                            }
                            item.DrawHighlightExt();
                            point = item.BoundingRectangle.SafeRandomPoint();
                            Mouse.MoveTo(point);
                            Mouse.Click();
                            break;
                        }
                        if (children.LastOrDefault() != null)
                        {
                            if (lastItemName.Equals(children.LastOrDefault().Name))
                            {
                                Keyboard.Type(VirtualKeyShort.ESC);
                                RandomWait.Wait(200, 800);
                                Keyboard.Type(VirtualKeyShort.BACK);
                                break;
                            }
                            lastItemName = children.LastOrDefault().Name;
                        }
                        else
                        {
                            break;
                        }
                        Mouse.Scroll(-2);
                        RandomWait.Wait(50, 500);
                    }
                }
                else
                {
                    Keyboard.Type(virtualKeys: VirtualKeyShort.BACK);
                }
                RandomWait.Wait(500, 1200);
            }
        }

        private void __InputText(TextBox textBox, string message)
        {
            textBox.Focus();
            Clipboard.SetText(message);
            var point = textBox.BoundingRectangle.SafeRandomPoint();
            Mouse.Position = point;
            Mouse.Click();
            RandomWait.Wait(50, 300);
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
            RandomWait.Wait(50, 300);
            Keyboard.TypeSimultaneously(VirtualKeyShort.BACK);
            RandomWait.Wait(50, 300);
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);
            RandomWait.Wait(50, 800);
            Keyboard.TypeSimultaneously(VirtualKeyShort.ENTER);
        }

        /// <summary>
        /// 发送文件,给当前窗口发送
        /// </summary>
        /// <param name="files">文件路径列表</param>
        public async Task SendFile(string[] files)
        {
            await WeChatInvoker.Call(SendFileCore, files);
        }

        internal void SendFileCore(UIA3Automation automation, string[] files)
        {
            var root = this.content.Root;
            if (root == null)
                return;
            var textBox = _GetContentArea(root);
            if (textBox == null)
                return;

            textBox.Focus();
            var point = textBox.BoundingRectangle.SafeRandomPoint();
            Mouse.Position = point;
            Mouse.Click();

            _Client.MainWindow.SilencePasteSimple(files, textBox);
            RandomWait.Wait(300, 1200);
            Keyboard.Type(VirtualKeyShort.ENTER);
        }

        /// <summary>
        /// 发送表情
        /// </summary>
        /// <param name="who">被发送消息的好友名称/群聊名称</param>
        /// <param name="emoji">表情名称或者描述或者索引</param>
        /// <param name="atUserList">被@的好友列表</param>
        public async Task SendEmoji(string who, OneOf<int, string> emoji, List<string> atUserList = null)
        {
            var message = "";
            emoji.Switch(
                (int emojiId) =>
                {
                    message = EmojiListHelper.Items.FirstOrDefault(item => item.Index == emojiId)?.Value ?? EmojiListHelper.Items[0].Value;
                },
                (string emojiName) =>
                {
                    message = emojiName;
                    if (!(message.StartsWith("[") && message.EndsWith("]")))
                    {
                        message = EmojiListHelper.Items.FirstOrDefault(item => item.Description == emojiName)?.Value ?? message;
                    }
                }
            );
            await this.SendMessage(who, message, atUserList);
        }

        /// <summary>
        /// 当前窗口的Sender区域点击，以获得焦点，可以取消系统的消息提醒或者关闭右侧Pane
        /// </summary>
        /// <returns></returns>
        public async Task FcouseSenderInput()
        {
            await WeChatInvoker.Call(FcouseSenderCore);
        }

        internal void FcouseSenderCore(UIA3Automation automation)
        {
            var root = this.content.Root;
            if (root == null)
                return;
            var input = root.FindFirstDescendant(cf => cf.ByAutomationId("chat_input_field").And(cf.ByClassName("mmui::ChatInputField").And(cf.ByControlType(ControlType.Edit)))).AsTextBox();
            if (input == null)
                return;
            //检查是否有右侧边Panel.
            var clickRoot = input.GetParent().GetParent().GetParent().GetParent();
            clickRoot.DrawHighlightExt();
            var rightPane = root.FindFirstDescendant(cf => cf.ByClassName("mmui::ChatRoomMemberInfoView").And(cf.ByControlType(ControlType.Group)));
            System.Drawing.Point point = System.Drawing.Point.Empty;
            if (rightPane != null)
            {
                rightPane.DrawHighlightExt();
                Rectangle rect = new Rectangle(clickRoot.BoundingRectangle.X, clickRoot.BoundingRectangle.Y,
                clickRoot.BoundingRectangle.Width - rightPane.BoundingRectangle.Width, clickRoot.BoundingRectangle.Height);
                point = rect.SafeRandomPoint();
            }
            else
            {
                rightPane = root.FindFirstDescendant(cf => cf.ByClassName("mmui::XView").And(cf.ByControlType(ControlType.Group).And(cf.ByAutomationId("single_chat_info_view"))));
                if (rightPane != null)
                {
                    rightPane.DrawHighlightExt();
                    Rectangle rect = new Rectangle(clickRoot.BoundingRectangle.X, clickRoot.BoundingRectangle.Y,
                    clickRoot.BoundingRectangle.Width - rightPane.BoundingRectangle.Width, clickRoot.BoundingRectangle.Height);
                    point = rect.SafeRandomPoint();
                }
                else
                {
                    point = clickRoot.BoundingRectangle.SafeRandomPoint();
                }
            }
            //测试.
            //Mouse.MoveTo(point);
            Mouse.Position = point;
            Mouse.LeftClick();
            RandomWait.Wait(100,800);
        }
    }
}