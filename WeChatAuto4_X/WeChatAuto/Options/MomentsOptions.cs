using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OneOf;
using WeChatAuto.Components;
using WeChatAuto.Models;

namespace WeChatAuto.Options
{
    /// <summary>
    /// 朋友圈选项
    /// </summary>
    public class MomentsOptions
    {
        /// <summary>
        /// 朋友圈被@的好友
        /// </summary>
        public OneOf<string, List<string>> AtUsrs { get; set; } = default;

        /// <summary>
        /// 朋友圈哪些设定的标签可以看，如果没有设置标签，则全部可见.
        /// </summary>
        public OneOf<string, List<string>> Labels { get; set; } = default;

        /// <summary>
        /// 是否执行操作后关闭朋友圈,默认关闭，也可以设置为False,然后使用者可以手动关闭<see cref="WeChatClient.CloseMoments"/>
        /// </summary>
        public bool IsCloseMoments { get; set; } = true;
    }
}