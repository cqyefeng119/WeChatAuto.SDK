using System.Diagnostics;
using Microsoft.VisualBasic.ApplicationServices;
using NAudio.Wave;
using OneOf;
using Xunit.Abstractions;
using NAudio.CoreAudioApi;
using WeChatAuto.Utils;
using FlaUI.Core.WindowsAPI;
using WeAutoCommon.Utils;


namespace WeChatAuto.Tests.Utils;

[Collection("UiTestCollection")]
public class SupperMouseKeyTests
{
    [Fact(DisplayName = "ctrl+a然后back")]
    public void Test_Ctrl_A_Back()
    {
        //请把焦点放到位置上
        // SupperMouseKey.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);

        // RandomWait.Wait(100, 300);
        // SupperMouseKey.TypeSimultaneously(VirtualKeyShort.BACK);
        SupperMouseKey.Scroll(-3);
        RandomWait.Wait(100, 300);
        SupperMouseKey.Scroll(-3);
        RandomWait.Wait(100, 300);
        SupperMouseKey.Scroll(-3);
        RandomWait.Wait(100, 300);
        SupperMouseKey.Scroll(-3);
        RandomWait.Wait(100, 300);
        SupperMouseKey.Scroll(-3);
        RandomWait.Wait(100, 300);
        SupperMouseKey.Scroll(-3);
        RandomWait.Wait(100, 300);
        SupperMouseKey.Scroll(-3);
        RandomWait.Wait(100, 300);

        Assert.True(true);
    }
}