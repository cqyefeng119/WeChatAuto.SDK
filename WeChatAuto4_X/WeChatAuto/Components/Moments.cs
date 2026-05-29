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
using System.Threading.Tasks;
using FlaUI.UIA3;
using WeAutoCommon.Extentions;
using System.IO;
using FlaUI.Core;
using MessagePack;
using System.Net.Http;
using System.Drawing;
using WeChatAuto.Options;
using WeChatAuto.Models;


namespace WeChatAuto.Components
{
	/// <summary>
	/// 朋友圈操作
	/// </summary>
	public class Moments
	{
		private readonly IServiceProvider _serviceProvider;
		private UIThreadInvoker _uiMainThreadInvoker;
		private AutoLogger<AddressBookList> _logger;
		private WeChatClient _Client;
		internal Moments(WeChatClient client, UIThreadInvoker uiThreadInvoker, IServiceProvider serviceProvider)
		{
			_logger = serviceProvider.GetRequiredService<AutoLogger<AddressBookList>>();
			_uiMainThreadInvoker = uiThreadInvoker;
			_Client = client;
			_serviceProvider = serviceProvider;
		}

		/// <summary>
		/// 打开朋友圈,如果未打开，则打开朋友圈，如果已经打开了，则窗口提前
		/// </summary>
		/// <returns></returns>
		public async Task OpenMoments()
		{
			await WeChatInvoker.Call(OpenMomentsCore);
		}

		internal void OpenMomentsCore(UIA3Automation automation)
		{
			(bool success, Window win) result = IsOpenMomentsWin(automation);
			if (!result.success)
			{
				this._Client.Navigation.SwitchNavigationCore(automation, NavigationType.朋友圈);
				RandomWait.Wait(600, 1200);
			}
			else
			{
				result.win.Focus();
				RandomWait.Wait(600, 1200);
			}
		}
		/// <summary>
		/// 是否打开朋友圈
		/// </summary>
		/// <param name="automation"></param>
		/// <returns></returns>
		internal (bool success, Window win) IsOpenMomentsWin(UIA3Automation automation)
		{
			var desktop = automation.GetDesktop();
			var winRetry = Retry.WhileNull(() => desktop.FindFirstChild(cf => cf.ByProcessId(this._Client.MainWindow.Properties.ProcessId).And(cf.ByName("朋友圈")).And(cf.ByAutomationId("SNSWindow"))), TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(200));
			return (winRetry.Success, winRetry.Result.AsWindow());
		}
		/// <summary>
		/// 关闭朋友圈
		/// </summary>
		/// <returns></returns>
		public async Task CloseMoments()
		{
			await WeChatInvoker.Call(CloseMomentsCore);
		}

		internal void CloseMomentsCore(UIA3Automation automation)
		{
			(bool success, Window win) result = IsOpenMomentsWin(automation);
			if (result.success)
			{
				result.win.Focus();
				RandomWait.Wait(600, 1200);
				SupperMouseKey.TypeSimultaneously(VirtualKeyShort.ESC);
			}
		}

		/// <summary>
		/// 发送朋友圈
		/// </summary>
		/// <param name="imageFiles">图片列表，可以一看，也可以多个</param>
		/// <param name="content">朋友圈内容</param>
		/// <param name="options">发送选项，请参考<see cref="MomentsOptions"/></param>
		/// <returns>成功还是失败</returns>
		public async Task<bool> AddMoments(List<string> imageFiles, string content, MomentsOptions options = null)
		{
			return await WeChatInvoker.Call(AddMomentsCore, imageFiles, content, options);
		}

		internal bool AddMomentsCore(UIA3Automation automation, List<string> imageFiles, string content, MomentsOptions options)
		{
			return false;
		}

		/// <summary>
		/// 移除自己发送的朋友圈
		/// </summary>
		/// <param name="content">朋友圈文字内容</param>
		/// <param name="date">日期，可以不填，如果不填，则删除最近发布的朋友圈内容</param>
		/// <returns></returns>
		public async Task<bool> RemoveMoments(string content, DateTime date = default)
		{
			return await WeChatInvoker.Call(RemoveMomentsCore, content, date);
		}

		internal bool RemoveMomentsCore(UIA3Automation automation, string content, DateTime date)
		{
			throw new NotImplementedException();
		}
	}
}