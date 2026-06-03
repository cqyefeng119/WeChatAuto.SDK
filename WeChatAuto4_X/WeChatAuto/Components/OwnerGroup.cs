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
using WeAutoCommon.Models;
using Emgu.CV;
using RapidOCRLib.Models;
using System.IO;
using MessagePack;

namespace WeChatAuto.Components
{
    /// <summary>
    /// 自有群管理
    /// </summary>
    public class OwnerGroup : Group
    {
        private readonly AutoLogger<OwnerGroup> _Logger;
        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);
        internal OwnerGroup(WeChatClient client, UIThreadInvoker uiThreadInvoker, IServiceProvider serviceProvider) :
            base(client, uiThreadInvoker, serviceProvider)
        {
            _Logger = serviceProvider.GetRequiredService<AutoLogger<OwnerGroup>>();
        }
        /// <summary>
        /// 显示缓存中存储的好友信息.
        /// </summary>
        /// <returns></returns>
        public List<FriendInfo> GetFriendListFromCache()
        {
            try
            {
                _cacheLock.Wait();
                List<FriendInfo> list = new List<FriendInfo>();
                var path = Path.Combine(AppContext.BaseDirectory, _Client.WxId + "_cache.dat");
                if (File.Exists(path))
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    return MessagePackSerializer.Deserialize<List<FriendInfo>>(bytes);
                }
                return list;
            }
            finally
            {
                _cacheLock.Release();
            }
        }
        /// <summary>
        /// 显示缓存中存储的好友信息,异步调用
        /// </summary>
        /// <returns></returns>
        public async Task<List<FriendInfo>> GetFriendListFromCacheAsync()
        {
            try
            {
                await _cacheLock.WaitAsync();
                List<FriendInfo> list = new List<FriendInfo>();
                var path = Path.Combine(AppContext.BaseDirectory, _Client.WxId + "_cache.dat");
                if (File.Exists(path))
                {
                    byte[] bytes = await File.ReadAllBytesAsync(path);
                    using MemoryStream ms = new MemoryStream(bytes);
                    return await MessagePackSerializer.DeserializeAsync<List<FriendInfo>>(ms);
                }
                return list;
            }
            finally
            {
                _cacheLock.Release();
            }
        }
        /// <summary>
        /// 从缓存中得到一个好友的信息
        /// </summary>
        /// <param name="who"></param>
        /// <returns>好友对象，请参考:<see cref="FriendInfo"/></returns>
        public FriendInfo GetFriendFromCache(string who)
        {
            if (string.IsNullOrWhiteSpace(who))
                return null;
            List<FriendInfo> list = GetFriendListFromCache();
            return list.FirstOrDefault(x => x.Name == who);

        }
        /// <summary>
        /// 从缓存中得到一个好友的信息,因为名字可能重复，而wxid永远不重复
        /// </summary>
        /// <param name="wxid">微信号</param>
        /// <returns>好友对象，请参考:<see cref="FriendInfo"/></returns>
        public FriendInfo GetFriendWithWxIDFromCache(string wxid)
        {
            if (string.IsNullOrWhiteSpace(wxid))
                return null;
            List<FriendInfo> list = GetFriendListFromCache();
            return list.FirstOrDefault(x => x.WxId == wxid);
        }
        /// <summary>
        /// 从缓存中得到一个好友的信息
        /// </summary>
        /// <param name="who"></param>
        /// <returns>好友对象，请参考:<see cref="FriendInfo"/></returns>
        public async Task<FriendInfo> GetFriendFromCacheAsync(string who)
        {
            if (string.IsNullOrWhiteSpace(who))
                return null;
            List<FriendInfo> list = await GetFriendListFromCacheAsync();
            return list.FirstOrDefault(x => x.Name == who);
        }
        /// <summary>
        /// 从缓存中得到一个好友的信息,通过wxid来获取，因为名字可能重复,而wxid号永远不重复
        /// </summary>
        /// <param name="wxid">微信号</param>
        /// <returns>好友对象，请参考:<see cref="FriendInfo"/></returns>
        public async Task<FriendInfo> GetFriendWithWxIDFromCacheAsync(string wxid)
        {
            if (string.IsNullOrWhiteSpace(wxid))
                return null;
            List<FriendInfo> list = await GetFriendListFromCacheAsync();
            return list.FirstOrDefault(x => x.WxId == wxid);

        }
        /// <summary>
        /// 从缓存中移除一个好友
        /// </summary>
        /// <param name="who"></param>
        public void RemoveFriendFromCache(string who)
        {
            if (string.IsNullOrWhiteSpace(who))
                return;
            List<FriendInfo> friendInfos = GetFriendListFromCache();
            friendInfos = friendInfos.Where(u => !u.Name.Equals(who)).ToList();
            try
            {
                _cacheLock.Wait();
                byte[] bytes = MessagePack.MessagePackSerializer.Serialize<List<FriendInfo>>(friendInfos);
                var path = Path.Combine(AppContext.BaseDirectory, _Client.WxId + "_cache.dat");
                File.WriteAllBytes(path, bytes);
            }
            finally
            {
                _cacheLock.Release();
            }
        }
        /// <summary>
        /// 从缓存中移除一个好友,通过微信id，因为通过微信名可能会重复,而微信id号永不重复
        /// </summary>
        /// <param name="wxid">微信号</param>
        public void RemoveFriendWithWxIDFromCache(string wxid)
        {
            if (string.IsNullOrWhiteSpace(wxid))
                return;
            List<FriendInfo> friendInfos = GetFriendListFromCache();
            friendInfos = friendInfos.Where(u => !u.WxId.Equals(wxid)).ToList();
            try
            {
                _cacheLock.Wait();
                byte[] bytes = MessagePack.MessagePackSerializer.Serialize<List<FriendInfo>>(friendInfos);
                var path = Path.Combine(AppContext.BaseDirectory, _Client.WxId + "_cache.dat");
                File.WriteAllBytes(path, bytes);
            }
            finally
            {
                _cacheLock.Release();
            }
        }
        /// <summary>
        /// 从缓存中移除一个好友,异步方法
        /// </summary>
        /// <param name="who"></param>
        public async Task RemoveFriendFromCacheAsync(string who)
        {
            await _cacheLock.WaitAsync();
            if (string.IsNullOrWhiteSpace(who))
                return;
            List<FriendInfo> friendInfos = await GetFriendListFromCacheAsync();
            try
            {
                await _cacheLock.WaitAsync();
                friendInfos = friendInfos.Where(u => !u.Name.Equals(who)).ToList();
                using MemoryStream ms = new MemoryStream();
                await MessagePack.MessagePackSerializer.SerializeAsync<List<FriendInfo>>(ms, friendInfos);
                var path = Path.Combine(AppContext.BaseDirectory, _Client.WxId + "_cache.dat");
                await File.WriteAllBytesAsync(path, ms.ToArray());
            }
            finally
            {
                _cacheLock.Release();
            }
        }
        /// <summary>
        /// 从缓存中移除一个好友,通过wxid,异步方法
        /// </summary>
        /// <param name="wxid"></param>
        public async Task RemoveFriendWithWxIDFromCacheAsync(string wxid)
        {
            await _cacheLock.WaitAsync();
            if (string.IsNullOrWhiteSpace(wxid))
                return;
            List<FriendInfo> friendInfos = await GetFriendListFromCacheAsync();
            friendInfos = friendInfos.Where(u => !u.WxId.Equals(wxid)).ToList();
            try
            {
                await _cacheLock.WaitAsync();
                using MemoryStream ms = new MemoryStream();
                await MessagePack.MessagePackSerializer.SerializeAsync<List<FriendInfo>>(ms, friendInfos);
                var path = Path.Combine(AppContext.BaseDirectory, _Client.WxId + "_cache.dat");
                await File.WriteAllBytesAsync(path, ms.ToArray());
            }
            finally
            {
                _cacheLock.Release();
            }
        }
        /// <summary>
        /// 手动增加或者修改一个好友对象
        /// </summary>
        /// <param name="friend">好友对象，请参考<see cref="FriendInfo"/></param>
        public void AddOrUpdateFriendFromCache(FriendInfo friend)
        {
            if (friend == null || string.IsNullOrWhiteSpace(friend.MemoName) || string.IsNullOrWhiteSpace(friend.WxId))
                return;
            try
            {
                List<FriendInfo> friendInfos = GetFriendListFromCache();
                var old = friendInfos.FirstOrDefault(u => u.WxId == friend.WxId);
                if (old != null)
                {
                    //修改
                    old.NickName = friend.NickName;
                    old.MemoName = friend.MemoName;
                    old.Area = friend.Area;
                    old.Lable = friend.Lable;
                    old.SameGroupNumber = friend.SameGroupNumber;
                    old.Signature = friend.Signature;
                    old.Source = friend.Source;
                    old.WxId = string.IsNullOrWhiteSpace(friend.WxId) ? old.WxId : friend.WxId;
                    old.AvatarPath = friend.AvatarPath;
                    old.ChatType = friend.ChatType;
                    old.AddDateTime = friend.AddDateTime;
                }
                else
                {
                    friendInfos.Add(friend);
                }
                _cacheLock.Wait();
                byte[] bytes = MessagePack.MessagePackSerializer.Serialize<List<FriendInfo>>(friendInfos);
                var path = Path.Combine(AppContext.BaseDirectory, _Client.WxId + "_cache.dat");
                File.WriteAllBytes(path, bytes);
            }
            finally
            {
                _cacheLock.Release();
            }
        }
        /// <summary>
        /// 手动增加或者修改一个好友对象，使用异步方法
        /// </summary>
        /// <param name="friend">好友对象，请参考<see cref="FriendInfo"/></param>
        public async Task AddOrUpdateFriendFromCacheAsync(FriendInfo friend)
        {

            if (friend == null || string.IsNullOrWhiteSpace(friend.MemoName) || string.IsNullOrWhiteSpace(friend.WxId))
                return;
            List<FriendInfo> friendInfos = await GetFriendListFromCacheAsync();
            var old = friendInfos.FirstOrDefault(u => u.WxId == friend.WxId);
            if (old != null)
            {
                //修改
                old.NickName = friend.NickName;
                old.MemoName = friend.MemoName;
                old.Area = friend.Area;
                old.Lable = friend.Lable;
                old.SameGroupNumber = friend.SameGroupNumber;
                old.Signature = friend.Signature;
                old.Source = friend.Source;
                old.WxId = string.IsNullOrWhiteSpace(friend.WxId) ? old.WxId : friend.WxId;
                old.AvatarPath = friend.AvatarPath;
                old.ChatType = friend.ChatType;
                old.AddDateTime = friend.AddDateTime;
            }
            else
            {
                friendInfos.Add(friend);
            }
            try
            {
                await _cacheLock.WaitAsync();
                var path = Path.Combine(AppContext.BaseDirectory, _Client.WxId + "_cache.dat");
                using FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
                await MessagePack.MessagePackSerializer.SerializeAsync(fs, friendInfos);
            }
            finally
            {
                _cacheLock.Release();
            }
        }
        /// <summary>
        /// 改变自有群群备注
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <param name="newMemo">新备注</param>
        /// <returns>微信响应结果<see cref="ChatResponse"/></returns>
        public ChatResponse ChangeOwnerChatGroupMemo(string groupName, string newMemo) => throw new Exception("待完成");
        //   => WxMainWindow.ChangeOwnerChatGroupMemo(groupName, newMemo);
        /// <summary>
        /// 修改群名，适用于自有群群名
        /// </summary>
        /// <param name="oldGroupName">旧群名称</param>
        /// <param name="newGroupName">新群名称</param>
        /// <returns>微信响应结果</returns>
        public ChatResponse ChangeOwnerChatGroupName(string oldGroupName, string newGroupName) => throw new Exception("待完成");
        //   => WxMainWindow.ChangeOwnerChatGroupName(oldGroupName, newGroupName);
        /// <summary>
        /// 更新群聊公告
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <param name="groupNotice">群聊公告</param>
        /// <returns>微信响应结果</returns>
        public async Task<ChatResponse> UpdateGroupNotice(string groupName, string groupNotice) => throw new Exception("待完成");
        //   => await WxMainWindow.UpdateGroupNotice(groupName, groupNotice);
        /// <summary>
        /// 创建群聊
        /// 如果存在，则打开它，否则创建一个新群聊
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <param name="memberName">成员名称</param>
        /// <returns>微信响应结果<see cref="ChatResponse"/></returns>
        public ChatResponse CreateOrUpdateOwnerChatGroup(string groupName, OneOf<string, string[]> memberName) => throw new Exception("待完成");


        /// <summary>
        /// 添加群聊成员，适用于自有群
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <param name="memberName">成员名称</param>
        /// <returns>微信响应结果<see cref="ChatResponse"/></returns>
        public async Task AddOwnerChatGroupMember(string groupName, OneOf<string, string[]> memberName)
        {
            await WeChatInvoker.Call(AddOwnerChatGroupMemberCore, groupName, memberName);
        }

        private void AddOwnerChatGroupMemberCore(UIA3Automation automation, string groupName, OneOf<string, string[]> memberName)
        {
            if (!CheckGroup(automation, groupName))
                return;
            var paneRoot = PaneRoot;
            var point = this._Client.OcrEngee.OCRVerticalDetect(paneRoot, 0.5f, "添加");
            if (point.IsEmpty)
                return;
            this._Client.MainWindow.Focus();
            Mouse.Position = paneRoot.BoundingRectangle.Center();
            RandomWait.Wait(600, 1200);
            var point2 = (new Point(point.X, point.Y - 30)).Confusion(10, 5);
            SupperMouseKey.MoveTo(point2);
            RandomWait.Wait(300, 1200);
            SupperMouseKey.LeftClick();
            //处理拉人事宜
            ProcessInviteMembers(memberName, automation);
        }

        internal void ProcessInviteMembers(OneOf<string, string[]> memberName, UIA3Automation automation)
        {
            var memberList = memberName.IsT0 ? new List<string> { memberName.AsT0.Trim() } : memberName.AsT1.ToList().Select(x => x.Trim()).ToList();
            if (memberList.Count() == 0)
                return;
            var winRetry = Retry.WhileNull(() => this._Client.MainWindow.FindFirstChild(cf => cf.ByName("微信添加群成员").And(cf.ByClassName("mmui::SessionPickerWindow")).And(cf.ByControlType(ControlType.Window))), TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(200));
            if (!winRetry.Success)
                return;
            var win = winRetry.Result.AsWindow();
            try
            {
                var groupRoot = win.FindFirstDescendant(cf => cf.ByClassName("mmui::SPMasterView").And(cf.ByControlType(ControlType.Group)));  //会变化.
                var RootFunc = () =>
                {
                    var item = groupRoot.FindFirstDescendant(cf => cf.ByControlType(ControlType.List).And(cf.ByName("请勾选需要添加的联系人"))).AsListBox();
                    return item;
                };
                var root = RootFunc();
                int index = 0;
                var oldSnap = new List<string>();
                var scrollPoint = root.BoundingRectangle.SafeRandomPoint();
                var rootBoundingRectangle = root.BoundingRectangle;
                while (index < 2)
                {
                    var allItem = root.FindAllChildren(cf => cf.ByControlType(ControlType.CheckBox)).ToList().Select(x => x.Name.Trim()).ToList();
                    var items = root.FindAllChildren(cf => cf.ByControlType(ControlType.CheckBox)).ToList().Where(u => !string.IsNullOrWhiteSpace(u.Name));
                    //第一个滚动
                    foreach (var item in items)
                    {
                        if (memberList.Contains(item.Name.Trim()))
                        {
                            if (item.BoundingRectangle.Y >= rootBoundingRectangle.Y && item.BoundingRectangle.Y + item.BoundingRectangle.Height <= rootBoundingRectangle.Y + rootBoundingRectangle.Height)
                            {
                                if (item.IsPatternSupported(item.Automation.PatternLibrary.TogglePattern))
                                {
                                    var point = item.BoundingRectangle.SafeRandomPoint();
                                    // Mouse.MoveTo(point);
                                    SupperMouseKey.MoveTo(point);
                                    RandomWait.Wait(200, 900);
                                    // Mouse.Click();
                                    SupperMouseKey.LeftClick();
                                    RandomWait.Wait(300, 900);
                                    memberList.Remove(item.Name.Trim());
                                    index = 2;
                                    break;
                                }
                                else
                                {
                                    memberList.Remove(item.Name.Trim());
                                }
                            }
                        }
                    }
                    if (memberList.Count == 0)
                    {
                        break;
                    }
                    if (index >= 2)
                    {
                        break;
                    }
                    var exceptList = allItem.Except(oldSnap);
                    if (exceptList.Count() > 0)
                    {
                        index = 0;
                        oldSnap = allItem;
                    }
                    else
                    {
                        index++;
                    }
                    // MouseScrollHelper.DownStep(scrollPoint, 2);
                    SupperMouseKey.MoveTo(scrollPoint.Confusion(5, 5));
                    RandomWait.Wait(300, 600);
                    SupperMouseKey.Scroll(-3);
                    RandomWait.Wait(300, 600);
                }
                //第二个，从筛选框选中
                if (memberList.Count > 0)
                {
                    var search = win.FindFirstDescendant(cf => cf.ByControlType(ControlType.Edit).And(cf.ByName("搜索")).And(cf.ByClassName("mmui::XValidatorTextEdit")));
                    Rectangle rectangle = Rectangle.Empty;
                    var searchPoint = search.BoundingRectangle.SafeRandomPoint();
                    foreach (var item in memberList.ToList())
                    {
                        SupperMouseKey.MoveTo(searchPoint);
                        RandomWait.Wait(500, 1500);
                        SupperMouseKey.LeftClick();
                        RandomWait.Wait(100, 500);
                        SupperMouseKey.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
                        RandomWait.Wait(100, 500);
                        SupperMouseKey.TypeSimultaneously(VirtualKeyShort.BACK);
                        RandomWait.Wait(100, 500);
                        // System.Windows.Clipboard.SetText(item);
                        // RandomWait.Wait(100, 500);
                        // // Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);
                        // SupperMouseKey.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);
                        SupperMouseKey.Type(item);
                        RandomWait.Wait(1200, 2500);
                        //有风控，换成OCR方案
                        if (rectangle == Rectangle.Empty)
                        {
                            var searchListRetry = Retry.WhileNull(() => win.FindFirstDescendant(cf => cf.ByAutomationId("sp_search_result_list").And(cf.ByName("请勾选需要添加的联系人"))), TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(200));
                            if (!searchListRetry.Success)
                            {
                                RandomWait.Wait(600, 1200);
                                continue;
                            }
                            if (rectangle == Rectangle.Empty)
                            {
                                rectangle = searchListRetry.Result.BoundingRectangle;
                            }
                        }

                        var point = this._Client.OcrEngee.OCRVerticalLeftCuttingDetect(rectangle, 0.5f, 100, item, false);
                        if (!point.IsEmpty)
                        {
                            // Mouse.Click(point.Confusion(5, 5));
                            SupperMouseKey.LeftClick(point.Confusion(5, 5));
                        }

                        #region UI Tree方案
                        // var searchListRetry = Retry.WhileNull(()=>win.FindFirstDescendant(cf=>cf.ByAutomationId("sp_search_result_list").And(cf.ByName("请勾选需要添加的联系人"))),TimeSpan.FromSeconds(2),TimeSpan.FromMilliseconds(200));
                        // if (!searchListRetry.Success)
                        // {
                        //     RandomWait.Wait(600, 1200);
                        //     continue;
                        // }
                        // var subItem = searchListRetry.Result.FindFirstChild(cf => cf.ByControlType(ControlType.CheckBox).And(cf.ByName(item)));
                        // if (subItem != null)
                        // {
                        //     if (subItem.IsPatternSupported(subItem.Automation.PatternLibrary.TogglePattern))
                        //     {
                        //         point = subItem.BoundingRectangle.SafeRandomPoint();
                        //         Mouse.MoveTo(point);
                        //         RandomWait.Wait(100, 300);
                        //         subItem.ClickEnhance(win);
                        //         memberList.Remove(item);
                        //         if (memberList.Count() == 0)
                        //             break;
                        //     }
                        // }
                        #endregion
                        RandomWait.Wait(600, 1200);
                    }
                }
                RandomWait.Wait(600, 1200);
                //选择框中可能选中，也可能没有，选中点击确定，没有选中选项直接关闭窗口
                var selectRoot = win.FindFirstDescendant(cf => cf.ByAutomationId("sp_choice_contact_list.qt_scrollarea_viewport").And(cf.ByControlType(ControlType.Group)));
                var selectItem = selectRoot.FindAllDescendants(cf => cf.ByControlType(ControlType.Button));
                if (selectItem.Length == 0)
                {
                    RandomWait.Wait(1000, 3000);
                    win.Close();
                }
                else
                {
                    var button = win.FindFirstDescendant(cf => cf.ByAutomationId("confirm_btn").And(cf.ByControlType(ControlType.Button)));
                    if (button != null)
                    {
                        RandomWait.Wait(600, 1500);
                        button.ClickEnhance(win);
                        RandomWait.Wait(1000, 2000);
                        //人数多时，可能会出现弹窗提示
                        var path = "/Window/Group/Group/Group/Button[@Name='邀请'][@ClassName='mmui::XOutlineButton']";
                        var buttonRetry = Retry.WhileNull(() => this._Client.MainWindow.FindFirstByXPath(path), TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(200));
                        if (buttonRetry.Success)
                        {
                            var qryButton = buttonRetry.Result;
                            var point = qryButton.BoundingRectangle.SafeRandomPoint();
                            // Mouse.Click(point);
                            SupperMouseKey.LeftClick(point);
                            RandomWait.Wait(1000, 3000);
                        }
                    }

                    this._Client.ChatContent.Sender.FcouseSenderCore(automation);
                }

            }
            catch (Exception ex)
            {
                _Logger.Error(ex.ToString());
            }
        }

        /// <summary>
        /// 删除群聊，适用于自有群,与退出群聊不同，退出群聊是退出群聊，删除群聊会删除自有群的所有好友，然后退出群聊
        /// willdo: 这里有一个问题，如果删除群的好友很多，则需要滚屏才能全部选中。
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <returns>微信响应结果<see cref="ChatResponse"/></returns>
        public async Task<ChatResponse> DeleteOwnerChatGroup(string groupName) => throw new Exception("待完成");
        //=> await WxMainWindow.DeleteOwnerChatGroup(groupName);
        /// <summary>
        /// 移除群聊成员,适用于自有群
        /// </summary>
        /// <param name="groupName">群聊名称</param>
        /// <param name="memberName">成员名称</param>
        /// <returns>微信响应结果<see cref="ChatResponse"/></returns>
        public async Task<ChatResponse> RemoveOwnerChatGroupMember(string groupName, OneOf<string, string[]> memberName) => throw new Exception("待完成");
        //=> await WxMainWindow.RemoveOwnerChatGroupMember(groupName, memberName);
    }
}