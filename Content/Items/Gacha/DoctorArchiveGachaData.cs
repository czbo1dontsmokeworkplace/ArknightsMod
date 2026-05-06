using System;
using System.Collections.Generic;
using System.Linq;

namespace ArknightsMod.Content.Items.Gacha
{
	public static class DoctorArchiveGachaData
	{
		public sealed record VanitySet(string SetKey, int Stars, string[] ItemKeys);

		public static readonly VanitySet[] Sets =
		[
			new VanitySet("Adnachiel", 3, ["AdnachielHead", "AdnachielBody", "AdnachielLegs"]),
			new VanitySet("Amiya", 5, ["AmiyaHead", "AmiyaBody", "AmiyaLegs"]),
			new VanitySet("Ansel", 3, ["AnselHead", "AnselBody", "AnselLegs"]),
			new VanitySet("Beagle", 3, ["BeagleHead", "BeagleBody", "BeagleLegs"]),
			new VanitySet("Cardigan", 3, ["CardiganHead", "CardiganBody", "CardiganLegs"]),
			new VanitySet("Catapult", 3, ["CatapultHead", "CatapultBody", "CatapultLegs"]),
			new VanitySet("Chen", 6, ["ChenHead", "ChenBody", "ChenLegs"]),
			new VanitySet("CivilightEterna", 6, ["CivilightEternaHead", "CivilightEternaBody", "CivilightEternaLegs"]),
			new VanitySet("Entelechia", 6, ["EntelechiaHead", "EntelechiaBody", "EntelechiaLegs"]),
			new VanitySet("Exusiai", 6, ["ExusiaiHead", "ExusiaiBody", "ExusiaiLegs"]),
			new VanitySet("ExusiaiAlter", 6, ["ExusiaiAlterHead", "ExusiaiAlterBody", "ExusiaiAlterLegs"]),
			new VanitySet("Endministrator", 6, ["EndministratorVanityBag"]),
			new VanitySet("Fang", 3, ["FangHead", "FangBody", "FangLegs"]),
			new VanitySet("Fartooth", 5, ["FartoothHead", "FartoothBody", "FartoothLegs"]),
			new VanitySet("Fiammetta", 5, ["FiammettaHead", "FiammettaBody", "FiammettaLegs"]),
			new VanitySet("Haze", 4, ["HazeHead", "HazeBody", "HazeLegs"]),
			new VanitySet("Hibiscus", 3, ["HibiscusHead", "HibiscusBody", "HibiscusLegs"]),
			new VanitySet("Indigo", 4, ["IndigoHead", "IndigoBody", "IndigoLegs"]),
			new VanitySet("Kaltsit", 5, ["KaltsitHead", "KaltsitBody", "KaltsitLegs"]),
			new VanitySet("Kroos", 3, ["KroosHead", "KroosBody", "KroosLegs"]),
			new VanitySet("LaPluma", 5, ["LaPlumaHead", "LaPlumaBody", "LaPlumaLegs"]),
			new VanitySet("Lappland", 5, ["LapplandHead", "LapplandBody", "LapplandLegs"]),
			new VanitySet("LastRite", 5, ["LastRiteHead", "LastRiteBody", "LastRiteLegs"]),
			new VanitySet("Lava", 3, ["LavaHead", "LavaBody", "LavaLegs"]),
			new VanitySet("Ling", 6, ["LingHead", "LingBody", "LingLegs"]),
			new VanitySet("Manticore", 5, ["ManticoreHead", "ManticoreBody", "ManticoreLegs"]),
			new VanitySet("Matoimaru", 4, ["MatoimaruHead", "MatoimaruBody", "MatoimaruLegs"]),
			new VanitySet("Melanite", 5, ["MelaniteHead", "MelaniteBody", "MelaniteLegs"]),
			new VanitySet("Melantha", 3, ["MelanthaHead", "MelanthaBody", "MelanthaLegs"]),
			new VanitySet("Midnight", 3, ["MidnightHead", "MidnightBody", "MidnightLegs"]),
			new VanitySet("Mizuki", 6, ["MizukiHead", "MizukiBody", "MizukiLegs"]),
			new VanitySet("Mlynar", 6, ["MlynarHead", "MlynarBody", "MlynarLegs"]),
			new VanitySet("Mortis", 5, ["MortisHead", "MortisBody", "MortisLegs"]),
			new VanitySet("Mostima", 6, ["MostimaHead", "MostimaBody", "MostimaLegs"]),
			new VanitySet("Nian", 6, ["NianVanityBag"]),
			new VanitySet("Mudrock", 6, ["MudrockHead", "MudrockBody", "MudrockGreaves"]),
			new VanitySet("Oblivionis", 5, ["OblivionisHead", "OblivionisBody", "OblivionisLegs"]),
			new VanitySet("Orchid", 3, ["OrchidHead", "OrchidBody", "OrchidLegs"]),
			new VanitySet("Plume", 3, ["PlumeHead", "PlumeBody", "PlumeLegs"]),
			new VanitySet("Popukar", 3, ["PopukarHead", "PopukarBody", "PopukarLegs"]),
			new VanitySet("Provence", 5, ["ProvenceHead", "ProvenceBody", "ProvenceLegs"]),
			new VanitySet("Raidian", 5, ["RaidianHead", "RaidianBody", "RaidianLegs"]),
			new VanitySet("Rosmontis", 6, ["RosmontisHead", "RosmontisBody", "RosmontisLegs"]),
			new VanitySet("Saria", 6, ["SariaHead", "SariaBody", "SariaLegs"]),
			new VanitySet("Skadi", 6, ["SkadiHead", "SkadiBody", "SkadiLegs"]),
			new VanitySet("Spot", 3, ["SpotHead", "SpotBody", "SpotLegs"]),
			new VanitySet("Steward", 3, ["StewardHead", "StewardBody", "StewardLegs"]),
			new VanitySet("Surtr", 6, ["SurtrHead", "SurtrBody", "SurtrLegs"]),
			new VanitySet("Texas", 5, ["TexasHead", "TexasBody", "TexasLegs"]),
			new VanitySet("Texalter", 6, ["TexalterHead", "TexalterBody", "TexalterLegs"]),
			new VanitySet("Typhon", 6, ["TyphonVanityBag"]),
			new VanitySet("Utage", 4, ["UtageHead", "UtageBody", "UtageLegs"]),
			new VanitySet("Ulpianus", 6, ["UlpianusVanityBag"]),
			new VanitySet("Vanilla", 3, ["VanillaHead", "VanillaBody", "VanillaLegs"]),
			new VanitySet("Vulcan", 5, ["VulcanHead", "VulcanBody", "VulcanLegs"]),
			new VanitySet("Warfarin", 5, ["WarfarinHead", "WarfarinBody", "WarfarinLegs"]),
			new VanitySet("W", 6, ["WHead", "WBody", "WLegs"]),
			new VanitySet("Wisadel", 6, ["WisadelHead", "WisadelBody", "WisadelLegs"]),
			new VanitySet("Yvonne", 6, ["YvonneHead", "YvonneBody", "YvonneLegs"]),
		];

		public static readonly IReadOnlyDictionary<string, int> ItemKeyToStars =
			Sets.SelectMany(s => s.ItemKeys.Select(k => (k, s.Stars)))
				.ToDictionary(x => x.k, x => x.Stars, StringComparer.Ordinal);

		public static VanitySet[] GetSetsByStars(int stars) => Sets.Where(s => s.Stars == stars).ToArray();
	}
}
