using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace WeChatAuto.Utils
{
    /// <summary>
    /// Windows剪贴板工具类
    /// </summary>
    public static class ClipboardHelper
    {
        public static void SetText(string text)
        {
            Exception ex = null;

            var thread = new Thread(() =>
            {
                try
                {
                    System.Windows.Clipboard.SetText(text);
                }
                catch (Exception e)
                {
                    ex = e;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);

            thread.Start();
            thread.Join();

            if (ex != null)
                throw ex;
        }
    }
}