using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.BlastCasters;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;

namespace ArknightsMod.Content.Projectiles.BlastCasters;

// One emission is one straight packet. Only the reference's soft body/core
// light composition is retained, with no twin lanes or spiral movement.
public sealed class BlastCasterFluxPacket : ModProjectile
{
    private int relayIdentity = -1;
    private float distance, clock;
    private bool redirected, fading, exploded;
    private Vector2 target;
    private BlastCasterKind kind = BlastCasterKind.Aphrissa;
    private PhoenixBeamPulse opticalBeam;
    private Vector2 beamOrigin;
    private bool visualStarted;
    private Vector2 bornAt,bornOwner,bornAxis,relayOrigin,relayAxis;
    private bool frameReady;
    private float relayTravel;
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
        writer.Write(frameReady);writer.WriteVector2(bornAt);writer.WriteVector2(bornOwner);writer.WriteVector2(bornAxis);
        writer.Write(distance);writer.Write(clock);writer.Write(relayTravel);writer.WriteVector2(relayOrigin);writer.WriteVector2(relayAxis);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        kind = (BlastCasterKind)Math.Clamp(reader.ReadByte(), (byte)0, (byte)2);
        target = reader.ReadVector2(); relayIdentity = reader.ReadInt32();
        redirected = reader.ReadBoolean(); fading = reader.ReadBoolean();
        frameReady=reader.ReadBoolean();bornAt=reader.ReadVector2();bornOwner=reader.ReadVector2();bornAxis=reader.ReadVector2();
        distance=reader.ReadSingle();clock=reader.ReadSingle();relayTravel=reader.ReadSingle();relayOrigin=reader.ReadVector2();relayAxis=reader.ReadVector2();
        ApplySize();
        Projectile.extraUpdates = fading ? 0 : BlastCasterRules.Updates(kind) - 1;
        if (fading) { Projectile.friendly = false; Projectile.velocity = Vector2.Zero; Projectile.timeLeft = Math.Min(7, Projectile.timeLeft); }
    }
    private float FlightRange => BlastCasterRules.FlightRange(kind, (int)Projectile.ai[0]) + (redirected ? 320 : 0);
    private float WidthScale => BlastCasterRules.ShotSize(kind, (int)Projectile.ai[0]);
    private void ApplySize()
    {
        Vector2 center = Projectile.Center;
        Projectile.width = Projectile.height = (int)MathF.Ceiling(BlastCasterRules.FluxContactRadius * 2 * WidthScale);
        Projectile.Center = center;
        Projectile.tileCollide = kind == BlastCasterKind.Ifrit && Projectile.ai[0] == 3 && !fading;
    }
    public override bool? CanDamage() => fading ? false : null;
    public override bool ShouldUpdatePosition() => !fading;
    private void Fade()
    {
        if (fading) return;
        fading = true; Projectile.friendly = false; Projectile.extraUpdates = 0;
        opticalBeam?.Stop();
        Projectile.tileCollide = false;
        Projectile.numUpdates = -1; Projectile.timeLeft = 7; Projectile.velocity = Vector2.Zero;
        if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
    }
    public override void AI()
    {
        clock += 1f / Projectile.MaxUpdates;
        if (fading) { opticalBeam?.Stop();Projectile.extraUpdates = 0; Projectile.numUpdates = -1; return; }
        if (!float.IsFinite(Projectile.velocity.X) || !float.IsFinite(Projectile.velocity.Y)) { Projectile.Kill(); return; }
        if (distance >= FlightRange || clock>BlastCasterRules.FlightTicks(kind,(int)Projectile.ai[0],redirected)+1f/Projectile.MaxUpdates) { Fade(); return; }
        if(!frameReady)
        {
            bornAt=Projectile.Center;bornOwner=Main.player[Projectile.owner].MountedCenter;
            bornAxis=Projectile.velocity.SafeNormalize(Vector2.UnitX);frameReady=true;
        }
        BlastCasterShotFrame frame=redirected?BlastCasterProjection.Relay(Projectile.owner,relayIdentity,relayOrigin,relayAxis)
            :BlastCasterProjection.Nozzle(Projectile.owner,kind,(int)Projectile.ai[0],bornAt,bornOwner,bornAxis);
        Projectile.Center=frame.Position(new Vector2(distance-(redirected?relayTravel:0),0));
        Projectile.velocity=frame.Axis*BlastCasterRules.OpticalStep;
        if(!visualStarted && !Main.dedServ){visualStarted=true;beamOrigin=Vector2.Zero;StartBeam();}
        if (kind == BlastCasterKind.Aphrissa && !redirected && relayIdentity >= 0)
        {
            Vector2 relayAt = new(Projectile.ai[1], Projectile.ai[2]);
            if (Vector2.DistanceSquared(Projectile.Center, relayAt) <= 22 * 22)
            {
                Projectile relay = AphrissaRelay.Find(Main.player[Projectile.owner]);
                if (relay != null && relay.identity == relayIdentity && Vector2.DistanceSquared(relay.Center, relayAt) < 16)
                {
                    redirected=true;
                    Projectile.velocity = (target - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * BlastCasterRules.OpticalStep;
                    relayTravel=distance;relayOrigin=Projectile.Center;relayAxis=Projectile.velocity.SafeNormalize(Vector2.UnitX);
                    opticalBeam?.Stop();beamOrigin=Vector2.Zero;
                    if(!Main.dedServ)StartBeam();
                    if (Projectile.ai[0] == 3) Projectile.damage = (int)(Projectile.damage * 1.25f);
                    BlastCasterVisuals.RelayFlash(relayAt, Projectile.velocity.SafeNormalize(Vector2.UnitX));
                }
                else relayIdentity=-1;
                if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
            }
        }
        // The finite-speed leading edge remains the damage carrier; the source
        // beam decorates the travelled path and never performs a separate hit.
        float remaining=Math.Max(0,FlightRange-distance);
        if(Projectile.velocity.Length()>remaining)
            Projectile.velocity=Projectile.velocity.SafeNormalize(Vector2.UnitX)*remaining;
        distance += Projectile.velocity.Length();
        opticalBeam?.Advance(new Vector2(distance-(redirected?relayTravel:0),0));
        if (Projectile.numUpdates == -1)
        {
            Color tint = BlastCasterRules.Tint(kind);
            Lighting.AddLight(Projectile.Center, tint.ToVector3() * .32f);
        }
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (kind == BlastCasterKind.Ifrit && Projectile.ai[0] == 3
            && !Collision.CanHitLine(Projectile.Center, 1, 1,
                targetHitbox.TopLeft(), targetHitbox.Width, targetHitbox.Height)) return false;
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
        // Impact VFX (including the HDR flare) are archived in BlastCasterVisuals.Hit.
        // Damage, debuffs and skill heat remain independent of the optical beam.
        if (Projectile.owner != Main.myPlayer || kind != BlastCasterKind.Ifrit) return;
        if (mode == 2 && !exploded)
        {
            exploded = true;
            SpawnHeat(point, Math.Max(1, Projectile.damage / 2), 0);
        }
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (kind != BlastCasterKind.Ifrit || Projectile.ai[0] != 3) return true;
        Vector2 normal;
        if (Projectile.velocity.X != oldVelocity.X && (Projectile.velocity.Y == oldVelocity.Y
            || Math.Abs(oldVelocity.X) >= Math.Abs(oldVelocity.Y)))
            normal = new Vector2(-Math.Sign(oldVelocity.X), 0);
        else normal = new Vector2(0, -Math.Sign(oldVelocity.Y));
        Vector2 contact = Projectile.Center - normal * (Projectile.width * .5f);
        IfritSurfaceFlame.Attach(Projectile, contact, normal);
        Fade();
        return false;
    }
    private void SpawnHeat(Vector2 point, int damage, int mode)
    {
        Projectile heat = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), point, Vector2.Zero,
            ModContent.ProjectileType<BlastCasterHeat>(), damage, mode == 0 ? 2 : 0, Projectile.owner, mode);
        heat.CritChance = Projectile.CritChance;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        // Previous stamped flame material is retained in BlastCasterFluxTrail,
        // but normal attacks exclusively draw Esperon's imported beam graph.
        return false;
    }
    private void StartBeam()
    {
        var style=BlastCasterProjection.Style(Projectile.owner,kind,(int)Projectile.ai[0],WidthScale,
            redirected?relayOrigin:bornAt,bornOwner,redirected?relayAxis:bornAxis,redirected?relayIdentity:-1);
        opticalBeam=PhoenixBeamPulse.Acquire(Projectile.owner,(int)kind*8+(int)Projectile.ai[0]*2+(redirected?1:0),beamOrigin,
            Vector2.UnitX,FlightRange,style);
    }
    public override void OnKill(int timeLeft)=>opticalBeam?.Stop();
}
