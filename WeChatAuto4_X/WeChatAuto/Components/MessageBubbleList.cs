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
using MessagePack.Formatters;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;

namespace WeChatAuto.Components
{
    /// <summary>
    /// 聊天内容区气泡列表
    /// </summary>
    public class MessageBubbleList
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
        /// 根据日期获取聊天历史
        /// </summary>
        /// <param name="who">微信名称，可以是好友/群聊的微信名称</param>
        /// <param name="date">查询日期,如果不传，则是当天日期</param>
        /// <returns>返回<see cref="ChatSimpleMessage"/>列表</returns>
        public async Task<List<ChatSimpleMessage>> GetChatHistory(string who, DateTime date = default)
        {
            if (date == default)
            {
                date = DateTime.Now;
            }
            if (string.IsNullOrWhiteSpace(who))
            {
                var chatInfo = await _Client.ChatContent.ChatHeader.GetTitle();
                if (!chatInfo.CanTalk())
                {
                    return new List<ChatSimpleMessage>();
                }
            }
            else
            {
                await _Client.Search(who);
            }
            RandomWait.Wait(300, 1000);
            return await GetChatHistory((new List<DateTime>() { date }).Select(x => x.Date).ToList());
        }
        /// <summary>
        /// 获取一段时间的聊天历史记录，包括日期-时间，其他的获取消息历史的api仅支持日期
        /// </summary>
        /// <param name="who">微信名称，可以是好友/群聊的微信名称</param>
        /// <param name="startDate">开始日期,包括日期与时间</param>
        /// <param name="endDate">结束日期，包括日期与时间</param>
        /// <returns></returns>
        public async Task<List<ChatSimpleMessage>> GetChatHistory(string who, DateTime startDate, DateTime endDate)
        {
            List<DateTime> result = GetDates(startDate, endDate);
            if (result.Count() == 0)
            {
                return new List<ChatSimpleMessage>();
            }
            if (string.IsNullOrWhiteSpace(who))
            {
                //可能没有选择聊天对象，如果没有选择聊天对象，则不发送.
                var chatInfo = await _Client.ChatContent.ChatHeader.GetTitle();
                if (!chatInfo.CanTalk())
                {
                    return new List<ChatSimpleMessage>();
                }
            }
            else
            {
                await _Client.Conversations.Search(who);
            }
            RandomWait.Wait(300, 1200);
            return await GetChatHistory(result);
        }

        /// <summary>
        /// 获取多个指定日期的聊天历史记录
        /// </summary>
        /// <param name="who">微信名称，可以是好友/群聊的微信名称</param>
        /// <param name="range">指定的多个日期</param>
        /// <returns></returns>
        public async Task<List<ChatSimpleMessage>> GetChatHistory(string who, List<DateTime> range)
        {
            if (range.Count() == 0)
            {
                return new List<ChatSimpleMessage>();
            }
            if (string.IsNullOrWhiteSpace(who))
            {
                var chatInfo = await _Client.ChatContent.ChatHeader.GetTitle();
                if (!chatInfo.CanTalk())
                {
                    return new List<ChatSimpleMessage>();
                }
            }
            else
            {
                await _Client.Conversations.Search(who);
            }
            RandomWait.Wait(300, 1200);
            return await GetChatHistory(range.Select(x => x.Date).ToList());
        }
        /// <summary>
        /// 根据日期获取当前聊天窗口的聊天历史
        /// </summary>
        /// <param name="dates">查询日期列表</param>
        /// <returns>返回<see cref="ChatSimpleMessage"/>列表</returns>
        public async Task<List<ChatSimpleMessage>> GetChatHistory(List<DateTime> dates = default)
        {
            return await WeChatInvoker.Call(GetAllChatHistoryCore, dates);
        }

        internal List<ChatSimpleMessage> GetAllChatHistoryCore(UIA3Automation automation, List<DateTime> dates)
        {
            var invokeButton = HistoryButton;
            if (invokeButton == null)
                return new List<ChatSimpleMessage>();
            HeaderInfo title = this._Client.ChatContent.ChatHeader.GetTitleCore(automation);
            if (!title.CanTalk())
                return new List<ChatSimpleMessage>();
            __ClickChatHistoryButton(invokeButton);
            return __FetchChatHistoryList(automation, dates, title.Title);
        }

