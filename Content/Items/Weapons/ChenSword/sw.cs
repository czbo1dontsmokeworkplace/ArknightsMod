using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.RGB;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Chen;

namespace ArknightsMod.Content.Items.Weapons.ChenSword
{ 
	// This is a basic item template.
	// Please see tModLoader's ExampleMod for every other example:
	// https://github.com/tModLoader/tModLoader/tree/stable/ExampleMod
	public class sw : ModItem
	{
        /// <summary>
        /// 偷懒写的，记得改（）
        /// </summary>
        public override string Texture => new sw_Proj_1().Texture;
        public override void SetDefaults()
		{
            Item.damage = 25;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.width = 64;
            Item.height = 64;
            Item.scale = 1f;
            Item.rare = 2;
            Item.knockBack = 9f;
            Item.value = Item.buyPrice(0, 0, 80, 0);
            Item.useTurn = false;
            Item.autoReuse = false;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<sw_Proj_2>();
            Item.useStyle = ItemUseStyleID.HiddenAnimation;
            Item.channel = true;
            Item.noUseGraphic = true;
            Item.noMelee = true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, player.Center, Vector2.Zero, ModContent.ProjectileType<sw_Proj_1>(), damage, knockback, player.whoAmI);
            //return false;
            return base.Shoot(player, source, position, velocity, type, damage, knockback);
        }
    }
    public class Skill_2_Player : ModPlayer
    {
        private Vector2 NowPos = Vector2.Zero;
        private Vector2 ToVec = Vector2.Zero;
        private float LerpStep = 0;
        private bool SettingScreenPos = false;
        public bool CanBeDamaged = true;
        public void SetScreenPos(Vector2 toPos)
        {
            ToVec = toPos;
            SettingScreenPos = true;
        }
        public override bool ConsumableDodge(Player.HurtInfo info)
        {
            bool c = !CanBeDamaged;
            CanBeDamaged = true;

            return base.ConsumableDodge(info) || c;
        }
        public override void PreUpdate()
        {
            if (SettingScreenPos)
            {
                NowPos = Vector2.Lerp(NowPos, ToVec, LerpStep);
                LerpStep = MathHelper.Lerp(LerpStep, 1, 0.03f);
            }
            else
            {
                NowPos = Vector2.Lerp(NowPos, Player.Center, 1 - LerpStep);
                LerpStep = MathHelper.Lerp(LerpStep, 0, 0.03f);
            }
            SettingScreenPos = false;

            base.PreUpdate();
        }
        public override void ModifyScreenPosition()
        {
            if (SettingScreenPos)
                Main.screenPosition = NowPos - Main.ScreenSize.ToVector2() * 0.5f;
            else
            {
                if (Vector2.Distance(NowPos, Player.Center) > 40)
                    Main.screenPosition = NowPos - Main.ScreenSize.ToVector2() * 0.5f;
                else
                    NowPos = Player.Center;
            }
            base.ModifyScreenPosition();
        }
    }

}
