from pydantic import BaseModel


class OwerInfo(BaseModel):
    """个人信息模型类"""
    nick_name: str
    wx_id: str
    avator_path: str
    upload: str