import uuid

from websocket_client import WebSocketClient
from models.owner_info import OwerInfo
from models.message_package import MessagePackage


class WeChatClient:
    def __init__(self, from_wechat: str, socket: WebSocketClient) -> None:
        self.from_wechat = from_wechat
        self.socket = socket

    async def get_ower_info(self):
        """
        获取本微信的个人信息，包括头像文件位置，wxid,昵称
        """
        request_package = MessagePackage(
            request_id=uuid.uuid4().hex,
            func_Name="GetOwerInfo",
            from_wechat=self.from_wechat,
            options="",
        )
        result = await self.socket.send_obj(request_package)
        return OwerInfo.model_validate_json(result)
        
