using System;
using ArknightsMod.Content.Items.Weapons.Medic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Medic.Arts;

public sealed class MedicArt : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_0";
    internal int ProfileId => Math.Clamp((int)Projectile.ai[0], 0, MedicCatalog.Profiles.Length - 1);
    internal MedicProfile Profile => MedicCatalog.Get(ProfileId);
    internal float Age;
    private bool deployed;
    private bool grounded;
    private Vector2 previous;
    internal Rectangle PatientHitbox => Utils.CenteredRectangle(Projectile.Center, new Vector2(Profile.PatientContactSize));
    internal bool CanTreatPlayers => Age > Profile.ArmingDelay;
    internal float Armed => MathHelper.Clamp((Age - Profile.ArmingDelay + 8) / 8f, 0, 1);
    internal float HomingStrength => Profile.Branch == MedicBranch.Therapist && Age >= Profile.ArmingDelay
        ? .35f + .65f * Utils.GetLerpValue(Profile.ArmingDelay, Profile.ArmingDelay + 24f, Age, true) : 0f;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 24;
        ProjectileID.Sets.TrailingMode[Type] = 2;
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 192;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 24;
        Projectile.friendly = Projectile.hostile = false;
        Projectile.damage = 0;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1;
        Projectile.extraUpdates = 1;
        Projectile.timeLeft = 720;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
    }
    public override bool? CanDamage() => false;
    public override bool? CanHitNPC(NPC target) => false;
    public override bool CanHitPvp(Player target) => false;
    public override void AI()
    {
        float step = 1f / Projectile.MaxUpdates;
        if (Age == 0)
        {
            // First AI runs on every observing client; Shoot only runs for the owner.
            MedicArtsAudio.Play(Projectile.Center, ProfileId, MedicSoundCue.Cast);
            previous = Projectile.Center;
            Projectile.ai[1] = Math.Clamp(Projectile.ai[1], 1f, 3f);
            Projectile.ai[2] = Math.Clamp(Projectile.ai[2], 20, 1800);
        }
        Age += step;
        if (grounded)
        {
            Projectile.velocity = Vector2.Zero;
            if (CanTreatPlayers) DeployField();
            return;
        }
        // Actual gravity remains present even during therapist steering.
        Projectile.velocity.Y = Math.Min(10, Projectile.velocity.Y + Profile.Gravity * step);
        if (Age > 18) Projectile.velocity.X *= MathF.Pow(.995f, step);
        if (Profile.Branch == MedicBranch.Therapist && Age >= Profile.ArmingDelay)
            HomeTowardPatient(step);
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (Projectile.numUpdates == 0)
        {
            MedicArtsVfx.Flight(this, previous);
            previous = Projectile.Center;
        }
        if (!CanTreatPlayers || Main.netMode == NetmodeID.MultiplayerClient) return;
        if (!Main.player.IndexInRange(Projectile.owner)) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead) { Projectile.Kill(); return; }
        if (Profile.Branch == MedicBranch.MultiTarget)
        {
            foreach (NPC npc in Main.ActiveNPCs)
                if (npc.CanBeChasedBy() && Projectile.Hitbox.Intersects(npc.Hitbox)) { DeployField(); return; }
        }
        foreach (Player target in Main.ActivePlayers)
        {
            if (!MedicHealingNet.Allied(owner, target) || !PatientHitbox.Intersects(target.Hitbox)) continue;
            if (Profile.Branch == MedicBranch.MultiTarget) { DeployField(); return; }
            if (MedicTreatment.TryTreat(owner, target, ProfileId, Projectile.ai[1], (int)Projectile.ai[2]))
            {
                Projectile.Kill(); return;
            }
        }
    }
    private void HomeTowardPatient(float step)
    {
        Player target = FindPatient();
        if (target == null)
        {
            float drift = MathF.Sin((Age + Projectile.identity * 5f) * .08f) * .004f;
            Projectile.velocity = Projectile.velocity.RotatedBy(drift * step) * MathF.Pow(.992f, step);
            return;
        }
        Vector2 direction = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
        float warmup = Utils.GetLerpValue(Profile.ArmingDelay, Profile.ArmingDelay + 36f, Age, true);
        float close = Utils.GetLerpValue(360f, 70f, Projectile.Distance(target.Center), true);
        float pull = MathHelper.Lerp(.35f, 1f, Math.Max(warmup, close * .75f));
        Vector2 desired = direction * Profile.Speed * MathHelper.Lerp(.65f, .88f, pull);
        // Retain momentum through broad bends. Normalize inertia to real ticks despite extraUpdates.
        float inertia = MathHelper.Lerp(32f, 24f, close);
        float steering = 1f - MathF.Pow(inertia / (inertia + 1f), step * pull);
        Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, steering);
        float sway = MathF.Sin((Age + Projectile.identity * 7f) * .075f) * MathHelper.Lerp(.008f, .003f, pull);
        Projectile.velocity = Projectile.velocity.RotatedBy(sway * step);
        float maxSpeed = Profile.Speed * .98f;
        if (Projectile.velocity.LengthSquared() > maxSpeed * maxSpeed)
            Projectile.velocity = Projectile.velocity.SafeNormalize(direction) * maxSpeed;
    }
    private Player FindPatient()
    {
        if (!Main.player.IndexInRange(Projectile.owner)) return null;
        Player owner = Main.player[Projectile.owner], result = null;
        float best = 640 * 640;
        foreach (Player player in Main.ActivePlayers)
        {
            if (!MedicHealingNet.Allied(owner, player) || player.statLife >= player.statLifeMax2) continue;
            float distance = player.DistanceSQ(Projectile.Center);
            if (distance < best) { best = distance; result = player; }
        }
        return result;
    }
    private void DeployField()
    {
        if (deployed || Main.netMode == NetmodeID.MultiplayerClient) return;
        deployed = true;
        // A world-space landing effect inherits the original shot's arming delay.
        int fieldIndex = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
            ModContent.ProjectileType<MedicHealingField>(), 0, 0, Projectile.owner,
            ProfileId, Projectile.ai[1], Projectile.ai[2]);
        if (Main.projectile.IndexInRange(fieldIndex))
        {
            // NewProjectile only initializes ai for the local owner in this tML environment.
            // Fields originate on the authority, so explicitly install and publish the snapshot.
            Projectile field = Main.projectile[fieldIndex];
            field.ai[0] = ProfileId;
            field.ai[1] = Projectile.ai[1];
            field.ai[2] = Projectile.ai[2];
            field.netUpdate = true;
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, fieldIndex);
        }
        Projectile.Kill();
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (Profile.Branch == MedicBranch.MultiTarget)
        {
            if (!CanTreatPlayers)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.tileCollide = false;
                grounded = true;
                return false;
            }
            DeployField();
            return false;
        }
        return true;
    }
    public override void OnKill(int timeLeft)
    {
        MedicArtsAudio.Play(Projectile.Center, ProfileId, MedicSoundCue.Release);
        MedicArtsVfx.Dissolve(Projectile.Center, Projectile.velocity, ProfileId, deployed);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        MedicArtsVfx.DrawArt(this); return false;
    }
}

