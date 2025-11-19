using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Players;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Color = Microsoft.Xna.Framework.Color;


namespace ArknightsMod.Content.Items.Weapons.Defender.NoirCorne
{
    // TODO 没有技能的目前还有点问题，先用ModItem
    public class NoirShield : UpgradeWeaponBase
    {

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.IronBar, 5);
            recipe.AddIngredient(ItemID.LeadBar, 5);
            recipe.AddTile(ModContent.TileType<FactoryTile>());
            recipe.Register();
        }
        private static SoundStyle NoSound;
        public override void Load()
        {
            NoSound = new SoundStyle("ArknightsMod/Sounds/NoSound")
            {
                Volume = 0f,
                MaxInstances = 4,
            };
        }
        public override void SetDefaults()
        {
            Item.damage = 18;
            Item.knockBack = 12;
            Item.crit = 2;
            Item.DamageType = DamageClass.Melee;
            Item.width = 78;
            Item.height = 102;
            Item.useTime = 20;
            Item.useAnimation = 16;
            Item.autoReuse = true;
            Item.useStyle = ItemUseStyleID.Thrust;
        }
        public override void HoldItem(Player player)
        {
            if (Main.myPlayer == player.whoAmI)
            {
                player.GetModPlayer<WeaponPlayer>().defenseBonus = 5;
            }
        }
    }
}