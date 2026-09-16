using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Buffs;

public sealed class PhantomSlow : ModBuff
{
    public override string Texture => "Terraria/Images/Buff_" + BuffID.Slow;
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = true;
    }
}
public sealed class PhantomBind : ModBuff
{
    public override string Texture => "ArknightsMod/Content/Buffs/StunDebuff";
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = true;
    }
}
public sealed class PhantomControlNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    private Vector2 beforeAI;
    public override bool PreAI(NPC npc)
    {
        beforeAI = npc.position;
        return true; // 束缚允许攻击与AI计时，眩晕交给已有的统一实现。
    }
    public override void PostAI(NPC npc)
    {
        if (npc.boss || npc.realLife >= 0) return;
        if (npc.HasBuff<PhantomBind>())
        {
            npc.position = beforeAI;
            npc.velocity = Vector2.Zero;
        }
        else if (npc.HasBuff<PhantomSlow>())
            npc.position -= npc.velocity * .8f;
    }
}
