using System.Diagnostics;
using OneOf;
using WeAutoCommon.Models;
using WeChatAuto.Models;
using Xunit.Abstractions;

namespace WeChatAuto.Tests.Components;


[Collection("UiTestCollection")]
public class MonitorTest
{
    private readonly string _wxClientName = "Alex";
    private readonly ITestOutputHelper _output;
    private UiTestFixture _globalFixture;
    public MonitorTest(ITestOutputHelper output, UiTestFixture globalFixture)
    {
        _output = output;
        _globalFixture = globalFixture;
    }

    [Fact(DisplayName = "开放式监听")]
    public async Task Test_Message_Monitor_open()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.AddMessageListener("", (context) =>
        {

        }, true);
        await Task.Delay(-1);
    }

    [Fact(DisplayName = "固定好友、群监听")]
    public async Task Test_Message_Monitor_friend()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.AddMessageListener(new string[] { "AI.Net", "DroidMirror官方技术支持" }, (context) =>
        {

        }, false);
        await Task.Delay(-1);
    }

    [Theory(DisplayName = "测试开始时间-结束时间的消息监听")]
    [InlineData("", "")]
    public async Task Test_Message_Monitor_friend_starendtime(string start, string end)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var t1 = TimeOnly.ParseExact(start, "HH:mm");
        var t2 = TimeOnly.ParseExact(end, "HH:mm");
        await client.AddMessageListener(new string[] { "AI.Net", "DroidMirror官方技术支持" }, (context) =>
        {

        }, t1, t2, false);
        await Task.Delay(-1);
    }

    [Fact(DisplayName = "测试多个时间段的监听")]
    public async Task Test_Message_Monitor_friend_muti_rang()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        List<TimeOnlyRange> list = new List<TimeOnlyRange>();
        list.Add(new TimeOnlyRange
        {

        });

        list.Add(new TimeOnlyRange
        {

        });
        await client.AddMessageListener(new string[] { "AI.Net", "DroidMirror官方技术支持" }, (context) =>
        {

        }, list, false);
        await Task.Delay(-1);
    }

    [Theory(DisplayName = "测试获取好友信息选项")]
    [InlineData("AI.Net_test")]
    [InlineData("秋歌")]
    public async Task Test_Fetch_Friend_Info(string who)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.MessageMonitor.FetchFriendInfo(who);
        FriendInfo friendInfo = client.GetFriendFromCache(who);
        _output.WriteLine(friendInfo.ToString());
        Debug.Assert(friendInfo != null);
    }

}