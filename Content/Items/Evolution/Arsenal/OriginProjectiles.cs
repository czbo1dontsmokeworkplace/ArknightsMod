using System;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Projectiles.Guard;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Evolution.Arsenal;

public sealed class OriginSwing : EvolutionArsenalProjectile
{
    private int bloodHitIndex;
    private bool bloodglowReleased;
    internal int Stage => Math.Clamp((int)Projectile.ai[0], 0, 2);
    internal float Progress => Math.Clamp(Projectile.ai[1] / Math.Max(1, Projectile.ai[2]), 0, 1);
    internal Vector2 Aim => Projectile.velocity.SafeNormalize(Vector2.UnitX);
    internal int Facing => Aim.X < 0 ? -1 : 1;
    internal Vector2 Tip(float progress) => EvolutionArsenalRules.SwordTip(Projectile.Center, Aim, Facing, Stage, progress);
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 18; Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.penetrate = -1; Projectile.timeLeft = 180;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1; Projectile.ownerHitCheck = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => EvolutionArsenalRules.SwordActive(Progress);
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float previous = Math.Max(0, (Projectile.ai[1] - 1) / Math.Max(1, Projectile.ai[2]));
        // Sweep the actual blade over this tick, including fast attack-speed modifiers.
        int samples = Math.Clamp((int)MathF.Ceiling(Vector2.Distance(Tip(previous), Tip(Progress)) / 14), 4, 24);
        for (int i = 0; i <= samples; i++)
        {
            float p = MathHelper.Lerp(previous, Progress, i / (float)samples);
            if (EvolutionArsenalRules.Capsule(targetHitbox, Projectile.Center + (Tip(p) - Projectile.Center) * .12f, Tip(p), (Stage == 2 ? 40 : 32) * EvolutionArsenalRules.OriginScale)) return true;
        }
        return false;
    }
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.CCed || owner.HeldItem.type != ModContent.ItemType<OriginSever>()) { Projectile.Kill(); return; }
        Projectile.Center = owner.MountedCenter; owner.direction = Facing; owner.heldProj = Projectile.whoAmI;
        owner.SetDummyItemTime(2);
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, (Tip(Progress) - Projectile.Center).ToRotation() - MathHelper.PiOver2);
        float old = Progress;
        Projectile.ai[1]++;
        if (Projectile.ai[1] >= Math.Max(6, Projectile.ai[2])) { Projectile.Kill(); return; }
        if (!Main.dedServ) OriginCutVisuals.Shed(this, old);
        if (old < .44f && Progress >= .44f)
        {
            if (!Main.dedServ) { SoundEngine.PlaySound(SoundID.Item71 with { Pitch = Stage == 2 ? -.4f : -.2f, Volume = .8f }, Projectile.Center); EvolutionWeaponVisuals.Sparks(Tip(Progress), 9, 3 * EvolutionArsenalRules.OriginScale, EvolutionArsenalRules.OriginScale); }
        }
        for (int i = 0; i < 6; i++)
        {
            float release = .06f + i * .09f;
            if (old >= release || Progress < release || Projectile.owner != Main.myPlayer) continue;
            Vector2 point = Tip(release), outward = (point - Projectile.Center).SafeNormalize(Aim);
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), point, outward * (4.2f * EvolutionArsenalRules.OriginScale),
                ModContent.ProjectileType<OriginMist>(), Math.Max(1, (int)(Projectile.damage * .15f)), 0, Projectile.owner);
        }
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (TryReleaseBloodglow())
        {
            int count = Stage == 2 ? 5 : 3;
            for (int i = 0; i < count; i++)
            {
                Vector2 point = EvolutionArsenalRules.HitBloodglowOrigin(target, i, Main.rand.NextFloat(-1, 1), Main.rand.NextFloat());
                Vector2 velocity = (target.Center - point).SafeNormalize(-Vector2.UnitY) * 32;
                Projectile.NewProjectile(Projectile.GetSource_OnHit(target), point, velocity, ModContent.ProjectileType<OriginShard>(),
                    Math.Max(1, (int)(Projectile.damage * (Stage == 2 ? .24f : .32f))), 2, Projectile.owner, 0, target.whoAmI + 1, i);
            }
        }
        if (Stage == 2) target.AddBuff(ModContent.BuffType<OriginBind>(), 60);
        if (!Main.dedServ)
        {
            CenturionHitVisuals.BloodSpray(target, OriginCutVisuals.CutDirection(this), Stage == 2 ? 1.6f : 1.3f,
                bloodHitIndex++, EvolutionWeaponVisuals.Blood, EvolutionArsenalRules.OriginScale);
        }
    }
    internal bool TryReleaseBloodglow()
    {
        if (bloodglowReleased || Projectile.owner != Main.myPlayer) return false;
        bloodglowReleased = true;
        return true;
    }
    public override bool PreDraw(ref Color lightColor) { EvolutionWeaponVisuals.SwordDetails(this); return false; }
    internal override void DrawMaterial() => EvolutionWeaponVisuals.SwordMaterial(this);
}

