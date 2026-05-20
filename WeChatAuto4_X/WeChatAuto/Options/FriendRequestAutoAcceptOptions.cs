using System;
using System.Collections.Generic;
using System.Threading;
using WeChatAuto.Components;

namespace WeChatAuto.Options
{
    public class FriendRequestAutoAcceptOptions
{
    /// <summary>
    /// 通过后的回调
    /// </summary>
    public Action<List<string>, WeChatClient, IServiceProvider> PassedCallBack { get; set; }

    /// <summary>
    /// 通过后是否删除申请记录
    /// </summary>
    public bool PassedDelete { get; set; } = true;

    /// <summary>
    /// 打招呼关键词过滤
    /// </summary>
    public string KeyWord { get; set; }

    /// <summary>
    /// 好友备注后缀
    /// 如果设置后缀，被通过的好友会自动加上此后缀,如:AI.Net_Test
    /// </summary>
    public string Suffix { get; set; }

    /// <summary>
    /// 微信标签
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// 取消令牌
    /// </summary>
    public CancellationToken TokenSource { get; set; }

    /// <summary>
    /// UI线程调度器
    /// UI的调度器，适用于把微信嵌入UI的场景使用，如：多微信切换Tab页等,SDK会给调用者注入一个微信名称
    /// </summary>
    public Action<string> UIInvoker { get; set; }
}
}