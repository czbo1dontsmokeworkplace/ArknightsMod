using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Caster.Leonhardt;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Projectiles.Caster.SpreadArts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Leonhardt;

/// <summary>玩家手中的蓄力法杖。蓄力状态只由持有者决定，弹幕由持有者发射一次。</summary>
public sealed class LeonhardtFocus : ModProjectile
{
    private const int FullChargeTicks = 88;
    private float aimAngle;
    private SpreadChargeVfx chargeVfx;
    private bool fired;
    private bool sawChannel;
    private int openingGrace;
    private Player Owner => Main.player[Projectile.owner];
    private Vector2 Direction => aimAngle.ToRotationVector2();
    // 贴图握点到杖尖约 65 像素；绘制缩放 .86 后约 56 像素。
    private Vector2 Muzzle => Owner.MountedCenter + Direction * 56f;
    private float Charge => MathHelper.Clamp(Projectile.ai[0] / FullChargeTicks, 0f, 1f);

    public override string Texture => "ArknightsMod/Content/Items/Weapons/Caster/Leonhardt/LeonhardtStaff";

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.friendly = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 2;
        Projectile.netImportant = true;
    }

    public override void OnSpawn(IEntitySource source) => aimAngle = Projectile.velocity.SafeNormalize(Vector2.UnitX).ToRotation();

    public override bool ShouldUpdatePosition() => false;

    public override void AI()
    {
        Player owner = Owner;
        if (!owner.active || owner.dead || owner.noItems || owner.CCed
            || owner.HeldItem.ModItem is not LeonhardtStaff)
        {
            Projectile.Kill();
            return;
        }

        Projectile.Center = owner.MountedCenter;
        if (Projectile.owner == Main.myPlayer)
        {
            Vector2 aim = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(new Vector2(owner.direction, 0f));
            aimAngle = aim.ToRotation();
            if (owner.channel) sawChannel = true;
            else if (sawChannel || ++openingGrace > 5)
            {
                if (sawChannel) Fire();
                owner.itemTime = owner.itemAnimation = 0;
                Projectile.Kill();
                return;
            }
            if ((int)Projectile.ai[0] % 6 == 0)
                Projectile.netUpdate = true;
            if (owner.channel && !ChannelMana.Sustain(owner, Projectile))
            { owner.itemTime = owner.itemAnimation = 0; Projectile.Kill(); return; }
        }
        owner.heldProj = Projectile.whoAmI;
        owner.itemTime = owner.itemAnimation = 2;

        owner.ChangeDir(Direction.X < 0f ? -1 : 1);
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full,
            (Muzzle - owner.MountedCenter).ToRotation() - MathHelper.PiOver2);
        Projectile.timeLeft = 2;
        Projectile.ai[0] = Math.Min(FullChargeTicks, Projectile.ai[0] + 1f);

        if (Main.dedServ) return;
        (chargeVfx ??= new()).Tick(Projectile, Muzzle, Charge, SpreadArt.Rupture, Direction);
        Lighting.AddLight(Muzzle, new Vector3(.12f, .28f, .45f) * Charge);
        if (Charge >= 1f && Projectile.localAI[0] == 0f)
        {
            Projectile.localAI[0] = 1f;
            ChargeCastFeedback.Ready(Muzzle, new Color(121, 201, 241), DustID.GemSapphire, .48f);
        }
    }

    private void Fire()
    {
        if (fired || Projectile.owner != Main.myPlayer) return;
        fired = true;
        if (Projectile.ai[0] < FullChargeTicks)
        {
            SoundEngine.PlaySound(SoundID.Item9 with { Volume = .25f, Pitch = -.15f }, Muzzle);
            return;
        }

        float charge = Charge;
        int damage = Math.Max(1, Owner.GetWeaponDamage(Owner.HeldItem));
        Vector2 velocity = Direction * (17f * 1.2f);
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Muzzle, velocity,
            ModContent.ProjectileType<LeonhardtCore>(), damage, Projectile.knockBack,
            Projectile.owner, charge);
        SoundEngine.PlaySound(SoundID.Item73 with { Volume = .58f }, Muzzle);
        if (!Main.dedServ)
        {
            LeonhardtVisuals.FireParticles(Muzzle, Direction);
            for (int i = 0; i < 5; i++)
            {
                Vector2 spread = Direction.RotatedByRandom(.44f) * Main.rand.NextFloat(3f, 9f);
                Dust dust = Dust.NewDustPerfect(Muzzle, DustID.GemSapphire, spread, 90, default,
                    Main.rand.NextFloat(.6f, 1.15f));
                dust.noGravity = true;
            }
            Main.instance.CameraModifiers.Add(new PunchCameraModifier(Muzzle, Direction * -1f, 2.5f * charge,
                5f, 10));
        }
    }

    public override void SendExtraAI(BinaryWriter writer) => writer.Write(aimAngle);
    public override void ReceiveExtraAI(BinaryReader reader) => aimAngle = reader.ReadSingle();

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D staff = TextureAssets.Item[ModContent.ItemType<LeonhardtStaff>()].Value;
        bool left = Direction.X < 0f;
        Vector2 grip = new(9f, 57f);
        Vector2 tip = new(53f, 9f);
        float nativeAngle = (tip - grip).ToRotation();
        if (left) grip.Y = staff.Height - grip.Y;
        Main.spriteBatch.Draw(staff, Owner.MountedCenter - Main.screenPosition, null, lightColor,
            left ? aimAngle + nativeAngle : aimAngle - nativeAngle, grip, .86f,
            left ? SpriteEffects.FlipVertically : SpriteEffects.None, 0f);

        if (Projectile.ai[1] == 0f)
            LeonhardtVisuals.DrawCharge(Muzzle, Charge, chargeVfx?.Phase ?? 0f, Direction);
        ChargeCastFeedback.DrawBar(Owner, Charge, new Color(121, 201, 241));
        return false;
    }
}
