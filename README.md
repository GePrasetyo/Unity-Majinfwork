1. Getting Started: The World Setting
The framework is bootstrapped through the GameWorldSettings ScriptableObject. This is the "brain" of your project that initializes the GameInstance and global services.
• Setup: Go to Majingari Framework > Get World Settings in the Unity menu.
• Initialization: This will create a GameWorldSettings asset in Assets/Resources.
• Configuration: You must attach a World Config asset here for the game to start correctly.
• Edit Mode: You can toggle the editMode checkbox to prevent the framework from auto-starting while you are working in the editor.
2. Configuring Your World and Game Modes
The framework uses a modular approach to define how different levels behave using WorldConfig and GameModeManager.
• World Config: This asset contains a mapping of scene names to specific GameModeManagers.
    ◦ Use the mapList to assign a unique GameMode to specific scenes.
    ◦ Set a defaultGameMode for scenes not explicitly listed.
• GameModeManager: This defines the "rules" and "players" for a specific mode.
    ◦ Managers: Assign your GameState and HUDManager prefabs.
    ◦ Player Setup: Assign prefabs for the PlayerController, PlayerState, PlayerPawn, and PlayerInput.
    ◦ Camera: Select a CameraHandler (e.g., CameraHandlerDefault) to manage how the player sees the world.
3. The Player System
The framework uses a decoupled player architecture where the PlayerController manages the relationship between input, state, and the possessed pawn.
• Spawning: The GameModeManager automatically handles spawning players at a PlayerStart location when a scene loads.
• Accessing Players: Use the PlayerAccessor utility class to quickly find player components from any script:
    ◦ PlayerAccessor.GetMainPlayerController(): Returns the primary player.
    ◦ PlayerAccessor.GetMainPlayerPawn<T>(): Returns the currently possessed pawn cast to your specific type.
4. Advanced Features
Cross-Scene References
Standard Unity references cannot link objects between different scenes. This framework provides a CrossSceneReference system to solve this.
1. Add a CrossSceneAnchor component to the target object in the scene.
2. In your script, mark a field with the [CrossSceneReference] attribute.
3. The CrossSceneManager will automatically resolve these links upon scene load using a GUID-based database.
Localization
The framework includes components to simplify UI localization using the Unity Localization package.
• LocalizerBasicText: Automatically updates a TMP_Text component's text and font based on the selected locale.
• LocalizationDropdown: A pre-built UI dropdown to let players switch between available languages at runtime
