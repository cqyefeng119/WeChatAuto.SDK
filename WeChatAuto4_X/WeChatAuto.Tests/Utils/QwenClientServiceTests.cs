using System.Diagnostics;
using Microsoft.VisualBasic.ApplicationServices;
using NAudio.Wave;
using OneOf;
using Xunit.Abstractions;
using NAudio.CoreAudioApi;
using WeChatAuto.Utils;
using WeChatAuto.Services;
using Microsoft.Extensions.DependencyInjection;


namespace WeChatAuto.Tests.Utils;

[Collection("UiTestCollection")]
public class QwenClientServiceTests
{
    private readonly string _wxClientName = "Alex";
    private readonly ITestOutputHelper _output;
    private UiTestFixture _globalFixture;
    public QwenClientServiceTests(ITestOutputHelper output, UiTestFixture globalFixture)
    {
        this._output = output;
        this._globalFixture = globalFixture;
    }

    [Fact(DisplayName = "测试HumenText")]
    public async Task Test_HumenText()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var service = client.Provider.GetRequiredService<QwenClientService>();
        var result = await service.HumenText(Environment.GetEnvironmentVariable("QWEN_API_KEY"), @"老张，今天真是累坏了。
        下午两点半开会一直开到五点多，期间老板又改了三次需求，我都快崩溃了。
        后来下楼买了杯咖啡，花了19.9元，又顺便买了个面包，一共27.5元。
        回家的时候已经晚上8:40了，路上还堵了半个小时。
        对了，下周三，也就是2026年8月5日上午10点，我们还要去客户那边演示系统。
        到时候你记得提前半小时到，别迟到了。
        如果有什么问题，直接打我手机13800138000，或者微信发我也行。
        哈哈哈，不过今天虽然累，事情总算做完了，晚上终于可以好好休息一下啦！
        ");
        _output.WriteLine(result);
        Assert.NotEmpty(result);

    }

}