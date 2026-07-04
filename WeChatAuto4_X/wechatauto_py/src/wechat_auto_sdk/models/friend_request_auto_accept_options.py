from pydantic import BaseModel


class FriendRequestAutoAcceptOptions(BaseModel):
    """通过后是否删除申请记录"""
    passed_delete: bool = True
    """打招呼关键词过滤,可以设置多个，回调的时候会携带此KeyWord的信息返回给调用者，调用者应该根据关键词做相应的处理."""
    keyword: str
    """好友备注后缀,如果设置后缀，被通过的好友会自动加上此后缀,如:AI.Net_Test"""
    suffix: str
    """微信标签"""
    label: str
