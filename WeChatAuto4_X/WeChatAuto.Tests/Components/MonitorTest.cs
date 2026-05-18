using System.Diagnostics;
using OneOf;
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

    // [Fact(DisplayName = "测试截图")]
    // public async Task Test_Capture_image()
    // {
    //     var framework = _globalFixture.clientFactory;
    //     var client = framework.GetWeChatClient(_wxClientName);
    //     _output.WriteLine($"微信客户端名称: {client.NickName}");
    //     await Task.Delay(30000);
    // }
    // [Fact(DisplayName = "测试是否被挡住")]
    // public async Task Test_element_visible()
    // {
    //     var framework = _globalFixture.clientFactory;
    //     var client = framework.GetWeChatClient(_wxClientName);
    //     _output.WriteLine($"微信客户端名称: {client.NickName}");

    //     await Task.Delay(30000);
    // }
    [Fact(DisplayName = "测试开放式监听")]
    public async Task Test_Message_Monitor_open()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        client.AddMessageListener("", (context) =>
        {

        }, true);
        await Task.Delay(-1);
    }
}