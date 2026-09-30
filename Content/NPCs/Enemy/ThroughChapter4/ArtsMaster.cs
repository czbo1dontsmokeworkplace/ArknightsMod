using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Utilities;
using ArknightsMod.Content.Projectiles.Medic.Sussurro;

namespace ArknightsMod.Content.NPCs.Enemy.ThroughChapter4;

public abstract class ArtsMasterDrone : ModNPC
{
    private const int FrameCount = 8;
    private const int WarningA1 = 32;
    private const int WarningA2 = 24;
    private const float EdgeDistance = 600f;
    private const float SearchRange = 1000f;
    // Measured from the eight source frames: one pixel beyond the lower energy tube's front edge.
    private static readonly float[] A1MuzzleX = [11f, 11f, 11f, 11f, 9f, 5f, 5f, 7f];
    private static readonly float[] A1MuzzleY = [15.5f, 15.5f, 15.5f, 15.5f, 15.5f, 15.5f, 13.5f, 13.5f];
    private static readonly float[] A2MuzzleX = [23f, 23f, 23f, 23f, 21f, 19f, 21f, 23f];
    private enum FlightState { Cruise, Warning, Sweep, FlyPast, Recovery }
    private FlightState State { get => (FlightState)(int)NPC.ai[0]; set => NPC.ai[0] = (float)value; }
    private int Timer { get => (int)NPC.ai[1]; set => NPC.ai[1] = value; }
    protected abstract bool IsSweep { get; }
    private int FlightDirection { get => NPC.ai[3] >= 0f ? 1 : -1; set => NPC.ai[3] = value; }
    private float aimAngle;
    private float patrolCenterX;
    private float patrolAltitudeY;

    public override void SetStaticDefaults() => Main.npcFrameCount[Type] = FrameCount;

