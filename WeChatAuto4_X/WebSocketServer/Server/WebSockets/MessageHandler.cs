using System.Diagnostics;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using WeAutoCommon.Enums;
using WeChatAuto.Components;

public class MessageHandler
{
    private readonly WeChatClientFactory factory;
    private readonly ILogger<MessageHandler> logger;

    public MessageHandler(WeChatClientFactory factory, ILogger<MessageHandler> logger)
    {
        this.factory = factory;
        this.logger = logger;
    }
    public async Task<RequestData> HandleAsync(MessagePackageWrapper wrapper)
    {
        var client = factory.GetWeChatClient(wrapper.FromWechat);
        RequestData response = new RequestData
        {
            Type = "echo",
            RequestId = wrapper.RequestId,
            Data = "",
        };
        switch (wrapper.FuncName)
        {
            case "GetOwerInfo":
                await _ProcessOwnerInfo(client, response);
                break;
            case "Max":
                await _ProcessMaxWindow(client, response);
                break;
            case "Restore":
                await _RestoreWindow(client, response);
                break;
            case "Pinned":
                await _PinedWidnow(client, response);
                break;
            case "UnPinned":
                await _UnPinnedWindow(client, response);
                break;
            case "Focus":
                await _FocusWindow(client, response);
                break;
            case "CloseSearchWindow":
                await _CloseSearchWindow(client, response, wrapper.Options!);
                break;
            case "OpenSubWin":
                await client.OpenSubWin(wrapper.Options);
                break;
            case "GetHandler":
                var handler = client.GetHandler();
                response.Data = handler.ToString();
                break;
            case "GetProcessId":
                var processId = client.GetProcessId();
                response.Data = processId.ToString();
                break;
            case "SwitchNavigation":
                await client.SwitchNavigation((NavigationType)Enum.Parse(typeof(NavigationType), wrapper.Options!));
                break;
            case "CloseNavWin":
                await client.CloseNavWin((NavigationType)Enum.Parse(typeof(NavigationType), wrapper.Options!));
                break;
            case "ClickNotifyIcon":
                if (int.TryParse(wrapper.Options!, out var result))
                {
                    await client.ClickNotifyIcon(result);
                }
                else
                {
                    await client.ClickNotifyIcon(wrapper.Options!);
                }
                break;
            case "GetAllConversations":
                var rList = await client.GetAllConversations();
                response.Data = JsonConvert.SerializeObject(rList);
                break;
            case "GetVisibleConversationTitles":
                var vList = await client.GetVisibleConversationTitles();
                response.Data = JsonConvert.SerializeObject(vList);
                break;
            case "GetVisibleConversations":
                var cobjList = await client.GetVisibleConversations();
                response.Data = JsonConvert.SerializeObject(cobjList);
                break;
            case "SearchFriend":
                var searchResult = await client.SearchFriend(wrapper.Options!);
                response.Data = searchResult.ToString();
                break;
            case "LocateConversation":
                var locateResult = await client.LocateConversation(wrapper.Options!);
                response.Data = locateResult.ToString();
                break;
            case "SetDoNotDisturb":
                var options = JsonConvert.DeserializeObject<Dictionary<string, object>>(wrapper.Options!);
                var donotDisturbResult = await client.SetDoNotDisturb(options!["who"].ToString(), bool.Parse(options!["setting"].ToString()!));
                response.Data = donotDisturbResult.ToString();
                break;
            case "SetTopMost":
                var topMostOptions = JsonConvert.DeserializeObject<Dictionary<string, object>>(wrapper.Options!);
                var setTopMostResult = await client.SetTopMost(topMostOptions!["who"].ToString(), bool.Parse(topMostOptions!["setting"].ToString()!));
                response.Data = setTopMostResult.ToString();
                break;
            default:
                throw new Exception("不支持的函数名!");
        }
        return response;
    }

    private async Task _CloseSearchWindow(WeChatClient client, RequestData response, string who)
    {
        await client.CloseSearchWindow(who);
        response.Data = "";
    }

    private async Task _FocusWindow(WeChatClient client, RequestData response)
    {
        await client.Focus();
        response.Data = "";
    }

    private async Task _UnPinnedWindow(WeChatClient client, RequestData response)
    {
        await client.Focus();
        await client.UnPinned();
        response.Data = "";
    }

    private async Task _PinedWidnow(WeChatClient client, RequestData response)
    {
        await client.Focus();
        await client.Pinned();
        response.Data = "";
    }

    private async Task _RestoreWindow(WeChatClient client, RequestData response)
    {
        await client.Focus();
        await client.Restore();
        response.Data = "";
    }

    //Max
    private async Task _ProcessMaxWindow(WeChatClient client, RequestData response)
    {
        await client.Focus();
        await client.Max();
        response.Data = "";
    }

    //GetOwerInfo()
    private async Task _ProcessOwnerInfo(WeChatClient client, RequestData response)
    {
        var info = client.GetOwerInfo();
        response.Data = JsonConvert.SerializeObject(info);
    }
}