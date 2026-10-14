using System.IO;
using ArknightsMod.Content.Items.Weapons.Caster.Mostima;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Mostima;

public sealed class MostimaFocus : ModProjectile
{
    private Vector2 target;
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Caster/Mostima/MostimaStaff";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.netImportant = true; Projectile.timeLeft = 2;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void OnSpawn(IEntitySource source) => target = Projectile.Center + Projectile.velocity * 160f;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead) { Projectile.Kill(); return; }
        Projectile clock = null;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == Projectile.owner && p.type == ModContent.ProjectileType<MostimaCrossBurst>()
                && (Projectile.ai[2] == 0f || p.identity == (int)Projectile.ai[2] - 1)) { clock = p; break; }
        if (Projectile.ai[1] == 0f && Projectile.owner == Main.myPlayer)
        {
            target = MostimaStaff.Target(owner);
            if (clock == null)
            {
                int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), target, Vector2.Zero,
                    ModContent.ProjectileType<MostimaCrossBurst>(), owner.GetWeaponDamage(owner.HeldItem),
                    Projectile.knockBack, owner.whoAmI);
                if (!Main.projectile.IndexInRange(index)) { Projectile.Kill(); return; }
                clock = Main.projectile[index];
                clock.CritChance = owner.GetWeaponCrit(owner.HeldItem);
            }
            Projectile.ai[1] = 1f; Projectile.ai[2] = clock.identity + 1;
            Projectile.netUpdate = true;
        }
        if (clock == null && Projectile.ai[1] != 0 && Projectile.owner == Main.myPlayer)
        { owner.itemTime = owner.itemAnimation = 0; Projectile.Kill(); return; }
        if (clock != null) target = clock.Center;
        Projectile.Center = owner.MountedCenter;
        Projectile.rotation = (target - owner.MountedCenter).SafeNormalize(new Vector2(owner.direction, 0)).ToRotation();
        Projectile.timeLeft = 2;
        if (owner.HeldItem.ModItem is MostimaStaff)
        {
            owner.ChangeDir(System.MathF.Cos(Projectile.rotation) < 0f ? -1 : 1);
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = owner.itemAnimation = 2;
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
        }
    }
    public override void SendExtraAI(BinaryWriter writer) { writer.Write(target.X); writer.Write(target.Y); }
    public override void ReceiveExtraAI(BinaryReader reader) { target = new(reader.ReadSingle(), reader.ReadSingle()); }
    internal static Vector2 StaffTip(Vector2 grip, float angle, float scale = 1f)
        => grip + new Vector2(23f, -23f).RotatedBy(angle + MathHelper.PiOver4) * scale;
    internal static void DrawStaff(Vector2 center, float angle, Color color, float scale = 1)
    {
        Texture2D texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Weapons/Caster/Mostima/MostimaStaff").Value;
        Main.spriteBatch.Draw(texture, center - Main.screenPosition, null, color, angle + MathHelper.PiOver4,
            new Vector2(8, texture.Height - 8), scale, SpriteEffects.None, 0);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Player owner = Main.player[Projectile.owner];
        if (owner.HeldItem.ModItem is MostimaStaff) DrawStaff(owner.MountedCenter, Projectile.rotation, lightColor);
        return false;
    }
}