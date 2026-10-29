using System;
using ArknightsMod.Content;
using ArknightsMod.Content.Items.Weapons.BlastCasters;
using ArknightsMod.Content.Projectiles.BlastCasters;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Players;

public sealed class BlastCasterPlayer : ModPlayer
{
    private bool skillHeld;
    private int activeType = -1;
    internal ulong NextScorch;
    internal int Rank => Math.Clamp(Player.GetModPlayer<WeaponPlayer>().CurrentSkill?.ForceReplaceLevel
        ?? Player.GetModPlayer<WeaponPlayer>().CurrentSkill?.Level ?? 1, 1, 10);
    internal int Mode(BlastCasterWeapon weapon)
    {
        WeaponPlayer state = Player.GetModPlayer<WeaponPlayer>();
        return activeType == weapon.Type && state.SkillActive && state.CurrentSkill?.Key.Item == weapon.Name ? state.Skill + 1 : 0;
    }
    internal void HandleInput(BlastCasterWeapon weapon)
    {
        if (Player.whoAmI != Main.myPlayer || !Allowed()) return;
        WeaponPlayer state = Player.GetModPlayer<WeaponPlayer>();
        bool pressed = ArknightsKeybinds.SkillActivatePressed(Player);
        if (pressed && !skillHeld && state.CurrentSkill?.Key.Item == weapon.Name
            && !BlastCasterRules.ChargedShot(weapon.Kind, state.Skill) && !state.SkillActive && state.StockCount > 0)
        {
            state.DelStockCount(); state.SkillTimer = 0; state.SkillActive = true; activeType = weapon.Type;
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = .48f, Pitch = .1f }, Player.Center);
            BlastCasterVisuals.Skill(Player.MountedCenter, weapon.Kind);
        }
        skillHeld = pressed;
        // Relay placement is retired. The implementation below is retained for reference.
    }
    private bool Allowed() => Player.active && !Player.dead && !Player.CCed && !Player.noItems && Main.hasFocus
        && !Main.gameMenu && !Main.playerInventory && !Main.blockInput && !Main.drawingPlayerChat
        && !Player.mouseInterface && Player.talkNPC < 0 && Player.sign < 0 && Player.chest == -1;
    private void PlaceRelay(BlastCasterWeapon weapon)
    {
        Vector2 target = Main.MouseWorld;
        foreach (Projectile relay in Main.ActiveProjectiles)
            if (relay.owner == Player.whoAmI && relay.ModProjectile is AphrissaRelay && Vector2.DistanceSquared(target, relay.Center) < 36 * 36)
            { relay.Kill(); return; }
        Vector2 delta = target - Player.MountedCenter;
        if (!float.IsFinite(delta.X) || !float.IsFinite(delta.Y)) return;
        if (delta.Length() > BlastCasterRules.RelayPlacementRange)
            target = Player.MountedCenter + delta.SafeNormalize(Vector2.UnitX) * BlastCasterRules.RelayPlacementRange;
        if (target.X < 32 || target.Y < 32 || target.X > Main.maxTilesX * 16f - 32 || target.Y > Main.maxTilesY * 16f - 32
            || Vector2.DistanceSquared(target, Player.MountedCenter) < 80 * 80
            || Collision.SolidCollision(target - new Vector2(14), 28, 28)) return;
        foreach (Projectile relay in Main.ActiveProjectiles)
            if (relay.owner == Player.whoAmI && relay.ModProjectile is AphrissaRelay) relay.Kill();
        Projectile.NewProjectile(Player.GetSource_ItemUse(weapon.Item), target, Vector2.Zero,
            ModContent.ProjectileType<AphrissaRelay>(), 0, 0, Player.whoAmI);
        SoundEngine.PlaySound(SoundID.Item93 with { Volume = .35f, Pitch = .3f }, target);
    }
    public override void PostUpdate()
    {
        if (!ArknightsKeybinds.SkillActivatePressed(Player)) skillHeld = false;
        if (Player.HeldItem.ModItem is not BlastCasterWeapon) activeType = -1;
    }
    public override void UpdateDead() { activeType = -1; skillHeld = false; NextScorch = 0; }
}
