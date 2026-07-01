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
using WeChatAuto.Models;

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
            case "GetTitle":
                var headInfo = await client.GetTitle();
                response.Data = JsonConvert.SerializeObject(headInfo);
                break;
            case "FocuseSenderInput":
                await client.FocuseSenderInput();
                break;
            case "GetOnlyTitle":
                var title = await client.GetOnlyTitle();
                response.Data = title;
                break;
            case "SendMessage":
                var sendMessageOptions = wrapper.Options;
                var optionsMessage = JsonConvert.DeserializeObject<Dictionary<string, string>>(sendMessageOptions!);
                await client.SendMessage(optionsMessage!["who"], optionsMessage!["message"], JsonConvert.DeserializeObject<List<string>>(optionsMessage!["atUser"]), JsonConvert.DeserializeObject<ChatRefer>(optionsMessage!["refer"]));
                break;
            case "SendEmoji":
                var emojiOptions = wrapper.Options;
                var emojiDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(emojiOptions!);
                var atUser = emojiDict!["atUser"].ToString().Equals("null") ? null : JsonConvert.DeserializeObject<List<string>>(emojiDict!["atUser"]);
                if (int.TryParse(emojiDict!["emoji"], out var emojiValue))
                {
                    await client.SendEmoji(emojiDict!["who"], emojiValue, atUser);
                }
                else
                {
                    await client.SendEmoji(emojiDict!["who"], emojiDict!["emoji"].ToString(), atUser);
                }

                break;
            case "SendFile":
                await _SendFile(wrapper, client);
                break;
            case "SendVoiceChat":
                var who = wrapper.Options!;
                await client.SendVoiceChat(who);
                break;
            case "SendVedioChat":
                who = wrapper.Options!;
                await client.SendVedioChat(who);
                break;
            case "SendVoiceChats":
                var payload = wrapper.Options!;
                var dicPayload = JsonConvert.DeserializeObject<Dictionary<string, string>>(payload!);
                await client.SendVoiceChats(dicPayload!["who"].ToString(), JsonConvert.DeserializeObject<string[]>(dicPayload!["partner"].ToString()));
                break;
            case "SendVoiceMessage":
                payload = wrapper.Options!;
                dicPayload = JsonConvert.DeserializeObject<Dictionary<string, string>>(payload!); ;
                who = dicPayload!["who"].ToString();
                //处理声音文件
                var localFileName = dicPayload["filePath"].ToString();
                var fileName = Path.GetFileName(localFileName);
                var pathRoot = Path.Combine(AppContext.BaseDirectory, "temp");
                if (!Directory.Exists(pathRoot))
                    Directory.CreateDirectory(pathRoot);
                pathRoot = Path.Combine(pathRoot, fileName);
                var bytes = Convert.FromBase64String(dicPayload!["upload"].ToString());
                await File.WriteAllBytesAsync(pathRoot, bytes);
                await Task.Delay(1000);
                //这里处理语音数据.
                if (string.IsNullOrEmpty(who))
                {
                    await client.SendVoiceMessage(pathRoot);
                }
                else
                {
                    await client.SendVoiceMessage(who, pathRoot);
                }
                break;
            case "GetChatHistory_Current_Window":
                payload = wrapper.Options!;
                var date = DateOnly.Parse(payload!);
                var fetchDate = date.ToDateTime(TimeOnly.Parse("00:00:00"));
                var list = await client.GetChatHistory(fetchDate);
                response.Data = JsonConvert.SerializeObject(list);
                break;
            case "GetChatHistory_Who":
                payload = wrapper.Options!;
                dicPayload = JsonConvert.DeserializeObject<Dictionary<string, string>>(payload);
                who = dicPayload!["who"];
                var fDate = DateTime.Parse(dicPayload!["fetch_date"]);
                list = await client.GetChatHistory(who, fDate);
                response.Data = JsonConvert.SerializeObject(list);
                break;
            case "TapWho":
                payload = wrapper.Options!;
                dicPayload = JsonConvert.DeserializeObject<Dictionary<string, string>>(payload);
                who = dicPayload!["who"];
                var prevNextNumber = int.Parse(dicPayload!["prev_scroll_number"]);
                var tapResult = await client.TapWho(who, prevNextNumber);
                response.Data = tapResult.ToString();
                break;
            case "ForwardMultipleMessage":
                payload = wrapper.Options!;
                dicPayload = JsonConvert.DeserializeObject<Dictionary<string, string>>(payload);
                who = dicPayload!["who"];
                var to = JsonConvert.DeserializeObject<List<string>>(dicPayload!["to"]);
                var fType = (ForwardMessageTypeEnums)Enum.Parse(typeof(ForwardMessageTypeEnums), dicPayload!["f_type"]);
                var rowCount = int.Parse(dicPayload["row_count"]);
                response.Data = (await client.ForwardMultipleMessage(who, to!.ToArray(), fType, rowCount)).ToString();
                break;
            case "ForwardSingleMessage":
                payload = wrapper.Options!;
                dicPayload = JsonConvert.DeserializeObject<Dictionary<string, string>>(payload)!;
                who = dicPayload["who"];
                var message = dicPayload["message"];
                to = JsonConvert.DeserializeObject<List<string>>(dicPayload["to"])!;
                var prev_scroll_number = int.Parse(dicPayload["prev_scroll_number"]);
                response.Data = (await client.ForwardSingleMessage(who, message, to.ToArray(), prev_scroll_number)).ToString();
                break;
            case "OpenAddFriensWin":
                await client.OpenAddFriensWin();
                break;
            case "CloseAddFriendWin":
                await client.CloseAddFriendWin();
                break;
            default:
                throw new Exception("不支持的函数名!");
        }
        return response;
    }

    private static async Task _SendFile(MessagePackageWrapper wrapper, WeChatClient client)
    {
        var payload = wrapper.Options!;
        var dicPayload = JsonConvert.DeserializeObject<Dictionary<string, string>>(payload!);
        var localFileNames = JsonConvert.DeserializeObject<List<string>>(dicPayload!["files"]);
        var localFiles = JsonConvert.DeserializeObject<Dictionary<string, string>>(dicPayload["upload"]);
        var pathRoot = Path.Combine(AppContext.BaseDirectory, "temp");
        if (!Directory.Exists(pathRoot))
            Directory.CreateDirectory(pathRoot);
        var sendFiles = new List<string>();
        foreach (var file in localFileNames!)
        {
            var fileName = Path.GetFileName(file);
            var path = Path.Combine(pathRoot, fileName);
            var base64 = localFiles![file];
            var bytes = Convert.FromBase64String(base64);
            await File.WriteAllBytesAsync(path, bytes);
            sendFiles.Add(path);
        }
        await client.SendFile(dicPayload!["who"], sendFiles.ToArray());
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
        var bytes = await File.ReadAllBytesAsync(info.AvatorPath);
        var upload = Convert.ToBase64String(bytes);
        OwerInfoExt infoExt = OwerInfoExt.CreateObject(info);
        infoExt.upload = upload;
        response.Data = JsonConvert.SerializeObject(infoExt);
    }

    private class OwerInfoExt : OwerInfo
    {
        public string? upload { get; set; }
        public static OwerInfoExt CreateObject(OwerInfo info)
        {
            return new OwerInfoExt()
            {
                AvatorPath = info.AvatorPath,
                NickName = info.NickName,
                WxId = info.WxId,
            };
        }
    }
}