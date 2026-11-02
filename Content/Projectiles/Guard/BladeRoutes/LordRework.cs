using System;
using ArknightsMod.Content.Projectiles.Charged;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.BladeRoutes;

internal static class LordReworkCombat
{
    internal static void Swing(Player player, IEntitySource source, int kind, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return;
        float scale = player.GetAdjustedItemScale(player.HeldItem);
        if (kind == 7)
        {
            var skills = player.GetModPlayer<WeaponPlayer>();
            damage = (int)(damage * 1.12f);
            if (skills.SkillActive)
            {
                if (skills.Skill == 0) damage = (int)(damage * 2.9f);
                else if (skills.Skill == 2) { damage *= 3; scale *= 1.35f; }
            }
        }
        Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
        Projectile.NewProjectile(source, player.MountedCenter, aim, ModContent.ProjectileType<LordThornsSwing>(), damage,
            knockback, player.whoAmI, kind, aim.ToRotation(), Math.Max(1, player.itemAnimationMax));
    }
    internal static NPC BirdTarget(Vector2 cursor)
    {
        NPC best = null; float distance = 600 * 600;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            float d = Vector2.DistanceSquared(cursor, npc.Center);
            if (npc.CanBeChasedBy() && d < distance) { best = npc; distance = d; }
        }
        return best;
    }
}

public sealed class LordThornsSwing : ModProjectile
{
    private readonly Vector2[] tips = new Vector2[14];
    private bool released;
    internal int Kind => (int)Projectile.ai[0];
    internal float Progress => Projectile.localAI[0] / Math.Max(1, Projectile.ai[2]);
    private Vector2 tip;
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16; Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.timeLeft = 360;
        Projectile.extraUpdates = 1; Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
        Projectile.ownerHitCheck = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        if (!player.active || player.dead || player.noItems || player.CCed || BladeRouteRules.HeldKind(player.HeldItem) != Kind)
        { Projectile.Kill(); return; }
        Projectile.localAI[0] += 1f / Projectile.MaxUpdates;
        Projectile.Center = player.MountedCenter;
        int facing = MathF.Cos(Projectile.ai[1]) >= 0 ? 1 : -1;
        player.ChangeDir(facing); player.heldProj = Projectile.whoAmI;
        // Thorns' sin/cos orbit: a broad advancing arc followed by a slower exit.
        float sweep = Math.Min(1, Progress / .82f), exit = Math.Max(0, Progress - .82f) * .4f;
        float theta = facing > 0 ? MathHelper.Pi * (1.75f + 1.25f * sweep + exit)
            : MathHelper.Pi * (1.15f + 1.35f * sweep + exit);
        float t = facing > 0 ? -Projectile.ai[1] : Projectile.ai[1];
        Vector2 radial = facing > 0 ? new Vector2(MathF.Sin(theta + t), MathF.Cos(theta + t))
            : new Vector2(MathF.Cos(theta + t), MathF.Sin(theta + t));
        Projectile.rotation = radial.ToRotation();
        Projectile.scale = player.GetAdjustedItemScale(player.HeldItem);
        var skills = player.GetModPlayer<WeaponPlayer>();
        if (Kind == 7 && skills.SkillActive && skills.Skill == 2) Projectile.scale *= 1.35f;
        tip = Projectile.Center + radial * 68 * Projectile.scale;
        for (int i = tips.Length - 1; i > 0; i--) tips[i] = tips[i - 1];
        tips[0] = radial * 60 * Projectile.scale;
        if (Projectile.localAI[0] <= 1f / Projectile.MaxUpdates) Array.Fill(tips, tips[0]);
        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
        if (!released && Progress >= .35f)
        {
            released = true;
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(radial);
                int damage = Math.Max(1, (int)(Projectile.damage * .8f));
                var clock = player.GetModPlayer<LordShotClock>();
                if (Kind is 3 or 5 or 6 && clock.TryRelease(Main.GameUpdateCount))
                {
                    Vector2 origin = player.MountedCenter + aim * 26;
                    if (Kind == 3)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), origin, aim * (clock.Sequence % 2 == 0 ? 9 : 11),
                            ModContent.ProjectileType<MidnightVeil>(), damage, Projectile.knockBack, Projectile.owner);
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), origin, aim,
                            ModContent.ProjectileType<MidnightVeilImpact>(), 0, 0, Projectile.owner, 0, .45f);
                    }
                    else if (Kind == 6)
                        FrostleafQueenWave.Spawn(Projectile, origin, aim * (clock.Sequence % 2 == 0 ? 7.5f : 9f), damage);
                    else
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), origin, aim * BladeRouteRules.WaveSpeed,
                            ModContent.ProjectileType<LapplandNoxShard>(), damage, Projectile.knockBack, Projectile.owner, 5);
                }
                else if (Kind == 7)
                {
                    Vector2 cursor = Main.MouseWorld; NPC target = LordReworkCombat.BirdTarget(cursor);
                    Vector2 start = player.MountedCenter + new Vector2(-facing * 48, -320);
                    Vector2 destination = target?.Center ?? cursor;
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), start, (destination - start).SafeNormalize(Vector2.UnitY) * 22,
                        ModContent.ProjectileType<SilverAshMacaw>(), damage, Projectile.knockBack, Projectile.owner,
                        target?.whoAmI + 1 ?? 0, cursor.X, cursor.Y);
                }
            }
        }
        if (Progress >= 1) Projectile.Kill();
    }
    public override bool? Colliding(Rectangle a, Rectangle b)
    {
        float collision = 0;
        return Collision.CheckAABBvLineCollision(b.TopLeft(), b.Size(), Projectile.Center, tip, 16 * Projectile.scale, ref collision);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Kind == 6) BladeRouteCombat.FrostleafHit(Main.player[Projectile.owner], target);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        LordThornsSlash.Draw(tips, Projectile.Center, MathF.Cos(Projectile.ai[1]) >= 0, Kind, Projectile.scale);
        BladeRouteArt.Blade(Projectile.Center, Projectile.rotation, 76 * Projectile.scale, Kind, lightColor);
        return false;
    }
}

