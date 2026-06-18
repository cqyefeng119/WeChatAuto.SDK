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
        await client.AddOwnerChatGroupMember(name, new string[] { "智影工坊_test" });
        Assert.True(true);
    }

    [Theory(DisplayName = "测试群内删人")]
    [InlineData("DroidMirror官方技术支持")]
    [InlineData("人工智能自动化技术讨论群")]
    public async Task Test_RemoveOwnerChatGroupMember(string name)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.RemoveOwnerChatGroupMember(name, new string[] { "智影工坊_test", "AI.Net", "秋歌", "khcgb" });
        Assert.True(true);
    }

    [Fact(DisplayName = "测试群内删人_本窗口")]
    public async Task Test_RemoveOwnerChatGroupMember_Fosuse()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.RemoveOwnerChatGroupMember(new string[] { "智影工坊_test" });
        Assert.True(true);
    }

    [Fact(DisplayName = "测试新建群")]
    public async Task Test_CreateOwnerChatGroup()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.CreateOwnerChatGroup("我的测试02", "AI.Net", new string[] { "智影工坊_test" });
        Assert.True(result.Success);
    }

    [Fact(DisplayName = "测试退出群_本窗口")]
    public async Task Test_QuitGroup_this()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.QuitChatGroup();
        Assert.True(true);
    }

    [Fact(DisplayName = "测试退出群")]
    public async Task Test_QuitGroup()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.QuitChatGroup("DroidMirror官方技术支持");
        Assert.True(true);
    }

    [Fact(DisplayName = "测试修改自有群名")]
    public async Task Test_ChangeOwnerChatGroupName()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.ChangeOwnerChatGroupName("DroidMirror官方技术支持222", "DroidMirror官方技术支持333");
        Assert.True(result.Success);
    }

    [Theory(DisplayName = "测试修改群备注")]
    [InlineData("DroidMirror官方技术支持", "aaa")]
    [InlineData("aaa", "DroidMirror官方技术支持")]
    public async Task Test_ChangeOwnerChatGroupMemo(string groupName, string memo)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.ChangeChatGroupMemo(groupName, memo);
        Assert.True(result.Success);
    }

    [Theory(DisplayName = "测试修改群备注_本窗口")]
    [InlineData("aaa")]
    [InlineData("bbb")]
    [InlineData("")]
    public async Task Test_ChangeOwnerChatGroupMemo_thisWindow(string memo)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.ChangeChatGroupMemo(memo);
        Assert.True(result.Success);
    }


    [Theory(DisplayName = "测试修改在群的昵称")]
    [InlineData("DroidMirror官方技术支持", "aaa")]
    [InlineData("DroidMirror官方技术支持", "bbb")]
    [InlineData("DroidMirror官方技术支持", "")]
    public async Task Test_ChangeOwnerChatGroupNickName(string groupName, string nickName)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.ChangeChatGroupNickName(groupName, nickName);
        Assert.True(result.Success);
    }

    [Theory(DisplayName = "测试修改在群中的昵称_本窗口")]
    [InlineData("aaa")]
    [InlineData("bbb")]
    [InlineData("")]
    public async Task Test_ChangeOwnerChatGroupNickName_thisWindow(string nickName)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.ChangeChatGroupNickName(nickName);
        Assert.True(result.Success);
    }

    [Fact(DisplayName = "测试修改群公告")]

    public async Task Test_ChangeOwnerChatGroupNotice()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.UpdateGroupNotice("DroidMirror官方技术支持", "测试02");
        Assert.True(result.Success);
    }

    [Fact(DisplayName = "测试修改群公告_本窗口")]
    public async Task Test_ChangeOwnerChatGroupNotice_thisWindow()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.UpdateGroupNotice("测试03");
        Assert.True(result.Success);
    }

    [Theory(DisplayName = "获取群聊的成员列表")]
    [InlineData("DroidMirror官方技术支持")]
    [InlineData("歪脖子的模版交流群")]
    [InlineData("实时AI快讯 5群")]
    [InlineData("人工智能自动化技术讨论群")]
    public async Task Test_GetChatGroupMemberList(string groupName)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.GetChatGroupMemberList(groupName);
        Assert.True(result.Count > 0);
        foreach (var item in result)
        {
            _output.WriteLine(item);
        }
        _output.WriteLine($"群{groupName}有成员: {result.Count} 个");
    }

    [Fact(DisplayName = "获取群聊的成员列表_本窗口")]
    public async Task Test_GetChatGroupMemberList_thisWindow()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.GetChatGroupMemberList();
        Assert.True(result.Count > 0);
        foreach (var item in result)
        {
            _output.WriteLine(item);
        }
        _output.WriteLine($"本群有成员: {result.Count} 个"); ;
    }

}