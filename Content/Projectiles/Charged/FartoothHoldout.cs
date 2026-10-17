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
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Charged;

// ai[0]: left shot / loading / released. ai[1]: loaded count. ai[2]: load or release age.
public sealed class FartoothHoldout : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    private readonly int[] damage = new int[6];
    private readonly float[] knockback = new float[6];
    private Item boundItem;
    private int shownCount;
    private float layoutCount;
    public static int LoadDuration(int loaded) => (int)MathF.Ceiling(36 * MathF.Pow(.95f, Math.Clamp(loaded, 0, 5)));
    public static float SlotOffset(int index, float count) => (index - (count - 1) * .5f) * 22;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.timeLeft = 2; Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void SendExtraAI(BinaryWriter writer) { writer.Write(layoutCount); }
    public override void ReceiveExtraAI(BinaryReader reader) { layoutCount = reader.ReadSingle(); }
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.ModItem is not FartoothGreatbow weapon)
        { Projectile.Kill(); return; }
        bool local = Projectile.owner == Main.myPlayer;
        if (local && boundItem != null && !ReferenceEquals(boundItem, owner.HeldItem)) { Projectile.Kill(); return; }
        boundItem ??= owner.HeldItem;
        var state = owner.GetModPlayer<ChargedOperatorPlayer>();
        if (local && Projectile.ai[0] != 2 && (!ChargedOperatorWeapon.WorldInputAllowed(owner) || state.SuppressSniperRight && Projectile.ai[0] == 1))
        { Projectile.Kill(); return; }
        if (local && Projectile.ai[0] != 2)
        {
            Vector2 aim = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(Vector2.UnitX * owner.direction);
            if (Vector2.DistanceSquared(aim, Projectile.velocity) > .0004f) { Projectile.velocity = aim; Projectile.netUpdate = true; }
        }
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
        Projectile.Center = owner.MountedCenter; Projectile.timeLeft = 2;
        owner.ChangeDir(direction.X >= 0 ? 1 : -1); owner.heldProj = Projectile.whoAmI;
        owner.itemTime = owner.itemAnimation = 2; owner.itemRotation = (direction * owner.direction).ToRotation();
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, direction.ToRotation() - MathHelper.PiOver2);
        owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, direction.ToRotation() - MathHelper.PiOver2 + .2f * owner.direction);
        int count = Math.Clamp((int)Projectile.ai[1], 0, 6);
        layoutCount = MathHelper.Lerp(layoutCount, count, .25f);
        if (Projectile.ai[0] == 2)
        {
            if (++Projectile.ai[2] >= 60) Projectile.Kill();
            return;
        }
        if (local && Projectile.ai[0] == 0)
        {
            if (weapon.ReleaseAmmo(owner, out int baseDamage, out float kb, out int ammo))
            {
                Shoot(owner, weapon, direction, 0, baseDamage, kb, ammo, false);
                state.NextSniperLeftShot = Main.GameUpdateCount + 60;
            }
            Finish(owner); return;
        }
        if (local && !Main.mouseRight)
        {
            for (int i = 0; i < count; i++) Shoot(owner, weapon, direction, SlotOffset(i, count), damage[i], knockback[i], 0, true);
            if (count == 0) { Projectile.Kill(); return; }
            state.NextSniperLeftShot = Main.GameUpdateCount + 60;
            Finish(owner); return;
        }
        if (local && count < 6 && ++Projectile.ai[2] >= LoadDuration(count))
        {
            if (weapon.ReleaseAmmo(owner, out int baseDamage, out float kb, out _))
            {
                damage[count] = baseDamage; knockback[count] = kb;
                Projectile.ai[1] = ++count; Projectile.ai[2] = 0; Projectile.netUpdate = true;
            }
            else Projectile.ai[2] = LoadDuration(count); // Empty ammo does not create a free blade.
        }
        if (shownCount < count)
        {
            shownCount = count;
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = .38f, Pitch = -.25f + count * .08f }, owner.Center);
                if (local) CombatText.NewText(owner.Hitbox, new Color(236, 221, 149), Language.GetTextValue(
                    "Mods.ArknightsMod.ChargedArsenal." + (count % 2 == 1 ? "Tick" : "Tock")), true);
            }
        }
    }
    private void Finish(Player owner)
    {
        Projectile.ai[0] = 2; Projectile.ai[2] = 0; Projectile.netUpdate = true;
        int skill = ChargedOperatorPlayer.ActiveSkill(owner);
        owner.GetModPlayer<ChargedOperatorPlayer>().Recovery = skill == 2 ? 10 : skill == 1 ? 16 : 24;
    }
    private void Shoot(Player owner, FartoothGreatbow weapon, Vector2 aim, float offset, int baseDamage, float kb, int ammo, bool blade)
    {
        int skill = ChargedOperatorPlayer.ActiveSkill(owner);
        float bonus = ChargedOperatorPlayer.DamageMultiplier(ChargedOperatorKind.Fartooth, skill)
            * (owner.GetModPlayer<ChargedOperatorPlayer>().UnhurtTicks >= 300 ? 1.2f : 1);
        Vector2 origin = owner.MountedCenter + aim * 50 + new Vector2(-aim.Y, aim.X) * offset;
        Projectile shot = Projectile.NewProjectileDirect(owner.GetSource_ItemUse_WithPotentialAmmo(weapon.Item, ammo), origin,
            aim * (blade ? 28 : 22), blade ? ModContent.ProjectileType<FartoothBlade>() : ModContent.ProjectileType<ChargedOperatorShot>(),
            Math.Max(1, (int)(baseDamage * bonus)), kb, owner.whoAmI, blade ? skill : (int)ChargedOperatorKind.Fartooth, 0, blade ? 0 : skill);
        shot.CritChance = owner.GetWeaponCrit(weapon.Item);
        if (!Main.dedServ) SoundEngine.PlaySound((blade ? SoundID.Item45 : SoundID.Item40) with { Volume = .35f, MaxInstances = 2 }, origin);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Player owner = Main.player[Projectile.owner];
        if (owner.HeldItem.ModItem is not FartoothGreatbow) return false;
        Main.instance.LoadItem(owner.HeldItem.type);
        Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX), normal = new(-aim.Y, aim.X);
        Texture2D bow = TextureAssets.Item[owner.HeldItem.type].Value;
        Main.EntitySpriteDraw(bow, Projectile.Center + aim * 14 - Main.screenPosition, null, lightColor, aim.ToRotation(),
            bow.Size() / 2, 72f / bow.Width, owner.direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None);
        if (Projectile.ai[0] != 1) return false;
        Main.instance.LoadProjectile(ProjectileID.EmpressBlade);
        Texture2D blade = TextureAssets.Projectile[ProjectileID.EmpressBlade].Value;
        for (int i = 0; i < (int)Projectile.ai[1]; i++)
        {
            Vector2 position = Projectile.Center + aim * 50 + normal * SlotOffset(i, Math.Max(1, layoutCount));
            if (i == (int)Projectile.ai[1] - 1) position -= aim * 55 * (1 - Math.Clamp(Projectile.ai[2] / 8f, 0, 1));
            FartoothBlade.DrawBlade(blade, position, aim, Color.White);
        }
        if (Projectile.ai[1] < 6)
        {
            float progress = Math.Clamp(Projectile.ai[2] / LoadDuration((int)Projectile.ai[1]), 0, 1);
            Vector2 position = Projectile.Center + aim * (50 - 70 * (1 - progress))
                + normal * SlotOffset((int)Projectile.ai[1], Projectile.ai[1] + 1);
            FartoothBlade.DrawBlade(blade, position, aim, Color.White * progress * .6f);
        }
        return false;
    }
}
