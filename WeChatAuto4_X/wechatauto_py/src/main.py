import websockets
import json
import time
import random
import asyncio

URI = "ws://localhost:5177/ws"

async def hello():
    async with websockets.connect(URI) as ws:
        response = await ws.recv()
        result = json.loads(response)
        print(f"收到消息： {result}")
        if (result["type"] == "ping"):
            await ws.send(json.dumps({
                "type":"pong"
            }))
        


def main():
    asyncio.run(hello())


if __name__ == "__main__":
    main()
