using System.IO;
using ArknightsMod.Content.Buffs;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Players;

namespace ArknightsMod.Content.Projectiles.Caster.Mostima;

public sealed class MostimaCrossBurst : ModProjectile
{
    private Vector2 followTarget;
    private float goldTransition, expansionTransition;
    private bool beatThisTick;
    internal float GoldTransition => goldTransition;
    internal float Expansion => MathHelper.SmoothStep(0f, 1f, expansionTransition);
    internal float ClockScale => 1f + .3f * Expansion;
    internal int Mode => (int)Projectile.ai[2];
    internal static bool CrossesBeat(float previous, float next)
        => next >= MostimaClockwork.MoveTicks
           && System.MathF.Floor((next - MostimaClockwork.MoveTicks) / MostimaClockwork.BeatTicks)
                > System.MathF.Floor((previous - MostimaClockwork.MoveTicks) / MostimaClockwork.BeatTicks);
    // Previous fully charged tracking was Lerp(..., .025f): exactly 1.2 times that response.
    internal static Vector2 Follow(Vector2 center, Vector2 target) => Vector2.Lerp(center, target, .03f);
    public override bool ShouldUpdatePosition() => false;
    public override void OnSpawn(Terraria.DataStructures.IEntitySource source) => followTarget = Projectile.Center;
    public override void SendExtraAI(BinaryWriter writer) { writer.Write(followTarget.X); writer.Write(followTarget.Y); }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Vector2 point = new(reader.ReadSingle(), reader.ReadSingle());
        if (float.IsFinite(point.X) && float.IsFinite(point.Y)) followTarget = point;
    }
    internal int Age => (int)Projectile.ai[0];
    internal float Visibility => MathHelper.Clamp(Age / 12f, 0f, 1f) * (1f - Projectile.ai[1] / MostimaClockwork.FadeTicks);
    private static readonly SoundStyle AppearSound = new("ArknightsMod/Sounds/MostimaClockAppear") { Volume = .7f, MaxInstances = 2 };
    private static readonly SoundStyle StrikeSound = new("ArknightsMod/Sounds/MostimaClockStrike") { Volume = .6f, MaxInstances = 2, SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest };
    public override string Texture => MostimaVisuals.Assets + "ChronoCore";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = (int)(MostimaClockwork.BlastRadius * 2f);
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 2;
        Projectile.netImportant = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 9;
    }
    // Damage is enabled for ONE frame per explosion, never throughout the afterglow.
    public override bool? CanDamage() => Projectile.ai[1] == 0 && beatThisTick ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        => Vector2.DistanceSquared(targetHitbox.ClosestPointInRect(Projectile.Center), Projectile.Center)
            <= MostimaClockwork.BlastRadius * ClockScale * MostimaClockwork.BlastRadius * ClockScale;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead) { Projectile.Kill(); return; }
        Projectile.timeLeft = 2;
        if (Projectile.owner == Main.myPlayer)
        {
            WeaponPlayer skills = owner.GetModPlayer<WeaponPlayer>();
            int mode = skills.SkillActive && skills.CurrentSkill?.Key.Item == nameof(global::ArknightsMod.Content.Items.Weapons.Caster.Mostima.MostimaStaff)
                ? skills.Skill + 1 : 0;
            if (Mode != mode) { Projectile.ai[2] = mode; Projectile.netUpdate = true; }
        }
        goldTransition = MathHelper.Clamp(goldTransition + (Mode == 2 ? 1f : -1f) / 30f, 0f, 1f);
        expansionTransition = MathHelper.Clamp(expansionTransition + (Mode == 3 ? 1f : -1f) / 60f, 0f, 1f);
        if (Projectile.owner == Main.myPlayer && Projectile.ai[1] == 0f
            && (!owner.channel || owner.noItems || owner.CCed || owner.HeldItem.ModItem is not global::ArknightsMod.Content.Items.Weapons.Caster.Mostima.MostimaStaff))
        { Projectile.ai[1] = 1f; Projectile.netUpdate = true; }
        else if (Projectile.ai[1] > 0f)
        {
            Projectile.ai[1]++;
            if (Projectile.ai[1] > MostimaClockwork.FadeTicks) { Projectile.Kill(); return; }
        }
        float previousTime = Projectile.ai[0];
        Projectile.ai[0] += 1f + .3f * Expansion;
        beatThisTick = Projectile.ai[1] == 0f && CrossesBeat(previousTime, Projectile.ai[0]);
        if (beatThisTick && Projectile.owner == Main.myPlayer
            && !owner.CheckMana(owner.HeldItem, 30, true, false))
        {
            Projectile.ai[1] = 1f;
            Projectile.netUpdate = true;
            beatThisTick = false;
        }
        int diameter = (int)System.MathF.Round(MostimaClockwork.BlastRadius * 2f * ClockScale);
        if (Projectile.width != diameter || Projectile.height != diameter)
        {
            Vector2 center = Projectile.Center;
            Projectile.width = Projectile.height = diameter;
            Projectile.Center = center;
        }
        if (Projectile.ai[1] == 0f)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                followTarget = global::ArknightsMod.Content.Items.Weapons.Caster.Mostima.MostimaStaff.Target(owner);
                if (Age % 5 == 0) Projectile.netUpdate = true;
            }
            Projectile.Center = Follow(Projectile.Center, followTarget);
        }
        if (beatThisTick
            && owner.HeldItem.ModItem is global::ArknightsMod.Content.Items.Weapons.Caster.Mostima.MostimaStaff)
            Projectile.damage = System.Math.Max(1, (int)(owner.GetWeaponDamage(owner.HeldItem) * (Mode == 2 ? .8f : 1f)));
        // Per-player uniqueness also protects against duplicated spawn requests.
        foreach (Projectile clock in Main.ActiveProjectiles)
            if (clock.type == Type && clock.owner == Projectile.owner && clock.identity < Projectile.identity)
            { Projectile.Kill(); return; }
        if (Main.dedServ) return;
        if (Projectile.localAI[0] == 0f)
        { Projectile.localAI[0] = 1f; SoundEngine.PlaySound(AppearSound, Projectile.Center); }
        Projectile.localAI[1]++;
        if (!SpreadVfxBudget.Visible(Projectile.Center, MostimaClockwork.BlastRadius * ClockScale + 40f)) return;
        if (Projectile.ai[1] == 0f) MostimaGrandClockVfx.Tick(Projectile, (int)Projectile.localAI[1], ClockScale, goldTransition);
        if (beatThisTick)
        {
            SoundEngine.PlaySound(StrikeSound, Projectile.Center);
            // Damage/sound mark the hand arrival; the visual stream never restarts here.
        }
        Lighting.AddLight(Projectile.Center, .1f, .3f, .5f);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!target.boss)
        {
            if (Mode == 2) MostimaTimeStopNPC.Apply(target, 90);
            else if (target.realLife < 0) OperatorStunNPC.TryApply(target, 20, showIndicator: false);
        }
        // One visual event per actual hit. The owner spawns it; projectile sync lets
        // every observer emit their own local three-system particles exactly once.
        if (Projectile.owner == Main.myPlayer)
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
                ModContent.ProjectileType<MostimaClockHit>(), 0, 0f, Projectile.owner,
                MathHelper.Clamp(System.Math.Max(target.width, target.height) * .5f, 16f, 64f), goldTransition);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        MostimaVisuals.Burst(Projectile.Center, Projectile.ai[0], 1f - Projectile.ai[1] / MostimaClockwork.FadeTicks,
            ClockScale, goldTransition, Projectile.localAI[1]);
        return false;
    }
}
