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

    [Fact(DisplayName = "获取当前窗口的聊天记录")]
    public async Task Test_Get_Current_ChatHistory()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var list = await client.GetChatHistory(new List<DateTime>() { DateTime.Parse("2026-05-16") });
        Assert.True(list.Count != 0);
        list.ForEach(item =>
        {
            _output.WriteLine(item.ToString());
        });
        _output.WriteLine($"总共有:{list.Count}条消息");
    }

    [Theory(DisplayName = "测试按日期获取历史消息")]
    [InlineData("WeChatAuto.SDK官方技术支持")]
    // [InlineData("前端攻城狮")]
    [InlineData("李金龙_vip")]
    [InlineData("[9]Senparc微信视频课程学员群")]
    public async Task Test_Get_ChatHistory(string who)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var list = await client.GetChatHistory(who, DateTime.Parse("2026-05-15"));
        Assert.True(list.Count != 0);
        list.ForEach(item =>
        {
            _output.WriteLine(item.ToString());
        });
        _output.WriteLine($"总共有:{list.Count}条消息");
    }

    [Theory(DisplayName = "测试按日期-开始时间-结束时间获取历史消息")]
    [InlineData("郭老总_vip")]
    [InlineData("前端攻城狮")]
    public async Task Test_GetChatHistory_startdate_enddate(string who)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var list = await client.GetChatHistory(who, DateTime.Parse("2026-05-12"), DateTime.Parse("2026-05-15"));
        Assert.True(list.Count != 0);
        list.ForEach(item =>
        {
            _output.WriteLine(item.ToString());
        });
        _output.WriteLine($"总共有:{list.Count}条消息");
    }

    [Theory(DisplayName = "测试按多个指定日期获取历史消息")]
    [InlineData("郭老总_vip")]
    [InlineData("前端攻城狮")]
    public async Task Test_GetChatHistory_multx_date(string who)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var list = await client.GetChatHistory(who, new List<DateTime>() { DateTime.Parse("2026-05-12"), DateTime.Parse("2026-05-15") });
        Assert.True(list.Count != 0);
        list.ForEach(item =>
        {
            _output.WriteLine(item.ToString());
        });
        _output.WriteLine($"总共有:{list.Count}条消息");
    }
}