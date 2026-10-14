using ArknightsMod.Content.Items.Weapons.Guard.Surtr;
using ArknightsMod.Content.Projectiles.Guard.ArtsFighter;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Laevatain;

// Visual overlay only: existing sword collision, skill damage and hit feedback stay authoritative.
public sealed class LaevatainArtsSlash : ArtsFighterSlash
{
    protected override int WeaponType => ModContent.ItemType<SurtrLaevatain>();
    protected override Color SlashColor => new(255, 104, 30);
    public override bool? CanDamage() => false;
}
