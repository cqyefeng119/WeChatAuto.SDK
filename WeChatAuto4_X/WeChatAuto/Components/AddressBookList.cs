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
using System.Windows;
using FlaUI.Core;
using MessagePack;
using System.Net.Http;
using System.Drawing;

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
		private ListBox Root => _Client.MainWindow.FindFirstDescendant(cf => cf.ByName("通讯录").And(cf.ByAutomationId("primary_table_.contact_list"))).AsListBox();
		//_Client.MainWindow.FindFirstByXPath("/Group/Custom/Group/Group/Group/Custom/Custom/Group/Group/Group/List[@Name='通讯录'][@AutomationId='primary_table_.contact_list']")?.AsListBox();
		public AddressBookList(WeChatClient client, UIThreadInvoker uiThreadInvoker, IServiceProvider serviceProvider)
		{
			_logger = serviceProvider.GetRequiredService<AutoLogger<AddressBookList>>();
			_uiMainThreadInvoker = uiThreadInvoker;
			_Client = client;
			_serviceProvider = serviceProvider;
		}
		/// <summary>
		///<para> 获取所有好友的信息列表,具体请考<see cref="FriendInfo"/>类说明.</para>
		///<para> 注意：只会获取通讯录中的联系人、企业微信联系人和群聊的记录,公众号，服务号，我的企业等特殊账号不会获取.</para>
		///<para> 1.如果是企业微信，会剔除@xxxx后缀，以保持一致性.</para>
		///<para> 2.如果好友/群聊/企业微信联系人等有备注，则备注会覆盖昵称显示.</para>
		///<para> 3.注意：如果微信联系人有重名，此方法会仅获取/保存一个联系人，所以运行此方法前:建议好友/群聊/企业微信联系人有重名时，通过手工的方式添加备注，以保持区分.</para>
		///<para> 4.普通联系人可以获取wxid,其他的如：群聊/企业微信联系人无法获取wxid.</para>
		///<para> 5. 此方法运行结果会保存在cache中,默认为true,从cache中获取数据，如果设置为false,则重新刷新一遍通讯录,cache也会同步更新，建议实际开发过程中运行一遍从通讯录获取好友信息的操作,并且做好添加好友时的同步工作（在一些监听的场景，如果读取到此好友没有wxid,也会自动获取,并同步更新cache，所以也不必太担心cache的过时问题）</para>
		/// </summary>
		/// <param name="fromCache">是否从cahce中获取数据</param>
		/// <returns>好友列表</returns>
		public async Task<List<FriendInfo>> GetAllFriends(bool fromCache = true)
		{
			return await WeChatInvoker.Call(GetAllFriendsCore, fromCache);
		}

		internal List<FriendInfo> GetAllFriendsCore(UIA3Automation automation, bool fromCache)
		{
			try
			{
				//通过cache获取数据
				if (fromCache)
				{
					(bool success, List<FriendInfo> lt) cacheResult = _GetFromCache();
					if (cacheResult.success)
					{
						return cacheResult.lt;
					}
				}
				//cache没有，重新刷新
				this._Client.Navigation.SwitchNavigationCore(automation, NavigationType.通讯录);
				var list = new List<FriendInfo>();
				var root = Root;
				if (root == null)
					return list;
				_Client.MainWindow.Focus();
				root?.DrawHighlightExt();
				__CollapseAllGroups(root);   //先折叠所有分组
											 //获取群聊的记录
				var items = root.Items;
				__FetchGroupChatFriends(items, list);
				__CollapseAllGroups(root);   //折叠所有分组
											 //获取企业微信联系人的记录
				items = root.Items;
				__FetchEntpriseWeChatFriends(items, list);
				__CollapseAllGroups(root);   //折叠所有分组
											 //获取普通联系人的记录
				items = root.Items;
				_FetchNormalFriends(items, list);
				__CollapseAllGroups(root);   //折叠所有分组
				_SaveList(list); //保存进入cache中
				return list;
			}
			catch (Exception ex)
			{
				_logger.Error($"获取好友列表失败，异常信息：{ex}");
				return new List<FriendInfo>();
			}
			finally
			{
				this._Client.Navigation.SwitchNavigationCore(automation, NavigationType.微信);
			}
		}
		//如果有cache，则取cache
		private (bool success, List<FriendInfo> lt) _GetFromCache()
		{
			var path = Path.Combine(AppContext.BaseDirectory, _Client.WxId + "_cache.dat");
			if (File.Exists(path))
			{
				byte[] bytes = File.ReadAllBytes(path);
				var lt = MessagePackSerializer.Deserialize<List<FriendInfo>>(bytes);
				return (true, lt);
			}
			return (false, null);
		}

		private void _SaveList(List<FriendInfo> list)
		{
			byte[] bytes = MessagePackSerializer.Serialize(list);
			var path = Path.Combine(AppContext.BaseDirectory, _Client.WxId + "_cache.dat");
			File.WriteAllBytes(path, bytes);
			RandomWait.Wait(800, 1800);
			_Client.MainWindow.Focus();
		}
		private System.Drawing.Point GetSafeRandomPoint(Rectangle rectangle)
		{
			var width = (int)(rectangle.Width * 0.2);  //取1/4的安全位置
			var height = (int)(rectangle.Height * 0.25);

			Random random = new Random((int)DateTime.Now.Ticks);
			var x = rectangle.Left + (int)(rectangle.Width / 2) + random.Next(width * -1, width);
			var y = rectangle.Top + (int)(rectangle.Height / 2) + random.Next(1, 150);
			return new System.Drawing.Point(x, y);
		}

		private void _FetchNormalFriends(ListBoxItem[] items, List<FriendInfo> list)
		{
			try
			{
				var rootItem = items.FirstOrDefault(u => u.Name.StartsWith("联系人"));
				if (rootItem == null)
					return;
				rootItem.DrawHighlightExt();
				rootItem.Click();   //展开群聊分组
				RandomWait.Wait(500, 1000);


				var count = int.TryParse(rootItem.Name.Substring(3), out var result) ? result : 0;  //获得联系人实际数量
				var findCount = 0;

				var scrollPoint = GetSafeRandomPoint(Root.BoundingRectangle);
				AutomationElement lastElement = Root.Items.LastOrDefault(u => u.ClassName.Equals("mmui::ContactsCellItemView") && u.ControlType.Equals(ControlType.ListItem));
				Mouse.Position = scrollPoint;
				RandomWait.Wait(50, 300);

				int index = 0;
				List<string> oldSnap = new List<string>();
				Random random = new Random((int)DateTime.Now.Ticks);
				while (index < 3)
				{
					var newItems = Root.Items.Where(u => u.ClassName.Equals("mmui::ContactsCellItemView") &&
						u.ControlType.Equals(ControlType.ListItem) && u.BoundingRectangle.Y >= Root.BoundingRectangle.Y && u.BoundingRectangle.Y + u.BoundingRectangle.Height <= Root.BoundingRectangle.Y + Root.BoundingRectangle.Height).ToList();
					var newSnap = newItems.Select(u => u.Name.Trim()).ToList();
					var actionList = newSnap.Except(oldSnap).ToList();
					oldSnap = newSnap;  //为下一次做准备.
					if (actionList.Count() == 0)
					{
						_DownStep(scrollPoint, random, 2);
						index++;
						continue;
					}
					index = 0;
					//获取用户数据
					foreach (var item in actionList)
					{
						if (string.IsNullOrWhiteSpace(item))
							continue;
						var subItem = Root.Items.Where(u => u.ClassName.Equals("mmui::ContactsCellItemView") &&
						u.ControlType.Equals(ControlType.ListItem) && u.BoundingRectangle.Y >= Root.BoundingRectangle.Y && u.BoundingRectangle.Y + u.BoundingRectangle.Height <= Root.BoundingRectangle.Y + Root.BoundingRectangle.Height).ToList().Find(u => u.Name.Trim().Equals(item.Trim()));
						if (subItem != null)
						{
							FriendInfo friendInfo = new FriendInfo();
							friendInfo.NickName = subItem.Name.Trim();  //后面纠正.
							friendInfo.MemoName = subItem.Name.Trim();
							friendInfo.ChatType = ChatType.好友;
							subItem.DrawHighlightExt();
							if (subItem.BoundingRectangle.Y + subItem.BoundingRectangle.Height > Root.BoundingRectangle.Y + Root.BoundingRectangle.Height)
							{
								_DownStep(scrollPoint, random, 3);
								subItem = Root.Items.Where(u => u.Name.Trim().Equals(item) && u.ClassName.Equals("mmui::ContactsCellItemView") &&
								  u.ControlType.Equals(ControlType.ListItem) && u.BoundingRectangle.Y >= Root.BoundingRectangle.Y && u.BoundingRectangle.Y + u.BoundingRectangle.Height <= Root.BoundingRectangle.Y + Root.BoundingRectangle.Height).FirstOrDefault();
							}
							RandomWait.Wait(100, 600);
							__FetchWxUserInfo(list, subItem, friendInfo);
							list.Add(friendInfo);
							findCount++;
						}
					}

					_DownStep(scrollPoint, random, 2);
					var lastItem = Root.Items.LastOrDefault(u => u.ClassName.Equals("mmui::ContactsCellItemView") && u.ControlType.Equals(ControlType.ListItem));
					if (lastItem == null)
						break;
					if (lastItem.Name != lastElement.Name)
					{
						lastElement = lastItem;
					}
					else
					{
						index++;
					}
				}

				if (findCount != count)
				{
					_logger.Error($"读取好友数量与实际的值不一致: 读取到{findCount}个，实际应该有:{count}个.");
				}
			}
			catch (Exception ex)
			{
				_logger.Error($"滚动点击时发生错误，错误原因:{ex.ToString()}");
			}
		}

		private static void _DownStep(System.Drawing.Point scrollPoint, Random random, int maxStep)
		{
			for (int i = 0; i < maxStep; i++)
			{
				Mouse.Position = scrollPoint;
				RandomWait.Wait(50, 200);
				if (i == 0)
				{
					Mouse.Scroll(-1 * random.Next(1, 3));
				}
				if (i == maxStep - 1)
				{
					Mouse.Scroll(-1 * random.Next(3, 5));
				}
				RandomWait.Wait(50, 300);
			}
		}

		//可能为空
		//也可能重复
		private void __FetchWxUserInfo(List<FriendInfo> list, AutomationElement subItem, FriendInfo friendInfo)
		{
			var index = 0;
			var success = false;
			while (!success && index < 2)
			{
				try
				{
					var clkRetry = Retry.WhileFalse(() =>
					{
						try
						{
							return !subItem.Properties.BoundingRectangle.Value.IsEmpty;
						}
						catch (Exception)
						{
							return false;
						}
					}, timeout: TimeSpan.FromSeconds(4), interval: TimeSpan.FromMilliseconds(200));
					if (clkRetry.Success)
					{
						subItem.Click();
						RandomWait.Wait(200, 900);
						var grouproot = _Client.MainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Group).And(cf.ByAutomationId("qt_scrollarea_viewport.profile_h_view")));
						if (grouproot == null)
							return;
						__FetchWxUserInfoCore(grouproot, friendInfo);
						success = true;
						break;
					}
					else
					{
						_logger.Error("没有获取到点击位位置");
						RandomWait.Wait(300, 900);
					}
				}
				catch (Exception ex)
				{
					index++;
					RandomWait.Wait(300, 900);
					var subItemRetry = Retry.WhileNull(() => Root.Items.Where(u => u.Name.Trim().Equals(subItem.Name.Trim()) && u.ClassName.Equals("mmui::ContactsCellItemView") &&
					  u.ControlType.Equals(ControlType.ListItem) && u.BoundingRectangle.Y >= Root.BoundingRectangle.Y && u.BoundingRectangle.Y + u.BoundingRectangle.Height <= Root.BoundingRectangle.Y + Root.BoundingRectangle.Height).FirstOrDefault(),
					  timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200));
					if (subItemRetry.Success)
					{
						subItem = subItemRetry.Result;
					}
					else
					{
						break;
					}

					if (index < 2)
					{
						_logger.Error($"点击列表项目发生错误，准备重试！错误原因:{ex.ToString()}");
					}
				}
			}
		}

		private void __FetchWxUserInfoCore(AutomationElement grouproot, FriendInfo friendInfo)
		{
			var nickNameRetry = Retry.WhileNull(() => grouproot.FindFirstDescendant(cf => cf.ByName("昵称：").And(cf.ByAutomationId("right_v_view.user_info_center_view.basic_line_view.basic_line.key_text")))
			, timeout: TimeSpan.FromSeconds(1), interval: TimeSpan.FromMilliseconds(200));
			if (nickNameRetry.Success)
			{
				var nickName = nickNameRetry.Result;
				var nickValue = nickName.GetSibling(1);
				if (nickValue != null)
				{
					friendInfo.NickName = nickValue.Name.Trim();
				}
			}
			//wxid
			var item = grouproot.FindFirstDescendant(cf => cf.ByAutomationId("right_v_view.user_info_center_view.basic_line_view.basic_line.key_text").And(cf.ByName("微信号：")));
			if (item != null)
			{
				item = item.GetSibling(1);
				if (item != null)
				{
					friendInfo.WxId = item.Name;
				}
			}
			//地区
			item = grouproot.FindFirstDescendant(cf => cf.ByAutomationId("right_v_view.user_info_center_view.basic_line_view.basic_line.key_text").And(cf.ByName("地区：")));
			if (item != null)
			{
				item = item.GetSibling(1);
				if (item != null)
				{
					friendInfo.Area = item.Name;
				}
			}
			//头像
			item = grouproot.FindFirstDescendant(cf => cf.ByAutomationId("head_image_v_view").And(cf.ByControlType(ControlType.Group)));
			if (item != null)
			{
				var path = Path.Combine(AppContext.BaseDirectory, "Avator", $"{friendInfo.WxId}.png");
				item.CaptureToFile(path);
				friendInfo.AvatarPath = path;
			}
			//共同群聊
			item = grouproot.FindFirstDescendant(cf => cf.ByAutomationId("content_v_view.ProfileResizeVBoxView.detail_content_host.detail_center_v_view.detail_derived_content_view.section_shell.more_line_v_view.chatroom_intersection.key_text_h_view.key_text_view").And(cf.ByName("共同群聊")));
			if (item != null)
			{
				var parent = item.GetParent().GetParent();
				var button = parent.FindFirstChild(cf => cf.ByControlType(ControlType.Button));
				if (button != null)
				{
					var text = button.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text));
					if (text != null)
					{
						friendInfo.SameGroupNumber = text.Name;
					}
				}
			}
			//个性签名
			item = grouproot.FindFirstDescendant(cf => cf.ByAutomationId("content_v_view.ProfileResizeVBoxView.detail_content_host.detail_center_v_view.detail_derived_content_view.section_shell.more_line_v_view.sign.key_text_h_view.key_text_view").And(cf.ByName("个性签名")));
			if (item != null)
			{
				var parent = item.GetParent().GetParent();
				var button = parent.FindFirstChild(cf => cf.ByControlType(ControlType.Button));
				if (button != null)
				{
					var text = button.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text));
					if (text != null)
					{
						friendInfo.Signature = text.Name;
					}
				}
			}
			//来源
			item = grouproot.FindFirstDescendant(cf => cf.ByAutomationId("content_v_view.ProfileResizeVBoxView.detail_content_host.detail_center_v_view.detail_derived_content_view.section_shell.more_line_v_view.source.key_text_h_view.key_text_view").And(cf.ByName("来源")));
			if (item != null)
			{
				var parent = item.GetParent().GetParent();
				var button = parent.FindFirstChild(cf => cf.ByControlType(ControlType.Button));
				if (button != null)
				{
					var text = button.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text));
					if (text != null)
					{
						friendInfo.Source = text.Name;
					}
				}
			}
			//添加时间
			item = grouproot.FindFirstDescendant(cf => cf.ByAutomationId("content_v_view.ProfileResizeVBoxView.detail_content_host.detail_center_v_view.detail_derived_content_view.section_shell.more_line_v_view.became_friend_time.key_text_h_view.key_text_view").And(cf.ByName("添加时间")));
			if (item != null)
			{
				var parent = item.GetParent().GetParent();
				var button = parent.FindFirstChild(cf => cf.ByControlType(ControlType.Button));
				if (button != null)
				{
					var text = button.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text));
					if (text != null)
					{
						friendInfo.AddDateTime = text.Name;
					}
				}
			}
			//标签
			item = grouproot.FindFirstDescendant(cf => cf.ByAutomationId("content_v_view.ProfileResizeVBoxView.detail_content_host.detail_center_v_view.detail_derived_content_view.section_shell.main_line_v_view.tag_line.key_text_h_view.key_text_view").And(cf.ByName("标签")));
			if (item != null)
			{
				var parent = item.GetParent().GetParent();
				var button = parent.FindFirstChild(cf => cf.ByControlType(ControlType.Button));
				if (button != null)
				{
					var text = button.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text));
					if (text != null)
					{
						if (!string.IsNullOrWhiteSpace(text.Name))
						{
							var labelList = text.Name.Split(',').ToList();
							friendInfo.Lable = labelList;
						}
					}
				}
			}
		}

		private void ___UpdateAddressBookMemoName(AutomationElement grouproot, FriendInfo friendInfo)
		{
			var button = grouproot.FindFirstDescendant(cf => cf.ByAutomationId("content_v_view.ProfileResizeVBoxView.detail_content_host.detail_center_v_view.detail_derived_content_view.section_shell.main_line_v_view.remark_line.value_remark_view").And(cf.ByControlType(ControlType.Button)).And(cf.ByClassName("mmui::ProfileDetailValueRemarkView")));
			if (button == null)
				return;
			button.DrawHighlightExt();
			var point = button.BoundingRectangle.SafeRandomPoint();
			Mouse.Position = point;
			Mouse.Click();
			Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
			RandomWait.Wait(50, 200);
			Keyboard.TypeSimultaneously(virtualKeys: VirtualKeyShort.BACK);
			RandomWait.Wait(50, 300);
			Clipboard.SetText(friendInfo.MemoName);
			Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);
			RandomWait.Wait(50, 300);
			Keyboard.Type(VirtualKeyShort.ENTER);
		}

		//获取企业微信联系人的记录
		private void __FetchEntpriseWeChatFriends(ListBoxItem[] items, List<FriendInfo> list)
		{
			var tmpList = new List<FriendInfo>();
			var rootItem = items.FirstOrDefault(u => u.Name.StartsWith("企业微信联系人"));
			if (rootItem == null)
				return;
			var count = int.TryParse(rootItem.Name.Substring(7), out var result) ? result : 0;
			rootItem.DrawHighlightExt();
			rootItem.Click();   //展开群聊分组
			RandomWait.Wait(500, 1000);

			var root = Root;
			var scrollPoint = root.BoundingRectangle.SafeRandomPoint();

			var subItem = rootItem.GetSibling(1);  //获取第一个子项
			while (subItem != null && subItem.ClassName != "mmui::ContactsCellGroupView")
			{
				if (subItem.ClassName == "mmui::ContactsCellItemView")
				{
					FriendInfo friendInfo = new FriendInfo();
					if (subItem.Name.Trim().IndexOf("@") > 0)
					{
						var name = subItem.Name.Trim().Substring(0, subItem.Name.Trim().IndexOf("@"));
						friendInfo.NickName = name.Trim();
						friendInfo.MemoName = subItem.Name.Trim();
					}
					else
					{
						friendInfo.NickName = subItem.Name.Trim();
						friendInfo.MemoName = subItem.Name.Trim();
					}
					friendInfo.ChatType = ChatType.企业微信;
					subItem.DrawHighlightExt();
					tmpList.Add(friendInfo);
				}
				if (subItem.BoundingRectangle.Y > root.BoundingRectangle.Y + (int)(root.BoundingRectangle.Height * 0.7))
				{
					Mouse.Position = scrollPoint;
					Mouse.Scroll(-3);
					RandomWait.Wait(100, 400);
				}
				var tryNextItem = subItem.GetSibling(1);
				if (tryNextItem == null)
				{
					Mouse.Position = scrollPoint;
					Mouse.Scroll(-3);
					tryNextItem = subItem.GetSibling(1);
					subItem = tryNextItem;
					RandomWait.Wait(100, 400);
				}
				else
				{
					if (tryNextItem.ClassName == "mmui::ContactsCellGroupView")
						break;
					subItem = tryNextItem;
					if (subItem.BoundingRectangle.Y + subItem.BoundingRectangle.Height > root.BoundingRectangle.Y + root.BoundingRectangle.Height)
					{
						Mouse.Position = scrollPoint;
						Mouse.Scroll(-1);
						RandomWait.Wait(100, 400);
					}
					else
					{
						Mouse.Position = scrollPoint;
						Mouse.Scroll(-2);
						RandomWait.Wait(100, 400);
					}
				}
			}
			if (tmpList.Count != count)
			{
				_logger.Error($"读取企业微信与实际的值不一致: 读取到{tmpList.Count}个，实际应该有:{count}个.");
			}
			list.AddRange(tmpList);
		}



		private void __FetchGroupChatFriends(ListBoxItem[] items, List<FriendInfo> list)
		{
			var item = items.FirstOrDefault(u => u.Name.StartsWith("群聊"));
			if (item == null)
				return;
			item.DrawHighlightExt();
			item.Click();   //展开群聊分组
			RandomWait.Wait(500, 1000);

			var count = int.TryParse(item.Name.Substring(2), out var result) ? result : 0;

			var root = Root;
			var scrollPoint = root.BoundingRectangle.SafeRandomPoint();

			var findCount = 0;

			var subItem = item.GetSibling(1);  //获取第一个子项
			while (subItem != null && subItem.ClassName == "mmui::ContactsCellItemView")
			{
				if (subItem.ClassName == "mmui::ContactsCellItemView")
				{
					FriendInfo friendInfo = new FriendInfo();
					friendInfo.NickName = subItem.Name.Trim();
					friendInfo.MemoName = subItem.Name.Trim();
					friendInfo.ChatType = ChatType.群聊;
					if (!list.Any(f => f.NickName == friendInfo.NickName))
					{
						subItem.DrawHighlightExt();
						list.Add(friendInfo);
						findCount++;
					}
				}
				if (subItem.BoundingRectangle.Y > root.BoundingRectangle.Y + (int)(root.BoundingRectangle.Height * 0.7))
				{
					Mouse.Position = scrollPoint;
					Mouse.Scroll(-3);
					RandomWait.Wait(100, 400);
				}
				var tryNextItem = subItem.GetSibling(1);
				if (tryNextItem == null)
				{
					Mouse.Position = scrollPoint;
					Mouse.Scroll(-3);
					tryNextItem = subItem.GetSibling(1);
					subItem = tryNextItem;
					RandomWait.Wait(100, 400);
				}
				else
				{
					if (tryNextItem.ClassName == "mmui::ContactsCellGroupView")
						break;
					subItem = tryNextItem;
					if (subItem.BoundingRectangle.Y + subItem.BoundingRectangle.Height > root.BoundingRectangle.Y + root.BoundingRectangle.Height)
					{
						Mouse.Position = scrollPoint;
						Mouse.Scroll(-1);
						RandomWait.Wait(100, 400);
					}
					else
					{
						Mouse.Position = scrollPoint;
						Mouse.Scroll(-2);
						RandomWait.Wait(100, 400);
					}
				}

			}

			if (findCount != count)
			{
				_logger.Error($"读取群聊个数与实际的值不一致: 读取到{findCount}个，实际应该有:{count}个.");
			}
		}

		private void __CollapseAllGroups(ListBox root)
		{
			var item = root.FindFirstChild(cf => cf.ByClassName("mmui::ContactsCellGroupView").And(cf.ByControlType(ControlType.ListItem)));
			if (item == null || item.BoundingRectangle.Y > root.BoundingRectangle.Y + 10)
			{
				__GotoTop(root);
			}
			item = root.FindFirstChild(cf => cf.ByClassName("mmui::ContactsCellGroupView").And(cf.ByControlType(ControlType.ListItem)));
			while (item != null)
			{
				item = item.GetSibling(1);
				if (item != null)
				{
					if (item.ClassName != "mmui::ContactsCellGroupView")
					{
						var parent = item.GetSibling(-1);
						parent.Click();
						RandomWait.Wait(300, 800);
						item = root.FindFirstChild(cf => cf.ByClassName("mmui::ContactsCellGroupView").And(cf.ByControlType(ControlType.ListItem)));
					}
				}
				else
				{
					break;
				}
			}

		}

		private void __GotoTop(ListBox root)
		{
			int index = 0;
			var itemName = "";
			var point = root.BoundingRectangle.SafeRandomPoint();
			var random = new Random((int)DateTime.Now.Ticks);
			while (index < 2)
			{
				Mouse.Position = point;
				Mouse.Scroll(random.Next(5, 12));
				RandomWait.Wait(20, 100);
				if (root.Items.First().Name == itemName)
				{
					index++;
				}
				else
				{
					index = 0;
					itemName = root.Items.First().Name;
				}
			}

		}

		/// <summary>
		/// 获取所有好友名称列表.（通过通讯录）
		/// 如果好友有昵称与备注，优先选择备注名
		/// 注意：如果是企业微信，会剔除@xxxx后缀，以保持一致性.
		/// </summary>
		/// <returns></returns>
		internal async Task<List<string>> GetAllFriendNames()
		{
			var list = (await GetAllFriends(true)).Select(f => f.Name).ToList();
			return list;
		}


		/// <summary>
		/// 获取所有待添加好友
		/// </summary>
		/// <param name="keyWord">关键字,如果设置关键字，则返回包含关键字的新好友，如果没有设置，则返回所有新好友</param>
		/// <returns>待添加好友昵称列表</returns>
		internal List<string> GetAllWillAddFriends(string keyWord = null)
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
		internal List<string> PassedAllNewFriend(string keyWord = null, string suffix = null, string label = null, bool isDelet = true)
		{
			return null;
		}

		/// <summary>
		/// 移除好友
		/// 注意： 如果删除好友，从通讯录删除好友后，同步的，应该将监听删除
		/// </summary>
		/// <param name="nickName">好友昵称</param>
		/// <returns>是否成功</returns>
		internal bool RemoveFriend(string nickName)
		{
			return false;
		}



		/// <summary>
		/// 添加好友
		/// </summary>
		/// <param name="friendNames">微信号/手机号列表</param>
		/// <param name="label">好友标签</param>
		/// <returns>好友昵称列表和是否成功</returns>
		internal List<(string friendName, bool isSuccess, string errMessage)> AddFriends(List<string> friendNames, string label = "")
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
		internal bool AddFriend(string friendName, string label = "")
		{
			return true;
		}
	}

}