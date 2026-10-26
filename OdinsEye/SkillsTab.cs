using System;
using System.Collections.Generic;
using UnityEngine;

namespace OdinsEye
{
    // Skills tab: every skill with its level. Pick one to set its level ("raiseskill" by the difference, since the
    // game has no "set" command) or reset it; on the right, Odin's Eye's own learning cheats. Skills added by other
    // mods are listed too: the console can't find them by name, so their buttons are Odin's Eye's own.
    internal static class SkillsTab
    {
        const float ListW = 470f, RowH = 30f, Gap = 14f;
        static readonly float[] Multipliers = { 1f, 2f, 5f, 10f, 25f };

        static readonly List<Skills.SkillDef> Shown = new List<Skills.SkillDef>();
        static readonly Dictionary<Skills.SkillType, Skills.Skill> Levels = new Dictionary<Skills.SkillType, Skills.Skill>();
        static Skills _builtFor;
        static Skills.SkillType _selected = Skills.SkillType.None;
        static int _target;
        static Vector2 _scroll;

        static readonly Func<Skills, Skills.SkillType, Skills.Skill> GetSkill =
            HarmonyLib.AccessTools.MethodDelegate<Func<Skills, Skills.SkillType, Skills.Skill>>(HarmonyLib.AccessTools.Method(typeof(Skills), "GetSkill"));

        internal static void Draw(Rect r)
        {
            var player = Player.m_localPlayer;
            if (player == null) return;
            var skills = player.GetSkills();
            Build(skills);
            Levels.Clear();
            foreach (var s in skills.GetSkillList())
                if (s.m_info != null) Levels[s.m_info.m_skill] = s;

            DrawList(new Rect(r.x, r.y, ListW, r.height), skills);
            var right = new Rect(r.x + ListW + Gap, r.y, r.width - ListW - Gap, r.height);
            float y = DrawSelected(right, skills);
            DrawLearning(right.x, y + 10f, right.width);
        }

        static bool Modded(Skills.SkillType type) => !Enum.IsDefined(typeof(Skills.SkillType), type);

        static void Build(Skills skills)
        {
            if (skills == _builtFor) return;
            _builtFor = skills;
            Shown.Clear();
            foreach (var def in skills.m_skills)
                if (def != null && def.m_skill != Skills.SkillType.None && def.m_skill != Skills.SkillType.All && !Shown.Exists(d => d.m_skill == def.m_skill))
                    Shown.Add(def);
            Shown.Sort((a, b) => string.CompareOrdinal(Name(a.m_skill), Name(b.m_skill)));
            if (_selected == Skills.SkillType.None && Shown.Count > 0) Select(Shown[0].m_skill);
        }

        // Modded skills' enum value has no name, so ToString gives their number: "$skill_<number>", which is the
        // key Jotunn registers their name under.
        static string Name(Skills.SkillType type)
        {
            string name = Localization.instance.Localize("$skill_" + type.ToString().ToLower());
            return name.StartsWith("[") || name.StartsWith("$") ? $"Modded skill {(int)type}" : name;
        }

        static float BaseLevel(Skills.SkillType type) => Levels.TryGetValue(type, out var s) ? s.m_level : 0f;

        static void Select(Skills.SkillType type)
        {
            _selected = type;
            _target = Mathf.FloorToInt(BaseLevel(type));
        }

        // ---------- Left: the list ----------

        static void DrawList(Rect r, Skills skills)
        {
            float y = r.y;
            Ui.Label(new Rect(r.x, y, 120f, Widgets.HeaderH), $"<color={Ui.Gold}>Skills</color>", Ui.Text);
            const float bw = 96f;
            Widgets.Command(new Rect(r.xMax - bw * 3f - 12f, y, bw, 24f), "All +10", "raiseskill all 10");
            Widgets.Command(new Rect(r.xMax - bw * 2f - 6f, y, bw, 24f), "All to 100", "raiseskill all 100");
            Widgets.Command(new Rect(r.xMax - bw, y, bw, 24f), "Reset all", "resetskill all", "Set every skill back to 0?");
            y += Widgets.HeaderH + 4f;

            var area = new Rect(r.x, y, r.width, r.yMax - y);
            Ui.Inset(area);
            var inner = new Rect(area.x + 4f, area.y + 4f, area.width - 8f, area.height - 8f);
            var view = new Rect(0f, 0f, inner.width - 20f, Shown.Count * RowH);
            _scroll = GUI.BeginScrollView(inner, _scroll, view);
            for (int i = 0; i < Shown.Count; i++)
                DrawRow(new Rect(0f, i * RowH, view.width, RowH - 3f), Shown[i], skills);
            GUI.EndScrollView();
        }

