$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$checkRoot = Join-Path $env:TEMP ('arknights-cannot-check-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $checkRoot | Out-Null
$ledger = [System.Security.SecurityElement]::Escape((Join-Path $repoRoot 'Systems/CannotStockLedger.cs'))
$math = [System.Security.SecurityElement]::Escape((Join-Path $repoRoot 'Content/NPCs/Friendly/CannotSupportMath.cs'))
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$ledger" Link="CannotStockLedger.cs" />
    <Compile Include="$math" Link="CannotSupportMath.cs" />
  </ItemGroup>
</Project>
"@
Set-Content -LiteralPath (Join-Path $checkRoot 'Checks.csproj') -Value $project -Encoding utf8
$program = @'
using ArknightsMod.Systems;
using ArknightsMod.Content.NPCs.Friendly;

int assertions = 0;
void Check(bool value, string message) {
    assertions++;
    if (!value) throw new Exception(message);
}
int seed = 0;
int Pick(int tier, HashSet<int> used, int previous) {
    int value;
    do { value = tier * 1000 + ++seed; } while (used.Contains(value) || previous == value);
    return value;
}
var ledger = new CannotStockLedger();
Check(!ledger.Unlocked, "A fresh world must not unlock Cannot");
ledger.RecordVictory("Eye", 1, Pick);
Check(ledger.Slots.Count == 2 && ledger.Slots.All(s => s.Tier == 1), "First boss must grant exactly two price-2 slots");
int[] old = ledger.Slots.Select(s => s.ItemType).ToArray();
ledger.RecordVictory("Eye", 5, Pick);
Check(ledger.Slots.Count == 2, "Repeating first boss must never add slots");
Check(ledger.Slots.All(s => s.Tier == 1), "Endgame rematch must retain first boss prices");
Check(ledger.Slots.Select(s => s.ItemType).Zip(old).All(p => p.First != p.Second), "Both baseline slots must reroll");
ledger.RecordVictory("Evil", 1, Pick);
Check(ledger.Slots.Count == 3 && ledger.Slots.Last().Tier == 2, "Next early boss must grant one price-4 slot");
ledger.RecordVictory("Skeletron", 3, Pick);
ledger.RecordVictory("Plantera", 4, Pick);
ledger.RecordVictory("Fishron", 5, Pick);
Check(ledger.Slots.Count == 6 && ledger.HighestTier == 5, "Milestone bosses must unlock the inclusive next tier");
Check(ledger.Slots.Select(s => CannotStockLedger.Prices[s.Tier]).SequenceEqual(new[] {2, 2, 4, 8, 12, 16}), "Price progression must match the requested five levels");
var unrelated = ledger.Slots.Where(s => s.BossKey != "Evil").Select(s => (s.BossKey, s.Tier, s.ItemType)).ToArray();
int previousEvil = ledger.Slots.Single(s => s.BossKey == "Evil").ItemType;
ledger.RecordVictory("Evil", 5, Pick);
Check(ledger.Slots.Count == 6 && ledger.Slots.Single(s => s.BossKey == "Evil").Tier == 2, "A late rematch must not upgrade or add its slot");
Check(ledger.Slots.Single(s => s.BossKey == "Evil").ItemType != previousEvil, "Rematch must refresh that boss's item");
Check(unrelated.SequenceEqual(ledger.Slots.Where(s => s.BossKey != "Evil").Select(s => (s.BossKey, s.Tier, s.ItemType))), "Rematch must leave all unrelated merchandise untouched");
ledger.RecordVictory("AnotherMod/Boss", 4, Pick);
Check(ledger.Slots.Count == 7 && ledger.Slots.Last().Tier == 4, "Modded bosses must also own a slot");
for (int i = 0; i < 100; i++) ledger.RecordVictory("Skeletron", 5, Pick);
Check(ledger.Slots.Count == 7, "Repeated farming must not grow inventory");
foreach (int life in new[] {60, 90, 188, 260, 350, 1600, 1800, 2250, int.MaxValue}) {
    long total = Enumerable.Range(1, 10).Sum(second => (long)CannotSupportMath.HealForSecond(life, second));
    Check(total == life * 40L / 100, $"Ten 4% pulses must total exactly 40% for {life} HP");
}
foreach (int life in new[] {1, 2, 3, 10, 2250}) {
    foreach (int regen in new[] {-120, -240, -12000}) {
        int counter = CannotSupportMath.ClampUndyingRegen(-12000, regen, life);
        long available = -(long)counter - regen;
        Check(available / 120 <= life - 1, "DOT batches must never consume the reserved final HP");
    }
}
Console.WriteLine($"PASS: {assertions} assertions (boss stock ownership, rematches, all five prices, fractional healing, lethal DOT)");

'@
Set-Content -LiteralPath (Join-Path $checkRoot 'Program.cs') -Value $program -Encoding utf8
dotnet run --project (Join-Path $checkRoot 'Checks.csproj') --nologo
if ($LASTEXITCODE -ne 0) { throw 'Cannot encounter rule checks failed.' }
