using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;

namespace ArknightsMod.Content.Items.Weapons.Guard.Chen;

// Independent hit and visual carrier: never changes item timers, arms or heldProj.
public sealed class ChenSkillTwoOverlay : ModProjectile
{
    private Player afterimage;
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Guard/Chen/Skill_1_Release_Effect_2";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 4; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee; Projectile.penetrate = -1;
        Projectile.tileCollide = false; Projectile.timeLeft = 18;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Projectile.timeLeft >= 13;
    public override void AI()
    {
        if (Projectile.localAI[0]++ == 0 && !Main.dedServ)
            SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/ChenSwordSkill2") { MaxInstances = 2 }, Projectile.Center);
        Projectile.rotation = Projectile.velocity.ToRotation();
    }
    public override bool? Colliding(Rectangle a, Rectangle b)
    {
        float at = 0;
        return Collision.CheckAABBvLineCollision(b.TopLeft(), b.Size(), Projectile.Center,
            Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 580, 100, ref at);
    }
    public override void OnKill(int timeLeft) => ChenSwordRules.EndSkill(Main.player[Projectile.owner], 1);
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
        float t = 1 - Projectile.timeLeft / 18f;
        Main.EntitySpriteDraw(texture, Projectile.Center + Projectile.velocity * (110 + 160 * t) - Main.screenPosition,
            null, new Color(255, 110, 80, 0) * (1 - t), Projectile.rotation,
            texture.Size() * .5f, new Vector2(2, .8f), SpriteEffects.None);
        DrawAfterimage(t);
        return false;
    }
    private void DrawAfterimage(float progress)
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active) return;
        if (afterimage == null) {
            // Clone the projectile owner, so other players see the correct outfit.
            afterimage = owner.clientClone();
            afterimage.CopyVisuals(owner);
            for (int i = 0; i < afterimage.armor.Length; i++) afterimage.armor[i] = owner.armor[i].Clone();
            for (int i = 0; i < afterimage.dye.Length; i++) afterimage.dye[i] = owner.dye[i].Clone();
            afterimage.ResetEffects();
            afterimage.ResetVisibleAccessories();
            afterimage.UpdateDyes();
            afterimage.DisplayDollUpdate();
            afterimage.invis = false;
        }
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        afterimage.direction = direction.X >= 0 ? 1 : -1;
        afterimage.velocity = direction * 12;
        afterimage.skipAnimatingValuesInPlayerFrame = true;
        afterimage.PlayerFrame();
        afterimage.skipAnimatingValuesInPlayerFrame = false;
        float rotation = direction.ToRotation() - (afterimage.direction < 0 ? MathHelper.Pi : 0);
        Vector2 center = Projectile.Center + direction * (40 + 480 * MathHelper.SmoothStep(0, 1, progress));
        for (int i = 2; i >= 0; i--) {
            float opacity = (1 - progress) * (i == 0 ? .85f : .22f);
            afterimage.immuneAlpha = (int)(255 * (1 - opacity));
            Vector2 position = center - direction * (i * 24) - afterimage.Size * .5f;
            Main.PlayerRenderer.DrawPlayer(Main.Camera, afterimage, position, rotation, afterimage.Size * .5f, 0, 1.3f);
        }
    }
}
