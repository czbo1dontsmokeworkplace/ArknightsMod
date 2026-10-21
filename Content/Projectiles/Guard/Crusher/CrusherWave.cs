using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Guard.Crusher;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Crusher;

// One Terra-shaped carrier per swing. Children are finite, non-recursive cores.
public sealed class CrusherWave : ModProjectile
{
    internal const int ActiveLifetime = 43, DamageEndTime = 34;
    private static readonly int[] BaseEmissionFrames = [6, 10, 14, 18, 23];
    internal const float CoreFrequencyMultiplier = 1.5f;
    // Compress the original cadence by 2/3 and round the 7.5-shot burst up to 8.
    internal static int CoreEmissionCount => (int)Math.Ceiling(BaseEmissionFrames.Length * CoreFrequencyMultiplier);
    internal static int EmissionFrame(int pulse) => (int)Math.Ceiling(
        (BaseEmissionFrames[pulse % BaseEmissionFrames.Length] + pulse / BaseEmissionFrames.Length * 21) / CoreFrequencyMultiplier);
    private readonly bool[] hitTargets = new bool[Main.maxNPCs];
    private int age, hits, emittedMask, emissionDamage;
    internal int Age => age;
    internal CrusherKind Kind => (CrusherKind)Math.Clamp((int)Projectile.ai[0], 0, 3);
    internal bool HasHit(int npc) => (uint)npc < hitTargets.Length && hitTargets[npc];
    public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.TerraBlade2Shot}";
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 750;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee; Projectile.penetrate = -1;
        Projectile.timeLeft = 48; Projectile.ignoreWater = true; Projectile.tileCollide = false;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void OnSpawn(Terraria.DataStructures.IEntitySource source) => emissionDamage = Projectile.damage;
    public override bool? CanDamage() => age < DamageEndTime && hits < 8 ? null : false;
    public override void AI()
    {
        age++;
        Projectile.rotation = Projectile.velocity.SafeNormalize(Vector2.UnitX).ToRotation();
        float scale = Projectile.ai[2] > 0 ? Projectile.ai[2] : 1.35f;
        float contraction = 1 - MathF.Pow(1 - Math.Clamp((age - 8f) / 35f, 0, 1), 2);
        Projectile.scale = MathHelper.Lerp(1.5f, 1, contraction) * scale;
        Projectile.Opacity = Math.Clamp(age / 6f, 0, 1) * Math.Clamp((ActiveLifetime - age) / 12f, 0, 1);
        if (Projectile.velocity.LengthSquared() > 64) Projectile.velocity *= .94f;
        if (age < DamageEndTime) {
            for (int i = 0; i < CoreEmissionCount; i++) {
                if (age < EmissionFrame(i) || (emittedMask & (1 << i)) != 0) continue;
                emittedMask |= 1 << i;
                if (Projectile.owner == Main.myPlayer) EmitCore(i);
            }
        }
        if (age >= DamageEndTime) { Projectile.friendly = false; Projectile.velocity *= .76f; }
        if (!Main.dedServ && CrusherVisuals.Visible(Projectile.Center)) {
            Lighting.AddLight(Projectile.Center, CrusherRules.Edge(Kind).ToVector3() * .4f * Projectile.Opacity);
            if (age % 3 == 0) CrusherWaveVisuals.WaveMotes(Projectile, Kind);
        }
        if (age >= ActiveLifetime) Projectile.Kill();
    }
    private void EmitCore(int pulse)
    {
        Vector2 forward = Projectile.rotation.ToRotationVector2(), side = forward.RotatedBy(MathHelper.PiOver2);
        float flank = pulse % 2 == 0 ? -1 : 1;
        Vector2 launch = (-forward).RotatedBy(flank * MathHelper.ToRadians(Main.rand.NextFloat(15, 20))) * 30;
        Vector2 release = Projectile.Center - forward * 6 * Projectile.scale + side * flank * 38 * Projectile.scale;
        int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), release, launch,
            ModContent.ProjectileType<CrusherWaveCore>(), Math.Max(1, (int)(emissionDamage * .2f)),
            Projectile.knockBack * .5f, Projectile.owner, (int)Kind, 0, flank);
        if (id < Main.maxProjectiles && Main.projectile[id].ModProjectile is CrusherWaveCore core) {
            core.LinkToWave(Projectile); Main.projectile[id].netUpdate = true;
        }
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
        targetHitbox.IntersectsConeFastInaccurate(Projectile.Center, 90 * Projectile.scale,
            Projectile.rotation, MathHelper.PiOver4) &&
        Collision.CanHit(Projectile.Center, 0, 0, targetHitbox.Center.ToVector2(), 0, 0);
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        hitTargets[target.whoAmI] = true; hits++;
        Projectile.velocity *= .85f; Projectile.damage = Math.Max(1, (int)(Projectile.damage * .85f));
        if (hits >= 8) Projectile.friendly = false;
        CrusherWaveVisuals.Impact(target.Hitbox.ClosestPointInRect(Projectile.Center), Kind, .7f);
        Projectile.netUpdate = true;
    }
    public override void OnKill(int timeLeft) => CrusherWaveVisuals.Dissipate(Projectile.Center, Projectile.rotation, Kind, Projectile.scale);
    public override bool PreDraw(ref Color lightColor)
    {
        CrusherWaveVisuals.DrawWave(Projectile, Kind, age / (float)ActiveLifetime); return false;
    }
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write((byte)age); writer.Write((byte)hits); writer.Write((byte)emittedMask); writer.Write(emissionDamage);
        int count = 0; foreach (bool hit in hitTargets) if (hit) count++;
        writer.Write((byte)count);
        for (int i = 0; i < hitTargets.Length; i++) if (hitTargets[i]) writer.Write((byte)i);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        age = Math.Clamp((int)reader.ReadByte(), 0, ActiveLifetime); hits = Math.Clamp((int)reader.ReadByte(), 0, 8);
        emittedMask = reader.ReadByte() & ((1 << CoreEmissionCount) - 1); emissionDamage = Math.Max(1, reader.ReadInt32());
        Array.Clear(hitTargets); int count = reader.ReadByte();
        for (int i = 0; i < count; i++) { int npc = reader.ReadByte(); if (npc < hitTargets.Length) hitTargets[npc] = true; }
        if (age >= DamageEndTime || hits >= 8) Projectile.friendly = false;
    }
}