        static void DrawRow(Rect r, Skills.SkillDef def, Skills skills)
        {
            var type = def.m_skill;
            if (GUI.Button(r, GUIContent.none)) Select(type);
            if (type == _selected) Ui.Accent(new Rect(r.x + 3f, r.yMax - 4f, r.width - 6f, 2f), Ui.GoldColor);
            Ui.Icon(new Rect(r.x + 5f, r.y + 3f, r.height - 6f, r.height - 6f), def.m_icon);
            Ui.Label(new Rect(r.x + r.height + 6f, r.y, 150f, r.height), Name(type), Ui.Small);

            float level = Mathf.Floor(BaseLevel(type));
            var bar = new Rect(r.x + 190f, r.y + r.height * 0.5f - 6f, r.width - 190f - 86f, 12f);
            Ui.Bar(bar, level / 100f, Ui.GoldColor);
            // Gear and food can raise a skill on top of its level. Asked from SEMan directly: Skills.GetSkillLevel
            // would add every skill you've never used to your character, and the game's skills screen lists them all.
            float modified = level;
            Player.m_localPlayer.GetSEMan().ModifySkillLevel(type, ref modified);
            int bonus = Mathf.RoundToInt(Mathf.Floor(modified) - level);
            string text = $"<b>{level:0}</b>" + (bonus > 0 ? $" <color={Ui.Good}>+{bonus}</color>" : bonus < 0 ? $" <color={Ui.Bad}>{bonus}</color>" : "");
            Ui.Label(new Rect(bar.xMax + 8f, r.y, 78f, r.height), text, Ui.Small);
        }

        // ---------- Right: the picked skill ----------

        static float DrawSelected(Rect r, Skills skills)
        {
            float x = r.x, w = r.width, y = r.y;
            var def = Shown.Find(d => d.m_skill == _selected);
            if (def == null) return y;
            var type = def.m_skill;
            string id = type.ToString();
            int level = Mathf.FloorToInt(BaseLevel(type));

            Ui.Icon(new Rect(x, y, 40f, 40f), def.m_icon);
            Ui.Label(new Rect(x + 48f, y, w - 48f, 24f), $"<b>{Name(type)}</b>", Ui.Text);
            float progress = Levels.TryGetValue(type, out var s) ? s.GetLevelPercentage() : 0f;
            Ui.Label(new Rect(x + 48f, y + 22f, w - 48f, 20f),
                $"<color={Ui.Muted}>Level {level}" + (level < 100 ? $", {progress * 100f:0}% to {level + 1}" : "") + "</color>", Ui.Small);
            y += 48f;

            _target = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(x, y + 8f, w - 110f, 16f), _target, 0f, 100f));
            int target = _target;
            Act(new Rect(x + w - 100f, y, 100f, Widgets.RowH - 4f), $"Set to {target}", $"raiseskill {id} {target - level}", () => SetLevel(skills, type, target));
            y += Widgets.RowH;
            int[] steps = { -10, -1, 1, 10 };
            float sw = (w - 3 * 6f) / 4f;
            for (int i = 0; i < steps.Length; i++)
            {
                int delta = steps[i];
                string label = delta > 0 ? "+" + delta : delta.ToString();
                Act(new Rect(x + i * (sw + 6f), y, sw, Widgets.RowH - 4f), label, $"raiseskill {id} {delta}",
                    () => SetLevel(skills, type, BaseLevel(type) + delta));
            }
            y += Widgets.RowH;
            float hw = (w - 6f) * 0.5f;
            Act(new Rect(x, y, hw, Widgets.RowH - 4f), "Max (100)", $"raiseskill {id} 100", () => SetLevel(skills, type, 100f));
            Act(new Rect(x + hw + 6f, y, hw, Widgets.RowH - 4f), "Reset to 0", $"resetskill {id}", () => skills.ResetSkill(type));
            y += Widgets.RowH;

