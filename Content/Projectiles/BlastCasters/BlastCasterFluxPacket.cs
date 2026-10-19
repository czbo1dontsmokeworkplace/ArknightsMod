using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.BlastCasters;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.BlastCasters;

// One emission is one straight packet. Only the reference's soft body/core
// light composition is retained, with no twin lanes or spiral movement.
public sealed class BlastCasterFluxPacket : ModProjectile
{
    private Vector2[] history, axes;
    private float[] births;
    private int count, relayIdentity = -1;
    private float distance, clock, nextStamp;
    private bool redirected, fading, exploded;
    private Vector2 target;
    private BlastCasterKind kind = BlastCasterKind.Aphrissa;
    internal void Configure(BlastCasterKind identity, Vector2 aimTarget, int relay)
    {
        kind = identity; target = aimTarget; relayIdentity = relay;
        Projectile.extraUpdates = BlastCasterRules.Updates(kind) - 1;
        ApplySize();
        Projectile.netUpdate = true;
    }
    public override string Texture => "ArknightsMod/Common/Particle/DefaultParticle";
    public override void OnSpawn(IEntitySource source)
    {
        history = new Vector2[BlastCasterRules.OpticalHistory];
        axes = new Vector2[BlastCasterRules.OpticalHistory];
        births = new float[BlastCasterRules.OpticalHistory];
        Projectile.GetGlobalProjectile<global::ArknightsMod.Systems.Gameplay.Damage.ArtsProjectileMarker>().IsArtsDamage = true;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 30;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Ranged;
        Projectile.ignoreWater = true; Projectile.tileCollide = false;
        Projectile.penetrate = Projectile.maxPenetrate = -1;
        Projectile.extraUpdates = BlastCasterRules.OpticalUpdates - 1;
        Projectile.timeLeft = 600;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write((byte)kind); writer.WriteVector2(target); writer.Write(relayIdentity);
        writer.Write(redirected); writer.Write(fading);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        kind = (BlastCasterKind)Math.Clamp(reader.ReadByte(), (byte)0, (byte)2);
        target = reader.ReadVector2(); relayIdentity = reader.ReadInt32();
        redirected = reader.ReadBoolean(); fading = reader.ReadBoolean();
        ApplySize();
        Projectile.extraUpdates = fading ? 0 : BlastCasterRules.Updates(kind) - 1;
        if (fading) { Projectile.friendly = false; Projectile.velocity = Vector2.Zero; Projectile.timeLeft = Math.Min(7, Projectile.timeLeft); }
    }
    private float FlightRange => BlastCasterRules.FlightRange(kind, (int)Projectile.ai[0]) + (redirected ? 320 : 0);
    private float WidthScale => BlastCasterRules.SizeMultiplier(kind)
        * (kind == BlastCasterKind.Aphrissa && Projectile.ai[0] == 2 ? 1.4f : 1f);
    private void ApplySize()
    {
        Vector2 center = Projectile.Center;
        Projectile.width = Projectile.height = (int)MathF.Ceiling(BlastCasterRules.FluxContactRadius * 2 * WidthScale);
        Projectile.Center = center;
    }
    public override bool? CanDamage() => fading ? false : null;
    public override bool ShouldUpdatePosition() => !fading;
    private void Fade()
    {
        if (fading) return;
        fading = true; Projectile.friendly = false; Projectile.extraUpdates = 0;
        Projectile.numUpdates = -1; Projectile.timeLeft = 7; Projectile.velocity = Vector2.Zero;
        if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
    }
    private void Stamp()
    {
        if (Main.dedServ) return;
        int last = Math.Min(count, history.Length - 1);
        for (int i = last; i > 0; i--) { history[i] = history[i - 1]; axes[i] = axes[i - 1]; births[i] = births[i - 1]; }
        history[0] = Projectile.Center; axes[0] = Projectile.velocity.SafeNormalize(Vector2.UnitX); births[0] = clock;
        count = Math.Min(count + 1, history.Length);
    }
    public override void AI()
    {
        clock += 1f / Projectile.MaxUpdates;
        if (fading) { Projectile.extraUpdates = 0; Projectile.numUpdates = -1; return; }
        if (!float.IsFinite(Projectile.velocity.X) || !float.IsFinite(Projectile.velocity.Y)) { Projectile.Kill(); return; }
        if (distance >= FlightRange) { Fade(); return; }
        if (kind == BlastCasterKind.Aphrissa && !redirected && relayIdentity >= 0)
        {
            Vector2 relayAt = new(Projectile.ai[1], Projectile.ai[2]);
            if (Vector2.DistanceSquared(Projectile.Center, relayAt) <= 22 * 22)
            {
                Projectile relay = AphrissaRelay.Find(Main.player[Projectile.owner]);
                redirected = true;
                if (relay != null && relay.identity == relayIdentity && Vector2.DistanceSquared(relay.Center, relayAt) < 16)
                {
                    Projectile.velocity = (target - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * BlastCasterRules.OpticalStep;
                    if (Projectile.ai[0] == 3) Projectile.damage = (int)(Projectile.damage * 1.25f);
                    BlastCasterVisuals.RelayFlash(relayAt, Projectile.velocity.SafeNormalize(Vector2.UnitX));
                }
                if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
            }
        }
        // Constant direction between relay turns: no wave, spiral, homing or hitscan.
        if (clock >= nextStamp)
        {
            Stamp(); nextStamp += 1f / 3f;
        }
        distance += Projectile.velocity.Length();
        if (Projectile.numUpdates == -1)
        {
            Color tint = BlastCasterRules.Tint(kind);
            Lighting.AddLight(Projectile.Center, tint.ToVector3() * .32f);
            if ((int)clock % 4 == 0) BlastCasterVisuals.FluxWake(Projectile.Center, Projectile.velocity, kind);
        }
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float radius = BlastCasterRules.FluxContactRadius * WidthScale;
        float contact = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            Projectile.Center - Projectile.velocity, Projectile.Center, radius * 2, ref contact);
    }
    private Vector2 Contact(NPC npc)
        => Vector2.Clamp(Projectile.Center, npc.position, npc.position + npc.Size);
    public override void OnHitNPC(NPC npc, NPC.HitInfo hit, int damageDone)
    {
        int mode = (int)Projectile.ai[0];
        npc.AddBuff(BuffID.OnFire3, kind == BlastCasterKind.Aphrissa ? 300 : 1200);
        if (kind == BlastCasterKind.Corroserum) npc.AddBuff(BuffID.CursedInferno, 1200);
        if (kind == BlastCasterKind.Corroserum && mode == 2 || kind == BlastCasterKind.Aphrissa && mode == 3) npc.AddBuff(BuffID.Ichor, 180);
        if (kind == BlastCasterKind.Ifrit && mode == 3) npc.AddBuff(BuffID.Oiled, 180);
        Vector2 point = Contact(npc);
        BlastCasterVisuals.Hit(Projectile, npc, point, kind);
        if (Projectile.owner != Main.myPlayer || kind != BlastCasterKind.Ifrit) return;
        if (mode == 2 && !exploded)
        {
            exploded = true;
            SpawnHeat(point, Math.Max(1, Projectile.damage / 2), 0);
        }
        if (mode == 3)
        {
            Player owner = Main.player[Projectile.owner];
            var runtime = owner.GetModPlayer<BlastCasterPlayer>();
            if (Main.GameUpdateCount >= runtime.NextScorch && owner.ownedProjectileCounts[ModContent.ProjectileType<BlastCasterHeat>()] < 7)
            {
                runtime.NextScorch = Main.GameUpdateCount + 20;
                SpawnHeat(point, Math.Max(1, Projectile.damage / 3), 1);
            }
        }
    }
    private void SpawnHeat(Vector2 point, int damage, int mode)
    {
        Projectile heat = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), point, Vector2.Zero,
            ModContent.ProjectileType<BlastCasterHeat>(), damage, mode == 0 ? 2 : 0, Projectile.owner, mode);
        heat.CritChance = Projectile.CritChance;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        BlastCasterFluxTrail.Draw(history, axes, births, count, clock, kind, WidthScale);
        return false;
    }
}
