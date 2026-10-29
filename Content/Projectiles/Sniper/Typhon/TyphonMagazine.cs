using System;
using System.IO;
using System.Linq;
using ArknightsMod.Content.Items.Weapons.Charged;
using ArknightsMod.Content.Items.Weapons.Sniper.Typhon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Typhon;

// The former six insertion animations are archived in Doc/PhoenixGammaPlasma.
// Charging keeps their combined duration and the same owner-bound cancellation.
public sealed class TyphonMagazine : ModProjectile
{
    public const int Capacity = 6;
    public static int LoadFrames(int count) => (int)MathF.Ceiling(18 * MathF.Pow(.85f, Math.Clamp(count, 0, Capacity - 1)));
    internal static readonly int ChargeFrames = Enumerable.Range(0, Capacity).Sum(LoadFrames);
    internal const int BeamFrames = 48; // Original rank-six Mirage: 0.8 seconds.
    private Item boundItem;
    private int chargeTicks;
    private bool Firing => Projectile.ai[0] > 0;
    internal float TotalLoadProgress => Firing ? Math.Clamp(1 - Projectile.ai[1] / BeamFrames, 0, 1) : Math.Clamp(chargeTicks / (float)ChargeFrames, 0, 1);
    internal bool BelongsTo(Item item) => boundItem == null || ReferenceEquals(boundItem, item);
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.timeLeft = 2; Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void SendExtraAI(BinaryWriter writer) => writer.Write(chargeTicks);
    public override void ReceiveExtraAI(BinaryReader reader) => chargeTicks = Math.Clamp(reader.ReadInt32(), 0, ChargeFrames);
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.CCed || owner.noItems || owner.HeldItem.ModItem is not TyphonBow bow)
        { Projectile.Kill(); return; }
        bool local = Projectile.owner == Main.myPlayer;
        if (local && !BelongsTo(owner.HeldItem)) { Projectile.Kill(); return; }
        boundItem ??= owner.HeldItem;
        Projectile.Center = owner.MountedCenter; Projectile.timeLeft = 2;
        if (!Firing)
        {
            if (local && !ChargedOperatorWeapon.WorldInputAllowed(owner)) return;
            if (local && (!Main.mouseRight || !owner.HasAmmo(bow.Item))) { Projectile.Kill(); return; }
            if (local)
            {
                Vector2 next = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(Vector2.UnitX * owner.direction);
                if (Vector2.DistanceSquared(next, Projectile.velocity) > .0004f)
                { Projectile.velocity = next; Projectile.netUpdate = true; }
            }
            chargeTicks = Math.Min(ChargeFrames, chargeTicks + 1);
            if (local && chargeTicks >= ChargeFrames) Fire(owner, bow);
            else if (local && chargeTicks % 6 == 0) Projectile.netUpdate = true;
        }
        else if (++Projectile.ai[1] >= BeamFrames) { Projectile.Kill(); return; }
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        owner.ChangeDir(direction.X < 0 ? -1 : 1); owner.heldProj = Projectile.whoAmI;
        owner.itemTime = owner.itemAnimation = 2; owner.itemRotation = (direction * owner.direction).ToRotation();
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, direction.ToRotation() - MathHelper.PiOver2);
    }
    private void Fire(Player owner, TyphonBow bow)
    {
        int totalDamage = 0, ammo = 0, paid = 0; float knockback = 0;
        // Pay the former six-round budget together after charging. If fewer
        // arrows remain, still release the rays with only the paid damage.
        for (int i = 0; i < Capacity; i++)
        {
            if (!bow.LoadArrow(owner, out int damage, out float kb, out int selectedAmmo)) break;
            totalDamage += damage; knockback += kb; ammo = selectedAmmo; paid++;
        }
        if (paid == 0) { Projectile.Kill(); return; }
        Projectile.ai[0] = 1; Projectile.ai[1] = 0; Projectile.netUpdate = true;
        Vector2 axis = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        // Six hits per beam share the former six half-damage arrows' budget.
        int perHit = Math.Max(1, (int)MathF.Round(totalDamage / 24f));
        foreach (int side in new[] { -1, 1 })
        {
            var ray = Projectile.NewProjectileDirect(owner.GetSource_ItemUse_WithPotentialAmmo(bow.Item, ammo),
                owner.MountedCenter, axis, ModContent.ProjectileType<TyphonPlasmaRay>(), perHit,
                knockback / paid, owner.whoAmI, side, 0, Projectile.identity);
            ray.CritChance = owner.GetWeaponCrit(bow.Item);
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Player owner = Main.player[Projectile.owner];
        if (owner.HeldItem.ModItem is not TyphonBow) return false;
        Main.instance.LoadItem(owner.HeldItem.type);
        Texture2D bow = TextureAssets.Item[owner.HeldItem.type].Value;
        Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        Main.EntitySpriteDraw(bow, Projectile.Center + aim * 12 - Main.screenPosition, null, lightColor,
            aim.ToRotation(), bow.Size() / 2, 1, owner.direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None);
        return false;
    }
}
