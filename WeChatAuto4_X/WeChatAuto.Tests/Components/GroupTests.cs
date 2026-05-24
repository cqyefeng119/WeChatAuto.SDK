using System.Diagnostics;
using Microsoft.VisualBasic.ApplicationServices;
using NAudio.Wave;
using OneOf;
using Xunit.Abstractions;
using NAudio.CoreAudioApi;
using WeAutoCommon.Utils;


namespace WeChatAuto.Tests.Components;


[Collection("UiTestCollection")]
public class GroupTests
{
    private readonly string _wxClientName = "Alex";
    private readonly ITestOutputHelper _output;
    private UiTestFixture _globalFixture;
    public GroupTests(ITestOutputHelper output, UiTestFixture globalFixture)
    {
        _output = output;
        _globalFixture = globalFixture;
    }


    [Theory(DisplayName = "获取群聊群主名称")]
    [InlineData("DroidMirror官方技术支持", "Alex")]
    [InlineData("RapidAI高手群聚集", "王佳华(SWHL)")]
    [InlineData("WeChatAuto.SDK官方技术支持", "AI.Net_test")]
    [InlineData("人工智能自动化技术讨论群", "AI.Net_test")]
    [InlineData("前端攻城狮", "远方os")]
    public async Task Test_GetGroupOwner(string name, string result)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var groupOwner = await client.GetGroupOwner(name);
        Assert.True(groupOwner == result);
    }

    [Theory(DisplayName = "测试是否自有群")]
    [InlineData("DroidMirror官方技术支持", true)]
    [InlineData("RapidAI高手群聚集", false)]
    [InlineData("WeChatAuto.SDK官方技术支持", false)]
    [InlineData("人工智能自动化技术讨论群", false)]
    [InlineData("前端攻城狮", false)]
    public async Task Test_IsOwnerChatGroup(string name, bool result)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var groupOwner = await client.GetGroupOwner(name);
        Assert.True((groupOwner == client.NickName) == result);
    }
    [Theory(DisplayName = "测试拉好友加群")]
    [InlineData("DroidMirror官方技术支持")]
    [InlineData("WeChatAuto.SDK官方技术支持")]
    [InlineData("人工智能自动化技术讨论群")]
    public async Task Test_AddOwnerChatGroupMember(string name)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.AddOwnerChatGroupMember(name, new string[] { "AI.Net_test", "秋歌", "智影工坊_test", "khcgb" });
        Assert.True(true);
    }

}