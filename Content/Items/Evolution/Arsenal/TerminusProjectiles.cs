using System;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Evolution.Arsenal;

public abstract class EvolutionArsenalProjectile : ModProjectile
{
    public override string LocalizationCategory => "Projectiles.EvolutionArsenal";
    public override string Texture => "ArknightsMod/Common/Particle/DefaultParticle";
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    internal virtual void DrawMaterial() { }
}

public sealed class TerminusMatrix : EvolutionArsenalProjectile
{
    internal float Age => Projectile.ai[0];
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12; Projectile.timeLeft = EvolutionArsenalRules.MatrixLife;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.penetrate = -1;
        Projectile.DamageType = DamageClass.Magic; Projectile.friendly = true;
    }
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead) { Projectile.Kill(); return; }
        int tick = (int)++Projectile.ai[0];
        if (Projectile.owner == Main.myPlayer)
        {
            if (tick == 1)
                for (int i = 0; i < EvolutionArsenalRules.CoreCount; i++)
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + EvolutionArsenalRules.CoreOffset(i, tick), Vector2.Zero,
                        ModContent.ProjectileType<TerminusOrb>(), EvolutionArsenalRules.CoreDamage(Projectile.damage), Projectile.knockBack * .2f,
                        Projectile.owner, Projectile.identity + 1, i);
            for (int i = 0; i < EvolutionArsenalRules.CoreCount; i++)
            {
                if (tick != EvolutionArsenalRules.CoreFireTick(i)) continue;
                Vector2 origin = Projectile.Center + EvolutionArsenalRules.CoreOffset(i, tick);
                NPC target = EvolutionArsenalRules.Target(origin, EvolutionArsenalRules.TerminusSearchRange, owner);
                if (target == null) continue;
                Vector2 direction = (target.Center - origin).SafeNormalize(Vector2.UnitX);
                float length = EvolutionArsenalRules.ClipRay(origin, direction, target, out int hitNpc);
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), origin, direction,
                    ModContent.ProjectileType<TerminusRay>(), Projectile.damage, Projectile.knockBack, Projectile.owner,
                    length, 0, hitNpc + 1);
            }
            if (tick == 54)
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<TerminusBloom>(), (int)(Projectile.damage * 1.2f), 4, Projectile.owner);
        }
        if (!Main.dedServ)
        {
            if (tick == 1) SoundEngine.PlaySound(SoundID.Item117 with { Volume = .65f, Pitch = -.3f }, Projectile.Center);
            for (int i = 0; i < EvolutionArsenalRules.CoreCount; i++)
            {
                if (tick != EvolutionArsenalRules.CoreFireTick(i)) continue;
                Vector2 core = Projectile.Center + EvolutionArsenalRules.CoreOffset(i, tick);
                EvolutionWeaponVisuals.Sparks(core, 7, 3);
                SoundEngine.PlaySound(SoundID.Item33 with { Volume = .3f, Pitch = .2f }, core);
            }
            if (tick % 4 == 0 && tick < 54)
            {
                Vector2 offset = Main.rand.NextVector2Unit() * Main.rand.NextFloat(32, 90);
                OperatorTextureEffects.Mote(Projectile.Center + offset, -offset * .045f, EvolutionWeaponVisuals.Blood, 6, 20);
            }
            Lighting.AddLight(Projectile.Center, new Vector3(.6f, .035f, .08f));
        }
    }
    public override bool PreDraw(ref Color lightColor) { EvolutionWeaponVisuals.MatrixDetails(this); return false; }
    internal override void DrawMaterial() => EvolutionWeaponVisuals.MatrixMaterial(this);
}

// Each visible core owns its own immunity clock; hitting never consumes the core or its laser.
public sealed class TerminusOrb : EvolutionArsenalProjectile
{
    private int matrixSlot = -1, missingTicks;
    internal TerminusMatrix Matrix
    {
        get
        {
            bool Valid(int slot) => slot >= 0 && slot < Main.maxProjectiles && Main.projectile[slot].active &&
                Main.projectile[slot].owner == Projectile.owner && Main.projectile[slot].identity == (int)Projectile.ai[0] - 1 &&
                Main.projectile[slot].ModProjectile is TerminusMatrix;
            if (Valid(matrixSlot)) return (TerminusMatrix)Main.projectile[matrixSlot].ModProjectile;
            for (int i = 0; i < Main.maxProjectiles; i++)
                if (Valid(i)) { matrixSlot = i; return (TerminusMatrix)Main.projectile[i].ModProjectile; }
            return null;
        }
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 28; Projectile.timeLeft = EvolutionArsenalRules.MatrixLife;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Magic; Projectile.penetrate = -1;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = EvolutionArsenalRules.CoreHitCooldown;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Matrix is TerminusMatrix matrix && EvolutionArsenalRules.CoreActive((int)Projectile.ai[1], matrix.Age);
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (Matrix is not TerminusMatrix matrix) return false;
        Vector2 head = matrix.Projectile.Center + EvolutionArsenalRules.CoreOffset((int)Projectile.ai[1], matrix.Age);
        Vector2 nearest = Vector2.Clamp(head, targetHitbox.TopLeft(), targetHitbox.BottomRight());
        return Vector2.DistanceSquared(head, nearest) <= 14 * 14;
    }
    public override void AI()
    {
        if (Matrix is not TerminusMatrix matrix)
        {
            // Network delivery may place the child before its matrix. It remains harmless while waiting.
            if (++missingTicks > 10) Projectile.Kill();
            return;
        }
        missingTicks = 0;
        if (matrix.Age < 1) return; // Initial matrix snapshots may precede its first local update.
        if (!EvolutionArsenalRules.CoreActive((int)Projectile.ai[1], matrix.Age)) { Projectile.Kill(); return; }
        Projectile.Center = matrix.Projectile.Center + EvolutionArsenalRules.CoreOffset((int)Projectile.ai[1], matrix.Age);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!Main.dedServ) EvolutionWeaponVisuals.Sparks(Projectile.Center, 3, 4);
    }
    // The matrix draws the same orbit once, keeping the existing layered core material intact.
    public override bool PreDraw(ref Color lightColor) => false;
}

