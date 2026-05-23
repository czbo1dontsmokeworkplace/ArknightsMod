import json
import re
import unicodedata
import random

# ---------- 加载映射表 ----------
with open("arknights_materials.json", "r", encoding="utf-8") as f:
    MATERIALS_MAP = json.load(f)

with open("arknights_classes.json", "r", encoding="utf-8") as f:
    CLASS_MAP = json.load(f)

# ---------- 构建T1材料列表（英文名） ----------
T1_MATERIALS = [info["name"] for info in MATERIALS_MAP.values() if info.get("tier") == 1]
# 固定随机种子，保证每次运行结果一致
random.seed(42)

# ---------- 字符映射表 ----------
CHAR_MAP = {
    'ł': 'l', 'Ł': 'L',
    'ø': 'o', 'Ø': 'O',
    'æ': 'ae', 'Æ': 'AE',
    'œ': 'oe', 'Œ': 'OE',
    'ß': 'ss',
    'ā': 'a', 'ă': 'a', 'ąć': 'ac', 'č': 'c', 'đ': 'd',
    'ē': 'e', 'ė': 'e', 'ę': 'e', 'ğ': 'g', 'ī': 'i',
    'ı': 'i', 'ł': 'l', 'ń': 'n', 'ň': 'n', 'ő': 'o',
    'ŕ': 'r', 'ř': 'r', 'ś': 's', 'ş': 's', 'š': 's',
    'ţ': 't', 'ť': 't', 'ū': 'u', 'ų': 'u', 'ů': 'u',
    'ý': 'y', 'ź': 'z', 'ż': 'z', 'ž': 'z',
    'А': 'A', 'Б': 'B', 'В': 'V', 'Г': 'G', 'Д': 'D',
    'Е': 'E', 'Ё': 'Yo', 'Ж': 'Zh', 'З': 'Z', 'И': 'I',
    'Й': 'Y', 'К': 'K', 'Л': 'L', 'М': 'M', 'Н': 'N',
    'О': 'O', 'П': 'P', 'Р': 'R', 'С': 'S', 'Т': 'T',
    'У': 'U', 'Ф': 'F', 'Х': 'Kh', 'Ц': 'Ts', 'Ч': 'Ch',
    'Ш': 'Sh', 'Щ': 'Shch', 'Ъ': '', 'Ы': 'Y', 'Ь': '',
    'Э': 'E', 'Ю': 'Yu', 'Я': 'Ya',
    'а': 'a', 'б': 'b', 'в': 'v', 'г': 'g', 'д': 'd',
    'е': 'e', 'ё': 'yo', 'ж': 'zh', 'з': 'z', 'и': 'i',
    'й': 'y', 'к': 'k', 'л': 'l', 'м': 'm', 'н': 'n',
    'о': 'o', 'п': 'p', 'р': 'r', 'с': 's', 'т': 't',
    'у': 'u', 'ф': 'f', 'х': 'kh', 'ц': 'ts', 'ч': 'ch',
    'ш': 'sh', 'щ': 'shch', 'ъ': '', 'ы': 'y', 'ь': '',
    'э': 'e', 'ю': 'yu', 'я': 'ya'
}

def sanitize_classname(raw_name: str) -> str:
    if not raw_name:
        return "Unknown"
    name = unicodedata.normalize('NFKD', raw_name)
    mapped_chars = []
    for ch in name:
        if ch in CHAR_MAP:
            mapped_chars.append(CHAR_MAP[ch])
        else:
            mapped_chars.append(ch)
    name = ''.join(mapped_chars)
    name = re.sub(r'[^a-zA-Z0-9_]', '', name)
    if not name:
        return "Unknown"
    if name[0].isdigit():
        name = 'C' + name
    return name[0].upper() + name[1:]

def convert_material_name(chinese_name: str) -> str:
    return MATERIALS_MAP.get(chinese_name, {}).get("name", chinese_name)

def convert_profession(chinese_prof: str) -> str:
    return CLASS_MAP.get(chinese_prof, chinese_prof)

def convert_items_in_data(obj):
    if isinstance(obj, dict):
        new = {}
        for k, v in obj.items():
            if k == "item" and isinstance(v, str):
                new[k] = convert_material_name(v)
            else:
                new[k] = convert_items_in_data(v)
        return new
    elif isinstance(obj, list):
        return [convert_items_in_data(item) for item in obj]
    else:
        return obj

