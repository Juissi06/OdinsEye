using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace OdinsEye
{
    // Odin's Eye: one window for Valheim's debug and admin commands. Every button runs the game's own console
    // command, so it follows the game's rules: cheats in single player or a world you host, admin commands for
    // server admins, and the achievement warning before the first cheat.
    [BepInPlugin(Guid, "Odin's Eye", ModInfo.Version)]
    [BepInProcess("valheim.exe")]
    public class Plugin : BaseUnityPlugin
    {
        // Also names the config file: BepInEx/config/OdinsEye.cfg.
        const string Guid = "OdinsEye";

        internal static ConfigEntry<KeyboardShortcut> MenuKey;
        internal static ConfigEntry<float> UiScale;
        internal static ConfigEntry<Tab> LastTab;
        internal static ConfigEntry<string> SpawnFavourites;
        internal static ManualLogSource Log;

        Harmony _harmony;
        GameObject _menuObject;

        void Awake()
        {
            Log = Logger;
            MenuKey = Config.Bind("General", "Menu key", new KeyboardShortcut(KeyCode.Home),
                "Opens and closes the Odin's Eye window.");
            UiScale = Config.Bind("General", "UI scale", 1f,
                new ConfigDescription("Size of the window.", new AcceptableValueRange<float>(0.5f, 2f)));
            LastTab = Config.Bind("General", "Last tab", Tab.Player, "The tab the window opens on.");
            SpawnFavourites = Config.Bind("Spawn", "Favourites", "", "Prefab names ticked as favourites in the Spawn tab, comma-separated.");

            _menuObject = new GameObject("OdinsEye_Menu");
            DontDestroyOnLoad(_menuObject);
            _menuObject.AddComponent<Menu>();
            _menuObject.AddComponent<MapPickHint>();

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo($"Loaded. Menu key: {MenuKey.Value}");
        }

        void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            if (_menuObject != null) Destroy(_menuObject);
        }

        void Update()
        {
            if (Player.m_localPlayer == null)
            {
                if (Menu.IsOpen) Menu.Close();
                return;
            }
            Ui.PrepareArt(); // once: the game's UI sprites are loaded by the time a player exists
            if (!_compatChecked) CheckCompat();
            if (MenuKey.Value.IsDown() && !IsTyping()) Menu.Toggle();
        }

        bool _compatChecked;

        // Other command mods change what the console allows. The window asks the game per command, so it keeps
        // working; just say so in the log so a "why is this button enabled" question has an answer.
        void CheckCompat()
        {
            _compatChecked = true;
            foreach (var info in BepInEx.Bootstrap.Chainloader.PluginInfos.Values)
            {
                string id = (info.Metadata.GUID + " " + info.Metadata.Name).ToLowerInvariant();
                if (id.Contains("devcommands") || id.Contains("cheat") || id.Contains("debugmenu"))
                    Logger.LogInfo($"{info.Metadata.Name} changes console commands; Odin's Eye follows whatever it allows.");
            }
        }

        // Chat and the console have focus; the window itself reports as a text box, so it doesn't count here.
        static bool IsTyping() =>
            (Chat.instance != null && Chat.instance.HasFocus()) || global::Console.IsVisible() || Menu.SearchFocused;
    }
}
