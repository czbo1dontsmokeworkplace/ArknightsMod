#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
PRTS 干员数据一键处理脚本（支持批量），默认不保存中间文件
流程: fetch(获取干员原始数据) → process(处理成模组需要数据) → armor(根据处理的数据最终生成代码)
用法:
    python main.py                              # 交互式输入，默认 process 步骤
    python main.py 干员名1,干员名2              # 命令行参数逗号分割
    python main.py --file operators.txt        # 从文件读取干员名字，换行分割
    python main.py 玛恩纳 --output result.json # 单干员输出到文件（仅 process 步骤）
    python main.py 玛恩纳 --step fetch         # 执行到 保存原始数据
    python main.py 玛恩纳 --step armor         # 执行到 生成盔甲代码
    python main.py 玛恩纳 --step armor --save-intermediate  # 生成盔甲代码并保存原始+处理后数据
"""

import re
import json
import argparse
import sys
import os
import requests
from pathlib import Path

import post_process
import generate_armor

# ---------- 原始数据获取 ----------
def parse_material_consumption(text):
    """解析 {{材料消耗|名称|数量}}，返回 [{"item": 名称, "amount": 数量}, ...]"""
    pattern = r'\{\{材料消耗\|([^|]+)\|([^}]+)\}\}'
    matches = re.findall(pattern, text)
    result = []
    for name, amount_str in matches:
        amount_str = amount_str.strip()
        if amount_str.endswith('w'):
            try:
                num = int(float(amount_str[:-1]) * 10000)
            except:
                num = 0
        else:
            try:
                num = int(amount_str)
            except:
                num = 0
        result.append({"item": name, "amount": num})
    return result

def extract_template_balanced(wikitext, template_name):
    """提取顶层模板 {{template_name ... }} 的内部内容"""
    start_pattern = r'\{\{' + re.escape(template_name)
    match = re.search(start_pattern, wikitext)
    if not match:
        return None
    start_pos = match.start()
    i = start_pos + 2
    balance = 1
    while i < len(wikitext) and balance > 0:
        if wikitext[i:i+2] == '{{':
            balance += 1
            i += 2
        elif wikitext[i:i+2] == '}}':
            balance -= 1
            i += 2
        else:
            i += 1
    if balance != 0:
        return None
    return wikitext[start_pos+2:i-2]

def fetch_operator_wikitext(operator_name):
    """从 PRTS API 获取干员页面的 wikitext"""
    url = "https://prts.wiki/api.php"
    params = {
        "action": "query",
        "prop": "revisions",
        "titles": operator_name,
        "rvprop": "content",
        "format": "json"
    }
    resp = requests.get(url, params=params)
    resp.raise_for_status()
    data = resp.json()
    pages = data["query"]["pages"]
    if "-1" in pages:
        raise ValueError(f"干员 {operator_name} 不存在")
    page = next(iter(pages.values()))
    return page["revisions"][0]["*"]

def parse_operator_raw(wikitext, cn_name):
    """从 wikitext 解析出原始数据字典"""
    # 基础信息
    en_name = re.search(r'\|\s*干员外文名\s*=\s*([^\n|]+)', wikitext)
    en_name = en_name.group(1).strip() if en_name else None
    rarity = re.search(r'\|\s*稀有度\s*=\s*([^\n|]+)', wikitext)
    rarity = rarity.group(1).strip() if rarity else None
    profession = re.search(r'\|\s*职业\s*=\s*([^\n|]+)', wikitext)
    profession = profession.group(1).strip() if profession else None

    # 最大生命 & 最大防御
    prop_inner = extract_template_balanced(wikitext, "属性")
    max_hp = 0
    max_def = 0
    if prop_inner:
        hp_pattern = r'\|\s*精\d+_满级_生命上限\s*=\s*(\d+)'
        def_pattern = r'\|\s*精\d+_满级_防御\s*=\s*(\d+)'
        hps = [int(x) for x in re.findall(hp_pattern, prop_inner)]
        defs = [int(x) for x in re.findall(def_pattern, prop_inner)]
        if hps:
            max_hp = max(hps)
        if defs:
            max_def = max(defs)
    if max_hp == 0:
        all_hp = re.findall(r'\|\s*[^\n]*生命上限[^\n]*=\s*(\d+)', wikitext)
        if all_hp:
            max_hp = max(int(x) for x in all_hp)
    if max_def == 0:
        all_def = re.findall(r'\|\s*[^\n]*防御[^\n]*=\s*(\d+)', wikitext)
        if all_def:
            max_def = max(int(x) for x in all_def)

    # 精英化材料
    elite_inner = extract_template_balanced(wikitext, "精英化材料")
    elite_materials = {}
    if elite_inner:
        elite1_match = re.search(r'\|\s*精1\s*=\s*([^\n]+?)(?=\n\||$)', elite_inner, re.DOTALL)
        elite2_match = re.search(r'\|\s*精2\s*=\s*([^\n]+?)(?=\n\||$)', elite_inner, re.DOTALL)
        if elite1_match:
            elite_materials["elite_1"] = parse_material_consumption(elite1_match.group(1))
        if elite2_match:
            elite_materials["elite_2"] = parse_material_consumption(elite2_match.group(1))

    # 技能升级材料
    skill_inner = extract_template_balanced(wikitext, "技能升级材料")
    skill_materials = {}
    if skill_inner:
        for level in range(2, 8):
            pattern = rf'\|\s*{level}\s*=\s*([^\n]+?)(?=\n\||$)'
            match = re.search(pattern, skill_inner, re.DOTALL)
            if match:
                skill_materials[f"level_{level}"] = parse_material_consumption(match.group(1))
        skill_keys = {
            "skill_1": ["一8", "一9", "一10"],
            "skill_2": ["二8", "二9", "二10"],
            "skill_3": ["三8", "三9", "三10"]
        }
        for skill_name, keys in skill_keys.items():
            spec_mats = {}
            for i, key in enumerate(keys, start=8):
                pattern = rf'\|\s*{re.escape(key)}\s*=\s*([^\n]+?)(?=\n\||$)'
                match = re.search(pattern, skill_inner, re.DOTALL)
                if match:
                    spec_mats[f"level_{i}"] = parse_material_consumption(match.group(1))
            if spec_mats:
                skill_materials[skill_name] = spec_mats

    return {
        "中文名": cn_name,
        "英文名": en_name,
        "稀有度": rarity,
        "职业": profession,
        "最大生命": max_hp,
        "最大防御": max_def,
        "精英化材料": elite_materials,
        "技能升级材料": skill_materials
    }

# ---------- 保存原始 JSON ----------
def save_raw_data(raw_data, operator_name):
    filename = f"原始_{operator_name}.json"
    with open(filename, "w", encoding="utf-8") as f:
        json.dump(raw_data, f, ensure_ascii=False, indent=2)
    print(f"  原始数据已保存至: {filename}")

# ---------- 保存处理后数据 ----------
def save_final_data(final_data, output_path):
    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(final_data, f, ensure_ascii=False, indent=2)
    print(f"  最终结果已保存至: {output_path}")

# ---------- 处理单个干员的不同步骤 ----------
def process_fetch(operator_name):
    """仅抓取并保存原始 JSON"""
    print(f"正在抓取: {operator_name}")
    wikitext = fetch_operator_wikitext(operator_name)
    raw_data = parse_operator_raw(wikitext, operator_name)
    save_raw_data(raw_data, operator_name)
    return None  # 无最终数据

def process_process(operator_name, save_intermediate, output_file=None, quiet=False):
    """生成处理后数据，可选保存原始 JSON"""
    print(f"正在处理: {operator_name}")
    wikitext = fetch_operator_wikitext(operator_name)
    raw_data = parse_operator_raw(wikitext, operator_name)

    if save_intermediate:
        save_raw_data(raw_data, operator_name)

    final_data = post_process.process_operator(raw_data)

    if output_file:
        save_final_data(final_data, output_file)
    else:
        # 自动生成默认文件名
        default_out = f"{operator_name}_最终.json"
        save_final_data(final_data, default_out)

    if not quiet:
        print(json.dumps(final_data, ensure_ascii=False, indent=2))
    return final_data

def process_armor(operator_name, save_intermediate, template_paths, armor_output_dir, quiet=False):
    """生成盔甲代码，可选保存原始+处理后数据"""
    print(f"正在为 {operator_name} 生成盔甲代码...")
    wikitext = fetch_operator_wikitext(operator_name)
    raw_data = parse_operator_raw(wikitext, operator_name)

    if save_intermediate:
        save_raw_data(raw_data, operator_name)

    final_data = post_process.process_operator(raw_data)

    if save_intermediate:
        # 保存处理后数据 以便调试
        final_json_path = f"{operator_name}_最终.json"
        save_final_data(final_data, final_json_path)

    # 生成盔甲代码
    generate_armor.generate_armor_from_data(final_data, template_paths, armor_output_dir)

    if not quiet:
        print(json.dumps(final_data, ensure_ascii=False, indent=2))
    return final_data

# ---------- 解析干员列表 ----------
def parse_operator_names_from_args(args):
    """根据命令行参数返回干员名列表"""
    operators = []
    if args.file:
        with open(args.file, "r", encoding="utf-8") as f:
            lines = f.readlines()
            for line in lines:
                line = line.strip()
                if line:
                    operators.append(line)
    elif args.operator_names:
        for part in args.operator_names:
            names = part.split(',')
            for name in names:
                name = name.strip()
                if name:
                    operators.append(name)
    else:
        inp = input("请输入干员名（多个用逗号分隔）：").strip()
        if inp:
            operators = [n.strip() for n in inp.split(',') if n.strip()]
    return operators

# ---------- 主函数 ----------
def main():
    parser = argparse.ArgumentParser(description="从PRTS获取干员数据并处理（支持批量及盔甲生成）")
    parser.add_argument("operator_names", nargs="*", help="干员名，支持逗号分割多个")
    parser.add_argument("--file", "-f", help="包含干员列表的文件（每行一个）")
    parser.add_argument("--save-intermediate", action="store_true", help="保存中间数据")
    parser.add_argument("--output", "-o", help="最终输出文件路径（仅当单干员且 --step process 时有效）")
    parser.add_argument("--quiet", action="store_true", help="不打印处理后干员数据JSON到控制台")

    parser.add_argument("--step", choices=["fetch", "process", "armor"], default="armor",
                        help="执行步骤：fetch=抓取生成原始JSON；process=生成处理后干员数据JSON（默认）；armor=生成盔甲代码")
    parser.add_argument("--armor-templates", default="Template_Head.template,Template_Body.template,Template_Legs.template",
                        help="三个盔甲模板文件路径，用逗号分隔")
    parser.add_argument("--armor-output-dir", default="Armor", help="盔甲代码输出根目录（默认：Armor），实际文件会放在 {根目录}/{职业名}/{干员英文名}/ 下")

    args = parser.parse_args()

    # 解析模板路径
    template_paths = [p.strip() for p in args.armor_templates.split(',')]
    if len(template_paths) != 3:
        print("错误：--armor-templates 必须包含三个文件路径，用逗号分隔", file=sys.stderr)
        sys.exit(1)

    operators = parse_operator_names_from_args(args)
    if not operators:
        print("错误：未提供任何干员名", file=sys.stderr)
        sys.exit(1)

    # 批量模式或单干员模式分别处理
    if len(operators) > 1 and args.output:
        print("警告：批量模式下 --output 参数被忽略", file=sys.stderr)
        args.output = None

    for op in operators:
        try:
            if args.step == "fetch":
                process_fetch(op)
            elif args.step == "process":
                # 单干员且指定了 --output 时使用该路径，否则自动生成
                out_path = args.output if len(operators) == 1 and args.output else None
                process_process(op, args.save_intermediate, out_path, args.quiet)
            elif args.step == "armor":
                process_armor(op, args.save_intermediate, template_paths, args.armor_output_dir, args.quiet)
        except Exception as e:
            print(f"处理干员 {op} 时出错: {e}", file=sys.stderr)
            continue

if __name__ == "__main__":
    main()