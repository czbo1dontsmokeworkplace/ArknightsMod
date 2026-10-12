using System;
using ArknightsMod.Players;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster.Gitano;

public sealed class GitanoOrbPlayer : ModPlayer
{
    private bool keyConsumed;
    private bool fateWasActive;
    private int lastFateRank;
    private int recoveryTicks;
    private static readonly int[] RecoverySeconds = [15, 15, 15, 14, 14, 14, 13, 12, 11, 10];
    internal bool Exhausted => recoveryTicks > 0;

    internal void TryActivate()
    {
        if (Player.whoAmI != Main.myPlayer || keyConsumed || Player.HeldItem.ModItem is not GitanoOrb
            || Player.dead || Player.CCed || Player.noItems || Exhausted) return;
        keyConsumed = true;
        WeaponPlayer skills = Player.GetModPlayer<WeaponPlayer>();
        if (skills.CurrentSkill?.Key.Item != nameof(GitanoOrb) || skills.Skill is < 0 or > 1
            || skills.SkillActive || skills.StockCount <= 0) return;
        skills.DelStockCount();
        skills.SkillActive = true;
        skills.SkillTimer = 0;
        SoundEngine.PlaySound(SoundID.Item29 with { Volume = .35f, Pitch = .18f }, Player.Center);
    }

    public override void PostUpdate()
    {
        if (Player.dead)
        {
            fateWasActive = false;
            recoveryTicks = 0;
        }
        else
        {
            if (recoveryTicks > 0) recoveryTicks--;
            WeaponPlayer skills = Player.GetModPlayer<WeaponPlayer>();
            bool fateActive = skills.SkillActive && skills.Skill == 1
                && skills.CurrentSkill?.Key.Item == nameof(GitanoOrb);
            if (fateActive)
                lastFateRank = Math.Clamp((skills.CurrentSkill.ForceReplaceLevel ?? skills.CurrentSkill.Level) - 1, 0, 9);
            if (fateWasActive && !fateActive)
                recoveryTicks = RecoverySeconds[lastFateRank] * 60;
            fateWasActive = fateActive;
        }
        if (!ArknightsKeybinds.SkillActivatePressed(Player)) keyConsumed = false;
        else TryActivate();
    }
}
