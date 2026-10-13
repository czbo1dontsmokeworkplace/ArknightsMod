using System;
using System.IO;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Items.Weapons.Caster.Lava;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Lava;

// The book remains a separate held visual; this focus only owns charging and release.
public sealed class LavaFocus : ModProjectile
{
    private const int ChargeTicks = 42;
    private float aimAngle;
    private bool sawChannel;
    private int openingGrace;
    private bool fired;
    private Player Owner => Main.player[Projectile.owner];
    private Vector2 Direction => aimAngle.ToRotationVector2();
    private Vector2 Muzzle => Owner.MountedCenter + Direction * 36f;
    private float Charge => MathHelper.Clamp(Projectile.ai[0] / ChargeTicks, 0f, 1f);
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 2;
        Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void OnSpawn(IEntitySource source)
        => aimAngle = Projectile.velocity.SafeNormalize(Vector2.UnitX).ToRotation();

    public override void AI()
    {
        Player owner = Owner;
        if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.ModItem is not Lava_Dagger)
        { Projectile.Kill(); return; }
        Projectile.Center = owner.MountedCenter;
        if (Projectile.owner == Main.myPlayer)
        {
            aimAngle = (Main.MouseWorld - owner.MountedCenter)
                .SafeNormalize(new Vector2(owner.direction, 0f)).ToRotation();
            if (owner.channel) sawChannel = true;
            else if (sawChannel || ++openingGrace > 5)
            {
                if (sawChannel) Fire();
                owner.itemTime = owner.itemAnimation = 0;
                Projectile.Kill();
                return;
            }
            if ((int)Projectile.ai[0] % 6 == 0) Projectile.netUpdate = true;
        }
        owner.heldProj = Projectile.whoAmI;
        owner.itemTime = owner.itemAnimation = 2;
        owner.ChangeDir(Direction.X < 0 ? -1 : 1);
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, aimAngle - MathHelper.PiOver2);
        Projectile.timeLeft = 2;
        Projectile.ai[0] = Math.Min(ChargeTicks, Projectile.ai[0] + 1f);
        if (Main.dedServ) return;
        Lighting.AddLight(Muzzle, new Vector3(.45f, .14f, .04f) * Charge);
        if (Charge >= 1f && Projectile.localAI[0] == 0f)
        {
            Projectile.localAI[0] = 1f;
            ChargeCastFeedback.Ready(Muzzle, new Color(255, 143, 65), DustID.Torch, .30f);
        }
        if (Projectile.ai[0] > 8 && Main.rand.NextBool(4))
            OperatorFlightVfx.Spark(Muzzle + Main.rand.NextVector2Circular(16f, 16f),
                -Direction * Main.rand.NextFloat(.4f, 1.3f), new Color(255, 158, 72), 12f, 12, 1.2f);
    }

    private void Fire()
    {
        if (fired || Projectile.owner != Main.myPlayer) return;
        fired = true;
        if (Projectile.ai[0] < ChargeTicks)
        { SoundEngine.PlaySound(SoundID.Item9 with { Volume = .20f }, Muzzle); return; }
        int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Muzzle, Direction * 12f,
            ModContent.ProjectileType<LavaBolt>(), Math.Max(1, Owner.GetWeaponDamage(Owner.HeldItem)),
            Projectile.knockBack, Projectile.owner);
        if (Main.projectile.IndexInRange(index))
            Main.projectile[index].CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
        SoundEngine.PlaySound(SoundID.Item73 with { Volume = .43f }, Muzzle);
        OperatorFlightVfx.Ring(Muzzle, new Color(255, 143, 65), 45f, 15);
    }

    public override void SendExtraAI(BinaryWriter writer) => writer.Write(aimAngle);
    public override void ReceiveExtraAI(BinaryReader reader) => aimAngle = reader.ReadSingle();
    public override bool PreDraw(ref Color lightColor)
    {
        SpreadChargeSigil.Draw(Muzzle, 59f, aimAngle, Main.GlobalTimeWrappedHourly, Charge,
            new Color(255, 143, 65));
        ChargeCastFeedback.DrawBar(Owner, Charge, new Color(255, 143, 65));
        return false;
    }
}
