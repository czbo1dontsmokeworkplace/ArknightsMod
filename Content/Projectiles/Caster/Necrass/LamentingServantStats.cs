using System;
using System.IO;

namespace ArknightsMod.Content.Projectiles.Caster.Necrass
{
	// 与弹幕AI分离，生命、复利强化和同步共用这一份状态。
	internal sealed class LamentingServantStats
	{
		public const int MaxStacks = 6;
		public int Life { get; private set; } = 1000;
		public int LifeMax { get; private set; } = 1000;
		public int Attack { get; private set; } = 30;
		public int Defense { get; private set; } = 25;
		public int Stacks { get; private set; }

		public void ReinforceAndHeal() {
			if (Life <= 0)
				return;

			if (Stacks < MaxStacks) {
				Attack = (int)Math.Round(Attack * 1.8d, MidpointRounding.AwayFromZero);
				Defense = (int)Math.Round(Defense * 1.4d, MidpointRounding.AwayFromZero);
				LifeMax = (int)Math.Round(LifeMax * 1.8d, MidpointRounding.AwayFromZero);
				Stacks++;
			}

			// 增加上限本身不补血；随后按新的上限治疗20%，满层后仍可治疗。
			Life = Math.Min(LifeMax, Life + LifeMax / 5);
		}

		public int TakeDamage(int rawDamage) {
			if (rawDamage <= 0 || Life <= 0)
				return 0;
			int damage = Math.Max(1, rawDamage - Defense);
			Life = Math.Max(0, Life - damage);
			return damage;
		}

		public void Write(BinaryWriter writer) {
			writer.Write(Life);
			writer.Write(LifeMax);
			writer.Write(Attack);
			writer.Write(Defense);
			writer.Write(Stacks);
		}

		public void Read(BinaryReader reader) {
			Life = reader.ReadInt32();
			LifeMax = reader.ReadInt32();
			Attack = reader.ReadInt32();
			Defense = reader.ReadInt32();
			Stacks = reader.ReadInt32();
		}
	}
}
