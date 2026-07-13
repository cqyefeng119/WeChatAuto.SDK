using System.Diagnostics;
using Microsoft.VisualBasic.ApplicationServices;
using NAudio.Wave;
using OneOf;
using Xunit.Abstractions;
using NAudio.CoreAudioApi;


namespace WeChatAuto.Tests.Components;


[Collection("UiTestCollection")]
public class ChatHeaderTests
{
    private readonly string _wxClientName = "Alex";
    private readonly ITestOutputHelper _output;
    private UiTestFixture _globalFixture;
    public ChatHeaderTests(ITestOutputHelper output, UiTestFixture globalFixture)
    {
        _output = output;
        _globalFixture = globalFixture;
    }

    [Fact(DisplayName = "测试获取当前窗口标题")]
    public async Task Test_Get_Title()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.GetTitle();
        _output.WriteLine(result.ToString());
        Assert.NotNull(result);
    }

}