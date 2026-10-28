using ArknightsMod.Content.Projectiles.Sniper.Rosmontis;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Common.GlobalNPCs;

public sealed class RosmontisTacticalGlobalNPC : GlobalNPC
{
    private static void ApplyDefenseReduction(NPC npc, ref NPC.HitModifiers modifiers) {
        if (RosmontisTacticalEquipment.IsInDebuffRange(npc))
            modifiers.Defense.Flat -= System.Math.Min(RosmontisTacticalEquipment.DefenseReduction,
                System.Math.Max(0, npc.defense));
    }

    public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        => ApplyDefenseReduction(npc, ref modifiers);

    public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        => ApplyDefenseReduction(npc, ref modifiers);
}
