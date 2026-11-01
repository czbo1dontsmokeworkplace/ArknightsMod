"""Offline source/data regression checks. Requires Python and hjson; does not simulate Terraria."""
from pathlib import Path
import csv
import re
import subprocess
import hjson

ROOT = Path(__file__).resolve().parents[1]
checks = 0

def read(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")

def check(condition, message):
    global checks
    if not condition:
        raise AssertionError(message)
    checks += 1

catalog = read("Content/Items/Weapons/Defender/DefenderRouteCatalog.cs")
pattern = r'new\("(\w+)", "([^"]+)", "(\w+)", (\d+), \[(.*?)\], (\d+), (\d+)\)'
nodes = {}
for name, operator, branch, tier, parents, damage, interval in re.findall(pattern, catalog):
    check(name not in nodes, f"duplicate node: {name}")
    nodes[name] = dict(operator=operator, branch=branch, tier=int(tier),
        parents=re.findall(r'"(\w+)"', parents), damage=int(damage), interval=int(interval))
check(len(nodes) == 35, "expected all 35 Defender weapons")
for name, node in nodes.items():
    for parent in node["parents"]:
        check(parent in nodes, f"unknown parent for {name}")
        check(nodes[parent]["tier"] < node["tier"], f"non-progressing or cyclic edge: {parent} -> {name}")
check(nodes["AshlockLauncher"]["parents"] == ["FirewhistleLauncher"], "Ashlock must upgrade Firewhistle")
check(nodes["HornGrenadeLauncher"]["parents"] == ["AshlockLauncher"], "Horn must upgrade Ashlock")
fortress = [nodes[n] for n in ["FirewhistleLauncher", "AshlockLauncher", "HornGrenadeLauncher"]]
check([n["tier"] for n in fortress] == [2, 3, 4], "Fortress stages must differ")
check(all(a["damage"] / a["interval"] < b["damage"] / b["interval"] for a, b in zip(fortress, fortress[1:])),
    "Fortress base damage per second must rise at every stage")

rows = list(csv.DictReader(read("Assets/LevelDatas/SkillDatas.csv").splitlines()))
localized = {}
for language in ["zh-Hans", "en-US"]:
    combined = hjson.loads(read(f"Localization/{language}/Mods.ArknightsMod.Skills.hjson"))
    for category in ["Defender", "DefenderRoute"]:
        doc = hjson.loads(read(f"Localization/{category}/{language}_Mods.ArknightsMod.hjson"))
        combined.update(doc.get("Skills", {}))
        check(bool(doc.get("Items")), f"empty item translations: {category}/{language}")
    localized[language] = combined

skills = [r for r in rows if r["Item"] in nodes]
check(len(skills) == 70, "skill slots were lost or duplicated")
keys = set()
for row in skills:
    name, slot = row["Item"], row["Name"]
    check((name, row["Index"]) not in keys, f"duplicate skill slot: {name}/{slot}")
    keys.add((name, row["Index"]))
    check((ROOT / f"Common/UI/SkillIcons/{name}_{slot}.png").is_file(), f"missing skill icon: {name}/{slot}")
    data = list(csv.DictReader(read(f"Assets/LevelDatas/Skills/{name}_{slot}.csv").splitlines()))
    usable = [r for r in data if r["MaxSP"]]
    check(len(data) == 10 and bool(usable), f"missing rank data: {name}/{slot}")
    check(all(int(r["MaxSP"]) > 0 and float(r["ActiveTime"]) >= 0 and int(r["MaxStack"]) > 0 for r in usable),
        f"invalid rank values: {name}/{slot}")
    for language, doc in localized.items():
        check(name in doc and slot in doc[name] and bool(doc[name][slot].get("Desc")),
            f"missing skill translation: {language}/{name}/{slot}")

check(not any(r["Item"] == "NoirShield" for r in skills), "Noir Corne must remain a skill-free starter")
for name in ["NianWeapon", "MudrockHammer", "EunectesAxe", "HornGrenadeLauncher"]:
    check(sum(r["Item"] == name for r in skills) == 3, f"lost third skill: {name}")
for name in ["JessicaLiberatedGun", "HoshigumaBlazingBlade"]:
    check(next(r for r in skills if r["Item"] == name and r["Index"] == "0")["IsPermanent"] == "1",
        f"permanent mode not registered: {name}")

effects = read("Content/Projectiles/Defender/DefenderRouteEffects.cs")
check('"PhilaeBlade" => DefenderAreaKind.Necrosis' in effects, "Philae element mismatch")
check('"UnderflowBlade" or "SanbyBlade" => DefenderAreaKind.Corrosion' in effects, "Corrosion element mismatch")
check('"YuBlade" => DefenderAreaKind.Burn' in effects, "Yu element mismatch")
check('case "BasslineBlade": container.AddAfflictionValue<NecrosisImpairment>' not in effects, "old Bassline mismatch retained")
holdout = read("Content/Projectiles/Defender/DefenderHoldout.cs")
for token in ["DrawSwingTrail", "Color.Purple", '"BeagleWeapon" => Color.Gray', "CutTiles()", "SyncSkill()", "WantsRaised(p)", "RemoteUntil"]:
    check(token in holdout, f"missing held attack behavior: {token}")
field = read("Content/Projectiles/Defender/DefenderSkillArea.cs")
for token in ["Main.netMode != NetmodeID.MultiplayerClient", "!npc.boss", "npc.realLife < 0", "sourceItemType", "period", "hits.Clear()", "other.identity < Projectile.identity"]:
    check(token in field, f"missing field guard: {token}")
burst = read("Content/Projectiles/Defender/Horn/HornBurst.cs")
check("Vector2.DistanceSquared" in burst and "Projectile.ai[0] * Projectile.ai[0]" in burst and "HornCombat.Clear" in burst,
    "blast radius / line-of-sight mismatch")
check("DamageClass.Magic : DamageClass.Ranged" in burst, "Horn Arts blast must have a magic damage class")
for path in ["Content/Projectiles/Defender/DefenderSkillVisuals.cs", "Content/Projectiles/Defender/DefenderArtsShard.cs"]:
    check("Main.dedServ" in read(path), f"missing server visual guard: {path}")

# Existing textures, weapons and projectiles must not be deleted during this repair.
deleted = subprocess.check_output(["git", "diff", "--diff-filter=D", "--name-only", "--", "Content/Items/Weapons/Defender", "Content/Projectiles/Defender"], cwd=ROOT, text=True)
check(not deleted.strip(), "existing Defender files were deleted")
print(f"PASS: {checks} source/data assertions; 35 weapons, {len(skills)} skill slots.")
print("This does not validate live rendering, combat balance, collisions or multiplayer behavior.")
