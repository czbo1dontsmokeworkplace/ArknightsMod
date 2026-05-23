import json
import re
import os

def generate_armor_from_data(data, template_paths, output_dir="output"):
    """
    根据干员数据字典直接生成盔甲代码文件
    :param data: 经过 post_process 处理后的干员数据字典
    :param template_paths: 包含三个路径的元组或列表 (head_template, body_template, legs_template)
    :param output_dir: 输出目录
    """
    name_en = data['name_en']
    class_name = data['class']
    rarity = data['rarity']
    armor_stats = data['armor_stats']

    materials_map = {
        'head': 'head_materials',
        'body': 'body_materials',
        'legs': 'legs_materials'
    }

    # 读取三个模板内容
    head_path, body_path, legs_path = template_paths
    with open(head_path, 'r', encoding='utf-8') as f:
        head_template = f.read()
    with open(body_path, 'r', encoding='utf-8') as f:
        body_template = f.read()
    with open(legs_path, 'r', encoding='utf-8') as f:
        legs_template = f.read()

    templates = {
        'head': head_template,
        'body': body_template,
        'legs': legs_template
    }

    # 通用替换字典（用于替换模板中除配方方法体之外的占位符）
    base_replacements = {
        '{name_en}': name_en,
        '{class}': class_name,
        '{rarity}': str(rarity),
        '{head_life}': str(armor_stats['head_life']),
        '{head_defense}': str(armor_stats['head_defense']),
        '{body_life}': str(armor_stats['body_life']),
        '{body_defense}': str(armor_stats['body_defense']),
        '{legs_life}': str(armor_stats['legs_life']),
        '{legs_defense}': str(armor_stats['legs_defense'])
    }

    for part, template in templates.items():
        content = template

        # 基本占位符替换（影响方法体之外的部分，以及方法体内将被整体替换的部分，无影响）
        for key, value in base_replacements.items():
            content = content.replace(key, value)

        # 生成配方代码（自引用 + Orundum + 材料列表 + 工厂 + 条件 + 禁止拆解）
        materials_key = materials_map[part]
        materials = data.get(materials_key, [])
        orundum_amount = rarity * 10

        recipe_lines = [
            "\t\t\tCreateRecipe()",
            f"\t\t\t.AddIngredient<{name_en}{part.capitalize()}>(1)",
            f"\t\t\t.AddIngredient<Orundum>({orundum_amount})"
        ]
        for mat in materials:
            item_name = mat['item']
            amount = mat['amount']
            recipe_lines.append(f"\t\t\t.AddIngredient<{item_name}>({amount})")
        recipe_lines.append("\t\t\t.AddTile(ModContent.TileType<FactoryTile>())")
        recipe_lines.append("\t\t\t.AddCondition(NeoArmorUtils.NeedVanity)")
        recipe_lines.append("\t\t\t.DisableDecraft()")
        recipe_lines.append("\t\t\t.Register();")

        new_recipe_body = "\n".join(recipe_lines)

        # 替换 AddRecipes 方法体
        method_pattern = r'(public override void AddRecipes\(\)\s*\{)'
        match = re.search(method_pattern, content)
        if match:
            start_pos = match.end()  # 指向 '{' 之后
            brace_count = 1
            end_pos = start_pos
            for i in range(start_pos, len(content)):
                if content[i] == '{':
                    brace_count += 1
                elif content[i] == '}':
                    brace_count -= 1
                    if brace_count == 0:
                        end_pos = i
                        break
            content = content[:start_pos] + "\n" + new_recipe_body + "\n\t\t" + content[end_pos:]
        else:
            print(f"警告：在 {part} 模板中未找到 AddRecipes 方法，跳过配方生成")

        # 构造输出目录：根目录/干员职业名/干员英文名/
        final_output_dir = os.path.join(output_dir, class_name, name_en)
        os.makedirs(final_output_dir, exist_ok=True)

        output_filename = f"{name_en}{part.capitalize()}.cs"
        output_path = os.path.join(final_output_dir, output_filename)
        with open(output_path, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"已生成盔甲文件: {output_path}")

def generate_armor_files(json_path, template_head_path, template_body_path, template_legs_path, output_dir="output"):
    """
    兼容原脚本的函数：从 JSON 文件读取数据并生成盔甲代码
    """
    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    generate_armor_from_data(data, (template_head_path, template_body_path, template_legs_path), output_dir)

if __name__ == "__main__":
    # 保持原脚本的独立运行能力
    generate_armor_files(
        json_path="XXX_最终.json",
        template_head_path="Template_Head.template",
        template_body_path="Template_Body.template",
        template_legs_path="Template_Legs.template",
        output_dir="output"
    )