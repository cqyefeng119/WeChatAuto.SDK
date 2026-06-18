using System.Diagnostics;
using OneOf;
using WeAutoCommon.Utils;
using Xunit.Abstractions;

namespace WeChatAuto.Tests.Components;


[Collection("UiTestCollection")]
public class ConversationList
{
    private readonly string _wxClientName = "Alex";
    private readonly ITestOutputHelper _output;
    private UiTestFixture _globalFixture;
    public ConversationList(ITestOutputHelper output, UiTestFixture globalFixture)
    {
        _output = output;
        _globalFixture = globalFixture;
    }

    [Theory(DisplayName = "测试定位标题")]
    [InlineData("师父")]
    public async Task Test_Location_Title(string who)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var flag = await client.LocateConversation(who);
        Assert.True(flag);
    }

    [Fact(DisplayName = "测试向上滚动")]
    public async Task Test_Scroll_Up()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.Up((items, rect) =>
        {
            return true;
        });
    }

    [Fact(DisplayName = "测试向下滚动")]
    public async Task Test_Scroll_Down()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.Down((items, rect) =>
        {
            return true;
        });
    }


    [Fact(DisplayName = "测试向下滚动后向上滚动")]
    public async Task Test_Scroll_Down_up()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.Down((items, rect) =>
        {
            return true;
        });
        await client.Up((items, rect) =>
        {
            return true;
        });
    }

    [Fact(DisplayName = "测试获取所有的会话标题")]
    public async Task Test_GetAllConversations()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var list = await client.GetAllConversations();
        Assert.True(list.Count() > 0);
        foreach (var item in list)
        {
            _output.WriteLine(item);
        }
    }

    [Fact(DisplayName = "测试获取所有可见的会话标题")]
    public async Task Test_GetVisibleConversationTitles()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var list = await client.GetVisibleConversationTitles();
        Assert.True(list.Count() > 0);
        foreach (var item in list)
        {
            _output.WriteLine(item);
        }
    }

    [Fact(DisplayName = "测试获取所有可见的会话对象")]
    public async Task Test_GetVisibleConversation_Object()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var list = await client.GetVisibleConversations();
        Assert.True(list.Count() > 0);
        foreach (var item in list)
        {
            _output.WriteLine(item.ToString());
        }
    }

    [Fact(DisplayName = "测试置顶")]
    public async Task Test_Top_Most()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.SetTopMost("DroidMirror官方技术支持", true);
        Assert.True(result);
    }

    [Fact(DisplayName = "测试反置顶")]
    public async Task Test_Invert_Top_Most()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.SetTopMost("秋歌", false);
        Assert.True(result);
    }

    [Fact(DisplayName = "测试消息免打扰")]
    public async Task Test_dont_DisturbCore()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.SetDoNotDisturb("秋歌", true);
        Assert.True(result);
    }

    [Fact(DisplayName = "测试取消消息免打扰")]
    public async Task Test_invert_dont_disturb()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.SetDoNotDisturb("秋歌", false);
        Assert.True(result);
    }

    [Theory(DisplayName = "测试搜索")]
    [InlineData("秋歌")]
    [InlineData("师父")]
    [InlineData("女女")]
    [InlineData("AI.Net_test")]
    [InlineData("梁世京")]
    [InlineData("WeChatAuto.SDK官方技术支持")]
    public async Task Test_Search_Who(string who)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.SearchFriend(who);
        Assert.True(result);
    }

    [Theory(DisplayName = "打开子窗口")]
    [InlineData("秋歌")]
    [InlineData("师父")]
    [InlineData("女女")]
    [InlineData("AI.Net_test")]
    [InlineData("梁世京")]
    [InlineData("WeChatAuto.SDK官方技术支持")]
    public async Task Test_Open_SubWin(string who)
    {
        RandomWait.Wait(1000, 2000);
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.OpenSubWin(who);
        Assert.True(result != null);
    }

    [Theory(DisplayName = "打开子窗口-错误情况")]
    [InlineData("秋歌2")]
    [InlineData("师父2")]
    [InlineData("女女2")]
    [InlineData("AI.Net_test")]
    [InlineData("梁世京2")]
    [InlineData("WeChatAuto.SDK官方技术支持2")]
    public async Task Test_Open_SubWin_error(string who)
    {
        RandomWait.Wait(1000, 2000);
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.OpenSubWin(who);
        Assert.True(result == null);
    }

    [Theory(DisplayName = "测试搜索（错误情况）")]
    [InlineData("秋歌2")]
    [InlineData("师父2")]
    [InlineData("女女2")]
    [InlineData("AI.Net2")]
    [InlineData("梁世京2")]
    public async Task Test_Search_Who_error(string who)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.SearchFriend(who);
        Assert.False(result);
    }

    [Theory(DisplayName = "测试搜索(群聊)")]
    [InlineData("测试他人群")]
    [InlineData("AI软件开发交流群")]
    [InlineData("RapidOCR3群")]
    [InlineData("猫哥上架互助群")]
    [InlineData("Admin.net官方")]
    public async Task Test_Search_Group(string who)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.SearchFriend(who);
        Assert.True(result);
    }

    [Theory(DisplayName = "测试搜索(群聊) - 错误")]
    [InlineData("测试他人群2")]
    [InlineData("AI软件开发交流群2")]
    [InlineData("RapidOCR3群2")]
    [InlineData("猫哥上架互助群2")]
    [InlineData("Admin.net官方2")]
    public async Task Test_Search_Group_error(string who)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.SearchFriend(who);
        Assert.False(result);
    }
}