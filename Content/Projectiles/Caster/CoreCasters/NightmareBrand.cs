using System;
using ArknightsMod.Content.Items.Weapons.Caster.CoreCasters;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.CoreCasters;

public sealed class NightmareBrand : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_0";
    private Vector2 previous;
    private float distance;
    private int age;
    internal static void Apply(Player owner, NPC target, int damage, int duration)
    {
        foreach (var p in Main.ActiveProjectiles)
            if (p.type == ModContent.ProjectileType<NightmareBrand>() && p.owner == owner.whoAmI && (int)p.ai[0] == target.whoAmI)
            { p.timeLeft = duration; p.damage = damage; p.netUpdate = true; return; }
        var brand = CoreCasterRules.Spawn(owner.GetSource_ItemUse(owner.HeldItem), target.Center, Vector2.Zero,
            ModContent.ProjectileType<NightmareBrand>(), damage, 0, owner.whoAmI, target.whoAmI, duration, target.type);
        if (brand != null) { brand.timeLeft = duration; brand.netUpdate = true; }
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2; Projectile.timeLeft = 420;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void AI()
    {
        if (!Main.npc.IndexInRange((int)Projectile.ai[0])) { Projectile.Kill(); return; }
        NPC target = Main.npc[(int)Projectile.ai[0]];
        if (!target.active || target.life <= 0 || target.type != (int)Projectile.ai[2]) { Projectile.Kill(); return; }
        if (age == 0) { previous = target.Center; Projectile.timeLeft = Math.Clamp((int)Projectile.ai[1], 1, 1800); }
        // Teleports cannot manufacture arbitrarily large damage. Boss AI and velocity are never changed.
        distance += Math.Min(24, Vector2.Distance(previous, target.Center));
        distance = Math.Min(120, distance);
        previous = Projectile.Center = target.Center;
        if (!target.boss && target.realLife < 0 && target.knockBackResist > 0)
            target.AddBuff(ModContent.BuffType<NightmareFetter>(), 2);
        if (++age % 12 == 0 && distance >= 16 && CoreCasterRules.Authority)
        {
            int damage = Math.Max(1, (int)(Projectile.damage * Math.Min(1, distance / 96)));
            CoreCasterRules.Spawn(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
                ModContent.ProjectileType<CoreCasterPulse>(), damage, 0, Projectile.owner,
                (int)CoreCasterKind.Nightmare, Math.Min(120, Math.Max(target.width, target.height) * .5f + 20), target.whoAmI + 1);
            distance = 0;
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (Main.npc.IndexInRange((int)Projectile.ai[0]))
        {
            NPC target = Main.npc[(int)Projectile.ai[0]];
            CoreCasterVisuals.Brand(target.Center, Math.Min(65, target.width * .5f + 18), age / 60f, Projectile.timeLeft / 20f);
        }
        return false;
    }
}

public sealed class NightmareFetter : ModBuff
{
    public override string Texture => "Terraria/Images/Buff_" + BuffID.ShadowFlame;
    public override void SetStaticDefaults() { Main.debuff[Type] = true; Main.buffNoSave[Type] = true; }
    public override void Update(NPC npc, ref int buffIndex)
    {
        if (!npc.boss && npc.realLife < 0 && npc.knockBackResist > 0) npc.velocity *= .9f;
    }
}

public sealed class EyjafjallaScorch : ModBuff
{
    public override string Texture => "Terraria/Images/Buff_" + BuffID.OnFire3;
    public override void SetStaticDefaults() { Main.debuff[Type] = true; Main.buffNoSave[Type] = true; }
}
