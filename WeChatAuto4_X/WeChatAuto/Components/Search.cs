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
using WeChatAuto.Models;
using WeAutoCommon.Models;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Reflection.Metadata.Ecma335;
using System.Windows.Media.Imaging;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace WeChatAuto.Components
{
    /// <summary>
    /// 聊天内容区发送者
    /// </summary>
    public class Search
    {
        private readonly AutoLogger<Search> _logger;
        private UIThreadInvoker _uiThreadInvoker;
        private readonly IServiceProvider _serviceProvider;
        private WeChatClient _Client;
        private ChatContent content;
        /// <summary>
        /// 聊天内容区发送者构造函数
        /// </summary>
        internal Search(WeChatClient client, UIThreadInvoker uiThreadInvoker, IServiceProvider serviceProvider, ChatContent content)
        {
            _uiThreadInvoker = uiThreadInvoker;
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<AutoLogger<Search>>();
            this._Client = client;
            this.content = content;
        }
    }
}
