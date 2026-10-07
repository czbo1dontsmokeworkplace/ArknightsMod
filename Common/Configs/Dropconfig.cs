using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Terraria.ModLoader.Config;


public class Dropconfig : ModConfig
{
	public override ConfigScope Mode => ConfigScope.ServerSide;

	[DefaultValue(12.5f)]
	[Range(0f, 100f)]
	[Increment(0.01f)]
	public float DropOriginiumSlugPercent = 12.5f;

	[DefaultValue(100f)]
	[Range(0f, 100f)]
	[Increment(0.01f)]
	public float DropLSPercent = 100f;

	[DefaultValue(14.29f)]
	[Range(0f, 100f)]
	[Increment(0.01f)]
	public float DropOriginiumSlugAlphaPercent = 14.29f;

	[DefaultValue(16.67f)]
	[Range(0f, 100f)]
	[Increment(0.01f)]
	public float DropOriginiumSlugBetaPercent = 16.67f;

	[DefaultValue(12.5f)]
	[Range(0f, 100f)]
	[Increment(0.01f)]
	public float DropSoldier1Percent = 12.5f;

	[DefaultValue(12.5f)]
	[Range(0f, 100f)]
	[Increment(0.01f)]
	public float DropSoldier2Percent = 12.5f;

	[DefaultValue(12.5f)]
	[Range(0f, 100f)]
	[Increment(0.01f)]
	public float DropAcidOgSlug1Percent = 12.5f;

	[DefaultValue(12.5f)]
	[Range(0f, 100f)]
	[Increment(0.01f)]
	public float DropAcidOgSlug2Percent = 12.5f;

	[DefaultValue(100f)]
	[Range(0f, 100f)]
	[Increment(0.01f)]
	public float DropDrone1Percent = 100f;

	[DefaultValue(10f)]
	[Range(0f, 100f)]
	[Increment(0.01f)]
	public float DropDrone2Percent = 10f;

	// 仅接收旧 JSON 字段；没有 getter，因此保存时只写新的百分比字段。
	private int? legacyOriginiumSlug, legacyLS, legacyOriginiumSlugAlpha, legacyOriginiumSlugBeta;
	private int? legacySoldier1, legacySoldier2, legacyAcidOgSlug1, legacyAcidOgSlug2, legacyDrone1, legacyDrone2;

	[JsonProperty("DropOriginiumSlug")]
	private int LegacyOriginiumSlug { set => legacyOriginiumSlug = value; }
	[JsonProperty("DropLS")]
	private int LegacyLS { set => legacyLS = value; }
	[JsonProperty("DropOriginiumSlugAlpha")]
	private int LegacyOriginiumSlugAlpha { set => legacyOriginiumSlugAlpha = value; }
	[JsonProperty("DropOriginiumSlugBeta")]
	private int LegacyOriginiumSlugBeta { set => legacyOriginiumSlugBeta = value; }
	[JsonProperty("DropSoldier1")]
	private int LegacySoldier1 { set => legacySoldier1 = value; }
	[JsonProperty("DropSoldier2")]
	private int LegacySoldier2 { set => legacySoldier2 = value; }
	[JsonProperty("DropAcidOgSlug1")]
	private int LegacyAcidOgSlug1 { set => legacyAcidOgSlug1 = value; }
	[JsonProperty("DropAcidOgSlug2")]
	private int LegacyAcidOgSlug2 { set => legacyAcidOgSlug2 = value; }
	[JsonProperty("DropDrone1")]
	private int LegacyDrone1 { set => legacyDrone1 = value; }
	[JsonProperty("DropDrone2")]
	private int LegacyDrone2 { set => legacyDrone2 = value; }

	[OnDeserialized]
	private void MigrateLegacyRates(StreamingContext context) {
		ConvertLegacy(ref DropOriginiumSlugPercent, ref legacyOriginiumSlug);
		ConvertLegacy(ref DropLSPercent, ref legacyLS);
		ConvertLegacy(ref DropOriginiumSlugAlphaPercent, ref legacyOriginiumSlugAlpha);
		ConvertLegacy(ref DropOriginiumSlugBetaPercent, ref legacyOriginiumSlugBeta);
		ConvertLegacy(ref DropSoldier1Percent, ref legacySoldier1);
		ConvertLegacy(ref DropSoldier2Percent, ref legacySoldier2);
		ConvertLegacy(ref DropAcidOgSlug1Percent, ref legacyAcidOgSlug1);
		ConvertLegacy(ref DropAcidOgSlug2Percent, ref legacyAcidOgSlug2);
		ConvertLegacy(ref DropDrone1Percent, ref legacyDrone1);
		ConvertLegacy(ref DropDrone2Percent, ref legacyDrone2);
	}

	private static void ConvertLegacy(ref float percentage, ref int? denominator) {
		if (denominator > 0)
			percentage = 100f / denominator.Value;
		denominator = null;
	}
}
