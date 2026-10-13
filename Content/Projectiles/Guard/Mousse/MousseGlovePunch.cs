using System;
using System.Collections.Generic;
using ArknightsMod.Content.Items.Weapons.Guard.Mousse;
using ArknightsMod.Players;
using ArknightsMod.Systems.Gameplay.Skill;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Mousse;

// Timing/positioning reference: AugerHoldout, credit Azafure, LLC.
// https://github.com/CalamityTeam/CalamityModPublic
// Independent implementation using Terraria and ArknightsMod resources only.
public class MousseGlovePunch : ModProjectile
{
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Guard/Mousse/MousseGlove";
    private ref float Timer => ref Projectile.ai[0];
    private ref float SwingCount => ref Projectile.ai[1];
    private float Unit => Math.Max(2f, Projectile.ai[2]);
    private float bladeRotation;
    private float bladePower;
    private bool strikeIssued;
    private int age;
    private Player Owner => Main.player[Projectile.owner];

    public override void SetDefaults() {
        Projectile.width = Projectile.height = 20;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 5;
        Projectile.netImportant = true;
    }

    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;

    internal static float Ease(float p, float acceleration, float deceleration) {
        p = MathHelper.Clamp(p, 0f, 1f);
        return p < .5f ? .5f * MathF.Pow(2f * p, acceleration)
            : 1f - .5f * MathF.Pow(2f * (1f - p), deceleration);
    }

    public override void AI() {
        Player owner = Owner;
        if (!owner.active || owner.dead || owner.noItems || owner.CCed || !owner.channel
            || owner.HeldItem.type != ModContent.ItemType<MousseGlove>()) {
            Projectile.Kill();
            return;
        }
        Projectile.timeLeft = 5;
        if (owner.whoAmI == Main.myPlayer) {
            Vector2 aim = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(new Vector2(owner.direction, 0));
            if (Vector2.DistanceSquared(aim, Projectile.velocity) > .0009f) {
                Projectile.velocity = aim;
                Projectile.netUpdate = true;
            }
        }
        Vector2 toMouse = Projectile.velocity.SafeNormalize(new Vector2(owner.direction, 0));
        owner.ChangeDir(toMouse.X < 0 ? -1 : 1);
        owner.heldProj = Projectile.whoAmI;
        owner.itemTime = owner.itemAnimation = 2;
        Projectile.scale = MathHelper.Clamp(owner.GetAdjustedItemScale(owner.HeldItem), .5f, 3f);

        bool releaseStrike = false;
        if (Timer < 0) {
            strikeIssued = false;
            bool pairFinished = SwingCount >= 2;
            float recovery = Unit * (pairFinished ? 3f : 1f);
            float progress = Utils.GetLerpValue(-recovery, -1f, Timer + 1f, true);
            float sign = SwingCount % 2 != 0 ? 1f : -1f;
            bladeRotation = MathHelper.Lerp(-MathHelper.Pi * sign, -.75f * MathHelper.Pi * sign,
                Ease(progress, 2f, 2f));
            if (pairFinished && Timer + 1f <= -recovery * .35f)
                bladePower = MathHelper.Lerp(bladePower, 0f, progress);
            else {
                float grow = Utils.GetLerpValue(-recovery * (pairFinished ? .35f : 1f), -1f, Timer + 1f, true);
                bladePower = MathHelper.Lerp(bladePower, 1.5f, grow);
            }
            Timer++;
            if (Timer >= 0) SwingCount = pairFinished ? 1 : SwingCount + 1;
        }
        else {
            float progress = Utils.GetLerpValue(0, Unit - 1f, Timer, true);
            float sign = SwingCount % 2 != 0 ? -1f : 1f;
            bladeRotation = MathHelper.Lerp(-.75f * MathHelper.Pi * sign, MathHelper.Pi * sign,
                Ease(progress, 5f, 3f));
            if (progress > .5f && !strikeIssued) {
                strikeIssued = true;
                releaseStrike = true;
            }
            Timer++;
            if (Timer >= Unit) Timer = -Unit * (SwingCount >= 2 ? 3f : 1f);
        }

        float angle = toMouse.ToRotation() + bladeRotation * owner.direction;
        owner.itemRotation = angle + (owner.direction < 0 ? MathHelper.Pi : 0f);
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, angle - MathHelper.PiOver2);
        Vector2 hand = owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.None,
            owner.compositeFrontArm.rotation) + angle.ToRotationVector2() * 9f;
        owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full,
            (hand - owner.Center).ToRotation() - MathHelper.PiOver2);
        Projectile.Center = hand;
        Projectile.rotation = angle;
        if (releaseStrike) {
            if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Item68 with { Volume = .7f, MaxInstances = 3 }, hand);
            if (owner.whoAmI == Main.myPlayer) {
                WeaponPlayer skills = owner.GetModPlayer<WeaponPlayer>();
                bool scratch = skills.Skill == 0 && skills.SkillActive;
                int damage = owner.GetWeaponDamage(owner.HeldItem);
                int slash = Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.MountedCenter + toMouse * 25f * Projectile.scale,
                    toMouse * 31.25f * Projectile.scale, ModContent.ProjectileType<MousseLightSlash>(),
                    damage, owner.GetWeaponKnockback(owner.HeldItem, owner.HeldItem.knockBack), owner.whoAmI,
                    SwingCount % 2 == 0 ? 1 : -1, Projectile.scale, scratch ? 1f : 0f);
                if (Main.projectile.IndexInRange(slash)) Main.projectile[slash].CritChance = owner.GetWeaponCrit(owner.HeldItem);
                if (scratch) skills.SkillActive = false;
                else if (skills.Skill == 0 && skills.CurrentSkill?.ChargeType == SkillChargeType.Attack)
                    skills.OffensiveRecovery();
            }
        }
        age++;
    }

    public override bool PreDraw(ref Color lightColor) {
        if (age > 1)
            MousseVisuals.DrawGlove(Projectile, Owner, bladePower, lightColor);
        return false;
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => overPlayers.Add(index);
}
