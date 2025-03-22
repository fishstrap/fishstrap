import re

with open("Strings.vi.resx", "r", encoding="utf-8") as file:
    content = file.read()

# Step 1: Extract <data ...> blocks and replace them with placeholders.
data_blocks = {}
placeholder_prefix = "__DATA_BLOCK_"

def data_block_replacer(match):
    block = match.group(0)
    key = f"{placeholder_prefix}{len(data_blocks)}__"
    data_blocks[key] = block
    return key

# Use DOTALL so that the block (which may span multiple lines) is captured.
content_temp = re.sub(r'<data[^>]*>.*?</data>', data_block_replacer, content, flags=re.DOTALL)

# Step 2: Replace 'Bloxstrap' with 'Fishstrap' in the content outside <data> blocks.
content_temp = content_temp.replace("Bloxstrap", "Fishstrap")

# Step 3: Process each <data> block.
def process_data_block(block):
    # Split the block into three parts: opening tag, inner content, and closing tag.
    m = re.match(r'(<data[^>]*>)(.*?)(</data>)', block, re.DOTALL)
    if m:
        opening = m.group(1)  # Contains attributes (do not change this)
        inner = m.group(2)    # Text content inside the tag
        closing = m.group(3)
        # Replace only inside the inner content.
        inner_replaced = inner.replace("Bloxstrap", "Fishstrap")
        return opening + inner_replaced + closing
    return block

# Restore and process each data block.
for key, block in data_blocks.items():
    processed_block = process_data_block(block)
    content_temp = content_temp.replace(key, processed_block)

with open("Strings.zh.TW.resx", "w", encoding="utf-8") as file:
    file.write(content_temp)

print("Replacement done!")
