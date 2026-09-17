using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Necrass;

public abstract class NecrassAttack : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_0";
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (NecrassCourt.Find(Projectile.owner)?.ModProjectile is NecrassCourt court && court.Elite >= 2
            && target.life < target.lifeMax * .5f) modifiers.SourceDamage *= 1.4f;
    }
}

public sealed class NecrassSoulBolt : NecrassAttack
{
    private int normalTarget = -1;
    private float particleDistance;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 18;
        ProjectileID.Sets.TrailingMode[Type] = 0;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 14;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = 1; Projectile.timeLeft = 180;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void AI()
    {
        // Mode 2 belongs only to the staff's normal attack; servant bolts retain their original AI.
        if (Projectile.ai[0] == 2)
        {
            NormalAttackAI();
            return;
        }
        if (Projectile.localAI[0]++ == 0 && Projectile.ai[0] == 1) Projectile.penetrate = 3;
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (Projectile.localAI[0] > 10)
        {
            NPC npc = NecrassCourt.Target(Projectile.Center, 450, Main.player[Projectile.owner]);
            if (npc != null)
            {
                Vector2 wanted = (npc.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * (Projectile.ai[0] == 1 ? 15 : 13);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, wanted, .055f);
            }
        }
        if (!Main.dedServ)
        {
            Lighting.AddLight(Projectile.Center, .32f, .08f, .48f);
            if (Projectile.localAI[0] % 3 == 0) NecrassVisuals.Embers(Projectile.Center, 1, .8f);
        }
    }
    private void NormalAttackAI()
    {
        if (Projectile.localAI[0] == 0)
        {
            // 30 updates per frame is about 30% of the former Vivid Clarity-style
            // 101 updates, so the visible flight speed drops by roughly 70%.
            Projectile.extraUpdates = 29;
            Projectile.timeLeft = 360;
        }
        float age = Projectile.localAI[0]++;
        // Let the initial curved shot read clearly before it begins to seek a target.
        // At 30 updates per frame, 180 updates is six game frames.
        if (age >= 180 && (int)age % 15 == 0)
            normalTarget = NecrassCourt.Target(Projectile.Center, 600, Main.player[Projectile.owner])?.whoAmI ?? -1;
        if (normalTarget >= 0 && Main.npc[normalTarget].active)
        {
            Vector2 delta = Main.npc[normalTarget].Center - Projectile.Center;
            float turn = MathHelper.WrapAngle(delta.ToRotation() - Projectile.ai[1]);
            Projectile.ai[1] += Math.Clamp(turn, -.018f, .018f);
        }
        float speed = MathHelper.Lerp(2.4f, 6f, MathHelper.Clamp(age / 110f, 0, 1));
        float wave = MathF.Sin(age * MathHelper.TwoPi / 100f + Projectile.ai[2]) * .20f
            * (1 - .65f * MathHelper.Clamp(age / 240f, 0, 1));
        Projectile.velocity = (Projectile.ai[1] + wave).ToRotationVector2() * speed;
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (Main.dedServ) return;
        particleDistance += speed;
        if (particleDistance >= 24)
        {
            particleDistance -= 24;
            NecrassVisuals.SoulHelix(Projectile, age * .13f + Projectile.ai[2]);
        }
        if ((int)age % 24 == 0)
        {
            Lighting.AddLight(Projectile.Center, .32f, .08f, .48f);
            NecrassVisuals.Embers(Projectile.Center, 1, .8f);
        }
    }
    public override void OnKill(int timeLeft)
    {
        NecrassVisuals.Embers(Projectile.Center, 12, 3);
        if (NecrassCourt.Authority) NecrassImpact.Spawn(Projectile, Projectile.Center, Vector2.Zero, 0, 3, 40);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        NecrassVisuals.Bolt(Projectile);
        return false;
    }
}

