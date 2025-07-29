# WebRTC Signaling Server for SmartFoxServer (SFS2X)

This project demonstrates a **Unity prefab** (C#) that integrates WebRTC in a mesh topology for multiplayer, combined with a **SmartFoxServer extension** that acts as the signaling server.  

It includes:
- A Unity prefab (`.prefab`) ready to drop into your project.
- The SmartFoxServer extension `.jar` file (`OUT23Extension.jar`).
- The Java source code to edit and recompile the extension if needed (need SFS2X API).
- A sample C# script and screenshot for reference.

---

## How It Works
- The `OUT23Extension.jar` must be deployed as an **extension of a SmartFoxServer Zone**.  
- Users must be in a room in order to use **Room Variables**.  
- The extension exposes an endpoint called **`WebRTC`**, which handles signaling between peers.  
- Once connected, players exchange their peer information through SmartFoxServer and establish direct WebRTC connections.

---

## Requirements
- SmartFoxServer 2X running with the provided extension.  
- A room with joined users in the target Zone.  
- Unity project with SmartFoxServer API initialized before calling the prefab.  

---

## Notes
- The project is functional but has **limited testing across devices**, so some issues may occur in practice due to lack of resources for broader QA.  
- The code currently uses a **STUN server** (and includes an internal lightweight TURN-like implementation on the same server).  
  - You can replace the server address and credentials with your own **TURN server** for production scenarios.  
- To integrate:
  1. Place the `OUT23Extension.jar` in your SmartFoxServer Zone’s `extensions/` folder.
  2. Restart SmartFoxServer.
  3. In Unity, ensure SmartFoxServer is initialized.
  4. Drag the provided prefab into your scene and configure its parameters.

---

⚠️ **Disclaimer**: This repository is meant as a **technical demonstration** of WebRTC signaling with Unity + SmartFoxServer.  
It is not a finished production-ready system but a base project to learn, test, and extend.

