# 🎮 Majingari Framework

The **Majingari Framework** is a modular Unity framework designed to streamline game initialization, player management, and cross-scene referencing.

## 🚀 Getting Started

To begin using the framework, you must initialize the **World Settings**. This acts as the central hub for your project's configuration.

> [!TIP]
> You can quickly access or create your settings by navigating to **Majingari Framework > Get World Settings** in the Unity menu.

1.  **Create Settings:** Using the menu item above will generate a `GameWorldSettings.asset` file in your `Assets/Resources` folder.
2.  **Assign Game Instance:** Ensure a **Game Instance** (such as the provided `PersistentGameInstance`) is assigned to the settings asset.
3.  **Attach World Config:** Create and attach a `WorldConfig` ScriptableObject to define how your scenes behave.

---

## 🌍 Core Systems

### **1. World Configuration & Game Modes**
The framework uses a "Map-to-Mode" architecture. You define a **WorldConfig** asset, which contains:
*   **Map List:** A dictionary-style mapping of scene names to specific **GameModeManagers**.
*   **Default Game Mode:** A fallback mode used for any scene not explicitly listed.

### **2. Game Mode Manager**
The **GameModeManager** is a ScriptableObject that defines the "rules" for a specific scene. It handles the spawning of:
*   **Managers:** Automatically instantiates your `GameState` and `HUDManager` prefabs.
*   **Player Components:** Defines prefabs for the **PlayerController**, **PlayerState**, **PlayerPawn**, and **PlayerInput**.
*   **Camera Logic:** Assigns a **CameraHandler** to manage the player's view.

### **3. Player System**
The framework decouples player logic into four distinct components for maximum flexibility:
*   **PlayerController:** The brain that possesses a pawn and links input to state.
*   **PlayerPawn:** The physical representation of the player in the world.
*   **PlayerInput:** Handles the Unity Input System actions.
*   **PlayerState:** Stores persistent data for that specific player.

> [!NOTE]
> Use the **PlayerAccessor** utility for easy access to player components from any script:
> ```csharp
> // Example: Getting the main player's pawn
> var myPawn = PlayerAccessor.GetMainPlayerPawn<MyPawnType>();
> ```

---

## 🛠️ Advanced Features

### **🔗 Cross-Scene References**
Unity does not natively allow referencing objects between different scenes in the inspector. This framework provides a **CrossSceneReference** system to bypass this limitation.

*   **Setup:** Add a `CrossSceneAnchor` component to the object you want to reference.
*   **Usage:** Mark your field in a script with the `[CrossSceneReference]` attribute.
*   **Resolution:** The **CrossSceneManager** automatically resolves these links via GUID when the scene loads.

### **🌐 Networking (LAN Support)**
The framework includes built-in support for **Netcode for GameObjects** with LAN discovery.
*   **ConnectionLANSupport:** Allows clients to broadcast and find local game sessions without entering IP addresses manually.
*   **UNetcodeConnectionHandler:** Manages server/host start-up, client approval checks, and connection payloads.

### **🌍 Localization**
Easily localize your UI using the integrated localization components:
*   `LocalizerBasicText`: Localizes **TextMeshPro** text and fonts.
*   `LocalizationDropdown`: A pre-built dropdown for switching between available languages at runtime.

---

## 📦 Utilities

| Feature | Description |
| :--- | :--- |
| **TickSignal** | A custom ticking system that integrates with the Unity Player Loop for high-performance updates. |
| **PoolByReference** | An efficient object pooling system that uses reference-based keys for instantiation. |
| **PhysxExtension** | Non-allocating physics helpers like `OverlapSphere` and `IsObstructed` checks. |

---

## 📜 License
This project is licensed under the **MIT License**.

---

**Analogy for Understanding:**
Think of the **Majingari Framework** as a **Smart Home Hub**. The `GameWorldSettings` is the hub itself that connects everything. The `WorldConfig` is the **profile** (e.g., "Movie Night" or "Cleaning Mode") that decides which devices (**GameModes**) turn on in which rooms (**Scenes**). The `PlayerController` is the **remote control** that can operate different appliances (**Pawns**) while keeping the same batteries (**PlayerState**).
