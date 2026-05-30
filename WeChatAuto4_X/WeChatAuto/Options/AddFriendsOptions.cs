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
    /// 增加朋友选项
    /// </summary>
    public class AddFriendsOptions
    {
        /// <summary>
        /// 间隔时间,以秒为单位，默认为三秒，如果担心风控，可以把此时间设置长一点
        /// </summary>
        public int IntervalTime { get; set; } = 3;
        /// <summary>
        /// 是否关闭增加朋友窗口,默认关闭，可以设置为false不关闭
        /// </summary>
        public bool IsCloseWin {get;set;} = true;
    }
}