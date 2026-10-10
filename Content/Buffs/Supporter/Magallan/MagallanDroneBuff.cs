using ArknightsMod.Content.Projectiles.Supporter.Magallan;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Buffs.Supporter.Magallan;

public sealed class MagallanDroneBuff : ModBuff
{
    // Temporary vanilla icon until the dedicated Soaring Dragon buff artwork is ready.
    public override string Texture => $"Terraria/Images/Buff_{BuffID.BabySlime}";

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
