using System;
using System.Collections.Generic;
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
/// A temporary, synchronized infection of existing terrain. It never writes a tile,
/// paint, frame, slope, or world save: the original tile geometry supplies the silhouette.
/// </summary>
public sealed class FrostNovaBlackIceRegion : ModProjectile
{
    public const int ActiveRegionCap = 3;
    public const int WidthInTiles = 29;
    private const int DepthInTiles = 4;
    private const int FadeTicks = 60;
    private const int MaxTileCount = WidthInTiles * DepthInTiles;
    private readonly List<Point> infectedTiles = new(MaxTileCount);
    private readonly List<Point> surfaces = new(WidthInTiles);
    private int hostToken;
    private int totalLifetime = 600;
    private bool ready;
    private bool ownerConfirmed;
    private bool playedImpact;
    private int ownerWaitTicks;

    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.timeLeft = 600;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.penetrate = -1;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;

    internal static bool IsSupportTile(int x, int y)
    {
        if (!WorldGen.InWorld(x, y, 2)) return false;
        Tile tile = Main.tile[x, y];
        return tile.HasUnactuatedTile && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);
    }

    internal static float SurfaceY(int x, int y, float worldX)
    {
        Tile tile = Main.tile[x, y];
        if (tile.IsHalfBlock) return y * 16 + 8;
        float localX = Math.Clamp(worldX - x * 16, 0, 16);
        return y * 16 + (tile.Slope switch {
            SlopeType.SlopeDownLeft => localX,
            SlopeType.SlopeDownRight => 16 - localX,
            _ => 0
        });
    }

    internal static bool IsOwnerActive(int ownerIndex, int token)
    {
        return Main.npc.IndexInRange(ownerIndex) && token != 0 && Main.npc[ownerIndex].active
            && Main.npc[ownerIndex].ModNPC is FrostNova boss && boss.EncounterToken == token
            && boss.CombatHazardsActive && boss.AirbornePhase;
    }

    public static int ActiveCount(int ownerIndex)
    {
        int count = 0;
        foreach (Projectile patch in Main.ActiveProjectiles)
            if (patch.ModProjectile is FrostNovaBlackIceRegion region && (int)patch.ai[0] == ownerIndex
                && region.ready && IsOwnerActive(ownerIndex, region.hostToken)) count++;
        return count;
    }

    internal static void Spawn(Projectile source, Point impactTile, int ownerIndex, int token, int lifetime)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !IsOwnerActive(ownerIndex, token)) return;
        // An overlap cannot accumulate indefinite coverage: replace the oldest patch for this encounter.
        while (ActiveCount(ownerIndex) >= ActiveRegionCap)
        {
            Projectile oldest = null;
            foreach (Projectile patch in Main.ActiveProjectiles)
                if (patch.ModProjectile is FrostNovaBlackIceRegion region && (int)patch.ai[0] == ownerIndex
                    && region.hostToken == token && (oldest == null || patch.timeLeft < oldest.timeLeft)) oldest = patch;
            if (oldest == null) break;
            oldest.Kill();
        }

        Vector2 surfaceCenter = impactTile.ToWorldCoordinates(8, 0);
        if (IsSupportTile(impactTile.X, impactTile.Y))
            surfaceCenter.Y = SurfaceY(impactTile.X, impactTile.Y, surfaceCenter.X);
        int index = Projectile.NewProjectile(source.GetSource_FromThis(), surfaceCenter,
            Vector2.Zero, ModContent.ProjectileType<FrostNovaBlackIceRegion>(), 0, 0, Main.myPlayer,
            ownerIndex, impactTile.X, impactTile.Y);
        if (!Main.projectile.IndexInRange(index)) return;
        Projectile projectile = Main.projectile[index];
        if (projectile.ModProjectile is not FrostNovaBlackIceRegion patchRegion) return;
        patchRegion.hostToken = token;
        patchRegion.totalLifetime = Math.Clamp(lifetime, 480, 720);
        projectile.timeLeft = patchRegion.totalLifetime;
        patchRegion.CaptureSurface(impactTile);
        patchRegion.ready = true;
        projectile.netUpdate = true;
        if (patchRegion.infectedTiles.Count == 0) projectile.Kill();
    }

    private void CaptureSurface(Point impactTile)
    {
        infectedTiles.Clear();
        surfaces.Clear();
        // Each column follows its nearest exposed surface, including steps, slopes and platforms.
        // Search is local to the impact so a ceiling or a distant cavern floor cannot become infected.
        for (int dx = -WidthInTiles / 2; dx <= WidthInTiles / 2; dx++)
        {
            int x = impactTile.X + dx;
            int nearestY = -1;
            int nearestDistance = int.MaxValue;
            for (int dy = -10; dy <= 14; dy++)
            {
                int y = impactTile.Y + dy;
                if (!IsSupportTile(x, y) || IsSupportTile(x, y - 1)) continue;
                int distance = Math.Abs(dy);
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearestY = y;
            }
            if (nearestY < 0) continue;
            surfaces.Add(new Point(x, nearestY));
            for (int depth = 0; depth < DepthInTiles; depth++)
                if (IsSupportTile(x, nearestY + depth)) infectedTiles.Add(new Point(x, nearestY + depth));
        }
    }

    internal void ContributeTiles(Dictionary<Point, float> tiles)
    {
        if (!ready || !IsOwnerActive((int)Projectile.ai[0], hostToken)) return;
        int age = totalLifetime - Projectile.timeLeft;
        float radius = WidthInTiles * .5f * Math.Clamp((age + 1) / 24f, 0, 1);
        float opacity = Math.Clamp(Projectile.timeLeft / (float)FadeTicks, 0, 1);
        foreach (Point point in infectedTiles)
        {
            if (Math.Abs(point.X - Projectile.ai[1]) > radius || !IsSupportTile(point.X, point.Y)) continue;
            tiles[point] = Math.Max(tiles.GetValueOrDefault(point), opacity);
        }
    }

    public override void AI()
    {
        bool ownerActive = IsOwnerActive((int)Projectile.ai[0], hostToken);
        if (!ready || !ownerActive)
        {
            // JIP projectile snapshots can arrive before the NPC's token. Once confirmed, cleanup
            // is immediate; an ended encounter must never leave its terrain effect behind.
            if (Main.netMode == NetmodeID.MultiplayerClient && !ownerConfirmed && ownerWaitTicks++ < 90) return;
            Projectile.Kill();
            return;
        }
        ownerConfirmed = true;
        int age = totalLifetime - Projectile.timeLeft;
        if (!Main.dedServ && !playedImpact)
        {
            playedImpact = true;
            // Joining midway must not replay a ten-second-old impact.
            if (age < 15) EmitImpact();
        }
        if (!Main.dedServ && age % 9 == 0 && surfaces.Count > 0
            && ChargedOperatorVfx.Visible(Projectile.Center) && ParticleManager.activeParticles.Count < 1200)
        {
            Point point = surfaces[Main.rand.Next(surfaces.Count)];
            if (Math.Abs(point.X - Projectile.ai[1]) < WidthInTiles * .5f * Math.Clamp((age + 1) / 24f, 0, 1))
                new FrostNovaBlackIceShardParticle(new Vector2(point.X * 16 + 8, SurfaceY(point.X, point.Y, point.X * 16 + 8)),
                    new Vector2(Main.rand.NextFloat(-.35f, .35f), Main.rand.NextFloat(-1.15f, -.45f)),
                    Main.rand.Next(22, 35), Main.rand.NextFloat(3, 6)).Spawn();
        }
    }

    private void EmitImpact()
    {
        if (!ChargedOperatorVfx.Visible(Projectile.Center)) return;
        SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/FNIceRingStg1") with { Volume = .7f }, Projectile.Center);
        for (int i = 0; i < 14; i++)
        {
            float side = i % 2 == 0 ? -1 : 1;
            Vector2 velocity = new(side * Main.rand.NextFloat(1.5f, 6), Main.rand.NextFloat(-4, -1));
            if (ParticleManager.activeParticles.Count < 1200)
                new FrostNovaBlackIceShardParticle(Projectile.Center, velocity, 24 + i % 10, 5 + i % 5).Spawn();
            if (i % 3 == 0)
                ChargedOperatorVfx.Sparkle(Projectile.Center + new Vector2(side * i * 2, -3), velocity * .3f,
                    new Color(115, 215, 245), velocity.ToRotation(), .65f);
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (!ready || !IsOwnerActive((int)Projectile.ai[0], hostToken)) return false;
        int age = totalLifetime - Projectile.timeLeft;
        if (age < 30)
        {
            Texture2D explosion = ModContent.Request<Texture2D>("ArknightsMod/Content/NPCs/Enemy/Chapter6/FrostNova/BlackIceExplode").Value;
            Rectangle frame = explosion.Frame(1, 6, 0, Math.Clamp(age / 5, 0, 5));
            Main.EntitySpriteDraw(explosion, Projectile.Center - Main.screenPosition, frame, Color.White,
                0, frame.Size() * .5f, 1.2f, SpriteEffects.None);
        }
        float opacity = Math.Clamp(Projectile.timeLeft / (float)FadeTicks, 0, 1);
        float radius = WidthInTiles * .5f * Math.Clamp((age + 1) / 24f, 0, 1);
        using var pass = new ChargedOperatorVfx.Pass(true);
        foreach (Point point in surfaces)
        {
            if (Math.Abs(point.X - Projectile.ai[1]) > radius || !IsSupportTile(point.X, point.Y)) continue;
            float centerX = point.X * 16 + 8;
            Vector2 at = new(centerX, SurfaceY(point.X, point.Y, centerX));
            Vector2 left = new(centerX - 5, SurfaceY(point.X, point.Y, centerX - 5));
            Vector2 right = new(centerX + 5, SurfaceY(point.X, point.Y, centerX + 5));
            float pulse = .4f + .15f * MathF.Sin(age * .045f + point.X * .6f);
            // Narrow cracks are anchored to the tile, rather than covering the arena in lines.
            ChargedOperatorVfx.Line(left, at + new Vector2(1, 2),
                new Color(70, 155, 195) * (opacity * pulse), 1);
            ChargedOperatorVfx.Line(at + new Vector2(1, 2), right - new Vector2(0, 1),
                new Color(120, 205, 225) * (opacity * pulse), 1);
        }
        return false;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(hostToken);
        writer.Write((ushort)totalLifetime);
        writer.Write((ushort)Math.Clamp(Projectile.timeLeft, 0, totalLifetime));
        writer.Write(ready);
        writer.Write((byte)infectedTiles.Count);
        foreach (Point point in infectedTiles)
        {
            writer.Write((sbyte)(point.X - (int)Projectile.ai[1]));
            writer.Write((sbyte)(point.Y - (int)Projectile.ai[2]));
        }
        writer.Write((byte)surfaces.Count);
        foreach (Point point in surfaces)
        {
            writer.Write((sbyte)(point.X - (int)Projectile.ai[1]));
            writer.Write((sbyte)(point.Y - (int)Projectile.ai[2]));
        }
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        hostToken = reader.ReadInt32();
        totalLifetime = Math.Clamp((int)reader.ReadUInt16(), 480, 720);
        Projectile.timeLeft = Math.Clamp((int)reader.ReadUInt16(), 1, totalLifetime);
        ready = reader.ReadBoolean();
        infectedTiles.Clear();
        surfaces.Clear();
        int count = reader.ReadByte();
        for (int i = 0; i < count; i++)
        {
            int dx = reader.ReadSByte(), dy = reader.ReadSByte();
            if (i < MaxTileCount && Math.Abs(dx) <= WidthInTiles / 2 && dy >= -10 && dy <= 17)
                infectedTiles.Add(new Point((int)Projectile.ai[1] + dx, (int)Projectile.ai[2] + dy));
        }
        count = reader.ReadByte();
        for (int i = 0; i < count; i++)
        {
            int dx = reader.ReadSByte(), dy = reader.ReadSByte();
            if (i < WidthInTiles && Math.Abs(dx) <= WidthInTiles / 2 && dy >= -10 && dy <= 14)
                surfaces.Add(new Point((int)Projectile.ai[1] + dx, (int)Projectile.ai[2] + dy));
        }
    }
}

