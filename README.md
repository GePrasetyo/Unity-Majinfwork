# 🎮 Majingari Framework

<p align="center">
  <img src="https://img.shields.io/badge/Unity-6.3%2B-black?style=flat-square&logo=unity" alt="Unity 6.3+"/>
  <img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" alt="MIT License"/>
  <img src="https://img.shields.io/badge/Version-1.0.0-blue?style=flat-square" alt="Version"/>
</p>

The Majingari Framework is a modular foundation designed to streamline game initialization, player management. It is built using patterns familiar to Unreal Engine developers, providing a structured approach to Unity development

---

## 📑 Table of Contents

- [Architecture Overview](#️-architecture-overview-ue-comparison)
- [Getting Started](#-getting-started)
- [Core Systems](#-core-systems)
  - [World Configuration & Game Modes](#1-world-configuration--game-modes)
  - [Game Mode Manager](#2-game-mode-manager)
  - [Player System](#3-player-system)
- [Advanced Features](#️-advanced-features)
  - [Cross-Scene References](#-cross-scene-references)
  - [Level Streaming & Loading](#-level-streaming--loading)
  - [HUD & UI Widget System](#️-hud--ui-widget-system)
  - [Networking System](#--networking-system)
    - [Hosting a Session](#3-hosting-a-session)
    - [Joining a Session](#4-joining-a-session)
    - [LAN Discovery & Matchmaking](#5-lan-discovery--matchmaking)
    - [Custom Connection Handler](#7-custom-connection-handler)
  - [Save System](#-save-system)
  - [Localization (Legacy)](#-localization-legacy)
  - [Utilities](#-utilities)
- [License](#-license)

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

#### **PlayerAccessor API**
Use the **PlayerAccessor** utility for easy access to player components from any script:
```csharp
// Main player shortcuts
var pawn = PlayerAccessor.GetMainPlayerPawn<MyPawn>();
var controller = PlayerAccessor.GetMainPlayerController<MyController>();
var input = PlayerAccessor.GetMainPlayerInput<MyInput>();
var state = PlayerAccessor.GetMainPlayerState<MyState>();

// Safe try-get patterns
if (PlayerAccessor.TryGetPlayerController<MyController>(out var ctrl)) {
    ctrl.DoSomething();
}

// Multi-player support (by index)
var player2Pawn = PlayerAccessor.GetPlayerPawn<MyPawn>(index: 1);
int playerCount = PlayerAccessor.GetPlayerCount();
```

#### **Possession System**
PlayerController can possess/unpossess pawns at runtime:
```csharp
public class MyController : PlayerController {
    protected override void OnPossess(PlayerPawn pawn) {
        // Called when possessing a new pawn
    }

    protected override void OnUnPossess() {
        // Called when releasing current pawn
    }
}

// Switch pawns at runtime
controller.Possess(newPawn);
controller.UnPossess();
```

---

## 🛠️ Advanced Features

### **🔗 Cross-Scene References**
Unity does not natively allow referencing objects between different scenes in the inspector. This framework provides a **CrossSceneReference** system to bypass this limitation.

*   **Setup:** Add a `CrossSceneAnchor` component to the object you want to reference.
*   **Usage:** Mark your field in a script with the `[CrossSceneReference]` attribute.
*   **Resolution:** The **CrossSceneManager** automatically resolves these links via GUID when the scene loads.


### **🎬 Level Streaming & Loading**

The framework provides an async scene loading system with built-in loading screen support.

#### **LevelManager**
The `LevelManager` orchestrates scene transitions and GameMode lifecycle:
```csharp
// Load a new level with loading screen
await GameInstance.Instance.LevelManager.LoadLevelAsync("GameplayScene");

// Check loading state
if (GameInstance.Instance.LevelManager.IsLoading) { /* ... */ }
```

#### **Loading Screen Implementations**
Two loading screen implementations are available:

| Class | UI System | Description |
| :--- | :--- | :--- |
| `LoadingStreamerDefault` | **UI Toolkit** | Creates UI at runtime - no prefabs/UXML required. Uses `RuntimeLoadingPanel`. |
| `LoadingStreamerCanvas` | **Canvas** | Legacy Canvas-based implementation for projects using uGUI. |

Both support:
*   **Async fade in/out** with configurable speed
*   **Task coalescing** - multiple callers can await the same fade operation
*   **Cancellation tokens** - caller cancellation doesn't affect other waiters
*   **ForceCancel()** - immediately cancel all pending operations

#### **Custom Loading Screens**
Extend `LoadingStreamer` to create custom loading screens:
```csharp
public class MyLoadingScreen : LoadingStreamer {
    protected override async Task StartLoadingAsync(CancellationToken token) {
        // Show your loading UI
    }

    protected override async Task StopLoadingAsync(CancellationToken token) {
        // Hide your loading UI
    }
}
```


### **🖼️ HUD & UI Widget System**

The framework provides a complete UI management system with widget caching, navigation stacks, and model-based data binding.

#### **Basic Usage**
```csharp
// Show/Hide widgets
HUD.Show<GameplayHUD>();
HUD.Hide<GameplayHUD>();

// Get reference to active widget
var hud = HUD.Get<GameplayHUD>();

// Async variants (wait for animations)
await HUD.ShowAsync<GameplayHUD>();
await HUD.HideAsync<GameplayHUD>();
```

#### **Model-Based Widgets**
Pass data models to widgets for clean separation of concerns:
```csharp
// Define a model
public struct PlayerHUDModel {
    public float healthPercent;
    public int score;
}

// Show with model
HUD.Show<PlayerHUD, PlayerHUDModel>(new PlayerHUDModel {
    healthPercent = 0.8f,
    score = 1000
});

// Widget implementation
public class PlayerHUD : UIWidget<PlayerHUDModel> {
    protected override void OnSetup(PlayerHUDModel model) {
        healthBar.value = model.healthPercent;
        scoreText.text = model.score.ToString();
    }
}
```

#### **Navigation Stack**
Built-in stack-based navigation for menu flows:
```csharp
// Push screens onto stack
HUD.Push<SettingsMenu>();
HUD.Push<AudioSettings>();

// Go back one screen
HUD.Back();

// Return to root
HUD.PopToRoot();

// Query stack
int depth = HUD.StackCount();
HUD.ClearStack();
```

#### **UIWidget Lifecycle**
| Method | Description |
| :--- | :--- |
| `Show()` / `ShowAsync()` | Display the widget |
| `Hide()` / `HideAsync()` | Hide the widget |
| `Setup(model)` | Pass data to the widget (deferred if inactive) |
| `Refresh()` | Re-apply current model data |


###  🌐 Networking System

The network system is built on **Unity Netcode for GameObjects** with a layered architecture:

```
UNetcodeConnectionHandler (Main Entry Point)
  ├── SessionManager (Session lifecycle)
  ├── LANDiscoveryService (Local network discovery)
  ├── ConnectionApprovalValidator (Join validation)
  └── NetworkManager (Unity Netcode)
```

#### **1. Initialization & Service Access**
After calling `UNetcodeConnectionHandler.Initialize()`, services are auto-registered via ServiceLocator:
```csharp
var networkService = ServiceLocator.Resolve<INetworkService>();
var sessionManager = ServiceLocator.Resolve<ISessionManager>();
var lanDiscovery = ServiceLocator.Resolve<ILANDiscoveryService>();
```

#### **2. Network Configuration**
Create via: **Right-click > Create > MFramework/Config Object/Network Config**

| Field | Default | Description |
| :--- | :--- | :--- |
| `protocolVersion` | 1 | Increment for breaking network changes |
| `discoveryPort` | 47777 | UDP port for LAN discovery |
| `discoveryTimeout` | 5f | Scan duration in seconds |
| `broadcastInterval` | 1f | Broadcast frequency in seconds |
| `sessionStaleTimeout` | 3f | Session timeout threshold |
| `maxConnectPayload` | 256 | Max connection data size in bytes |
| `connectionTimeout` | 10f | Connection attempt timeout |
| `defaultMaxPlayers` | 4 | Default session capacity |

#### **3. Hosting a Session**
```csharp
// Create session settings
var settings = SessionSettings.Create(
    name: "My Game Session",
    maxPlayers: 4,
    password: null,  // null = no password
    mapIndex: 0
);

// Add custom metadata (broadcasted to LAN discovery)
settings.SetCustomData("gameMode", "TeamDeathmatch");
settings.SetCustomData("difficulty", 2);

// Start hosting
networkService.HostSession(settings);

// Check host status
if (networkService.IsHost) {
    var session = networkService.CurrentSession;
    Debug.Log($"Hosting: {session.SessionName} ({session.CurrentPlayerCount}/{session.MaxPlayers})");
}
```

#### **4. Joining a Session**
```csharp
// Option A: Join by IP address
networkService.JoinSession(
    address: "192.168.1.100",
    port: 7777,
    password: null
);

// Option B: Join a discovered session (from LAN scan)
networkService.JoinSession(discoveredSession, password: "secret");

// Monitor connection result
networkService.OnStatusChanged += (status) => {
    switch (status) {
        case ConnectionStatus.Connecting:
            Debug.Log("Connecting...");
            break;
        case ConnectionStatus.Connected:
            Debug.Log("Successfully joined!");
            break;
        case ConnectionStatus.Hosting:
            Debug.Log("Server started!");
            break;
        case ConnectionStatus.Disconnected:
            Debug.Log("Disconnected");
            break;
        default:
            if (status.IsRejection())
                Debug.Log($"Rejected: {status.ToMessage()}");
            break;
    }
};
```

#### **5. LAN Discovery & Matchmaking**
```csharp
var lanDiscovery = ServiceLocator.Resolve<ILANDiscoveryService>();

// Start scanning (runs for discoveryTimeout duration)
lanDiscovery.StartScan();

// Subscribe to discovery events
lanDiscovery.OnSessionDiscovered += (session) => {
    Debug.Log($"Found: {session.SessionName} @ {session.Address}:{session.Port}");
    Debug.Log($"Players: {session.CurrentPlayers}/{session.MaxPlayers}");
    Debug.Log($"Password protected: {session.HasPassword}");

    // Access custom metadata
    var gameMode = session.GetMetadata<string>("gameMode", "Unknown");
};

lanDiscovery.OnSessionUpdated += (session) => {
    // Player count changed, etc.
};

lanDiscovery.OnSessionLost += (session) => {
    // Session no longer responding
};

lanDiscovery.OnScanComplete += () => {
    Debug.Log($"Scan complete. Found {lanDiscovery.DiscoveredSessions.Count} sessions");
};

// Filter joinable sessions
var joinable = lanDiscovery.DiscoveredSessions
    .Where(s => s.IsCompatible(protocolVersion))
    .Where(s => !s.IsFull)
    .Where(s => !s.HasPassword || userHasPassword)
    .ToList();
```

#### **6. Connection Status Codes**
| Status | Meaning |
| :--- | :--- |
| `Disconnected` | Not connected |
| `Connecting` | Connection in progress |
| `Connected` | Successfully joined as client |
| `Hosting` | Running as server/host |
| `ServerFull` | Session has reached `maxPlayers` |
| `IncorrectPassword` | Wrong or missing password |
| `ProtocolMismatch` | Client/server version mismatch |
| `InvalidPlayerName` | Player name empty or too long |
| `Timeout` | Connection attempt timed out |
| `Banned` | Client is banned from session |

**Helper methods:**
```csharp
status.IsConnected();   // true if Connected or Hosting
status.IsRejection();   // true if rejection reason (ServerFull, etc.)
status.ToMessage();     // User-friendly error message
```

#### **7. Custom Connection Handler**
Extend `UNetcodeConnectionHandler` to customize behavior:
```csharp
public class MyGameHandler : UNetcodeConnectionHandler {
    // --- Server-side callbacks ---
    protected override void OnServerStartedCustom() {
        Debug.Log("Server ready for connections");
    }

    protected override void OnClientJoined(ulong clientId) {
        var payload = GetClientPayload(clientId);
        Debug.Log($"Player {payload.playerName} joined");
    }

    protected override void OnClientLeft(ulong clientId) {
        Debug.Log($"Client {clientId} disconnected");
    }

    // --- Client-side callbacks ---
    protected override void OnLocalClientConnected() {
        Debug.Log("Connected to server!");
        // Load game scene, show game UI, etc.
    }

    protected override void OnLocalClientDisconnected() {
        Debug.Log("Disconnected from server");
        // Return to menu, show reconnect dialog, etc.
    }

    // --- Validation callbacks ---
    protected override void OnConnectionApproved(ulong clientId, ConnectionPayload payload) {
        var customData = payload.GetCustomData<MyJoinData>();
        // Setup player based on custom data
    }

    protected override void OnConnectionRejected(ulong clientId, ConnectionStatus reason, ConnectionPayload payload) {
        Debug.LogWarning($"Connection rejected: {reason.ToMessage()}");
    }

    // --- Spawn customization ---
    protected override void ConfigurePlayerSpawn(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response,
        ConnectionPayload payload) {
        response.Position = GetSpawnPoint(payload.playerName);
        response.Rotation = Quaternion.identity;
    }
}
```

#### **8. Connection Payload (Custom Join Data)**
Send custom data when connecting:
```csharp
[Serializable]
public class MyJoinData {
    public int selectedCharacter;
    public string selectedSkin;
    public int teamIndex;
}

public class MyGameHandler : UNetcodeConnectionHandler {
    // Client: Send custom data when joining
    protected override ConnectionPayload CreateClientPayload(string password) {
        var payload = base.CreateClientPayload(password);
        payload.SetCustomData(new MyJoinData {
            selectedCharacter = 2,
            selectedSkin = "warrior_blue",
            teamIndex = 0
        });
        return payload;
    }

    // Server: Read custom data on approval
    protected override void OnConnectionApproved(ulong clientId, ConnectionPayload payload) {
        var joinData = payload.GetCustomData<MyJoinData>();
        if (joinData != null) {
            SpawnCharacter(clientId, joinData.selectedCharacter, joinData.selectedSkin);
            AssignTeam(clientId, joinData.teamIndex);
        }
    }
}
```

#### **9. Custom Session & Validation**
Extend session management for game-specific logic:
```csharp
// Custom session info with game-specific properties
public class MySessionInfo : SessionInfo {
    public string GameMode { get; private set; }

    public MySessionInfo(SessionSettings settings, int protocolVersion)
        : base(settings, protocolVersion) { }

    protected override void OnCreated(SessionSettings settings) {
        GameMode = settings.GetCustomData("gameMode", "Default");
    }
}

// Custom session manager with validation
public class MySessionManager : SessionManager {
    protected override SessionInfo CreateSessionInfo(SessionSettings settings, int protocolVersion) {
        return new MySessionInfo(settings, protocolVersion);
    }

    protected override bool ValidateCustom(ConnectionPayload payload, out ConnectionStatus rejectionReason) {
        // Add custom validation (e.g., check if player is banned)
        var joinData = payload.GetCustomData<MyJoinData>();
        if (joinData?.teamIndex < 0) {
            rejectionReason = ConnectionStatus.GenericFailure;
            return false;
        }
        rejectionReason = ConnectionStatus.Success;
        return true;
    }
}

// Register custom manager
public class MyGameHandler : UNetcodeConnectionHandler {
    protected override SessionManager CreateSessionManager(NetworkConfig config) {
        return new MySessionManager(config);
    }
}
```


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


### **🌍 Localization (Legacy)**

> **⚠️ Deprecated:** These TextMeshPro-based localizers are for legacy Canvas/uGUI projects only.
> For UI Toolkit projects, use the localization features in **Majinfwork UI Toolkit Extension** (`com.majingari.ui`) with Unity Localization package instead.

| Component | Description |
| :--- | :--- |
| `LocalizerBasicText` | Localizes **TextMeshPro** text and fonts |
| `LocalizerFontText` | Font-only localization for text components |
| `LocalizerInputField` | Localizes input field placeholders |
| `LocalizerValueText` | Localizes text with dynamic value substitution |
| `LocalizationDropdown` | Pre-built dropdown for language switching at runtime |

**For new projects using UI Toolkit:**
- Static text: Use UXML `@Table/Key` syntax (most performant)
- Dynamic text: Use `LabelRef`, `ButtonRef`, etc. with `InitializeLocalization()` + `Arguments`


### 📦 Utilities

#### **TickSignal**
A custom ticking system that integrates with Unity's PlayerLoop for decoupled updates:
```csharp
public class MyComponent : MonoBehaviour, ITickObject {
    void OnEnable() => this.RegisterTick();
    void OnDisable() => this.UnregisterTick();

    public void Tick() {
        // Called every frame, independent of MonoBehaviour
    }
}

// Also supports fixed tick
public class PhysicsComponent : MonoBehaviour, IFixedTickObject {
    public void FixedTick() {
        // Called every fixed update
    }
}
```

#### **Object Pooling**
Three pooling implementations for different use cases:

| Class | Use Case | Example |
| :--- | :--- | :--- |
| `PoolByReference` | Prefab pooling with extension methods | `prefab.InstantiatePoolRef()` |
| `PoolByUnityObject<T>` | Manual pool for Unity objects | Custom capacity/callbacks |
| `PoolByObject<T>` | Generic C# object pooling | Non-Unity classes |

**PoolByReference** (most common):
```csharp
// Spawn from pool (uses prefab as key)
prefab.InstantiatePoolRef(position, rotation, out Transform instance);

// Return to pool
instance.ReleasePoolRef(prefab);
```

#### **PhysxExtension**
Non-allocating physics helpers with pre-allocated buffers:
```csharp
// Non-allocating overlap (reuses internal buffer)
List<Collider> results = new();
PhysxExtension.OverlapSphere(ref results, position, radius, layerMask);

// Obstruction check with optional destructible support
bool blocked = PhysxExtension.IsObstructed(from, to, layerMask, maxDistance);
```

---

## 📜 License
This project is licensed under the **MIT License**.
