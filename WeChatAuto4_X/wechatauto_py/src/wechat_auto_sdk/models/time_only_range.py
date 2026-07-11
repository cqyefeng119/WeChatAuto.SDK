from pydantic import BaseModel
from datetime import time


class TimeOnlyRange(BaseModel):
    """时间范围，即定义：开始时间与结束时间"""

    # 开始时间
    star_time: time
    # 结束时间
    end_time: time
