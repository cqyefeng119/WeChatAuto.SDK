import asyncio

from websocket_client import WebSocketClient


class WeChatClient:
    def __init__(self,from_wechat:str,socket: WebSocketClient) -> None:
        self.from_wechat = from_wechat
        self.socket = socket
    

