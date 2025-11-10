
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Items.Weapons
{ 
	public class SurtrLaevatain : ModItem
	{
		public override void SetDefaults()
		{
			Item.damage = 160;
			Item.DamageType = DamageClass.Melee;
			Item.width = 64;
			Item.height = 70;
			Item.useTime = 15;
			Item.useAnimation = 15;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.knockBack = 10;
			Item.value = Item.sellPrice(silver: 3000);
			Item.rare = ItemRarityID.Red;
			Item.UseSound = SoundID.Item1;
			Item.autoReuse = true;
		}
	}
}
