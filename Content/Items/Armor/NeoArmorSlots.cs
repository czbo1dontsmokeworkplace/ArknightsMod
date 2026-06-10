using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Armor
{
	public abstract class NeoArmorHead : NeoArmorItem
	{
		protected sealed override void SetSlotStaticDefaults() {
			ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = false;
		}
	}
	public abstract class NeoArmorBody : NeoArmorItem
	{
		protected sealed override void SetSlotStaticDefaults() {
			ArmorIDs.Body.Sets.HidesTopSkin[Item.bodySlot] = true;
			ArmorIDs.Body.Sets.HidesArms[Item.bodySlot] = true;
		}
	}
	public abstract class NeoArmorLegs : NeoArmorItem
	{
		protected sealed override void SetSlotStaticDefaults() {
			ArmorIDs.Legs.Sets.HidesBottomSkin[Item.legSlot] = true;
		}
	}
}
