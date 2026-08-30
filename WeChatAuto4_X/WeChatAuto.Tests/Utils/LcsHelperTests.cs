using System.Diagnostics;
using Microsoft.VisualBasic.ApplicationServices;
using NAudio.Wave;
using OneOf;
using Xunit.Abstractions;
using NAudio.CoreAudioApi;
using WeChatAuto.Utils;


namespace WeChatAuto.Tests.Utils;

public class LcsHelperTest
{
    private readonly ITestOutputHelper _output;
    public LcsHelperTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(DisplayName = "测试Diff")]
    public void TestDiff()
    {
        var oldList = new[]
        {
            "A",
            "B",
            "C",
            "D",
            "E",
            "F"
        };

        var newList = new[]
        {
            "C",
            "D",
            "F",
            "H",
            "xxx撤消一条消息"
        };
        var diff = LcsHelper.Diff(oldList,newList);
        foreach(var item in diff)
        {
            _output.WriteLine($"{item.Type,-6}  {item.Value}");
        }
    }


}