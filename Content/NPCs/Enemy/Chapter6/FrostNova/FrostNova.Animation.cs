using System;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

public partial class FrostNova
{
    private bool TryCombatCastFrame(int height)
    {
        if (height <= 0 || !FNBirthEnd || !attackInitialized || !CombatHazardsActive
            || AirbornePhase && ReBirthTimer <= FrostNovaCombatRules.PhaseTwoEntrance) return false;
        if (FNOnATK) {
            // Keep all eleven frames of the old ordinary swing, replayed with
            // its repeated projectile beats rather than the obsolete AI timer.
            int local = combatTimer % 78;
            NPC.frame.Y = (5 + Math.Min(10, local / 6)) * height;
            NPC.frameCounter = 0; FNATKFrameInitialized = true;
            return true;
        }
        if (!FNOnSkill) return false;
        int release = CombatPattern switch { 0 => 48, 4 => 40, 6 or 9 or 10 => 90, _ => 36 };
        if (CombatPattern == 8 && combatTimer >= 130) release = 156;
        if (CombatPattern == 9 && AirbornePhase && combatTimer >= 150) release = 210;
        int frame;
        if (combatTimer < release)
            frame = 16 + Math.Min(9, combatTimer * 10 / Math.Max(1, release));
        else if (combatTimer < release + 32)
            frame = 26 + Math.Min(7, (combatTimer - release) / 4);
        else frame = 18 + (combatTimer - release - 32) / 8 % 7;
        NPC.frame.Y = frame * height;
        NPC.frameCounter = 0; FNSkillFrameInitialized = true;
        return true;
    }
}
