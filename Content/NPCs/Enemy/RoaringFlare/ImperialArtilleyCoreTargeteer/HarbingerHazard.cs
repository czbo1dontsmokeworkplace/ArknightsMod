using System;
using System.IO;
using ArknightsMod.Content.Projectiles.BossArsenal;
using ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.RoaringFlare.ImperialArtilleyCoreTargeteer;

public sealed class HarbingerHazard : ModProjectile
{
    // 0 locked strike, 1 weak-homing salvo, 2 explosion, 3 mortar, 4 visual meson, 5 rain, 6 ember.
    internal int Kind => (int)Projectile.ai[0];
    internal int Age => (int)Projectile.localAI[0];
    private bool locked;
    private float lockedY;
    private bool retired;
    private PhoenixMotionFx missile;
    private PhoenixBeamFx beam;
    public override string Texture => "Terraria/Images/MagicPixel";
    private NPC Parent => (int)Projectile.ai[2] > 0 && (int)Projectile.ai[2] <= Main.maxNPCs ? Main.npc[(int)Projectile.ai[2] - 1] : null;
    private bool ParentAlive => Parent is { active: true, ModNPC: AACT boss } && boss.Phase < 4;
    public override void SetStaticDefaults() { ProjectileID.Sets.TrailCacheLength[Type] = 16; ProjectileID.Sets.TrailingMode[Type] = 2; }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16; Projectile.timeLeft = 180;
        CooldownSlot = ImmunityCooldownID.Bosses;
        Projectile.hostile = true; Projectile.friendly = false; Projectile.penetrate = -1;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.netImportant = true;
    }
    public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
    {
        Projectile.timeLeft = Kind switch { 0 => 100, 2 => 36, 4 => 20, 6 => 85, _ => 180 };
        if (Kind is 0 or 2 or 4 or 6) Projectile.hostile = Kind == 2;
    }
    public override bool ShouldUpdatePosition() => Kind is 1 or 3 or 5;
    public override bool? CanDamage() => HarbingerRules.Harmful(Kind, Age) && Kind != 0 ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (Kind == 2) return Vector2.DistanceSquared(Projectile.Center, targetHitbox.ClosestPointInRect(Projectile.Center)) <= Projectile.ai[1] * Projectile.ai[1];
        return null;
    }
    public override void AI()
    {
        if (!ParentAlive) { Projectile.Kill(); return; }
        Projectile.localAI[0]++;
        if (Kind == 0) {
            if (Age == 1 && !Main.dedServ) SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Assets/Sound/ImperialArtilleyCoreTargeteer/Alert") with { Volume = .5f }, Projectile.Center);
            if (Age == 78) Explode(Projectile.ai[1]);
        }
        else if (Kind == 1) {
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (!locked && (Projectile.Center.Y >= target.Center.Y - 220 || Age >= 85)) { locked = true; lockedY = target.Center.Y; Projectile.netUpdate = true; }
            if (!locked && !target.dead) Projectile.velocity = HarbingerRules.WeakHoming(Projectile.velocity, target.Center - Projectile.Center);
            float speed = Math.Min(22, Projectile.velocity.Length() + .14f);
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * speed;
            Projectile.velocity.Y = Math.Max(4, Projectile.velocity.Y);
            if (locked && Projectile.Center.Y >= lockedY + 90) Projectile.Kill();
        }
        else if (Kind == 3) {
            Projectile.velocity.Y += .17f;
            if (Age > 105) Projectile.Kill();
        }
        else if (Kind == 5) {
            Projectile.velocity *= 1.008f;
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (Projectile.Center.Y > target.Center.Y + 160) Projectile.Kill();
        }
        else if (Kind == 6 && Age == 60) Explode(48);
        if (!Main.dedServ) {
            if (Kind == 1) {
                var style = PhoenixFx.Original(); style.Opacity = static () => .7f; style.SuppressShake = true;
                missile ??= new PhoenixMotionFx(36, Projectile, style);
                if (Age == 1) missile.Activate("actor.player.weapon.36.activation");
            }
            if (Kind == 2 && Age == 1) SoundEngine.PlaySound(SoundID.Item14 with { Volume = .5f }, Projectile.Center);
            if (Kind == 4) {
                if (beam == null) {
                    var style = PhoenixFx.Colored(new Color(255, 30, 45), new Color(255, 170, 170));
                    style.SuppressShake = true; style.MonochromeTexture = true; style.SoundVolume = .3f;
                    beam = new PhoenixBeamFx("projectile.player.34.1", () => Projectile.Center,
                        () => Projectile.Center + Projectile.velocity * Projectile.ai[1], () => Projectile.active, 2000, style);
                    ReaperSwarmVisuals.Spawn("actor.player.weapon.34.muzzle", () => Projectile.Center,
                        () => Projectile.velocity, active: () => Projectile.active, style: style);
                    PhoenixFx.Burst("soundemitter.player.34.hit", Projectile.Center, Projectile.velocity, style);
                }
                beam.Extend();
            }
        }
        if (!Main.dedServ && Kind is 1 or 3 or 5 && Age % 3 == 0) {
            Dust d = Dust.NewDustPerfect(Projectile.Center, Kind == 5 ? DustID.Firework_Red : DustID.Torch,
                -Projectile.velocity * .15f, 80, Scale: 1.2f); d.noGravity = true;
        }
        if (Kind is 1 or 3 or 5) Projectile.rotation = Projectile.velocity.ToRotation();
    }
    private void Explode(float radius)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !ParentAlive) return;
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, Type,
            Projectile.damage, 0, Main.myPlayer, 2, radius, Projectile.ai[2]);
    }
    public override void OnKill(int timeLeft)
    {
        missile?.Stop();
        beam?.Stop();
        if (retired || Main.netMode == NetmodeID.MultiplayerClient || !ParentAlive) return;
        if (Kind is 1 or 3) Explode(Kind == 1 ? 94 : 80);
        if (Kind == 5) Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, Type,
            Math.Max(1, Projectile.damage * 3 / 4), 0, Main.myPlayer, 6, 48, Projectile.ai[2]);
    }
    internal void Retire() { retired = true; Projectile.Kill(); }
    public override void SendExtraAI(BinaryWriter writer) { writer.Write(Projectile.localAI[0]); writer.Write(locked); writer.Write(lockedY); }
    public override void ReceiveExtraAI(BinaryReader reader) { Projectile.localAI[0] = reader.ReadSingle(); locked = reader.ReadBoolean(); lockedY = reader.ReadSingle(); }
    public override bool PreDraw(ref Color lightColor)
    {
        HarbingerVisuals.DrawHazard(this);
        return false;
    }
}
