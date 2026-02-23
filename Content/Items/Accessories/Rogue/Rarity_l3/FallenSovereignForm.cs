using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using System.Collections.Generic;

namespace ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l3
{
	public class FallenSovereignForm : ModItem
	{
		public override void SetDefaults() {
			Item.width = 32;
			Item.height = 32;
			Item.accessory = true;
			Item.rare = ItemRarityID.Purple;
			Item.value = Item.sellPrice(12, 0, 0, 0);
		}

		public override void UpdateAccessory(Player player, bool hideVisual) {
			player.GetModPlayer<FallenSovereignShieldPlayer>().hasFallenSovereignShield = true;
			player.statDefense += 15;
		}
	}

	public class FallenSovereignShieldPlayer : ModPlayer
	{

		public const float HP_REDUCTION_RATIO = 0.3f;
		public const float SHIELD_MAX_HP_RATIO = 0.7f;
		public const float SHIELD_REGEN_RATIO = 0.5f;
		public const int SHIELD_BREAK_COOLDOWN = 300;


		public bool hasFallenSovereignShield = false;
		public float currentShield = 0f;
		public float maxShield = 0f;
		public int shieldBreakTimer = 0;


		private float shieldRegenCounter = 0f;
		private int originalMaxLife = 0;
		private bool wasDeadLastFrame = false;
		private bool bossShieldFilled = false;

		public override void ResetEffects() {
			hasFallenSovereignShield = false;
		}

		public override void UpdateDead() {

			wasDeadLastFrame = true;
		}

		public override void PostUpdateMiscEffects() {

			HandleRevival();
		}

		public override void PostUpdateEquips() {
			if (!hasFallenSovereignShield) {
				ResetShield();
				return;
			}


			UpdateOriginalMaxLife();


			maxShield = originalMaxLife * SHIELD_MAX_HP_RATIO;


			if (currentShield > maxShield)
				currentShield = maxShield;


			CheckBossShield();


			if (shieldBreakTimer > 0)
				shieldBreakTimer--;


			if (currentShield >= 0 && shieldBreakTimer <= 0 && currentShield < maxShield) {
				RegenShield();
			}


			ApplyHPReduction();
		}

		private void UpdateOriginalMaxLife() {
			if (originalMaxLife == 0)
				originalMaxLife = Player.statLifeMax2;

			if (Player.statLifeMax2 > originalMaxLife)
				originalMaxLife = Player.statLifeMax2;
		}

		private void HandleRevival() {

			if (wasDeadLastFrame && !Player.dead && Player.active) {

				if (hasFallenSovereignShield) {
					currentShield = maxShield; 
					shieldBreakTimer = 0;
					shieldRegenCounter = 0f;

				}
			}


			wasDeadLastFrame = Player.dead;
		}

		private void CheckBossShield() {
			bool bossAlive = AnyBossAlive();

			if (bossAlive && !bossShieldFilled) {
				currentShield = maxShield;
				bossShieldFilled = true;
			}
			else if (!bossAlive) {
				bossShieldFilled = false; 
			}
		}

		private void RegenShield() {
			if (Player.lifeRegen > 0 || Player.lifeRegen == 0) {

				float lifeRegenPerFrame = System.Math.Abs(Player.lifeRegen);
				float baseRegen = 1f; 
				float shieldRegenPerSecond = baseRegen + (lifeRegenPerFrame * SHIELD_REGEN_RATIO);


				shieldRegenCounter += shieldRegenPerSecond / 90f;


				while (shieldRegenCounter >= 1f && currentShield < maxShield) {
					currentShield++;
					shieldRegenCounter -= 1f;
				}


				if (currentShield > maxShield)
					currentShield = maxShield;
			}
		}

		private void ApplyHPReduction() {
			int reducedMaxLife = (int)(originalMaxLife * HP_REDUCTION_RATIO);
			Player.statLifeMax2 = reducedMaxLife;

			if (Player.statLife > Player.statLifeMax2)
				Player.statLife = Player.statLifeMax2;
		}


		private void ProcessDamage(int damage, ref Player.HurtModifiers modifiers) {
			if (!hasFallenSovereignShield || currentShield <= 0)
				return;

			float shieldBeforeHit = currentShield;

			if (damage <= currentShield) {
				currentShield -= damage;
				modifiers.SetMaxDamage(0);
				CombatText.NewText(Player.getRect(), new Color(113, 133, 162), $"-{damage}", true);


				if (Player.statLife < Player.statLifeMax2) {
					Player.statLife += 1;
					if (Player.statLife > Player.statLifeMax2)
						Player.statLife = Player.statLifeMax2;
				}

				if (currentShield <= 0) {
					PlayShieldBreakSound();
					shieldBreakTimer = SHIELD_BREAK_COOLDOWN;
				}
			}
			else {
				modifiers.SourceDamage.Flat -= (int)currentShield;
				CombatText.NewText(Player.getRect(), new Color(113, 133, 162), $"-{currentShield}", true);

				currentShield = 0;
				PlayShieldBreakSound();
				shieldBreakTimer = SHIELD_BREAK_COOLDOWN;
			}
		}

		public override void ModifyHitByProjectile(Projectile proj, ref Player.HurtModifiers modifiers) {
			ProcessDamage(proj.damage, ref modifiers);
		}

		public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers) {
			ProcessDamage(npc.damage, ref modifiers);
		}

		private void ResetShield() {
			currentShield = 0;
			maxShield = 0;
			originalMaxLife = 0;
			shieldRegenCounter = 0f;
			shieldBreakTimer = 0;
			bossShieldFilled = false;
			wasDeadLastFrame = false;
		}


		private bool AnyBossAlive() {
			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];
				if (npc.active && npc.boss)
					return true;
			}
			return false;
		}

		private void PlayShieldBreakSound() {
			SoundEngine.PlaySound(SoundID.NPCDeath56, Player.position);
			shieldRegenCounter = 0f;
		}
	}

	public class ShieldTextSystem : ModSystem
	{
		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {

			int resourceBarIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Resource Bars");
			if (resourceBarIndex != -1) {
				layers.Insert(resourceBarIndex + 1, new LegacyGameInterfaceLayer(
					"ArknightsMod: Shield Text",
					delegate {
						DrawShieldText();
						return true;
					},
					InterfaceScaleType.UI)
				);
			}
		}

		private void DrawShieldText() {
			Player player = Main.LocalPlayer;
			if (player.dead || !player.active)
				return;

			var shieldPlayer = player.GetModPlayer<FallenSovereignShieldPlayer>();
			if (!shieldPlayer.hasFallenSovereignShield || shieldPlayer.currentShield <= 0)
				return;


			Vector2 screenCenter = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);
			Vector2 position = new Vector2(screenCenter.X, screenCenter.Y + 25f);


			string shieldText = $"{(int)shieldPlayer.currentShield}/{(int)shieldPlayer.maxShield}";
			Vector2 textSize = FontAssets.ItemStack.Value.MeasureString(shieldText);
			Vector2 textPos = position - new Vector2(textSize.X / 2, 0);

			Color textColor = new Color(150, 200, 255);


			Utils.DrawBorderStringFourWay(
				Main.spriteBatch,
				FontAssets.ItemStack.Value,
				shieldText,
				textPos.X,
				textPos.Y,
				textColor,
				Color.Black,
				Vector2.Zero,
				0.8f
			);
		}
	}
}