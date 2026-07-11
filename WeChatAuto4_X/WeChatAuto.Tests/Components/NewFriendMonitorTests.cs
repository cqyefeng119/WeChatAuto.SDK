using System.Diagnostics;
using OneOf;
using WeAutoCommon.Utils;
using WeChatAuto.Models;
using Xunit.Abstractions;

namespace WeChatAuto.Tests.Components;


[Collection("UiTestCollection")]
public class NewFriendMonitorTests
{
    private readonly string _wxClientName = "Alex";
    private readonly ITestOutputHelper _output;
    private UiTestFixture _globalFixture;
    public NewFriendMonitorTests(ITestOutputHelper output, UiTestFixture globalFixture)
    {
        _output = output;
        _globalFixture = globalFixture;
    }
    [Fact(DisplayName = "测试通过新好友监听-关键词-标签-后缀")]
    public async Task Test_Monitor_NewFriend_Auto_Passed()
    {
        var framework = _globalFixture.clientFactory;
        var client = framework.GetWeChatClient(_wxClientName);
        await client.AddFriendRequestAutoAcceptListener(new Options.FriendRequestAutoAcceptOptions()
        {
            KeyWord = new List<string>(){"test"},
            Label = "测试标签",
            Suffix = "test",
            PassedCallBack = async (list, _client, serviceProvider) =>
            {
                foreach(var item in list)
                {
                    await _client.SendMessage(item.Who,$"你好 {item} :我已经通过你的申请，请问有什么可以帮到您？");
                    await RandomWait.WaitAsync(1000,2500);
                }
            }
        });
        await Task.Delay(-1);
    }
}