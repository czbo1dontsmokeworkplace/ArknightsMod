using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Buffs;

public sealed class MousseScratch : ModBuff
{
    // Existing icon until dedicated scratch art is available.
    public override string Texture => "Terraria/Images/Buff_" + BuffID.Weak;
    public override void SetStaticDefaults() {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = true;
    }
}

public sealed class MousseScratchNPC : GlobalNPC
{
    public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers) {
        if (npc.HasBuff(ModContent.BuffType<MousseScratch>())) modifiers.SourceDamage *= .6f;
    }
    public override void ModifyHitNPC(NPC npc, NPC target, ref NPC.HitModifiers modifiers) {
        if (npc.HasBuff(ModContent.BuffType<MousseScratch>())) modifiers.SourceDamage *= .6f;
    }
}

public sealed class MousseScratchProjectile : GlobalProjectile
{
    public override void OnSpawn(Projectile projectile, IEntitySource source) {
        if (projectile.hostile && projectile.damage > 0
            && source is EntitySource_Parent { Entity: NPC npc }
            && npc.HasBuff(ModContent.BuffType<MousseScratch>()))
            projectile.damage = Math.Max(1, (int)MathF.Round(projectile.damage * .6f));
    }
}
