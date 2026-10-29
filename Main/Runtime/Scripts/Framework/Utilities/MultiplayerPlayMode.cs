using System;

namespace Majinfwork {
    /// <summary>
    /// Which editor instance this is under Unity's Multiplayer Play Mode. A virtual player is an editor process of its own,
    /// a clone of the project: the main editor's session state never reaches it, and it shares the project's persistent
    /// data path. So the framework boots in it on its own (it only plays when the main editor does), and it saves apart.
    /// In a player build, or without the Multiplayer Play Mode package, this is always the main (and only) player.
    /// </summary>
    public static class MultiplayerPlayMode {
#if UNITY_EDITOR && HAS_MPPM
        private static string playerName;

        /// <summary>This is a virtual player, not the main editor.</summary>
        public static bool IsVirtualPlayer => !Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor;

        /// <summary>The virtual player's name as Unity launched it (Player2, Player3...); null for the main editor.</summary>
        public static string PlayerName => IsVirtualPlayer ? playerName ??= NameFromLaunch() : null;

        // Unity launches each virtual player with its name as the -name argument.
        private static string NameFromLaunch() {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-name");
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : "VirtualPlayer";
        }
#else
        /// <summary>This is a virtual player, not the main editor.</summary>
        public static bool IsVirtualPlayer => false;

        /// <summary>The virtual player's name as Unity launched it (Player2, Player3...); null for the main editor.</summary>
        public static string PlayerName => null;
#endif
    }
}
