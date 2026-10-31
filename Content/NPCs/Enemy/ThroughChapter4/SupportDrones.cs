using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Utilities;
using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Projectiles.Supporter.Magallan;
using ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

namespace ArknightsMod.Content.NPCs.Enemy.ThroughChapter4;

public abstract class SupportDrone : ModNPC
{
    protected static bool Authority => Main.netMode != NetmodeID.MultiplayerClient;
    protected abstract int FrameCount { get; }
    protected abstract string BestiaryText { get; }
    protected Vector2 Home => new(NPC.ai[3], NPC.ai[2]);

    public override void SetStaticDefaults() => Main.npcFrameCount[Type] = FrameCount;

    public override void SetDefaults()
    {
        NPC.width = 40;
        NPC.height = 28;
        NPC.lifeMax = 240;
        NPC.defense = 8;
        NPC.damage = 0;
        NPC.value = Item.buyPrice(silver: 2);
        NPC.knockBackResist = .35f;
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.aiStyle = -1;
        NPC.HitSound = SoundID.NPCHit4;
        NPC.DeathSound = SoundID.NPCDeath14;
    }

    public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
    public override bool CanHitNPC(NPC target) => false;

    public override void OnSpawn(IEntitySource source)
    {
        if (!Authority) return;
        NPC.ai[2] = NPC.Center.Y;
        NPC.ai[3] = NPC.Center.X;
        NPC.direction = NPC.spriteDirection = Main.rand.NextBool() ? 1 : -1;
        NPC.netUpdate = true;
    }

    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry entry) => entry.Info.AddRange(
        new IBestiaryInfoElement[] {
            BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
            new FlavorTextBestiaryInfoElement(BestiaryText)
        });

    public override void ModifyNPCLoot(NPCLoot npcLoot)
    {
        npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<OrironShard>(), 2, 1, 3));
        npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Ester>(), 3, 1, 2));
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        if (!(NPC.downedBoss1 || NPC.downedBoss2 || NPC.downedBoss3 || NPC.downedQueenBee || Main.hardMode))
            return 0f;
        return SpawnCondition.OverworldDaySlime.Chance * .02f;
    }

    protected Player FindTarget()
    {
        NPC.TargetClosest(false);
        Player target = Main.player[NPC.target];
        return target.active && !target.dead && !target.ghost &&
            Vector2.DistanceSquared(NPC.Center, target.Center) < 1000f * 1000f ? target : null;
    }

    protected void FlyTo(Vector2 destination, float speed = 3.2f)
    {
        // Preserve the spawn altitude over pits, but rise to clear nearby ground and the target.
        float ground = NPC.Center.Y + 160f;
        Point tile = NPC.Center.ToTileCoordinates();
        for (int y = tile.Y; y < tile.Y + 18; y++) {
            if (!WorldGen.InWorld(tile.X, y, 10)) continue;
            Tile t = Framing.GetTileSafely(tile.X, y);
            if (t.HasTile && !t.IsActuated && Main.tileSolid[t.TileType] && !Main.tileSolidTop[t.TileType]) {
                ground = y * 16f;
                break;
            }
        }
        bool bound = NPC.TryGetGlobalNPC<MaterialistWing>(out var wing) && wing.Bound;
        destination.Y = Math.Min(destination.Y, Math.Min(bound ? destination.Y : Home.Y, ground - 96f));
        Vector2 desired = new(MathHelper.Clamp((destination.X - NPC.Center.X) * .035f, -speed, speed),
            MathHelper.Clamp((destination.Y - NPC.Center.Y) * .045f, -3f, 1.8f));
        NPC.velocity = Vector2.Lerp(NPC.velocity, desired, .085f);
        if (Math.Abs(NPC.velocity.X) > .25f) NPC.direction = NPC.spriteDirection = Math.Sign(NPC.velocity.X);
        NPC.rotation = MathHelper.Lerp(NPC.rotation, NPC.velocity.X * .018f, .12f);
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        Texture2D texture = TextureAssets.Npc[Type].Value;
        Main.EntitySpriteDraw(texture, NPC.Center - screenPos, NPC.frame, NPC.GetAlpha(drawColor), NPC.rotation,
            NPC.frame.Size() * .5f, NPC.scale,
            NPC.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
        return false;
    }
}

public sealed class FrostDrone : SupportDrone
{
    internal const float MistRadius = 200f;
    protected override int FrameCount => 4;
    protected override string BestiaryText => "寒霜：持续释放低温烟雾的控场无人机。冷雾会拖慢玩家，不造成伤害。";
    private Vector2 MistCenter => NPC.Center + new Vector2(0f, 68f);

