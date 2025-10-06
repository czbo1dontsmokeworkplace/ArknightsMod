using System;
using Terraria.ModLoader;
using System.Numerics;
using static System.Net.Mime.MediaTypeNames;
using Terraria.Utilities.Terraria.Utilities;

namespace Terraria;
public class NPCa
{
	public enum DamageCategory : byte
	{
		Physical,
		Arts,
		True,
		Elemental
	}
	public partial struct HitModifiers
	{
		public DamageClass DamageType { get; init; } = DamageClass.Default;
		public int HitDirection { get; init; } = default;
		public bool SuperArmor { get; init; } = false;
		public StatModifier SourceDamage = new(); //original damage

		public AddableFloat ScalingBonusDamage = new(); //multiplicative
		public AddableFloat FlatBonusDamage = new(); //additive

		public StatModifier CritDamage = new(2f, 1f); //crit multiplicative
		public StatModifier NonCritDamage = new(); 
		public MultipliableFloat DamageVariationScale = new(); //variation

		public StatModifier Defense = new(); //DEF
		public AddableFloat ArmorPenetration = new(); //DEF penetrate per hitpoint
		public AddableFloat ScalingArmorPenetration = new(); //DEF penetrate as percentage
		public MultipliableFloat DefenseEffectivenew = MultipliableFloat.One * 0.5f;

		public StatModifier ArtsResistance = new(); //RES
		public AddableFloat ArtsIgnorance = new(); //RES ignorance per 1%
		public AddableFloat ScalingArtsIgnorance = new(); //RES ignorance as percentage

		public StatModifier ElementalResistance = new(); //E-RES

		public MultipliableFloat TargetDamageMultiplier = new(); //fragility
		public StatModifier DamageImmuneRate = new(); //damage immune rate
		public StatModifier FinalDamage = new();

		private int _damageLimit = int.MaxValue;
		public void SetMaxDamage(int limit) => _damageLimit = Math.Min(_damageLimit, Math.Max(limit, 1));

		private bool? _critOverride = default;
		public void DisableCrit() => _critOverride = false;
		public void SetCrit() => _critOverride ??= true;

		public StatModifier Knockback = new();
		private bool _knockbackDisabled = false;
		public void DisableKnockack() => _knockbackDisabled = true;

		public int? HitDirectionOverride { private get; set; } = default;

		private bool _instantKill = default;
		public void SetInstantKill() => _instantKill = true;

		private bool _combatTextHidden = default;
		public void HideCombatText() => _combatTextHidden = true;

		public delegate void HitInfoModifier(ref HitInfo Info);
		public event HitInfoModifier ModifyHitInfo = null;
		public HitModifiers() { }

		public readonly int GetDamage(float baseDamage, bool crit, bool damageVariation = false, float luck = 0f) {
			return GetDamageInternal(DamageCategory.Physical, baseDamage, crit, damageVariation, luck);
		}
		public readonly int GetDamage(DamageCategory category, float baseDamage, bool crit, bool damageVariation = false, float luck = 0f) {
			return GetDamageInternal(category, baseDamage, crit, damageVariation, luck);
		}
		public readonly struct DamageParams { //64 byte
			public readonly float BaseDamage;
			public readonly Vector2 BonusModifier; //vectorized bonus

			public readonly float DefenseEffective;
			public readonly Vector2 ArmorPenModifier; //vectorized pen

			public readonly float ArtsResistance;
			public readonly Vector2 ArtsIgnoreModifier; //vectorized ignore

			public readonly float ElementalResistance;

			public readonly float FinalMultiplier; //target multiplicate * (1 - immune rate)
			public readonly float Luck;
			public readonly DamageCategory Category;

			public DamageParams(float baseDamage,HitModifiers modifiers,DamageCategory category, float luck) {
				BaseDamage = baseDamage;
				BonusModifier = new Vector2 (
					modifiers.FlatBonusDamage.Value, modifiers.ScalingBonusDamage.Value
				);
				DefenseEffective = modifiers.Defense.ApplyTo(0) * modifiers.DefenseEffectivenew.Value;
				ArmorPenModifier = new Vector2(
					modifiers.ArmorPenetration.Value, modifiers.ScalingArmorPenetration.Value
				);
				ArtsResistance = modifiers.ArtsResistance.ApplyTo(0);
				ArtsIgnoreModifier = new Vector2(
					modifiers.ArtsIgnorance.Value, modifiers.ScalingArtsIgnorance.Value
				);
				ElementalResistance = modifiers.ElementalResistance.ApplyTo(0);
				FinalMultiplier = modifiers.TargetDamageMultiplier.Value * modifiers.DamageImmuneRate.ApplyTo(0);
				Luck = luck;
				Category = category;
			}
		}

