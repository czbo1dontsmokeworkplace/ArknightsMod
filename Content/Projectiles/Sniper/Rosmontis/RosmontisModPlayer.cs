using ArknightsMod.Content.Items.Weapons.Sniper.Rosmontis;
using ArknightsMod.Content.Projectiles.Sniper.Rosmontis.WeaponInPlayer;
using ArknightsMod.Content.Systems;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Rosmontis
{
	public class RosmontisModPlayer : ModPlayer
	{
		private bool _isEquipWeapon;
		public RosmontisVisualEffectController Controller { get; private set; }

		public List<(int,int)> Skill3TaggedProjetileIndexs { get; } = new List<(int,int)>();

		private bool wasInSkill3 = false;
		public override void Initialize()
		{

		}

		public bool IsEquipWeapon
		{
			get => _isEquipWeapon;
			set
			{
				if (_isEquipWeapon == value)
					return;

				_isEquipWeapon = value;
				if (value)
					OnEquipingWeapon();
				else
					OnDisEquipingWeapon();
			}
		}

		public override void PreUpdate() {
			IsEquipWeapon = Player.HeldItem.ModItem is RosmontisSwordShield;
			WeaponPlayer modPlayer = Player.GetModPlayer<WeaponPlayer>();
			if (modPlayer.SkillActive && modPlayer.Skill == 3) {
			}

			base.PreUpdate();

		}

		protected virtual void OnEquipingWeapon() {
			Controller ??= new RosmontisVisualEffectController(Player);
			Controller.StartOnEquipingWeaponAnimation();
		}

		protected virtual void OnDisEquipingWeapon()
		{
			Controller?.StartOnDisEquipingWeaponAnimation();
		}

		public void TryReturnSkill4Projectile(int projID) {
			for (int i = Skill3TaggedProjetileIndexs.Count -1; i >= 0; i--) {
				var item = Skill3TaggedProjetileIndexs[i];
				if (item.Item1 == projID) {
					Controller.StartOnEquipingWeaponAnimationForSkill4(item.Item2);
					Skill3TaggedProjetileIndexs.RemoveAt(i);
					break;
				}
			}
		}

		public void ShootSkill3TaggedProjectile(EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int damage, float knockback,
			Player player) {

			if (Skill3TaggedProjetileIndexs.Count > 2) {
				var _out = Skill3TaggedProjetileIndexs.First();
				var modProj = Main.projectile[_out.Item1].ModProjectile as RosmontisProjectileBase;
				if (modProj != null) {
					modProj.Skill4Exit();
				}
			}

			var proj = ModContent.ProjectileType<RosmontisProjectileBase>();
			var projID = Projectile.NewProjectile(source, position, velocity, proj, damage, knockback, player.whoAmI,4);
			int record = Controller.ShootAnimationForSkill4();
			Skill3TaggedProjetileIndexs.Add((projID, record));

		}

		public void RecordSkill3Start() {
			wasInSkill3 = true;
		}
	}

	public class RosmontisVisualEffectController
	{
		private readonly RosmontiWeaponInPlayerDrawer[] weaponDrawers;
		private readonly PlayerExtraDrawerHandle[] drawerHandles;

		public RosmontisVisualEffectController(Player player)
		{
			weaponDrawers = new[]
			{
				CreateDrawer(player, 25f, true, "Rosmontis Sword1", 0),
				CreateDrawer(player, -25f, true, "Rosmontis Sword2", 1),
				CreateDrawer(player, 35f, true, "Rosmontis Sword3", 2),
				CreateDrawer(player, -35f, true, "Rosmontis Sword4", 3)
			};

			weaponDrawers[0].DrawInFrontOfPlayer = true;
			weaponDrawers[1].DrawInFrontOfPlayer = true;
			weaponDrawers[2].DrawBehindPlayer = true;
			weaponDrawers[3].DrawBehindPlayer = true;

			drawerHandles = new PlayerExtraDrawerHandle[weaponDrawers.Length];
			PlayerExtraDrawerSystem system = ModContent.GetInstance<PlayerExtraDrawerSystem>();
			for (int i = 0; i < weaponDrawers.Length; i++)
				drawerHandles[i] = system.RegisterExtraDrawer(weaponDrawers[i], player);
		}

		public void StartOnEquipingWeaponAnimation()
		{
			foreach (RosmontiWeaponInPlayerDrawer drawer in weaponDrawers)
				drawer.StartOnEquipingWeaponAnimation();
		}

		public void StartOnEquipingWeaponAnimationForSkill4(int index)
		{
			weaponDrawers[index].StartOnEquipingWeaponAnimation();
		}

		public void StartOnDisEquipingWeaponAnimation()
		{
			foreach (RosmontiWeaponInPlayerDrawer drawer in weaponDrawers)
				drawer.StartOnDisEquipingWeaponAnimation();
		}

		public void StartShootAnimation(int indexs,bool shouldReturn  =true)
		{
			weaponDrawers[indexs].StartShootAnimation(indexs,shouldReturn);
		}

		public void StartShootAnimationForSkill4(int indexs,bool shouldReturn  =true)
		{
			weaponDrawers[indexs].StartSpeicalShootAnimation(indexs,shouldReturn);
		}

		public void ShootAnimationForBase() {
			bool ready1 = weaponDrawers[2].CurrentAnimationType == RosmontiWeaponAnimationType.Weaponing;
			bool ready2= weaponDrawers[3].CurrentAnimationType == RosmontiWeaponAnimationType.Weaponing;

			if ((ready1 && ready2) || (!ready1 && !ready2)) {
				int randomIndex = Main.rand.Next(2, 4);
				StartShootAnimation(randomIndex);
				return;
			}

			if (ready1) {
				StartShootAnimation(2);
				return;
			}

			if (ready2) {
				StartShootAnimation(3);
				return;
			}
		}
		public int ShootAnimationForSkill4() {
			bool ready1 = weaponDrawers[0].CurrentAnimationType != RosmontiWeaponAnimationType.Skill3SpecialShoot;
			bool ready2= weaponDrawers[1].CurrentAnimationType != RosmontiWeaponAnimationType.Skill3SpecialShoot;

			if ((ready1 && ready2) || (!ready1 && !ready2)) {
				int randomIndex = Main.rand.Next(0, 2);
				StartShootAnimationForSkill4(randomIndex,false);
				return randomIndex;
			}

			if (ready1) {
				StartShootAnimationForSkill4(0,false);
				return 0;
			}

			if (ready2) {
				StartShootAnimationForSkill4(1,false);
				return 1;
			}
			return 0;
		}
		private static RosmontiWeaponInPlayerDrawer CreateDrawer(
			Player player,
			float rotationDegrees,
			bool chasingPlayer,
			string textureName,
			int index)
		{
			Texture2D texture = ModContent.Request<Texture2D>(
				$"ArknightsMod/Content/Projectiles/Sniper/Rosmontis/WeaponInPlayer/{textureName}",
				AssetRequestMode.ImmediateLoad).Value;
			Texture2D glowTexture = ModContent.Request<Texture2D>(
				$"ArknightsMod/Content/Projectiles/Sniper/Rosmontis/WeaponInPlayer/{textureName}_Glow",
				AssetRequestMode.ImmediateLoad).Value;

			return new RosmontiWeaponInPlayerDrawer(
				player,
				MathHelper.ToRadians(rotationDegrees),
				chasingPlayer,
				texture,
				glowTexture,
				index);
		}
	}
}
