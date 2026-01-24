namespace Majinfwork.SaveSystem.Samples {
    //// ============================================
    //// EXAMPLE 1: Custom Save Data Classes
    //// ============================================

    ///// <summary>
    ///// Example: Player profile save data.
    ///// </summary>
    //[Serializable]
    //public class PlayerProfileSaveData : SaveData {
    //    public const string SaveFileName = "PlayerProfile";
    //    public override string FileName => SaveFileName;

    //    public string playerName;
    //    public int level;
    //    public int experience;
    //    public float totalPlayTime;

    //    public override async Task CreateAsync(ISaveDataService saveService, CancellationToken cancellationToken = default) {
    //        // Initialize with defaults
    //        playerName = "New Player";
    //        level = 1;
    //        experience = 0;
    //        totalPlayTime = 0;

    //        await base.CreateAsync(saveService, cancellationToken);
    //    }
    //}

    ///// <summary>
    ///// Example: Game settings save data.
    ///// </summary>
    //[Serializable]
    //public class GameSettingsSaveData : SaveData {
    //    public const string SaveFileName = "GameSettings";
    //    public override string FileName => SaveFileName;

    //    public float musicVolume = 0.8f;
    //    public float sfxVolume = 1.0f;
    //    public int qualityLevel = 2;
    //    public bool fullscreen = true;
    //    public string language = "en";
    //}

    ///// <summary>
    ///// Example: Inventory save data using generic SaveData.
    ///// </summary>
    //[Serializable]
    //public class InventoryItem {
    //    public string itemId;
    //    public int quantity;
    //    public int slotIndex;
    //}

    //[Serializable]
    //public class InventorySaveData : SaveData<List<InventoryItem>> {
    //    public const string SaveFileName = "Inventory";
    //    public override string FileName => SaveFileName;

    //    public int maxSlots = 20;
    //    public int gold = 0;

    //    public override async Task CreateAsync(ISaveDataService saveService, CancellationToken cancellationToken = default) {
    //        data = new List<InventoryItem>();
    //        gold = 100; // Starting gold
    //        await base.CreateAsync(saveService, cancellationToken);
    //    }
    //}

    //// ============================================
    //// EXAMPLE 2: Custom Save Slot
    //// ============================================

    ///// <summary>
    ///// Example: Extended save slot with game-specific info.
    ///// </summary>
    //[Serializable]
    //public class GameSaveSlot : SaveSlot {
    //    public string characterClass;
    //    public int currentChapter;
    //    public string lastLocation;

    //    public GameSaveSlot() : base() { }
    //    public GameSaveSlot(int index) : base(index) { }

    //    public override void Reset() {
    //        base.Reset();
    //        characterClass = null;
    //        currentChapter = 0;
    //        lastLocation = null;
    //    }
    //}

    ///// <summary>
    ///// Example: Custom container using GameSaveSlot.
    ///// </summary>
    //[Serializable]
    //public class GameSaveContainer : SaveContainer {
    //    public GameSaveContainer() : base() { }
    //    public GameSaveContainer(int slotCount) : base(slotCount) { }

    //    protected override SaveSlot CreateSlot(int index) {
    //        return new GameSaveSlot(index);
    //    }
    //}

    //// ============================================
    //// EXAMPLE 3: Custom Save Service
    //// ============================================

    ///// <summary>
    ///// Example: Extended save service with game-specific features.
    ///// </summary>
    //public class GameSaveService : SaveDataService {
    //    public GameSaveService(string baseDirectory = null, int slotCount = 3, ISaveSerializer serializer = null)
    //        : base(baseDirectory, slotCount, serializer) { }

    //    protected override SaveContainer CreateContainer(int slotCount) {
    //        return new GameSaveContainer(slotCount);
    //    }

    //    protected override void OnNewSaveCreated(SaveSlot slot) {
    //        base.OnNewSaveCreated(slot);

    //        // Set custom slot data
    //        if (slot is GameSaveSlot gameSlot) {
    //            gameSlot.characterClass = "Warrior";
    //            gameSlot.currentChapter = 1;
    //            gameSlot.lastLocation = "Starting Village";
    //        }
    //    }

    //    /// <summary>
    //    /// Updates the current slot's game progress.
    //    /// </summary>
    //    public async Task UpdateProgressAsync(int chapter, string location, CancellationToken cancellationToken = default) {
    //        if (CurrentSlot is GameSaveSlot gameSlot) {
    //            gameSlot.currentChapter = chapter;
    //            gameSlot.lastLocation = location;
    //            await SaveContainerAsync(cancellationToken);
    //        }
    //    }
    //}

    //// ============================================
    //// EXAMPLE 4: Save Listener Implementation
    //// ============================================

    ///// <summary>
    ///// Example: Service that manages player data and implements ISaveListener.
    ///// </summary>
    //public class PlayerDataManager : ISaveListener {
    //    private ISaveDataService saveService;
    //    private PlayerProfileSaveData profileData;
    //    private InventorySaveData inventoryData;

    //    public string PlayerName => profileData?.playerName ?? "Unknown";
    //    public int Level => profileData?.level ?? 1;
    //    public int Gold => inventoryData?.gold ?? 0;
    //    public IReadOnlyList<InventoryItem> Items => inventoryData?.data;

    //    public PlayerDataManager(ISaveDataService saveService) {
    //        this.saveService = saveService;
    //    }

    //    public async Task LoadAsync(CancellationToken cancellationToken = default) {
    //        profileData = await saveService.LoadAsync<PlayerProfileSaveData>(
    //            PlayerProfileSaveData.SaveFileName, cancellationToken);

    //        inventoryData = await saveService.LoadAsync<InventorySaveData>(
    //            InventorySaveData.SaveFileName, cancellationToken);
    //    }

    //    public void AddExperience(int amount) {
    //        if (profileData == null) return;

    //        profileData.experience += amount;
    //        while (profileData.experience >= GetExpForLevel(profileData.level + 1)) {
    //            profileData.experience -= GetExpForLevel(profileData.level + 1);
    //            profileData.level++;
    //            Debug.Log($"Level up! Now level {profileData.level}");
    //        }
    //        profileData.MarkDirty();
    //    }

    //    public void AddItem(string itemId, int quantity = 1) {
    //        if (inventoryData?.data == null) return;

    //        var existing = inventoryData.data.Find(i => i.itemId == itemId);
    //        if (existing != null) {
    //            existing.quantity += quantity;
    //        }
    //        else {
    //            inventoryData.data.Add(new InventoryItem {
    //                itemId = itemId,
    //                quantity = quantity,
    //                slotIndex = inventoryData.data.Count
    //            });
    //        }
    //        inventoryData.MarkDirty();
    //    }

    //    public void AddGold(int amount) {
    //        if (inventoryData == null) return;
    //        inventoryData.gold += amount;
    //        inventoryData.MarkDirty();
    //    }

    //    // ISaveListener implementation
    //    public async Task UpdateSaveAsync(CancellationToken cancellationToken = default) {
    //        // Only save if data has changed
    //        if (profileData?.isDirty == true) {
    //            await profileData.UpdateAsync(saveService, cancellationToken);
    //        }

    //        if (inventoryData?.isDirty == true) {
    //            await inventoryData.UpdateAsync(saveService, cancellationToken);
    //        }
    //    }

    //    private int GetExpForLevel(int level) => level * 100;
    //}

    //// ============================================
    //// EXAMPLE 5: MonoBehaviour Integration
    //// ============================================

    ///// <summary>
    ///// Example: Bootstrap component that initializes the save system.
    ///// </summary>
    //public class SaveSystemBootstrap : MonoBehaviour {
    //    [Header("Configuration")]
    //    [SerializeField] private int saveSlotCount = 3;
    //    [SerializeField] private bool useJsonSerialization = false;
    //    [SerializeField] private bool prettyPrintJson = true;

    //    private GameSaveService saveService;
    //    private PlayerDataManager playerData;

    //    public ISaveDataService SaveService => saveService;
    //    public PlayerDataManager PlayerData => playerData;

    //    private async void Start() {
    //        // Create serializer based on settings
    //        ISaveSerializer serializer = useJsonSerialization
    //            ? new JsonSaveSerializer(prettyPrintJson)
    //            : new BinarySaveSerializer();

    //        // Create and initialize save service
    //        saveService = new GameSaveService(slotCount: saveSlotCount, serializer: serializer);
    //        await saveService.InitializeAsync();

    //        // Create player data manager and register as listener
    //        playerData = new PlayerDataManager(saveService);
    //        saveService.AddListener(playerData);

    //        // Register with ServiceLocator if available
    //        ServiceLocator.Register<ISaveDataService>(saveService);
    //        ServiceLocator.Register<PlayerDataManager>(playerData);

    //        Debug.Log("[SaveSystemBootstrap] Save system initialized");
    //    }

    //    private void OnDestroy() {
    //        ServiceLocator.Unregister<ISaveDataService>(out _);
    //        ServiceLocator.Unregister<PlayerDataManager>(out _);
    //        saveService?.Shutdown();
    //    }

    //    private void OnApplicationQuit() {
    //        // Auto-save on quit
    //        _ = saveService?.UpdateAllAsync();
    //    }
    //}

    ///// <summary>
    ///// Example: UI controller for save slot selection.
    ///// </summary>
    //public class SaveSlotUI : MonoBehaviour {
    //    [SerializeField] private GameObject saveIndicator;

    //    private ISaveDataService saveService;
    //    private PlayerDataManager playerData;

    //    private void Start() {
    //        saveService = ServiceLocator.Resolve<ISaveDataService>();
    //        playerData = ServiceLocator.Resolve<PlayerDataManager>();
    //    }

    //    // Call from UI button - Start New Game
    //    public async void OnNewGameClicked(int slotIndex) {
    //        saveService.SetCurrentSlot(slotIndex);
    //        await saveService.CreateNewSaveAsync();
    //        await playerData.LoadAsync();

    //        Debug.Log($"New game started in slot {slotIndex}");
    //        // Load game scene
    //    }

    //    // Call from UI button - Continue Game
    //    public async void OnContinueClicked(int slotIndex) {
    //        var slot = saveService.Container.GetSlot(slotIndex);
    //        if (slot == null || !slot.isValid) {
    //            Debug.LogWarning("No save in this slot!");
    //            return;
    //        }

    //        saveService.SetCurrentSlot(slotIndex);

    //        // Validate save before loading
    //        bool isValid = await saveService.ValidateSlotAsync();
    //        if (!isValid) {
    //            Debug.LogError("Save data is corrupted!");
    //            return;
    //        }

    //        await playerData.LoadAsync();

    //        Debug.Log($"Loaded save from slot {slotIndex}");
    //        // Load game scene
    //    }

    //    // Call from UI button - Delete Save
    //    public async void OnDeleteClicked(int slotIndex) {
    //        await saveService.DeleteSlotAsync(slotIndex);
    //        Debug.Log($"Deleted save slot {slotIndex}");
    //        // Refresh UI
    //    }

    //    // Call from UI button - Manual Save
    //    // Shows how to handle UI with async/await (no events needed)
    //    public async void OnSaveClicked() {
    //        if (saveIndicator != null) saveIndicator.SetActive(true);

    //        bool success = await saveService.UpdateAllAsync();

    //        if (saveIndicator != null) saveIndicator.SetActive(false);
    //        Debug.Log(success ? "Save complete!" : "Save failed!");
    //    }

    //    // Display slot info
    //    public void RefreshSlotDisplay() {
    //        for (int i = 0; i < saveService.Container.SlotCount; i++) {
    //            var slot = saveService.Container.GetSlot(i);

    //            if (slot.isValid) {
    //                Debug.Log($"Slot {i}: {slot.displayName}");
    //                Debug.Log($"  Last Save: {slot.FormattedLastSaveTime}");
    //                Debug.Log($"  Play Time: {slot.FormattedPlayTime}");

    //                if (slot is GameSaveSlot gameSlot) {
    //                    Debug.Log($"  Class: {gameSlot.characterClass}");
    //                    Debug.Log($"  Chapter: {gameSlot.currentChapter}");
    //                    Debug.Log($"  Location: {gameSlot.lastLocation}");
    //                }
    //            }
    //            else {
    //                Debug.Log($"Slot {i}: Empty");
    //            }
    //        }
    //    }
    //}

    //// ============================================
    //// EXAMPLE 6: Auto-Save System
    //// ============================================

    ///// <summary>
    ///// Example: Component that auto-saves periodically.
    ///// </summary>
    //public class AutoSaveManager : MonoBehaviour {
    //    [Header("Settings")]
    //    [SerializeField] private float autoSaveInterval = 300f; // 5 minutes
    //    [SerializeField] private bool saveOnSceneChange = true;
    //    [SerializeField] private bool enabled = true;

    //    private ISaveDataService saveService;
    //    private float lastSaveTime;
    //    private CancellationTokenSource autoSaveCts;

    //    private void Start() {
    //        saveService = ServiceLocator.Resolve<ISaveDataService>();
    //        lastSaveTime = Time.time;
    //    }

    //    private void Update() {
    //        if (!enabled || saveService?.CurrentSlot == null) return;

    //        if (Time.time - lastSaveTime >= autoSaveInterval) {
    //            _ = AutoSaveAsync();
    //            lastSaveTime = Time.time;
    //        }
    //    }

    //    private async Task AutoSaveAsync() {
    //        autoSaveCts?.Cancel();
    //        autoSaveCts = new CancellationTokenSource();

    //        try {
    //            Debug.Log("[AutoSave] Starting auto-save...");
    //            await saveService.UpdateAllAsync(autoSaveCts.Token);
    //            Debug.Log("[AutoSave] Auto-save complete");
    //        }
    //        catch (OperationCanceledException) {
    //            Debug.Log("[AutoSave] Auto-save cancelled");
    //        }
    //    }

    //    private void OnDestroy() {
    //        autoSaveCts?.Cancel();
    //        autoSaveCts?.Dispose();
    //    }

    //    public void SetEnabled(bool value) {
    //        enabled = value;
    //    }

    //    public void SetInterval(float seconds) {
    //        autoSaveInterval = seconds;
    //    }

    //    public void TriggerSaveNow() {
    //        _ = AutoSaveAsync();
    //        lastSaveTime = Time.time;
    //    }
    //}
}
