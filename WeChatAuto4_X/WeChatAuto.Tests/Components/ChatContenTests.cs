using System.Diagnostics;
using OneOf;
using Xunit.Abstractions;

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
    public async Task Test_Send_Message(string who)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.SendMessage(who,"""
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

    [Fact(DisplayName = "测试发送文本消息并@好友")]
    public async Task Test_Send_Message_at_friend()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.SendMessage("","""
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
""", new List<string> {"所有人", "AI.Net","","秋歌" });
    }

}