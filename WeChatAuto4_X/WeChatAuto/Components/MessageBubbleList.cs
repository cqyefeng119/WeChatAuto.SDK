using System;
using System.Collections.Generic;
using System.Linq;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using WeAutoCommon.Enums;
using WeAutoCommon.Utils;
using System.Text.RegularExpressions;
using WeAutoCommon.Interface;
using WeChatAuto.Extentions;
using System.Globalization;
using WeChatAuto.Utils;
using WeChatAuto.Models;
using Microsoft.Extensions.DependencyInjection;
using FlaUI.Core.Patterns;
using System.Drawing;
using FlaUI.Core.Tools;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.Core.Capturing;
using WeAutoCommon.Models;
using FlaUI.UIA3;
using System.Threading.Tasks;
using System.Net.Http;
using WeAutoCommon.Extentions;
using WeChatAuto.Services;

namespace WeChatAuto.Components
{
    /// <summary>
    /// 聊天内容区气泡列表
    /// </summary>
    internal class MessageBubbleList
    {
        private IServiceProvider _serviceProvider;
        private AutoLogger<MessageBubbleList> _logger;
        private UIThreadInvoker _uiThreadInvoker;
        private ChatContent _ChatContent;
        private WeChatClient _Client;

        internal Button HistoryButton => _GetHistoryButton();   //实时获取聊天记录按钮

        internal MessageBubbleList(WeChatClient client, UIThreadInvoker uiThreadInvoker, ChatContent content, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<AutoLogger<MessageBubbleList>>();
            this._Client = client;
            _uiThreadInvoker = uiThreadInvoker;
            _ChatContent = content;
        }

