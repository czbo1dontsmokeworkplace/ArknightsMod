using ArknightsMod.Systems.Gameplay.Damage;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Buffs.Caster.Leonhardt;

/// <summary>解构与爆破留下的六秒法抗裂隙。</summary>
public sealed class LeonhardtFracture : ModBuff
{
    public override string Texture => "Terraria/Images/Buff_67";
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = true;
    }
}

public sealed class LeonhardtFractureNPC : GlobalNPC
{
    private static bool Affected(NPC npc) => npc.HasBuff(ModContent.BuffType<LeonhardtFracture>());

    private static float ResistanceCorrection(NPC npc)
    {
        float resistance = System.Math.Clamp(npc.GetGlobalNPC<DamageCategoryNPC>().artsResistance, 0f, .95f);
        return (1f - System.Math.Max(0f, resistance - .15f)) / (1f - resistance);
    }

    public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
    {
        if (Affected(npc) && projectile.GetGlobalProjectile<ArtsProjectileMarker>().IsArtsDamage)
            modifiers.FinalDamage *= ResistanceCorrection(npc);
    }

    public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
    {
        if (Affected(npc) && item.DamageType.CountsAsClass(DamageClass.Magic))
            modifiers.FinalDamage *= ResistanceCorrection(npc);
    }
}
