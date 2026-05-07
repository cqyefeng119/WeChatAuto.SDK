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
    public class ChatContent
    {
        private readonly AutoLogger<ChatContent> _logger;
        private UIThreadInvoker _uiMainThreadInvoker;
        private readonly IServiceProvider _serviceProvider;
        private volatile bool _disposed = false;
        private WeChatClient _Client;



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
        }
    }
}