public sealed class MedicHealingField : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_0";
    internal int ProfileId => Math.Clamp((int)Projectile.ai[0], 10, 13);
    internal MedicProfile Profile => MedicCatalog.Get(ProfileId);
    internal int Age;
    internal float Visibility => Math.Min(MathHelper.Clamp(Age / 14f, 0, 1), MathHelper.Clamp(Projectile.timeLeft / 40f, 0, 1));
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 256;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 4;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 1500;
        Projectile.netImportant = true;
    }
    public override bool? CanDamage() => false;
    public override bool? CanHitNPC(NPC target) => false;
    public override bool CanHitPvp(Player target) => false;
    public override void AI()
    {
        if (Age++ == 0)
        {
            MedicArtsAudio.Play(Projectile.Center, ProfileId, MedicSoundCue.FieldOpen);
            Projectile.timeLeft = Profile.FieldDuration;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Bound overlapping fields from one caster without limiting the team's player count.
                int count = 0; Projectile oldest = null;
                foreach (Projectile other in Main.ActiveProjectiles)
                    if (other.owner == Projectile.owner && other.type == Type && other.whoAmI != Projectile.whoAmI && other.timeLeft > 40)
                    {
                        count++;
                        if (oldest == null || other.timeLeft < oldest.timeLeft) oldest = other;
                    }
                if (count >= 2 && oldest != null) { oldest.timeLeft = 40; oldest.netUpdate = true; }
            }
        }
        if (Age >= 14 && Age % 90 == 14)
            MedicArtsAudio.Play(Projectile.Center, ProfileId, MedicSoundCue.FieldPulse);
        MedicArtsVfx.FieldAmbient(this);
        if (Main.netMode == NetmodeID.MultiplayerClient || Age < 14 || Age % 15 != 0
            || !Main.player.IndexInRange(Projectile.owner)) return;
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead) { Projectile.Kill(); return; }
        foreach (Player patient in Main.ActivePlayers)
            if (patient.DistanceSQ(Projectile.Center) <= Profile.FieldRadius * Profile.FieldRadius)
                MedicTreatment.TryTreat(owner, patient, ProfileId, Math.Clamp(Projectile.ai[1], 1, 3),
                    Math.Clamp((int)Projectile.ai[2], 20, 1800));
    }
    public override void OnKill(int timeLeft) => MedicArtsAudio.Play(Projectile.Center, ProfileId, MedicSoundCue.FieldClose);
    public override bool PreDraw(ref Color lightColor)
    {
        MedicArtsVfx.DrawField(this); return false;
    }
}
