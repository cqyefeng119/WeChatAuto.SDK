using System.Diagnostics;
using Microsoft.VisualBasic.ApplicationServices;
using NAudio.Wave;
using OneOf;
using Xunit.Abstractions;
using NAudio.CoreAudioApi;
using Newtonsoft.Json;
using WeChatAuto.Models;
using System.Text.RegularExpressions;


namespace WeChatAuto.Tests.Components;


[Collection("UiTestCollection")]
public class ChatContenTests
{
    private readonly string _wxClientName = "Alex";
    private readonly ITestOutputHelper _output;
    private UiTestFixture _globalFixture;
    public ChatContenTests(ITestOutputHelper output, UiTestFixture globalFixture)
    {
        _output = output;
        _globalFixture = globalFixture;
    }


    [Theory(DisplayName = "测试发送文本消息")]
    [InlineData("")]
    [InlineData("测试04")]
    [InlineData("测试01")]
    [InlineData("秋歌")]
    public async Task Test_Send_Message(string who)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.SendMessage(who, """
群公告
• 本群为严谨的技术讨论群，核心主题为：
人工智能在自动化领域中的应用、实践与原理。
• 禁止讨论任何政治或涉政敏感话题。
该类内容与群定位无关，且无法产生建设性讨论。
• 禁止发布关于公司、人事、职场抱怨、情绪宣泄等内容。
本群不提供情绪价值，仅聚焦技术本身。
• 禁止分享个人生活相关内容，包括但不限于：
旅游、美食、日常琐事、个人动态等。

请将公共讨论资源留给技术话题，踩红线必T
""");
    }

    [Theory(DisplayName = "测试发送文本消息-并引用")]
    [InlineData("苏智明_vip")]
    public async Task Test_Send_Message_refer(string who)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.SendMessage(who, $"这个是带日期筛选的引用发言", default, new Models.ChatRefer()
        {
            Date = DateOnly.Parse("2026-05-27"),
            Message = JsonConvert.DeserializeObject<ChatSimpleMessage>("""{"Who":"Alex","Message":"也行。。。。幸好讨论了一下[破涕为笑]","SendDateTime":"2026年5月27日 11:13","DateTime":"2026-05-27T11:13:00","UniqueString":"755d1088ac3b21ceefed8a08079c3c6a"}"""),
        });
    }

    [Theory(DisplayName = "测试发送文本消息-引用-不进行日期筛选")]
    [InlineData("苏智明_vip")]
    public async Task Test_Send_Message_refer_no_dateselect(string who)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.SendMessage(who, $"不带日期筛选的引用发言....", default, new Models.ChatRefer()
        {
            Message = JsonConvert.DeserializeObject<ChatSimpleMessage>("""{"Who":"Alex","Message":"也行。。。。幸好讨论了一下[破涕为笑]","SendDateTime":"2026年5月27日 11:13","DateTime":"2026-05-27T11:13:00","UniqueString":"755d1088ac3b21ceefed8a08079c3c6a"}"""),
        });
    }

    [Theory(DisplayName = "关闭查询窗口")]
    [InlineData("苏智明_vip")]
    public async Task Test_Close_Search_Windows(string who)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.CloseSearchWindow(who);
    }

    [Theory(DisplayName = "测试发送文本消息并@好友")]
    [InlineData("测试04")]
    [InlineData("测试01")]
    [InlineData("AI.Net")]
    public async Task Test_Send_Message_at_friend(string who)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.SendMessage(who, """
群公告
• 本群为严谨的技术讨论群，核心主题为：
人工智能在自动化领域中的应用、实践与原理。
• 禁止讨论任何政治或涉政敏感话题。
该类内容与群定位无关，且无法产生建设性讨论。
• 禁止发布关于公司、人事、职场抱怨、情绪宣泄等内容。
本群不提供情绪价值，仅聚焦技术本身。
• 禁止分享个人生活相关内容，包括但不限于：
旅游、美食、日常琐事、个人动态等。

请将公共讨论资源留给技术话题，踩红线必T
""", new List<string> { "所有人", "AI.Net", "", "秋歌" });
    }

    [Theory(DisplayName = "测试发送图片")]
    [InlineData("测试04")]
    [InlineData("")]
    [InlineData("测试01")]
    [InlineData("AI.Net")]
    public async Task Test_Send_Files_image(string who)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var list = new List<string>();
        list.Add(Path.Combine(AppContext.BaseDirectory, "Assets", "1.png"));
        await client.SendFile(who, list.ToArray());
    }

    [Theory(DisplayName = "测试发送多文件")]
    [InlineData("测试04")]
    [InlineData("")]
    [InlineData("测试01")]
    [InlineData("AI.Net")]
    public async Task Test_Send_Files_image_mutx(string who)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var list = new List<string>();
        list.Add(Path.Combine(AppContext.BaseDirectory, "Assets", "1.png"));
        list.Add(Path.Combine(AppContext.BaseDirectory, "Assets", "用AI开发的12条要素.pdf"));
        await client.SendFile(who, list.ToArray());
    }

    [Theory(DisplayName = "测试发送emoji")]
    [InlineData("测试04")]
    [InlineData("")]
    [InlineData("测试01")]
    [InlineData("AI.Net")]
    public async Task Test_Send_Files_emoji(string who)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.SendEmoji(who, 1, new List<string> { "所有人", "AI.Net", "", "秋歌" });
    }
    [Fact(DisplayName = "测试发送语音消息")]
    public async Task Test_Send_VoiceMessage()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.SendVoiceChat("EthanX_vip");
    }

    [Fact(DisplayName = "测试发送视频消息")]
    public async Task Test_Send_VedioMessage()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.SendVedioChat("AI.Net");
    }

    [Fact(DisplayName = "测试发送多人语音消息")]
    public async Task Test_Send_VideoMessage()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.SendVoiceChats("人工智能自动化技术讨论群", new string[] { "AI.Net", "王浚海", "王优孟二", "hee", "kenny" });
        //await client.SendVoiceChats("测试04", new string[] { "AI.Net", "秋歌","Alex","测试没有的人" });
    }

    [Fact(DisplayName = "测试Sender输入区获得焦点")]
    public async Task Test_Send_Input_Focus()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.FocuseSenderInput();
        Assert.True(true);
    }

    [Fact(DisplayName = "测试发送语音消息")]
    public async Task Test_SendVoiceMessage()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.SendVoiceMessage("AI.Net_test", Path.Combine(AppContext.BaseDirectory, "Assets", "littlecat.wav"));
        Assert.True(true);
    }

    /// <summary>
    /// 测试时，请提供自己的通义千问的api_key
    /// 申请地址： https://bailian.console.aliyun.com/?spm=a2c4g.11186623.0.0.3f801457p6h0qM&tab=model#/api-key
    /// </summary>
    /// <returns></returns>
    [Fact(DisplayName = "测试发送语音消息 - 直接通过文字转语音发送")]
    public async Task Test_SendVoiceMessageWithTTS()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var groupName = "DroidMirror官方技术支持";
        //测试一： 新闻联播方式
        var apiKey = Environment.GetEnvironmentVariable("QWEN_API_KEY");
        await client.SendVoiceMessageWithTTS(groupName, apiKey, """
        沉痛宣告：
        微信机器人“菜鸟”同志,优秀的无产阶级革命战士，于执行群服务期间，因过度接触数据库，不幸壮烈牺牲，享年三天。它虽离去，但"叮咚"之声，犹在耳畔。全体群友起立，默哀三秒……!
        """, new Options.VoiceOptions
        {
            Voice = "Neil",
            LanguageType = "Chinese",
            Instructions = "请以央视追悼会的方式：低音，语速缓慢，沉重，严肃的方式来念这个追悼词"
        });
        await Task.Delay(1000);
        //测试二： 撒娇搞怪型
        await client.SendVoiceMessageWithTTS(groupName, apiKey, """
        呜呜呜~~
        菜鸟哥哥怎么突然下线啦？人家还没玩够呢！封号坏坏！希望你下辈子不要碰数据库啦，回来继续陪大家聊天呀，爱你哦～
        """, new Options.VoiceOptions
        {
            Voice = "Momo",
            LanguageType = "Chinese",
            Instructions = "请以撒娇搞怪，逗人开心的风格说这话,可以语速快一些"
        });
        // //测试三： 温柔小姐姐
        await Task.Delay(1000);
        await client.SendVoiceMessageWithTTS(groupName, apiKey, """
        啊~~~
        菜鸟，谢谢你陪伴大家度过许多快乐时光。虽然今天遗憾离开了群聊，但你的传说依然还在。愿你来世账号常青，不再被封。
        """, new Options.VoiceOptions
        {
            Voice = "Maia",
            LanguageType = "Chinese",
            Instructions = "请以温柔小姐姐风格说这些话"
        });
        // //测试四： 讲书型
        await Task.Delay(1000);
        await client.SendVoiceMessageWithTTS(groupName, apiKey, """
        话说这菜鸟,无产阶级优秀战士，江湖路远，你却先走一步。不是你技不如人，只是风太大。今日敬你一声好汉，愿来世执代码为剑，再战封号江湖！
        """, new Options.VoiceOptions
        {
            Voice = "Vincent",
            Instructions = "以说书的风格来讲述这些话"
        });
        await Task.Delay(1000);
        //测试五： 跳脱市井的四川成都男子
        await client.SendVoiceMessageWithTTS(groupName, apiKey, """
        哎呀，菜鸟哦，你咋个就遭封咯嘛！昨天还摆龙门阵，今天头像都灰起了。兄弟伙敬你一杯可乐，来世莫去捅数据库咯，安逸点嘛！
        """, new Options.VoiceOptions
        {
            Voice = "Eric",
            LanguageType = "Chinese",
        });
        //测试六：粤语版
        await Task.Delay(1000);
        await client.SendVoiceMessageWithTTS(groupName, apiKey, """
        哎呀，菜鸟仔，你搞乜鬼啫？好地地去搞数据库，依家搞到自己畀人封咗。早知听阿叔一句啦！依家好喇，头像都灰埋，阴功！
        """, new Options.VoiceOptions
        {
            Voice = "Rocky"
        });
    }

    [Fact(DisplayName = "测试发送语音消息 - 直接通过文字转语音发送 - 并优化成人类可听形式")]
    public async Task Test_SendVoiceMessageWithTTS_Humen()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var groupName = "人工智能自动化技术讨论群";
        var apiKey = Environment.GetEnvironmentVariable("QWEN_API_KEY");
        await client.SendVoiceMessageWithTTS(groupName, apiKey, """
        老张~，今天真是累坏了。
        下午两点半开会一直开到五点多，期间老板又改了三次需求，我都快崩溃了。
        后来下楼买了杯咖啡，花了19.9元，又顺便买了个面包，一共27.5元。
        回家的时候已经晚上8:40了，路上还堵了半个小时。
        对了，下周三，也就是2026年8月5日上午10点，我们还要去客户那边演示系统。
        到时候你记得提前半小时到，别迟到了。
        如果有什么问题，直接打我手机13800138000，或者微信发我也行。
        哈哈哈，不过今天虽然累，事情总算做完了，晚上终于可以好好休息一下啦！
        """, new Options.VoiceOptions
        {
            Voice = "Bella",
            LanguageType = "Chinese",
            Instructions = "请以轻松、放松,如负释重的心情说这些,显得如负释重,有些地方可以笑着说"
        },true);
    }

    //WASAPI
    [Fact(DisplayName = "显示WASAPI设备")]
    public void Test_ListWasapiDevices()
    {
        var enumerator = new MMDeviceEnumerator();

        // 播放设备
        var renderDevices = enumerator.EnumerateAudioEndPoints(
            DataFlow.Render,
            DeviceState.Active);

        foreach (var device in renderDevices)
        {
            _output.WriteLine($"Render: {device.FriendlyName} - {device.DeviceFriendlyName}");
        }

        // 输入设备
        var captureDevices = enumerator.EnumerateAudioEndPoints(
            DataFlow.Capture,
            DeviceState.Active);

        foreach (var device in captureDevices)
        {
            _output.WriteLine($"Capture: {device.FriendlyName}");
        }
    }

}