    public override void SetDefaults()
    {
        NPC.width = IsSweep ? 40 : 34;
        NPC.height = IsSweep ? 34 : 30;
        NPC.lifeMax = 260;
        NPC.damage = 26;
        NPC.defense = 7;
        NPC.knockBackResist = .45f;
        NPC.value = Item.buyPrice(silver: 2);
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.aiStyle = -1;
        NPC.HitSound = SoundID.NPCHit4;
        NPC.DeathSound = SoundID.NPCDeath14;
    }

    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry entry)
    {
        entry.Info.AddRange(new IBestiaryInfoElement[] {
            BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
            new FlavorTextBestiaryInfoElement(IsSweep
                ? "法术大师 A2：高空巡航，悬停后以持续光束扫过战场。"
                : "法术大师 A1：低空突袭，瞄准后射出高速光线。")
        });
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo) => SpawnCondition.OverworldDaySlime.Chance * .0325f;

    public override void OnSpawn(IEntitySource source)
    {
        patrolCenterX = NPC.Center.X;
        patrolAltitudeY = NPC.Center.Y;
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        FlightDirection = Main.rand.NextBool() ? 1 : -1;
        NPC.netUpdate = true;
    }

    public override void AI()
    {
        NPC.TargetClosest(false);
        Player player = Main.player[NPC.target];
        bool valid = player.active && !player.dead;
        float speed = IsSweep ? 3.1f : 5.2f;
        float groundY = FindGroundY();
        // Keep the initial cruise altitude as a ceiling for descent. Terrain gaps must not drag the drone downward.
        float desiredY = Math.Min(groundY - (IsSweep ? 8f : 5f) * 16f - NPC.height * .5f, patrolAltitudeY);
        if (valid && Math.Abs(player.Center.X - NPC.Center.X) < SearchRange)
            desiredY = Math.Min(desiredY, player.Top.Y - (IsSweep ? 128f : 80f));
        NPC.velocity.Y = MathHelper.Clamp((desiredY - NPC.Center.Y) * .09f, -3.5f, 1.2f);
        if (Math.Abs(desiredY - NPC.Center.Y) < 3f) NPC.velocity.Y = 0f;

        if (!valid && State != FlightState.Cruise && Main.netMode != NetmodeID.MultiplayerClient)
            ChangeState(FlightState.Cruise);

        switch (State) {
            case FlightState.Cruise:
                if (valid && Math.Abs(player.Center.X - NPC.Center.X) < SearchRange &&
                    Math.Abs(player.Center.X - NPC.Center.X) > 32f && Main.netMode != NetmodeID.MultiplayerClient) {
                    int searchDirection = player.Center.X > NPC.Center.X ? 1 : -1;
                    if (FlightDirection != searchDirection) {
                        FlightDirection = searchDirection;
                        NPC.netUpdate = true;
                    }
                }
                Fly(speed, valid ? player.Center.X : patrolCenterX, true);
                if (valid && Main.netMode != NetmodeID.MultiplayerClient &&
                    Math.Abs(NPC.Center.X - player.Center.X) <= (IsSweep ? 256f : 96f) &&
                    Math.Abs(NPC.Center.Y - player.Center.Y) <= (IsSweep ? 480f : 320f))
                    ChangeState(FlightState.Warning);
                break;
            case FlightState.Warning:
                NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, IsSweep ? 0f : FlightDirection * 1.1f, .14f);
                if (valid) aimAngle = (player.Center - Muzzle).ToRotation();
                Timer++;
                if (Main.netMode != NetmodeID.MultiplayerClient && Timer >= (IsSweep ? WarningA2 : WarningA1)) {
                    Vector2 direction = (player.Center - Muzzle).SafeNormalize(new Vector2(FlightDirection, 0f));
                    if (IsSweep) {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), Muzzle, direction,
                            ModContent.ProjectileType<ArtsMasterBeam>(), 18, 0f, Main.myPlayer, NPC.whoAmI);
                        ChangeState(FlightState.Sweep);
                    }
                    else {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), Muzzle, direction * 12f,
                            ModContent.ProjectileType<ArtsMasterNeedle>(), 20, 0f, Main.myPlayer);
                        ChangeState(FlightState.FlyPast);
                    }
                    if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Item158 with { Volume = .4f }, NPC.Center);
                }
                break;
            case FlightState.Sweep:
                NPC.velocity.X *= .82f;
                Timer++;
                if (Main.netMode != NetmodeID.MultiplayerClient && Timer >= 60)
                    ChangeState(FlightState.Recovery);
                break;
            case FlightState.FlyPast:
                Fly(speed * 1.15f, player.Center.X, false);
                Timer++;
                if (Main.netMode != NetmodeID.MultiplayerClient && Timer > 15 &&
                    ((FlightDirection > 0 && NPC.Center.X >= player.Center.X + EdgeDistance) ||
                     (FlightDirection < 0 && NPC.Center.X <= player.Center.X - EdgeDistance))) {
                    FlightDirection *= -1;
                    ChangeState(FlightState.Recovery);
                }
                break;
            case FlightState.Recovery:
                NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, FlightDirection * speed, .055f);
                Timer++;
                if (Main.netMode != NetmodeID.MultiplayerClient && Timer >= (IsSweep ? 75 : 30))
                    ChangeState(FlightState.Cruise);
                break;
        }

        if (State is FlightState.Cruise or FlightState.FlyPast or FlightState.Recovery) {
            if (NPC.Center.X < 160f || NPC.Center.X > Main.maxTilesX * 16f - 160f ||
                Collision.SolidCollision(NPC.Center + new Vector2(FlightDirection * 24f, -NPC.height * .5f), 10, NPC.height)) {
                if (Main.netMode != NetmodeID.MultiplayerClient) {
                    FlightDirection *= -1;
                    NPC.netUpdate = true;
                }
                NPC.velocity.X *= .5f;
            }
        }
        NPC.direction = NPC.spriteDirection = FlightDirection;
        NPC.rotation = NPC.velocity.X * .012f;
    }

    internal Vector2 Muzzle {
        get {
            int frameHeight = IsSweep ? 46 : 38;
            int frame = Math.Clamp(NPC.frame.Y / frameHeight, 0, FrameCount - 1);
            float x = IsSweep ? A2MuzzleX[frame] : A1MuzzleX[frame];
            float y = IsSweep ? 17.5f : A1MuzzleY[frame];
            Vector2 offset = new Vector2(x * NPC.spriteDirection, y).RotatedBy(NPC.rotation) * NPC.scale;
            return NPC.Center + offset;
        }
    }

    private void Fly(float speed, float centerX, bool turnAtEdge)
    {
        float edge = centerX + FlightDirection * EdgeDistance;
        if (turnAtEdge && FlightDirection * (edge - NPC.Center.X) <= 0f && Main.netMode != NetmodeID.MultiplayerClient) {
            FlightDirection *= -1;
            NPC.netUpdate = true;
        }
        NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, FlightDirection * speed, .075f);
    }

    private float FindGroundY()
    {
        int x = Math.Clamp((int)(NPC.Center.X / 16f), 10, Main.maxTilesX - 11);
        int startY = Math.Clamp((int)(NPC.Center.Y / 16f), 10, Main.maxTilesY - 11);
        for (int y = startY; y < Math.Min(startY + 25, Main.maxTilesY - 10); y++) {
            Tile tile = Framing.GetTileSafely(x, y);
            if (tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                return y * 16f;
        }
        return NPC.Center.Y + (IsSweep ? 8f : 5f) * 16f + NPC.height * .5f;
    }

    private void ChangeState(FlightState state)
    {
        State = state;
        Timer = 0;
        NPC.netUpdate = true;
    }

    public override void FindFrame(int frameHeight)
    {
        NPC.frameCounter++;
        NPC.frame.Y = (int)(NPC.frameCounter / 5) % FrameCount * frameHeight;
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (State == FlightState.Warning) {
            Vector2 direction = aimAngle.ToRotationVector2();
            float length = ArtsMasterBeam.TraceLength(Muzzle, direction);
            ArtsMasterBeam.DrawLine(Muzzle, Muzzle + direction * length, new Color(210, 80, 255), 2f, .35f + .35f * Timer / (IsSweep ? WarningA2 : WarningA1));
        }
        Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
        Main.EntitySpriteDraw(texture, NPC.Center - screenPos, NPC.frame, NPC.GetAlpha(drawColor), NPC.rotation,
            NPC.frame.Size() * .5f, NPC.scale,
            NPC.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
        return false;
    }
}

public sealed class ArtsMasterA1 : ArtsMasterDrone
{
    protected override bool IsSweep => false;
    public override string Texture => "ArknightsMod/Content/NPCs/Enemy/ThroughChapter4/ArtsMasterA1";
}

public sealed class ArtsMasterA2 : ArtsMasterDrone
{
    protected override bool IsSweep => true;
    public override string Texture => "ArknightsMod/Content/NPCs/Enemy/ThroughChapter4/ArtsMasterA2";
}

public sealed class ArtsMasterNeedle : ModProjectile
{
    private readonly bool[] hitPlayers = new bool[Main.maxPlayers];
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 30 * 15;
        Projectile.extraUpdates = 14;
    }
    public override bool CanHitPlayer(Player target) => !hitPlayers[target.whoAmI];
    public override void OnHitPlayer(Player target, Player.HurtInfo info) => hitPlayers[target.whoAmI] = true;
    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (Projectile.numUpdates == 0) Lighting.AddLight(Projectile.Center, .35f, .05f, .45f);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        SussurroVisuals.Soft(Projectile.Center - direction * 18f, new Vector2(90f, 14f),
            new Color(145, 35, 210), .55f, Projectile.rotation);
        ArtsMasterBeam.DrawLine(Projectile.Center - direction * 55f, Projectile.Center + direction * 15f,
            new Color(190, 65, 255), 6f, .65f);
        ArtsMasterBeam.DrawLine(Projectile.Center - direction * 25f, Projectile.Center + direction * 15f,
            Color.White, 2f, .85f);
        return false;
    }
}

