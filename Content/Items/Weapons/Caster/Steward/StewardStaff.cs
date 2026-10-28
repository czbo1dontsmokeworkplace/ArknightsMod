using ArknightsMod.Content.Items.Weapons.Caster.CoreCasters;
using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Content.Items.Armor.Caster.Steward;
using ArknightsMod.Players;
using ArknightsMod.Systems.Gameplay.Skill;
using ArknightsMod.Content.Projectiles.Caster.CoreCasters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster.Steward
{
	/// <summary>
	/// 史都华德的法杖（迁移自 ArknightsWeaponryExpansion 的 SDHD）。
	/// <br/>1 技能·强力击：攻击充能，每蓄满一次后下一击射出强化直线弹。
	/// </summary>
	public class StewardStaff : ExpansionWeaponBase
	{
		public override void SetStaticDefaults() => Item.staff[Type] = true;
		protected override int[] EliteDamage => [43, 54];

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Magic;
			Item.width = 54;
			Item.height = 54;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.knockBack = 2f;
			Item.value = Item.sellPrice(silver: 50);
			Item.rare = ItemRarityID.Blue;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.shoot = ModContent.ProjectileType<StewardProj>();
			Item.mana = 7;
			Item.crit = 4;
			Item.shootSpeed = 11f;
			Item.useTurn = false;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.channel = false;
			Item.staff[Item.type] = true;
            CoreCasterCadence.Apply(Item);
		}

		public override void AddRecipes() => CreateRecipe()
			.AddIngredient<global::ArknightsMod.Content.Items.Weapons.Caster.Durin.DurinWand>()
			.AddRecipeGroup(RecipeGroupID.IronBar, 8).AddIngredient(ItemID.Sapphire, 3)
			.AddIngredient(ItemID.FallenStar, 5).AddTile(TileID.Anvils).Register();

		public override bool CanUseItem(Player player) {
            if (player.itemAnimation <= 0) CoreCasterCadence.Apply(Item);
			if (Main.myPlayer == player.whoAmI) {
				var mp = player.GetModPlayer<WeaponPlayer>();
				// 攻击充能：每次攻击积攒技力，蓄满即得到 1 层库存
				if (mp.CurrentSkill?.ChargeType == SkillChargeType.Attack && !mp.SkillActive)
					mp.OffensiveRecovery();
			}
			return base.CanUseItem(player);
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			CoreCasterVisuals.Muzzle(position + velocity * 2, velocity.SafeNormalize(Vector2.UnitX), CoreCasterKind.Steward, mp.StockCount > 0);

			if (mp.StockCount > 0) {
				// 强力击：消耗 1 层库存，射出强化追踪弹
				mp.DelStockCount();
				Projectile.NewProjectile(source, position + velocity, velocity * 0.9f, type,
					(int)(damage * 1.90f), knockback, player.whoAmI, ai0: 1f);
				SoundEngine.PlaySound(SoundID.Item20, position);
				SoundEngine.PlaySound(SoundID.Item28, position);
			}
			else {
				Projectile.NewProjectile(source, position + velocity, velocity, type,
					damage, knockback, player.whoAmI);
				SoundEngine.PlaySound(SoundID.Item28, position);
			}
			return false;
		}
	}

	/// <summary>史都华德的追踪法术弹，优先追踪附近防御力最高的敌人。</summary>
	public class StewardProj : ModProjectile, ICoreCasterGuidance
	{
		private CoreCasterFlight flight;
		public bool GuidanceReady => flight?.CanGuide(CoreCasterKind.Steward) == true;
		public override void SendExtraAI(BinaryWriter writer) => CoreCasterGuidance.Write(writer, flight);
		public override void ReceiveExtraAI(BinaryReader reader) => flight = CoreCasterGuidance.Read(reader);
		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailCacheLength[Type] = 80;
			ProjectileID.Sets.TrailingMode[Type] = 0;
		}
		public override string Texture => "ArknightsMod/Content/Items/Weapons/Caster/Steward/StewardProj";

		public override void SetDefaults() {
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.scale = 1f;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = false;
			Projectile.timeLeft = 60;
			Projectile.penetrate = 1;
			Projectile.alpha = 255;
			Projectile.light = 0.5f;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 8;
			Projectile.usesIDStaticNPCImmunity = true;
			Projectile.idStaticNPCHitCooldown = 60;
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source) => CoreCasterFlight.Configure(Projectile, CoreCasterKind.Steward);
		public override void AI() {
			flight ??= new CoreCasterFlight(Projectile.Center, Projectile.velocity, Projectile.MaxUpdates, Projectile.identity);
			if (Projectile.numUpdates == 0)
				CoreCasterVisuals.Trail(Projectile, CoreCasterKind.Steward, (int)Projectile.ai[0]);

			// 穿套装时由全局PostAI统一选择最高防目标，避免两套追踪互相拉扯。
			if (Main.player[Projectile.owner].GetModPlayer<StewardSetPlayer>().StewardSetActive)
			{
				flight.Step(CoreCasterKind.Steward, (int)Projectile.ai[0], Projectile.Center, ref Projectile.velocity, Projectile.MaxUpdates);
				Projectile.rotation = Projectile.velocity.ToRotation();
				return;
			}

			// 优先追踪 200 像素内防御力最高的敌人
			NPC target = null;
			float distanceMax = 200f;
			int highestDefense = 0;
			foreach (NPC npc in Main.npc) {
				if (npc.active && !npc.friendly) {
					float currentDistance = Vector2.Distance(npc.Center, Projectile.Center) + Vector2.Distance(npc.Size, Vector2.Zero) / 2f;
					if (currentDistance < distanceMax) {
						highestDefense = npc.defense;
						distanceMax = currentDistance;
						target = npc;
					}
					if (npc.defense > highestDefense && currentDistance < 200) {
						highestDefense = npc.defense;
						target = npc;
					}
				}
			}

			flight.Step(CoreCasterKind.Steward, (int)Projectile.ai[0], Projectile.Center, ref Projectile.velocity, Projectile.MaxUpdates, target?.Center);
			Projectile.rotation = Projectile.velocity.ToRotation();
		}

		public override bool PreDraw(ref Color lightColor) {
			CoreCasterVisuals.Bolt(Projectile, CoreCasterKind.Steward, (int)Projectile.ai[0]);
			return false;
		}
		public override void OnKill(int timeLeft) { CoreCasterVisuals.Retire(Projectile, CoreCasterKind.Steward, (int)Projectile.ai[0]); CoreCasterVisuals.Impact(Projectile.Center, Projectile.velocity,
			CoreCasterKind.Steward, Projectile.ai[0] == 1); }
	}
}
