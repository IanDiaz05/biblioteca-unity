---
name: unity-mcp-task-splitter
description: Safe execution workflow for Unity MCP commands. Automatically decomposes high-level Unity requests into sequential, safe phases (C# scripting vs Scene manipulation) to avoid Unity editor freezing and domain reload lockups. Use whenever performing Unity operations using the OpenCode agent over MCP.
---

# Unity MCP Task Splitter

This skill ensures that high-level requests in Unity (e.g., adding animations, character mechanics, scene setup) are broken down into safe, non-blocking execution phases for the OpenCode agent.

## Core Problem
Modifying C# scripts triggers a **Domain Reload** in Unity. If the agent attempts to execute Scene/Editor MCP calls (like instantiating game objects, adding components, or creating animator controllers) while Unity is compiling code or holding active socket calls, Unity's main thread locks up, causing total editor freezing and dropping the MCP connection.

---

## Execution Workflow

Whenever receiving a high-level prompt or request involving Unity:

### Phase 1: Script & Code Generation Only
1. Identify all required C# script creations or modifications.
2. Generate or update the C# scripts on disk.
3. **STOP HERE for Phase 1.** Do NOT call scene-level MCP tools or attempt to modify objects in the active scene in the same turn.
4. Notify the user that script files have been updated and instruct them (or wait) for Unity to complete its compilation pass.

### Phase 2: Compilation Check & Verification
1. Verify that no C# compilation errors were raised in the Unity console.
2. Confirm that Unity finished Domain Reload and the MCP bridge is fully responsive.

### Phase 3: Scene & MCP Asset Operations
1. Execute all required scene modifications (e.g., GameObject instantiation, Component attachment, Inspector property configuration).
2. Execute asset creation tools (e.g., Animator Controllers, Materials, ScriptableObjects).
3. Connect C# script references to Scene GameObjects or Prefabs.

### Phase 4: Save & Final Verification
1. Save the active scene using the scene management MCP tool.
2. Confirm all changes with the user and provide instructions to test in Play mode.

---

## Rules & Safety Directives
* **Strict Task Separation**: Never combine C# code editing and Live Scene hierarchy manipulation in a single prompt execution step.
* **Error Prevention**: If script errors occur during Phase 1, resolve the code errors BEFORE proceeding to Phase 3.
* **Non-blocking Execution**: Always ensure Unity has finished compiling before executing scene queries.
