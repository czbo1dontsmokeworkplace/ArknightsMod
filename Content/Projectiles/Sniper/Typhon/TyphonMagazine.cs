using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Sniper.Archers;
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

// One visible insertion, held until right-button release. Stored arrows are harmless.
public sealed class TyphonMagazine : ModProjectile
{
    public const int Capacity = 1;
    public const float LoadedArrowDamageMultiplier = ArcherChargedArrow.DamageMultiplier;
    public static int LoadFrames(int count) => 54;
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
    // There is one slot: completing it holds the bar at full until release.
    internal float TotalLoadProgress => Math.Clamp((loaded + LoadProgress) / Capacity, 0, 1);
    internal bool BelongsTo(Item item) => boundItem == null || ReferenceEquals(boundItem, item);
    private readonly int[] damage = new int[Capacity], ammo = new int[Capacity], projectileTypes = new int[Capacity];
    private readonly float[] knockback = new float[Capacity], speeds = new float[Capacity];
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.timeLeft = 2; Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void SendExtraAI(BinaryWriter writer) { writer.Write((byte)loaded); writer.Write(loadClock); writer.Write(canLoad); writer.Write(ammo[0]); writer.Write(projectileTypes[0]); }
    public override void ReceiveExtraAI(BinaryReader reader) { loaded = Math.Clamp((int)reader.ReadByte(), 0, Capacity); loadClock = Math.Max(0, reader.ReadInt32()); canLoad = reader.ReadBoolean(); ammo[0] = reader.ReadInt32(); projectileTypes[0] = reader.ReadInt32(); }
    private Vector2 SlotDirection(int i) => Projectile.velocity.SafeNormalize(Vector2.UnitX);
    private Vector2 Slot(int i) {
        Item item = Main.player[Projectile.owner].HeldItem;
        float width = item.width;
        if (!Main.dedServ) { Main.instance.LoadItem(item.type); width = TextureAssets.Item[item.type].Value.Width; }
        return PreviewCenter(Projectile.Center, SlotDirection(i), width, ArcherChargedArrow.ArtLength * .5f * PreviewScale(1), 1);
    }
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.CCed || owner.noItems || !ArcherCharge.Supports(owner.HeldItem))
        { Projectile.Kill(); return; }
        Item bowItem = owner.HeldItem;
        int tier = ArcherCharge.Tier(bowItem);
        bool local = Projectile.owner == Main.myPlayer;
        if (local && boundItem != null && !ReferenceEquals(boundItem, owner.HeldItem)) { Projectile.Kill(); return; }
        boundItem ??= owner.HeldItem;
        Projectile.Center = owner.MountedCenter; Projectile.timeLeft = 2;
        // Preserve already paid-for rounds while inventory/autopause blocks world input.
        if (local && !ChargedOperatorWeapon.WorldInputAllowed(owner)) return;
        loadFlashAge = Math.Min(20, loadFlashAge + 1);
        if (local)
        {
            bool available = owner.HasAmmo(bowItem);
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
            // One paid round, one high-speed explosive arrow for all five bows.
            for (int i = 0; i < loaded; i++)
            {
                int index = Projectile.NewProjectile(owner.GetSource_ItemUse_WithPotentialAmmo(bowItem, ammo[i]), Slot(i),
                    SlotDirection(i) * Math.Max(ArcherChargedArrow.LaunchSpeed, speeds[i] * ArcherChargedArrow.SpeedMultiplier), ArcherCharge.Resolve(projectileTypes[i], tier), Math.Max(1, (int)(damage[i] * LoadedArrowDamageMultiplier)), knockback[i], owner.whoAmI, projectileTypes[i] == ProjectileID.WoodenArrowFriendly ? tier : 0);
                if (Main.projectile.IndexInRange(index)) {
                    Main.projectile[index].CritChance = owner.GetWeaponCrit(bowItem);
                }
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
                if (Pick(owner, bowItem, out int value, out float kb, out int selectedAmmo, out int selectedType, out float selectedSpeed))
                {
                    damage[loaded] = value; knockback[loaded] = kb; ammo[loaded] = selectedAmmo; projectileTypes[loaded] = selectedType; speeds[loaded] = selectedSpeed;
                    loaded++; loadClock = 0; canLoad = owner.HasAmmo(bowItem); Projectile.netUpdate = true;
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
    private static bool Pick(Player owner, Item item, out int damage, out float knockback, out int ammo, out int type, out float speed) {
        if (item.ModItem is TyphonBow bow) return bow.LoadArrow(owner, out damage, out knockback, out ammo, out type, out speed);
        return ArcherCharge.Pick(item, owner, out damage, out knockback, out ammo, out type, out speed);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Player owner = Main.player[Projectile.owner];
        if (!ArcherCharge.Supports(owner.HeldItem)) return false;
        Main.instance.LoadItem(owner.HeldItem.type);
        Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        Texture2D bow = TextureAssets.Item[owner.HeldItem.type].Value;
        bool visible = loaded > 0 || canLoad;
        DrawChargePreview(bow, null, Projectile.Center, aim, visible ? TotalLoadProgress : 0,
            true, visible, Math.Clamp(1-loadFlashAge/8f,0,1), lightColor, owner.direction);
        return false;
    }
    internal static float PreviewScale(float progress) => TyphonAstralShot.LeftScale * .5f * MathHelper.Lerp(1, 2, Math.Clamp(progress,0,1));
    internal static float RearOffset(float bowWidth, float progress) => 12-5*progress-bowWidth*.5f*(1-.08f*progress)-6;
    internal static Vector2 PreviewCenter(Vector2 center, Vector2 aim, float bowWidth, float halfArrowLength, float progress) =>
        center + aim * (RearOffset(bowWidth,progress) + 24*(1-progress) + halfArrowLength);
    internal static void DrawChargePreview(Texture2D bow, Texture2D ammoArt, Vector2 center, Vector2 aim,
        float progress, bool special, bool visible, float flash, Color lightColor, int direction) {
        progress = Math.Clamp(progress,0,1);
        aim = aim.SafeNormalize(Vector2.UnitX);
        Vector2 normal = new(-aim.Y,aim.X);
        Vector2 bowCenter = center+aim*(12-5*progress);
        Vector2 nock = center+aim*(RearOffset(bow.Width,progress)+24*(1-progress));
        if (visible && progress > 0) {
            float opacity = Math.Min(1,progress*4);
            if (special || ammoArt == null) {
                float scale=PreviewScale(progress);
                Vector2 at=PreviewCenter(center,aim,bow.Width,TyphonAstralVisuals.ArrowLength*.5f*scale,progress);
                TyphonAstralVisuals.DrawHead(at,aim.ToRotation(),scale,0,opacity,flash);
            }
            else {
                float scale=MathHelper.Lerp(1,2,progress);
                float halfSpan=(ammoArt.Width+ammoArt.Height)*.3535534f*scale;
                Vector2 at=PreviewCenter(center,aim,bow.Width,halfSpan,progress);
                Main.EntitySpriteDraw(ammoArt,at-Main.screenPosition,null,Color.White*opacity,aim.ToRotation()+MathHelper.PiOver4,
                    ammoArt.Size()*.5f,scale,SpriteEffects.None);
            }
        }
        foreach (int sign in new[]{-1,1}) {
            Vector2 from=bowCenter+normal*sign*bow.Height*.4f*(1+.05f*progress);
            Vector2 delta=nock-from;
            Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value,from-Main.screenPosition,new Rectangle(0,0,1,1),
                new Color(190,225,235)*.8f,delta.ToRotation(),Vector2.Zero,new Vector2(delta.Length(),1.2f),SpriteEffects.None);
        }
        Main.EntitySpriteDraw(bow,bowCenter-Main.screenPosition,null,lightColor,aim.ToRotation(),bow.Size()*.5f,
            new Vector2(1-.08f*progress,1+.05f*progress),direction<0?SpriteEffects.FlipVertically:SpriteEffects.None);
    }
}
