using ArknightsMod.Content.Items.Material;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace ArknightsMod.Content.NPCs.Friendly
{
	/// <summary>
	/// 当前打开的商店标题。由 <see cref="Closure.ModifyActiveShop"/> 在铺货架时写入，
	/// 供标题自绘读取。放在独立类型里是为了服务端也能安全引用（自绘那部分只在客户端加载）。
	/// </summary>
	public static class ClosureShopTitleState
	{
		/// <summary>可露希尔当前打开的商店名（如「蓝色材料商店」）；关店或换人对话时为 null。</summary>
		public static string Current;
	}

	// 原版商店面板的标题是写死的 Lang.inter[28]（"商店"），而且 tModLoader 也没有给模组商店单独命名的接口
	// —— NPCShop 的 name 只是 "NPC全名/商店名" 这样的标识符（见 NPCShopDatabase.GetShopName）。
	// 所以这里在 vanilla 那行标题右侧补画当前商店的名字，让「白色材料商店」这类名字玩家真的看得见。
	[Autoload(Side = ModSide.Client)]
	public sealed class ClosureShopTitleSystem : ModSystem
	{
		public override void ClearWorld() => ClosureShopTitleState.Current = null;

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
			if (mouseTextIndex == -1)
				return;

			// 插在鼠标文本之前：既保证画在物品栏/商店面板（Vanilla: Inventory）之上，又不会盖住提示框。
			layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
				"ArknightsMod: Closure Shop Title",
				delegate {
					DrawShopTitle();
					return true;
				},
				InterfaceScaleType.UI
			));
		}

		private static void DrawShopTitle() {
			if (Main.dedServ || Main.gameMenu || string.IsNullOrEmpty(ClosureShopTitleState.Current))
				return;
			if (Main.npcShop <= 0 || !Main.playerInventory)
				return;

			NPC talkNPC = Main.LocalPlayer.TalkNPC;
			if (talkNPC == null || talkNPC.type != ModContent.NPCType<Closure>())
				return;

			// 贴着原版标题画：vanilla 在 Main.DrawInventory 里用 (504, Main.instance.invBottom) 画 Lang.inter[28]，
			// 这里量一下那行的宽度再往右挪一点，字号/阴影都跟原版一致。
			var font = FontAssets.MouseText.Value;
			float baseWidth = font.MeasureString(Language.GetTextValue("LegacyInterface.28")).X;
			Color color = Color.White * (Main.mouseTextColor / 255f);
			Utils.DrawBorderStringFourWay(Main.spriteBatch, font, ClosureShopTitleState.Current,
				504f + baseWidth + 12f, Main.instance.invBottom, color, Color.Black, Vector2.Zero);
		}
	}
}
