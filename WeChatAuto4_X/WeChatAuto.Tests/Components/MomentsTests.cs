using System.Diagnostics;
using OneOf;
using Xunit.Abstractions;

namespace WeChatAuto.Tests.Components;


[Collection("UiTestCollection")]
public class MomentsTests
{
    private readonly string _wxClientName = "Alex";
    private readonly ITestOutputHelper _output;
    private UiTestFixture _globalFixture;
    public MomentsTests(ITestOutputHelper output, UiTestFixture globalFixture)
    {
        _output = output;
        _globalFixture = globalFixture;
    }

    [Fact(DisplayName = "打开朋友圈-关闭朋友圈")]
    public async Task Test_open_close_moments()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.OpenMoments();
        await Task.Delay(5000);
        await client.CloseMoments();
        Assert.True(true);
    }

    [Fact(DisplayName = "发朋友圈")]
    public async Task Test_add_moments()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        var result = await client.AddMoments(new List<string> {Path.Combine(AppContext.BaseDirectory, "Assets", "1.png"),
        Path.Combine(AppContext.BaseDirectory,"Assets","pzllm.png") }, "仅是一个测试");
        Assert.True(result);
    }
}