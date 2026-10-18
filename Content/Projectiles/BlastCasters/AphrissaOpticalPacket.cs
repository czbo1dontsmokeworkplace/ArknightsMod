using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.BlastCasters;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.BlastCasters;

// A finite, very fast flying packet. Collision happens on every 12px substep.
// The material follows positions actually visited; there is no hitscan or beam hitbox.
public sealed class AphrissaOpticalPacket : ModProjectile
{
    private Vector2[] history;
    private int count, steps, relayIdentity = -1;
    private float distance, phase;
    private bool redirected, fading;
    private Vector2 target;
    internal void Configure(Vector2 aimTarget, int relay, float spectralPhase)
    { target = aimTarget; relayIdentity = relay; phase = spectralPhase; Projectile.netUpdate = true; }
    public override string Texture => "ArknightsMod/Common/Particle/DefaultParticle";
    public override void OnSpawn(IEntitySource source)
    {
        history = new Vector2[BlastCasterRules.OpticalHistory];
        Projectile.GetGlobalProjectile<global::ArknightsMod.Systems.Gameplay.Damage.ArtsProjectileMarker>().IsArtsDamage = true;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 14; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged; Projectile.ignoreWater = true;
        Projectile.tileCollide = false; Projectile.penetrate = Projectile.maxPenetrate = -1;
        Projectile.extraUpdates = BlastCasterRules.OpticalUpdates - 1;
        Projectile.timeLeft = 240;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void SendExtraAI(BinaryWriter writer)
    { writer.WriteVector2(target); writer.Write(relayIdentity); writer.Write(phase); writer.Write(redirected); writer.Write(fading); }
    public override void ReceiveExtraAI(BinaryReader reader)
    { target = reader.ReadVector2(); relayIdentity = reader.ReadInt32(); phase = reader.ReadSingle(); redirected = reader.ReadBoolean(); fading = reader.ReadBoolean(); }
    private float FlightRange => BlastCasterRules.Range((int)Projectile.ai[0]) + (redirected ? 320 : 0);
    public override bool? CanDamage() => fading ? false : null;
    public override bool ShouldUpdatePosition() => !fading;
    private void Fade()
    {
        if (fading) return;
        fading = true; Projectile.friendly = false; Projectile.tileCollide = false;
        Projectile.extraUpdates = 0; Projectile.numUpdates = -1;
        Projectile.timeLeft = 5; Projectile.velocity = Vector2.Zero;
        if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
    }
    public override void AI()
    {
        if (fading) { Projectile.extraUpdates = 0; Projectile.numUpdates = -1; return; }
        steps++;
        distance += Projectile.velocity.Length();
        if (distance >= FlightRange || !float.IsFinite(distance))
        { Fade(); return; }
        if (!redirected && relayIdentity >= 0)
        {
            Vector2 relayAt = new(Projectile.ai[1], Projectile.ai[2]);
            if (Vector2.DistanceSquared(Projectile.Center, relayAt) <= 22 * 22)
            {
                Projectile relay = AphrissaRelay.Find(Main.player[Projectile.owner]);
                redirected = true;
                if (relay != null && relay.identity == relayIdentity && Vector2.DistanceSquared(relay.Center, relayAt) < 16)
                {
                    // No teleport: keep this packet at its reached position and turn its velocity.
                    Projectile.velocity = (target - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * BlastCasterRules.OpticalStep;
                    if (Projectile.ai[0] == 3) Projectile.damage = (int)(Projectile.damage * 1.25f);
                    BlastCasterVisuals.RelayFlash(relayAt, Projectile.velocity.SafeNormalize(Vector2.UnitX));
                }
                if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
            }
        }
        for (int i = Math.Min(count, history.Length - 1); i > 0; i--) history[i] = history[i - 1];
        history[0] = Projectile.Center; count = Math.Min(count + 1, history.Length);
        if (steps % 8 == 0) BlastCasterVisuals.OpticalWake(Projectile.Center, Projectile.velocity, phase + distance * .0004f);
        if (Projectile.numUpdates == 0) Lighting.AddLight(Projectile.Center, .10f, .28f, .24f);
    }
    public override void ModifyDamageHitbox(ref Rectangle hitbox)
    { if (Projectile.ai[0] == 2) hitbox.Inflate(8, 8); }
    public override void OnHitNPC(NPC targetNpc, NPC.HitInfo hit, int damageDone)
    {
        targetNpc.AddBuff(BuffID.OnFire3, 300);
        if (Projectile.ai[0] == 3) targetNpc.AddBuff(BuffID.Ichor, 180);
        Vector2 contact = Vector2.Clamp(Projectile.Center, targetNpc.position, targetNpc.position + targetNpc.Size);
        BlastCasterVisuals.Hit(Projectile, targetNpc, contact, BlastCasterKind.Aphrissa);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        OpticalPacketTrail.Draw(history, count, phase, Projectile.ai[0] == 2 ? 22f : 14f,
            fading ? Projectile.timeLeft / 5f : Math.Clamp((FlightRange - distance) / 80f, .2f, 1));
        return false;
    }
    public override void OnKill(int timeLeft)
    { if (count > 0) BlastCasterVisuals.PacketEnd(Projectile.Center, Projectile.velocity); }
}
