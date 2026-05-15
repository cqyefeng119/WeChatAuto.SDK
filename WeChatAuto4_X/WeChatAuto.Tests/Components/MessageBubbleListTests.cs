using System.Diagnostics;
using OneOf;
using Xunit.Abstractions;

namespace WeChatAuto.Tests.Components;


[Collection("UiTestCollection")]
public class MessageBubbleListTests
{
    private readonly string _wxClientName = "Alex";
    private readonly ITestOutputHelper _output;
    private UiTestFixture _globalFixture;
    public MessageBubbleListTests(ITestOutputHelper output, UiTestFixture globalFixture)
    {
        _output = output;
        _globalFixture = globalFixture;
    }

    [Theory(DisplayName = "测试按日期获取历史消息")]
    [InlineData("")]
    public async Task Test_GetAllChatHistory(string who)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var list = await client.GetChatHistory(who,DateTime.Parse("2026-05-12"));
        Assert.True(list.Count != 0);
        list.ForEach(item =>
        {
           _output.WriteLine(item.ToString()); 
        });
        _output.WriteLine($"总共有:{list.Count}条消息");
    }

    [Theory(DisplayName = "测试按日期-开始时间-结束时间获取历史消息")]
    [InlineData("郭老总_vip")]
    public async Task Test_GetChatHistory_startdate_enddate(string who)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var list = await client.GetChatHistory(who,DateTime.Parse("2026-05-12"),DateTime.Parse("2026-05-15"));
        Assert.True(list.Count != 0);
        list.ForEach(item =>
        {
           _output.WriteLine(item.ToString()); 
        });
        _output.WriteLine($"总共有:{list.Count}条消息");
    }
}