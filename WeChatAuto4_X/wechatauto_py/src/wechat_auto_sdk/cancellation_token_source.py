import asyncio

class CancellationTokenSource:
    """取消源封装"""
    def __init__(self) -> None:
        self._event = asyncio.Event()
    def cancel(self) -> None:
        self._event.set()
    @property
    def token(self) -> asyncio.Event:
        return self._event