using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistHazard : ModProjectile
{
    internal int Age, Delay, Duration = 240, Target, Shooter = -1;
    internal float Sweep;
    internal float Length = MaterialistRules.BeamLength;
    internal FireStyle Style;
    internal float Acceleration = 1, Bend;
    internal Vector2 Anchor;
    internal bool VerticalSlide, DenseBallistics;
    private float beamSlideSpeed;
    private float terrainLength;
    internal bool IsBeam => Kind is DroneShot.Beam or DroneShot.ChaseBeam or DroneShot.PincerBeam;
    internal bool Moving => Kind is DroneShot.Bolt or DroneShot.Missile or DroneShot.Prism or DroneShot.Seeker or DroneShot.ArcSpark or DroneShot.Petal or DroneShot.Lattice or DroneShot.CoreLance or DroneShot.ClusterOrb or DroneShot.OrbitSpark or DroneShot.Needle or DroneShot.RailSlug or DroneShot.MachineRound or DroneShot.GravityOrb or DroneShot.DropMissile or DroneShot.CommandRocket or DroneShot.CannonShell or DroneShot.ScoutPulse or DroneShot.ArtsOrb or DroneShot.SaucerRound or DroneShot.RaptorRocket;
    internal float Reach => IsBeam ? terrainLength : Length;
    internal float Width => Kind == DroneShot.PincerBeam ? 24 : Style == FireStyle.Containment && IsBeam ? 30 : Kind == DroneShot.ChaseBeam ? 14 : Style == FireStyle.Precision ? 10 : MaterialistRules.BeamWidth;
    internal DroneShot Kind => (DroneShot)(int)Projectile.ai[2];
    internal MaterialistAntagonizer Boss => MaterialistAntagonizer.Owner((int)Projectile.ai[0] - 1, (int)Projectile.ai[1]);
    private bool firedVisual;
    private readonly bool[] hitPlayers = new bool[Main.maxPlayers];
    internal bool BelongsTo(MaterialistAntagonizer boss) => Projectile.ai[0] == boss.NPC.whoAmI + 1 && Projectile.ai[1] == boss.Encounter;
    public override string Texture => MaterialistAntagonizer.AssetRoot + "MaterialistAntagonizer";
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 22;
        ProjectileID.Sets.TrailingMode[Type] = 0;
        // A terrain-limited laser may cross the screen while its projection node is far away.
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 150000;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12;
        Projectile.hostile = true; Projectile.friendly = false;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.timeLeft = 720;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }
    public override bool ShouldUpdatePosition() => Retiring ? Moving : Age >= Delay && (Moving || Kind == DroneShot.Beacon && Age < 45);
    internal Vector2 ScanSize => new(Math.Max(1, Length), Math.Max(1, Sweep));
    internal float MineRadius => Kind == DroneShot.TrailBomb ? 62 : 48;
    internal static Vector2 Guide(Vector2 velocity, Vector2 delta, int flightAge)
    {
        if (flightAge < 18 || flightAge >= 72) return velocity;
        float turn = MathHelper.Clamp(MathHelper.WrapAngle(delta.ToRotation() - velocity.ToRotation()), -.009f, .009f);
        return velocity.RotatedBy(turn).SafeNormalize(Vector2.UnitY) * 3.8f;
    }
    internal Vector2 BeamDirection
    {
        get
        {
            if (Kind == DroneShot.PincerBeam)
            {
                float close = MaterialistFormation.Smooth((Age - Delay) / (float)Math.Max(1, MaterialistAntagonizer.PincerSweepTicks));
                float half = MathHelper.Lerp(.80f, PincerFinalHalfAngle, close);
                return Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(-Bend * half);
            }
            float t = MathHelper.Clamp((Age - Delay) / (float)Math.Max(1, Duration), 0, 1);
            return Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy((t - .5f) * Sweep);
        }
    }
    internal float BeamStrength => MathHelper.Clamp((Age - Delay) / 7f, 0, 1) * MathHelper.Clamp((Delay + Duration - Age) / 8f, 0, 1);
    private bool ShooterValid => Shooter < 0 || Shooter < Main.maxNPCs && Main.npc[Shooter].active &&
        Main.npc[Shooter].TryGetGlobalNPC<MaterialistWing>(out var wing) && Boss != null && wing.BelongsTo(Boss) && !wing.Retreating;
    public override void AI()
    {
        if (Kind == DroneShot.MachineRound)
        {
            Projectile.extraUpdates = MaterialistRules.MachineUpdates - 1;
            if (!Retiring && !Main.dedServ && Projectile.numUpdates % (MaterialistVisuals.Reduced ? 8 : 2) == 0)
                MaterialistBallistics.Wake(Projectile, DenseBallistics);
            // Physics/collision execute all 16 substeps; ages, sounds and retirement advance once per game tick.
            if (Projectile.numUpdates != 0) return;
        }
        if (Retiring) { UpdateRetirement(); return; }
        var boss = Boss;
        if (boss == null || boss.Order is FlightOrder.Transition or FlightOrder.Death or FlightOrder.Recover || IsBeam && !ShooterValid)
        {
            // Wait for the owner packet on clients; authority performs all gameplay removal.
            if (MaterialistAntagonizer.Authority) Retire();
            return;
        }
        Age++;
        if (RetirementDue)
        {
            Retire();
            UpdateRetirement();
            return;
        }
        if (Kind == DroneShot.MachineRound)
        {
            // Reapply on clients as well: hitbox/terrain flags are not part of our extra-AI packet.
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            if (Projectile.width != 6)
            {
                Vector2 center = Projectile.Center;
                Projectile.width = Projectile.height = 6;
                Projectile.Center = center;
            }
        }
        if (Kind == DroneShot.SaucerRound)
        {
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
        }
        int flight = Age - Delay;
        if (!UpdatePayload(boss, flight)) return;
        if (!UpdateArtillery(boss, flight)) return;
        UpdateEscortAmmo(flight);
        if (flight >= 0 && Moving && Acceleration != 1) Projectile.velocity *= Acceleration;
        if (flight >= 0 && Kind == DroneShot.ArcSpark && flight < 52) Projectile.velocity = Projectile.velocity.RotatedBy(Bend);
        if (flight >= 0 && Kind == DroneShot.Petal) Projectile.velocity = Projectile.velocity.RotatedBy(MathF.Sin(flight * .115f) * Bend);
        if (Kind == DroneShot.OrbitSpark && Age < Delay)
            Projectile.Center = Anchor + Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(Age * .115f) * (MathF.Sin(Age / (float)Math.Max(1, Delay) * MathHelper.Pi) * 42);
        if (Kind == DroneShot.ClusterOrb && flight == 44 && MaterialistAntagonizer.Authority)
        {
            // Burst in the original flight direction, never into a newly sampled player position.
            float axis = Projectile.velocity.ToRotation();
            for (int i = -2; i <= 2; i++)
                boss.Shoot(DroneShot.Needle, Projectile.Center, (axis + i * .24f * MaterialistRules.SpreadSpacing).ToRotationVector2() * 9,
                    duration: 78, style: Style);
            Projectile.Kill();
            return;
        }
        if (Kind == DroneShot.Seeker && Age >= Delay && Target >= 0 && Target < Main.maxPlayers && Main.player[Target].active && !Main.player[Target].dead)
        {
            Projectile.velocity = Guide(Projectile.velocity, Main.player[Target].Center - Projectile.Center, Age - Delay);
            if (MaterialistAntagonizer.Authority && Age % 20 == 0) Projectile.netUpdate = true;
        }
        // Final-phase A2 nodes slide horizontally after their fixed vertical warning.
        // Other phases retain stationary nodes, independent of escort formation movement.
        if (VerticalSlide)
        {
            Projectile.velocity = Vector2.UnitY;
            Sweep = 0;
            if (flight >= 0)
            {
                float desired = Target >= 0 && Target < Main.maxPlayers && Main.player[Target].active && !Main.player[Target].dead
                    ? MathHelper.Clamp((Main.player[Target].Center.X - Anchor.X) * .012f, -1.6f, 1.6f) : 0;
                beamSlideSpeed += MathHelper.Clamp(desired - beamSlideSpeed, -.04f, .04f);
                Anchor.X = MathHelper.Clamp(Anchor.X + beamSlideSpeed, 32, Main.maxTilesX * 16 - 32);
                if (MaterialistAntagonizer.Authority && Age % 15 == 0) Projectile.netUpdate = true;
            }
        }
        if (IsBeam) Projectile.Center = Anchor;
        if (Kind == DroneShot.PincerBeam) UpdatePincerAim();
        if (Kind == DroneShot.ChaseBeam && !VerticalSlide)
        {
            if (flight >= 0 && Target >= 0 && Target < Main.maxPlayers && Main.player[Target].active && !Main.player[Target].dead)
            {
                float desired = (Main.player[Target].Center - Projectile.Center).ToRotation();
                float turn = MathHelper.Clamp(MathHelper.WrapAngle(desired - Projectile.velocity.ToRotation()), -.0021f, .0021f);
                Projectile.velocity = Projectile.velocity.RotatedBy(turn).SafeNormalize(Vector2.UnitY);
                if (MaterialistAntagonizer.Authority && Age % 15 == 0) Projectile.netUpdate = true;
            }
        }
        if (IsBeam) terrainLength = TraceBeam(Projectile.Center, BeamDirection, Width);
        if (Kind == DroneShot.Missile && Age < 65 && Target >= 0 && Target < Main.maxPlayers && Main.player[Target].active && !Main.player[Target].dead)
        {
            Vector2 desired = (Main.player[Target].Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 8;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, .026f);
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (!Main.dedServ)
        {
            Color accent = MaterialistVisuals.AmmoColor(this);
            if (Kind == DroneShot.ClusterOrb && flight == 43)
            {
                MaterialistVisuals.Burst(Projectile.Center, accent, 1);
                MaterialistEffects.Pulse(Projectile.Center, 60, accent, .4f);
            }
            if (Age == 1 && Kind == DroneShot.MachineRound)
            {
                MaterialistEffects.MuzzleShock(Anchor, Projectile.velocity.ToRotation(), accent, .85f, Shooter);
                MaterialistBallistics.Muzzle(Anchor, Projectile.velocity, Shooter);
            }
            if (Age == 1 && Kind != DroneShot.MachineRound && (Moving || IsBeam || Kind == DroneShot.Beacon))
                MaterialistEffects.MuzzleShock(Kind is DroneShot.DropMissile or DroneShot.CannonShell ? Projectile.Center : Anchor, Projectile.velocity.ToRotation(), accent, Style is FireStyle.CoreSpread or FireStyle.CoreFocus ? 1.25f : .9f, Shooter < 0 ? (int)Projectile.ai[0] - 1 : Shooter);
            if (Kind is DroneShot.Missile or DroneShot.Bolt or DroneShot.Prism or DroneShot.Seeker && Age >= Delay && Age % 5 == 0)
                MaterialistVisuals.Nano(Projectile.Center, -Projectile.velocity * .12f, accent, .65f, 18);
            Lighting.AddLight(Projectile.Center, accent.ToVector3() * (Kind == DroneShot.ArtsOrb ? .196f : .28f));
            if (!firedVisual && Age >= Delay)
            {
                firedVisual = true;
                if (Delay > 0 && (Moving || IsBeam))
                    MaterialistEffects.MuzzleShock(Kind is DroneShot.OrbitSpark or DroneShot.DropMissile ? Projectile.Center : Anchor, Projectile.velocity.ToRotation(), accent,
                        Style is FireStyle.CoreSpread or FireStyle.CoreFocus ? 1.4f : 1.1f, Shooter < 0 ? (int)Projectile.ai[0] - 1 : Shooter);
                if (Kind is DroneShot.Bombardment or DroneShot.FluxMine or DroneShot.TrailBomb or DroneShot.ScanBurst or DroneShot.ArtilleryBlast || IsBeam)
                {
                    bool mine = Kind is DroneShot.FluxMine or DroneShot.TrailBomb;
                    MaterialistVisuals.Burst(Projectile.Center, accent, IsBeam ? 1 : 1.4f);
                    if (!IsBeam || Shooter >= 0)
                        MaterialistEffects.Pulse(Projectile.Center, mine ? MineRadius : IsBeam ? 100 : Kind == DroneShot.ArtilleryBlast ? Length : 180, accent, IsBeam ? 1 : .7f);
                    SoundEngine.PlaySound((Kind == DroneShot.PincerBeam ? SoundID.Zombie104 : IsBeam ? SoundID.Item158 : SoundID.Item14)
                        with { Volume = IsBeam ? .5f : .65f, MaxInstances = 3 }, Projectile.Center);
                }
                else if (Kind is DroneShot.CommandRocket or DroneShot.RaptorRocket or DroneShot.DropMissile or DroneShot.Missile or DroneShot.CannonShell)
                    SoundEngine.PlaySound((Kind == DroneShot.CannonShell ? SoundID.Item61 : SoundID.Item39) with { Volume = .5f, Pitch = -.2f, MaxInstances = 3 }, Projectile.Center);
                else if (Kind == DroneShot.SaucerRound) PlaySaucerSound();
                else if (Kind == DroneShot.MachineRound)
                    SoundEngine.PlaySound(SoundID.Item40 with { Volume = .45f, Pitch = -.25f, PitchVariance = .08f, MaxInstances = 4 }, Anchor);
                else SoundEngine.PlaySound(SoundID.Item33 with { Volume = .28f, Pitch = Kind is DroneShot.CoreLance or DroneShot.ClusterOrb ? -.15f : .35f, MaxInstances = 3 }, Projectile.Center);
            }
        }
        if (Age >= Delay + Duration) Retire();
    }
    public override bool? CanDamage()
    {
        var boss = Boss;
        if (HarmlessRetirement || boss == null || boss.Paused || Age < Delay || Age >= Delay + Duration) return false;
        if (Kind == DroneShot.Beacon) return false;
        if (Kind is DroneShot.CommandRocket or DroneShot.RaptorRocket && Age - Delay < 28) return false;
        if (Kind == DroneShot.CannonShell && Age - Delay < 12) return false;
        if (Kind == DroneShot.ArtilleryBlast && Age >= Delay + 10) return false;
        if (IsBeam && (!ShooterValid || BeamStrength < .3f || Reach < 1)) return false;
        if (Kind == DroneShot.Bombardment && Age >= Delay + 12) return false;
        if (Kind is DroneShot.FluxMine or DroneShot.TrailBomb && Age >= Delay + 18) return false;
        if (Kind == DroneShot.ScanBurst && Age >= Delay + 12) return false;
        return null;
    }
    public override bool CanHitPlayer(Player target) => !HarmlessRetirement && !hitPlayers[target.whoAmI];
    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        hitPlayers[target.whoAmI] = true;
        if (Kind == DroneShot.SaucerRound) SaucerImpact();
        if (Kind == DroneShot.MachineRound) MaterialistBallistics.Impact(Projectile.Center, -Projectile.velocity.SafeNormalize(Vector2.UnitY), false, true);
        if (Kind is DroneShot.ScoutPulse or DroneShot.ArtsOrb && !Main.dedServ)
            MaterialistEffects.Pulse(Projectile.Center, Kind == DroneShot.ArtsOrb ? 36 : 22, MaterialistVisuals.AmmoColor(this));
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (Kind == DroneShot.CannonShell)
        {
            // An early terrain impact did not occur at the announced landing circle: show a short fuse there.
            if (!Main.dedServ) MaterialistBallistics.Impact(Projectile.Center, -oldVelocity.SafeNormalize(Vector2.UnitY), true);
            DetonateShell(Boss, 18);
            return false;
        }
        if (Kind == DroneShot.MachineRound)
        {
            Vector2 normal = Vector2.Zero;
            if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > .01f) normal.X = -Math.Sign(oldVelocity.X);
            if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > .01f) normal.Y = -Math.Sign(oldVelocity.Y);
            MaterialistBallistics.Impact(Projectile.Center + oldVelocity.SafeNormalize(Vector2.UnitY) * 3,
                normal.SafeNormalize(-oldVelocity.SafeNormalize(Vector2.UnitY)), true, true);
        }
        if (Kind is DroneShot.GravityOrb or DroneShot.DropMissile && !Main.dedServ)
        {
            MaterialistVisuals.Burst(Projectile.Center, MaterialistVisuals.AmmoColor(this), Kind == DroneShot.DropMissile ? 1.25f : .75f);
            MaterialistEffects.Pulse(Projectile.Center, Kind == DroneShot.DropMissile ? 78 : 44, MaterialistVisuals.AmmoColor(this), .6f);
            if (Kind == DroneShot.DropMissile)
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = .55f, MaxInstances = 3 }, Projectile.Center);
        }
        if (Kind == DroneShot.SaucerRound) SaucerImpact();
        return true;
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (HarmlessRetirement) return false;
        if (Kind == DroneShot.ScanBurst)
        {
            Vector2 size = ScanSize;
            return new Rectangle((int)(Projectile.Center.X - size.X * .5f), (int)(Projectile.Center.Y - size.Y * .5f),
                (int)size.X, (int)size.Y).Intersects(targetHitbox);
        }
        if (Kind is DroneShot.Bombardment or DroneShot.FluxMine or DroneShot.TrailBomb or DroneShot.ArtilleryBlast)
        {
            Vector2 closest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            float radius = Kind == DroneShot.Bombardment ? MaterialistRules.BombRadius : Kind == DroneShot.ArtilleryBlast ? Length : MineRadius;
            return Vector2.DistanceSquared(closest, Projectile.Center) <= radius * radius;
        }
        if (Moving)
        {
            float distance = 0;
            Vector2 previous = Kind == DroneShot.MachineRound ? Projectile.Center - Projectile.velocity :
                Projectile.oldPos.Length > 0 && Projectile.oldPos[0] != Vector2.Zero ? Projectile.oldPos[0] + Projectile.Size * .5f : Projectile.Center - Projectile.velocity;
            if (Kind is DroneShot.ScoutPulse or DroneShot.ArtsOrb)
            {
                // Visual light and long tails never enlarge the solid head's collision.
                Vector2 closest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
                float radius = Kind == DroneShot.ArtsOrb ? 13 : 6;
                return Vector2.DistanceSquared(closest, Projectile.Center) <= radius * radius;
            }
            return projHitbox.Intersects(targetHitbox) || Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), previous,
                Projectile.Center, Kind == DroneShot.SaucerRound ? 8 : Kind is DroneShot.RailSlug or DroneShot.MachineRound ? 6 : 10, ref distance);
        }
        if (IsBeam)
        {
            float distance = 0;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
                Projectile.Center + BeamDirection * Reach, Width * BeamStrength, ref distance);
        }
        return null;
    }
    public override void SendExtraAI(BinaryWriter w)
    { w.Write(Age); w.Write(Delay); w.Write(Duration); w.Write(Target); w.Write(Shooter); w.Write(Sweep); w.Write(Length); w.Write((int)Style); w.Write(Acceleration); w.Write(Bend); w.WriteVector2(Anchor); w.Write(RetirementAge); w.WriteVector2(LaunchVelocity); w.Write(RocketCommitted); w.Write(terrainLength); w.Write(VerticalSlide); w.Write(DenseBallistics); w.Write(beamSlideSpeed); }
    public override void ReceiveExtraAI(BinaryReader r)
    {
        Age = r.ReadInt32(); Delay = r.ReadInt32(); Duration = r.ReadInt32(); Target = r.ReadInt32(); Shooter = r.ReadInt32(); Sweep = r.ReadSingle(); Length = r.ReadSingle(); Style = (FireStyle)r.ReadInt32(); Acceleration = r.ReadSingle(); Bend = r.ReadSingle(); Anchor = r.ReadVector2();
        RetirementAge = r.ReadInt32();
        LaunchVelocity = r.ReadVector2(); RocketCommitted = r.ReadBoolean();
        terrainLength = r.ReadSingle();
        VerticalSlide = r.ReadBoolean(); DenseBallistics = r.ReadBoolean(); beamSlideSpeed = r.ReadSingle();
        if (Retiring) { Projectile.hostile = Projectile.friendly = false; Projectile.damage = 0; Projectile.tileCollide = false; }
        // The synced position is already the server's six-pixel hitbox top-left.
        if (Kind == DroneShot.MachineRound)
        {
            Projectile.width = Projectile.height = 6;
            Projectile.extraUpdates = MaterialistRules.MachineUpdates - 1;
        }
        if (Kind == DroneShot.ArtsOrb) Projectile.width = Projectile.height = 26;
        if (Kind == DroneShot.SaucerRound) Projectile.width = Projectile.height = 8;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        // Only beams need the world-sized engine draw allowance set above.
        if (!IsBeam && !Projectile.Hitbox.Intersects(new Rectangle((int)Main.screenPosition.X - 1600,
            (int)Main.screenPosition.Y - 1600, Main.screenWidth + 3200, Main.screenHeight + 3200))) return false;
        if (Boss != null || Retiring) MaterialistVisuals.DrawHazard(this);
        return false;
    }
}
