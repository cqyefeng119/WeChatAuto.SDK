using System.Diagnostics;
using OneOf;
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
        var flag = await client.Conversations.LocateConversation(who);
        Assert.True(flag);
    }

    [Fact(DisplayName = "测试向上滚动")]
    public async Task Test_Scroll_Up()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.Conversations.Up((items, rect) =>
        {
            return true;
        });
    }

    [Fact(DisplayName = "测试向下滚动")]
    public async Task Test_Scroll_Down()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.Conversations.Down((items, rect) =>
        {
            return true;
        });
    }


    [Fact(DisplayName = "测试向下滚动后向上滚动")]
    public async Task Test_Scroll_Down_up()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.Conversations.Down((items, rect) =>
        {
            return true;
        });
        await client.Conversations.Up((items, rect) =>
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

    [Theory(DisplayName = "测试搜索")]
    [InlineData("秋歌")]
    [InlineData("师父")]
    [InlineData("女女")]
    [InlineData("AI.Net")]
    [InlineData("梁世京")]
    public async Task Test_Search_Who(string who)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.Search(who);
        Assert.True(result);
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
        var result = await client.Search(who);
        Assert.False(result);
    }
}