public sealed class NecrassImpact : NecrassAttack
{
    private int age;
    private int Mode => (int)Projectile.ai[0];
    private float Radius => Math.Clamp(Projectile.ai[1], 8, 400);
    internal static void Spawn(Projectile source, Vector2 center, Vector2 direction, int damage, int mode, float radius, int target = -1)
    {
        if (!NecrassCourt.Authority) return;
        Projectile.NewProjectile(source.GetSource_FromThis(), center, direction, ModContent.ProjectileType<NecrassImpact>(),
            damage, source.knockBack, source.owner, mode, radius, target);
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Magic;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.timeLeft = 32;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => age <= 6 && Projectile.damage > 0 ? null : false;
    public override bool? CanHitNPC(NPC target)
        => Mode == 4 && target.whoAmI != (int)Projectile.ai[2] ? false : null;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (!Collision.CanHitLine(Projectile.Center, 1, 1, targetHitbox.TopLeft(), targetHitbox.Width, targetHitbox.Height)) return false;
        if (Mode == 2)
        {
            float collision = 0;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
                Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * Radius, 46, ref collision);
        }
        Vector2 nearest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
        return Vector2.DistanceSquared(nearest, Projectile.Center) <= Radius * Radius;
    }
    public override void AI()
    {
        age++;
        if (age == 1)
        {
            NecrassVisuals.Embers(Projectile.Center, Mode == 1 ? 44 : Mode == 4 ? 5 : 18, Mode == 1 ? 7 : 3);
            if (Mode is 0 or 1 or 2 or 5)
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14 with { Volume = Mode == 1 ? .48f : .18f,
                    Pitch = Mode == 2 ? .3f : -.65f, MaxInstances = 4 }, Projectile.Center);
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        NecrassVisuals.Impact(Projectile.Center, Projectile.velocity, age / 32f, Radius, Mode);
        return false;
    }
}

public sealed class NecrassSleep : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_0";
    private int age;
    private NPC Target => (int)Projectile.ai[0] >= 0 && (int)Projectile.ai[0] < Main.maxNPCs ? Main.npc[(int)Projectile.ai[0]] : null;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2; Projectile.timeLeft = 780;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.netImportant = true;
    }
    public override bool? CanDamage() => false;
    public override void AI()
    {
        NPC target = Target;
        if (target == null || !target.active || target.friendly || NecrassCourt.Find(Projectile.owner) == null
            || NecrassCourt.Find(Projectile.owner).ai[0] != 1)
        { Projectile.Kill(); return; }
        Projectile.Center = target.Center;
        target.GetGlobalNPC<NecrassSleepState>().SleepTime = 2;
        if (++age % 30 == 0 && NecrassCourt.Authority)
            NecrassImpact.Spawn(Projectile, target.Center, Vector2.Zero, Projectile.damage, 4,
                Math.Max(target.width, target.height) * .5f + 10, target.whoAmI);
        if (!Main.dedServ && age % 8 == 0) NecrassVisuals.Embers(target.Center, 2, 1.2f);
        if (age >= Math.Clamp((int)Projectile.ai[1], 60, 900)) Projectile.Kill();
    }
    internal static bool HasBrand(NPC target, int owner)
    {
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == owner && p.ModProjectile is NecrassSleep && (int)p.ai[0] == target.whoAmI) return true;
        return false;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        NPC target = Target;
        if (target == null) return false;
        float pulse = .65f + .2f * MathF.Sin(age * .07f);
        NecrassVisuals.Seal(Projectile.Center, Math.Min(85, target.width * .55f + 22), age * -.025f, pulse);
        NecrassVisuals.Crown(target.Top + new Vector2(0, -18), 18, age * .015f, pulse, 3);
        return false;
    }
}

public sealed class NecrassSleepState : GlobalNPC
{
    public override bool InstancePerEntity => true;
    internal int SleepTime;
    public override void ResetEffects(NPC npc) { if (SleepTime > 0) SleepTime--; }
    public override bool PreAI(NPC npc)
    {
        // 沉睡只控制可击退的普通敌人；首领和多节生物仍承受灼烧，不冻结其 AI。
        if (SleepTime <= 0 || npc.boss || npc.realLife >= 0 || npc.knockBackResist <= 0 || npc.friendly) return true;
        npc.velocity = Vector2.Zero;
        return false;
    }
}
