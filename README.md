# 🎮 Majingari Framework

<p align="center">
  <img src="https://img.shields.io/badge/Unity-6.3%2B-black?style=flat-square&logo=unity" alt="Unity 6.3+"/>
  <img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" alt="MIT License"/>
  <img src="https://img.shields.io/badge/Version-1.0.0-blue?style=flat-square" alt="Version"/>
</p>

The Majingari Framework is a modular foundation designed to streamline game initialization, player management. It is built using patterns familiar to Unreal Engine developers, providing a structured approach to Unity development

---

## 🏛️ Architecture Overview (UE Comparison)

The framework adopts Unreal's decoupled architecture, separating data, rules, and physical representations.

| Feature | Unreal Engine Equivalent | Majingari Framework |
| :--- | :--- | :--- |
| **Global Manager** | `GameInstance` | `GameInstance` |
| **Scene Rules** | `GameMode` | `GameModeManager` |
| **Input & Logic** | `PlayerController` | `PlayerController` |
| **Physical Body** | `Pawn` | `Pawn` / `PlayerPawn` |
| **Player Data** | `PlayerState` | `PlayerState` |
| **Global Data** | `GameState` | `GameState` |

---

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

The documentation for the **Majingari Framework** has been updated to reflect the latest network features. This update focuses on the robust integration with **Unity Netcode for GameObjects**, featuring a customisable connection lifecycle, LAN discovery, and a detailed approval system.


###  🌐 Networking System

The network system is built on **Unity Netcode** and managed via the `UNetcodeConnectionHandler`. It handles everything from initial handshake to session discovery.

#### **1. Network Configuration**
Global network parameters are defined in the **NetworkConfig** ScriptableObject:
*   **Protocol Version:** Ensures only compatible clients can connect.
*   **LAN Discovery:** Configurable UDP ports (default: 47777), broadcast intervals, and scan timeouts.
*   **Connection Constraints:** Set maximum payload sizes and connection timeouts.

#### **2. Connection & Approval Lifecycle**
The framework uses a surgical **Approval Check** system to validate incoming connections before they are allowed into the world.

*   **ConnectionPayload:** Clients send a payload containing their GUID, player name, and protocol version.
*   **Validation:** The `ConnectionApprovalValidator` checks for server capacity, password matches, and protocol compatibility.
*   **Custom Data:** You can extend `ConnectionPayload` with your own JSON data (e.g., selected character skins or team IDs).

#### **3. LAN Discovery Service**
The `LANDiscoveryService` allows players to find local games without manual IP entry.
*   **Automatic Scanning:** Searches for active `DiscoveryResponseData` on the local network.
*   **Session Metadata:** Servers can broadcast custom session info like current map name or game mode.
*   **Compatibility Check:** Automatically filters out sessions with mismatched protocol versions.

#### **4. Custom Connection Handling**
To implement your own logic, extend the `UNetcodeConnectionHandler`:
```csharp
public class MyGameHandler : UNetcodeConnectionHandler {
    protected override void OnLocalClientConnected() {
        // Handle logic when you successfully join a server
    }

    protected override void ConfigurePlayerSpawn(ConnectionApprovalRequest request, ConnectionApprovalResponse response, ConnectionPayload payload) {
        // Custom spawn positioning based on payload data
        response.Position = new Vector3(10, 0, 10); 
    }
}
```

####  🛠️ Connection Status Codes
The framework provides detailed feedback for connection failures via the `ConnectionStatus` enum:

| Status | Meaning |
| :--- | :--- |
| `Success` | Connection established. |
| `ServerFull` | The session has reached `maxPlayers`. |
| `ProtocolMismatch` | Client and Server version numbers do not match. |
| `IncorrectPassword` | The session requires a password that was not provided correctly. |


### 💾 Save System

The **Majingari Save System** provides a high-performance, asynchronous, and modular way to handle persistent game data. It is inspired by Unreal Engine’s `SaveGame` architecture but enhanced with a multi-slot management system and flexible serialization.

#### **1. Core Concepts**
The system is built on four primary pillars:
*   **SaveData:** The base class for any data you want to persist. It includes built-in versioning and a "dirty" flag system to optimize save operations.
*   **SaveDataService:** The central engine that handles asynchronous file I/O and manages the lifecycle of your saves.
*   **SaveSlot:** Metadata containers that track information like `totalPlayTime`, `lastSaveTime`, and custom display names for each save file.
*   **ISaveListener:** An interface for systems (like an Inventory or Quest manager) to subscribe to bulk save operations, ensuring all data is synchronized at once.

#### **2. Supported Serializers**
You can choose the serialization format that best fits your project's needs:
| Format | Class | Description |
| :--- | :--- | :--- |
| **Binary** | `BinarySaveSerializer` | Compact, fast, and secure; ideal for production. |
| **JSON** | `JsonSaveSerializer` | Human-readable; perfect for debugging and modding. |
| **Compressed** | `CompressedJsonSaveSerializer` | JSON convenience with GZip compression for smaller file sizes. |

#### **3. Usage Guide**

##### **Defining Save Data**
Create a class that inherits from `SaveData` and define your serializable fields.
```csharp
[Serializable]
public class PlayerProfileData : SaveData {
    public override string FileName => "PlayerProfile";
    public string playerName;
    public int currentLevel;
}
```

##### **Initializing the Service**
Initialize the service via your `GameInstance` or a bootstrap script.
```csharp
// Setup with 3 save slots and JSON serialization
var service = new SaveDataService(slotCount: 3, serializer: new JsonSaveSerializer());
await service.InitializeAsync();

// Select the first slot
service.SetCurrentSlot(0); 
```

##### **Saving and Loading**
All operations are asynchronous to prevent frame stutters during disk I/O.
```csharp
// Saving
var myData = new PlayerProfileData { playerName = "Hero", currentLevel = 10 };
await service.SaveAsync(myData);

// Loading
var loadedData = await service.LoadAsync<PlayerProfileData>("PlayerProfile");
```

> [!TIP]
> Use `service.UpdateAllAsync()` to trigger a save on all registered `ISaveListener` objects. This is perfect for "Checkpoints" or auto-save triggers.


### **🌍 Localization**
Easily localize your UI using the integrated localization components:
*   `LocalizerBasicText`: Localizes **TextMeshPro** text and fonts.
*   `LocalizationDropdown`: A pre-built dropdown for switching between available languages at runtime.


### 📦 Utilities

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
