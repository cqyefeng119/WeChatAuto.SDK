using System.Collections.Generic;
using FlaUI.Core.AutomationElements;
using System.Linq;
using WeAutoCommon.Utils;
using FlaUI.Core.Definitions;
using WeAutoCommon.Models;
using WeAutoCommon.Enums;
using System;
using FlaUI.Core.Tools;
using FlaUI.Core.Conditions;
using System.Text.RegularExpressions;
using WeChatAuto.Utils;
using WeChatAuto.Extentions;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using WeChatAuto.Services;
using WeAutoCommon.Simulator;
using FlaUI.UIA3;
using System.Threading.Tasks;

namespace WeChatAuto.Components
{
    /// <summary>
    /// 会话列表
    /// </summary>
    public class ConversationList
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly AutoLogger<ConversationList> _logger;
        private UIThreadInvoker _uiThreadInvoker;
        private WeChatClient _Client;
        private ListBox ConversationRoot => _GetConversationRoot();   //会话列表根结点的查找方法

        private readonly List<string> _TitleTypeList = new List<string> {
            WeChatConstant.WECHAT_CONVERSATION_WX_TEAM,
            WeChatConstant.WECHAT_CONVERSATION_SERVICE_NOTICE,
            WeChatConstant.WECHAT_CONVERSATION_WX_PAY,
            WeChatConstant.WECHAT_CONVERSATION_TX_NEWS,
            WeChatConstant.WECHAT_CONVERSATION_SUBSCRIPTION,
            WeChatConstant.WECHAT_CONVERSATION_FILE_TRANSFER,
            WeChatConstant.WECHAT_CONVERSATION_COLLAPSED_GROUP
        };
        private readonly string _titleSuffix = WeChatConstant.WECHAT_SESSION_BOX_HAS_TOP;  //已置顶前缀
        public ConversationList(WeChatClient client, UIThreadInvoker uiThreadInvoker, IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<AutoLogger<ConversationList>>();
            _uiThreadInvoker = uiThreadInvoker;
            _Client = client;
            _serviceProvider = serviceProvider;
        }
        /// <summary>
        /// 获取会话列表可见会话
        /// 会话信息包含：会话名称、会话类型、会话状态、会话时间、会话未读消息数、会话头像<see cref="Conversation"/>
        /// </summary>
        /// <returns>返回<see cref="Conversation"/>列表</returns>
        public List<Conversation> GetVisibleConversations()
        {
            //var items = _GetVisibleConversatItems();
            List<Conversation> conversations = new List<Conversation>();
            // foreach (var item in items)
            // {
            //     Conversation conversation = new Conversation();
            //     conversation.ConversationTitle = _GetConversationTitle(item, conversation);
            //     conversation.IsTop = _GetConversationIsTop(item);
            //     conversation.ConversationType = _GetConversationType(conversation.ConversationTitle);
            //     conversation.ConversationContent = _GetConversationContent(item);
            //     conversation.IsCompanyGroup = _IsCompanyGroup(item);
            //     conversation.ImageButton = _GetConversationImageButton(item);
            //     conversation.HasNotRead = _GetConversationHasNotRead(item);
            //     conversation.Time = _GetConversationTime(item);
            //     conversation.IsDoNotDisturb = _IsDoNotDisturb(item);
            //     conversations.Add(conversation);
            // }
            return conversations;
        }
        /// <summary>
        /// 获取会话列表所有会话的名称
        /// 考虑到效率，只返回名称列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllConversations()
        {
            var items = new List<string>();
            return items;
        }
        /// <summary>
        /// 定位会话
        /// 定位会话的用途：可以将会话列表滚动到指定会话的位置，使指定会话可见
        /// </summary>
        /// <param name="title">会话标题</param>
        /// <returns>如果找到会话，则返回true，否则返回false</returns>
        public async Task<bool> LocateConversation(string title)
        {
            return await _uiThreadInvoker.Run(automation =>
            {
                return LocateConversationCore(title, automation);
            });
        }

        private bool LocateConversationCore(string title, UIA3Automation automation)
        {
            _ScrollListBox();
            return false;
        }

        /// <summary>
        /// 点击会话
        /// </summary>
        /// <param name="title">会话标题</param>
        public void ClickConversation(string title)
        {
            // var root = GetConversationRoot();
            // var items = _uiThreadInvoker.Run(automation => root.FindAllChildren(cf => cf.ByControlType(ControlType.ListItem)).ToList()).GetAwaiter().GetResult();
            // var item = items.FirstOrDefault(c => (c.Name.Equals(title) || c.Name.Equals(title + _titleSuffix)));
            // if (item != null)
            // {
            //     DoConversionClick(item, root);
            // }
            // else
            // {
            //     _logger.Trace($"未找到会话：{title}");
            // }
        }

        /// <summary>
        /// 点击第一个会话
        /// </summary>
        public void ClickFirstConversation()
        {
            var root = _GetConversationRoot();
            var items = _uiThreadInvoker.Run(automation =>
            {
                return root.FindAllChildren(cf => cf.ByControlType(ControlType.ListItem)).ToList();
            }).GetAwaiter().GetResult();
            var item = items.FirstOrDefault(u => !u.IsOffscreen);
            var parentY = root.BoundingRectangle.Y;
            var itemY = item.BoundingRectangle.Center().Y;
            if (itemY <= parentY)
            {
                item = item.GetSibling(1);
            }
            if (item != null)
            {
                //DoConversionClick(item, root);
            }
            else
            {
                _logger.Trace($"未找到第一个会话");
            }
        }
        /// <summary>
        /// 双击会话
        /// </summary>
        /// <param name="title">会话标题</param>
        public void DoubleClickConversation(string title)
        {

        }


        /// <summary>
        /// 获取会话列表可见会话标题
        /// </summary>
        /// <returns></returns>
        public List<string> GetVisibleConversationTitles()
        {
            var root = _GetConversationRoot();
            var items = _uiThreadInvoker.Run(automation => root.FindAllChildren(cf => cf.ByControlType(ControlType.ListItem)).ToList()).GetAwaiter().GetResult();
            return items.Select(item => item.Name.Replace(WeChatConstant.WECHAT_SESSION_BOX_HAS_TOP, "")).ToList();
        }



        /// <summary>
        /// 获取会话列表根节点
        /// </summary>
        /// <returns></returns>
        internal ListBox _GetConversationRoot()
        {
            var path = @"/Group/Custom/Group/Group/Group/Custom/Custom/Group/Group/Group/Group/Group/Group/List[@Name='会话'][@AutomationId='session_list']";
            var root = _Client.MainWindow.FindFirstByXPath(path).AsListBox();
            root?.DrawHighlightExt();
            return root;
        }
        internal void _ScrollListBox()
        {
            var root = this.ConversationRoot;
            //先回到顶端，从顶端开始.
            _Client.MainWindow.Focus();
            var bound = root.BoundingRectangle;
            
            var retryCount = 0;
            
        }
    }
}