		private readonly int GetDamageInternal(DamageCategory category,float baseDamage, bool crit, bool damageVariation, float luck) {
			crit = _critOverride ?? crit;
			if (SuperArmor) {
				float dmg = 1;
				if (crit)
					dmg *= CritDamage.Additive * CritDamage.Multiplicative;
				return Math.Clamp((int)dmg, 1, Math.Min(_damageLimit, 4));
			}
			var p = new DamageParams(baseDamage, this, category, luck);

			baseDamage = p.BaseDamage + p.BonusModifier.X + baseDamage * p.BonusModifier.Y;

			if (damageVariation) {
				int variationPercent = Utils.Clamp((int)Math.Round(Main.DefaultDamageVariationPercent * DamageVariationScale.Value), 0, 100);
				baseDamage = Main.DamageVar(baseDamage, variationPercent, luck);
			}

			baseDamage = category switch {
				DamageCategory.Physical => CalculatePhysicalDamage(p,baseDamage),
				DamageCategory.Arts => CalculateArtsDamage(p, baseDamage),
				DamageCategory.Elemental => CalculateElementalDamage(p, baseDamage),
				DamageCategory.True => baseDamage
			};

			baseDamage *= p.FinalMultiplier;
			return (int)(crit ? CritDamage : NonCritDamage).ApplyTo(baseDamage);
		}

		private readonly float CalculatePhysicalDamage(in DamageParams p, float baseDamage) {

			float defenseEffective = p.DefenseEffective;
			float finalArmorPenetration = defenseEffective * Math.Clamp(p.ArmorPenModifier.Y, 0, 1) + p.ArmorPenModifier.X;
			defenseEffective = MathF.Max(defenseEffective - finalArmorPenetration, 0);
			baseDamage = baseDamage - defenseEffective;
			return MathF.Max(baseDamage, 1);
		}

		private readonly float CalculateArtsDamage(in DamageParams p, float baseDamage) {
			float artsRes = MathF.Max(p.ArtsResistance - p.ArtsIgnoreModifier.X, 0);
			baseDamage *= (1f - artsRes* (1f - p.ArtsIgnoreModifier.Y)) ;
			return MathF.Max(baseDamage, 1);
		}

		private readonly float CalculateElementalDamage(in DamageParams p, float baseDamage) {
			baseDamage *= (1f - p.ElementalResistance);
			return MathF.Max(baseDamage, 1);
		}

		public readonly float GetKnockback(float baseKnockback) => _knockbackDisabled ? 0 : Math.Max(Knockback.ApplyTo(baseKnockback), 0);

		public HitInfo ToHitInfo( float baseDamage, bool crit, float baseKnockback, bool damageVariation = false, float luck = 0f, DamageCategory category = DamageCategory.Physical) {
			var hitInfo = new HitInfo() {
				DamageType = DamageType ?? DamageClass.Default,
				SourceDamage = Math.Max((int)SourceDamage.ApplyTo(baseDamage), 1),
				Damage = _instantKill ? 1 : GetDamage(category, baseDamage, crit, damageVariation, luck),
				Crit = _critOverride ?? crit,
				Knockback = GetKnockback(baseKnockback),
				HitDirection = HitDirectionOverride ?? HitDirection,
				InstantKill = _instantKill,
				HideCombatText = _combatTextHidden
			};

			ModifyHitInfo?.Invoke(ref hitInfo);
			ModifyHitInfo = null;
			return hitInfo;
		}
	}
	public struct HitInfo {
		public DamageClass DamageType = DamageClass.Default;

		private int _sourceDamage = 1;
		public int SourceDamage {
			readonly get => _sourceDamage;
			set => _sourceDamage = Math.Max(value, 1);
		}
		private int _damage = 1;
		public int Damage {
			readonly get => _damage;
			set => _damage = Math.Max(value, 1);
		}
		public bool Crit = false;

		public int HitDirection = 0;

		public float Knockback = 0;

		public bool InstantKill = false;

		public bool HideCombatText = false;

		public HitInfo() { }
	}

}