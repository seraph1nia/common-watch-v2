// SPDX-License-Identifier: GPL-3.0-only
using Game.Shared;
using Godot;

namespace Game.GodotClient;

internal static class FeedbackAnchors
{
    public static Vector2 Resolve(FeedbackCue cue, GodotSession? session, SettlementHud hud, SettlementView settlement, BattlefieldView battlefield)
    {
        var viewport = hud.Size;
        if (session is { HasState: true } && cue.City == session.ViewedCity.Id)
        {
            if (!hud.BlocksWorld && cue.Combat && cue.Cell > 0 && battlefield.Active && battlefield.FeedbackPoint(cue.Cell) is { } battle)
                return battlefield.GetGlobalTransformWithCanvas() * battle;
            if (!cue.Combat && cue.Slot is >= 0 and < 9 && settlement.Visible && !hud.BlocksWorld)
                return settlement.GetGlobalTransformWithCanvas() * settlement.PlotPosition(cue.Slot);
            if (hud.Details.Visible)
            {
                if (cue.Kind == FeedbackKind.Recruitment && hud.Details.Army.Recruitment.Buttons.TryGetValue("Recruit" + cue.Soldier, out var recruit) && recruit.IsVisibleInTree())
                    return recruit.GetGlobalRect().GetCenter() - new Vector2(0, 24);
                if (cue.Technology != Game.Core.TechnologyId.None && hud.Details.Research.TechnologyButtons.TryGetValue("Tech" + cue.Technology, out var technology) && technology.IsVisibleInTree())
                    return technology.GetGlobalRect().GetCenter();
                if (cue.Unit > 0 && hud.Details.Army.MemberButtons.TryGetValue("Member" + cue.Unit, out var member) && member.IsVisibleInTree()
                    && hud.Details.Army.RosterScroll.GetGlobalRect().HasPoint(member.GetGlobalRect().GetCenter())) return member.GetGlobalRect().GetCenter();
            }
        }
        if (cue.Kind == FeedbackKind.Production) return new(viewport.X - 190, Mathf.Min(hud.LedgerArea.End.Y + 35, hud.PhaseArea.Position.Y - 12));
        return new(viewport.X / 2, hud.BlocksWorld ? 48 : 132);
    }
}
