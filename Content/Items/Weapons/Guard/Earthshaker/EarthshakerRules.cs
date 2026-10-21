using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Weapons.Guard.Earthshaker;

public enum EarthshakerKind { Odda, Zima, Pepe }

public static class EarthshakerRules
{
    public static int HammerPower(EarthshakerKind kind) => kind switch { EarthshakerKind.Odda => 80, EarthshakerKind.Zima => 95, _ => 105 };
    public static int ArtItem(EarthshakerKind kind) => kind switch { EarthshakerKind.Odda => ItemID.TheBreaker, EarthshakerKind.Zima => ItemID.Pwnhammer, _ => ItemID.PaladinsHammer };
    public static string Art(EarthshakerKind kind) => "Terraria/Images/Item_" + ArtItem(kind);
    public static Color Tint(EarthshakerKind kind) => kind switch { EarthshakerKind.Odda => new(242, 146, 62), EarthshakerKind.Zima => new(237, 76, 66), _ => new(246, 190, 65) };
    public static Color Accent(EarthshakerKind kind) => kind switch { EarthshakerKind.Odda => new(255, 228, 166), EarthshakerKind.Zima => new(255, 200, 147), _ => new(81, 224, 216) };
}
