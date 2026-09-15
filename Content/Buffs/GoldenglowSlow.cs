using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Buffs;

public sealed class GoldenglowSlow : ModBuff
{
    public override string Texture => "ArknightsMod/Content/Buffs/GoldenglowBeaconBuff";
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = true;
    }
}

// Reduce this tick's movement without repeatedly multiplying stored velocity or altering AI timers.
public sealed class GoldenglowSlowNPC : GlobalNPC
{
    public override void PostAI(NPC npc)
    {
        if (npc.HasBuff<GoldenglowSlow>())
            npc.position -= npc.velocity * 0.8f;
    }
}
