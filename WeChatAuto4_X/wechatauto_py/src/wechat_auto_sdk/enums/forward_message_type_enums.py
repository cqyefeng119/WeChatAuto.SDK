from enum import IntEnum


class ForwardMessageTypeEnums(IntEnum):
    # 逐条转发
    ForwardOneByOne = 0
    # 合并转发
    ForwardMerge = 1
