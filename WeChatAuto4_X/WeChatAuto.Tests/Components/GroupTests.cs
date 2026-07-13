using System.Diagnostics;
using Microsoft.VisualBasic.ApplicationServices;
using NAudio.Wave;
using OneOf;
using Xunit.Abstractions;
using NAudio.CoreAudioApi;
using WeAutoCommon.Utils;
using System.Text.RegularExpressions;


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

    [Fact(DisplayName = "测试新建群")]
    public async Task Test_CreateOwnerChatGroup()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.CreateOwnerChatGroup("DroidMirror官方技术支持", "AI.Net", new string[] { "智影工坊_test" });
        Assert.True(result.Success);
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
        var result = await client.ChangeOwnerChatGroupName("DroidMirror官方技术支持", "DroidMirror官方技术支持333");
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

    [Fact(DisplayName = "测试修改群公告")]

    public async Task Test_ChangeOwnerChatGroupNotice()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.UpdateGroupNotice("DroidMirror官方技术支持", """
使用 DroidMirror 过程中如果遇到问题，欢迎随时反馈，我们会尽快处理和优化🚀🚀。
如果有好的功能建议或改进想法，也欢迎一起讨论。
👉 遇到不会用的地方也不用担心，我们会尽量帮大家解答。
""");
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


    [Theory(DisplayName = "邀请群聊成员,适用于外部群")]
    [InlineData("")]
    [InlineData("人工智能自动化技术讨论群")]
    [InlineData("人工智能自动化技术讨论群22")]
    public async Task Test_InviteChatGroupMember(string groupName)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.InviteChatGroupMember(groupName, new List<string> { "khcgb", "秋歌" }, "仅是一个测试");
        Assert.True(result.Success);
    }

    [Theory(DisplayName = "他有群里加好友")]
    [InlineData("人工智能自动化技术讨论群")]
    [InlineData("测试sss")]
    [InlineData("")]
    public async Task Test_OuterGroup_AddFriend(string groupName)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var list = new List<string>();
        if (string.IsNullOrWhiteSpace(groupName))
        {
            list = await client.GetChatGroupMemberList("");
        }
        else
        {
            list = await client.GetChatGroupMemberList(groupName);
        }
        list = list.Skip(21).ToList();
        var result = await client.AddChatGroupMemberToFriends(groupName, list, new Options.AddFriendsOptions
        {
            SayHi = "测试自动化群里加好友，不用理会，如有打扰，海涵😻",
            Suffix = "test",
            Label = "wechatauto22"
        });
        foreach (var item in result)
        {
            _output.WriteLine($"{item.Key}  {item.Value.ToString()}");
        }
        Assert.True(result.Keys.Count > 0);
    }
}