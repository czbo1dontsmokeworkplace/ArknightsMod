using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Charged;

// Visual-only equip / guard / reverse equip. The actual attack remains a
// separate projectile, preserving its one-target limit and captured crit.
public sealed class DreadnoughtDrawnBlade : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    private int itemType, endAge;
    private float drawAge;
    private float aim;
    private int facing = 1;
    private bool initialized;
    private ChargedOperatorKind Kind => (ChargedOperatorKind)(int)Projectile.ai[0];
    public override void SetDefaults() {
        Projectile.width = Projectile.height = 2; Projectile.timeLeft = 2;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void AI() {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.CCed) { Projectile.Kill(); return; }
        bool held = owner.HeldItem.ModItem is ChargedOperatorWeapon weapon && weapon.Melee && weapon.Kind == Kind;
        if (!initialized) {
            if (!held) { Projectile.Kill(); return; }
            initialized = true; itemType = owner.HeldItem.type;
            aim = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction).ToRotation();
            facing = owner.direction * (int)owner.gravDir;
        }
        if (!held) { if (++endAge >= 16) { Projectile.Kill(); return; } }
        else if (endAge > 0) { Projectile.Kill(); return; }
        Projectile.timeLeft = 2; Projectile.Center = owner.MountedCenter + new Vector2(0, owner.gfxOffY);
        if (drawAge < 16) drawAge++;
        bool attacking = false;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == Projectile.owner && p.ModProjectile is DreadnoughtSwing) { attacking = true; break; }
        Projectile.localAI[0] = attacking ? 1 : 0;
        if (held && !attacking) {
            if (Projectile.owner == Main.myPlayer) {
                float wanted = (Main.MouseWorld - owner.MountedCenter).ToRotation();
                aim = aim.AngleLerp(wanted, .15f);
                if (++Projectile.localAI[1] >= 6) { Projectile.localAI[1] = 0; Projectile.netUpdate = true; }
            }
            facing = (MathF.Cos(aim) < 0 ? -1 : 1) * (int)owner.gravDir;
            float draw = Math.Clamp(drawAge / 16f, 0, 1);
            draw = draw * draw * (3 - 2 * draw);
            float ready = aim + MathHelper.Lerp(.05f, DreadnoughtSwingMotion.ReadyAngle, draw) * facing;
            owner.ChangeDir(MathF.Cos(aim) < 0 ? -1 : 1);
            BladeSwingTemplate.Pose(owner, ready);
        }
        if (drawAge == 2 && !Main.dedServ)
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = .25f, Pitch = .65f }, owner.Center);
    }
    public override void SendExtraAI(BinaryWriter writer) {
        writer.Write(initialized); writer.Write(itemType); writer.Write(drawAge); writer.Write(aim); writer.Write(facing); writer.Write(endAge);
    }
    public override void ReceiveExtraAI(BinaryReader reader) {
        initialized = reader.ReadBoolean(); itemType = reader.ReadInt32(); drawAge = reader.ReadSingle();
        aim = reader.ReadSingle(); facing = reader.ReadInt32(); endAge = reader.ReadInt32();
    }
    public override bool PreDraw(ref Color lightColor) {
        if (!initialized || Projectile.localAI[0] != 0 || itemType <= 0) return false;
        Main.instance.LoadItem(itemType);
        Texture2D texture = Kind == ChargedOperatorKind.Melantha
            ? ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Weapons/Guard/Melantha/MelanthasSword_white").Value
            : TextureAssets.Item[itemType].Value;
        float t = Math.Clamp(endAge > 0 ? 1 - endAge / 16f : drawAge / 16f, 0, 1);
        t = t * t * (3 - 2 * t);
        bool flip = facing * (int)Main.player[Projectile.owner].gravDir < 0;
        DreadnoughtBladeLayout layout = DreadnoughtBladeLayout.For(Kind, texture.Size());
        float reach = ChargedOperatorRules.Reach(Kind, true) * DreadnoughtRules.WeaponScale;
        Vector2 center = Projectile.Center + new Vector2(-facing * 14, 9) * (1 - t);
        float angle = aim + MathHelper.Lerp(.05f, DreadnoughtSwingMotion.ReadyAngle, t) * facing;
        Vector2 axis = angle.ToRotationVector2();
        // A restrained scabbard silhouette: the weapon slides out horizontally
        // before the wrist rolls into its upward-tilted guard; reverse on unequip.
        var pixel = TextureAssets.MagicPixel.Value;
        Main.EntitySpriteDraw(pixel, center - axis * reach * .55f - Main.screenPosition, new Rectangle(0, 0, 1, 1),
            new Color(30, 26, 39) * (1 - t), angle, new Vector2(0, .5f), new Vector2(reach * .7f, 7), SpriteEffects.None);
        center += axis * (1 - t) * -reach * .45f;
        Main.EntitySpriteDraw(texture, center - Main.screenPosition, null, lightColor * (.2f + .8f * t),
            angle - layout.Axis(flip).ToRotation(), layout.Origin(texture.Width, flip), reach / layout.Axis(flip).Length(),
            flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
        return false;
    }
}
