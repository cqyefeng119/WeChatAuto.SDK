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
                return this._Client.Conversations.ConversationRoot.GetParent().GetParent().GetParent().GetParent().GetParent().GetParent();
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
    }
}