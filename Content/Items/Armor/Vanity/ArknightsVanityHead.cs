using System.ComponentModel;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity
{
	public abstract class ArknightsVanityHead : ModItem
	{
		public virtual int Rarity => ItemRarityID.LightPurple;
		public sealed override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 1;
			if (Main.netMode == NetmodeID.Server)
				return;
			ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = false;
			SafeSetStaticDefaults();
		}

		public sealed override void SetDefaults()
		{
			Item.width = 28;
			Item.height = 28;
			Item.rare = Rarity;
			Item.vanity = true;
			SafeSetDefaults();
		}
		public virtual void SafeSetDefaults() { }
		public virtual void SafeSetStaticDefaults() { }
	}
}
