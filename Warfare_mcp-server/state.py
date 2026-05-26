import socketio
from aiohttp import web
from mcp.server.fastmcp import FastMCP

# 1. Initialize the Core MCP Application
mcp = FastMCP("UnityCommander")

# 2. Initialize the Socket.IO Server Engine
sio = socketio.AsyncServer(async_mode='aiohttp', cors_allowed_origins='*')
app = web.Application()
sio.attach(app)

# 3. Memory Pointers for the Live Network State
unity_sid = None
server_loop = None