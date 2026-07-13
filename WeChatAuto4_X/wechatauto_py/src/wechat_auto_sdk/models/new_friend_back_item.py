from pydantic import BaseModel


class NewFriendBackItem(BaseModel):
    """自动加好友返回信息"""

    # 新增加好友昵称
    who: str
    # 新增加好友从哪个关键词过来
    from_keyword: str
