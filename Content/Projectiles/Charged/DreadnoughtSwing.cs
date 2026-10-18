using System;
using System.Collections.Generic;
using System.IO;
using ArknightsMod.Content;
using ArknightsMod.Content.Items.Weapons.Charged;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Charged;

// Stellar Striker timing: a live swept blade deals damage on contact. Each full
// rotation resets hit immunity; the first enemy receives the heavy opening hit.
public sealed class DreadnoughtSwing : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    private ChargedOperatorKind Kind => (ChargedOperatorKind)(int)Projectile.ai[0];
    private float Age { get => Projectile.ai[1]; set => Projectile.ai[1] = value; }
    private float duration = DreadnoughtRules.BaseSwingTicks;
    private int facing = 1, skill;
    private bool initialized, gainedMomentum;
    private int targetsHit;
    private Item boundItem;
    private readonly HashSet<NPC> hitRoots = new();
    private float sweepStartAngle, sweepEndAngle;
    private Vector2 sweepStartCenter, sweepEndCenter;
    private float previousAngle;
    private Vector2 previousCenter;
    private readonly List<(float Angle, Vector2 Center, float Age)> bladeHistory = new();
    private float Progress => Math.Clamp(Age / duration, 0, 1);
    private float Angle => Projectile.velocity.ToRotation()
        + MathHelper.Lerp(-MathHelper.Pi, MathHelper.Pi, DreadnoughtRules.Turn(Progress)) * facing;
    private float Reach => ChargedOperatorRules.Reach(Kind, true) * DreadnoughtRules.WeaponScale
        * (Kind == ChargedOperatorKind.NearlRadiant && skill is 1 or 3 ? 1.15f : 1f);

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 2;
        Projectile.extraUpdates = 2;
        Projectile.netImportant = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => initialized;
    public override bool CanHitPvp(Player target) => false;
    private static NPC Root(NPC npc) => npc.realLife >= 0 && Main.npc[npc.realLife].active ? Main.npc[npc.realLife] : npc;
    public override bool? CanHitNPC(NPC target) => hitRoots.Contains(Root(target)) ? false : null;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
        TryBladeContact(targetHitbox, out _, out _);

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(initialized); writer.Write(duration); writer.Write(facing); writer.Write(skill);
        writer.Write(Projectile.damage); writer.Write(Projectile.knockBack); writer.Write(Projectile.CritChance);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        initialized = reader.ReadBoolean(); duration = reader.ReadSingle(); facing = reader.ReadInt32(); skill = reader.ReadInt32();
        Projectile.damage = reader.ReadInt32(); Projectile.knockBack = reader.ReadSingle(); Projectile.CritChance = reader.ReadInt32();
        previousAngle = Angle;
        Player owner = Main.player[Projectile.owner];
        previousCenter = owner.MountedCenter + new Vector2(0, owner.gfxOffY);
        bladeHistory.Clear();
    }

    private void BeginSwing(Player owner, ChargedOperatorWeapon weapon)
    {
        initialized = true;
        Age = 0;
        hitRoots.Clear();
        targetsHit = 0;
        bladeHistory.Clear();
        Array.Clear(Projectile.localNPCImmunity);
        Projectile.numHits = 0;
        gainedMomentum = false;
        skill = ChargedOperatorPlayer.ActiveSkill(owner);
        Vector2 aim = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(Vector2.UnitX * owner.direction);
        Projectile.velocity = aim;
        facing = (aim.X < 0 ? -1 : 1) * (int)owner.gravDir;
        bool swift = Kind == ChargedOperatorKind.Franka && skill == 1 || Kind == ChargedOperatorKind.NearlRadiant && skill == 1;
        var momentum = owner.GetModPlayer<DreadnoughtPlayer>();
        duration = DreadnoughtRules.SwingTicks(owner.GetAttackSpeed(DamageClass.Melee) * momentum.Multiplier, swift);
        momentum.Attacking(weapon.Item);
        Projectile.damage = Math.Max(1, (int)(owner.GetWeaponDamage(weapon.Item) * ChargedOperatorPlayer.DamageMultiplier(Kind, skill)));
        Projectile.originalDamage = Projectile.damage;
        Projectile.knockBack = owner.GetWeaponKnockback(weapon.Item);
        Projectile.CritChance = owner.GetWeaponCrit(weapon.Item);
        previousAngle = Angle;
        previousCenter = owner.MountedCenter + new Vector2(0, owner.gfxOffY);
        Projectile.netUpdate = true;
    }

    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        bool local = Projectile.owner == Main.myPlayer;
        if (!owner.active || owner.dead || owner.noItems || owner.CCed
            || owner.HeldItem.ModItem is not ChargedOperatorWeapon weapon || !weapon.Melee || weapon.Kind != Kind
            || local && boundItem != null && !ReferenceEquals(boundItem, owner.HeldItem))
        { Projectile.Kill(); return; }
        boundItem ??= owner.HeldItem;
        if (!initialized)
        {
            if (!local) { Projectile.timeLeft = 2; return; }
            BeginSwing(owner, weapon);
        }
        bool holding = !local || owner.controlUseItem && !ArknightsKeybinds.SkillActivatePressed(owner)
            && ChargedOperatorWeapon.WorldInputAllowed(owner);
        if (Age >= duration)
        {
            if (!local) { Projectile.timeLeft = 2; return; }
            if (!holding) { Projectile.Kill(); return; }
            BeginSwing(owner, weapon);
        }
        Projectile.timeLeft = 2;
        Projectile.Center = owner.MountedCenter + new Vector2(0, owner.gfxOffY);
        owner.ChangeDir(Projectile.velocity.X < 0 ? -1 : 1);
        Projectile.direction = owner.direction;
        owner.heldProj = Projectile.whoAmI;
        owner.itemTime = owner.itemAnimation = 2;
        float before = Progress;
        Age = Math.Min(duration, Age + 1f / (Projectile.extraUpdates + 1));
        sweepStartAngle = previousAngle;
        sweepEndAngle = Angle;
        sweepStartCenter = previousCenter;
        sweepEndCenter = Projectile.Center;
        owner.itemRotation = Angle;
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Angle - MathHelper.PiOver2);
        if (local) owner.GetModPlayer<DreadnoughtPlayer>().Attacking(weapon.Item);
        if (before < .36f && Progress >= .36f && !Main.dedServ)
            SoundEngine.PlaySound(SoundID.Item71 with { Volume = .45f, Pitch = .1f + (int)Kind * -.04f }, owner.Center);
        if (Projectile.numUpdates == 0 && !Main.dedServ && Progress > .3f && Progress < .7f)
        {
            Color color = ChargedOperatorRules.ColorFor(Kind);
            Vector2 tip = Projectile.Center + Angle.ToRotationVector2() * Reach;
            ChargedOperatorVfx.Sparkle(tip, (Angle + facing * MathHelper.PiOver2).ToRotationVector2() * 2,
                color, Angle, .55f);
            Lighting.AddLight(tip, color.ToVector3() * .4f);
        }
        previousAngle = Angle;
        previousCenter = Projectile.Center;
        if (!Main.dedServ)
        {
            bladeHistory.RemoveAll(sample => Age - sample.Age > 3);
            bladeHistory.Add((Angle, Projectile.Center, Age));
        }
    }

    // The same grip-to-tip segment used for the sprite. Subdivide the swept angle
    // so a fast blade cannot tunnel through small enemies between projectile updates.
    private bool TryBladeContact(Rectangle box, out Vector2 point, out Vector2 tangent)
    {
        float delta = sweepEndAngle - sweepStartAngle;
        int steps = Math.Max(1, (int)MathF.Ceiling(Math.Abs(delta) / .04f));
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps, distance = 0;
            float angle = MathHelper.Lerp(sweepStartAngle, sweepEndAngle, t);
            Vector2 start = Vector2.Lerp(sweepStartCenter, sweepEndCenter, t);
            Vector2 axis = angle.ToRotationVector2();
            if (!Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(), start, start + axis * Reach,
                24 * DreadnoughtRules.WeaponScale, ref distance)
                || !Collision.CanHitLine(start, 1, 1, box.TopLeft(), box.Width, box.Height)) continue;
            point = box.ClosestPointInRect(start + axis * Math.Clamp(distance, 0, Reach));
            tangent = axis.RotatedBy(facing * MathHelper.PiOver2);
            return true;
        }
        point = default;
        tangent = default;
        return false;
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (DreadnoughtRules.OpeningHit(targetsHit))
        {
            modifiers.SourceDamage *= 3;
            modifiers.Knockback *= 3;
            modifiers.SetCrit();
        }
        if (Kind == ChargedOperatorKind.Franka && Main.rand.NextFloat() < (skill == 2 ? .75f : .25f)
            || Kind == ChargedOperatorKind.NearlRadiant && skill == 3) modifiers.DefenseEffectiveness *= 0;
        modifiers.HitDirectionOverride = target.Center.X < Projectile.Center.X ? -1 : 1;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (damageDone <= 0) return;
        bool opening = DreadnoughtRules.OpeningHit(targetsHit);
        hitRoots.Add(Root(target));
        targetsHit++;
        if (!gainedMomentum)
        {
            Main.player[Projectile.owner].GetModPlayer<DreadnoughtPlayer>().RegisterHit();
            gainedMomentum = true;
        }
        if (Kind == ChargedOperatorKind.Franka) target.AddBuff(BuffID.OnFire3, 180);
        if (Projectile.owner == Main.myPlayer && TryBladeContact(target.Hitbox, out Vector2 point, out Vector2 tangent))
        {
            if (opening)
                Projectile.NewProjectile(Projectile.GetSource_OnHit(target), point, tangent,
                    ModContent.ProjectileType<DreadnoughtHeavyImpact>(), 0, 0, Projectile.owner, (int)Kind);
            else ChargedOperatorVfx.Sparkle(point, tangent * 2, ChargedOperatorRules.ColorFor(Kind), tangent.ToRotation(), .55f);
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (!initialized) return false;
        Player owner = Main.player[Projectile.owner];
        if (owner.HeldItem.ModItem is not ChargedOperatorWeapon weapon || weapon.Kind != Kind) return false;
        Main.instance.LoadItem(owner.HeldItem.type);
        // The inventory icon is two crossed swords. The original author supplied
        // separate horizontal blade sprites for the actual held sword.
        Texture2D texture = Kind == ChargedOperatorKind.Melantha
            ? ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Weapons/Guard/Melantha/MelanthasSword_white").Value
            : TextureAssets.Item[owner.HeldItem.type].Value;
        Vector2 center = Projectile.Center;
        bool flip = Projectile.direction < 0;
        DreadnoughtBladeLayout layout = DreadnoughtBladeLayout.For(Kind, texture.Size());
        float correction = -layout.Axis(flip).ToRotation();
        Vector2 origin = layout.Origin(texture.Width, flip);
        float scale = Reach / layout.Axis(flip).Length();
        SpriteEffects effects = flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        DreadnoughtArcTrail.Draw(center, Projectile.velocity.ToRotation(), facing, Progress, Reach, Kind);
        float fade = DreadnoughtRules.TrailIntensity(Progress);
        for (int i = 0; i < bladeHistory.Count - 1; i += 2)
        {
            var sample = bladeHistory[i];
            float opacity = (1 - Math.Clamp((Age - sample.Age) / 3f, 0, 1)) * fade * .18f;
            Main.EntitySpriteDraw(texture, sample.Center - Main.screenPosition, null, lightColor * opacity,
                sample.Angle + correction, origin, scale, effects);
        }
        Main.EntitySpriteDraw(texture, center - Main.screenPosition, null, lightColor, Angle + correction, origin,
            scale, effects);
        return false;
    }
}
