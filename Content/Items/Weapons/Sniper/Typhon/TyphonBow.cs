using System;
using ArknightsMod.Content.Items.Weapons.Charged;

using ArknightsMod.Content.Projectiles.Sniper.Typhon;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Sniper.Typhon
{
    [LegacyName("FartoothGreatbow")]
    public class TyphonBow : ExpansionWeaponBase
    {
        // Shared by the restored skill-arrow effects.
        public const float NormalAttackMuzzleRingRadius = 66, NormalAttackMuzzleVisualScale = 1.12f, NormalAttackMuzzleCrossGeometryScale = .3f;
        public const int NormalAttackMuzzleCrossDurationTicks = 32;
        public const int S2FollowupDelay = 6;
        internal static readonly SoundStyle FireSound = new("ArknightsMod/Assets/Sounds/Typhon/StarmadaFire") { Volume = .5f, MaxInstances = 4 };
        public static Vector2 MortarDirection(Player player, Vector2 target)
        {
            Vector2 skyAnchor = new(MathHelper.Lerp(target.X, player.Center.X, .55f), player.Center.Y - 500f * player.gravDir);
            return (skyAnchor - player.MountedCenter).SafeNormalize(-Vector2.UnitY * player.gravDir);
        }
        public static float GetS3SkillIdleItemRotation(Player player)
            => (MortarDirection(player, TyphonAimReticle.ResolveAimPoint(player)) * player.direction).ToRotation();
        internal static void ApplyS3Pose(Player player)
        {
            Vector2 target = TyphonAimReticle.ResolveAimPoint(player);
            Vector2 aim = MortarDirection(player, target);
            player.ChangeDir(target.X < player.Center.X ? -1 : 1);
            player.itemRotation = (aim * player.direction).ToRotation();
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, aim.ToRotation() - MathHelper.PiOver2);
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Quarter, aim.ToRotation() - MathHelper.PiOver2 - .12f * player.direction);
        }
        private const int OriginalSkillUseTime = 72;
        private static readonly SoundStyle SkillActiveSound = new("ArknightsMod/Sounds/SkillActive1") { Volume = .4f, MaxInstances = 4 };
        protected override int[] EliteDamage => [816, 960, 1120];
        private bool consuming;
        public override void SetDefaults()
        {
            Item.damage = EliteDamage[0]; Item.DamageType = DamageClass.Ranged;
            Item.width = 40; Item.height = 40; Item.useTime = Item.useAnimation = 60;
            Item.useStyle = ItemUseStyleID.Shoot; Item.noMelee = true; Item.autoReuse = true;
            Item.knockBack = 6; Item.rare = ItemRarityID.Red; Item.value = Item.buyPrice(gold: 1);
            Item.shoot = ModContent.ProjectileType<TyphonAstralShot>(); Item.shootSpeed = 20;
            Item.useAmmo = AmmoID.Arrow;
            Item.UseSound = FireSound;
        }
        public override ModItem Clone(Item newEntity) { var clone = (TyphonBow)base.Clone(newEntity); clone.consuming = false; return clone; }
        public override bool AltFunctionUse(Player player) => true;
        public override Vector2? HoldoutOffset() => new(-8, 0);
        public override bool CanConsumeAmmo(Item ammo, Player player) => consuming || player.altFunctionUse != 2;
        public override void AddRecipes() => CreateRecipe().AddIngredient<global::ArknightsMod.Content.Items.Weapons.Charged.FirewatchRifle>()
            .AddIngredient(ItemID.FragmentVortex, 14).AddIngredient(ItemID.ShroomiteBar, 12).AddTile(TileID.LunarCraftingStation).Register();
        public override void HoldItem(Player player)
        {
            base.HoldItem(player);
            Item.noUseGraphic = player.ownedProjectileCounts[ModContent.ProjectileType<TyphonMagazine>()] > 0;
            ConfigureAttack(player);
            if (player.whoAmI != Main.myPlayer) return;
            var skills = player.GetModPlayer<WeaponPlayer>();
            if (skills.Skill == 2 && skills.SkillActive && player.ownedProjectileCounts[ModContent.ProjectileType<TyphonAimReticle>()] == 0)
                Projectile.NewProjectile(player.GetSource_FromThis(), player.Center, Vector2.Zero,
                    ModContent.ProjectileType<TyphonAimReticle>(), 0, 0, player.whoAmI);
            var state = player.GetModPlayer<ChargedOperatorPlayer>();
            bool valid = ChargedOperatorWeapon.WorldInputAllowed(player);
            if (!Main.mouseRight) state.SuppressSniperRight = false;
            else if (!valid || player.tileInteractionHappened) state.SuppressSniperRight = true;
            if (ArknightsKeybinds.SkillActivatePressed(player)) { ActivateSkill(player); return; }
            if (!valid || !Main.mouseRight || state.SuppressSniperRight || player.itemAnimation > 0 || Item.noUseGraphic || !player.HasAmmo(Item)) return;
            Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
            Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.MountedCenter, aim, ModContent.ProjectileType<TyphonMagazine>(), 0, 0, player.whoAmI);
            Item.noUseGraphic = true;
        }
        private void ActivateSkill(Player player)
        {
            var skills = player.GetModPlayer<WeaponPlayer>();
            if (skills.SkillActive || skills.StockCount <= 0 || skills.CurrentSkill == null) return;
            skills.SkillActive = true; skills.SkillTimer = 0; skills.DelStockCount();
            if (skills.Skill == 0) player.GetModPlayer<TyphonState>().S1Stacks = 0;
            if (skills.Skill == 1)
            {
                var state = player.GetModPlayer<TyphonState>();
                state.S2ActivationCount++;
                skills.CurrentSkill.AutoUpdateActive = state.S2ActivationCount < 2;
                if (state.S2ActivationCount == 1) state.S2FirstHits = 0;
            }
            SoundEngine.PlaySound(SkillActiveSound, player.Center);
        }
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            base.ModifyWeaponDamage(player, ref damage);
            var skills = player.GetModPlayer<WeaponPlayer>();
            if (!skills.SkillActive) return;
            damage *= skills.Skill switch { 0 => 1.45f + .05f * player.GetModPlayer<TyphonState>().S1Stacks, 1 => 1.911f, _ => 1.75f };
        }
        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (Item.noUseGraphic) return;
            var modPlayer = player.GetModPlayer<WeaponPlayer>();
            if (modPlayer.Skill == 2 && modPlayer.SkillActive)
                ApplyS3Pose(player);
        }

        public static int AttackInterval(int skill, bool active, int stacks)
            => !active ? 60 : skill switch {
                0 => Math.Max(1, (int)(OriginalSkillUseTime / (1.45f + .05f * Math.Clamp(stacks, 0, 30)))),
                2 => OriginalSkillUseTime + 93,
                _ => OriginalSkillUseTime
            };
        private void ConfigureAttack(Player player)
        {
            var state = player.GetModPlayer<WeaponPlayer>();
            Item.useTime = Item.useAnimation = AttackInterval(state.Skill, state.SkillActive, player.GetModPlayer<TyphonState>().S1Stacks);
            Item.shootSpeed = state.SkillActive && state.Skill != 1 ? 8 : 20;
            // S2 plays the same sound on each actual star-crystal spawn; S3 plays it when charge releases.
            Item.UseSound = state.SkillActive ? state.Skill is 1 or 2 ? null : SoundID.Item5 : FireSound;
        }
        public override bool CanUseItem(Player player)
        {
            ConfigureAttack(player);
            return player.altFunctionUse != 2 && !Main.mouseRight
                && !ArknightsKeybinds.SkillActivatePressed(player) && ChargedOperatorWeapon.WorldInputAllowed(player)
                && player.ownedProjectileCounts[ModContent.ProjectileType<TyphonMagazine>()] == 0;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.whoAmI != Main.myPlayer) return false;
            Vector2 aim = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            var modPlayer = player.GetModPlayer<WeaponPlayer>();
            int cleanDamage = ComputeFixedDamage(player);
            if (modPlayer.Skill == 2 && modPlayer.SkillActive)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    Vector2 target = TyphonAimReticle.ResolveAimPoint(player);
                    float aimAngle = MortarDirection(player, target).ToRotation();
                    Projectile.NewProjectile(
                        source, position, Vector2.Zero,
                        ModContent.ProjectileType<TyphonStar>(),
                        cleanDamage, knockback, player.whoAmI,
                        target.X, target.Y,
                        aimAngle);
                }
                return false;
            }

            if (modPlayer.Skill == 1 && modPlayer.SkillActive)
            {
                FindTwoTargets(player, position, out NPC? t1, out NPC? t2);
                NPC second = t2 ?? t1;
                int crit = player.GetWeaponCrit(Item);
                SpawnS2Crystal(player, source, aim, damage, knockback, crit, t1?.whoAmI + 1 ?? 0);
                player.GetModPlayer<TyphonState>().QueueS2Followup(aim, damage, knockback, crit, source.AmmoItemIdUsed, second?.whoAmI + 1 ?? 0);
                return false;
            }

            if (modPlayer.Skill == 0 && modPlayer.SkillActive)
            {
                Projectile.NewProjectile(
                    source, position, velocity,
                    ModContent.ProjectileType<TyphonArrow>(),
                    cleanDamage, knockback, player.whoAmI,
                    0f, 0f, 2f);
                return false;
            }


            int index = Projectile.NewProjectile(source, player.MountedCenter + aim * 48, aim * 26,
                ModContent.ProjectileType<TyphonAstralShot>(), damage, knockback, player.whoAmI, 0);
            if (Main.projectile.IndexInRange(index)) Main.projectile[index].CritChance = player.GetWeaponCrit(Item);
            return false;
        }
        internal bool LoadArrow(Player player, out int damage, out float knockback, out int ammo)
        {
            consuming = true;
            try { return player.PickAmmo(Item, out _, out _, out damage, out knockback, out ammo); }
            finally { consuming = false; }
        }
        internal static void SpawnS2Crystal(Player player, IEntitySource source, Vector2 aim, int damage, float knockback, int crit, int target)
        {
            int index = Projectile.NewProjectile(source, player.MountedCenter + aim * 48, aim * 26,
                ModContent.ProjectileType<TyphonAstralShot>(), damage, knockback, player.whoAmI, 2, target, 0);
            if (Main.projectile.IndexInRange(index)) Main.projectile[index].CritChance = crit;
        }
        private int ComputeFixedDamage(Player player)
        {
            StatModifier mod = player.GetDamage(Item.DamageType);
            ModifyWeaponDamage(player, ref mod);
            return (int)Math.Round(mod.ApplyTo(Item.damage));
        }

        private static void FindTwoTargets(Player player, Vector2 origin, out NPC? t1, out NPC? t2)
        {
            t1 = null;
            t2 = null;
            const float maxRange = 1200f;
            Vector2 aim = Main.MouseWorld;
            float best1 = float.MaxValue;
            float best2 = float.MaxValue;
            float maxRangeSq = maxRange * maxRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(player)) continue;
                if (Vector2.DistanceSquared(npc.Center, origin) > maxRangeSq) continue;
                float d = Vector2.DistanceSquared(npc.Center, aim);
                if (d < best1)
                {
                    best2 = best1; t2 = t1;
                    best1 = d;     t1 = npc;
                }
                else if (d < best2)
                {
                    best2 = d; t2 = npc;
                }
            }
        }

        public class TyphonState : ModPlayer
        {
            private int s2Delay, s2Damage, s2Crit, s2Ammo, s2Target;
            private float s2Knockback;
            private Vector2 s2Aim;
            private Item s2Item;
            internal void QueueS2Followup(Vector2 aim, int damage, float knockback, int crit, int ammo, int target)
            {
                s2Delay = S2FollowupDelay; s2Aim = aim; s2Damage = damage; s2Knockback = knockback;
                s2Crit = crit; s2Ammo = ammo; s2Target = target; s2Item = Player.HeldItem;
            }
            public override void UpdateDead() { s2Delay = 0; s2Item = null; }
            public int S1Stacks;
            public int S2ActivationCount;
            public int S2FirstHits;
            private bool _wasFirstS2Active;

            public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
            {
                if (Player.HeldItem.ModItem is not TyphonBow) return;
                var wp = Player.GetModPlayer<WeaponPlayer>();
                if (!wp.SkillActive) return;

                if (wp.Skill == 0 && target.life <= 0)
                {
                    if (S1Stacks < 30) S1Stacks++;
                    wp.SkillTimer = Math.Max(0, wp.SkillTimer - 60 * 3);
                }
                else if (wp.Skill == 1 && S2ActivationCount == 1 && S2FirstHits < 30)
                {
                    S2FirstHits++;
                }
            }

            public override void PostUpdate()
            {
                if (s2Delay > 0)
                {
                    if (!Player.active || Player.dead || Player.CCed || Player.noItems
                        || Player.HeldItem.ModItem is not TyphonBow || !ReferenceEquals(Player.HeldItem, s2Item))
                        s2Delay = 0;
                    else if (Player.whoAmI == Main.myPlayer && --s2Delay == 0)
                        SpawnS2Crystal(Player, Player.GetSource_ItemUse_WithPotentialAmmo(Player.HeldItem, s2Ammo),
                            s2Aim, s2Damage, s2Knockback, s2Crit, s2Target);
                }
                var wp = Player.GetModPlayer<WeaponPlayer>();
                bool nowActive = Player.HeldItem.ModItem is TyphonBow
                    && wp.Skill == 1 && wp.SkillActive && S2ActivationCount == 1;

                if (_wasFirstS2Active && !nowActive && S2FirstHits > 0 && wp.CurrentSkill != null)
                {
                    int gain = Math.Min(S2FirstHits, 30);
                    wp.SkillCharge = Math.Min(wp.SkillCharge + gain * wp.Div, wp.SkillChargeMax);
                    wp.SP          = Math.Min(wp.SP + gain, wp.CurrentSkill.CurrentLevelData.MaxSP);
                    S2FirstHits = 0;
                }
                _wasFirstS2Active = nowActive;

                if (Player.HeldItem.ModItem is not TyphonBow)
                {
                    S2ActivationCount = 0;
                    S1Stacks = 0;
                    S2FirstHits = 0;
                }
            }
        }
    }
}