public sealed class SilverAshMacaw : ModProjectile
{
    internal bool Fading => Projectile.ai[0] < 0;
    public override string Texture => "Terraria/Images/NPC_" + NPCID.ScarletMacaw;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 24; ProjectileID.Sets.TrailingMode[Type] = 2;
    }
    public override void SetDefaults()
    {
        Projectile.width = 34; Projectile.height = 30; Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.timeLeft = 150; Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1; Projectile.netImportant = true;
    }
    public override void AI()
    {
        Projectile.localAI[0]++;
        // Native NPC 671: six rows; idle row 0, flying rows 1..5, one step per 4 ticks.
        if (++Projectile.frameCounter >= 4) { Projectile.frameCounter = 0; if (++Projectile.frame >= 6) Projectile.frame = 1; }
        if (Fading)
        {
            Projectile.friendly = false; Projectile.velocity *= .92f;
            Projectile.Opacity = Math.Clamp(Projectile.timeLeft / 36f, 0, 1);
        }
        else
        {
            Vector2 targetPoint = new(Projectile.ai[1], Projectile.ai[2]);
            int index = (int)Projectile.ai[0] - 1;
            bool hasTarget = Main.npc.IndexInRange(index) && Main.npc[index].CanBeChasedBy(Projectile);
            if (hasTarget)
                targetPoint = Main.npc[index].Center + Main.npc[index].velocity * 3;
            Vector2 to = targetPoint - Projectile.Center;
            float speed = Math.Min(32, Projectile.velocity.Length() + .45f);
            Projectile.velocity = Projectile.velocity.ToRotation().AngleTowards(to.ToRotation(), .055f).ToRotationVector2() * speed;
            if ((!hasTarget && to.Length() < 25) || Projectile.localAI[0] >= 100) Fade();
        }
        Projectile.spriteDirection = Projectile.velocity.X >= 0 ? 1 : -1;
        Projectile.rotation = Math.Clamp(Projectile.velocity.Y * .014f * Projectile.spriteDirection, -.7f, .7f);
        if (!Main.dedServ) Lighting.AddLight(Projectile.Center, new Vector3(.24f, .28f, .32f) * Projectile.Opacity);
    }
    private void Fade()
    {
        if (Fading) return;
        Projectile.ai[0] = -1; Projectile.timeLeft = 36; Projectile.netUpdate = true; Projectile.friendly = false;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Fade(); // No Kill: body, flapping and the silver trail persist throughout the exit.
        Projectile.velocity *= .7f;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        ChargedOperatorVfx.Trail(Projectile, new Color(165, 190, 215) * Projectile.Opacity, 16);
        using (var pass = new ChargedOperatorVfx.Pass(true))
        {
            Vector2 last = Projectile.Center; float length = 0;
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) break;
                Vector2 point = Projectile.oldPos[i] + Projectile.Size * .5f;
                float distance = Vector2.Distance(last, point);
                if (distance > 96 || (length += distance) > 260) break;
                float fade = 1 - length / 260;
                if (distance > .5f)
                {
                    ChargedOperatorVfx.Sprite("TrailFilament", (last + point) * .5f,
                        new Vector2(distance + 8, 22 * fade), new Color(165, 185, 210) * (.8f * fade * Projectile.Opacity), (last - point).ToRotation());
                    ChargedOperatorVfx.Line(last, point, new Color(210, 220, 235) * (.55f * fade * Projectile.Opacity), 1.5f * fade);
                }
                last = point;
            }
        }
        Main.instance.LoadNPC(NPCID.ScarletMacaw); Texture2D tex = TextureAssets.Npc[NPCID.ScarletMacaw].Value;
        // Vanilla frame height is sheet height / npcFrameCount, never draw the whole atlas.
        int frames = Math.Max(6, Main.npcFrameCount[NPCID.ScarletMacaw]);
        Rectangle frame = tex.Frame(1, frames, 0, Projectile.frame);
        Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, Color.White * Projectile.Opacity,
            Projectile.rotation, frame.Size() * .5f, 1.6f, Projectile.spriteDirection > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
        return false;
    }
}

