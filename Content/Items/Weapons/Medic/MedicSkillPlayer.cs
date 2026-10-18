using System;
using ArknightsMod.Content.Projectiles.Medic.Arts;
using ArknightsMod.Content.Items.Weapons.Medic.Shining;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Medic;

public sealed class MedicSkillPlayer : ModPlayer
{
    private bool keyHeld;
    private int activeItem = -1;
    internal int Rank;
    internal bool ActiveFor(MedicWeapon weapon) => activeItem == weapon.Type
        && Player.GetModPlayer<WeaponPlayer>().SkillActive
        && Player.GetModPlayer<WeaponPlayer>().CurrentSkill?.Key.Item == weapon.Name;

    internal void TryActivate(MedicWeapon weapon)
    {
        if (Player.whoAmI != Main.myPlayer || Player.dead || Player.CCed || Player.noItems || keyHeld) return;
        keyHeld = true;
        var state = Player.GetModPlayer<WeaponPlayer>();
        if (state.CurrentSkill?.Key.Item != weapon.Name || state.StockCount <= 0 || state.SkillActive) return;
        state.DelStockCount(); state.SkillActive = true; state.SkillTimer = 0;
        activeItem = weapon.Type;
        Rank = Math.Clamp((state.CurrentSkill.ForceReplaceLevel ?? state.CurrentSkill.Level) - 1, 0, 9);
        if (weapon.ProfileId == 5 && state.Skill == 1)
            Player.GetModPlayer<ShiningStaffPlayer>().GrantShield(.15f, (int)(state.CurrentSkill.CurrentLevelData.ActiveTime * 60 * WeaponPlayer.ActiveDurationMultiplier));
        MedicArtsVfx.Cast(Player.MountedCenter, -Vector2.UnitY, weapon.ProfileId, true);
        MedicArtsAudio.Play(Player.Center, weapon.ProfileId, MedicSoundCue.Skill);
    }
    public override void PostUpdate()
    {
        if (!ArknightsKeybinds.SkillActivatePressed(Player)) keyHeld = false;
    }
    public override void PostUpdateEquips()
    {
        if (Player.HeldItem.ModItem is MedicWeapon weapon && ActiveFor(weapon)
            && weapon.ProfileId == 5 && Player.GetModPlayer<WeaponPlayer>().Skill == 2)
        {
            Player.statDefense += 30;
            Player.endurance += Math.Min(.12f, Math.Max(0, .85f - Player.endurance));
        }
    }
    public override void UpdateDead() { activeItem = -1; keyHeld = false; Rank = 0; }
}
