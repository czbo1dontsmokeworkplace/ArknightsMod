using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Caster.Skyfire;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Skyfire;

public sealed class SkyfireFocus : ModProjectile
{
    private const int FullChargeTicks = 88;
    private float aimAngle;
    private bool fired;
    private bool sawChannel;
    private int openingGrace;
    private Player Owner => Main.player[Projectile.owner];
    private Vector2 Direction => aimAngle.ToRotationVector2();
    private Vector2 Muzzle => Owner.MountedCenter + Direction * 43f;
    private float Charge => MathHelper.Clamp(Projectile.ai[0] / FullChargeTicks, 0f, 1f);
    public override string Texture => "Terraria/Images/Item_" + ItemID.MeteorStaff;

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
        if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.ModItem is not SkyfireStaff)
        {
            Projectile.Kill();
            return;
        }
        Projectile.Center = owner.MountedCenter;
        if (Projectile.owner == Main.myPlayer)
        {
            aimAngle = (Main.MouseWorld - owner.MountedCenter)
                .SafeNormalize(new Vector2(owner.direction, 0f)).ToRotation();
            if (owner.channel) sawChannel = true;
            else if (sawChannel || ++openingGrace > 5)
            {
                if (sawChannel) Fire();
                owner.itemTime = owner.itemAnimation = 0;
                Projectile.Kill();
                return;
            }
            if ((int)Projectile.ai[0] % 6 == 0) Projectile.netUpdate = true;
        }
        owner.heldProj = Projectile.whoAmI;
        owner.itemTime = owner.itemAnimation = 2;
        owner.ChangeDir(Direction.X < 0f ? -1 : 1);
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, aimAngle - MathHelper.PiOver2);
        Projectile.timeLeft = 2;
        Projectile.ai[0] = Math.Min(FullChargeTicks, Projectile.ai[0] + 1f);
        if (Main.dedServ) return;
        Lighting.AddLight(Muzzle, new Vector3(.58f, .19f, .04f) * Charge);
        if (Charge >= 1f && Projectile.localAI[0] == 0f)
        {
            Projectile.localAI[0] = 1f;
            ChargeCastFeedback.Ready(Muzzle, new Color(245, 140, 83), DustID.Torch, .22f);
        }
        if (Projectile.ai[0] > 8 && Main.rand.NextFloat() < .22f + Charge * .43f)
            SkyfireVisuals.ChargeParticles(Muzzle, Charge);
    }

    private void Fire()
    {
        if (fired || Projectile.owner != Main.myPlayer) return;
        fired = true;
        if (Projectile.ai[0] < FullChargeTicks)
        {
            SoundEngine.PlaySound(SoundID.Item9 with { Volume = .24f, Pitch = -.18f }, Muzzle);
            return;
        }
        WeaponPlayer skills = Owner.GetModPlayer<WeaponPlayer>();
        bool skyfall = skills.SkillActive && skills.Skill == 1
            && skills.CurrentSkill?.Key.Item == nameof(SkyfireStaff);
        Vector2 start = Muzzle;
        Vector2 velocity = Direction * (18f * 1.2f);
        float multiplier = 1f;
        if (skyfall)
        {
            Vector2 target = Main.MouseWorld;
            Vector2 offset = target - Owner.MountedCenter;
            if (offset.LengthSquared() > 850f * 850f)
                target = Owner.MountedCenter + offset.SafeNormalize(Vector2.UnitX) * 850f;
            start = target + new Vector2(-230f, -520f);
            velocity = (target - start).SafeNormalize(Vector2.UnitY) * (22f * 1.2f);
            int rank = Math.Clamp((skills.CurrentSkill.ForceReplaceLevel ?? skills.CurrentSkill.Level) - 1, 0, 9);
            multiplier = MathHelper.Lerp(1.5f, 2.35f, rank / 9f);
        }
        int damage = Math.Max(1, (int)(Owner.GetWeaponDamage(Owner.HeldItem) * multiplier));
        int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), start, velocity,
            ModContent.ProjectileType<SkyfireMeteor>(), damage, Projectile.knockBack,
            Projectile.owner, skyfall ? 1f : 0f);
        if (Main.projectile.IndexInRange(index))
            Main.projectile[index].CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
        SoundEngine.PlaySound(SoundID.Item73 with { Volume = .58f }, Muzzle);
        if (!Main.dedServ) SkyfireVisuals.FireParticles(Muzzle, Direction);
    }

    public override void SendExtraAI(BinaryWriter writer) => writer.Write(aimAngle);
    public override void ReceiveExtraAI(BinaryReader reader) => aimAngle = reader.ReadSingle();

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D staff = TextureAssets.Item[ItemID.MeteorStaff].Value;
        bool left = Direction.X < 0f;
        // 原版法杖是右上朝向，握点在左下；尺寸取贴图本身，避免硬编码大图坐标。
        Vector2 grip = new(staff.Width * .22f, staff.Height * .82f);
        Vector2 tip = new(staff.Width * .82f, staff.Height * .18f);
        float nativeAngle = (tip - grip).ToRotation();
        if (left) grip.Y = staff.Height - grip.Y;
        Main.spriteBatch.Draw(staff, Owner.MountedCenter - Main.screenPosition, null, lightColor,
            left ? aimAngle + nativeAngle : aimAngle - nativeAngle, grip, 1f,
            left ? SpriteEffects.FlipVertically : SpriteEffects.None, 0f);
        if (Projectile.ai[1] == 0f)
            SpreadChargeSigil.Draw(Muzzle, 128f, aimAngle, Main.GlobalTimeWrappedHourly,
                Charge, new Color(245, 140, 83));
        ChargeCastFeedback.DrawBar(Owner, Charge, new Color(245, 140, 83));
        return false;
    }
}
