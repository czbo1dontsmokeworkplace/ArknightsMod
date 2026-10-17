using System;
using ArknightsMod.Content.Items.Weapons.Medic;
using ArknightsMod.Content.Items.Weapons.Medic.Sussurro;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Medic.Warfarin;

/// <summary>紧急包扎抛出的血浆包；先越过目标，再在半秒后回转结算医疗。</summary>
public sealed class WarfarinEmergencyDressing : ModProjectile
{
    public override string Texture => "ArknightsMod/Content/Items/Weapons/WarfarinWeaponTrueExtra";

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 18;
        Projectile.friendly = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 180;
        Projectile.netImportant = true;
    }

    public override bool? CanDamage() => false;

    public override void AI()
    {
        Projectile.localAI[0]++;
        float age = Projectile.localAI[0];
        Player owner = Main.player[Projectile.owner];
        int targetIndex = (int)Projectile.ai[0];
        Player target = Main.player.IndexInRange(targetIndex) ? Main.player[targetIndex] : owner;
        if (!owner.active || owner.dead || !target.active || target.dead)
        {
            Projectile.Kill();
            return;
        }

        if (age < MedicalTreatment.InitialTreatmentDelayTicks)
        {
            Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + .14f, 10f);
        }
        else
        {
            Vector2 toTarget = target.MountedCenter - Projectile.Center;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity,
                toTarget.SafeNormalize(Vector2.UnitY) * 11f, .13f);
            if (toTarget.LengthSquared() < 24f * 24f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int amount = MedicHealingNet.Heal(owner, target, Math.Clamp((int)Projectile.ai[1], 1, 48));
                if (amount > 0)
                {
                    Burst(target.MountedCenter, 28);
                    WarfarinPlasmaVisuals.DressingBurst(target.MountedCenter);
                    SoundEngine.PlaySound(SoundID.Item4 with { Volume = .3f, Pitch = -.1f }, target.MountedCenter);
                }
                Projectile.Kill();
                return;
            }
        }

        Projectile.rotation = Projectile.velocity.ToRotation();
        Lighting.AddLight(Projectile.Center, .42f, .025f, .09f);
        if (!Main.dedServ)
        {
            int count = age < MedicalTreatment.InitialTreatmentDelayTicks ? 2 : 3;
            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 5f),
                    i == 0 && age >= MedicalTreatment.InitialTreatmentDelayTicks ? DustID.RedTorch : DustID.Blood,
                    -Projectile.velocity * Main.rand.NextFloat(.045f, .09f) + Main.rand.NextVector2Circular(.5f, .5f),
                    20, default, Main.rand.NextFloat(.68f, 1.05f));
                dust.noGravity = true;
            }
        }
    }

    private static void Burst(Vector2 position, int count)
    {
        if (Main.dedServ)
            return;
        for (int i = 0; i < count; i++)
        {
            Dust dust = Dust.NewDustPerfect(position, DustID.Blood,
                Main.rand.NextVector2Circular(2.5f, 2.5f), 20, default, Main.rand.NextFloat(.8f, 1.25f));
            dust.noGravity = true;
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D bag = ModContent.Request<Texture2D>(Texture).Value;
        Main.EntitySpriteDraw(bag, Projectile.Center - Main.screenPosition, null, lightColor,
            Projectile.rotation * .15f, bag.Size() * .5f, 1f, SpriteEffects.None);
        return false;
    }
}
