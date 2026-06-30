using System.Diagnostics;
using Microsoft.VisualBasic.ApplicationServices;
using NAudio.Wave;
using OneOf;
using Xunit.Abstractions;
using NAudio.CoreAudioApi;
using Newtonsoft.Json;
using WeChatAuto.Models;


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
        await client.SendVoiceMessage("AI.Net_test",Path.Combine(AppContext.BaseDirectory,"Assets","littlecat.wav"));
        Assert.True(true);
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