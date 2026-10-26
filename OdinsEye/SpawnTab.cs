using System.Collections.Generic;
using UnityEngine;

namespace OdinsEye
{
    // Spawn tab: every item and creature the game knows, with icons, categories and search. The selected one is
    // spawned with the game's own "spawn" command (into your inventory with its 'p' flag), and premade item sets
    // with "itemset".
    internal static class SpawnTab
    {
        internal enum Category { All, Weapons, Armor, Ammo, Food, Materials, Tools, Trophies, Other, Creatures, ItemSets }

        static readonly Category[] Categories =
        {
            Category.All, Category.Weapons, Category.Armor, Category.Ammo, Category.Food, Category.Materials, Category.Tools,
            Category.Trophies, Category.Other, Category.Creatures, Category.ItemSets
        };

        class Entry
        {
            internal string Prefab, Name, Search;
            internal Sprite Icon;
            internal Category Category;
            internal int MaxQuality = 1, MaxStack = 1;
            internal bool Creature;
        }

        const float CatW = 118f, DetailW = 230f, Cell = 54f, Gap = 10f, SearchH = 28f;
        const string SearchControl = "OdinsEye_SpawnSearch";

        static readonly List<Entry> Entries = new List<Entry>();
        static readonly List<Entry> Shown = new List<Entry>();
        static List<string> _itemSets;
        static ObjectDB _builtFor;
        static Category _category = Category.All;
        static string _search = "", _shownSearch;
        static Category _shownCategory = (Category)(-1);
        static Entry _selected;
        static string _selectedSet;
        static Vector2 _scroll;
        static int _amount = 1, _level = 1;
        static bool _toInventory = true;

        internal static void Draw(Rect r)
        {
            Build();
            DrawCategories(new Rect(r.x, r.y, CatW, r.height));
            var middle = new Rect(r.x + CatW + Gap, r.y, r.width - CatW - DetailW - Gap * 2f, r.height);
            var detail = new Rect(r.xMax - DetailW, r.y, DetailW, r.height);
            if (_category == Category.ItemSets)
            {
                DrawSets(middle);
                DrawSetDetail(detail);
            }
            else
            {
                DrawGrid(middle);
                DrawDetail(detail);
            }
        }

        // ---------- Data ----------

        static void Build()
        {
            var db = ObjectDB.instance;
            if (db == null || db == _builtFor) return;
            _builtFor = db;
            Entries.Clear();
            _selected = null;
            _shownSearch = null;

            foreach (var prefab in db.m_items)
            {
                var drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                if (drop == null) continue;
                var shared = drop.m_itemData.m_shared;
                string name = Localization.instance.Localize(shared.m_name);
                if (string.IsNullOrEmpty(name) || name.StartsWith("$")) continue;
                Entries.Add(new Entry
                {
                    Prefab = prefab.name, Name = name, Search = (name + " " + prefab.name).ToLowerInvariant(),
                    Icon = shared.m_icons != null && shared.m_icons.Length > 0 ? shared.m_icons[0] : null,
                    Category = CategoryOf(shared.m_itemType), MaxQuality = Mathf.Max(1, shared.m_maxQuality),
                    MaxStack = Mathf.Max(1, shared.m_maxStackSize)
                });
            }

            if (ZNetScene.instance != null)
                foreach (var prefab in ZNetScene.instance.m_prefabs)
                {
                    var character = prefab != null ? prefab.GetComponent<Character>() : null;
                    if (character == null || prefab.GetComponent<Player>() != null) continue;
                    string name = Localization.instance.Localize(character.m_name);
                    if (string.IsNullOrEmpty(name) || name.StartsWith("$")) name = prefab.name;
                    Entries.Add(new Entry
                    {
                        Prefab = prefab.name, Name = name, Search = (name + " " + prefab.name).ToLowerInvariant(),
                        Category = Category.Creatures, MaxQuality = 3, Creature = true
                    });
                }

            Entries.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            _itemSets = ItemSets.instance != null ? ItemSets.instance.GetSetNames() : new List<string>();
            _itemSets.Sort();
        }

        static Category CategoryOf(ItemDrop.ItemData.ItemType type)
        {
            switch (type)
            {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.Shield:
                    return Category.Weapons;
                case ItemDrop.ItemData.ItemType.Helmet:
                case ItemDrop.ItemData.ItemType.Chest:
                case ItemDrop.ItemData.ItemType.Legs:
                case ItemDrop.ItemData.ItemType.Hands:
                case ItemDrop.ItemData.ItemType.Shoulder:
                case ItemDrop.ItemData.ItemType.Utility:
                    return Category.Armor;
                case ItemDrop.ItemData.ItemType.Ammo:
                case ItemDrop.ItemData.ItemType.AmmoNonEquipable:
                    return Category.Ammo;
                case ItemDrop.ItemData.ItemType.Consumable:
                    return Category.Food;
                case ItemDrop.ItemData.ItemType.Material:
                    return Category.Materials;
                case ItemDrop.ItemData.ItemType.Tool:
                case ItemDrop.ItemData.ItemType.Torch:
                    return Category.Tools;
                case ItemDrop.ItemData.ItemType.Trophy:
                    return Category.Trophies;
                default:
                    return Category.Other;
            }
        }

