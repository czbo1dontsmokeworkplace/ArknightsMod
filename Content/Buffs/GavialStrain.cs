using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Buffs;

public sealed class GavialStrain : ModBuff
{
    public override string Texture => "Terraria/Images/Buff_" + BuffID.Bleeding;
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
    }
}
