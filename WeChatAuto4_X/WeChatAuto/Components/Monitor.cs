using System;
using System.Collections.Generic;
using System.Linq;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using WeAutoCommon.Enums;
using WeAutoCommon.Interface;
using WeAutoCommon.Utils;
using WeChatAuto.Extentions;
using WeChatAuto.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using WeAutoCommon.Models;
using System.Threading.Tasks;
using System.Drawing;
using FlaUI.Core;
using FlaUI.Core.Identifiers;
using FlaUI.Core.Conditions;
using System.IO;
using FlaUI.UIA3;
using WeAutoCommon.Extentions;
using System.Threading;
using System.Diagnostics;

namespace WeChatAuto.Components
{
    /// <summary>
    /// 监听器
    /// </summary>
    public class Monitor : IDisposable
    {
        private int _disposed = 0;
        private WeChatClient _Client;
        private IServiceProvider serviceProvider;
        private UIThreadInvoker _MainThreadInvoker;
        private ManualResetEvent noticeEvent;

        private Thread MonitorThread;  //单微信总消息监听线程
        private TaskCompletionSource<bool> _started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private CancellationTokenSource cts = new CancellationTokenSource();
        private AutoLogger<Monitor> _Logger;

        /// <summary>
        /// <para>构造器，不应该自行调用</para>
        /// </summary>
        /// <param name="client"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="event"></param>
        /// <param name="_uiMainThreadInvoker"></param>
        internal Monitor(WeChatClient client, IServiceProvider serviceProvider, UIThreadInvoker _uiMainThreadInvoker, ManualResetEvent resetEvent)
        {
            this._Client = client;
            this.serviceProvider = serviceProvider;
            this._MainThreadInvoker = _uiMainThreadInvoker;
            this.noticeEvent = resetEvent;

            _Logger = serviceProvider.GetRequiredService<AutoLogger<Monitor>>();
            InitMonitorThread();
            if (!_started.Task.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("Monitor startup timeout.");
            }
        }
        private void InitMonitorThread()
        {
            MonitorThread = new Thread(MonitorTotalMessage);
            MonitorThread.IsBackground = true;
            MonitorThread.Priority = ThreadPriority.Lowest;
            MonitorThread.SetApartmentState(ApartmentState.MTA);
            MonitorThread.Start();
        }


        private void MonitorTotalMessage()
        {
            UIA3Automation automation = null;
            try
            {
                automation = new UIA3Automation();
                _started.TrySetResult(true);
                while (!cts.Token.IsCancellationRequested)
                {
                    MonitorTotalMessageCore(automation);
                    cts.Token.WaitHandle.WaitOne(10 * 1000);
                }
            }
            catch (OperationCanceledException)
            {
                if (!_started.Task.IsCompleted)
                    _started.TrySetCanceled();
            }
            catch (Exception ex)
            {
                if (!_started.Task.IsCompleted)
                    _started.TrySetException(ex);
                _Logger.Error($"{nameof(Monitor)} - {nameof(MonitorTotalMessage)}: {ex.ToString()}");
            }
            finally
            {
                automation?.Dispose();
            }
        }

        private void MonitorTotalMessageCore(UIA3Automation automation)
        {
            var deskTop = automation.GetDesktop();
            var windowResult = Retry.WhileNull(() => deskTop.FindFirstChild(cf => cf.ByClassName("mmui::MainWindow").And((cf.ByName("微信").Or(cf.ByName(" 微信")))).And(cf.ByControlType(ControlType.Window)).And(cf.ByProcessId(_Client.MainWindow.Properties.ProcessId))),
            timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200));
            if (windowResult.Success)
            {
                var window = windowResult.Result.AsWindow();
                var root = _GetToolBarRoot(window);
                var path = @"/Button[@Name='微信']/Group[2]";
                var numberGroup = root.FindFirstByXPath(path);
                numberGroup.CaptureToFile(@"c:\1212.png");
                _Logger.Info(numberGroup.IsElementActuallyVisible(window.Properties.NativeWindowHandle) ? "没有被挡住" : "被挡住");
            }
        }

        private AutomationElement _GetToolBarRoot(Window window)
        {
            var toolBarRetry = Retry.WhileNull(() => window.FindFirstDescendant(cf => cf.ByAutomationId("MainView.main_tabbar").And(cf.ByControlType(ControlType.ToolBar).And(cf.ByName("导航")))), timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200));
            return toolBarRetry.Success ? toolBarRetry.Result : null;
        }

        #region 消息监听

        #endregion

        #region 会话切换监听
        #endregion

        #region 新好友申请监听

        #endregion

        #region 添加朋友圈监听
        #endregion

        #region 释放器
        ~Monitor()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
                return;
            if (disposing)
            {
                cts?.Cancel();
                try
                {
                    MonitorThread.Join(TimeSpan.FromSeconds(5));
                }
                catch (OperationCanceledException) { }
                catch (AggregateException) { }

                cts?.Dispose();

            }
        }
        #endregion
    }
}