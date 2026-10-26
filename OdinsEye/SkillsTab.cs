using System;
using System.Collections.Generic;
using UnityEngine;

namespace OdinsEye
{
    // Skills tab: every skill with its level. Pick one to set its level ("raiseskill" by the difference, since the
    // game has no "set" command) or reset it; on the right, Odin's Eye's own learning cheats.
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

        // Only the game's own skills: the console finds skills by their name in Skills.SkillType, so skills
        // added by other mods can't be set with it.
        static void Build(Skills skills)
        {
            if (skills == _builtFor) return;
            _builtFor = skills;
            Shown.Clear();
            foreach (var def in skills.m_skills)
                if (def != null && def.m_skill != Skills.SkillType.None && def.m_skill != Skills.SkillType.All &&
                    Enum.IsDefined(typeof(Skills.SkillType), def.m_skill))
                    Shown.Add(def);
            Shown.Sort((a, b) => string.CompareOrdinal(Name(a.m_skill), Name(b.m_skill)));
            if (_selected == Skills.SkillType.None && Shown.Count > 0) Select(Shown[0].m_skill);
        }

        static string Name(Skills.SkillType type) => Localization.instance.Localize("$skill_" + type.ToString().ToLower());

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
            // Gear and food can raise a skill on top of its level (SEMan.ModifySkillLevel).
            int bonus = Mathf.RoundToInt(skills.GetSkillLevel(type) - level);
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
            Widgets.Command(new Rect(x + w - 100f, y, 100f, Widgets.RowH - 4f), $"Set to {_target}", $"raiseskill {id} {_target - level}");
            y += Widgets.RowH;
            Widgets.CommandRow(x, ref y, w, new[] { "-10", "-1", "+1", "+10" },
                new[] { $"raiseskill {id} -10", $"raiseskill {id} -1", $"raiseskill {id} 1", $"raiseskill {id} 10" });
            Widgets.CommandRow(x, ref y, w, new[] { "Max (100)", "Reset to 0" }, new[] { $"raiseskill {id} 100", $"resetskill {id}" });
            return y;
        }

        // ---------- Right: learning cheats ----------

        static void DrawLearning(float x, float y, float w)
        {
            Widgets.Header(x, ref y, w, "Learning");
            Ui.Label(new Rect(x, y, 90f, 26f), "Learn faster", Ui.Small);
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
            if (!Cheats.Allowed) note = "These two are Odin's Eye's own cheats: they work once cheats are on.";
            else if (!Mathf.Approximately(Game.m_skillGainRate, 1f)) note = $"The world's own skill speed is x{Game.m_skillGainRate:0.##}; this multiplies on top.";
            else note = "Odin's Eye's own cheats: they stop when dev commands are turned off.";
            GUI.Label(new Rect(x, y, w, 64f), $"<color={Ui.Muted}>{note}</color>", Widgets.Wrap);
        }
    }
}