public partial class OriginShard : EvolutionArsenalProjectile
{
    protected virtual bool Summoned => false;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = Summoned ? 12 : 20; ProjectileID.Sets.TrailingMode[Type] = 2;
        if (Summoned) ProjectileID.Sets.MinionShot[Type] = true;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = Summoned ? 14 : 10; Projectile.timeLeft = 110; Projectile.penetrate = 1;
        Projectile.friendly = true; Projectile.DamageType = Summoned ? DamageClass.Summon : DamageClass.Melee;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
    }
    public override bool? CanDamage() => Projectile.ai[0] >= (Summoned ? 6 : 1);
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        => Summoned ? null : hasSweep ? EvolutionArsenalRules.Capsule(targetHitbox, sweepStart, Projectile.Center, 12 * EvolutionArsenalRules.OriginScale) : projHitbox.Intersects(targetHitbox);
    public override void AI()
    {
        if (!Summoned) { UpdateSeverBlade(); return; }
        Projectile.ai[0]++;
        if (Projectile.ai[0] >= 6)
        {
            NPC target = EvolutionArsenalRules.Target(Projectile.Center, 800, Main.player[Projectile.owner], Summoned);
            if (target != null)
            {
                float current = Projectile.velocity.ToRotation(), desired = (target.Center - Projectile.Center).ToRotation();
                Projectile.velocity = current.AngleTowards(desired, .085f).ToRotationVector2() * MathHelper.Lerp(Projectile.velocity.Length(), 15, .04f);
            }
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (!Main.dedServ && (int)Projectile.ai[0] % 5 == 0)
            OperatorTextureEffects.Mote(Projectile.Center, -Projectile.velocity * .08f, EvolutionWeaponVisuals.Blood, 5, 16);
    }
    public override void OnKill(int timeLeft)
    {
        if (Main.dedServ) return;
        EvolutionWeaponVisuals.Sparks(Projectile.Center, Summoned ? 5 : 9, Summoned ? 2 : 4 * EvolutionArsenalRules.OriginScale, Summoned ? 1 : EvolutionArsenalRules.OriginScale);
        if (!Summoned) OperatorTextureEffects.Puff(Projectile.Center, -Projectile.velocity * .025f, new Color(145, 20, 42), 22 * EvolutionArsenalRules.OriginScale, 18);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!Summoned && !Main.dedServ)
            CenturionHitVisuals.BloodSpray(target, Projectile.velocity, .6f, 0, EvolutionWeaponVisuals.Blood, EvolutionArsenalRules.OriginScale);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (Summoned) EvolutionWeaponVisuals.ShardDetails(Projectile);
        else OriginSeverBladeVisuals.Details(Projectile);
        return false;
    }
    internal override void DrawMaterial()
    {
        if (!Summoned) { OriginSeverBladeVisuals.Draw(Projectile); return; }
        for (int i = 1; i < Projectile.oldPos.Length; i++)
        {
            if (Projectile.oldPos[i] == Vector2.Zero) break;
            EvolutionWeaponVisuals.Ribbon(Projectile.oldPos[i - 1] + Projectile.Size * .5f, Projectile.oldPos[i] + Projectile.Size * .5f,
                11 * (1 - i / 12f), .65f * (1 - i / 12f), Projectile.identity);
        }
        EvolutionWeaponVisuals.Core(Projectile.Center, 23, .9f, Projectile.rotation);
    }
}
public sealed class NestSpore : OriginShard { protected override bool Summoned => true; }

public sealed class OriginMist : EvolutionArsenalProjectile
{
    internal float Age => Projectile.ai[0];
    internal float Radius => (22 + 24 * Math.Clamp(Age / 15, 0, 1)) * EvolutionArsenalRules.OriginScale;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 10; Projectile.timeLeft = EvolutionArsenalRules.MistLife; Projectile.penetrate = -1;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.usesIDStaticNPCImmunity = true; Projectile.idStaticNPCHitCooldown = 30;
    }
    public override bool? CanDamage() => Age >= 4 && Age <= 31;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        => Vector2.DistanceSquared(Projectile.Center, Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight())) < Radius * Radius;
    public override void AI() { Projectile.ai[0]++; Projectile.velocity *= .975f; Projectile.rotation += .022f; }
    public override bool PreDraw(ref Color lightColor)
    {
        float opacity = MathF.Sin(MathHelper.Pi * Math.Clamp(Age / EvolutionArsenalRules.MistLife, 0, 1)) * .28f;
        OperatorTextureEffects.Cloud(Projectile.Center, Radius * 2.5f, new Color(120, 22, 41), opacity, Projectile.identity * 2 + Projectile.rotation);
        return false;
    }
    internal override void DrawMaterial() => EvolutionWeaponVisuals.Mist(Projectile.Center, Radius * 2.5f, MathF.Sin(MathHelper.Pi * Math.Clamp(Age / EvolutionArsenalRules.MistLife, 0, 1)) * .22f, Projectile.identity, Projectile.rotation);
}

public sealed class OriginBind : ModBuff
{
    public override string LocalizationCategory => "Buffs.EvolutionArsenal";
    public override string Texture => $"Terraria/Images/Buff_{BuffID.Bleeding}";
    public override void SetStaticDefaults() { Main.debuff[Type] = true; Main.buffNoSave[Type] = true; }
    public override void Update(NPC npc, ref int buffIndex) => npc.GetGlobalNPC<OriginBoundNPC>().Bound = true;
}
public sealed class OriginBoundNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    internal bool Bound;
    public override void ResetEffects(NPC npc) => Bound = false;
    public override void PostAI(NPC npc)
    {
        bool bossBody = npc.realLife >= 0 && npc.realLife < Main.maxNPCs && Main.npc[npc.realLife].boss;
        if (Bound && !npc.boss && !bossBody && npc.knockBackResist > 0) npc.velocity *= .15f;
    }
    public override void UpdateLifeRegen(NPC npc, ref int damage)
    {
        if (!Bound) return;
        if (npc.lifeRegen > 0) npc.lifeRegen = 0;
        npc.lifeRegen -= 24; damage = Math.Max(damage, 6);
    }
}