public sealed class FrostNovaBlackIceSystem : ModSystem
{
    internal static readonly Dictionary<Point, float> Tiles = new();
    public override void OnWorldLoad() => Tiles.Clear();
    public override void OnWorldUnload() => Tiles.Clear();
    public override void Unload() => Tiles.Clear();
    public override void PreUpdatePlayers() => Rebuild();
    public override void PostUpdateProjectiles() => Rebuild();

    private static void Rebuild()
    {
        Tiles.Clear();
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.ModProjectile is FrostNovaBlackIceRegion region) region.ContributeTiles(Tiles);
    }

    internal static bool StandingOnBlackIce(Player player)
    {
        if (Tiles.Count == 0 || player.dead || !player.active || player.gravDir < 0 || Math.Abs(player.velocity.Y) > 1.2f) return false;
        int left = Math.Max(2, (int)(player.position.X + 2) / 16);
        int right = Math.Min(Main.maxTilesX - 3, (int)(player.position.X + player.width - 2) / 16);
        // Includes the lower foot on sloped tiles and the exact top edge of a platform.
        int top = Math.Max(2, (int)(player.Bottom.Y - 5) / 16);
        int bottom = Math.Min(Main.maxTilesY - 3, (int)(player.Bottom.Y + 3) / 16);
        for (int x = left; x <= right; x++)
            for (int y = top; y <= bottom; y++)
            {
                if (!Tiles.ContainsKey(new Point(x, y)) || !FrostNovaBlackIceRegion.IsSupportTile(x, y)) continue;
                float surface = Math.Min(FrostNovaBlackIceRegion.SurfaceY(x, y, Math.Max(player.Left.X + 2, x * 16)),
                    FrostNovaBlackIceRegion.SurfaceY(x, y, Math.Min(player.Right.X - 2, x * 16 + 16)));
                if (Math.Abs(player.Bottom.Y - surface) <= 3) return true;
            }
        return false;
    }
}

