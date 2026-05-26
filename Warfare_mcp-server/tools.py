import asyncio
import concurrent.futures
from mcp.server.fastmcp import Image
import json

import state

@state.mcp.tool()
def get_game_state() -> str:
    """Gets the live match state: time, points, yellow team status, and DETECTED red team status."""
    if not state.unity_sid:
        return "Error: Unity is not connected. Is the game running?"
    
    try:
        future = asyncio.run_coroutine_threadsafe(
            state.sio.call('get_game_state', {}, to=state.unity_sid, timeout=3.0),
            state.server_loop
        )
        
        return future.result(timeout=4.0)
        
    except concurrent.futures.TimeoutError:
        return "Error: Unity timed out. It didn't respond to the state request."
    except Exception as e:
        return f"Error communicating with Unity: {e}"
    
@state.mcp.tool()
def issue_reposition_command_to_vehicle(x: float, z: float, vehicle_name: str = "all") -> str:
    """
    Issues a command to move a specific vehicle to a target X, Z coordinate on ground level.
    If you want to move the entire team, leave vehicle_name as 'all'.
    """
    if not state.unity_sid:
        return "Error: Unity is not connected. Is the game running?"
    
    # Bundle the arguments into a dictionary payload
    payload_string = json.dumps({
        "x": x,
        "y": 3,
        "z": z,
        "vehicleName": vehicle_name
    })
    
    try:
        # Send the event and the payload to Unity
        future = asyncio.run_coroutine_threadsafe(
            state.sio.call('issue_reposition_command', payload_string, to=state.unity_sid, timeout=3.0),
            state.server_loop
        )
        
        return future.result(timeout=4.0)
        
    except concurrent.futures.TimeoutError:
        return "Error: Unity timed out. It didn't respond to the move command."
    except Exception as e:
        return f"Error communicating with Unity: {e}"
    
@state.mcp.tool()
def issue_eliminate_target_command_to_vehicle(target_name: str, vehicle_name: str = "all") -> str:
    """
    Issues a command to eliminate a specific target vehicle to the vehicle specified with vehicle_name.
    If you want to issue the command to the entire team, leave vehicle_name as 'all'.
    Only detected red vehicles can be targeted.
    """
    if not state.unity_sid:
        return "Error: Unity is not connected. Is the game running?"
    
    payload_string = json.dumps({
        "targetName": target_name,
        "vehicleName": vehicle_name
    })
    
    try:
        # Send the event and the payload to Unity
        future = asyncio.run_coroutine_threadsafe(
            state.sio.call('issue_eliminate_target_command', payload_string, to=state.unity_sid, timeout=3.0),
            state.server_loop
        )
        
        return future.result(timeout=4.0)
        
    except concurrent.futures.TimeoutError:
        return "Error: Unity timed out. It didn't respond to the eliminate command."
    except Exception as e:
        return f"Error communicating with Unity: {e}"
    
@state.mcp.tool()
def show_plain_map() -> Image:
    """Provides the plain map overview image for visual analysis.
        Map boundaries and the bigger obstacles on the map are highlighted with red.
        Use the compass in the image to orient yourself and understand the directions of the map.
        In the sides there are help regarding the coordinates of the map."""
    with open("assets/plain_map.png", "rb") as f:
        # We explicitly wrap the bytes in an Image object so Copilot knows how to render it
        return Image(data=f.read(), format="png")

@state.mcp.tool()
def show_pine_forest_map() -> Image:
    """Provides the pine forest map overview image for visual analysis.
        Map boundaries and the bigger obstacles on the map are highlighted with red.
        Use the compass in the image to orient yourself and understand the directions of the map.
        In the sides there are help regarding the coordinates of the map."""
    with open("assets/pine_forest_map.png", "rb") as f:
        return Image(data=f.read(), format="png")