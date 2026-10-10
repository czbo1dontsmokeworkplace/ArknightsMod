using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed class MaterialistHazard : ModProjectile
{
    internal int Age, Delay, Duration = 240, Target, Shooter = -1;
    internal float Sweep;
    internal DroneShot Kind => (DroneShot)(int)Projectile.ai[2];
    internal MaterialistAntagonizer Boss => MaterialistAntagonizer.Owner((int)Projectile.ai[0] - 1, (int)Projectile.ai[1]);
    private bool firedVisual;
    private readonly bool[] hitPlayers = new bool[Main.maxPlayers];
    internal bool BelongsTo(MaterialistAntagonizer boss) => Projectile.ai[0] == boss.NPC.whoAmI + 1 && Projectile.ai[1] == boss.Encounter;
    public override string Texture => MaterialistAntagonizer.AssetRoot + "MaterialistAntagonizer";
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 10;
        ProjectileID.Sets.TrailingMode[Type] = 0;
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1600;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12;
        Projectile.hostile = true; Projectile.friendly = false;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.timeLeft = 720;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }
    public override bool ShouldUpdatePosition() => Kind is DroneShot.Bolt or DroneShot.Missile;
    internal Vector2 BeamDirection
    {
        get
        {
            float t = MathHelper.Clamp((Age - Delay) / (float)Math.Max(1, Duration), 0, 1);
            return Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy((t - .5f) * Sweep);
        }
    }
    internal float BeamStrength => MathHelper.Clamp((Age - Delay) / 7f, 0, 1) * MathHelper.Clamp((Delay + Duration - Age) / 8f, 0, 1);
    private bool ShooterValid => Shooter < 0 || Shooter < Main.maxNPCs && Main.npc[Shooter].active &&
        Main.npc[Shooter].TryGetGlobalNPC<MaterialistWing>(out var wing) && Boss != null && wing.BelongsTo(Boss) && !wing.Retreating;
    public override void AI()
    {
        var boss = Boss;
        if (boss == null || boss.Order is FlightOrder.Transition or FlightOrder.Death or FlightOrder.Recover || Kind == DroneShot.Beam && !ShooterValid)
        {
            // Wait for the owner packet on clients; authority performs all gameplay removal.
            if (MaterialistAntagonizer.Authority) Projectile.Kill();
            return;
        }
        Age++;
        if (Kind == DroneShot.Beam && Shooter >= 0) Projectile.Center = MaterialistWing.Muzzle(Main.npc[Shooter]);
        if (Kind == DroneShot.Missile && Age < 65 && Target >= 0 && Target < Main.maxPlayers && Main.player[Target].active && !Main.player[Target].dead)
        {
            Vector2 desired = (Main.player[Target].Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 8;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, .026f);
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (!Main.dedServ)
        {
            Lighting.AddLight(Projectile.Center, .25f, .12f, .03f);
            if (!firedVisual && Age >= Delay)
            {
                firedVisual = true;
                if (Kind is DroneShot.Bombardment or DroneShot.Beam)
                {
                    MaterialistVisuals.Burst(Projectile.Center, Kind == DroneShot.Beam ? MaterialistVisuals.Violet : MaterialistVisuals.Amber, Kind == DroneShot.Beam ? .7f : 1.2f);
                    MaterialistEffects.Pulse(Projectile.Center, Kind == DroneShot.Beam ? 100 : 180, MaterialistVisuals.Amber, Kind == DroneShot.Beam ? 1 : 2);
                    SoundEngine.PlaySound((Kind == DroneShot.Beam ? SoundID.Item33 : SoundID.Item14) with { Volume = .55f, MaxInstances = 3 }, Projectile.Center);
                }
                else SoundEngine.PlaySound(SoundID.Item33 with { Volume = .18f, Pitch = .35f, MaxInstances = 3 }, Projectile.Center);
            }
        }
        if (Age >= Delay + Duration) Projectile.Kill();
    }
    public override bool? CanDamage()
    {
        var boss = Boss;
        if (boss == null || boss.Paused || Age < Delay || Age >= Delay + Duration) return false;
        if (Kind == DroneShot.Beam && (!ShooterValid || BeamStrength < .3f)) return false;
        if (Kind == DroneShot.Bombardment && Age >= Delay + 12) return false;
        return null;
    }
    public override bool CanHitPlayer(Player target) => !hitPlayers[target.whoAmI];
    public override void OnHitPlayer(Player target, Player.HurtInfo info) => hitPlayers[target.whoAmI] = true;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (Kind == DroneShot.Bombardment)
        {
            Vector2 closest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            return Vector2.DistanceSquared(closest, Projectile.Center) <= MaterialistRules.BombRadius * MaterialistRules.BombRadius;
        }
        if (Kind == DroneShot.Beam)
        {
            float distance = 0;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
                Projectile.Center + BeamDirection * MaterialistRules.BeamLength, MaterialistRules.BeamWidth * BeamStrength, ref distance);
        }
        return null;
    }
    public override void SendExtraAI(BinaryWriter w)
    { w.Write(Age); w.Write(Delay); w.Write(Duration); w.Write(Target); w.Write(Shooter); w.Write(Sweep); }
    public override void ReceiveExtraAI(BinaryReader r)
    { Age = r.ReadInt32(); Delay = r.ReadInt32(); Duration = r.ReadInt32(); Target = r.ReadInt32(); Shooter = r.ReadInt32(); Sweep = r.ReadSingle(); }
    public override bool PreDraw(ref Color lightColor)
    {
        if (Boss != null) MaterialistVisuals.DrawHazard(this);
        return false;
    }
}