public sealed class ArtsMasterBeam : ModProjectile
{
    private const float MaxLength = 1200f;
    private readonly bool[] hitPlayers = new bool[Main.maxPlayers];
    private Vector2 Direction => Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(
        MathHelper.ToRadians(-15f + 30f * MathHelper.Clamp(Projectile.localAI[0] / 59f, 0f, 1f)));
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1300;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 60;
    }
    public override bool CanHitPlayer(Player target) => !hitPlayers[target.whoAmI];
    public override void OnHitPlayer(Player target, Player.HurtInfo info) => hitPlayers[target.whoAmI] = true;
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        int index = (int)Projectile.ai[0];
        if (!Main.npc.IndexInRange(index) || !Main.npc[index].active || Main.npc[index].type != ModContent.NPCType<ArtsMasterA2>()) {
            Projectile.Kill();
            return;
        }
        NPC source = Main.npc[index];
        Projectile.Center = ((ArtsMasterDrone)source.ModNPC).Muzzle;
        Projectile.localAI[0]++;
        if (!Main.dedServ) Lighting.AddLight(Projectile.Center + Direction * 90f, .35f, .07f, .5f);
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float collision = 0f;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
            Projectile.Center + Direction * TraceLength(Projectile.Center, Direction), 13f, ref collision);
    }
    public override bool? CanDamage() => Projectile.localAI[0] > 3f ? null : false;
    public override bool PreDraw(ref Color lightColor)
    {
        float fade = MathHelper.Clamp(Math.Min(Projectile.localAI[0] / 7f, Projectile.timeLeft / 8f), 0f, 1f);
        SussurroVisuals.DrawCompression(Projectile.Center, Direction, TraceLength(Projectile.Center, Direction),
            fade, true, new Color(125, 25, 190), new Color(205, 80, 255), Color.White);
        return false;
    }
    internal static float TraceLength(Vector2 start, Vector2 direction)
    {
        // The warning and the sweep share this full length; neither stops at tiles.
        return MaxLength;
    }
    internal static void DrawLine(Vector2 start, Vector2 end, Color color, float width, float opacity)
    {
        Vector2 distance = end - start;
        if (distance.LengthSquared() < 1f) return;
        Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, start - Main.screenPosition,
            new Rectangle(0, 0, 1, 1), color * opacity, distance.ToRotation(), new Vector2(0f, .5f),
            new Vector2(distance.Length(), width), SpriteEffects.None);
    }
}
