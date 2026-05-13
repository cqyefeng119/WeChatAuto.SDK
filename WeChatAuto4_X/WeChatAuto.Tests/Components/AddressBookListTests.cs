using System.Diagnostics;
using Microsoft.VisualBasic.ApplicationServices;
using NAudio.Wave;
using OneOf;
using Xunit.Abstractions;
using NAudio.CoreAudioApi;


namespace WeChatAuto.Tests.Components;


[Collection("UiTestCollection")]
public class AddressBookListTests
{
    private readonly string _wxClientName = "Alex";
    private readonly ITestOutputHelper _output;
    private UiTestFixture _globalFixture;
    public AddressBookListTests(ITestOutputHelper output, UiTestFixture globalFixture)
    {
        _output = output;
        _globalFixture = globalFixture;
    }


    [Fact(DisplayName = "测试获取所有好友")]
    public async Task Test_GetAllFriends()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var list = await client.GetAllFriends(false);
        Assert.True(list != null && list.Any());
        foreach (var friend in list)
        {
            _output.WriteLine($"{friend.ToString()}");
        }
        _output.WriteLine($"总好友数：{list.Count}");
        _output.WriteLine($"其中企业微信好友数：{list.Count(f => f.ChatType == WeAutoCommon.Enums.ChatType.企业微信)}");
        _output.WriteLine($"其中群聊好友数：{list.Count(f => f.ChatType == WeAutoCommon.Enums.ChatType.群聊)}");
        _output.WriteLine($"其中个人聊天好友数：{list.Count(f => f.ChatType == WeAutoCommon.Enums.ChatType.好友)}");
    }

    [Fact(DisplayName = "测试获取所有好友昵称")]
    public async Task Test_GetAllFriend_NickName()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var list = await client.GetAllFriendNames();
        Assert.True(list != null && list.Any());
        foreach (var friendName in list)
        {
            _output.WriteLine($"好友昵称：{friendName}");
        }
    }

}