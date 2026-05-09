using ArknightsMod.Content.Items.Armor.Vanity.Defender.Beagle.Armor;
using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Projectiles.Defender.Beagle;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Players;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Defender.Durnar
{
    public class DN_Weapon : UpgradeWeaponBase
    {
        public override void AddRecipes() 
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<OrirockCluster>(), 19)
				.AddIngredient(ModContent.ItemType<RMA7012>(), 3)
				.AddTile(ModContent.TileType<FactoryTile>())
				.Register();
		}
		private static SoundStyle SkillActive3;
		private static SoundStyle NoSound;
		public override void Load() 
		{
			SkillActive3 = new SoundStyle("ArknightsMod/Sounds/SkillActive3") 
			{
				Volume = 0.4f,
				MaxInstances = 4,
			};
			NoSound = new SoundStyle("ArknightsMod/Sounds/NoSound") 
			{
				Volume = 0f,
				MaxInstances = 4,
			};
		}
		public override void SetDefaults()
		{
			Item.damage = 23; // �����˺�
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
		public override bool CanUseItem(Player player) 
		{
			var modPlayer = Main.LocalPlayer.GetModPlayer<WeaponPlayer>();
			if (Main.myPlayer == player.whoAmI) 
			{
				if (player.altFunctionUse == 2) 
				{
					if (!modPlayer.SummonMode) 
					{
						// S1
						if (modPlayer.Skill == 0 && modPlayer.StockCount > 0 && !modPlayer.SkillActive) 
						{
							modPlayer.SkillActive = true;
							modPlayer.SkillTimer = 0;

							modPlayer.DelStockCount();
							player.GetModPlayer<DN_player>().hasDNplayer = true;
							Item.UseSound = SkillActive3;
							SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
						}
						else
							player.GetModPlayer<DN_player>().hasDNplayer = false;
						return false;
					}
				}
				else 
				{
					if (!modPlayer.SummonMode) 
					{
						Item.UseSound = NoSound;
						SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
						// S1
						if (modPlayer.Skill == 0 && modPlayer.SkillActive) {}
						else if (modPlayer.Skill == 0 && !modPlayer.SkillActive) {}
					}
				}
			}
			return base.CanUseItem(player);
		}
		public override void HoldItem(Player player)
		{
			var modPlayer = Main.LocalPlayer.GetModPlayer<WeaponPlayer>();
			if (Main.myPlayer == player.whoAmI) {
				if (modPlayer.Skill == 0 && !modPlayer.SkillActive) {
					player.GetModPlayer<DN_player>().hasDNplayer = false;
				}
				if (modPlayer.Skill == 0 && modPlayer.SkillActive && Item.type == ModContent.ItemType<DN_Weapon>()) {
					player.GetModPlayer<DN_player>().hasDNplayer = true;
				}
			}
		}
		public class DN_player : ModPlayer
		{
			public bool hasDNplayer = false;
			public override void ResetEffects() 
			{
				//����2��������Ч��
				if (hasDNplayer == true) {
					Player.statDefense *= 1.5f;
				}
				if (Main.myPlayer != Player.whoAmI)
					return;  // ֻ�����������
				bool isHoldingTargetWeapon = Player.HeldItem.type == ModContent.ItemType<DN_Weapon>();
				if (!isHoldingTargetWeapon) {
					Player.GetModPlayer<DN_player>().hasDNplayer = false;
				}
			}
		}
    
    }
}