using Terraria;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using ArknightsMod.Systems.Gameplay.Elemental;
using ArknightsMod.Content.NPCs.Enemy.Seamonster;
using ArknightsMod.Common.UI;


//如果是元素损伤不如命名成ElementalDamage，Sanity是理智
namespace ArknightsMod.Players
{
    public sealed class ElementalPlayer: ModPlayer
	{
		private ElementalData playerElementalData;
		private int _updatedDmg;
		private int _playerWhoAmI;
		private IEntitySource playerSource;
		private bool elementalRegistered;
		
		public override void OnEnterWorld() {
			elementalRegistered = false;
			EnsureElementalRegistered();
			playerSource = Player.GetSource_FromThis();

			if (!Main.dedServ && Player.whoAmI == Main.myPlayer) Santable.Visible = true;
		
		}
		public override void PreUpdate() {
			// OnEnterWorld only initializes the local client. Server and remote
			// instances also run PreUpdate and need their own actual player index.
			EnsureElementalRegistered();
			CheckNearbyEnemies();
			ElementalSystem.RegenerateElemental(_playerWhoAmI);
		}

		private void EnsureElementalRegistered() {
			if (elementalRegistered && _playerWhoAmI == Player.whoAmI) return;
			_playerWhoAmI = Player.whoAmI;
			playerElementalData = ElementalSystem.RegisterEntityElemental(_playerWhoAmI, EntityType.Player);
			elementalRegistered = true;
		}



		private void CheckNearbyEnemies()
		{
			foreach (var npc in Main.ActiveNPCs) {
				if (npc.type == ModContent.NPCType<BasinSeaReaper>() &&
				    Vector2.DistanceSquared(npc.position, Player.position) <= 36000 &&
				    npc.life <= npc.lifeMax * 0.99f) {

					ElementalSystem.ApplyDamage(0, 5, _playerWhoAmI);
				}
			}
		}

		public override void OnHitByNPC(NPC npc, Player.HurtInfo hurtInfo) {
			switch (npc.type) {
				case var _ when npc.type==ModContent.NPCType<DeepSeaSlider>():
					ElementalSystem.ApplyDamage(0, 5, _playerWhoAmI);
					break;
				case var _ when npc.type==ModContent.NPCType<TheFirstToTalk>():
					ElementalSystem.ApplyDamage(0, Main.expertMode ? 350 : 300, _playerWhoAmI);
					break;
			}
		}

		//public override void OnHitByProjectile(Projectile proj, Player.HurtInfo hurtInfo) {
		//	if (ElementalBurstCD <= 600)
		//		return;
		//	var NervousDmg = new Dictionary<int, int> {
		//		{ModContent.ProjectileType<TFTTShoot>(), Main.expertMode? 500:400 },
		//		{ModContent.ProjectileType<seashoot>(), 300},
		//		{ModContent.ProjectileType<TFTTSkillshoot>(), 300},
		//		{ModContent.ProjectileType<TFTTRush2>(), 300},
		//		{ModContent.ProjectileType<TFTTRush>(), 300},
		//		{ModContent.ProjectileType<PocketSeaCrawlerShoot>(), 300},
		//		{ModContent.ProjectileType<PocketSeaCrawlerShoot2>(), 300},
		//	};

		//}

		//public void OnEnterWorld() {
		//	Santable.Visible = true;
		//}

		
		//这个放在哪？
	}
}