        internal Button _GetHistoryButton()
        {
            var buttonRetry = Retry.WhileNull(() =>
            {
                var button = _Client.MainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("聊天记录")).And(cf.ByClassName("mmui::XButton")));
                return button == null ? null : button.AsButton();
            }, timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200));
            return buttonRetry.Success ? buttonRetry.Result : null;
        }

        /// <summary>
        /// 获取最后一个气泡
        /// </summary>
        /// <returns>最后一个气泡</returns>
        public MessageBubble GetLastBubble()
        {
            return null;
        }

        /// <summary>
        /// 获取所有气泡标题列表
        /// 注意：可能速度比较慢,但是信息比较全
        /// </summary>
        /// <param name="pageCount">获取的气泡数量，默认是10页,可以指定获取的页数，如果指定为-1，则获取所有气泡</param>
        /// <returns>所有气泡标题列表<see cref="ChatSimpleMessage"/></returns>
        public List<ChatSimpleMessage> GetAllChatHistory(int pageCount = 10)
        {
            return null;
        }

        /// <summary>
        /// 根据日期获取聊天历史
        /// </summary>
        /// <param name="who">微信名称，可以是好友/群聊的微信名称</param>
        /// <param name="date">查询日期,如果不传，则是当天日期</param>
        /// <returns>返回<see cref="ChatSimpleMessage"/>列表</returns>
        public async Task<List<ChatSimpleMessage>> GetAllChatHistory(string who, DateTime date = default)
        {
            if (date == default)
            {
                date = DateTime.Now;
            }
            if (string.IsNullOrWhiteSpace(who))
            {
                //可能没有选择聊天对象，如果没有选择聊天对象，则不发送.
                if (_Client.ChatContent.Sender.unSelectChatItem())
                    return new List<ChatSimpleMessage>();
            }
            else
            {
                _Client.Conversations.SearchWhoCore(_Client.MainThreadInvoker.Automation, who);
            }
            RandomWait.Wait(300, 1200);
            return await GetAllChatHistory(date);
        }
        /// <summary>
        /// 根据日期获取当前聊天窗口的聊天历史
        /// </summary>
        /// <param name="date">查询日期</param>
        /// <returns>返回<see cref="ChatSimpleMessage"/>列表</returns>
        public async Task<List<ChatSimpleMessage>> GetAllChatHistory(DateTime date = default)
        {
            if (date == default)
            {
                date = DateTime.Now;
            }
            return await WeChatInvoker.Call(GetAllChatHistoryCore, date);
        }

        internal List<ChatSimpleMessage> GetAllChatHistoryCore(UIA3Automation automation, DateTime date)
        {
            var invokeButton = HistoryButton;
            if (invokeButton == null)
                return new List<ChatSimpleMessage>();
            HeaderInfo title = this._Client.ChatContent.ChatHeader.GetTitleCore(automation);
            if (!title.CanTalk())
                return new List<ChatSimpleMessage>();
            __ClickChatHistoryButton(invokeButton);
            return __FetchChatHistoryList(automation, date, title.Title);
        }

        private List<ChatSimpleMessage> __FetchChatHistoryList(UIA3Automation automation, DateTime date, string title)
        {
            var desktop = automation.GetDesktop();
            var winResult = Retry.WhileNull(() => desktop.FindAllChildren(cf => cf.ByClassName("mmui::SearchMsgUniqueChatWindow").And(cf.ByControlType(ControlType.Window).And(cf.ByProcessId(_Client.MainWindow.Properties.ProcessId)))), timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200));
            if (winResult.Success)
            {
                var subWins = winResult.Result;
                var subWin = subWins.FirstOrDefault(u =>
                {
                    var name = u.Name.Replace("“", "").Replace("”","");
                    if (name.Contains($"{title}"))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }).AsWindow();
                if (subWin == null)
                    return new List<ChatSimpleMessage>();
                subWin.Focus();
                subWin.DrawHighlightExt();
                try
                {
                    var root = subWin.FindFirstDescendant(cf => cf.ByAutomationId("chat_log_message_list").And(cf.ByClassName("mmui::RecyclerListView")).And(cf.ByControlType(ControlType.List))).AsListBox();
                    if (root.Items.Count() == 0)
                        return new List<ChatSimpleMessage>();
                    var chatItems = root.Items.Where(x => x.ControlType == ControlType.ListItem && !string.IsNullOrWhiteSpace(x.Name) && x.BoundingRectangle.Y >= root.BoundingRectangle.Y);
                    if (chatItems.Count() == 0)
                    {
                        Mouse.Position = root.BoundingRectangle.SafeRandomPoint();
                        Mouse.Scroll(-5);
                        RandomWait.Wait(100, 300);
                    }
                    ListBoxItem chatItem = null;
                    foreach (var item in chatItems)
                    {
                        var value = item.Name;
                        var pattern = @"\s\d{4}年\d{1,2}月\d{1,2}日\s+\d{1,2}:\d{2}$";
                        if (Regex.Match(value, pattern).Success)
                        {
                            chatItem = item;
                            break;
                        }
                    }
                    if (chatItem == null)
                        return new List<ChatSimpleMessage>();
                    chatItem.DrawHighlightExt();
                    var initOffsetX = WeAutomation.Config.HistoryMessageOffset_X + chatItem.BoundingRectangle.X;
                    var initOffsetY = WeAutomation.Config.HistoryMessageOffset_Y + chatItem.BoundingRectangle.Y;
                    var point = new Point(initOffsetX, initOffsetY);
                    Mouse.Position = point;
                    RandomWait.Wait(100, 400);
                    while (CursorHelper.GetCurrentCursorHandle() != (IntPtr)65541)
                    {
                        Mouse.MoveTo(new Point(point.X, point.Y + 3));
                        RandomWait.Wait(50, 200);
                    }
                    Mouse.RightClick();
                    RandomWait.Wait(100, 300);



                    return new List<ChatSimpleMessage>();
                }
                catch (Exception ex)
                {
                    _logger.Error($"{nameof(MessageBubbleList)} - {nameof(__FetchChatHistoryList)}:{ex.ToString()}");
                    return new List<ChatSimpleMessage>();
                }
                finally
                {
                    subWin.Close();
                }
            }
            else
            {
                return new List<ChatSimpleMessage>();
            }
        }

        private void __ClickChatHistoryButton(Button invokeButton)
        {
            RandomWait.Wait(100, 800);
            Mouse.Position = invokeButton.BoundingRectangle.SafeRandomPoint();
            RandomWait.Wait(100, 400);
            _Client.MainWindow.Focus();
            Mouse.Click();
            RandomWait.Wait(300, 900);
        }




        /// <summary>
        /// 获取气泡列表,不包括系统消息
        /// 注意：可能速度比较慢,但是信息比较全
        /// </summary>
        public List<MessageBubble> GetVisibleBubbles()
        {
            return null;
        }
        public List<MessageBubble> GetVisibleBubblesByPolling(UIThreadInvoker privateThreadInvoker)
        {
            return null;
        }
        /// <summary>
        /// 获取可见气泡列表,仅返回气泡标题
        /// </summary>
        /// <returns>可见气泡列表,仅返回气泡标题</returns>
        public List<ChatSimpleMessage> GetVisibleChatSimpleMessages()
        {
            return null;
        }
        /// <summary>
        /// 获取气泡列表
        /// </summary>
        /// <returns>气泡列表<see cref="MessageBubble"/></returns>
        public List<MessageBubble> GetVisibleNativeBubbles()
        {
            return null;
        }
        //通过私有线程获取气泡列表
        public List<MessageBubble> GetVisibleNativeBubblesByPolling(UIThreadInvoker privateThreadInvoker)
        {
            return null;
        }
        /// <summary>
        /// 收藏消息
        /// </summary>
        /// <param name="chatSimpleMessage">要收藏的消息<see cref="ChatSimpleMessage"/></param>
        /// <param name="prevPageCount">如果当前页找不到，往前翻页的次数</param>
        public void CollectMessage(ChatSimpleMessage chatSimpleMessage, int prevPageCount = 3)
        {

        }
        /// <summary>
        /// 收藏指定的消息
        /// 注意，只能收藏有的消息，不会翻页，如果消息不在当前页，则不会收藏
        /// </summary>
        /// <param name="lastRowIndex">要收藏的消息的索引</param>
        public void CollectMessage(int lastRowIndex)
        {

        }
        /// <summary>
        /// 拍一拍
        /// 注意：此动作仅适用于群聊中,并且只能拍别人，不适用于单聊
        /// </summary>
        /// <param name="who">要拍一拍的好友昵称</param>
        /// <param name="prevPageCount">如果当前页找不到，往前翻页的次数</param>
        public void TapWho(string who, int prevPageCount = 3)
        {
            // _uiThreadInvoker.Run(automation =>
            // {
            //     _PopupWhoMenuCore(who, _TapWhoCore, prevPageCount);
            // })
            // .GetAwaiter().GetResult();
        }

        /// <summary>
        /// 收藏消息
        /// </summary>
        /// <param name="who">要收藏的好友昵称</param>
        /// <param name="message">要收藏的消息内容</param>
        /// <param name="prevPageCount">如果当前页找不到，往前翻页的次数</param>
        public void CollectMessage(string who, string message, int prevPageCount = 3)
        {

        }

        /// <summary>
        /// 引用消息
        /// </summary>
        /// <param name="chatSimpleMessage">要引用的消息<see cref="ChatSimpleMessage"/></param>
        /// <param name="prevPageCount">如果当前页找不到，往前翻页的次数</param>
        public void ReferencedMessage(ChatSimpleMessage chatSimpleMessage, int prevPageCount = 3)
        {

        }
        /// <summary>
        /// 引用最后一条消息
        /// 注意，只能引用有的消息，不会翻页，如果消息不在当前页，则不会引用
        /// </summary>
        /// <param name="lastRowIndex">最后一条消息的索引</param>
        public void ReferencedMessage(int lastRowIndex)
        {

        }
        /// <summary>
        /// 引用消息
        /// </summary>
        /// <param name="who">要引用的好友昵称</param>
        /// <param name="message">要引用的消息内容</param>
        /// <param name="prevPageCount">如果当前页找不到，往前翻页的次数</param>
        public void ReferencedMessage(string who, string message, int prevPageCount = 3)
        {

        }

        /// <summary>
        /// 转发多条消息,默认转发最后5条消息，可以自行指定转发多少条消息
        /// 注意：
        /// 转发会做如下预处理：
        /// 1、图片，会自动测试是否能够转发，直到能转发为止;
        /// 2、视频，会自动下载，并且测试是否能够转发，直到能转发为止
        /// 3、语音，会自行语音转文字
        /// </summary>
        /// <param name="to">要转发给谁</param>
        /// <param name="isCapture">是否要转发的内容进行截图，默认是true</param>
        /// <param name="rowCount">要转发多少条消息，默认是最后的5条消息,如果当前没有十条，则转发所有消息</param>
        public void ForwardMultipleMessage(string to, bool isCapture = true, int rowCount = 5)
        {
            // var result = _uiThreadInvoker.Run(automation =>
            // {
            //     List<ListBoxItem> _WillProcessItems = _GetWillForwardMessageList(rowCount);  //得到所有要转发的消息

            //     // 前置操作，如果有图片、视频、语音，则先处理
            //     var r = EnsureSuccess(_PreImageVedioMessage(_WillProcessItems));
            //     if (!r.Success) return r;

            //     // 选择要转发多少条消息
            //     r = EnsureSuccess(_SelectMultipleMessage(_WillProcessItems));
            //     if (!r.Success) return r;

            //     r = EnsureSuccess(_ProcessMaybeError());
            //     if (!r.Success) return r;

            //     // 转发消息
            //     r = EnsureSuccess(_ForwardMessageCore(to));
            //     if (!r.Success) return r;

            //     r = EnsureSuccess(_ProcessMaybeError());
            //     if (!r.Success) return r;

            //     // 如果需要截图，则进行截图
            //     if (isCapture)
            //     {
            //         r = EnsureSuccess(_CaptureMultipleMessage(_WillProcessItems, to));
            //         if (!r.Success) return r;
            //     }

            //     return Result.Ok();
            // })
            // .GetAwaiter().GetResult();
            // if (result.Success && isCapture)
            // {
            //     var from = this._ChatBody.ChatContent.ChatHeader.Title.Title; //得到发送者
            //     this._ChatBody.ChatContent.MainWxWindow.PasteContentToWho(to).GetAwaiter().GetResult();
            //     //转回from
            //     this._ChatBody.ChatContent.MainWxWindow.FocusWho(from);
            // }
            // else
            // {
            //     _logger.Error($"转发失败: {result.Error}");
            // }
        }

        /// <summary>
        /// 检查结果，如果失败则返回失败，否则返回成功的结果以便继续链式调用
        /// </summary>
        private Result EnsureSuccess(Result result)
        {
            return result.Success ? Result.Ok() : Result.Fail(result.Error);
        }

        /// <summary>
        /// 转发单条消息
        /// 流程：
        /// 1. 找到这一条消息,倒序找，这里注意一点，如果找不到消息，往前翻三页找不到，则不会转发此消息,日志显示错误，但不会报错.
        /// 2. 右键点击这一条消息
        /// 3. 找到菜单
        /// 4. 找到发送人
        /// </summary>
        /// <param name="to">要转发给谁</param>
        /// <param name="chatSimpleMessage">要转发的消息<see cref="ChatSimpleMessage"/></param>
        /// <param name="prevPageCount">如果当前页找不到，往前翻页的次数</param>
        public void ForwardSingleMessage(ChatSimpleMessage chatSimpleMessage, string to, int prevPageCount = 3)
        {

        }
        /// <summary>
        /// 转发最后的第index条消息,1表示最后一条消息，2表示倒数第二条消息
        /// 注意，只能转发有的消息，不会翻页，如果消息不在当前页，则不会转发
        /// </summary>
        /// <param name="lastRowIndex">最后一条消息的索引</param>
        /// <param name="to"></param>
        public void ForwardSingleMessage(int lastRowIndex, string to)
        {

        }
        /// <summary>
        /// 转发单条消息
        /// </summary>
        /// <param name="who">要转发的好友昵称</param>
        /// <param name="message">要转发的消息内容</param>
        /// <param name="to">要转发给谁</param>
        /// <param name="prevPageCount">如果当前页找不到，往前翻页的次数</param>
        public void ForwardSingleMessage(string who, string message, string to, int prevPageCount = 3)
          => ForwardSingleMessage(new ChatSimpleMessage { Who = who, Message = message }, to, prevPageCount);




    }
}