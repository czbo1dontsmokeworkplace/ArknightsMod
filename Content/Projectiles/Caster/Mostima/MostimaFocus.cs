using System;
using System.IO;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Items.Weapons.Caster.Mostima;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Mostima;

public sealed class MostimaFocus : ModProjectile
{
    private Vector2 target;
    private bool sawChannel;
    private int openingGrace;
    private const int ChargeTicks = 90;
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Caster/Mostima/MostimaStaff";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.timeLeft = 2;
    }
    public override bool ShouldUpdatePosition() => false;
    public override void OnSpawn(IEntitySource source) => target = Projectile.Center + Projectile.velocity * 160;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.CCed || owner.noItems || owner.HeldItem.ModItem is not MostimaStaff)
        { Projectile.Kill(); return; }
        Projectile.Center = owner.MountedCenter;
        Projectile.localAI[1]++;
        if (Projectile.owner == Main.myPlayer)
        {
            target = MostimaStaff.Target(owner);
            if (owner.channel) sawChannel = true;
            else if (sawChannel || ++openingGrace > 5)
            {
                if (sawChannel && Projectile.ai[0] >= ChargeTicks)
                {
                    int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), target, Vector2.Zero,
                        ModContent.ProjectileType<MostimaCrossBurst>(), owner.GetWeaponDamage(owner.HeldItem),
                        Projectile.knockBack, owner.whoAmI);
                    if (Main.projectile.IndexInRange(index)) Main.projectile[index].CritChance = owner.GetWeaponCrit(owner.HeldItem);
                }
                owner.itemTime = owner.itemAnimation = 0;
                Projectile.Kill(); return;
            }
            if ((int)Projectile.localAI[1] % 5 == 0) Projectile.netUpdate = true;
        }
        Vector2 direction = (target - owner.MountedCenter).SafeNormalize(new Vector2(owner.direction, 0));
        Projectile.rotation = direction.ToRotation();
        owner.ChangeDir(direction.X < 0 ? -1 : 1);
        owner.heldProj = Projectile.whoAmI;
        owner.itemTime = owner.itemAnimation = 2;
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
        Projectile.timeLeft = 2;
        Projectile.ai[0] = Math.Min(ChargeTicks, Projectile.ai[0] + 1);
        if (Main.dedServ) return;
        Vector2 muzzle = owner.MountedCenter + direction * 43;
        if (Projectile.ai[0] == ChargeTicks && Projectile.localAI[0] == 0)
        {
            Projectile.localAI[0] = 1;
            ChargeCastFeedback.Ready(muzzle, MostimaVisuals.Blue, DustID.BlueCrystalShard, -.15f);
        }
        if ((int)Projectile.localAI[1] % 6 == 0)
        {
            float phase = Main.GlobalTimeWrappedHourly * 3;
            Vector2 offset = phase.ToRotationVector2() * 45;
            new MostimaChronoParticle(muzzle + offset, -offset * .08f, MostimaVisuals.Blue, 24).Spawn();
        }
        Lighting.AddLight(muzzle, .08f, .18f, .3f);
    }
    public override void SendExtraAI(BinaryWriter writer) { writer.Write(target.X); writer.Write(target.Y); }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Vector2 received = new(reader.ReadSingle(), reader.ReadSingle());
        if (float.IsFinite(received.X) && float.IsFinite(received.Y)) target = received;
    }
    internal static void DrawStaff(Vector2 center, float angle, Color color, float scale = 1)
    {
        Texture2D texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Weapons/Caster/Mostima/MostimaStaff").Value;
        Main.spriteBatch.Draw(texture, center - Main.screenPosition, null, color, angle + MathHelper.PiOver4,
            new Vector2(8, texture.Height - 8), scale, SpriteEffects.None, 0);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Player owner = Main.player[Projectile.owner];
        float charge = Projectile.ai[0] / ChargeTicks;
        DrawStaff(owner.MountedCenter, Projectile.rotation, lightColor);
        MostimaVisuals.Charge(owner.MountedCenter + Projectile.rotation.ToRotationVector2() * 43,
            target, charge, Main.GlobalTimeWrappedHourly);
        ChargeCastFeedback.DrawBar(owner, charge, MostimaVisuals.Blue);
        return false;
    }
}