        static void Filter()
        {
            if (_shownSearch == _search && _shownCategory == _category) return;
            _shownSearch = _search;
            _shownCategory = _category;
            Shown.Clear();
            string q = _search.Trim().ToLowerInvariant();
            foreach (var e in Entries)
            {
                if (_category != Category.All && e.Category != _category) continue;
                if (_category == Category.All && e.Creature) continue; // creatures have their own list
                if (q.Length > 0 && !e.Search.Contains(q)) continue;
                Shown.Add(e);
            }
            _scroll = Vector2.zero;
        }

        // ---------- Left: categories ----------

        static void DrawCategories(Rect r)
        {
            float y = r.y, h = Mathf.Min(28f, (r.height - (Categories.Length - 1) * 4f) / Categories.Length);
            foreach (var c in Categories)
            {
                string label = c == Category.ItemSets ? "Item sets" : c.ToString();
                if (Widgets.Choice(new Rect(r.x, y, r.width, h), label, c == _category)) _category = c;
                y += h + 4f;
            }
        }

        // ---------- Middle: search and grid ----------

        static void DrawGrid(Rect r)
        {
            GUI.SetNextControlName(SearchControl);
            _search = GUI.TextField(new Rect(r.x, r.y, r.width, SearchH), _search ?? "", 40);
            Menu.SearchFocused = GUI.GetNameOfFocusedControl() == SearchControl;
            if (string.IsNullOrEmpty(_search) && !Menu.SearchFocused)
                Ui.Label(new Rect(r.x + 8f, r.y, r.width, SearchH), $"<color={Ui.Muted}>Search by name...</color>", Ui.Small);
            Filter();

            var area = new Rect(r.x, r.y + SearchH + 6f, r.width, r.height - SearchH - 6f);
            Ui.Inset(area);
            var inner = new Rect(area.x + 4f, area.y + 4f, area.width - 8f, area.height - 8f);
            int cols = Mathf.Max(1, Mathf.FloorToInt((inner.width - 18f) / Cell));
            int rows = (Shown.Count + cols - 1) / cols;
            var view = new Rect(0f, 0f, cols * Cell, rows * Cell);
            _scroll = GUI.BeginScrollView(inner, _scroll, view);
            int first = Mathf.Max(0, Mathf.FloorToInt(_scroll.y / Cell)), last = Mathf.Min(rows - 1, first + Mathf.CeilToInt(inner.height / Cell) + 1);
            for (int row = first; row <= last; row++)
            for (int col = 0; col < cols; col++)
            {
                int i = row * cols + col;
                if (i >= Shown.Count) break;
                DrawCell(new Rect(col * Cell, row * Cell, Cell - 4f, Cell - 4f), Shown[i]);
            }
            GUI.EndScrollView();
            if (Shown.Count == 0)
                Ui.Label(new Rect(inner.x + 8f, inner.y + 4f, inner.width, 24f), $"<color={Ui.Muted}>Nothing matches.</color>", Ui.Small);
        }

        static GUIStyle _cellText, _titleText;
        static GUIStyle CellText => _cellText ?? (_cellText = new GUIStyle(Ui.SmallCenter) { wordWrap = true, fontSize = 10 });
        static GUIStyle TitleText => _titleText ?? (_titleText = new GUIStyle(Ui.Text) { wordWrap = true });

