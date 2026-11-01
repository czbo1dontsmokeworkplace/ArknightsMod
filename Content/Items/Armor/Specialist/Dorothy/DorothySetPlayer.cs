using System;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using ArknightsMod.Content.Items.Armor.Specialist.Mortis;
using ArknightsMod.Content.Projectiles.Specialist.Dorothy;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.Dorothy;

internal class DorothySetPlayer : ArknightsArmorPlayer
{
    public bool DorothyHelmetActive;
    public bool DorothySetActive;

    internal static bool HasSet(Player player) => player.active && !player.dead
        && player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<DorothyHead>()
        && player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<DorothyBody>()
        && player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<DorothyLegs>();

    public override void ResetEffects() => DorothyHelmetActive = DorothySetActive = false;

    public override void PostUpdateEquips() {
        DorothyHelmetActive = Player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<DorothyHead>();
        DorothySetActive = HasSet(Player);
        if (DorothyHelmetActive) Player.maxMinions++;
    }

    public override void PostUpdateMiscEffects() {
        if (DorothyHelmetActive) Player.GetDamage(DamageClass.Summon) *= 1.15f;
        if (DorothySetActive) Player.GetDamage(DamageClass.Summon) += .02f * Math.Max(0, Player.maxMinions);
    }

    internal void TryPlaceMine() {
        if (Player.whoAmI != Main.myPlayer || !HasSet(Player) || Player.CCed || Player.noItems
            || MortisSetPlayer.BlocksActions(Player)) return;
        int capacity = Math.Max(0, Player.maxMinions);
        int mineType = ModContent.ProjectileType<DorothyMine>();
        int count = 0;
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.owner == Player.whoAmI && projectile.type == mineType) count++;
        if (count >= capacity) return;
        Vector2 position = Main.MouseWorld;
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y)) return;
        position.X = Math.Clamp(position.X, 0, Main.maxTilesX * 16);
        position.Y = Math.Clamp(position.Y, 0, Main.maxTilesY * 16);
        Projectile.NewProjectile(Player.GetSource_FromThis(), position, Vector2.Zero,
            mineType, 0, 0, Player.whoAmI, Main.rand.NextBool(5) ? 1f : 0f);
    }

    public override void PostUpdate() {
        if (Player.whoAmI != Main.myPlayer) return;
        int capacity = HasSet(Player) ? Math.Max(0, Player.maxMinions) : 0;
        int count = 0;
        int mineType = ModContent.ProjectileType<DorothyMine>();
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.owner == Player.whoAmI && projectile.type == mineType) count++;
        // If the slot cap drops, retire the oldest mines first.
        while (count > capacity) {
            Projectile oldest = null;
            foreach (Projectile projectile in Main.ActiveProjectiles)
                if (projectile.owner == Player.whoAmI && projectile.type == mineType
                    && (oldest == null || projectile.timeLeft < oldest.timeLeft)) oldest = projectile;
            if (oldest == null) break;
            oldest.Kill();
            count--;
        }
    }
}
