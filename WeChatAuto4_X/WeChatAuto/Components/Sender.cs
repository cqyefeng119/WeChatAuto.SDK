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
        internal List<(ChatBoxToolBarType type, Button button)> ToolBarButtons => GetToolBarButtons();
        private WeChatClient _Client;
        private ChatContent content;
        /// <summary>
        /// 聊天内容区发送者构造函数
        /// </summary>
        public Sender(WeChatClient client, UIThreadInvoker uiThreadInvoker, IServiceProvider serviceProvider, ChatContent content)
        {
            _uiThreadInvoker = uiThreadInvoker;
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<AutoLogger<Sender>>();
            this._Client = client;
            this.content = content;
        }

        /// <summary>
        /// 获取工具栏按钮
        /// </summary>
        /// <param name="type">工具栏按钮类型</param>
        /// <returns>工具栏按钮</returns>
        public Button GetToolBarButton(ChatBoxToolBarType type)
        {
            var toolBarButtons = GetToolBarButtons();
            return toolBarButtons.FirstOrDefault(btn => btn.type == type).button;
        }
        /// <summary>
        /// 发起单个语音聊天
        /// </summary>
        internal void SendVoiceChat()
        {
            // var voiceChatButton = GetToolBarButton(ChatBoxToolBarType.语音聊天);
            // if (voiceChatButton == null)
            // {
            //     _logger.Error("无法找到语音聊天按钮，无法发起单个语音聊天");
            //     return;
            // }
            // voiceChatButton.DrawHighlightExt(_uiThreadInvoker);
            // RandomWait.Wait(300, 800);
            // voiceChatButton.ClickEnhance(_Window);
        }
        /// <summary>
        /// 发起多个语音聊天
        /// </summary>
        /// <param name="whos">好友昵称列表</param>
        internal void SendVoiceChats(string[] whos)
        {
            // var voiceChatButton = GetToolBarButton(ChatBoxToolBarType.语音聊天);
            // if (voiceChatButton == null)
            // {
            //     _logger.Error("无法找到语音聊天按钮，无法发起多个语音聊天");
            //     return;
            // }
            // voiceChatButton.DrawHighlightExt(_uiThreadInvoker);
            // RandomWait.Wait(300, 800);
            // _Window.Focus();
            // voiceChatButton.ClickEnhance(_Window);
            // _AddChatGroupMember(whos);
        }

        /// <summary>
        /// 发起直播,适用于群聊中发起直播，单个好友没有直播功能
        /// </summary>
        internal void SendLiveStreaming()
        {
            // var liveStreamingButton = GetToolBarButton(ChatBoxToolBarType.直播);
            // liveStreamingButton.DrawHighlightExt(_uiThreadInvoker);
            // if (liveStreamingButton == null)
            // {
            //     _logger.Error("无法找到直播按钮，无法发起直播");
            //     return;
            // }
            // _Window.Focus();
            // RandomWait.Wait(300, 800);
            // liveStreamingButton.ClickEnhance(_Window);
        }
        /// <summary>
        /// 发起视频聊天
        /// </summary>
        internal void SendVideoChat()
        {
            // var videoChatButton = GetToolBarButton(ChatBoxToolBarType.视频聊天);
            // if (videoChatButton == null)
            // {
            //     _logger.Error("无法找到视频聊天按钮，无法发起视频聊天");
            //     return;
            // }
            // videoChatButton.DrawHighlightExt(_uiThreadInvoker);
            // RandomWait.Wait(300, 800);
            // _Window.Focus();
            // RandomWait.Wait(300, 1500);
            // videoChatButton.ClickEnhance(_Window);
        }
        /// <summary>
        /// 获取工具栏按钮
        /// </summary>
        /// <returns>工具栏按钮</returns>
        internal List<(ChatBoxToolBarType type, Button button)> GetToolBarButtons()
        {
            // var toolBarRoot = _uiThreadInvoker.Run(automation => _SenderRoot.FindFirstDescendant(cf => cf.ByControlType(ControlType.ToolBar))).GetAwaiter().GetResult();
            // DrawHightlightHelper.DrawHightlight(toolBarRoot, _uiThreadInvoker);
            // var buttons = _uiThreadInvoker.Run(automation => toolBarRoot.FindAllChildren(cf => cf.ByControlType(ControlType.Button))).GetAwaiter().GetResult();
            // List<Button> buttonList = buttons.Select(btn => btn.AsButton()).ToList();
            // List<(ChatBoxToolBarType type, Button button)> toolBarButtons = new List<(ChatBoxToolBarType type, Button button)>
            // {
            //     (ChatBoxToolBarType.表情, buttonList.FirstOrDefault(btn => btn.Name.Contains(WeChatConstant.WECHAT_CHAT_BOX_EMOTION))),
            //     (ChatBoxToolBarType.发送文件, buttonList.FirstOrDefault(btn => btn.Name.Contains(WeChatConstant.WECHAT_CHAT_BOX_SEND_FILE))),
            //     (ChatBoxToolBarType.截图, buttonList.FirstOrDefault(btn => btn.Name.Contains(WeChatConstant.WECHAT_CHAT_BOX_SCREENSHOT))),
            //     (ChatBoxToolBarType.聊天记录, buttonList.FirstOrDefault(btn => btn.Name.Contains(WeChatConstant.WECHAT_CHAT_BOX_CHAT_RECORD))),
            //     (ChatBoxToolBarType.直播, buttonList.FirstOrDefault(btn => btn.Name.Contains(WeChatConstant.WECHAT_CHAT_BOX_LIVE))),
            //     (ChatBoxToolBarType.语音聊天, buttonList.FirstOrDefault(btn => btn.Name.Equals("语音聊天"))),
            //     (ChatBoxToolBarType.视频聊天, buttonList.FirstOrDefault(btn => btn.Name.Equals("视频聊天")))
            // };
            // return toolBarButtons;
            return null;
        }

        /// <summary>
        /// 获取发送按钮
        /// </summary>
        /// <returns>发送按钮</returns>
        internal Button _GetSendButton(AutomationElement root)
        {
            var button = root.FindFirstByXPath(@"/Custom/Group/Group/Group/ToolBar[@AutomationId='tool_bar_accessible']/Group/Group/Button[@Name='发送']").AsButton();
            return button;
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
        internal async Task SendMessage(string who, string message, OneOf<string, string[], List<string>> atUser = default)
        {
            await _uiThreadInvoker.Run(automation =>
            {
                if (string.IsNullOrWhiteSpace(who))
                {
                    //可能没有选择聊天对象，如果没有选择聊天对象，则不发送.
                    if (unSelectChatItem())
                        return;
                }
                else
                {
                    _Client.Conversations.SearchWhoCore(who);
                }
                RandomWait.Wait(100, 600);
                SendMessageCore(message, atUser);
            });
        }
        internal async Task SendFile(string who, string[] files)
        {
            await _uiThreadInvoker.Run(automation =>
            {
                if (string.IsNullOrWhiteSpace(who))
                {
                    //可能没有选择聊天对象，如果没有选择聊天对象，则不发送.
                    if (unSelectChatItem())
                        return;
                }
                else
                {
                    _Client.Conversations.SearchWhoCore(who);
                }
                RandomWait.Wait(100, 600);
                SendFileCore(files);
            });
        }
        /// <summary>
        /// 检查是否是选中状态.
        /// </summary>
        /// <returns></returns>
        private bool unSelectChatItem() => !this._Client.Conversations.CheckSelectState();

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="atUser">被@的好友</param>
        internal async Task SendMessage(string message, OneOf<string, string[], List<string>> atUser = default)
        {
            await _uiThreadInvoker.Run(automation =>
            {
                SendMessageCore(message, atUser);
            });
        }

        internal void SendMessageCore(string message, OneOf<string, string[], List<string>> atUser)
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
        /// 发送文件
        /// </summary>
        /// <param name="files">文件路径列表</param>
        internal async Task SendFile(string[] files)
        {
            await _uiThreadInvoker.Run(automation =>
            {
                SendFileCore(files);
            }).ConfigureAwait(false);
        }

        internal void SendFileCore(string[] files)
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
        internal async Task SendEmoji(string who, OneOf<int, string> emoji, List<string> atUserList = null)
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
            await this.SendMessage(who,message, atUserList);
        }
    }
}