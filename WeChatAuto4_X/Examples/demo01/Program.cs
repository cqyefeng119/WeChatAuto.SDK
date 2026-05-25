using WeChatAuto.Services;
using WeChatAuto.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using WeAutoCommon.Utils;

/*************************************************************
此demo为一个完整的自动化脚本，适用于自媒体自动接入好友
功能如下：
- 根据好友打招呼内容自动通过好友申请（这里是"test"）,并且自动设置好友后缀_test;
- 自动设置好友标签: test,并且自动获取好友的wxid等等信息，并且保存进cache文件中;
- 自动在通讯录列表删除好友申请记录，以保持“新增好友”列清洁;
- 主动跟新添加的好友打招呼;
- 发送文字信息;
- 发送图片信息;
- 发送emoji信息;
- 发送视频信息;
- 自动将新增好友拉入“人工智能自动化技术讨论群”;
- 在群中发送消息，并且@好友;
- 持续监听;

一个完整的流程
*/

var serviceProvider = WeAutomation.Initialize(options =>
{
    options.WxVersion = "4.1.9.55";
    options.EnableOCR = true;
    // options.DebugMode = false;
    // options.EnableMouseKeyboardSimulator = false;
    // //下面的内容可选，如果需要使用键鼠模拟器，请填写设备VID和PID，并启用键鼠模拟器，如果有校验数据，请填写校验数据
    // options.KMDevicePID = 0x1701;
    // options.KMDeviceVID = 0x2612;
});

using var clientFactory = serviceProvider.GetRequiredService<WeChatClientFactory>();
// 请修改为你的微信昵称
var client = clientFactory.GetWeChatClient("AI.Net");
await client.AddFriendRequestAutoAcceptListener(new WeChatAuto.Options.FriendRequestAutoAcceptOptions
{
    KeyWord = new string[] {"test","wechatauto"},
    Suffix = "test",
    Label = "wechatauto",
    PassedDelete = true,
    PassedCallBack = async (whos, client, serviceProvider) =>
    {
        foreach (var item in whos)
        {
            if (item.FromKeyword.Equals("wechatauto"))
            {
                //处理好友通过打招呼"wechatauto"而来
                await client.SendMessage(item.Who, "Hi,你要咨询WeChatAuto.SDK的技术问题还是VIP相关问题哩？");
            }
            if (item.FromKeyword.Equals("test"))
            {
                //处理好友通过打招呼"test"而来
                await client.SendMessage(item.Who, "亲，终于盼到你了，我是基于微信4.1.9.55的wechatauto.sdk测试导航机器人，很高兴认识你！现在让我带你体验一下wechatauto.sdk的部分功能..大概1分钟时间..咱们开始咯....");
                await RandomWait.WaitAsync(2000, 2500);
                await client.SendMessage(item.Who, "~~嘘~~,别作声，我准备给你发图片消息 - 也是作者 Alex 的头像:");
                await RandomWait.WaitAsync(600, 2000);
                await client.SendFile(item.Who, new string[] { $"{AppContext.BaseDirectory}/Images/1.png" });
                await RandomWait.WaitAsync(1500, 3000);
                await client.SendMessage(item.Who, "我准备发送表情消息:");
                await client.SendEmoji(item.Who, 1);
                await RandomWait.WaitAsync(2000, 3000);
                await client.SendMessage(item.Who, "我准备发送视频...文件比较大，请稍候:");
                await client.SendFile(item.Who, new string[] { $"{AppContext.BaseDirectory}/Videos/1.mp4" });
                await RandomWait.WaitAsync(10000, 15000);
                await client.SendMessage(item.Who, "Now...我准备拉你到一个人工智能自动化技术讨论群（非VIP群），请稍候...");
                await RandomWait.WaitAsync(2000, 4000);
                await client.AddOwnerChatGroupMember("人工智能自动化技术讨论群", item.Who);
                await RandomWait.WaitAsync(2000, 4000);
                await client.SendMessage("人工智能自动化技术讨论群", $"欢迎🎉🎉 {item} 🎉🎉来到本群- “ {item} ”老仙，德配天地，威震寰宇，古今无比！", "所有人");
                await RandomWait.WaitAsync(1000, 3000);
                await client.SendMessage("人工智能自动化技术讨论群",
    """

群规（请务必阅读）

- 本群为严谨的技术讨论群，核心主题为：
人工智能在自动化领域中的应用、实践与原理。
- 禁止讨论任何政治或涉政敏感话题。
该类内容与群定位无关，且无法产生建设性讨论。
- 禁止发布关于公司、人事、职场抱怨、情绪宣泄等内容。
本群不提供情绪价值，仅聚焦技术本身。
- 禁止分享个人生活相关内容，包括但不限于：
旅游、美食、日常琐事、个人动态等。

请将公共讨论资源留给技术话题，踩红线必T

欢迎内容：
 技术问题与实践经验
 架构设计、实现思路、踩坑总结
 对 AI + 自动化 的独立思考与专业见解

🎉 理性讨论，观点自由；聚焦技术，拒绝灌水。祝你在本群玩得开心😊
""", item.Who);
                await RandomWait.WaitAsync(1000, 3000);
                await client.SendMessage("人工智能自动化技术讨论群", "怎么样?....是不是很Cool?呵呵😊,WeChatAuto.SDK为UI Tree自动化+OCR视觉混合方案，并且天生为人工智能而生，下面我把我的源码发给你，给你看看优美的API设计，另外你接入LLM大模型后将更智能哦🎉🎉🚀🚀");
                await RandomWait.WaitAsync(1000, 3000);
                await client.SendFile("人工智能自动化技术讨论群", new string[] { $"{AppContext.BaseDirectory}/Images/wechatauto_code.txt" });
                await RandomWait.WaitAsync(1000, 5000);
                await client.SendMessage(item.Who, "怎么样?....是不是很Cool?呵呵😊,WeChatAuto.SDK为UI Tree自动化+OCR视觉混合方案，并且天生为人工智能而生，下面我把我的源码发给你，给你看看优美的API设计，另外你接入LLM大模型后将更智能哦🎉🎉🚀🚀");
                await RandomWait.WaitAsync(1000, 3000);
                await client.SendFile(item.Who, new string[] { $"{AppContext.BaseDirectory}/Images/wechatauto_code.txt" });
                await RandomWait.WaitAsync(2000, 5000);
                await client.SendMessage(item.Who, "感谢你的体验，如果你有任何问题，可以随时联系作者，或者加入VIP群进行更深入的学习交流，祝你生活愉快,我做为测试导航机器人将暂时陪你到这，下次回复的会是人类😊。。。不过，如果是技术问题请在群里聊，因为这个微信号是挂我的，挂我的，挂我的...😊");
                await RandomWait.WaitAsync(1000, 2000);
                await client.SendMessage(item.Who, "~~再见~~");
            }
        }
    },
});




await Task.Delay(-1);