        private List<ChatSimpleMessage> __FetchChatHistoryList(UIA3Automation automation, List<DateTime> dates, string title)
        {
            var desktop = automation.GetDesktop();
            var winResult = Retry.WhileNull(() => desktop.FindAllChildren(cf => cf.ByClassName("mmui::SearchMsgUniqueChatWindow").And(cf.ByControlType(ControlType.Window).And(cf.ByProcessId(_Client.MainWindow.Properties.ProcessId)))), timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200));
            if (winResult.Success)
            {
                var subWins = winResult.Result;
                var subWin = subWins.FirstOrDefault(u =>
                {
                    var name = u.Name.Replace("“", "").Replace("”", "");
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
                int targetX = _Client.MainWindow.BoundingRectangle.X + (int)((_Client.MainWindow.BoundingRectangle.Width - subWin.BoundingRectangle.Width) / 2);
                int targetY = _Client.MainWindow.BoundingRectangle.Y + (int)((_Client.MainWindow.BoundingRectangle.Height - subWin.BoundingRectangle.Height) / 2);
                subWin.Move(targetX, targetY);  //移动子窗口至主窗口中间
                RandomWait.Wait(100, 600);
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
                        chatItems = root.Items.Where(x => x.ControlType == ControlType.ListItem && !string.IsNullOrWhiteSpace(x.Name) && x.BoundingRectangle.Y >= root.BoundingRectangle.Y);
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

                    Mouse.RightClick();  //得到完整的UI Tree列表
                    RandomWait.Wait(100, 300);
                    var menuWinRetry = Retry.WhileNull(() => subWin.FindFirstChild(cf => cf.ByControlType(ControlType.Window).And(cf.ByName("Weixin").And(cf.ByClassName("mmui::XMenu")))).AsWindow(), timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200));
                    if (menuWinRetry.Success)
                    {
                        menuWinRetry.Result.DrawHighlightExt();
                        var menuWin = menuWinRetry.Result;
                        var selectItem = menuWin.FindFirstChild(cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName("多选")));
                        if (selectItem == null)
                            return new List<ChatSimpleMessage>();
                        selectItem.DrawHighlightExt();
                        RandomWait.Wait(300, 900);
                        selectItem.Click();
                        RandomWait.Wait(300, 900);
                        root = subWin.FindFirstDescendant(cf => cf.ByAutomationId("chat_log_message_list").And(cf.ByClassName("mmui::RecyclerListView")).And(cf.ByControlType(ControlType.List))).AsListBox();
                        return __FetchChatHistoryListCore(root, dates);
                    }
                    else
                    {
                        return new List<ChatSimpleMessage>();
                    }
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
        //往下翻页，得到所有日期的字段.
        private List<ChatSimpleMessage> __FetchChatHistoryListCore(ListBox root, List<DateTime> dates)
        {
            var list = new List<ChatSimpleMessage>();
            var scollPoint = root.BoundingRectangle.SafeRandomPoint();
            Mouse.Position = scollPoint;
            RandomWait.Wait(100, 600);
            var index = 0;
            var exit = false;
            var oldSnap = new List<string>();
            while (index < WeAutomation.Config.HistoryRetryNumber && !exit)
            {
                var items = root.FindAllChildren(cf => cf.ByControlType(ControlType.CheckBox));
                if (items.Count() == 0)
                {
                    MouseScrollHelper.DownStep(scollPoint, 2);
                    index++;
                    continue;
                }
                var newSnap = items.Select(x => x.Name).ToList();
                var exceptList = newSnap.Except(oldSnap).ToList();
                oldSnap = newSnap;
                if (exceptList.Count() == 0)
                {
                    //处理长文本.
                    items = root.FindAllChildren(cf => cf.ByControlType(ControlType.CheckBox));
                    if (items.Count() == 1)
                    {
                        var longIndex = 0;
                        while (longIndex < 20)
                        {
                            MouseScrollHelper.DownStep(scollPoint, 2);
                            longIndex++;
                            items = root.FindAllChildren(cf => cf.ByControlType(ControlType.CheckBox));
                            if (items.Count() == 1)
                            {
                                continue;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        MouseScrollHelper.DownStep(scollPoint, 2);
                        index++;
                        continue;
                    }
                }

                index = 0;
                //获取数据.
                foreach (var name in exceptList)
                {
                    var item = items.FirstOrDefault(x => x.Name.Equals(name));
                    RandomWait.Wait(30, 80);
                    item.DrawHighlightExt();

                    exit = __AddToList(list, item.Name, dates);
                    if (exit)
                        break;
                }

                //滚动
                MouseScrollHelper.DownStep(scollPoint, 3);
            }

            list.Reverse();
            return list;
        }

        private bool __AddToList(List<ChatSimpleMessage> list, string input, List<DateTime> dates)
        {
            if (dates.Count == 2)
            {
                if (dates[0].TimeOfDay > TimeSpan.MinValue || dates[1].TimeOfDay > TimeSpan.MinValue)
                {
                    return __ProcessWithTime(list, input, dates);
                }
            }
            return __ProcessNoneTime(list, input, dates);
        }

        private bool __ProcessWithTime(List<ChatSimpleMessage> list, string input, List<DateTime> dates)
        {
            var minDate = dates.Min();  //最小日期-时间
            var maxDate = dates.Max();  //最大日期-时间
            var pattern = @"^(.+?)\s(.*?)\s(\d{4}年\d{1,2}月\d{1,2}日\s\d{1,2}:\d{2})$";
            var match = Regex.Match(input, pattern, RegexOptions.Singleline);
            if (match.Success)
            {
                var fullDateStr = match.Groups[3].Value;
                pattern = @"(\d{4}年\d{1,2}月\d{1,2}日\s\d{1,2}:\d{2})";
                var dateStr = Regex.Match(fullDateStr, pattern).Groups[1].Value;
                var date = DateTime.ParseExact(dateStr, "yyyy年M月d日 HH:mm", CultureInfo.InvariantCulture);
                if (date < minDate)
                    return true;

                if (date >= minDate && date <= maxDate)
                {
                    ChatSimpleMessage item = new ChatSimpleMessage();
                    item.Who = match.Groups[1].Value;
                    item.Message = match.Groups[2].Value;
                    item.SendDateTime = match.Groups[3].Value;
                    item.UniqueString = GetMd5(input);
                    list.Add(item);
                }
            }
            else
            {
                _logger.Error($"格式分析错误，未能加进消息列表，input={input}");
            }
            return false;
        }

        //处理没有日期-时间格式的.
        private bool __ProcessNoneTime(List<ChatSimpleMessage> list, string input, List<DateTime> dates)
        {
            var minDate = dates.Min();  //最小日期
            var pattern = @"^(.+?)\s(.*?)\s(\d{4}年\d{1,2}月\d{1,2}日\s\d{1,2}:\d{2})$";
            var match = Regex.Match(input, pattern, RegexOptions.Singleline);
            if (match.Success)
            {
                var fullDateStr = match.Groups[3].Value;
                pattern = @"(\d{4}年\d{1,2}月\d{1,2}日)";
                var dateStr = Regex.Match(fullDateStr, pattern).Groups[1].Value;
                var date = DateTime.ParseExact(dateStr, "yyyy年M月d日", CultureInfo.InvariantCulture);
                if (date < minDate)
                    return true;

                if (dates.Contains(date))
                {
                    ChatSimpleMessage item = new ChatSimpleMessage();
                    item.Who = match.Groups[1].Value;
                    item.Message = match.Groups[2].Value;
                    item.SendDateTime = match.Groups[3].Value;
                    item.UniqueString = GetMd5(input);
                    list.Add(item);
                }
            }
            else
            {
                _logger.Error($"格式分析错误，未能加进消息列表，input={input}");
            }
            return false;
        }

        public static string GetMd5(string input)
        {
            // 转成字节数组
            byte[] bytes = Encoding.UTF8.GetBytes(input);

            // 创建 MD5 对象
            using (MD5 md5 = MD5.Create())
            {
                // 计算哈希
                byte[] hashBytes = md5.ComputeHash(bytes);

                // 转成16进制字符串
                StringBuilder sb = new StringBuilder();

                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2")); // 小写
                }

                return sb.ToString();
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


        /// <summary>
        /// 获取开始日期到结束日期之间的所有日期（包含首尾）
        /// </summary>
        private List<DateTime> GetDates(DateTime startDate, DateTime endDate)
        {
            var result = new List<DateTime>();

            // 只保留日期部分
            var checkStartDate = startDate.Date;
            var checkEndDate = endDate.Date;

            // 防止开始日期大于结束日期
            if (checkStartDate > checkEndDate)
                return result;
            result.Add(startDate);
            result.Add(endDate);

            // for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            // {
            //     result.Add(date);
            // }


            return result;
        }

    }
}