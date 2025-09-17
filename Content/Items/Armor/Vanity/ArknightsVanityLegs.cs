using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity
{
	public abstract class ArknightsVanityLegs : ModItem
	{
		public virtual int Rarity => ItemRarityID.LightPurple;
		public sealed override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 1;
			if (Main.netMode == NetmodeID.Server)
				return;
			ArmorIDs.Legs.Sets.HidesBottomSkin[Item.legSlot] = true;
			SafeSetStaticDefaults();
		}

		public sealed override void SetDefaults() {
			Item.width = 18;
			Item.height = 8;
			Item.rare = Rarity;
			Item.vanity = true;
			SafeSetDefaults();
		}
		public virtual void SafeSetDefaults() { }
		public virtual void SafeSetStaticDefaults() { }
	}
}
