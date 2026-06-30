from pydantic import BaseModel

class SimpleConversation(BaseModel):
    # 会话标题
    conversation_title: str
    # 是否免打扰
    is_do_not_disturb: bool = False
    # 是否置顶
    is_top: bool = False
    # 未读消息数
    not_read_numbr : int = 0
    

