from pydantic import BaseModel

from wechat_auto_sdk.wechat_client import WeChatClient

class SystemMessageContext(BaseModel):
    # 本次消息的来源，为好友或者群聊名称.
    from_who: str
    # 新消息气泡列表
    new_messages: list[str]
    # 当前微信客户端,通过Client可以执行发消息等操作
    client: WeChatClient
    