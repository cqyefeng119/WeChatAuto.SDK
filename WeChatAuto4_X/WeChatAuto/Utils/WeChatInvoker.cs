using System.Diagnostics;
using FlaUI.Core.Capturing;
using WeChatAuto.Services;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using NAudio.Wave;
using FlaUI.UIA3;
using WeChatAuto.Components;

namespace WeChatAuto.Utils
{
    /// <summary>
    /// 微信执行器
    /// </summary>
    public class WeChatInvoker
    {
        /// <summary>
        /// 执行方法
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static async Task Call(Action<UIA3Automation> action)
        {
            if (System.Threading.Thread.CurrentThread.ManagedThreadId == WeChatClientFactory.MainActionThreadInvoker.ActionThreadId)
            {
                action(WeChatClientFactory.MainActionThreadInvoker.Automation);
                await Task.CompletedTask;
                return;
            }
            await WeChatClientFactory.MainActionThreadInvoker.Run(automation =>
            {
                action(automation);
            }).ConfigureAwait(false);
        }
        /// <summary>
        /// 执行方法
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static async Task Call(Action action)
        {
            if (System.Threading.Thread.CurrentThread.ManagedThreadId == WeChatClientFactory.MainActionThreadInvoker.ActionThreadId)
            {
                action();
                await Task.CompletedTask;
                return;
            }
            await WeChatClientFactory.MainActionThreadInvoker.Run(automation =>
            {
                action();
            }).ConfigureAwait(false);
        }
        /// <summary>
        /// 执行带一个参数的方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static async Task Call<T>(Action<UIA3Automation, T> action, T t)
        {
            if (System.Threading.Thread.CurrentThread.ManagedThreadId == WeChatClientFactory.MainActionThreadInvoker.ActionThreadId)
            {
                action(WeChatClientFactory.MainActionThreadInvoker.Automation, t);
                await Task.CompletedTask;
                return;
            }
            await WeChatClientFactory.MainActionThreadInvoker.Run(automation =>
            {
                action(automation, t);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 执行一个返回一个函数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Func"></param>
        /// <returns></returns>
        public static async Task<T> Call<T>(Func<UIA3Automation,T> Func)
        {
            if (System.Threading.Thread.CurrentThread.ManagedThreadId == WeChatClientFactory.MainActionThreadInvoker.ActionThreadId)
            {
                return await Task.FromResult(Func(WeChatClientFactory.MainActionThreadInvoker.Automation));
            }
            return await WeChatClientFactory.MainActionThreadInvoker.Run(automation =>
            {
                return Func(automation);
            }).ConfigureAwait(false);
        }
    }
}