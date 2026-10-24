// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class BattlefieldView
{
    internal int CrownedFigures => _figures.Values.Count(f => f.BossDecorated && f.Visible);
    internal float FigureScale(int id) => _figures.TryGetValue(id, out var figure) ? figure.Scale.X : 0;
    internal int BossFigureCount { get; private set; }
    internal int BossDefeatBursts { get; private set; }
    internal string BossHealthText { get; private set; } = "";
    internal Rect2 BossHealthArea { get; private set; }
    internal bool BossHealthFits { get; private set; }
    private void ClearBossPresentation() { BossFigureCount = BossDefeatBursts = 0; BossHealthText = ""; BossHealthArea = default; BossHealthFits = true; }
    private void DrawBossPresentation(UnitState[] units, Dictionary<int, Vector2> positions, Rect2 visible,
        MatchSnapshot state, double tick, Func<System.Numerics.Vector2, Vector2> project)
    {
        var bosses = units.Where(u => u.IsBoss && u.Faction == Faction.Skeletons).ToArray();
        BossFigureCount = bosses.Length; BossDefeatBursts = 0; BossHealthText = ""; BossHealthFits = true;
        int city = _session.ViewedCity.Id;
        var local = bosses.Where(u => u.Destination == city).ToArray();
        var deathEvents = _motion.Events.Where(e => e.Type == CombatEventType.Death && e.Unit is { IsBoss: true, Faction: Faction.Skeletons }
            && tick >= e.Tick && tick - e.Tick < 60).ToArray();
        var localDeath = deathEvents.LastOrDefault(e => e.Unit!.Destination == city);
        if (local.Length > 0 || localDeath is not null)
        {
            double maximum = local.Sum(u => (double)u.Profile.Health), health = local.Sum(u => (double)Math.Max(0, u.Health));
            float fraction = maximum > 0 ? (float)Math.Clamp(health / maximum, 0, 1) : 0;
            float scale = GetGlobalTransformWithCanvas().Scale.Y;
            float width = Math.Min(440 / scale, visible.Size.X - 20 / scale);
            var rect = new Rect2(new(visible.GetCenter().X - width / 2, visible.Position.Y + 12 / scale), new(width, 42 / scale));
            BossHealthArea = rect;
            bool defeated = local.All(u => u.Health <= 0);
            BossHealthText = defeated ? "BOSS DEFEATED" : $"BOSS{(local.Length > 1 ? " ×" + local.Length : "")} → P{city} · {HealthPoints.Format((int)health)}/{HealthPoints.Format((int)maximum)} HP";
            int fontSize = Math.Max(1, (int)Math.Round(16 / scale));
            BossHealthFits = visible.Encloses(rect) && ThemeDB.FallbackFont.GetStringSize(BossHealthText, fontSize: fontSize).X <= width - 8 / scale;
            _combatOverlay!.Bar(rect, new Color("182326ee"));
            _combatOverlay.Bar(new(rect.Position + new Vector2(4 / scale, 33 / scale), new((width - 8 / scale) * fraction, 5 / scale)), new Color("efcb7f"));
            _combatOverlay.Text(BossHealthText, rect.Position + new Vector2(width / 2, 22 / scale), fontSize, new Color("ffe2a3"));
        }
        foreach (var unit in bosses.Where(u => u.Health > 0 && u.PendingImpact && tick >= u.ActionStartTick && tick < u.ImpactTick))
        {
            float progress = (float)((tick - unit.ActionStartTick) / Math.Max(1, unit.ImpactTick - unit.ActionStartTick));
            Vector2 at = positions[unit.Id];
            DrawSetTransform(at, 0, new(1, .45f));
            DrawArc(Vector2.Zero, 18 + 5 * progress, -.5f * Mathf.Pi, -.5f * Mathf.Pi + Mathf.Tau * progress, 36, new Color("f2b673") with { A = .8f }, 2.3f, true);
            DrawSetTransform(Vector2.Zero);
        }
        foreach (var entry in deathEvents)
        {
            Vector2 at = project(CombatVisuals.Position(entry.Unit!, state, entry.Tick));
            if (!visible.Grow(80).HasPoint(at)) continue;
            BossDefeatBursts++;
            float progress = (float)((tick - entry.Tick) / 60), alpha = 1 - progress;
            DrawSetTransform(at, 0, new(1, .55f));
            for (int n = 0; n < 8; n++)
            {
                float angle = n * Mathf.Tau / 8;
                DrawArc(Vector2.Zero, 14 + progress * 32, angle, angle + .42f, 5, new Color("efcb7f") with { A = alpha }, 2.2f, true);
            }
            DrawSetTransform(Vector2.Zero);
            TextCentered("BOSS DEFEATED", at + new Vector2(0, -80 - 10 * progress), 12, new Color("ffe4ab") with { A = alpha });
        }
    }
}