// Teaching chapter 12: pooled vanilla PrettySparkle, reset every borrowed field,
// normalized opacity envelope plus an independent 0..60 shape envelope.
public sealed class LordLightingBolts : ModSystem
{
    private static ParticlePool<PrettySparkleParticle> pool;
    public override void Unload() => pool = null;
    internal static void Flight(Projectile projectile)
    {
        if (Main.dedServ || Main.gameMenu || projectile.numUpdates != 0 || !Main.rand.NextBool(4)) return;
        pool ??= new ParticlePool<PrettySparkleParticle>(128, () => new PrettySparkleParticle());
        var p = pool.RequestParticle();
        Vector2 axis = projectile.velocity.SafeNormalize(Vector2.UnitX);
        p.LocalPosition = projectile.Center - axis * 6 + axis.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-24, 24);
        p.Velocity = -axis * .4f; p.AccelerationPerFrame = Vector2.Zero;
        p.Rotation = axis.ToRotation(); p.RotationVelocity = p.RotationAcceleration = 0;
        p.Scale = new Vector2(.38f, .2f); p.ScaleVelocity = p.ScaleAcceleration = Vector2.Zero;
        p.ColorTint = new Color(205, 215, 245) * .8f; p.TimeToLive = 18;
        p.FadeInNormalizedTime = .04f; p.FadeOutNormalizedTime = .78f;
        p.FadeInEnd = 6; p.FadeOutStart = 18; p.FadeOutEnd = 60;
        p.DrawHorizontalAxis = true; p.DrawVerticalAxis = false; p.AdditiveAmount = .45f;
        Main.ParticleSystem_World_OverPlayers.Add(p);
    }
}