def calc_armor_stats(hp_elite2: int, def_elite2: int) -> dict:
    hp_base = round(hp_elite2 * 0.2)
    def_base = round(def_elite2 * 0.1)
    return {
        "head_life": round(hp_base * 0.5),
        "body_life": round(hp_base * 0.25),
        "legs_life": round(hp_base * 0.25),
        "head_defense": 0,
        "body_defense": round(def_base * 0.75),
        "legs_defense": round(def_base * 0.25),
    }

def extract_materials_by_rarity(data: dict, rarity_original: int):
    elite = data.get("精英化材料", {})
    skill = data.get("技能升级材料", {})

    def last_n(lst, n):
        if not isinstance(lst, list):
            return []
        return lst[-n:] if len(lst) >= n else lst[:]

    if rarity_original == 2:
        head = last_n(skill.get("level_3", []), 1)
        body = last_n(skill.get("level_5", []), 1)
        legs = last_n(skill.get("level_4", []), 1)
        return head, body, legs

    elif rarity_original == 3:
        e1 = elite.get("elite_1", [])
        e2 = elite.get("elite_2", [])
        head = (last_n(e1, 1) + last_n(e2, 1))[:2]
        l5 = skill.get("level_5", [])
        l6 = skill.get("level_6", [])
        body = (last_n(l5, 1) + last_n(l6, 1))[:2]
        l4 = skill.get("level_4", [])
        l7 = skill.get("level_7", [])
        legs = (last_n(l4, 1) + last_n(l7, 1))[:2]
        return head, body, legs
    
    elif rarity_original == 4:
        e2 = elite.get("elite_2", [])
        head = last_n(e2, 2)
        s1 = skill.get("skill_1", {})
        l8_1 = s1.get("level_8", []) if isinstance(s1, dict) else []
        body = last_n(l8_1, 2)
        s2 = skill.get("skill_2", {})
        l8_2 = s2.get("level_8", []) if isinstance(s2, dict) else []
        legs = last_n(l8_2, 2)
        return head, body, legs
    
    elif rarity_original == 5:
        s1 = skill.get("skill_1", {})
        l10_1 = s1.get("level_10", []) if isinstance(s1, dict) else []
        head = last_n(l10_1, 2)
        s2 = skill.get("skill_2", {})
        l10_2 = s2.get("level_10", []) if isinstance(s2, dict) else []
        body = last_n(l10_2, 2)
        s3 = skill.get("skill_3", {})
        l10_3 = s3.get("level_10", []) if isinstance(s3, dict) else []
        legs = last_n(l10_3, 2)
        return head, body, legs
    else:
        print("该干员不支持自动生成配方，请手动添加")
        return [], [], []

def process_operator(data: dict) -> dict:
    result = data.copy()

    # 原始星级
    try:
        original_rarity = int(result["稀有度"])
    except (ValueError, TypeError):
        original_rarity = 0

    # 清洗英文名、职业
    if "英文名" in result and result["英文名"]:
        result["英文名"] = sanitize_classname(result["英文名"])
    if "职业" in result and result["职业"]:
        result["职业"] = convert_profession(result["职业"])

    # 转换所有材料名
    result = convert_items_in_data(result)

    # 提取三部件材料
    head_mats, body_mats, legs_mats = extract_materials_by_rarity(result, original_rarity)

    # 计算装甲属性
    armor = {}
    if "最大生命" in result and "最大防御" in result:
        try:
            hp = int(result["最大生命"])
            defense = int(result["最大防御"])
            armor = calc_armor_stats(hp, defense)
        except (ValueError, TypeError):
            pass

    # 最终输出：全部英文键名
    output = {
        "name_cn": result["中文名"],
        "name_en": result["英文名"],
        "rarity": original_rarity + 1,
        "class": result["职业"],
        "max_hp": int(result["最大生命"]),
        "max_def": int(result["最大防御"]),
        "head_materials": head_mats,
        "body_materials": body_mats,
        "legs_materials": legs_mats,
        "armor_stats": armor
    }
    return output