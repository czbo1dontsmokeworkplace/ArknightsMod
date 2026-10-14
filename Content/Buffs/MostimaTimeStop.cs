using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Buffs;

public sealed class MostimaTimeStop : ModBuff
{
    public override string Texture => "ArknightsMod/Content/Buffs/StunDebuff";
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = true;
    }
}

public sealed class MostimaTimeStopNPC : GlobalNPC
{
    public static void Apply(NPC target, int duration)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || target.boss) return;
        int rootIndex = target.realLife >= 0 ? target.realLife : target.whoAmI;
        if (!Main.npc.IndexInRange(rootIndex) || Main.npc[rootIndex].boss) return;
        foreach (NPC npc in Main.ActiveNPCs)
            if (!npc.boss && (npc.whoAmI == rootIndex || npc.realLife == rootIndex))
            {
                npc.AddBuff(ModContent.BuffType<MostimaTimeStop>(), duration);
                npc.netUpdate = true;
            }
    }
    public override bool InstancePerEntity => true;
    private bool pinned;
    private Vector2 anchor;
    public override bool PreAI(NPC npc)
    {
        if (!npc.boss && npc.HasBuff<MostimaTimeStop>())
        {
            if (!pinned) { anchor = npc.position; pinned = true; }
            npc.position = anchor;
            npc.velocity = Vector2.Zero;
            if (Main.netMode == NetmodeID.Server && Main.GameUpdateCount % 10 == 0)
                npc.netUpdate = true;
            return false;
        }
        pinned = false;
        return true;
    }
    public override void PostAI(NPC npc)
    {
        if (pinned) { npc.position = anchor; npc.velocity = Vector2.Zero; }
    }
}
