using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace ArknightsMod.Content.Items.MaterialistArsenal;

// One beam's damage for one eight-tick interval. Sorting by distance keeps
// attenuation independent of NPC slots, the other beam and earlier intervals.
public sealed class PlasmaProjectorPulse : ModProjectile
{
    private readonly List<(int Index, float Distance)> targets = new();
    private readonly Dictionary<int, float> multipliers = new();
    internal Vector2 End => Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * PlasmaProjectorHoldout.Range;
    public override string LocalizationCategory => "Projectiles.MaterialistArsenal";
    public override string Texture => "Terraria/Images/MagicPixel";

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 6;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 2;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.hide = true;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Projectile.ai[0] == 1 ? null : false;
    public override bool? CanHitNPC(NPC target) => multipliers.ContainsKey(target.whoAmI) ? null : false;
    public override void AI()
    {
        if (++Projectile.ai[0] != 1) return;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!npc.CanBeChasedBy(Projectile)) continue;
            float distance = 0;
            if (Collision.CheckAABBvLineCollision(npc.position, npc.Size, Projectile.Center, End,
                PlasmaProjectorHoldout.Width, ref distance))
                targets.Add((npc.whoAmI, Math.Max(0, distance)));
        }
        targets.Sort((a, b) => {
            int comparison = a.Distance.CompareTo(b.Distance);
            return comparison == 0 ? a.Index.CompareTo(b.Index) : comparison;
        });
        var random = new UnifiedRandom((int)Projectile.ai[1]);
        int remaining = random.Next(2, 5);
        float multiplier = 1f;
        foreach (var target in targets)
        {
            multipliers[target.Index] = multiplier;
            if (--remaining != 0) continue;
            multiplier *= random.NextFloat(.8f, .9f);
            remaining = random.Next(2, 5);
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float distance = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            Projectile.Center, End, PlasmaProjectorHoldout.Width, ref distance);
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (multipliers.TryGetValue(target.whoAmI, out float multiplier))
            modifiers.SourceDamage *= multiplier;
    }

    public override bool PreDraw(ref Color lightColor) => false;
}
