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
        internal static ConfigEntry<Vector2> WindowOffset;
        internal static ConfigEntry<bool> AskFirst;
        internal static ConfigEntry<string> HiddenTabs;
        internal static ConfigEntry<KeyboardShortcut> KeyGod, KeyGhost, KeyFly, KeyNoCost, KeyBack;
        internal static ConfigEntry<float> XpMultiplier;
        internal static ConfigEntry<bool> KeepSkills;
        internal static ConfigEntry<float> FunSize, FunJump, FunSpeed;
        internal static ConfigEntry<bool> FunStamina, FunNoWeight, FunNoWear, FunNoFall, FunNightVision, FunOneHit, FunKnockback, FunInstantCraft;
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
            WindowOffset = Config.Bind("General", "Window offset", Vector2.zero, "Where you dragged the window, from the centre of the screen.");
            AskFirst = Config.Bind("General", "Ask before destructive actions", true,
                "Ask \"are you sure\" before killing everything, deleting, dropping your items and so on.");
            HiddenTabs = Config.Bind("General", "Hidden tabs", "",
                "Tabs you don't use, comma-separated, e.g. \"Admin, Inspect\". Names: " + string.Join(", ", System.Enum.GetNames(typeof(Tab))));
            KeyGod = Config.Bind("Hotkeys", "God mode", KeyboardShortcut.Empty, "Turns god mode on or off without opening the window.");
            KeyGhost = Config.Bind("Hotkeys", "Ghost mode", KeyboardShortcut.Empty, "Turns ghost mode on or off without opening the window.");
            KeyFly = Config.Bind("Hotkeys", "Fly", KeyboardShortcut.Empty, "Turns flying on or off without opening the window.");
            KeyNoCost = Config.Bind("Hotkeys", "No build cost", KeyboardShortcut.Empty, "Turns no build cost on or off without opening the window.");
            KeyBack = Config.Bind("Hotkeys", "Back", KeyboardShortcut.Empty, "Teleports back to where you were before your last teleport.");
            XpMultiplier = Config.Bind("Cheats", "Skill speed", 1f,
                new ConfigDescription("Skills level this many times faster. Only while cheats are on.", new AcceptableValueRange<float>(0f, 100f)));
            KeepSkills = Config.Bind("Cheats", "Keep skills on death", false, "Dying doesn't lower your skills. Only while cheats are on.");
            FunSize = Config.Bind("Fun", "Size", 1f, new ConfigDescription("Your size. Other players see your normal size.", new AcceptableValueRange<float>(0.25f, 4f)));
            FunJump = Config.Bind("Fun", "Jump", 1f, new ConfigDescription("Jump strength.", new AcceptableValueRange<float>(1f, 10f)));
            FunSpeed = Config.Bind("Fun", "Speed", 1f, new ConfigDescription("Walking, running, crouching and swimming speed.", new AcceptableValueRange<float>(1f, 5f)));
            FunStamina = Config.Bind("Fun", "Infinite stamina", false, "Nothing uses stamina.");
            FunNoWeight = Config.Bind("Fun", "No weight limit", false, "Never overweight.");
            FunNoWear = Config.Bind("Fun", "No wear", false, "Tools, weapons and armour stay at full durability.");
            FunNoFall = Config.Bind("Fun", "No fall damage", false, "Falls don't hurt.");
            FunNightVision = Config.Bind("Fun", "Night vision", false, "See at night and in caves without a torch.");
            FunOneHit = Config.Bind("Fun", "One-hit kills", false, "Your hits kill any creature.");
            FunKnockback = Config.Bind("Fun", "Super knockback", false, "Your hits send creatures flying.");
            FunInstantCraft = Config.Bind("Fun", "Instant crafting", false, "Crafting and upgrading at stations finish right away.");
            SpawnFavourites = Config.Bind("Spawn", "Favourites", "", "Prefab names ticked as favourites in the Spawn tab, comma-separated.");

            _menuObject = new GameObject("OdinsEye_Menu");
            DontDestroyOnLoad(_menuObject);
            _menuObject.AddComponent<Menu>();
            _menuObject.AddComponent<MapPickHint>();
            _menuObject.AddComponent<Fun>();
            _menuObject.AddComponent<Hotkeys>();

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
        internal static bool IsTyping() =>
            (Chat.instance != null && Chat.instance.HasFocus()) || global::Console.IsVisible() || Menu.SearchFocused;
    }
}
