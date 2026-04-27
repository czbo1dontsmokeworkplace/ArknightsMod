using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Projectiles.Sniper.Schwarz;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Sniper.Schwarz
{
    public class SchwarzBow : UpgradeWeaponBase
    {
        private static SoundStyle AttackSound;
        private static SoundStyle Skill2Sound;
        private static SoundStyle Skill3Sound;
        private static SoundStyle SkillActive1;

        private float _s1Multiplier = 1f;

        public override void Load()
        {
            AttackSound = new SoundStyle("ArknightsMod/Content/Items/Weapons/Sniper/Schwarz/SchwarzAttackSound")
            {
                Volume = 0.4f,
                MaxInstances = 4,
            };

            Skill2Sound = new SoundStyle("ArknightsMod/Content/Items/Weapons/Sniper/Schwarz/SchwarzSkill2Sound")
            {
                Volume = 0.4f,
                MaxInstances = 4,
            };

            Skill3Sound = new SoundStyle("ArknightsMod/Content/Items/Weapons/Sniper/Schwarz/SchwarzSkill3Sound")
            {
                Volume = 0.4f,
                MaxInstances = 4,
            };

            SkillActive1 = new SoundStyle("ArknightsMod/Sounds/SkillActive1")
            {
                Volume = 0.4f,
                MaxInstances = 4,
            };
        }

        public override void SetDefaults()
        {
            Item.width = 62;
            Item.height = 32;

            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(0, 40, 30, 0);

            Item.useTime = 48;
            Item.useAnimation = 48;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;

            Item.DamageType = DamageClass.Ranged;
            Item.damage = 168;
            Item.knockBack = 10f;

            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<SchwarzArrow>();
            Item.shootSpeed = 20f;

            Item.UseSound = AttackSound;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<D32Steel>(4)
                .AddIngredient<OrironBlock>(5)
                .AddTile(ModContent.TileType<FactoryTile>())
                .Register();
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<WeaponPlayer>();

            if (player.altFunctionUse == 2) // 右键
            {
                if (!modPlayer.SummonMode && modPlayer.StockCount > 0 && !modPlayer.SkillActive)
                {
                    if (modPlayer.Skill == 1 || modPlayer.Skill == 2)
                    {
                        modPlayer.SkillActive = true;
                        modPlayer.SkillTimer = 0;
                        modPlayer.DelStockCount();

                        SoundEngine.PlaySound(SkillActive1, player.Center);
                        return true;
                    }
                }
                return false;
            }
            else // 左键
            {
                if (!modPlayer.SummonMode)
                {
                    Item.UseSound = AttackSound;

                    if (modPlayer.Skill == 0 && modPlayer.StockCount > 0 && !modPlayer.SkillActive)
                    {
                        modPlayer.SkillActive = true;
                        modPlayer.SkillTimer = 0;
                        modPlayer.DelStockCount();

                        _s1Multiplier = Main.rand.NextFloat() < 0.2f ? 2.2f : 2.2f * 1.6f;

                        SoundEngine.PlaySound(SkillActive1, player.Center);
                    }
                }
            }

            return base.CanUseItem(player);
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            var modPlayer = player.GetModPlayer<WeaponPlayer>();

            if (modPlayer.Skill == 1 && modPlayer.SkillActive)
            {
                damage *= Main.rand.NextFloat() < 0.5f ? 2.3f : 2.3f * 1.6f;
            }
            else if (modPlayer.Skill == 2 && modPlayer.SkillActive)
            {
                damage *= 2.8f * 1.6f;
            }
        }

        public override bool Shoot(Player player,
            EntitySource_ItemUse_WithAmmo source,
            Vector2 position,
            Vector2 velocity,
            int type,
            int damage,
            float knockback)
        {
            var modPlayer = player.GetModPlayer<WeaponPlayer>();

            int finalDamage = damage;

            // S1
            if (modPlayer.Skill == 0 && modPlayer.SkillActive)
            {
                finalDamage = (int)(damage * _s1Multiplier);
            }

            // 发射箭
            var proj = Projectile.NewProjectileDirect(
                source,
                position,
                velocity,
                ModContent.ProjectileType<SchwarzArrow>(),
                finalDamage,
                knockback,
                player.whoAmI
            );

            // ⭐ 给 Projectile 标记技能状态（给 shader/轨迹用）
            proj.ai[0] = modPlayer.Skill;
            proj.ai[1] = modPlayer.SkillActive ? 1f : 0f;

            // 技能音效
            if (modPlayer.Skill == 1 && modPlayer.SkillActive)
            {
                SoundEngine.PlaySound(Skill2Sound, player.Center);
            }
            else if (modPlayer.Skill == 2 && modPlayer.SkillActive)
            {
                SoundEngine.PlaySound(Skill3Sound, player.Center);
            }

            // 回能
            if (modPlayer.Skill == 0 && modPlayer.StockCount == 0 && !modPlayer.SkillActive)
            {
                modPlayer.OffensiveRecovery();
            }

            return false;
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(0, 4);
        }
    }
}