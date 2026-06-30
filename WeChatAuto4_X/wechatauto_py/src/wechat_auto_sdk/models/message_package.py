from pydantic import BaseModel

class MessagePackage(BaseModel):
    # 请求id,每次请求唯一一个id
    request_id: str
    # 内部调用函数
    func_Name: str
    # 函数参数
    options: str
    # 通过哪个微信来发送,目的最主要是为了支持多微信
    from_wechat: str