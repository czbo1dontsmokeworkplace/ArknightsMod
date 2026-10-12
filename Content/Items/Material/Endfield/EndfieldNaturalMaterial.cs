using ArknightsMod.Systems;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Material.Endfield;

// 与原有在坎诺特处换源石锭的 RareCollectibleItem 分开，接入可露希尔的合成玉回收。
public abstract class EndfieldNaturalMaterial : ArknightsMaterial
{
    public abstract int ResourceIndex { get; }
    public override int Rarity => 3;
    public override int OrundumRecycleValue => EndfieldNaturalCatalog.Resources[ResourceIndex].OrundumValue;
    public override string LocalizationCategory => "Items.EndfieldNatural";
    public override string Texture => $"ArknightsMod/Content/Items/Material/Endfield/{Name}";

    public override void SafeSetDefaults()
    {
        Item.DefaultToPlaceableTile(EndfieldNaturalCatalog.TileType(ResourceIndex));
        Item.maxStack = Item.CommonMaxStack;
        Item.rare = ItemRarityID.LightPurple;
        Item.value = 0; // 普通 NPC 不产生钱币回购循环；可露希尔由回收钩子发合成玉。
    }

    public override void AddRecipes() => Recipe.Create(ModContent.ItemType<Orundum>(), OrundumRecycleValue)
        .AddIngredient(Type)
        .AddTile<FactoryTile>()
        .Register();
}
