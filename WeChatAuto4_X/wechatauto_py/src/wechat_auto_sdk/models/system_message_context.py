from pydantic import BaseModel


class SystemMessageContext(BaseModel):
    # 本次消息的来源，为好友或者群聊名称.
    from_who: str
    # 新消息气泡列表
    new_messages: list[str]
    