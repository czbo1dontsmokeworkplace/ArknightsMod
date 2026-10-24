using ArknightsMod.Content.Projectiles.Defender;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Weapons.Defender;

// New route nodes reuse existing body/shield art and the same public holdout.
public abstract class RoutedDefenderWeapon : DefenderWeapon
{
    private DefenderRouteNode Node => DefenderRouteCatalog.Find(Name);
    public override string Texture => Node.Branch == "SentinelProtector"
        ? "Terraria/Images/Item_" + ItemID.Handgun
        : Node.Branch == "Duelist" || Node.Branch == "Juggernaut"
        ? "ArknightsMod/Content/Items/Weapons/Defender/Vulcan/Vulcan_Weapon"
        : "ArknightsMod/Content/Items/Weapons/Defender/Durnar/DN_Weapon";
    protected override int[] EliteDamage => [Node.Damage, (int)(Node.Damage * 1.25f), (int)(Node.Damage * 1.55f)];
    protected override int Interval => Node.Interval;
    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.DamageType = Node.Branch == "SentinelProtector" ? Terraria.ModLoader.DamageClass.Magic : Terraria.ModLoader.DamageClass.Melee;
        Item.rare = Node.Tier >= 4 ? ItemRarityID.Lime : Node.Tier == 3 ? ItemRarityID.Pink : ItemRarityID.Orange;
        Item.value = Item.sellPrice(gold: Node.Tier * 2);
    }
}
public sealed class BisonShield : RoutedDefenderWeapon { }
public sealed class HeavyrainBlade : RoutedDefenderWeapon { }
public sealed class HoshigumaShield : RoutedDefenderWeapon { }
public sealed class LutonHammer : RoutedDefenderWeapon { }
public sealed class BlitzPistol : RoutedDefenderWeapon { }
public sealed class JessicaLiberatedGun : RoutedDefenderWeapon { }
public sealed class MechanistGun : RoutedDefenderWeapon { }
public sealed class FaithMixer : RoutedDefenderWeapon { }
public sealed class AsbestosBlade : RoutedDefenderWeapon { }
public sealed class ShalemBlade : RoutedDefenderWeapon { }
public sealed class CzernyBlade : RoutedDefenderWeapon { }
public sealed class HoshigumaBlazingBlade : RoutedDefenderWeapon { }
public sealed class AshlockLauncher : FirewhistleLauncher
{
    protected override int[] EliteDamage => [64, 84, 108];
    protected override int Interval => 54;
}
public sealed class BubbleMaul : RoutedDefenderWeapon { }
public sealed class AuroraAxe : RoutedDefenderWeapon { }
public sealed class CementMaul : RoutedDefenderWeapon { }
public sealed class MatterhornBlade : RoutedDefenderWeapon { }
public sealed class PhilaeBlade : RoutedDefenderWeapon { }
public sealed class BasslineBlade : RoutedDefenderWeapon { }
public sealed class UnderflowBlade : RoutedDefenderWeapon { }
public sealed class YuBlade : RoutedDefenderWeapon { }
public sealed class SanbyBlade : RoutedDefenderWeapon { }
