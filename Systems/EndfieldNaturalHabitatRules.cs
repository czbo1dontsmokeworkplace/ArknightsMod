namespace ArknightsMod.Systems;

// 纯生境规则：世界生成、旧世界生长、/plant 均使用同一张概率表。
public static class EndfieldNaturalHabitatRules
{
    public enum Ground { Other, Grass, CorruptGrass, CrimsonGrass, HallowedGrass, JungleGrass, Stone, Sand }
    public enum Depth { Space, Surface, Underground, Caverns, Mantle }

    public const double NormalChance = 0.003;
    public const double LowChance = NormalChance / 2;
    public const double PreferredChance = NormalChance * 2;

    public static double SpawnChance(int resourceIndex, Ground ground, Depth depth, bool inland)
    {
        if (resourceIndex is < 0 or >= 18)
            return 0;

        if (resourceIndex < 3)
            return ground == Ground.Grass && depth == Depth.Surface && inland ? NormalChance : 0;

        if (resourceIndex < 6)
        {
            bool preferred = resourceIndex switch
            {
                3 => ground is Ground.CorruptGrass or Ground.CrimsonGrass,
                4 => ground == Ground.HallowedGrass,
                5 => ground == Ground.Grass && depth == Depth.Space,
                _ => false
            };
            if (preferred)
                return PreferredChance;
            return ground == Ground.Grass ? LowChance : 0;
        }

        if (resourceIndex < 12)
            return ground is Ground.Grass or Ground.JungleGrass ? NormalChance : 0;

        if (resourceIndex == 15 && ground == Ground.Stone && depth == Depth.Mantle
            || resourceIndex == 16 && ground == Ground.JungleGrass
            || resourceIndex == 17 && ground == Ground.Sand && inland)
            return PreferredChance;

        if (ground == Ground.Stone && depth is Depth.Underground or Depth.Caverns)
            return NormalChance;
        return ground == Ground.Grass && depth == Depth.Surface ? LowChance : 0;
    }
}
