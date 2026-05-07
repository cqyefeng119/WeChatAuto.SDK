using System;
using FlaUI.Core.AutomationElements;
using Microsoft.Extensions.DependencyInjection;
using WeAutoCommon.Enums;
using WeAutoCommon.Models;
using System.Text.RegularExpressions;
using WeChatAuto.Extentions;
using WeChatAuto.Utils;
using WeAutoCommon.Utils;

namespace WeChatAuto.Components
{
    /// <summary>
    /// 聊天内容区标题区
    /// </summary>
    public class ChatHeader
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly AutoLogger<ChatContent> _logger;
        private UIThreadInvoker _uiMainThreadInvoker;
        private WeChatClient _Client;

        /// <summary>
        /// 聊天内容区标题区构造函数
        /// </summary>
        public ChatHeader(WeChatClient client,IServiceProvider serviceProvider, UIThreadInvoker _uiMainThreadInvoker)
        {
            this._uiMainThreadInvoker = _uiMainThreadInvoker;
            _logger = serviceProvider.GetRequiredService<AutoLogger<ChatContent>>();
            _serviceProvider = serviceProvider;
            this._Client = client;
        }
        /// <summary>
        /// 聊天标题
        /// </summary>
        public HeaderInfo Title
        {
            get
            {
                // var infoResult = _uiMainThreadInvoker.Run(automation =>
                // {
                //     HeaderInfo info = new HeaderInfo()
                //     {
                //         Title = "",
                //         HeaderType = ChatType.其他,
                //     };
                //     var result = TryCheckFriend(info);
                //     if (!result)
                //     {
                //         result = TryCheckSubscription(info);
                //         if (!result)
                //         {
                //             result = TryCheckAnother(info);
                //         }
                //     }
                //     return info;
                // }).GetAwaiter().GetResult();
                // return infoResult;
                return null;
            }
        }

        /// <summary>
        /// 重写ToString方法
        /// </summary>
        /// <returns>聊天标题和聊天信息按钮名称</returns>
        public override string ToString()
        {
            return $"Title: {Title.Title} - HeaderType:{Title.ChatNumber.ToString()} - ChatNumber:{Title.ChatNumber}";
        }
    }
}