public sealed class TerminusRay : EvolutionArsenalProjectile
{
    internal Vector2 End => Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * Math.Clamp(Projectile.ai[0], 0, EvolutionArsenalRules.TerminusSearchRange);
    internal float Opacity => MathF.Pow(Math.Clamp(1 - Projectile.ai[1] / 24f, 0, 1), .7f);
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 10; Projectile.timeLeft = 24;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Magic; Projectile.penetrate = -1;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Projectile.ai[1] <= 6 && Projectile.ai[2] >= 0;
    public override bool? CanHitNPC(NPC target) => Projectile.ai[2] > 0 && target.whoAmI == (int)Projectile.ai[2] - 1;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => EvolutionArsenalRules.Capsule(targetHitbox, Projectile.Center, End, 16);
    public override void AI()
    {
        if (Projectile.ai[2] > 0 && Projectile.ai[2] <= Main.maxNPCs && Projectile.ai[1] <= 6)
        {
            NPC target = Main.npc[(int)Projectile.ai[2] - 1];
            if (target.CanBeChasedBy())
            {
                Projectile.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                Projectile.ai[0] = EvolutionArsenalRules.ClipRay(Projectile.Center, Projectile.velocity, target, out int first);
                Projectile.ai[2] = first + 1;
            }
        }
        Projectile.ai[1]++;
        if (!Main.dedServ && Projectile.ai[1] == 1)
        {
            EvolutionWeaponVisuals.Sparks(End, 5, 2.2f);
            OperatorTextureEffects.Puff(End, Vector2.Zero, EvolutionWeaponVisuals.Blood, 22, 22);
        }
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.ai[2] = -(target.whoAmI + 1);
        Projectile.netUpdate = true;
        if (!Main.dedServ) EvolutionWeaponVisuals.Sparks(target.Center, 4, 2);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        EvolutionWeaponVisuals.Glow(Projectile.Center, 40, Opacity);
        if (!EvolutionWeaponRenderer.Ready) EvolutionWeaponVisuals.FallbackLine(Projectile.Center, End, 12 * Opacity, Opacity);
        return false;
    }
    internal override void DrawMaterial() => EvolutionWeaponVisuals.Ribbon(Projectile.Center, End, 28 * Opacity + 2, Opacity, Projectile.identity * .71f);
}

public sealed class TerminusBloom : EvolutionArsenalProjectile
{
    internal float Age => Projectile.ai[0];
    internal float Radius => 26 + 88 * MathHelper.SmoothStep(0, 1, Math.Clamp(Age / 9f, 0, 1));
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 10; Projectile.timeLeft = 26;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Magic; Projectile.penetrate = -1;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool? CanDamage() => Age >= 3 && Age <= 9;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        => Vector2.DistanceSquared(Projectile.Center, Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight())) <= Radius * Radius;
    public override void AI()
    {
        Projectile.ai[0]++;
        if (Age == 1 && !Main.dedServ)
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = .65f, Pitch = -.2f }, Projectile.Center);
            EvolutionWeaponVisuals.Sparks(Projectile.Center, 20, 6);
            for (int i = 0; i < 7; i++)
                OperatorTextureEffects.Puff(Projectile.Center, Main.rand.NextVector2Circular(3, 3), EvolutionWeaponVisuals.Blood, 40, 32);
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        float opacity = Math.Clamp(1 - Age / 26, 0, 1);
        EvolutionWeaponVisuals.Ring(Projectile.Center, Radius * 2, opacity, Age * .04f);
        EvolutionWeaponVisuals.Glow(Projectile.Center, Radius * 2, opacity * .6f);
        return false;
    }
    internal override void DrawMaterial() => EvolutionWeaponVisuals.Core(Projectile.Center, Radius * 1.6f, Math.Clamp(1 - Age / 26, 0, 1), Age * .07f);
}
