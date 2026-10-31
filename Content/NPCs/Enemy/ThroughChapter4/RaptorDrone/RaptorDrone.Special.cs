using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

namespace ArknightsMod.Content.NPCs.Enemy.ThroughChapter4.RaptorDrone;

public partial class RaptorDrone
{
    internal const int SpecialChargeTicks = 90, SpecialAimLockTick = 60, SpecialStrafeTicks = 180;
    private int ordinaryRounds, specialHeading = 1;
    private float specialAim;
    private Vector2 specialOrigin;
    private Vector2 SpecialMuzzle => NPC.Center + new Vector2(27f * NPC.spriteDirection, 14f).RotatedBy(NPC.rotation);
    internal static float NaturalSpawnWeight(bool plantera, bool dungeon) => plantera && dungeon ? .035f : 0f;
    public override float SpawnChance(NPCSpawnInfo info) => NaturalSpawnWeight(NPC.downedPlantBoss,
        info.Player.ZoneDungeon && Main.wallDungeon[Framing.GetTileSafely(info.SpawnTileX, info.SpawnTileY).WallType]);

    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry entry) => entry.Info.AddRange(
        new IBestiaryInfoElement[] {
            BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.TheDungeon,
            new FlavorTextBestiaryInfoElement("威龙：世纪之花后出没于地牢的无人机。三轮常规射击后蓄力，以金色曳光弹持续扫射并缓慢平移。")
        });

    private void BeginSpecial(Player target)
    {
        State = AiState.SpecialCharge;
        StateTimer = 0;
        specialHeading = target.Center.X >= NPC.Center.X ? 1 : -1;
        NPC.direction = NPC.spriteDirection = specialHeading;
        NPC.velocity = Vector2.Zero;
        specialOrigin = NPC.Center;
        specialAim = (target.Center - SpecialMuzzle).SafeNormalize(new Vector2(specialHeading, 0f)).ToRotation();
        NPC.netUpdate = true;
    }

    private void UpdateSpecialCharge()
    {
        NPC.velocity = Vector2.Zero;
        StateTimer++;
        if (!TryGetTargetPlayer(out Player target)) { if (HasAuthority) EnterIdle(); return; }
        if (StateTimer <= SpecialAimLockTick && HasAuthority) {
            specialAim = (target.Center - SpecialMuzzle).SafeNormalize(new Vector2(specialHeading, 0f)).ToRotation();
            if ((int)StateTimer % 6 == 0) NPC.netUpdate = true;
        }
        if (!Main.dedServ && StateTimer == 1)
            SoundEngine.PlaySound(SoundID.Item93 with { Volume = .55f, Pitch = -.45f, MaxInstances = 3 }, NPC.Center);
        if (HasAuthority && StateTimer >= SpecialChargeTicks) {
            State = AiState.SpecialStrafe;
            StateTimer = 0;
            NPC.netUpdate = true;
        }
    }

    internal static float SpecialSweepAngle(float centerAngle, int ticks) =>
        centerAngle + MathHelper.ToRadians(25f) * MathF.Sin(MathHelper.TwoPi * ticks / 120f);

    private void UpdateSpecialStrafe()
    {
        if (!TryGetTargetPlayer(out _)) { if (HasAuthority) EnterIdle(); return; }
        StateTimer++;
        // The gun sweeps the locked lane; the airframe slowly traverses it without chasing the player.
        NPC.direction = NPC.spriteDirection = specialHeading;
        float speed = .65f + .45f * MathF.Sin(MathHelper.Pi * Math.Clamp(StateTimer / SpecialStrafeTicks, 0f, 1f));
        NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, specialHeading * speed, .08f);
        NPC.velocity.Y = 0;
        NPC.Center = new Vector2(NPC.Center.X, specialOrigin.Y);
        if (!Main.dedServ && (int)StateTimer % 6 == 0)
            SoundEngine.PlaySound(SoundID.Item11 with { Volume = .5f, Pitch = -.1f, MaxInstances = 3 }, SpecialMuzzle);
        if (HasAuthority && StateTimer <= SpecialStrafeTicks) {
            Vector2 velocity = SpecialSweepAngle(specialAim, (int)StateTimer).ToRotationVector2() * 10f;
            Projectile.NewProjectile(NPC.GetSource_FromAI(), SpecialMuzzle, velocity,
                ModContent.ProjectileType<RaptorGoldenRound>(), 22, 1f, Main.myPlayer, SpecialMuzzle.X, SpecialMuzzle.Y);
        }
        if (HasAuthority && StateTimer >= SpecialStrafeTicks) {
            ordinaryRounds = 0;
            EnterIdle();
        }
    }

    private void DrawSpecialWarning()
    {
        if (State != AiState.SpecialCharge) return;
        Vector2 muzzle = SpecialMuzzle;
        float fade = .25f + .5f * MathHelper.Clamp(StateTimer / SpecialChargeTicks, 0f, 1f);
        MaterialistVisuals.Glow(muzzle, new Color(255, 192, 65) * fade, new Vector2(28f + 18f * fade));
        for (int side = -1; side <= 1; side++) {
            Vector2 direction = (specialAim + side * MathHelper.ToRadians(25f)).ToRotationVector2();
            float[] samples = new float[3];
            Collision.LaserScan(muzzle, direction, 4f, 1100f, samples);
            float length = (samples[0] + samples[1] + samples[2]) / 3f;
            MaterialistVisuals.Line(muzzle, muzzle + direction * length,
                new Color(255, 194, 65) * (side == 0 ? fade : fade * .45f), side == 0 ? 1.8f : 1f);
        }
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(ordinaryRounds); writer.Write(specialHeading); writer.Write(specialAim);
        writer.Write(specialOrigin.X); writer.Write(specialOrigin.Y);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        ordinaryRounds = reader.ReadInt32(); specialHeading = reader.ReadInt32(); specialAim = reader.ReadSingle();
        specialOrigin = new Vector2(reader.ReadSingle(), reader.ReadSingle());
    }
}

// Standalone hostile projectile: shares the carrier's ballistic artwork, without requiring a carrier owner.
public sealed class RaptorGoldenRound : ModProjectile
{
    internal const int MotionUpdates = 8;
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Bullet;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 6;
        Projectile.hostile = true; Projectile.friendly = false;
        Projectile.tileCollide = true; Projectile.ignoreWater = true;
        Projectile.penetrate = 1; Projectile.extraUpdates = MotionUpdates - 1;
        Projectile.timeLeft = 40 * MotionUpdates;
    }
    public override bool? CanCutTiles() => false;
    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (!Main.dedServ && Projectile.numUpdates == 0) {
            MaterialistBallistics.Wake(Projectile, true, new Vector2(Projectile.ai[0], Projectile.ai[1]));
            Lighting.AddLight(Projectile.Center, .5f, .3f, .05f);
        }
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Vector2 normal = Projectile.velocity.Y != oldVelocity.Y ? new Vector2(0, -Math.Sign(oldVelocity.Y))
            : new Vector2(-Math.Sign(oldVelocity.X), 0);
        MaterialistBallistics.Impact(Projectile.Center, normal, true);
        return true;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        MaterialistBallistics.DrawRound(Projectile, new Vector2(Projectile.ai[0], Projectile.ai[1]), true);
        return false;
    }
}
