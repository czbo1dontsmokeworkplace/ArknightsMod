using ArknightsMod.Content.Items.Weapons.Guard.Mousse;
using ArknightsMod.Content.Buffs;
using ArknightsMod.Content.Projectiles.Guard.ArtsFighter;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Mousse;

public sealed class MousseLightSlash : ArtsFighterSlash
{
    protected override int WeaponType => ModContent.ItemType<MousseGlove>();
    protected override Color SlashColor => MousseVisuals.Pale;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Projectile.ai[2] == 1f) target.AddBuff(ModContent.BuffType<MousseScratch>(), 300);
        base.OnHitNPC(target, hit, damageDone);
    }
}
