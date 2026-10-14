using System;
using System.Collections.Generic;
using System.IO;
using ArknightsMod.Content.Projectiles.Guard.Mousse;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.ArtsFighter;

public abstract class ArtsFighterHoldout : ModProjectile
{
    protected abstract int WeaponType { get; }
    protected abstract int SlashType { get; }
    protected abstract Color BladeColor { get; }
    protected virtual bool PhysicalSword => false;
    protected Player Owner => Main.player[Projectile.owner];
    private float bladeRotation;
    private float bladePower;
    private int age;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 20;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 5;
        Projectile.netImportant = true;
        Projectile.hide = true;
    }
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    protected virtual float PrepareStrike() => 0f;
    protected virtual void AfterStrike() { }
    public override void AI()
    {
        Player owner = Owner;
        if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.type != WeaponType
            || (owner.whoAmI == Main.myPlayer && !owner.channel))
        {
            Projectile.Kill(); return;
        }
        Projectile.timeLeft = 5;
        if (owner.whoAmI == Main.myPlayer)
        {
            Vector2 aim = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(new Vector2(owner.direction, 0));
            if (Vector2.DistanceSquared(aim, Projectile.velocity) > .0009f)
            {
                Projectile.velocity = aim;
                Projectile.netUpdate = true;
            }
        }
        Vector2 toMouse = Projectile.velocity.SafeNormalize(new Vector2(owner.direction, 0));
        owner.ChangeDir(toMouse.X < 0 ? -1 : 1);
        owner.heldProj = Projectile.whoAmI;
        owner.itemTime = owner.itemAnimation = 2;
        Projectile.scale = MathHelper.Clamp(owner.GetAdjustedItemScale(owner.HeldItem), .5f, 3f);
        bool release = ArtsFighterSwing.Step(ref Projectile.ai[0], ref Projectile.ai[1],
            Math.Max(2f, Projectile.ai[2]), ref bladeRotation, ref bladePower);
        float angle = toMouse.ToRotation() + bladeRotation * owner.direction;
        owner.itemRotation = angle + (owner.direction < 0 ? MathHelper.Pi : 0f);
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, angle - MathHelper.PiOver2);
        Vector2 hand = owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.None,
            owner.compositeFrontArm.rotation) + angle.ToRotationVector2() * 9f;
        owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full,
            (hand - owner.Center).ToRotation() - MathHelper.PiOver2);
        Projectile.Center = hand;
        Projectile.rotation = angle;
        if (release)
        {
            if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Item68 with { Volume = .7f, MaxInstances = 3 }, hand);
            if (owner.whoAmI == Main.myPlayer)
            {
                float mode = PrepareStrike();
                float scale = Projectile.scale * MousseVisuals.RangeMultiplier;
                int slash = Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.MountedCenter + toMouse * 20f * scale,
                    toMouse * 25f * scale, SlashType, owner.GetWeaponDamage(owner.HeldItem),
                    owner.GetWeaponKnockback(owner.HeldItem, owner.HeldItem.knockBack), owner.whoAmI,
                    Projectile.ai[1], Projectile.scale, mode);
                if (Main.projectile.IndexInRange(slash)) Main.projectile[slash].CritChance = owner.GetWeaponCrit(owner.HeldItem);
                AfterStrike();
                Projectile.netUpdate = true;
            }
        }
        age++;
    }
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(bladeRotation); writer.Write(bladePower);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        bladeRotation = reader.ReadSingle(); bladePower = reader.ReadSingle();
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (age > 1) MousseVisuals.DrawHeldBlade(Projectile, Owner, bladePower, lightColor, BladeColor, PhysicalSword);
        return false;
    }
    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => overPlayers.Add(index);
}