    public override void AI()
    {
        Player target = FindTarget();
        NPC.ai[0]++;
        float lateral = NPC.TryGetGlobalNPC<MaterialistWing>(out var wing) && wing.Bound ? (wing.Slot % 2 == 0 ? -150f : 150f) : 0f;
        FlyTo(target == null ? Home + new Vector2(MathF.Sin(NPC.ai[0] / 100f) * 120f, 0f)
            : target.Center + new Vector2(lateral, -116f), 2.7f);

        // Apply the debuff independently of damage/immunity. Only the owning client changes its player buffs.
        if (!Main.dedServ && Main.LocalPlayer.active && !Main.LocalPlayer.dead &&
            MistTouches(MistCenter, Main.LocalPlayer.Hitbox) &&
            Collision.CanHitLine(MistCenter, 1, 1, Main.LocalPlayer.position, Main.LocalPlayer.width, Main.LocalPlayer.height))
            Main.LocalPlayer.AddBuff(ModContent.BuffType<FrostDroneChill>(), 30);

        if (!Main.dedServ && (int)NPC.ai[0] % 4 == 0) {
            MagallanVisuals.ColdMist(MistCenter, MistRadius, false);
            MagallanVisuals.Puff(NPC.Center + new Vector2(0f, 12f).RotatedBy(NPC.rotation),
                new Vector2(NPC.velocity.X * .2f, 1.8f), new Color(158, 211, 231), 46f, 38, .42f, .018f);
        }
    }

    internal static bool MistTouches(Vector2 center, Rectangle target)
    {
        Vector2 nearest = Vector2.Clamp(center, target.TopLeft(), target.BottomRight());
        return Vector2.DistanceSquared(center, nearest) <= MistRadius * MistRadius;
    }

    public override void FindFrame(int frameHeight)
    {
        if (NPC.GetGlobalNPC<global::ArknightsMod.Content.NPCs.Friendly.CannotSupportNPC>().IsSleeping) return;
        NPC.frame.Y = (int)(++NPC.frameCounter / 6) % FrameCount * frameHeight;
    }
}

public sealed class FrostDroneChill : ModBuff
{
    public override string Texture => $"Terraria/Images/Buff_{BuffID.Chilled}";
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = true;
        BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
    }
}

public sealed class FrostDroneChillPlayer : ModPlayer
{
    public override void PostUpdateRunSpeeds()
    {
        if (!Player.HasBuff<FrostDroneChill>()) return;
        Player.maxRunSpeed *= .5f;
        Player.accRunSpeed *= .5f;
        Player.runAcceleration *= .5f;
    }
}

public sealed class BomberDrone : SupportDrone
{
    private const int LaunchFrameTicks = 6;
    internal const int AimTicks = 72, LockTick = 42;
    private enum FlightState { Seeking, Aiming, Launching, Spent }
    private FlightState State { get => (FlightState)(int)NPC.ai[0]; set => NPC.ai[0] = (int)value; }
    private int Timer { get => (int)NPC.ai[1]; set => NPC.ai[1] = value; }
    protected override int FrameCount => 9;
    protected override string BestiaryText => "暴鸽：携带一枚大型爆破罐的无人机。预瞄线锁定后投下受重力影响的炸弹；每架只能投弹一次，随后空载飞离。";
    private Vector2 launchVelocity;
    private float flightTicks;
    private bool aimLocked;
    internal bool Spent => State == FlightState.Spent;
    internal bool PayloadReleased => State is FlightState.Launching or FlightState.Spent;
    internal Vector2 PlannedVelocity => launchVelocity;
    internal float PlannedFlightTicks => flightTicks;
    internal Vector2 BombMount => NPC.Center + new Vector2(0f, 15f).RotatedBy(NPC.rotation) * NPC.scale;
    internal int DrawFrame => State switch {
        FlightState.Seeking or FlightState.Aiming => (int)(NPC.frameCounter / 6) % 4,
        FlightState.Launching => 4 + Math.Clamp((Timer - 1) / LaunchFrameTicks, 0, 4),
        // The last two empty-rack frames keep the rotors moving without drawing a new bomb.
        _ => 7 + (int)(NPC.frameCounter / 6) % 2
    };

    public override void SetDefaults()
    {
        base.SetDefaults();
        NPC.lifeMax = 320;
        NPC.height = 32;
        NPC.value = Item.buyPrice(silver: 3);
    }

