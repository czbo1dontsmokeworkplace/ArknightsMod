using System.Collections.Generic;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Guard.Crusher;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Crusher;

public sealed class CrusherAura : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_0";
    private int age;
    public override void SetDefaults()
    {
        Projectile.width = 100; Projectile.height = 24; Projectile.tileCollide = false;
        Projectile.ignoreWater = true; Projectile.timeLeft = 2; Projectile.penetrate = -1; Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void AI()
    {
        Player p = Main.player[Projectile.owner];
        if (!p.active || p.dead || p.HeldItem.ModItem is not CrusherWeapon weapon || (int)weapon.Kind != (int)Projectile.ai[0] || p.selectedItem != (int)Projectile.ai[1]) { Projectile.Kill(); return; }
        Projectile.Center = p.gravDir > 0 ? p.Bottom : p.Top;
        Projectile.timeLeft = 2; age++;
        if (p.whoAmI == Main.myPlayer && age % 60 == 0) Projectile.netUpdate = true;
        if (!Main.dedServ && age % 7 == 0) CrusherVisuals.AuraMote(Projectile.Center, weapon.Kind, age, p.gravDir);
    }
    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => behindProjectiles.Add(index);
    public override bool PreDraw(ref Color lightColor)
    {
        Player p = Main.player[Projectile.owner]; var state = p.GetModPlayer<CrusherPlayer>();
        CrusherMaterial.DrawSeal(Projectile.Center, (CrusherKind)(int)Projectile.ai[0], MathHelper.Clamp(age / 25f, 0, 1), state.Charge, p.gravDir);
        return false;
    }
    public override void SendExtraAI(BinaryWriter writer) => Main.player[Projectile.owner].GetModPlayer<CrusherPlayer>().WriteState(writer);
    public override void ReceiveExtraAI(BinaryReader reader) => Main.player[Projectile.owner].GetModPlayer<CrusherPlayer>().ReadState(reader);
}
