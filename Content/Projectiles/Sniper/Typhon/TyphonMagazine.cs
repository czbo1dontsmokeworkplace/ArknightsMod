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
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Typhon;

// Clockwork-style insertion and fan redistribution; stored arrows remain harmless.
public sealed class TyphonMagazine : ModProjectile
{
    public const int Capacity = 6;
    public const float LoadedArrowDamageMultiplier = .5f;
    public static int LoadFrames(int count) => (int)MathF.Ceiling(18 * MathF.Pow(.85f, Math.Clamp(count, 0, Capacity - 1)));
    public static float FanAngle(int index, int count) => (index - (Math.Clamp(count, 1, Capacity) - 1) * .5f) * MathHelper.ToRadians(16);
    public static float LoadingFanAngle(int index, int count, float progress)
        => count == 0 ? 0 : (index - (count - 1 + (count >= Capacity ? 0 : Math.Clamp(progress * 2, 0, 1))) * .5f) * MathHelper.ToRadians(16);
    internal static string LoadTextKey(int count) => "Mods.ArknightsMod.ChargedArsenal." + (count % 2 == 1 ? "Tick" : "Tock");
    internal static SoundStyle LoadSound(int count) => count >= Capacity
        ? SoundID.Item23 with { Volume = .85f, MaxInstances = 6 }
        : SoundID.Item108 with { Volume = .3f, Pitch = count % 2 == 1 ? .12f : -.12f, MaxInstances = 6 };
    public override string Texture => "Terraria/Images/MagicPixel";
    private Item boundItem;
    private int loaded, loadClock, shownCount;
    private int halfwaySoundRound = -1, loadFlashAge = 20;
    private bool canLoad;
    private float LoadProgress => loaded >= Capacity || !canLoad ? 0 : Math.Clamp(loadClock / (float)LoadFrames(loaded), 0, 1);
    private readonly int[] damage = new int[Capacity], ammo = new int[Capacity];
    private readonly float[] knockback = new float[Capacity];
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.timeLeft = 2; Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void SendExtraAI(BinaryWriter writer) { writer.Write((byte)loaded); writer.Write(loadClock); writer.Write(canLoad); }
    public override void ReceiveExtraAI(BinaryReader reader) { loaded = Math.Clamp((int)reader.ReadByte(), 0, Capacity); loadClock = Math.Max(0, reader.ReadInt32()); canLoad = reader.ReadBoolean(); }
    private Vector2 SlotDirection(int i) => Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(LoadingFanAngle(i, loaded, LoadProgress));
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
        loadFlashAge = Math.Min(20, loadFlashAge + 1);
        if (local)
        {
            bool available = owner.HasAmmo(bow.Item);
            if (canLoad != available) { canLoad = available; Projectile.netUpdate = true; }
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
            {
                SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Assets/Sounds/Typhon/StarmadaFire") { Volume = .5f }, owner.Center);
                SoundEngine.PlaySound(SoundID.Item38 with { Volume = .7f }, owner.Center);
            }
            // Fire the complete fan immediately, with no orbit, gathering animation or shot queue.
            for (int i = 0; i < loaded; i++)
            {
                int index = Projectile.NewProjectile(owner.GetSource_ItemUse_WithPotentialAmmo(bow.Item, ammo[i]), Slot(i),
                    SlotDirection(i) * TyphonAstralShot.LaunchSpeed, ModContent.ProjectileType<TyphonAstralShot>(), Math.Max(1, (int)(damage[i] * LoadedArrowDamageMultiplier)), knockback[i], owner.whoAmI, 1);
                if (Main.projectile.IndexInRange(index)) Main.projectile[index].CritChance = owner.GetWeaponCrit(bow.Item);
            }
            Projectile.Kill(); return;
        }
        if (loaded < Capacity && canLoad)
        {
            int frames = LoadFrames(loaded);
            // Peers predict only the visible insertion; ammo and completed loads belong to the owner.
            loadClock = Math.Min(frames, loadClock + 1);
            if (loadClock >= (frames + 1) / 2 && halfwaySoundRound != loaded)
            {
                halfwaySoundRound = loaded;
                if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Item17 with { Volume = .65f, MaxInstances = 6 }, owner.Center);
            }
            if (local && loadClock >= frames)
            {
                if (bow.LoadArrow(owner, out int value, out float kb, out int selectedAmmo))
                {
                    damage[loaded] = value; knockback[loaded] = kb; ammo[loaded] = selectedAmmo;
                    loaded++; loadClock = 0; canLoad = owner.HasAmmo(bow.Item); Projectile.netUpdate = true;
                }
                else { canLoad = false; loadClock = 0; Projectile.netUpdate = true; }
            }
        }
        if (shownCount < loaded)
        {
            shownCount = loaded; loadFlashAge = 0;
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(LoadSound(loaded), owner.Center);
                Rectangle textArea = new((int)owner.Center.X - 14, (int)owner.Top.Y - 28, 28, 16);
                CombatText.NewText(textArea, loaded % 2 == 1 ? new Color(255, 200, 100) : new Color(155, 255, 255),
                    Language.GetTextValue(LoadTextKey(loaded)), true);
            }
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Player owner = Main.player[Projectile.owner];
        if (owner.HeldItem.ModItem is not TyphonBow) return false;
        Main.instance.LoadItem(owner.HeldItem.type);
        Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        Texture2D bow = TextureAssets.Item[owner.HeldItem.type].Value;
        DrawLoadedArrows();
        Main.EntitySpriteDraw(bow, Projectile.Center + aim * 12 - Main.screenPosition, null, lightColor, aim.ToRotation(),
            bow.Size() / 2, 1, owner.direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None);
        return false;
    }
    private void DrawLoadedArrows()
    {
        float flash = Math.Clamp(1 - loadFlashAge / 8f, 0, 1);
        int visible = loaded + (loaded < Capacity && canLoad ? 1 : 0);
        for (int i = 0; i < visible; i++)
        {
            bool inserting = i == loaded;
            float opacity = inserting ? LoadProgress : 1;
            Vector2 pointing = SlotDirection(i);
            Vector2 position = Slot(i) + pointing * (inserting ? 40 * (1 - LoadProgress) : 0);
            TyphonAstralVisuals.DrawHead(position, pointing.ToRotation(), TyphonAstralShot.RightScale, i * .15f,
                opacity, !inserting && (loaded == Capacity || i == loaded - 1) ? flash : 0);
        }
    }
}