    public override void AI()
    {
        Player target = FindTarget();
        Timer++;
        switch (State) {
            case FlightState.Seeking:
                FlyTo(target == null ? Home : target.Center - new Vector2(0f, 176f), 3.8f);
                if (Authority && target != null && Timer >= 60 &&
                    Math.Abs(NPC.Center.X - target.Center.X) < 220f &&
                    NPC.Center.Y < target.Center.Y - 80f && NPC.Center.Y > target.Center.Y - 480f &&
                    Collision.CanHitLine(BombMount, 1, 1, target.position, target.width, target.height)) {
                    State = FlightState.Aiming;
                    Timer = 0;
                    aimLocked = false;
                    NPC.netUpdate = true;
                }
                break;
            case FlightState.Aiming:
                NPC.velocity = Vector2.Zero;
                if (!aimLocked && target != null) {
                    (launchVelocity, flightTicks) = BomberDroneBomb.Plan(BombMount, target.Center, target.velocity);
                    if (Authority && Timer >= LockTick) { aimLocked = true; NPC.netUpdate = true; }
                    else if (Authority && Timer % 6 == 0) NPC.netUpdate = true;
                }
                if (Authority && target == null && !aimLocked) {
                    State = FlightState.Seeking;
                    Timer = 0;
                    NPC.netUpdate = true;
                }
                else if (Authority && aimLocked && Timer >= AimTicks) {
                    int index = Projectile.NewProjectile(NPC.GetSource_FromAI(), BombMount, launchVelocity,
                        ModContent.ProjectileType<BomberDroneBomb>(), 40, 5f, Main.myPlayer);
                    if (Main.projectile.IndexInRange(index) && Main.projectile[index].ModProjectile is BomberDroneBomb bomb) {
                        if (NPC.TryGetGlobalNPC<MaterialistWing>(out var owner) && owner.Bound)
                            bomb.Bind(owner.OwnerIndex, owner.Encounter);
                        Main.projectile[index].netUpdate = true;
                        State = FlightState.Launching;
                        Timer = 0;
                        NPC.direction = NPC.spriteDirection = target != null && NPC.Center.X != target.Center.X
                            ? Math.Sign(NPC.Center.X - target.Center.X) : NPC.direction == 0 ? 1 : NPC.direction;
                        UpdateEvacuation();
                        NPC.netUpdate = true;
                    }
                }
                break;
            case FlightState.Launching:
                UpdateEvacuation();
                if (Authority && Timer >= 5 * LaunchFrameTicks) {
                    State = FlightState.Spent;
                    Timer = 0;
                    NPC.netUpdate = true;
                }
                break;
            case FlightState.Spent:
                // Never re-enter Seeking: this airframe carries exactly one payload for its entire life.
                UpdateEvacuation();
                bool nearbyPlayer = false;
                foreach (Player player in Main.ActivePlayers)
                    if (!player.dead && !player.ghost && Vector2.DistanceSquared(NPC.Center, player.Center) < 1600f * 1600f)
                    { nearbyPlayer = true; break; }
                if (Authority && (Timer >= 180 || Timer >= 45 && !nearbyPlayer)) {
                    NPC.active = false;
                    NPC.netUpdate = true;
                }
                break;
        }
    }

    private void UpdateEvacuation()
    {
        // Keep the release-side heading committed; changing targets must not turn an empty bomber back.
        NPC.velocity = Vector2.Lerp(NPC.velocity, new Vector2(NPC.direction * 18f, -9f), .13f);
        NPC.rotation = MathHelper.Lerp(NPC.rotation, NPC.direction * .18f, .12f);
        NPC.spriteDirection = NPC.direction;
    }

    public override void FindFrame(int frameHeight)
    {
        if (NPC.GetGlobalNPC<global::ArknightsMod.Content.NPCs.Friendly.CannotSupportNPC>().IsSleeping) return;
        NPC.frameCounter++;
        NPC.frame.Y = DrawFrame * frameHeight;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(launchVelocity.X); writer.Write(launchVelocity.Y);
        writer.Write(flightTicks); writer.Write(aimLocked);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        launchVelocity = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        flightTicks = reader.ReadSingle(); aimLocked = reader.ReadBoolean();
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (State == FlightState.Aiming && flightTicks > 0f) {
            Color color = aimLocked ? new Color(255, 100, 55) : new Color(255, 195, 85);
            Vector2 previous = BombMount;
            for (float tick = 2; tick <= Math.Min(100f, flightTicks + 24f); tick += 2f) {
                Vector2 next = BomberDroneBomb.TrajectoryPoint(BombMount, launchVelocity, tick);
                MaterialistVisuals.Line(previous, next, color * (aimLocked ? .8f : .5f), aimLocked ? 2f : 1.2f);
                if (Collision.SolidCollision(next - new Vector2(9f, 12f), 18, 24)) break;
                previous = next;
            }
        }
        return base.PreDraw(spriteBatch, screenPos, drawColor);
    }
}
