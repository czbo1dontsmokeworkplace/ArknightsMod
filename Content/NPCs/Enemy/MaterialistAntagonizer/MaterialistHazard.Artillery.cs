using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistHazard
{
    internal Vector2 LaunchVelocity;
    internal bool RocketCommitted;
    internal int ShellFlight => Math.Max(1, (int)Sweep);
    internal Vector2 ShellOrigin => Anchor - LaunchVelocity * ShellFlight - Vector2.UnitY * (Bend * ShellFlight * (ShellFlight + 1) * .5f);
    internal Vector2 ShellPoint(float step) => ShellOrigin + LaunchVelocity * step + Vector2.UnitY * (Bend * step * (step + 1) * .5f);

    private bool UpdateArtillery(MaterialistAntagonizer boss, int flight)
    {
        if (Kind == DroneShot.CommandRocket && flight >= 0)
        {
            Projectile.penetrate = 1;
            Projectile.tileCollide = flight >= 20;
            if (flight < 20)
                Projectile.velocity *= .985f;
            else
            {
                bool valid = Target >= 0 && Target < Main.maxPlayers && Main.player[Target].active && !Main.player[Target].dead;
                if (MaterialistAntagonizer.Authority && !RocketCommitted &&
                    (flight >= 104 || !valid || Vector2.DistanceSquared(Projectile.Center, Main.player[Target].Center) < 140 * 140))
                {
                    // A close pass commits once: no U-turn back into a player who already dodged.
                    RocketCommitted = true;
                    Projectile.netUpdate = true;
                }
                float speed = MathHelper.Lerp(5.2f, 11.6f + boss.Phase * .45f, MathHelper.Clamp((flight - 20) / 62f, 0, 1));
                if (!RocketCommitted && flight < 104 && valid)
                {
                    Player player = Main.player[Target];
                    Vector2 aim = player.Center + Vector2.Clamp(player.velocity * 5, new Vector2(-70), new Vector2(70));
                    float turn = MathHelper.Clamp(MathHelper.WrapAngle((aim - Projectile.Center).ToRotation() - Projectile.velocity.ToRotation()), -.062f, .062f);
                    Projectile.velocity = Projectile.velocity.RotatedBy(turn);
                    if (MaterialistAntagonizer.Authority && flight % 15 == 0) Projectile.netUpdate = true;
                }
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * speed;
            }
            if (!Main.dedServ && flight == 20)
            {
                MaterialistEffects.MuzzleShock(Projectile.Center, Projectile.velocity.ToRotation(), MaterialistVisuals.Cyan, .7f, -1);
                SoundEngine.PlaySound(SoundID.Item62 with { Volume = .45f, Pitch = .1f, MaxInstances = 3 }, Projectile.Center);
            }
            if (!Main.dedServ && flight >= 20 && flight % (MaterialistVisuals.Reduced ? 8 : 3) == 0)
            {
                Vector2 rear = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 13;
                MaterialistVisuals.Nano(rear, -Projectile.velocity * .12f, MaterialistVisuals.Cyan, .7f, 24);
            }
        }
        if (Kind == DroneShot.CannonShell && flight >= 0)
        {
            Projectile.penetrate = -1;
            // Permit a clean upward exit from the launcher, then let real terrain stop the shell.
            Projectile.tileCollide = flight >= 12;
            if (flight > ShellFlight)
            {
                DetonateShell(boss, 0);
                return false;
            }
            Projectile.velocity.Y += Bend;
        }
        return true;
    }
    private void DetonateShell(MaterialistAntagonizer boss, int warning)
    {
        if (Retiring || !Projectile.active) return;
        if (MaterialistAntagonizer.Authority)
            boss?.Shoot(DroneShot.ArtilleryBlast, Projectile.Center, Vector2.Zero, warning, 22,
                length: Length, style: FireStyle.CoreFocus);
        Projectile.Kill();
    }
    public override void OnKill(int timeLeft)
    {
        if (Retiring || Main.dedServ || Kind != DroneShot.CommandRocket) return;
        // Rocket impact is a visual pop, not an unannounced second area-damage hitbox.
        MaterialistVisuals.Burst(Projectile.Center, MaterialistVisuals.Amber, .9f);
        MaterialistEffects.Pulse(Projectile.Center, 58, MaterialistVisuals.Cyan, .5f);
        SoundEngine.PlaySound(SoundID.Item14 with { Volume = .4f, Pitch = .3f, MaxInstances = 3 }, Projectile.Center);
    }
}
