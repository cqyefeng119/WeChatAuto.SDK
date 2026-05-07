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

namespace WeChatAuto.Components
{
    /// <summary>
    /// 聊天内容区发送者
    /// </summary>
    internal class Sender
    {
        private readonly AutoLogger<Sender> _logger;
        private UIThreadInvoker _uiThreadInvoker;
        public TextBox ContentArea => GetContentArea();
        private readonly IServiceProvider _serviceProvider;
        public List<(ChatBoxToolBarType type, Button button)> ToolBarButtons => GetToolBarButtons();
        public Button SendButton => GetSendButton();
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

        public string FullTitle => "";

        public void Focuse()
        {

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
        public void SendVoiceChat()
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
        public void SendVoiceChats(string[] whos)
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
        public void SendLiveStreaming()
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
        public void SendVideoChat()
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
        public List<(ChatBoxToolBarType type, Button button)> GetToolBarButtons()
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
        /// 获取输入框
        /// </summary>
        /// <returns>输入框</returns>
        public TextBox GetContentArea()
        {
            //     var contentArea = _uiThreadInvoker.Run(automation => _SenderRoot.FindFirstDescendant(cf => cf.ByControlType(ControlType.Edit))).GetAwaiter().GetResult().AsTextBox();
            //     return contentArea;
            return null;
        }
        /// <summary>
        /// 获取发送按钮
        /// </summary>
        /// <returns>发送按钮</returns>
        public Button GetSendButton()
        {
            // var sendButton = _uiThreadInvoker.Run(automation => _SenderRoot.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByText(WeChatConstant.WECHAT_CHAT_BOX_CONTENT_SEND)))).GetAwaiter().GetResult().AsButton();
            // DrawHightlightHelper.DrawHightlight(sendButton, _uiThreadInvoker);
            // return sendButton;
            return null;
        }
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="atUserList">被@的好友列表</param>
        private void SendMessage(string message, List<string> atUserList = null)
        {
            var root = this.content.Root;
            root.DrawHighlightExt();
            // if (atUserList == null || atUserList.Count == 0)
            // {
            //     _WxWindow.SilenceEnterText(ContentArea, message);
            //     Thread.Sleep(500);
            //     var button = SendButton;
            //     _WxWindow.SilenceClickExt(button);
            // }
            // else
            // {
            //     this._AtUserActionCore(atUserList);
            //     RandomWait.Wait(300, 800);
            //     Keyboard.Press(VirtualKeyShort.END);
            //     RandomWait.Wait(100, 500);
            //     _WxWindow.SilenceEnterText(ContentArea, message);
            //     RandomWait.Wait(300, 800);
            //     var button = SendButton;
            //     button.ClickEnhance(_WxWindow.SelfWindow);
            // }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="atUser">被@的好友</param>
        public void SendMessage(string message, OneOf<string, string[], List<string>> atUser = default)
        {
            // var atUserList = new List<string>();
            // if (atUser.Value != default)
            // {
            //     atUser.Switch(
            //         (string user) =>
            //         {
            //             if (!string.IsNullOrWhiteSpace(user))
            //             {
            //                 atUserList.Add(user);
            //             }
            //         },
            //         (string[] atUsers) =>
            //         {
            //             atUserList.AddRange(atUsers);
            //         },
            //         (List<string> atUsers) =>
            //         {
            //             atUserList = atUsers;
            //         }
            //     );
            // }
            // this.SendMessage(message, atUserList);
        }

        /// <summary>
        /// 粘贴图片等文件到输入框
        /// </summary>
        public void PasteImageFiles()
        {
            // _Window.Focus();
            // TextBox textBox = ContentArea;
            // textBox.Focus();
            // textBox.ClickEnhance(_WxWindow.SelfWindow);
            // Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);
            // RandomWait.Wait(300, 1500);
            // var button = GetSendButton();
            // button.ClickEnhance(_WxWindow.SelfWindow);
        }

        /// <summary>
        /// 发送文件
        /// </summary>
        /// <param name="files">文件路径列表</param>
        public void SendFile(string[] files)
        {
            // _WxWindow.SilencePasteSimple(files, ContentArea);

            // var button = SendButton;
            // _WxWindow.SilenceClickExt(button);
        }
        /// <summary>
        /// 发送表情
        /// </summary>
        /// <param name="emoji">表情名称或者描述或者索引</param>
        /// <param name="atUserList">被@的好友列表</param>
        public void SendEmoji(OneOf<int, string> emoji, List<string> atUserList = null)
        {
            // var message = "";
            // emoji.Switch(
            //     (int emojiId) =>
            //     {
            //         message = EmojiListHelper.Items.FirstOrDefault(item => item.Index == emojiId)?.Value ?? EmojiListHelper.Items[0].Value;
            //     },
            //     (string emojiName) =>
            //     {
            //         message = emojiName;
            //         if (!(message.StartsWith("[") && message.EndsWith("]")))
            //         {
            //             message = EmojiListHelper.Items.FirstOrDefault(item => item.Description == emojiName)?.Value ?? message;
            //         }
            //     }
            // );
            // this.SendMessage(message, atUserList);
        }
    }
}