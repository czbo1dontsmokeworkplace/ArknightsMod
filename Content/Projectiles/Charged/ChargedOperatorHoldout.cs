using System;
using System.IO;
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

// ai[0] operator; ai[1] charge ticks; ai[2] release age (zero while charging).
public sealed class ChargedOperatorHoldout : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    internal ChargedOperatorKind Kind => (ChargedOperatorKind)(int)Projectile.ai[0];
    private bool Melee => ChargedOperatorRules.IsMelee(Kind);
    private int skill, secondaryHits, recovery = 26;
    private bool heavy, playedRelease;
    private float power;
    private Item boundItem;
    internal float Completion => ChargedOperatorRules.Completion(Projectile.ai[1], ChargedOperatorRules.ChargeTicks(Kind));
    private float Reach => ChargedOperatorRules.Reach(Kind, heavy) * (Kind == ChargedOperatorKind.NearlRadiant && skill is 1 or 3 ? 1.15f : 1);
    private float SwingProgress => Math.Clamp((Projectile.ai[2] - 1) / 18f, 0, 1);
    private float BladeAngle => Projectile.velocity.ToRotation() + (Projectile.ai[2] == 0 ? -1.2f
        : MathHelper.Lerp(-1.5f, 1.5f, SwingProgress * SwingProgress * (3 - 2 * SwingProgress))) * Projectile.direction;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12;
        Projectile.friendly = true; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.timeLeft = 2; Projectile.netImportant = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Melee && Projectile.ai[2] >= 2 && Projectile.ai[2] <= 19 ? null : false;
    public override void SendExtraAI(BinaryWriter writer)
    { writer.Write(skill); writer.Write(heavy); writer.Write(power); writer.Write(recovery); writer.Write(Projectile.damage); }
    public override void ReceiveExtraAI(BinaryReader reader)
    { skill = reader.ReadInt32(); heavy = reader.ReadBoolean(); power = reader.ReadSingle(); recovery = reader.ReadInt32(); Projectile.damage = reader.ReadInt32(); }
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.noItems || owner.CCed
            || owner.HeldItem.ModItem is not ChargedOperatorWeapon weapon || weapon.Kind != Kind)
        { Projectile.Kill(); return; }
        bool local = Projectile.owner == Main.myPlayer;
        if (local && boundItem != null && !ReferenceEquals(owner.HeldItem, boundItem)) { Projectile.Kill(); return; }
        boundItem ??= owner.HeldItem;
        Projectile.DamageType = Melee ? DamageClass.Melee : DamageClass.Ranged;
        if (local && Projectile.ai[2] == 0)
        {
            Vector2 aim = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(Vector2.UnitX * owner.direction);
            if (Vector2.DistanceSquared(aim, Projectile.velocity) > .0004f) { Projectile.velocity = aim; Projectile.netUpdate = true; }
        }
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
        Projectile.Center = owner.MountedCenter;
        Projectile.direction = direction.X >= 0 ? 1 : -1;
        owner.ChangeDir(Projectile.direction); owner.heldProj = Projectile.whoAmI;
        owner.itemTime = owner.itemAnimation = 2;
        owner.itemRotation = (direction * owner.direction).ToRotation();
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, (Melee ? BladeAngle : direction.ToRotation()) - MathHelper.PiOver2);
        Projectile.timeLeft = 2;
        if (Projectile.ai[2] == 0)
        {
            // Release is evaluated BEFORE advancing the charge: a 59-tick hold cannot become heavy.
            if (local && !owner.channel) Release(owner, weapon);
            else
            {
                float before = Completion;
                Projectile.ai[1] = Math.Min(ChargedOperatorRules.ChargeTicks(Kind), Projectile.ai[1] + 1);
                if (local && (int)Projectile.ai[1] % 6 == 0 && Completion < 1) Projectile.netUpdate = true;
                if (before < 1 && Completion >= 1)
                {
                    Projectile.netUpdate = local;
                    if (!Main.dedServ) SoundEngine.PlaySound(SoundID.MaxMana with { Volume = .55f }, owner.Center);
                    ChargedOperatorVfx.Burst(Muzzle(owner), Kind, 12, 28);
                }
                if (!Main.dedServ && Main.GameUpdateCount % 8 == 0)
                {
                    float angle = (float)Main.GameUpdateCount * .08f;
                    Vector2 tip = Muzzle(owner);
                    Vector2 radial = angle.ToRotationVector2();
                    ChargedOperatorVfx.Sparkle(tip + radial * (24 - 16 * Completion), -radial * .6f,
                        ChargedOperatorRules.ColorFor(Kind), angle, .6f + Completion * .4f);
                }
            }
        }
        else
        {
            if (!playedRelease)
            {
                playedRelease = true;
                if (!Main.dedServ) SoundEngine.PlaySound((Melee ? SoundID.Item71 : SoundID.Item91) with { Volume = .65f, Pitch = heavy ? -.25f : .1f }, owner.Center);
                ChargedOperatorVfx.Burst(Muzzle(owner), Kind, heavy ? 18 : 6, heavy ? 48 : 20);
            }
            Projectile.ai[2]++;
            if (Projectile.ai[2] >= recovery) Projectile.Kill();
        }
    }
    private Vector2 Muzzle(Player player)
    {
        Vector2 offset = Melee ? BladeAngle.ToRotationVector2() * (Projectile.ai[2] == 0 ? 82 : Reach)
            : Projectile.velocity.SafeNormalize(Vector2.UnitX) * 50;
        return player.MountedCenter + Collision.TileCollision(player.MountedCenter - new Vector2(3), offset, 6, 6, true, true);
    }
    private void Release(Player owner, ChargedOperatorWeapon weapon)
    {
        if (!weapon.ReleaseAmmo(owner, out int damage, out float knockback, out int ammo)) { Projectile.Kill(); return; }
        power = Completion; heavy = power >= 1;
        skill = ChargedOperatorPlayer.ActiveSkill(owner);
        recovery = Melee ? 28 : 24;
        if (Kind == ChargedOperatorKind.Franka && skill == 1 || Kind == ChargedOperatorKind.NearlRadiant && skill == 1
            || Kind == ChargedOperatorKind.Fartooth && skill == 1) recovery = Melee ? 21 : 16;
        if (Kind == ChargedOperatorKind.Fartooth && skill == 2) recovery = 10;
        Projectile.damage = Math.Max(1, (int)(damage * (Melee ? heavy ? 3f : .8f : ChargedOperatorRules.ShotMultiplier(power))
            * ChargedOperatorPlayer.DamageMultiplier(Kind, skill)));
        Projectile.knockBack = knockback * MathHelper.Lerp(.5f, 1.8f, power);
        if (Kind == ChargedOperatorKind.Fartooth && owner.GetModPlayer<ChargedOperatorPlayer>().UnhurtTicks >= 300)
            Projectile.damage = (int)(Projectile.damage * 1.2f);
        Projectile.CritChance = owner.GetWeaponCrit(weapon.Item);
        Projectile.ai[2] = 1;
        owner.GetModPlayer<ChargedOperatorPlayer>().Recovery = recovery;
        if (!Melee)
        {
            Projectile shot = Projectile.NewProjectileDirect(owner.GetSource_ItemUse_WithPotentialAmmo(weapon.Item, ammo),
                Muzzle(owner), Projectile.velocity.SafeNormalize(Vector2.UnitX) * MathHelper.Lerp(12, 22, power),
                ModContent.ProjectileType<ChargedOperatorShot>(), Projectile.damage, Projectile.knockBack, owner.whoAmI,
                (int)Kind, power, skill);
            shot.CritChance = Projectile.CritChance;
            if (Kind == ChargedOperatorKind.Fartooth && skill == 2 && heavy)
            {
                NPC target = FindSupportTarget(owner);
                if (target != null)
                {
                    Projectile support = Projectile.NewProjectileDirect(owner.GetSource_ItemUse_WithPotentialAmmo(weapon.Item, ammo),
                        Muzzle(owner), (target.Center - Muzzle(owner)).SafeNormalize(Projectile.velocity) * 22,
                        ModContent.ProjectileType<ChargedOperatorShot>(), Projectile.damage / 2, Projectile.knockBack, owner.whoAmI, (int)Kind, power, 0);
                    support.CritChance = Projectile.CritChance;
                }
            }
        }
        Projectile.netUpdate = true;
    }
    private NPC FindSupportTarget(Player owner)
    {
        NPC best = null; float distance = 1200 * 1200;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            float d = npc.DistanceSQ(owner.Center);
            if (!npc.CanBeChasedBy(Projectile) || d >= distance || !Collision.CanHitLine(owner.Center, 1, 1, npc.position, npc.width, npc.height)) continue;
            foreach (Player ally in Main.ActivePlayers)
                if (ally.whoAmI != owner.whoAmI && ally.active && !ally.dead
                    && (!ally.hostile && !owner.hostile || owner.team != 0 && ally.team == owner.team)
                    && ally.DistanceSQ(npc.Center) < 240 * 240)
                { best = npc; distance = d; break; }
        }
        return best;
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        // A swept blade, not a screen-sized bounding box. Collision and art share Reach/BladeAngle.
        float previousProgress = Math.Clamp((Projectile.ai[2] - 2) / 18f, 0, 1);
        float previousAngle = Projectile.velocity.ToRotation() + MathHelper.Lerp(-1.5f, 1.5f,
            previousProgress * previousProgress * (3 - 2 * previousProgress)) * Projectile.direction;
        for (int i = 0; i <= 3; i++)
        {
            float point = 0;
            Vector2 end = Projectile.Center + MathHelper.Lerp(previousAngle, BladeAngle, i / 3f).ToRotationVector2() * Reach;
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
                end, heavy ? 38 : 20, ref point)) return true;
        }
        return false;
    }
    public override bool? CanHitNPC(NPC target) => Collision.CanHitLine(Projectile.Center, 1, 1, target.position, target.width, target.height) ? null : false;
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Kind == ChargedOperatorKind.Franka && Main.rand.NextFloat() < (skill == 2 ? .75f : .25f)) modifiers.DefenseEffectiveness *= 0;
        if (Kind == ChargedOperatorKind.NearlRadiant && skill == 3) modifiers.DefenseEffectiveness *= 0;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Kind == ChargedOperatorKind.Franka) target.AddBuff(BuffID.OnFire3, 180);
        if (!heavy || Projectile.owner != Main.myPlayer || secondaryHits++ >= 6) return;
        Vector2 impact = target.Hitbox.ClosestPointInRect(Projectile.Center + BladeAngle.ToRotationVector2() * Reach);
        Projectile.NewProjectile(Projectile.GetSource_OnHit(target), impact, Vector2.Zero,
            ModContent.ProjectileType<ChargedOperatorImpact>(), Math.Max(1, (int)(Projectile.damage * .32f)), 3, Projectile.owner, (int)Kind, 64 + (int)Kind * 12);
        for (int i = 0; i < 2; i++)
        {
            Vector2 velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(i == 0 ? -.45f : .45f) * 10;
            Projectile.NewProjectile(Projectile.GetSource_OnHit(target), impact, velocity,
                ModContent.ProjectileType<DreadnoughtFragment>(), Math.Max(1, (int)(Projectile.damage * .22f)), 2,
                Projectile.owner, (int)Kind, target.whoAmI);
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Player owner = Main.player[Projectile.owner];
        if (owner.HeldItem.ModItem is not ChargedOperatorWeapon weapon || weapon.Kind != Kind) return false;
        Color color = ChargedOperatorRules.ColorFor(Kind);
        Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        Main.instance.LoadItem(owner.HeldItem.type);
        Texture2D texture = TextureAssets.Item[owner.HeldItem.type].Value;
        if (Melee)
        {
            float angle = BladeAngle;
            float length = Projectile.ai[2] == 0 ? 82 : Reach;
            // Vanilla sword art runs diagonally from lower-left grip to upper-right tip.
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, angle + MathF.Atan2(texture.Height, texture.Width),
                new Vector2(0, texture.Height), length / texture.Size().Length(), SpriteEffects.None);
            using var pass = new ChargedOperatorVfx.Pass(true);
            if (Projectile.ai[2] > 0 && SwingProgress < 1)
            {
                float fade = MathF.Sin(SwingProgress * MathHelper.Pi);
                // A broad material band follows only the already-traversed blade arc.
                int segments = 20;
                float span = Math.Min(SwingProgress * 3f, heavy ? 1.25f : .7f);
                for (int i = 0; i < segments; i++)
                {
                    float t = i / (segments - 1f);
                    float a = angle - t * span * Projectile.direction;
                    Vector2 point = Projectile.Center + a.ToRotationVector2() * (Reach - 14);
                    ChargedOperatorVfx.Sprite(Kind == ChargedOperatorKind.Franka ? "ForwardFlame" : "OrbitSmear", point,
                        new Vector2(heavy ? 40 : 22, heavy ? 25 : 12), color * (fade * (1 - t) * .38f), a + MathHelper.PiOver2);
                }
                if (Kind == ChargedOperatorKind.NearlRadiant)
                    ChargedOperatorVfx.Sprite("StarCore", Projectile.Center + angle.ToRotationVector2() * Reach,
                        new Vector2(64, 28), color * fade * .7f, angle);
            }
            else
            {
                string motif = Kind switch { ChargedOperatorKind.Melantha => "ImpactPetals", ChargedOperatorKind.Matoimaru => "CrystalVortex",
                    ChargedOperatorKind.Franka => "ForwardFlame", _ => "StarCore" };
                ChargedOperatorVfx.Sprite(motif, Muzzle(owner), new Vector2(16 + 28 * Completion), color * (.2f + Completion * .5f), angle);
            }
        }
        else
        {
            Main.EntitySpriteDraw(texture, Projectile.Center + aim * 14 - Main.screenPosition, null, lightColor,
                aim.ToRotation(), texture.Size() / 2, 72f / texture.Width,
                Projectile.direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None);
            if (Projectile.ai[2] == 0)
            {
                using var pass = new ChargedOperatorVfx.Pass(true);
                Vector2 muzzle = Muzzle(owner); float spread = (1 - Completion) * .18f;
                float sightLength = 640 + Completion * 320;
                foreach (float sign in new[] { -1f, 1f })
                    ChargedOperatorVfx.Line(muzzle, muzzle + aim.RotatedBy(sign * spread) * sightLength, color * (.18f + Completion * .3f), 1.2f);
                ChargedOperatorVfx.Sprite("StarCore", muzzle, new Vector2(18 + 26 * Completion, 12 + 16 * Completion), color * .6f, aim.ToRotation());
            }
        }
        return false;
    }
}
