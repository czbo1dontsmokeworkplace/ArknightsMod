using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.NPCs.Enemy.W;
using ArknightsMod.Content.Projectiles.Bosses.W;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.BossSummon
{
	/// <summary>
	/// W 的召唤物（占位贴图沿用 D12 骰子）：地表任意时间可用。<br/>
	/// 使用时向光标方向掷出一枚召唤骰（WSummonDie），骰子停稳、锁定 12、炸烟后 W 从烟里登场。
	/// </summary>
	public class WSummon : ModItem
	{
		public override void SetDefaults() {
			Item.width = 22;
			Item.height = 22;
			Item.maxStack = 1;
			Item.value = Item.buyPrice(silver: 50);
			Item.rare = ItemRarityID.Blue;
			Item.useAnimation = 30;
			Item.useTime = 30;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.noMelee = true;
			Item.consumable = false;
		}

		public override bool CanUseItem(Player player) {
			// 场上无 W、没有正在滚的召唤骰，且玩家处于地表
			return !NPC.AnyNPCs(ModContent.NPCType<WBoss>())
				&& player.ownedProjectileCounts[ModContent.ProjectileType<WSummonDie>()] == 0
				&& player.Center.Y < Main.worldSurface * 16.0;
		}

		public override bool? UseItem(Player player) {
			// 骰子由使用者本地掷出（玩家所有的弹幕会自行同步到服务端）
			if (player.whoAmI == Main.myPlayer) {
				Vector2 dir = (Main.MouseWorld - player.Center).SafeNormalize(Vector2.UnitX * player.direction);
				Vector2 vel = dir * 8f + new Vector2(0f, -3f);
				Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, vel,
					ModContent.ProjectileType<WSummonDie>(), 0, 0f, player.whoAmI);
				SoundEngine.PlaySound(SoundID.Item1, player.position);
			}
			return true;
		}

		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient<OrirockCube>(2)
				.AddIngredient(ItemID.IronBar, 8)
				.Register();
		}
	}
}
