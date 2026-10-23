using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Defender;

// Same centred, behind-body cache insertion used by Fran's ShiFuBackDrawPlayer.
public sealed class DefenderBackWeaponPlayer : ModPlayer
{
    internal static bool ShouldDraw(Player player) => player.active && !player.dead && !player.invis
        && !player.CCed && !player.noItems && DefenderRules.IsWeapon(player.HeldItem)
        && DefenderRules.MainArt(player.HeldItem) != null
        && DefenderHoldout.Find(player) is { MainWeaponInHands: false };

    internal static float Rotation(Player player, bool verticalTexture)
    {
        if (DefenderRules.IsFortress(player.HeldItem)) return -player.direction * MathHelper.ToRadians(25f);
        if (verticalTexture) return -player.direction * MathHelper.PiOver4;
        return player.direction > 0 ? MathHelper.Pi : 0f;
    }

    public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;
        if (drawInfo.shadow != 0f || !ShouldDraw(player)) return;
        Texture2D texture = ModContent.Request<Texture2D>(DefenderRules.MainArt(player.HeldItem)).Value;
        bool vertical = texture.Height > texture.Width * 2;
        bool gun = DefenderRules.IsFortress(player.HeldItem) || player.HeldItem.ModItem?.Name == "LiskarmPistol";
        float scale = gun ? 1f : DefenderRules.WeaponLength(player.HeldItem)
            / (vertical ? texture.Height : texture.Size().Length());
        SpriteEffects effects = player.direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        if (player.gravDir < 0) effects |= SpriteEffects.FlipVertically;
        Vector2 position = drawInfo.Position + new Vector2(player.width * .5f, player.height * .5f)
            + player.bodyPosition + new Vector2(-player.direction * 5f, -4f * player.gravDir + player.gfxOffY)
            - Main.screenPosition;
        var weapon = new DrawData(texture, position, null,
            player.GetImmuneAlphaPure(drawInfo.colorArmorBody, drawInfo.shadow),
            Rotation(player, vertical) * player.gravDir + player.bodyRotation,
            texture.Size() * .5f, scale, effects, 0f) { shader = 0 };
        drawInfo.DrawDataCache.Insert(0, weapon);
    }
}
