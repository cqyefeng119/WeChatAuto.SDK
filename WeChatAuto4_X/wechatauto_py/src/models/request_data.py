from pydantic import BaseModel

class RequestData(BaseModel):
    type: str = "command"
    data: str
    request_id: str