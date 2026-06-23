from pydantic import BaseModel

class RequestData(BaseModel):
    type: str
    data: str
    request_id: str