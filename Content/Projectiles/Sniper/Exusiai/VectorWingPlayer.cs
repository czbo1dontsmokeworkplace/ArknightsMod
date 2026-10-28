using ArknightsMod.Content.Items.Weapons.Sniper.Exusiai;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Exusiai;

// The cosmetic belongs to the player, not the weapon's holdout or any armor set.
// One state machine merges skill, functional accessory and vanity accessory sources.
public sealed class VectorWingPlayer : ModPlayer
{
    public bool EquippedVisual;
    private VectorWingMotion wings;
    public VectorWingPose Pose => wings?.Pose ?? default;

    public override void ResetEffects() => EquippedVisual = false;

    internal bool VisualEnabled()
    {
        if (!Player.active || Player.dead) return false;
        if (EquippedVisual) return true;
        if (Player.HeldItem.ModItem is not ExusiaiVector gun) return false;
        if (Player.whoAmI == Main.myPlayer) return gun.Mode(Player) > 0;
        // The owner synchronizes ai[2]; do not read another client's local skill state.
        int held = ModContent.ProjectileType<ExusiaiVectorHoldout>();
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.owner == Player.whoAmI && projectile.type == held && projectile.ai[2] > 0)
                return true;
        return false;
    }

    public override void PostUpdate()
    {
        if (Main.dedServ) return;
        if (!Player.active || Player.dead) { wings = null; return; }
        bool enabled = VisualEnabled();
        if (!enabled && wings == null) return;
        wings ??= new();
        wings.Update(Player, enabled ? 3 : 0);
        VectorVisualEffects.Wings(Player, 3, wings.Pose);
        if (!enabled && Pose.Presence <= .001f) wings = null;
    }

    public override void UpdateDead() { EquippedVisual = false; wings = null; }
    public override void PlayerDisconnect() { EquippedVisual = false; wings = null; }
}

public sealed class VectorWingLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Wings);

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;
        if (drawInfo.shadow != 0 || player.dead || player.invis) return;
        VectorWingPose pose = player.GetModPlayer<VectorWingPlayer>().Pose;
        // Respect alternate draw positions (e.g. player previews) and the mount's graphic offset.
        Vector2 center = player.MountedCenter + drawInfo.Position - player.position
            + Vector2.UnitY * player.gfxOffY - Main.screenPosition;
        VectorArt.CollectWings(drawInfo.DrawDataCache, center, player.direction, pose);
    }
}
