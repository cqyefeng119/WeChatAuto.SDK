from pydantic import BaseModel
from wechat_auto_sdk.models.chat_type import ChatType


class HeaderInfo(BaseModel):
    # 标题,也就是好友/群聊名称
    title: str
    # 标题类型
    header_type: ChatType = ChatType.其他
    # 如果HeaderType是ChatType.群聊,则显示群聊人数数量，如果不是群聊，这里的数量恒为1
    chat_number: int = 1

    def can_talk(self) -> bool:
        """是否是聊天类型

        Returns:
            bool: True - 可以聊天类型，False - 不能聊天类型
        """
        return (
            True
            if (
                (self.header_type == ChatType.好友)
                or (self.header_type == ChatType.企业微信)
                or (self.header_type == ChatType.群聊)
            )
            else False
        )
