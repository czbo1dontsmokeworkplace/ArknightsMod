using ArknightsMod.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Buffs;

public sealed class AstonishingMoment : ModBuff
{
    public override string Texture => "Terraria/Images/Buff_" + BuffID.Wrath;
    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = true;
        Main.buffNoSave[Type] = true;
    }
    public override void Update(Player player, ref int buffIndex)
    {
        if (player.GetModPlayer<DreadnoughtPlayer>().HasMoment) player.buffTime[buffIndex] = 2;
        else player.DelBuff(buffIndex--);
    }
}
