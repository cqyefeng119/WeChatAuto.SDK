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
        var result = await service.HumenText(Environment.GetEnvironmentVariable("QWEN_API_KEY"), @"大家好，
        今天是2026年7月28日，现在是14:35。今天室外温度32℃，湿度68%。我刚刚支付了￥128.50，优惠了15%，订单号A202607280001。欢迎访问www.example.com，或者拨打400-800-1234咨询。我们的软件已经升级到V3.2.15，预计10:30开始发布，整个过程大约持续1.5小时。");
        _output.WriteLine(result);
        Assert.NotEmpty(result);

    }

}