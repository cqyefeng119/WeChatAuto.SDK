using System.Diagnostics;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class WebSocketHandler
{
    private readonly ConnectionManager _manager;
    private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
    private readonly int heatBeatDelay = 5000;  //心跳设置为5秒

    public WebSocketHandler(ConnectionManager manager)
    {
        _manager = manager;
    }

    public async Task HandleAsync(WebSocket ws)
    {
        var connId = _manager.Add(ws);
        var cts = new CancellationTokenSource();

        // 心跳任务
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        _ = Task.Run(() => Heartbeat(ws, connId, cts, stopwatch));

        try
        {
            var buffer = new byte[12288];  //不需要太大，因为接受的都是简单的文本
            //保持长连接
            while (ws.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
            {
                try
                {
                    var sb = new StringBuilder();
                    var result = await ws.ReceiveAsync(buffer, cts.Token);  //阻塞读取.
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    while (!result.EndOfMessage)
                    {
                        result = await ws.ReceiveAsync(buffer, cts.Token);
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }

                    var raw = sb.ToString();

                    var msg = JsonSerializer.Deserialize<WxMessage>(raw);

                    if (msg?.Type == "pong")
                    {
                        stopwatch.Restart();  //重新计时,
                        Console.WriteLine($"[{connId}] - 收到心跳回复");
                        continue;
                    }

                    Console.WriteLine($"[{connId}] {msg?.Data}");
                    try
                    {
                        // echo
                        await SendAsync(ws, new WxMessage
                        {
                            Type = "echo",
                            Data = $"server: - {connId} - {msg?.Data}"
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"给客户端发送消息时出错:{ex.ToString()}");  //这个基本上判断客户端断了
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    //只记录，不做处理
                    Console.WriteLine($"处理用户消息时出错，错误原因:{ex.ToString()}");
                    break;
                }
            }
        }
        finally
        {
            stopwatch.Stop();
            cts.Cancel();
            _manager.Remove(connId);
            if (ws.State == WebSocketState.Open)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
            }
            Console.WriteLine($"客户端 {connId} 退出连接....");
        }
    }
    public async Task SendAsync(WebSocket ws, WxMessage msg)
    {
        var json = JsonSerializer.Serialize(msg);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _sendLock.WaitAsync();
        try
        {
            await ws.SendAsync(
                bytes,
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            _sendLock.Release();
        }
    }
    private async Task Heartbeat(WebSocket ws, string connId, CancellationTokenSource cts, Stopwatch stopwatch)
    {
        while (!cts.Token.IsCancellationRequested && ws.State == WebSocketState.Open)
        {
            if (stopwatch.IsRunning && stopwatch.ElapsedMilliseconds > 3 * heatBeatDelay)
            {
                cts.Cancel();
                ws.Abort();
                break;
            }
            var ping = new WxMessage { Type = "ping" };
            await SendAsync(ws, ping);
            await Task.Delay(heatBeatDelay);
        }
    }
}