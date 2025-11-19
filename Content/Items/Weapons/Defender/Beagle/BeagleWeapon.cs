using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static ArknightsMod.Content.Items.Accessories.Rogue.ScoutsScope;
using static System.Net.Mime.MediaTypeNames;

namespace ArknightsMod.Content.Items.Weapons.Defender.Beagle
{
	public class BeagleWeapon : UpgradeWeaponBase
	{
		public override void AddRecipes() {
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient<Placeable.OrirockCube>(4);
			recipe.AddTile(ModContent.TileType<FactoryTile>());
			recipe.Register();
		}
		private static SoundStyle SkillActive3;
		private static SoundStyle NoSound;
		public override void Load() {
			SkillActive3 = new SoundStyle("ArknightsMod/Sounds/SkillActive3") {
				Volume = 0.4f,
				MaxInstances = 4,
			};
			NoSound = new SoundStyle("ArknightsMod/Sounds/NoSound") {
				Volume = 0f,
				MaxInstances = 4,
			};
		}
		public override void SetDefaults() {
			Item.damage = 25; // �����˺�
			Item.knockBack = 7;
			Item.crit = 2; // ������
			Item.DamageType = DamageClass.Melee; // �˺�����
			Item.width = 48; // ��Ʒ����
			Item.height = 60; // ��Ʒ�߶�
			Item.useTime = 25; // ʹ��ʱ��
			Item.useAnimation = 25; // ʹ�ö���ʱ��
			Item.autoReuse = true; // �Զ�ʹ��
			Item.noUseGraphic = true;
			Item.noMelee = true;
			Item.useStyle = ItemUseStyleID.HiddenAnimation;
		}
		public override bool AltFunctionUse(Player player) => true;
		public override bool CanUseItem(Player player) {
			var modPlayer = Main.LocalPlayer.GetModPlayer<WeaponPlayer>();
			if (Main.myPlayer == player.whoAmI) {
				if (player.altFunctionUse == 2) {
					if (!modPlayer.SummonMode) {
						// S1
						if (modPlayer.Skill == 0 && modPlayer.StockCount > 0 && !modPlayer.SkillActive) {
							modPlayer.SkillActive = true;
							modPlayer.SkillTimer = 0;

							modPlayer.DelStockCount();
							player.GetModPlayer<MGLDEFplayer>().hasMGLDEFplayer = true;
							Item.UseSound = SkillActive3;
							SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
						}
						else
							player.GetModPlayer<MGLDEFplayer>().hasMGLDEFplayer = false;
						return false;
					}
				}
				else {
					if (!modPlayer.SummonMode) {
						Item.UseSound = NoSound;
						SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
						// S1
						if (modPlayer.Skill == 0 && modPlayer.SkillActive) {

						}
						else if (modPlayer.Skill == 0 && !modPlayer.SkillActive) {

						}
					}
				}
			}
			return base.CanUseItem(player);
		}
		public class MGLDEFplayer : ModPlayer
		{
			public bool hasMGLDEFplayer = false;
			public override void ResetEffects() {
				//����2��������Ч��
				if (hasMGLDEFplayer == true) {
					Player.statDefense *= 1.5f;
				}
				if (Main.myPlayer != Player.whoAmI)
					return;  // ֻ�����������
				bool isHoldingTargetWeapon = Player.HeldItem.type == ModContent.ItemType<BeagleWeapon>();
				if (!isHoldingTargetWeapon) {
					Player.GetModPlayer<MGLDEFplayer>().hasMGLDEFplayer = false;
				}

			}
		}
		public override void HoldItem(Player player) {
			var modPlayer = Main.LocalPlayer.GetModPlayer<WeaponPlayer>();
			if (Main.myPlayer == player.whoAmI) {
				if (modPlayer.Skill == 0 && !modPlayer.SkillActive) {
					player.GetModPlayer<MGLDEFplayer>().hasMGLDEFplayer = false;
				}
				if (modPlayer.Skill == 0 && modPlayer.SkillActive && Item.type == ModContent.ItemType<BeagleWeapon>()) {
					player.GetModPlayer<MGLDEFplayer>().hasMGLDEFplayer = true;
				}
			}
		}
	}
}