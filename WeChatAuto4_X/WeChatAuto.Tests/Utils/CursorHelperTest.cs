using System.Diagnostics;
using Microsoft.VisualBasic.ApplicationServices;
using NAudio.Wave;
using OneOf;
using Xunit.Abstractions;
using NAudio.CoreAudioApi;
using WeChatAuto.Utils;


namespace WeChatAuto.Tests.Utils;


public class CursorHelperTest
{
    private readonly ITestOutputHelper _output;
    public CursorHelperTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(DisplayName = "测试当前光标")]
    public void Test_Current_Curssor()
    {
        var handle = CursorHelper.GetCurrentCursorHandle();
        _output.WriteLine($"handle:{handle}");

        Assert.True(true);
    }
}