using ArknightsMod.Content.Items.Armor.Reforge;
using ArknightsMod.Common;
using ArknightsMod.Content.Items.Material;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Rosmontis
{
	[AutoloadEquip(EquipType.Body)]
	public class RosmontisBody : ReforgeVanityBody
	{
		public override int Rarity => 6;

		public override ReforgeSetProfile SetProfile => new() {
			Defense = 21,
			LifeBonus = 97,
			LocalizationPrefix = "Mods.ArknightsMod.ArmorSets.Rosmontis",
			Materials = recipe => recipe
				.AddIngredient<Orundum>(60)
				.AddIngredient<BipolarNanoflake>(6)
				.AddIngredient<OptimizedDevice>(4),
		};

		internal class RosmontisBackLayer : PlayerDrawLayer
		{
			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Wings);
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				Player player = drawInfo.drawPlayer;
				// 时装和套装（RosmontisBodySet）各自注册了一个装备槽位（贴图相同，
				// 槽位 ID 不同），两个槽位都要认，不然穿套装就看不到这层背部特效了。
				int vanitySlot = EquipLoader.GetEquipSlot(Mod, nameof(RosmontisBody), EquipType.Body);
				int setSlot = EquipLoader.GetEquipSlot(Mod, nameof(RosmontisBody) + "Set", EquipType.Body);
				return (player.body == vanitySlot || player.body == setSlot) && !player.dead;
			}

			protected override void Draw(ref PlayerDrawSet drawInfo) {
				Texture2D texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Sniper/Rosmontis/RosmontisBody_Back").Value;

				var offset = new Vector2(0, -3) + new Vector2(0, 0);
				PlayerLayerHelper.AddPlayerDrawLayer(ref drawInfo, texture, 1, offset);
			}
		}
	}
}
