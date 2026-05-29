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
using Emgu.CV.Dai;


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
		internal AutomationElement ToolBar => GetToolBar();

		internal AutomationElement GetToolBar()
		{
			var window = OpenMomentsCore(_uiMainThreadInvoker.Automation);
			if (window == null)
				return null;
			var path = "/Group/Group/Group/ToolBar[@AutomationId='sns_window_tool_bar'][@ClassName='mmui::SNSWindowToolBar']";
			var toolBarRetry = Retry.WhileNull(() => window.FindFirstByXPath(path), TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(200));
			return toolBarRetry.Success ? toolBarRetry.Result : null;
		}

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
		public async Task<Window> OpenMoments()
		{
			return await WeChatInvoker.Call(OpenMomentsCore);
		}

		internal Window OpenMomentsCore(UIA3Automation automation)
		{
			(bool success, Window win) result = IsOpenMomentsWin(automation);
			if (!result.success)
			{
				this._Client.Navigation.SwitchNavigationCore(automation, NavigationType.朋友圈);
				RandomWait.Wait(600, 1200);
				result = IsOpenMomentsWin(automation);
				if (result.success)
				{
					this._Client.MoveWinToMainCenter(result.win);
					return result.win;
				}
			}
			else
			{
				this._Client.MoveWinToMainCenter(result.win);
				return result.win;
			}
			return null;
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
		/// <param name="imageFiles">图片列表，可以一个，也可以多个,如果是多个文件，要求在同一个目录中</param>
		/// <param name="content">朋友圈内容</param>
		/// <param name="options">发送选项，请参考<see cref="MomentsOptions"/></param>
		/// <returns>成功还是失败</returns>
		public async Task<bool> AddMoments(List<string> imageFiles, string content, MomentsOptions options = null)
		{
			return await WeChatInvoker.Call(AddMomentsCore, imageFiles, content, options);
		}

		internal bool AddMomentsCore(UIA3Automation automation, List<string> imageFiles, string content, MomentsOptions options)
		{
			var pathResult = CheckImagesValid(imageFiles);
			if (!pathResult.sucess)
				return false;
			var win = OpenMomentsCore(automation);  //打开朋友圈,返回朋友圈窗口
			if (win == null)
				return false;
			var toolBar = this.ToolBar;
			if (toolBar == null)
				return false;
			var sendButton = toolBar.FindFirstChild(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("发表")));
			var noticeButotn = toolBar.FindFirstChild(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("消息")));
			var point = noticeButotn.BoundingRectangle.SafeRandomPoint();
			Mouse.Position = point;
			RandomWait.Wait(200, 700);
			SupperMouseKey.MoveTo(sendButton.BoundingRectangle.SafeRandomPoint());
			RandomWait.Wait(200, 700);
			SupperMouseKey.LeftClick();
			var pathRoot = pathResult.path;

			bool result = _ProcessImageFiles(pathRoot, win, imageFiles);
			if (!result)
				return false;

			result = _ProcessContent(automation, content, options, win);

			return result;
		}

		private bool _ProcessContent(UIA3Automation automation, string content, MomentsOptions options, Window window)
		{
			var publishRootRetry = Retry.WhileNull(() => window.FindFirstByXPath("/Group/Group/Group/Group/Group[@AutomationId='SnsPublishPanel']"), TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(200));
			if (publishRootRetry.Success)
			{
				System.Windows.Clipboard.SetText(content);
				var pubishRoot = publishRootRetry.Result;
				var buttonRoot = pubishRoot.FindFirstByXPath("/Group/Group[2]");
				var contentRoot = pubishRoot.FindFirstByXPath("/Group/Group[1]/Group[@AutomationId='qt_scrollarea_viewport']");
				var edit = contentRoot.FindFirstDescendant(cf => cf.ByControlType(ControlType.Edit).And(cf.ByClassName("mmui::XValidatorTextEdit")));
				var point = edit.BoundingRectangle.SafeRandomPoint();
				SupperMouseKey.MoveTo(point);
				RandomWait.Wait(100, 300);
				SupperMouseKey.MoveTo(point.Confusion(5, 3));
				RandomWait.Wait(200, 900);
				SupperMouseKey.LeftClick();
				RandomWait.Wait(300, 1200);
				SupperMouseKey.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);
				if (options != null)
				{
					__ProcessAnthoer(automation, options, window);
				}

				var sendButton = buttonRoot.FindFirstChild(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("发表")));
				point = sendButton.BoundingRectangle.SafeRandomPoint();
				SupperMouseKey.MoveTo(point);
				RandomWait.Wait(100, 300);
				SupperMouseKey.MoveTo(point.Confusion(3, 2));
				RandomWait.Wait(200, 900);
				SupperMouseKey.LeftClick();
				RandomWait.Wait(300,1200);
				return true;
			}
			return false;
		}

		private void __ProcessAnthoer(UIA3Automation automation, MomentsOptions options, Window window)
		{

		}

		private bool _ProcessImageFiles(string pathRoot, Window window, List<string> imageFiles)
		{
			var path = "/Window[@Name='选择文件']";
			var rootRetry = Retry.WhileNull(() => window.FindFirstByXPath(path), TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(200));
			if (rootRetry.Success)
			{
				var fileWin = rootRetry.Result.AsWindow();
				var editRetry = Retry.WhileNull(() => fileWin.FindFirstDescendant(cf => cf.ByName("文件名(N):").And(cf.ByControlType(ControlType.Edit))), TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(200));
				if (editRetry.Success)
				{
					var point = editRetry.Result.BoundingRectangle.SafeRandomPoint();
					SupperMouseKey.MoveTo(point);
					RandomWait.Wait(200, 600);
					SupperMouseKey.LeftClick();
					System.Windows.Clipboard.SetText(pathRoot);
					SupperMouseKey.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);
					RandomWait.Wait(200, 900);
					SupperMouseKey.Enter();
					RandomWait.Wait(600, 1200);
					point = point.Confusion(20, 3);
					SupperMouseKey.MoveTo(point);
					RandomWait.Wait(200, 600);
					SupperMouseKey.LeftClick();
					SupperMouseKey.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
					RandomWait.Wait(200, 900);
					SupperMouseKey.TypeSimultaneously(VirtualKeyShort.BACK);
					RandomWait.Wait(200, 900);
					//组装字符串
					var fileNameList = imageFiles.Select(x => Path.GetFileName(x)).ToHashSet().ToList().Select(x => $"\"{x}\"").ToList();
					string fileStr = string.Join(" ", fileNameList);
					System.Windows.Clipboard.SetText(fileStr);
					SupperMouseKey.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);
					RandomWait.Wait(200, 900);
					//点击打开
					var openButton = fileWin.FindFirstByXPath("/Button[@Name='打开(O)']");
					if (openButton != null)
					{
						point = openButton.BoundingRectangle.SafeRandomPoint();
						SupperMouseKey.MoveTo(point);
						RandomWait.Wait(200, 600);
						SupperMouseKey.MoveTo(point.Confusion(5, 2));
						RandomWait.Wait(200, 600);
						SupperMouseKey.LeftClick();
						RandomWait.Wait(600, 1200);
						return true;
					}
				}

			}

			return false;
		}

		private (bool sucess, string path) CheckImagesValid(List<string> imageFiles)
		{
			if (imageFiles.Count == 0)
				return (false, "");
			//检查图片路径是否存在
			foreach (var file in imageFiles)
			{
				if (!File.Exists(file))
				{
					_logger.Error("错误：传入的imageFiles参数列表有图片文件在磁盘中不存在!");
					return (false, "");
				}
			}

			var dicsPath = imageFiles.Select(x => Path.GetDirectoryName(x)).ToHashSet();
			if (dicsPath.Count != 1)
			{
				_logger.Error("错误：传入的imageFiles参数列表中文件必须在一个目录中!");
				return (false, "");
			}

			var pathRoot = dicsPath.First();

			var supportSuffix = new string[] { ".png", ".jpg", ".jpeg", ".bmp" };
			var imgSuffix = imageFiles.Select(x => Path.GetExtension(x).ToLower()).ToList();
			var exceptList = imgSuffix.Except(supportSuffix).ToList();
			if (exceptList.Count() > 0)
			{
				_logger.Error($"错误：传入的imageFiles参数列表有图片格式不被支持，微信仅支持如下图片格式:{string.Join(",", supportSuffix)}");
				return (false, "");
			}

			return (true, pathRoot);
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