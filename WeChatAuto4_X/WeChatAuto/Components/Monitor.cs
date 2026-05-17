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

        /// <summary>
        /// <para>构造器，不应该自行调用</para>
        /// </summary>
        /// <param name="client"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="_uiMainThreadInvoker"></param>
        internal Monitor(WeChatClient client, IServiceProvider serviceProvider, UIThreadInvoker _uiMainThreadInvoker)
        {
            this._Client = client;
            this.serviceProvider = serviceProvider;
            this._MainThreadInvoker = _uiMainThreadInvoker;
        }

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

            }
        }
        #endregion
    }
}