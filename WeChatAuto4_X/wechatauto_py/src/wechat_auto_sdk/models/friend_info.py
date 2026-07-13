from pydantic import BaseModel

from wechat_auto_sdk.models.chat_type import ChatType


class FriendInfo(BaseModel):
    # 昵称
    nick_name: str = ""
    # 备注名
    memo_name: str = ""
    # 地区
    area: str = ""
    # 标签
    lable: str = ""
    # 共同群聊
    same_group_number: str = "0个"
    # 签名
    signature: str = ""
    # 来源
    source: str = ""
    # 微信id
    wx_id: str = ""
    # 图片的base64字符串,可以自行转成图片
    avatar_image_base64: str = ""
    # 好友类型，具体参见<see cref="ChatType"/>
    chat_type: ChatType
    # 添加好友时间，>4.x版本才有此属性
    add_datetime: str
    # 微信中使用的名称
    name: str
