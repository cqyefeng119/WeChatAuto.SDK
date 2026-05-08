using System.Linq;
using System.Text.RegularExpressions;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using WeAutoCommon.Enums;
using WeAutoCommon.Interface;
using WeChatAuto.Utils;
using WeAutoCommon.Utils;
using System;
using Microsoft.Extensions.DependencyInjection;
using FlaUI.Core.Tools;
using System.Drawing;
using FlaUI.Core.Capturing;
using WeChatAuto.Extentions;
using OneOf;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WeChatAuto.Components
{
    internal class ChatContent
    {
        private readonly AutoLogger<ChatContent> _logger;
        private UIThreadInvoker _uiMainThreadInvoker;
        private readonly IServiceProvider _serviceProvider;
        private WeChatClient _Client;
        private ChatHeader _Header;
        private MessageBubbleList _MessageList;
        private Sender _Sender;

        internal Sender Sender => _Sender;
        internal MessageBubbleList MessageBubbleList => _MessageList;
        internal ChatHeader ChatHeader => _Header;

        internal AutomationElement Root
        {
            get
            {
                var path = @"/Group/Custom/Group/Group/Group/Custom/Custom/Custom/Group/Custom/Custom/Group[@AutomationId='chat_message_page']";
                var itemResult = Retry.WhileNull(() => _Client.MainWindow.FindFirstByXPath(path), timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200));
                return itemResult.Success ? itemResult.Result : null;
            }
        }

        /// <summary>
        /// 聊天标题
        /// </summary>
        public string FullTitle => "";
        public ChatContent(WeChatClient client, UIThreadInvoker uiThreadInvoker, IServiceProvider serviceProvider)
        {
            this._Client = client;
            _logger = serviceProvider.GetRequiredService<AutoLogger<ChatContent>>();
            _uiMainThreadInvoker = uiThreadInvoker;
            _serviceProvider = serviceProvider;
            _Sender = new Sender(this._Client, _uiMainThreadInvoker, serviceProvider, this);
            _Header = new ChatHeader(this._Client, serviceProvider, _uiMainThreadInvoker, this);
            _MessageList = new MessageBubbleList(this._Client, _uiMainThreadInvoker, this, serviceProvider);
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="who">好友或者群聊的名称</param>
        /// <param name="message">消息内容</param>
        /// <param name="atUser">被@的好友</param>
        internal async Task SendMessage(string who, string message, OneOf<string, string[], List<string>> atUser = default)
        {
            await Sender.SendMessage(who, message, atUser);
        }

        /// <summary>
        /// 发送文件
        /// </summary>
        /// <param name="who">好友/群聊，可以为空,如果为空，则发送到当前聊天窗口</param>
        /// <param name="files">文件路径列表</param>
        internal async Task SendFile(string who, string[] files) => await Sender.SendFile(who, files);

        /// <summary>
        /// 发送表情
        /// </summary>
        /// <param name="who">好友或者群聊的名称</param>
        /// <param name="emoji">表情名称或者描述或者索引</param>
        /// <param name="atUserList">被@的好友列表</param>
        internal async Task SendEmoji(string who, OneOf<int, string> emoji, List<string> atUserList = null) => await Sender.SendEmoji(who, emoji, atUserList);

        /// <summary>
        /// 发起单人语音聊天
        /// </summary>
        /// <param name="who">好友昵称,可以为空，如果为空，则发送到当前聊天窗口</param>
        internal async Task SendVoiceChat(string who) => await Sender.SendVoiceChat(who);

        /// <summary>
        /// 发起单人视频聊天
        /// </summary>
        /// <param name="who">好友昵称,可以为空，如果为空，则发送到当前聊天窗口</param>
        internal async Task SendVedioChat(string who) => await Sender.SendVedioChat(who);

        /// <summary>
        /// 发起多人语音聊天，适用于群聊发起语音聊天
        /// </summary>
        /// <param name="who">群聊名称,可以为空，如果为空，则发送到当前聊天窗口</param>
        /// <param name="partner">参与者，好友昵称列表,必须是群聊成员</param>
        internal async Task SendVoiceChats(string who, string[] partner) => await Sender.SendVoiceChats(who, partner);
    }
}