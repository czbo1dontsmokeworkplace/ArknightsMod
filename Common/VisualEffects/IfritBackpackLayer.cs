using ArknightsMod.Content.Items.Weapons.BlastCasters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Common.VisualEffects;

public sealed class IfritBackpackLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Backpacks);

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;
        if (drawInfo.shadow != 0f || player.dead || player.invis
            || player.HeldItem.ModItem is not IfritFlamethrower)
            return;

        Texture2D texture = ModContent.Request<Texture2D>(
            "ArknightsMod/Content/Items/Weapons/BlastCasters/IfritBackpack").Value;
        Vector2 center = drawInfo.Position + new Vector2(player.width * .5f, player.height * .5f)
            + new Vector2(-13f * player.direction, -6f * player.gravDir)
            + drawInfo.bodyVect + player.bodyPosition - player.bodyFrame.Size() * .5f
            + new Vector2(0f, player.gfxOffY) - Main.screenPosition;
        SpriteEffects effects = player.direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        if (player.gravDir < 0f)
            effects |= SpriteEffects.FlipVertically;
        drawInfo.DrawDataCache.Add(new DrawData(texture, center, null, drawInfo.colorArmorBody,
            player.bodyRotation, texture.Size() * .5f, 1f, effects));
    }
}
