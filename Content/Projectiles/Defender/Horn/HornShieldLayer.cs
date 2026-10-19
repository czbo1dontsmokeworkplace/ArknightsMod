using System;
using ArknightsMod.Content.Items.Weapons.Defender.Horn;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Defender.Horn;

// Keep the mortar in its existing held-projectile layer while the shield travels
// from behind the body to the front of it.
public sealed class HornShieldBackLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.BackAcc);
    protected override void Draw(ref PlayerDrawSet drawInfo) => HornShieldDrawing.Draw(ref drawInfo, false);
}

public sealed class HornShieldFrontLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.HeldItem);
    protected override void Draw(ref PlayerDrawSet drawInfo) => HornShieldDrawing.Draw(ref drawInfo, true);
}

internal static class HornShieldDrawing
{
    internal static void Draw(ref PlayerDrawSet drawInfo, bool front)
    {
        Player player = drawInfo.drawPlayer;
        if (drawInfo.shadow != 0f || !player.active || player.dead || player.invis
            || player.CCed || player.noItems || player.HeldItem.ModItem is not HornGrenadeLauncher)
            return;

        int type = ModContent.ProjectileType<HornGuard>();
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.owner != player.whoAmI || projectile.type != type
                || projectile.ModProjectile is not HornGuard guard)
                continue;
            float raised = guard.ShieldRaise;
            if (front != (raised >= .5f)) return;

            Texture2D shield = ModContent.Request<Texture2D>(
                "ArknightsMod/Content/Items/Weapons/Defender/Horn/HornShield").Value;
            Vector2 position = projectile.Center + drawInfo.Position - player.position - Main.screenPosition;
            float rotation = -player.direction * player.gravDir * .22f
                * MathF.Sin(raised * MathHelper.Pi);
            SpriteEffects effects = player.direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            if (player.gravDir < 0) effects |= SpriteEffects.FlipVertically;
            drawInfo.DrawDataCache.Add(new DrawData(shield, position, null,
                player.GetImmuneAlphaPure(Lighting.GetColor(projectile.Center.ToTileCoordinates()), drawInfo.shadow),
                rotation, shield.Size() * .5f, 1.25f, effects, 0));
            return;
        }
    }
}
