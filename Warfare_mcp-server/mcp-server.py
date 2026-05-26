import asyncio
import threading
import logging
from aiohttp import web

import state

import tools

logging.basicConfig(filename='unity_mcp.log', level=logging.INFO, format='%(asctime)s - %(message)s', force=True)
logging.getLogger("aiohttp.access").setLevel(logging.ERROR)
logging.getLogger("aiohttp.server").setLevel(logging.ERROR)

@state.sio.event
async def connect(sid, environ):
    state.unity_sid = sid
    logging.info("Unity connected with SID: %s", sid)

@state.sio.event
async def disconnect(sid):
    if state.unity_sid == sid:
        state.unity_sid = None
        logging.info("Unity disconnected with SID: %s", sid)

def start_socketio_server():
    """Runs the Socket.IO server in a background thread."""
    state.server_loop = asyncio.new_event_loop()
    asyncio.set_event_loop(state.server_loop)
    
    runner = web.AppRunner(state.app, access_log=None)
    state.server_loop.run_until_complete(runner.setup())
    site = web.TCPSite(runner, 'localhost', 5000)
    state.server_loop.run_until_complete(site.start())
    state.server_loop.run_forever()

if __name__ == "__main__":
    # Start the Socket.IO server on a background thread
    threading.Thread(target=start_socketio_server, daemon=True).start()
    
    # Start the main FastMCP loop
    state.mcp.run()