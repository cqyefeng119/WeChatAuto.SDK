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
        var list = await client.GetChatHistory(DateTime.Parse("2026-06-14"));
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
    [InlineData("苏智明_vip")]
    [InlineData("软件作家涛哥_vip")]
    [InlineData("[9]Senparc微信视频课程学员群")]
    public async Task Test_Get_ChatHistory(string who)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var list = await client.GetChatHistory(who, DateTime.Parse("2026-05-27"));
        Assert.True(list.Count != 0);
        list.ForEach(item =>
        {
            _output.WriteLine(item.ToString());
        });
        _output.WriteLine($"总共有:{list.Count}条消息");
    }

    [Theory(DisplayName = "测试按日期-开始时间-结束时间获取历史消息")]
    [InlineData("郭老总_vip")]
    [InlineData("软件作家涛哥_vip")]
    [InlineData("苏智明_vip")]
    public async Task Test_GetChatHistory_startdate_enddate(string who)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var list = await client.GetChatHistory(who, DateTime.Parse("2026-05-27 11:08"), DateTime.Parse("2026-05-27 11:20"));
        Assert.True(list.Count != 0);
        list.ForEach(item =>
        {
            _output.WriteLine(item.ToString());
        });
        _output.WriteLine($"总共有:{list.Count}条消息");
    }
}