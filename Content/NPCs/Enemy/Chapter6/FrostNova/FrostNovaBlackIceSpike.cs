using System;
using System.IO;
using ArknightsMod.Common.Particle;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

/// <summary>
/// ai[0]: host FrostNova slot; ai[1]: locked warning duration (default 60 ticks);
/// ai[2]: temporary terrain infection duration (default 600 ticks).
/// Spawn above the target with a downward velocity; the warning never follows a player.
/// </summary>
public sealed class FrostNovaBlackIceSpike : ModProjectile
{
    private int age;
    private int warningTicks = 60;
    private int regionLifetime = 600;
    private int hostToken;
    private int ownerWaitTicks;
    private bool initialized;
    private bool released;
    private bool ownerConfirmed;
    private bool madeRegion;
    private Vector2 lockedCenter;
    private Vector2 impactPoint;
    internal bool Released => released;
    internal Vector2 WarningTarget => impactPoint;

    public override string Texture => "ArknightsMod/Content/NPCs/Enemy/Chapter6/FrostNova/BlackIce";
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 16;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }
    public override void SetDefaults()
    {
        Projectile.width = 22;
        Projectile.height = 28;
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.timeLeft = 300;
    }

    public override void OnSpawn(IEntitySource source)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        warningTicks = Math.Clamp((int)Projectile.ai[1] <= 0 ? 60 : (int)Projectile.ai[1], 45, 120);
        regionLifetime = Math.Clamp((int)Projectile.ai[2] <= 0 ? 600 : (int)Projectile.ai[2], 480, 720);
        if (Main.npc.IndexInRange((int)Projectile.ai[0]) && Main.npc[(int)Projectile.ai[0]].ModNPC is FrostNova boss)
            hostToken = boss.EncounterToken;
        lockedCenter = Projectile.Center;
        impactPoint = FindImpactPoint(lockedCenter);
        Projectile.velocity = Vector2.Zero;
        Projectile.timeLeft = warningTicks + 210;
        Projectile.rotation = -MathHelper.PiOver4;
        initialized = true;
        Projectile.netUpdate = true;
    }

    private Vector2 FindImpactPoint(Vector2 start)
    {
        int left = Math.Max(2, (int)(start.X - Projectile.width * .5f) / 16);
        int right = Math.Min(Main.maxTilesX - 3, (int)(start.X + Projectile.width * .5f) / 16);
        int startY = Math.Clamp((int)(start.Y + Projectile.height * .5f) / 16, 2, Main.maxTilesY - 3);
        int endY = Math.Min(Main.maxTilesY - 3, startY + 110);
        for (int y = startY; y <= endY; y++)
        {
            float firstSurfaceY = float.PositiveInfinity;
            for (int x = left; x <= right; x++)
                if (FrostNovaBlackIceRegion.IsSupportTile(x, y))
                {
                    float overlapLeft = Math.Max(start.X - Projectile.width * .5f, x * 16);
                    float overlapRight = Math.Min(start.X + Projectile.width * .5f, x * 16 + 16);
                    float surface = Math.Min(FrostNovaBlackIceRegion.SurfaceY(x, y, overlapLeft),
                        FrostNovaBlackIceRegion.SurfaceY(x, y, overlapRight));
                    firstSurfaceY = Math.Min(firstSurfaceY, surface);
                }
            if (float.IsFinite(firstSurfaceY)) return new Vector2(start.X, firstSurfaceY);
        }
        return start + Vector2.UnitY * 1760;
    }

    public override bool? CanDamage() => initialized && released
        && FrostNovaBlackIceRegion.IsOwnerActive((int)Projectile.ai[0], hostToken) ? null : false;

    public override void AI()
    {
        bool validOwner = FrostNovaBlackIceRegion.IsOwnerActive((int)Projectile.ai[0], hostToken);
        if (!initialized || !validOwner)
        {
            Projectile.velocity = Vector2.Zero;
            if (Main.netMode == NetmodeID.MultiplayerClient && !ownerConfirmed && ownerWaitTicks++ < 90) return;
            Projectile.Kill();
            return;
        }
        ownerConfirmed = true;
        age++;
        if (age < warningTicks)
        {
            Projectile.Center = lockedCenter;
            Projectile.velocity = Vector2.Zero;
            Projectile.Opacity = Math.Clamp(age / 12f, 0, 1);
        }
        else if (!released)
        {
            released = true;
            Projectile.Center = lockedCenter;
            Projectile.velocity = Vector2.UnitY * 4.8f;
            Projectile.tileCollide = true;
            Projectile.Opacity = 1;
            Projectile.netUpdate = Main.netMode != NetmodeID.MultiplayerClient;
            if (!Main.dedServ && ChargedOperatorVfx.Visible(Projectile.Center))
                SoundEngine.PlaySound(SoundID.Item28 with { Volume = .55f, Pitch = -.25f }, Projectile.Center);
        }
        else Projectile.velocity = Vector2.UnitY * Math.Min(12, Projectile.velocity.Y + .42f);

        Projectile.rotation = -MathHelper.PiOver4;
        if (!Main.dedServ && ChargedOperatorVfx.Visible(Projectile.Center) && age % 5 == 0)
        {
            if (ParticleManager.activeParticles.Count < 1200)
                new FrostNovaBlackIceShardParticle(Projectile.Center + new Vector2(Main.rand.NextFloat(-8, 8), -5),
                    released ? -Projectile.velocity * .12f : new Vector2(0, -.5f), 18, released ? 5 : 3).Spawn();
            if (age % 10 == 0)
                ChargedOperatorVfx.Sparkle(Projectile.Center + new Vector2(age % 20 == 0 ? -11 : 11, 0),
                    new Vector2(0, -.3f), new Color(150, 230, 255), MathHelper.PiOver2, .45f);
        }
    }

    public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
    {
        fallThrough = false;
        return true;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (!released) return false;
        FrostNovaIceBurstSystem.Emit(Projectile.Bottom, 1.55f);
        if (Main.netMode != NetmodeID.MultiplayerClient && !madeRegion
            && FrostNovaBlackIceRegion.IsOwnerActive((int)Projectile.ai[0], hostToken))
        {
            madeRegion = true;
            // Tile collision can stop a crystal with its feet exactly on the preceding tile row.
            Point impact = Projectile.Bottom.ToTileCoordinates();
            if (!FrostNovaBlackIceRegion.IsSupportTile(impact.X, impact.Y)
                && FrostNovaBlackIceRegion.IsSupportTile(impact.X, impact.Y + 1)) impact.Y++;
            FrostNovaBlackIceRegion.Spawn(Projectile, impact, (int)Projectile.ai[0], hostToken, regionLifetime);
        }
        return true;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        // The spike never freezes a player into the terrain denial that it is about to create.
        target.AddBuff(BuffID.Chilled, 75);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        FrostNovaProjectileArt.Draw(Projectile, Projectile.GetGlobalProjectile<FrostNovaOwnedProjectile>().Visibility);
        return false;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(hostToken);
        writer.Write((ushort)age);
        writer.Write((byte)warningTicks);
        writer.Write((ushort)regionLifetime);
        writer.Write(initialized);
        writer.Write(released);
        writer.Write(lockedCenter.X); writer.Write(lockedCenter.Y);
        writer.Write(impactPoint.X); writer.Write(impactPoint.Y);
        writer.Write((ushort)Math.Clamp(Projectile.timeLeft, 0, 400));
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        hostToken = reader.ReadInt32();
        age = reader.ReadUInt16();
        warningTicks = Math.Clamp((int)reader.ReadByte(), 45, 120);
        regionLifetime = Math.Clamp((int)reader.ReadUInt16(), 480, 720);
        initialized = reader.ReadBoolean();
        released = reader.ReadBoolean();
        lockedCenter = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        impactPoint = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        Projectile.timeLeft = Math.Clamp((int)reader.ReadUInt16(), 1, 400);
        Projectile.tileCollide = released;
    }
}
