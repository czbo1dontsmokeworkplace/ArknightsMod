using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Mostima;

public sealed class MostimaTemporalDrag : ModBuff
{
    public override string Texture => "Terraria/Images/Buff_" + BuffID.Slow;
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = true;
    }
}
public sealed class MostimaTemporalDragNPC : GlobalNPC
{
    public override void PostAI(NPC npc)
    {
        // Preserve AI and attacks; offset half of this tick's translation for ordinary enemies.
        if (!npc.boss && npc.realLife < 0 && npc.HasBuff<MostimaTemporalDrag>())
            npc.position -= npc.velocity * .5f;
    }
}