public sealed class FrostNovaBlackIceTileTint : GlobalTile
{
    public override void DrawEffects(int i, int j, int type, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
    {
        if (!FrostNovaBlackIceSystem.Tiles.TryGetValue(new Point(i, j), out float opacity)) return;
        Color blackIce = new(4, 8, 16);
        drawData.tileLight = Color.Lerp(drawData.tileLight, blackIce, opacity);
        drawData.finalColor = Color.Lerp(drawData.finalColor, blackIce, opacity);
        drawData.colorTint = Color.Lerp(drawData.colorTint, blackIce, opacity);
        drawData.glowColor *= 1 - opacity;
    }
}

public sealed class FrostNovaBlackIcePlayer : ModPlayer
{
    public override void PostUpdateRunSpeeds()
    {
        if (!FrostNovaBlackIceSystem.StandingOnBlackIce(Player)) return;
        // Vanilla resets these values every update. Apply once to the aggregate tile union;
        // overlapping regions therefore remain exactly 50%, never 25% or an exponential stop.
        Player.maxRunSpeed *= .5f;
        Player.accRunSpeed *= .5f;
        Player.runAcceleration *= .5f;
        float cap = Math.Max(Player.maxRunSpeed, Player.accRunSpeed);
        if (cap > 0 && Math.Abs(Player.velocity.X) > cap)
            Player.velocity.X = Math.Sign(Player.velocity.X) * cap;
    }
}

/// <summary>Crystal fragments use this mod's particle manager, separate from vanilla dust and pooled sparkles.</summary>
public sealed class FrostNovaBlackIceShardParticle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly float size;
    public override string TexturePath => "ArknightsMod/Content/NPCs/Enemy/Chapter6/FrostNova/BlackIce";
    public override BlendState DrawBlendState => BlendState.NonPremultiplied;
    public override SamplerState DrawSamplerState => SamplerState.LinearClamp;
    public FrostNovaBlackIceShardParticle(Vector2 position, Vector2 velocity, int lifetime, float size)
    {
        Position = position;
        Velocity = velocity;
        Lifetime = Math.Clamp(lifetime, 12, 40);
        this.size = Math.Clamp(size, 2, 12);
        Rotation = velocity.ToRotation();
        Color = new Color(140, 220, 245);
    }
    public override void Update()
    {
        Velocity *= .96f;
        Velocity.Y += .035f;
        Rotation += .045f;
    }
    public override void Draw()
    {
        float fade = MathF.Sin(MathHelper.Pi * Math.Clamp(LifetimeRatio, 0, 1));
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color * fade, Rotation,
            Texture.Size() * .5f, new Vector2(size, size * 1.7f) / Texture.Size(), SpriteEffects.None, 0);
    }
}
