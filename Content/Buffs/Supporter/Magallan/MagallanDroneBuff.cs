using ArknightsMod.Content.Items.Weapons.Supporter.Magallan;
using ArknightsMod.Content.Projectiles.Supporter.Magallan;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Buffs.Supporter.Magallan;

public sealed class MagallanDroneBuff : ModBuff
{
    public override string Texture => "ArknightsMod/Content/Buffs/Supporter/Magallan/MagallanDroneBuff_1";

    public override bool PreDraw(SpriteBatch spriteBatch, int buffIndex, ref BuffDrawParams drawParams)
    {
        // The last commanded module persists when the controller is put away.
        int module = Main.LocalPlayer.GetModPlayer<MagallanPlayer>().Module;
        drawParams.Texture = ModContent.Request<Texture2D>(
            $"ArknightsMod/Content/Buffs/Supporter/Magallan/MagallanDroneBuff_{module + 1}").Value;
        drawParams.SourceRectangle = drawParams.Texture.Bounds;
        return true;
    }

    public override void SetStaticDefaults()
    {
        Main.buffNoSave[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        if (player.ownedProjectileCounts[ModContent.ProjectileType<MagallanDrone>()] > 0)
            player.buffTime[buffIndex] = 2;
        else
        {
            player.DelBuff(buffIndex);
            buffIndex--;
        }
    }
}
