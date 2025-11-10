using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Accessories.Rogue
{
    public class ScoutsScope : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.value = Item.sellPrice(15, 0, 0, 0); // 15金币价值
            Item.rare = ItemRarityID.Master; 
            Item.accessory = true;
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
		{
			player.GetModPlayer<DistanceDamagePlayer>().hasDistanceAccessory = true;
		}
		public class DistanceDamagePlayer : ModPlayer
		{
			public bool hasDistanceAccessory = false;

			public override void ResetEffects() {
				hasDistanceAccessory = false;
			}
			public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
				if (hasDistanceAccessory) { 
					float distance = Vector2.Distance(Player.Center, target.Center);
					float multiplier = 1f + (distance / 40f) * 0.05f;
					multiplier = MathHelper.Clamp(multiplier, 1f, 2f);
					// 应用倍率
					modifiers.FinalDamage *= multiplier;
				}
			}
		}
	}
}