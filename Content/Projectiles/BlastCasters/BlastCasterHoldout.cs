using System;
using ArknightsMod.Content.Items.Weapons.BlastCasters;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.BlastCasters;

public sealed class BlastCasterHoldout : ModProjectile
{
    private float shotClock;
    private int volley, ticks;
    private int ItemType = -1;
    public override string Texture => BlastCasterRules.GunTexture;
    internal BlastCasterKind Kind => (BlastCasterKind)(int)Projectile.ai[0];
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16; Projectile.tileCollide = false;
        Projectile.ignoreWater = true; Projectile.penetrate = -1; Projectile.timeLeft = 2;
        Projectile.netImportant = true;
    }
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    internal static Vector2 GripPosition(Player owner, Vector2 aim) => owner.RotatedRelativePoint(owner.MountedCenter) + aim * 8f;
    internal static Vector2 Muzzle(Player owner, Vector2 aim, BlastCasterKind kind)
        => GripPosition(owner, aim) + BlastCasterRules.MuzzleOffset(new Vector2(72, 24), kind,
            aim.ToRotation(), aim.X >= 0 ? 1 : -1);
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.noItems || owner.CCed
            || owner.HeldItem.ModItem is not BlastCasterWeapon weapon || weapon.Kind != Kind
            || (ItemType >= 0 && owner.HeldItem.type != ItemType))
        { Projectile.Kill(); return; }
        ItemType = owner.HeldItem.type;
        bool local = owner.whoAmI == Main.myPlayer;
        if (local && (!owner.channel || !Main.hasFocus || Main.playerInventory || Main.blockInput || Main.drawingPlayerChat))
        { Projectile.Kill(); return; }
        ticks++;
        Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
        if (local)
        {
            Projectile relay = Kind == BlastCasterKind.Aphrissa ? AphrissaRelay.Find(owner) : null;
            Vector2 target = relay?.Center ?? Main.MouseWorld;
            Vector2 next = (target - owner.MountedCenter).SafeNormalize(aim);
            int mode = owner.GetModPlayer<BlastCasterPlayer>().Mode(weapon);
            int rank = owner.GetModPlayer<BlastCasterPlayer>().Rank;
            if (Vector2.DistanceSquared(next, aim) > .0004f || Projectile.ai[1] != mode || Projectile.ai[2] != rank)
            {
                Projectile.velocity = next; Projectile.ai[1] = mode; Projectile.ai[2] = rank;
                if (ticks % 4 == 0 || Projectile.localAI[2] != mode)
                { Projectile.netUpdate = true; Projectile.localAI[2] = mode; }
            }
            aim = next;
            if (ticks % 6 == 0) Projectile.netUpdate = true;
        }
        owner.ChangeDir(aim.X >= 0 ? 1 : -1);
        Projectile.Center = GripPosition(owner, aim); Projectile.rotation = aim.ToRotation();
        Projectile.direction = owner.direction; Projectile.timeLeft = 2;
        owner.heldProj = Projectile.whoAmI; owner.itemTime = owner.itemAnimation = 2;
        owner.itemRotation = (aim * owner.direction).ToRotation();
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
        Vector2 muzzle = Muzzle(owner, aim, Kind);
        if (local)
        {
            if (!owner.HasAmmo(weapon.Item)) { Projectile.Kill(); return; }
            shotClock -= owner.GetWeaponAttackSpeed(weapon.Item) * BlastCasterRules.RateMultiplier(Kind, (int)Projectile.ai[1], (int)Projectile.ai[2]);
            if (shotClock <= 0)
            {
                // Piercing terrain includes firing with the barrel inside a block.
                Fire(owner, weapon, muzzle, aim);
                shotClock += Math.Max(1, weapon.Item.useTime);
            }
        }
        if (ticks % 18 == 1 && BlastCasterVisuals.Visible(muzzle))
            SoundEngine.PlaySound((Kind == BlastCasterKind.Aphrissa ? SoundID.Item93 : SoundID.Item34)
                with { Volume = .22f, Pitch = Kind == BlastCasterKind.Ifrit ? -.15f : .12f, MaxInstances = 3 }, muzzle);
        if (ticks % 3 == 0) BlastCasterVisuals.Muzzle(muzzle, aim, Kind, ticks);
    }
    private void Fire(Player owner, BlastCasterWeapon weapon, Vector2 muzzle, Vector2 aim)
    {
        if (!weapon.PickGel(owner, BlastCasterRules.ConsumesGel(volley), out int damage, out float knockback, out int ammo))
        { Projectile.Kill(); return; }
        volley++;
        var skills = owner.GetModPlayer<WeaponPlayer>();
        var runtime = owner.GetModPlayer<BlastCasterPlayer>();
        int rank = runtime.Rank, mode = runtime.Mode(weapon);
        if (skills.CurrentSkill?.Key.Item == weapon.Name && BlastCasterRules.ChargedShot(Kind, skills.Skill) && skills.StockCount > 0)
        { mode = 2; skills.DelStockCount(); }
        damage = Math.Max(1, (int)(damage * BlastCasterRules.DamageMultiplier(Kind, mode, rank)));
        var source = owner.GetSource_ItemUse_WithPotentialAmmo(weapon.Item, ammo);
        Projectile relay = Kind == BlastCasterKind.Aphrissa ? AphrissaRelay.Find(owner) : null;
        Projectile shot = Projectile.NewProjectileDirect(source, muzzle, aim * BlastCasterRules.Step(Kind),
            ModContent.ProjectileType<BlastCasterFluxPacket>(), damage, knockback, owner.whoAmI,
            mode, relay?.Center.X ?? -1, relay?.Center.Y ?? -1);
        if (shot.ModProjectile is BlastCasterFluxPacket packet)
            packet.Configure(Kind, Main.MouseWorld, relay?.identity ?? -1);
        shot.CritChance = owner.GetWeaponCrit(weapon.Item);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        int facing = Projectile.direction;
        Vector2 origin = BlastCasterRules.Grip * texture.Size();
        if (facing < 0) origin.Y = texture.Height - origin.Y;
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, lightColor,
            Projectile.rotation, origin, BlastCasterRules.GunWidth(Kind) / texture.Width,
            facing < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None, 0);
        BlastCasterVisuals.RestoreBatch();
        return false;
    }
}
