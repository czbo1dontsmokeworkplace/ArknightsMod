using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed class DroneCoreMedic : ModProjectile
{
    public override string Texture => "ArknightsMod/Content/NPCs/Enemy/ThroughChapter4/ArtsMasterA2";
    public override string LocalizationCategory => "Projectiles.MaterialistArsenal";
    public override void SetStaticDefaults() => Main.projFrames[Type] = 8;
    public override void SetDefaults()
    {
        Projectile.width = 40; Projectile.height = 34; Projectile.timeLeft = 2;
        Projectile.friendly = Projectile.hostile = false;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.penetrate = -1;
        Projectile.netImportant = true;
    }
    public override bool? CanDamage() => false;
    private Vector2 Muzzle
    {
        get
        {
            float x = Projectile.frame switch { 4 => 21, 5 => 19, 6 => 21, _ => 23 };
            return Projectile.Center + new Vector2(Projectile.spriteDirection * x, 17.5f).RotatedBy(Projectile.rotation);
        }
    }
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        DroneCoreMedicPlayer state = owner.GetModPlayer<DroneCoreMedicPlayer>();
        if (!owner.active || owner.dead || !state.Equipped) { Projectile.Kill(); return; }
        Projectile.timeLeft = 2;
        Vector2 goal = owner.Center + new Vector2(-owner.direction * 75, -100);
        Vector2 desired = owner.velocity + (goal - Projectile.Center) * .10f;
        if (desired.LengthSquared() > 40 * 40) desired = desired.SafeNormalize(Vector2.UnitX) * 40;
        Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, .14f);
        Projectile.spriteDirection = owner.Center.X >= Projectile.Center.X ? 1 : -1;
        Projectile.rotation = MathHelper.Lerp(Projectile.rotation, MathHelper.Clamp(Projectile.velocity.X * .012f, -.18f, .18f), .12f);
        Projectile.frame = (int)(++Projectile.localAI[0] / 6) % 8;
        bool authority = Main.netMode != NetmodeID.MultiplayerClient;
        if (authority && Projectile.ai[0] == 0 && state.Cooldown == 0 && state.CanTreat && Projectile.Distance(owner.Center) < 280)
        { Projectile.ai[0] = 1; Projectile.netUpdate = true; }
        else if (Projectile.ai[0] > 0)
        {
            Projectile.ai[0]++;
            if (authority && Projectile.ai[0] == 25 && Projectile.Distance(owner.Center) < 400) state.Treat();
            if (Projectile.ai[0] >= 43) { Projectile.ai[0] = 0; if (authority) Projectile.netUpdate = true; }
        }
        if (!Main.dedServ && Projectile.ai[0] == 25)
        {
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = .38f, Pitch = .2f, MaxInstances = 2 }, owner.Center);
            for (int i = 0; i < 6; i++) MaterialistVisuals.Nano(owner.Center, Main.rand.NextVector2Circular(2, 2),
                new Color(83, 245, 148), .6f, 18);
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        Rectangle frame = texture.Frame(1, 8, 0, Projectile.frame);
        SpriteEffects flip = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        // Identical atlas frame/origin/flip for rim and body prevents sampling adjacent animation frames.
        for (int i = 0; i < 8; i++)
        {
            Vector2 offset = (i * MathHelper.PiOver4).ToRotationVector2() * 1.25f;
            Main.spriteBatch.Draw(texture, Projectile.Center + offset - Main.screenPosition, frame,
                new Color(57, 186, 113, 0) * .30f, Projectile.rotation, frame.Size() * .5f, 1, flip, 0);
        }
        Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, frame, Color.Lerp(lightColor, Color.White, .45f),
            Projectile.rotation, frame.Size() * .5f, 1, flip, 0);
        float age = Projectile.ai[0];
        if (age > 0 && age < 25) MaterialistVisuals.Glow(Muzzle, new Color(70, 230, 136) * (age / 25f * .45f), new Vector2(22));
        if (age >= 25)
        {
            Vector2 delta = Main.player[Projectile.owner].Center - Muzzle;
            MaterialistVisuals.DrawMedicLaser(Muzzle, delta, MathHelper.Clamp((43 - age) / 18f, 0, 1), (int)age);
        }
        return false;
    }
}
