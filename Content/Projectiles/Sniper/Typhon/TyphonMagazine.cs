using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Charged;
using ArknightsMod.Content.Items.Weapons.Sniper.Typhon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Typhon;

// A stationary fan of harmless loaded arrows; actual projectiles are created on release.
public sealed class TyphonMagazine : ModProjectile
{
    public const int Capacity = 6;
    public static int LoadFrames(int count) => (int)MathF.Ceiling(18 * MathF.Pow(.85f, Math.Clamp(count, 0, Capacity - 1)));
    public static float FanAngle(int index, int count) => (index - (Math.Clamp(count, 1, Capacity) - 1) * .5f) * MathHelper.ToRadians(16);
    public override string Texture => "Terraria/Images/MagicPixel";
    private Item boundItem;
    private int loaded, loadClock, shownCount;
    private readonly int[] damage = new int[Capacity], ammo = new int[Capacity];
    private readonly float[] knockback = new float[Capacity];
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.timeLeft = 2; Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void SendExtraAI(BinaryWriter writer) { writer.Write((byte)loaded); writer.Write(loadClock); }
    public override void ReceiveExtraAI(BinaryReader reader) { loaded = Math.Clamp((int)reader.ReadByte(), 0, Capacity); loadClock = Math.Max(0, reader.ReadInt32()); }
    private Vector2 SlotDirection(int i) => Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(FanAngle(i, loaded));
    private Vector2 Slot(int i) => Projectile.Center + SlotDirection(i) * 68;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.CCed || owner.noItems || owner.HeldItem.ModItem is not TyphonBow bow)
        { Projectile.Kill(); return; }
        bool local = Projectile.owner == Main.myPlayer;
        if (local && boundItem != null && !ReferenceEquals(boundItem, owner.HeldItem)) { Projectile.Kill(); return; }
        boundItem ??= owner.HeldItem;
        Projectile.Center = owner.MountedCenter; Projectile.timeLeft = 2;
        // Preserve already paid-for rounds while inventory/autopause blocks world input.
        if (local && !ChargedOperatorWeapon.WorldInputAllowed(owner)) return;
        if (local)
        {
            Vector2 aim = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(Vector2.UnitX * owner.direction);
            if (Vector2.DistanceSquared(aim, Projectile.velocity) > .0004f) { Projectile.velocity = aim; Projectile.netUpdate = true; }
        }
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        owner.ChangeDir(direction.X < 0 ? -1 : 1); owner.heldProj = Projectile.whoAmI;
        owner.itemTime = owner.itemAnimation = 2; owner.itemRotation = (direction * owner.direction).ToRotation();
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, direction.ToRotation() - MathHelper.PiOver2);
        if (local && !Main.mouseRight)
        {
            if (loaded > 0 && !Main.dedServ)
                SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Assets/Sounds/Typhon/StarmadaFire") { Volume = .5f }, owner.Center);
            // Fire the complete fan immediately, with no orbit, gathering animation or shot queue.
            for (int i = 0; i < loaded; i++)
            {
                int index = Projectile.NewProjectile(owner.GetSource_ItemUse_WithPotentialAmmo(bow.Item, ammo[i]), Slot(i),
                    SlotDirection(i) * TyphonAstralShot.LaunchSpeed, ModContent.ProjectileType<TyphonAstralShot>(), damage[i], knockback[i], owner.whoAmI, 1);
                if (Main.projectile.IndexInRange(index)) Main.projectile[index].CritChance = owner.GetWeaponCrit(bow.Item);
            }
            Projectile.Kill(); return;
        }
        if (local && loaded < Capacity && ++loadClock >= LoadFrames(loaded))
        {
            if (bow.LoadArrow(owner, out int value, out float kb, out int selectedAmmo))
            {
                damage[loaded] = value; knockback[loaded] = kb; ammo[loaded] = selectedAmmo;
                loaded++; loadClock = 0; Projectile.netUpdate = true;
            }
            else loadClock = LoadFrames(loaded);
        }
        if (shownCount < loaded)
        {
            shownCount = loaded;
            if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Item29 with { Volume = .3f, Pitch = -.2f + loaded * .07f }, owner.Center);
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Player owner = Main.player[Projectile.owner];
        if (owner.HeldItem.ModItem is not TyphonBow) return false;
        Main.instance.LoadItem(owner.HeldItem.type);
        Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        Texture2D bow = TextureAssets.Item[owner.HeldItem.type].Value;
        Main.EntitySpriteDraw(bow, Projectile.Center + aim * 12 - Main.screenPosition, null, lightColor, aim.ToRotation(),
            bow.Size() / 2, 1, owner.direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None);
        for (int i = 0; i < loaded; i++)
            TyphonAstralVisuals.DrawHead(Slot(i), SlotDirection(i).ToRotation(), TyphonAstralShot.RightScale, i * .15f);
        return false;
    }
}