        static void DrawCell(Rect r, Entry e)
        {
            bool selected = e == _selected;
            if (GUI.Button(r, GUIContent.none)) Select(e);
            if (selected) Ui.Accent(new Rect(r.x + 3f, r.yMax - 5f, r.width - 6f, 2f), Ui.GoldColor);
            if (e.Icon != null) Ui.Icon(new Rect(r.x + 7f, r.y + 7f, r.width - 14f, r.height - 14f), e.Icon);
            else
            {
                GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width - 4f, r.height - 4f), e.Name, CellText);
            }
        }

        static void Select(Entry e)
        {
            _selected = e;
            _level = 1;
            _amount = 1;
        }

        // ---------- Right: details ----------

        static void DrawDetail(Rect r)
        {
            Ui.Inset(r);
            float x = r.x + 10f, w = r.width - 20f, y = r.y + 10f;
            if (_selected == null)
            {
                GUI.Label(new Rect(x, y, w, 80f), $"<color={Ui.Muted}>Pick something to spawn. Search by its name or its prefab name.</color>", Widgets.Wrap);
                return;
            }
            var e = _selected;
            if (e.Icon != null) Ui.Icon(new Rect(x, y, 48f, 48f), e.Icon);
            float tx = e.Icon != null ? x + 56f : x;
            GUI.Label(new Rect(tx, y, w - (tx - x), 30f), $"<b>{e.Name}</b>", TitleText);
            Ui.Label(new Rect(tx, y + 28f, w - (tx - x), 20f), $"<color={Ui.Muted}>{e.Prefab}</color>", Ui.Small);
            y += 58f;

            // Amount: the command spawns one object per count, so keep it sane.
            int max = e.Creature ? 20 : Mathf.Clamp(e.MaxStack, 1, 100);
            Ui.Label(new Rect(x, y, w, 20f), "Amount", Ui.Small);
            y += 20f;
            if (GUI.Button(new Rect(x, y, 30f, 26f), "-")) _amount = Mathf.Max(1, _amount - 1);
            Ui.Label(new Rect(x + 34f, y, 50f, 26f), $"<b>{_amount}</b>", Ui.TextCenter);
            if (GUI.Button(new Rect(x + 88f, y, 30f, 26f), "+")) _amount = Mathf.Min(max, _amount + 1);
            if (GUI.Button(new Rect(x + 124f, y, w - 124f, 26f), max > 1 ? $"Max ({max})" : "1")) _amount = max;
            _amount = Mathf.Clamp(_amount, 1, max);
            y += 32f;

            // Quality for items, stars for creatures (the command's level: 1 = no star).
            if (e.MaxQuality > 1)
            {
                Ui.Label(new Rect(x, y, w, 20f), e.Creature ? "Stars" : "Quality", Ui.Small);
                y += 20f;
                int options = e.MaxQuality;
                float bw = (w - (options - 1) * 4f) / options;
                for (int i = 1; i <= options; i++)
                {
                    string label = e.Creature ? (i == 1 ? "None" : i == 2 ? "1 star" : $"{i - 1} stars") : i.ToString();
                    if (Widgets.Choice(new Rect(x + (i - 1) * (bw + 4f), y, bw, 26f), label, _level == i)) _level = i;
                }
                y += 32f;
            }

            if (!e.Creature)
            {
                _toInventory = GUI.Toggle(new Rect(x, y + 2f, w, 24f), _toInventory, "Straight into inventory");
                y += 30f;
            }

            string line = $"spawn {e.Prefab} {_amount} {_level}" + (!e.Creature && _toInventory ? " 0.5 p" : "");
            float by = r.yMax - 10f - 34f;
            Widgets.Command(new Rect(x, by, w, 34f), e.Creature ? $"Spawn {_amount} in front of you" : $"Spawn {_amount}", line);
            GUI.Label(new Rect(x, y, w, by - y - 4f),
                $"<color={Ui.Muted}>Spawns in front of you{(e.Creature ? ". Creatures spawn hostile unless tamed." : ".")}</color>", Widgets.Wrap);
        }

        // ---------- Item sets ----------

        static Vector2 _setScroll;

        static void DrawSets(Rect r)
        {
            Ui.Inset(r);
            const float rowH = 28f;
            var inner = new Rect(r.x + 4f, r.y + 4f, r.width - 8f, r.height - 8f);
            var view = new Rect(0f, 0f, inner.width - 20f, (_itemSets?.Count ?? 0) * rowH);
            _setScroll = GUI.BeginScrollView(inner, _setScroll, view);
            if (_itemSets != null)
                for (int i = 0; i < _itemSets.Count; i++)
                    if (Widgets.Choice(new Rect(0f, i * rowH, view.width, rowH - 3f), _itemSets[i], _itemSets[i] == _selectedSet))
                        _selectedSet = _itemSets[i];
            GUI.EndScrollView();
        }

        static void DrawSetDetail(Rect r)
        {
            Ui.Inset(r);
            float x = r.x + 10f, w = r.width - 20f, y = r.y + 10f;
            GUI.Label(new Rect(x, y, w, 90f),
                $"<color={Ui.Muted}>The game's premade gear sets, for testing a stage of the game: weapons, armour, food and tools of that tier.</color>",
                Widgets.Wrap);
            if (_selectedSet == null) return;
            float by = r.yMax - 10f - 34f;
            Widgets.Command(new Rect(x, by - 40f, w, 34f), "Give, keep my items", $"itemset {_selectedSet} keep");
            Widgets.Command(new Rect(x, by, w, 34f), "Give, drop my items", $"itemset {_selectedSet}",
                "Drop everything you carry on the ground and equip this set?");
        }
    }
}
