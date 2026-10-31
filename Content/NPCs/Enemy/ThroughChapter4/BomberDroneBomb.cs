using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;
using ArknightsMod.Content.Projectiles.Supporter.Magallan;
using ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;
using MaterialistBoss = ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer.MaterialistAntagonizer;

namespace ArknightsMod.Content.NPCs.Enemy.ThroughChapter4;

// Ownership follows the encounter token, so recycled NPC slots cannot inherit a live payload.
public abstract class BomberDronePayload : ModProjectile
{
    internal int OwnerIndex = -1, Encounter;
    protected bool Cancelled;
    protected static bool Authority => Main.netMode != NetmodeID.MultiplayerClient;
    internal void Bind(int ownerIndex, int encounter) { OwnerIndex = ownerIndex; Encounter = encounter; Projectile.netUpdate = true; }
    internal bool BelongsTo(MaterialistBoss boss) => Encounter > 0 && OwnerIndex == boss.NPC.whoAmI && Encounter == boss.Encounter;
    internal bool CombatValid => Encounter == 0 || MaterialistBoss.Owner(OwnerIndex, Encounter) is { Phase: 3, Paused: false };
    internal void Cancel() { Cancelled = true; Projectile.Kill(); }
    public override void SendExtraAI(BinaryWriter writer) { writer.Write(OwnerIndex); writer.Write(Encounter); }
    public override void ReceiveExtraAI(BinaryReader reader) { OwnerIndex = reader.ReadInt32(); Encounter = reader.ReadInt32(); }
}

public sealed class BomberDroneBomb : BomberDronePayload
{
    internal const float VisualScale = 3f, Gravity = .28f, InitialFallSpeed = 1.5f;
    private readonly ReaperSwarmVisualNode[] trails = new ReaperSwarmVisualNode[2];
    private ReaperSwarmVisualNode engine;
    private bool visualsStarted;
    private readonly PhoenixFxStyle style = PhoenixFx.Original(VisualScale);
    public override void SetDefaults()
    {
        Projectile.width = 18; Projectile.height = 24;
        Projectile.hostile = true; Projectile.friendly = false;
        Projectile.tileCollide = true; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.timeLeft = 180; Projectile.netImportant = true;
    }
    internal static Vector2 TrajectoryPoint(Vector2 origin, Vector2 velocity, float ticks) =>
        origin + velocity * ticks + new Vector2(0f, Gravity * ticks * (ticks + 1f) * .5f);
    private static float FallTicks(float height)
    {
        float b = InitialFallSpeed + Gravity * .5f;
        return Math.Clamp((-b + MathF.Sqrt(b * b + 2f * Gravity * Math.Max(0f, height))) / Gravity, 18f, 70f);
    }
    internal static (Vector2 Velocity, float Ticks) Plan(Vector2 origin, Vector2 target, Vector2 targetVelocity)
    {
        float ticks = FallTicks(target.Y - origin.Y);
        Vector2 prediction = target + new Vector2(MathHelper.Clamp(targetVelocity.X * ticks * .65f, -220f, 220f),
            MathHelper.Clamp(targetVelocity.Y * ticks * .35f, -80f, 80f));
        ticks = FallTicks(prediction.Y - origin.Y);
        return (new Vector2(MathHelper.Clamp((prediction.X - origin.X) / ticks, -10f, 10f), InitialFallSpeed), ticks);
    }
    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override void AI()
    {
        if (Authority && !CombatValid) { Cancel(); return; }
        Projectile.ai[2]++;
        Projectile.velocity.Y += Gravity;
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
        Projectile.rotation = direction.ToRotation() - MathHelper.PiOver2;
        if (Authority) {
            foreach (Player player in Main.ActivePlayers) {
                if (player.dead || player.ghost) continue;
                float collision = 0f;
                if (Collision.CheckAABBvLineCollision(player.position, player.Size, Projectile.Center,
                    Projectile.Center + Projectile.velocity, 18f, ref collision)) { Projectile.Kill(); return; }
            }
            if ((int)Projectile.ai[2] % 12 == 0) Projectile.netUpdate = true;
        }
        if (Main.dedServ) return;
        if (!visualsStarted) {
            visualsStarted = true;
            if (Projectile.ai[2] <= 3f) PhoenixFx.Burst("actor.player.weapon.66.muzzle", Projectile.Center, direction, style);
            for (int i = 0; i < trails.Length; i++) {
                float side = i == 0 ? -6f : 6f;
                trails[i] = ReaperSwarmVisuals.Spawn("trail.player.66",
                    () => Projectile.Center + new Vector2(side, -30f).RotatedBy(Projectile.rotation),
                    () => Projectile.velocity.SafeNormalize(Vector2.UnitY), active: () => Projectile.active,
                    scale: new Vector2(VisualScale), style: style);
            }
        }
        if (Projectile.ai[2] >= 6f && engine == null)
            engine = ReaperSwarmVisuals.Spawn("actor.player.weapon.66.activation", () => Projectile.Center,
                () => Projectile.velocity.SafeNormalize(Vector2.UnitY), active: () => Projectile.active,
                scale: new Vector2(VisualScale), style: style);
        Lighting.AddLight(Projectile.Center, .7f, .3f, .06f);
    }
    public override void OnKill(int timeLeft)
    {
        foreach (var trail in trails) trail?.Stop();
        engine?.Stop();
        if (!Authority || Cancelled || !CombatValid) return;
        int index = Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
            ModContent.ProjectileType<BomberDroneExplosion>(), Projectile.damage, Projectile.knockBack, Main.myPlayer,
            Projectile.velocity.ToRotation());
        if (Main.projectile.IndexInRange(index) && Main.projectile[index].ModProjectile is BomberDroneExplosion explosion)
            explosion.Bind(OwnerIndex, Encounter);
    }
}

public sealed class BomberDroneExplosion : BomberDronePayload
{
    internal const int BlastSize = 500;
    private readonly bool[] hitPlayers = new bool[Main.maxPlayers];
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 700;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = BlastSize;
        Projectile.hostile = true; Projectile.friendly = false;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.timeLeft = 24; Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => !Cancelled && CombatValid && Projectile.timeLeft >= 20 ? null : false;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPlayer(Player target) => !hitPlayers[target.whoAmI];
    public override void OnHitPlayer(Player target, Player.HurtInfo info) => hitPlayers[target.whoAmI] = true;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => projHitbox.Intersects(targetHitbox);
    public override void AI()
    {
        if (Authority && !CombatValid) { Cancel(); return; }
        if (Projectile.localAI[0]++ != 0 || Main.dedServ) return;
        PhoenixFx.Burst("emitter.player.weapon.66.impact", Projectile.Center,
            Projectile.ai[0].ToRotationVector2(), PhoenixFx.Original(BomberDroneBomb.VisualScale));
        MagallanVisuals.ExplosionSmoke(Projectile.Center, BlastSize * .5f);
        MaterialistEffects.Pulse(Projectile.Center, BlastSize * .5f, new Color(255, 130, 35), 3);
        SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1f, Pitch = -.3f, MaxInstances = 4 }, Projectile.Center);
    }
    public override bool PreDraw(ref Color lightColor) => false;
}
