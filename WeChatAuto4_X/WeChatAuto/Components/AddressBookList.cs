using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.Core.WindowsAPI;
using Microsoft.Extensions.DependencyInjection;
using WeAutoCommon.Models;
using WeAutoCommon.Utils;
using WeChatAuto.Extentions;
using WeChatAuto.Utils;
using WeAutoCommon.Enums;

namespace WeChatAuto.Components
{
    /// <summary>
    /// 通讯录列表
    /// </summary>
    public class AddressBookList
    {
        private readonly IServiceProvider _serviceProvider;
        private UIThreadInvoker _uiMainThreadInvoker;
        private AutoLogger<AddressBookList> _logger;
        private WeChatClient _Client;
        public AddressBookList(WeChatClient client, UIThreadInvoker uiThreadInvoker, IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<AutoLogger<AddressBookList>>();
            _uiMainThreadInvoker = uiThreadInvoker;
            _Client = client;
            _serviceProvider = serviceProvider;
        }
        /// <summary>
        /// 获取所有好友
        /// 如果是企业微信，会剔除@xxxx后缀，以保持一致性.
        /// </summary>
        /// <returns>好友列表</returns>
        public List<string> GetAllFriends()
        {
            return null;
        }

        /// <summary>
        /// 定位好友
        /// </summary>
        /// <param name="friendName">好友昵称</param>
        /// <returns>是否存在</returns>
        public bool LocateFriend(string friendName)
        {
            return false;
        }
        /// <summary>
        /// 获取所有公众号
        /// </summary>
        /// <returns>公众号列表</returns>
        public List<string> GetAllOfficialAccount()
        {
            return null;
        }
        /// <summary>
        /// 获取所有待添加好友
        /// </summary>
        /// <param name="keyWord">关键字,如果设置关键字，则返回包含关键字的新好友，如果没有设置，则返回所有新好友</param>
        /// <returns>待添加好友昵称列表</returns>
        public List<string> GetAllWillAddFriends(string keyWord = null)
        {
            return null;
        }

        /// <summary>
        /// 通过新好友
        /// </summary>
        /// <param name="keyWord">关键字,如果设置关键字，则通过包含关键字的新好友，如果没有设置，则通过所有新好友</param>
        /// <param name="suffix">后缀,如果设置后缀，则在此好友昵称后添加后缀</param>
        /// <param name="label">好友标签</param>
        /// <param name="isDelet">添加好友成功后是否删除好友申请按钮，默认删除</param>
        /// <returns>通过的新好友昵称列表</returns>
        public List<string> PassedAllNewFriend(string keyWord = null, string suffix = null, string label = null, bool isDelet = true)
        {
            return null;
        }

        /// <summary>
        /// 移除好友
        /// 注意： 如果删除好友，从通讯录删除好友后，同步的，应该将监听删除
        /// </summary>
        /// <param name="nickName">好友昵称</param>
        /// <returns>是否成功</returns>
        public bool RemoveFriend(string nickName)
        {
            return false;
        }



        /// <summary>
        /// 添加好友
        /// </summary>
        /// <param name="friendNames">微信号/手机号列表</param>
        /// <param name="label">好友标签</param>
        /// <returns>好友昵称列表和是否成功</returns>
        public List<(string friendName, bool isSuccess, string errMessage)> AddFriends(List<string> friendNames, string label = "")
        {
            return null;
        }
        /// <summary>
        /// 添加好友
        /// 注意：不能添加太频繁，否则可能会触发微信的风控机制，导致加好友失败
        /// </summary>
        /// <param name="friendName">微信号/手机号</param>
        /// <param name="label">好友标签</param>
        /// <returns>是否成功</returns>
        public bool AddFriend(string friendName, string label = "")
        {
            return true;
        }
    }

}