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

| Component | Description |
| :--- | :--- |
| `LocalizerBasicText` | Localizes **TextMeshPro** text and fonts |
| `LocalizerFontText` | Font-only localization for text components |
| `LocalizerInputField` | Localizes input field placeholders |
| `LocalizerValueText` | Localizes text with dynamic value substitution |
| `LocalizationDropdown` | Pre-built dropdown for language switching at runtime |


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