            // This skill's own learning speed, or "All" to follow the speed for every skill.
            Ui.Label(new Rect(x, y, 60f, 26f), "Learns", Ui.Small);
            bool hasOwn = Cheats.SkillSpeeds.TryGetValue(type, out float own);
            bool was = GUI.enabled;
            GUI.enabled = was && Cheats.Allowed;
            float cx = x + 60f, cw = (w - 60f - Multipliers.Length * 3f) / (Multipliers.Length + 1);
            if (Widgets.Choice(new Rect(cx, y, cw, 26f), "All", !hasOwn, "~Learns at the speed for all skills (below)")) Cheats.SetSkillSpeed(type, null);
            for (int i = 0; i < Multipliers.Length; i++)
            {
                float m = Multipliers[i];
                if (Widgets.Choice(new Rect(cx + (i + 1) * (cw + 3f), y, cw, 26f), $"x{m:0}", hasOwn && Mathf.Approximately(own, m), "~This skill's own learning speed"))
                    Cheats.SetSkillSpeed(type, m);
            }
            GUI.enabled = was;
            y += 30f;
            return y;
        }

        // A console button for the game's own skills; for modded ones the console can't find, the same action done
        // by Odin's Eye (only while cheats are on, like everything else).
        static void Act(Rect r, string label, string line, Action own)
        {
            if (!Modded(_selected))
            {
                Widgets.Command(r, label, line);
                return;
            }
            bool was = GUI.enabled;
            GUI.enabled = was && Cheats.Allowed;
            if (GUI.Button(r, new GUIContent(label, Widgets.OwnCheat)))
            {
                GUIUtility.keyboardControl = 0;
                own();
            }
            GUI.enabled = was;
        }

        static void SetLevel(Skills skills, Skills.SkillType type, float level)
        {
            if (GetSkill == null) return;
            var skill = GetSkill(skills, type);
            skill.m_level = Mathf.Clamp(Mathf.Floor(level), 0f, 100f);
            skill.m_accumulator = 0f;
            Commands.LastCommand = $"{Name(type)} level";
            Commands.LastOutput = $"set to {skill.m_level:0}";
        }

        // ---------- Right: learning cheats ----------

        static void DrawLearning(float x, float y, float w)
        {
            Widgets.Header(x, ref y, w, "Learning");
            Ui.Label(new Rect(x, y, 90f, 26f), "All skills", Ui.Small);
            float bx = x + 92f, bw = (w - 92f - (Multipliers.Length - 1) * 4f) / Multipliers.Length;
            bool was = GUI.enabled;
            GUI.enabled = was && Cheats.Allowed;
            for (int i = 0; i < Multipliers.Length; i++)
            {
                float m = Multipliers[i];
                var cell = new Rect(bx + i * (bw + 4f), y, bw, 26f);
                if (Widgets.Choice(cell, $"x{m:0}", Mathf.Approximately(Plugin.XpMultiplier.Value, m), Widgets.OwnCheat))
                    Plugin.XpMultiplier.Value = m;
            }
            GUI.enabled = was;
            y += 32f;
            bool keep = Widgets.CheatSwitch(new Rect(x, y + 2f, w, 24f), "Keep skills when you die", Plugin.KeepSkills.Value);
            if (keep != Plugin.KeepSkills.Value) Plugin.KeepSkills.Value = keep;
            y += 30f;

            string note;
            if (!Cheats.Allowed) note = "Learning speeds and keeping skills are Odin's Eye's own cheats: they work once cheats are on.";
            else if (!Mathf.Approximately(Game.m_skillGainRate, 1f)) note = $"The world's own skill speed is x{Game.m_skillGainRate:0.##}; this multiplies on top.";
            else note = "Odin's Eye's own cheats: they stop when dev commands are turned off.";
            GUI.Label(new Rect(x, y, w, 48f), $"<color={Ui.Muted}>{note}</color>", Widgets.Wrap);
        }
    }
}
