using System.Diagnostics;
using OneOf;
using Xunit.Abstractions;

namespace WeChatAuto.Tests.Components;


[Collection("UiTestCollection")]
public class ShellNotifyIconTests
{
    private readonly string _wxClientName = "Alex";
    private readonly ITestOutputHelper _output;
    private UiTestFixture _globalFixture;
    public ShellNotifyIconTests(ITestOutputHelper output, UiTestFixture globalFixture)
    {
        _output = output;
        _globalFixture = globalFixture;
    }

    [Fact(DisplayName = "测试获取所有任务栏图标按钮")]
    public async Task TestGetButtons()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var buttons = await client.NotifyIcon.GetButtons();
        buttons[0].Click();
        Assert.True(true);
    }

    [Theory(DisplayName = "测试通过索引点击任务栏图标")]
    [InlineData(1)]
    [InlineData(2)]
    public async Task TestClickNotifyIcon(int index)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.ClickNotifyIcon(index);
        Assert.True(true);
    }
    [Theory(DisplayName = "测试通过索引点击任务栏图标")]
    [InlineData("AI.Net")]
    [InlineData("Alex")]
    public async Task TestClickNotifyIconByName(string name)
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.ClickNotifyIcon(name);
        Assert.True(true);
    }
}