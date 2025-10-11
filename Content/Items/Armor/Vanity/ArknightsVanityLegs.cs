using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity
{
	public abstract class ArknightsVanityLegs : ModItem
	{
		/// <summary>
		/// 干员的星数，请输入1~6
		/// </summary>
		public virtual int Rarity => 6;
		public virtual int Value => 15000;
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
			Item.rare = ArknightsVanityHead.GetRarity(Rarity);
			Item.value = Value;
			Item.vanity = true;
			SafeSetDefaults();
		}
		public virtual void SafeSetDefaults() { }
		public virtual void SafeSetStaticDefaults() { }
	}
}
