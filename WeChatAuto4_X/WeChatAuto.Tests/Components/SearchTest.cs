using System.Diagnostics;
using OneOf;
using Xunit.Abstractions;

namespace WeChatAuto.Tests.Components;


[Collection("UiTestCollection")]
public class SearchTest
{
    private readonly string _wxClientName = "Alex";
    private readonly ITestOutputHelper _output;
    private UiTestFixture _globalFixture;
    public SearchTest(ITestOutputHelper output, UiTestFixture globalFixture)
    {
        _output = output;
        _globalFixture = globalFixture;
    }
    [Fact(DisplayName = "打开关闭新增好友窗口")]
    public async Task Test_open_close_addfriend()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.OpenAddFriensWin();
        await Task.Delay(5000);
        await client.CloseAddFriendWin();
        Assert.True(true);
    }

    [Fact(DisplayName = "打开新增好友窗口")]
    public async Task Test_open_addfriend()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.OpenAddFriensWin();
        Assert.True(true);
    }

    [Fact(DisplayName = "关闭新增好友窗口")]
    public async Task Test_close_addfriend()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.CloseAddFriendWin();
        Assert.True(true);
    }

    [Fact(DisplayName = "新增好友")]
    public async Task Test_addfriend()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.AddFriends(new string[] { "18978694189", "13719238557", "13719238558" },
        new Options.AddFriendsOptions
        {
            IntervalTime = 4,
            IsCloseWin = true,
        });
        Assert.True(true);
    }

}