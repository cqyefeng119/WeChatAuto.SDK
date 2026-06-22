/********************************************************************/
/*                WeChatAuto.SDK的websocket server                  */
/* author: alex                                                    */
/* date: 2026-02-20                                                */
/******************************************************************/


var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();

var manager = new ConnectionManager();
var handler = new WebSocketHandler(manager);

app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    var ws = await context.WebSockets.AcceptWebSocketAsync();

    await handler.HandleAsync(ws);
});

app.Run();