
# CODE BY CHATGPT (im suck at python 😭😭😭😭)


import re
import glob
import os

# This pattern will match all files like "Strings.anything.resx"
pattern = "Strings.*.resx"

# Loop through every matching file in the current directory
for file_path in glob.glob(pattern):
    print(f"Processing {file_path}...")
    
    with open(file_path, "r", encoding="utf-8") as file:
        content = file.read()

    # Step 1: Extract <data ...> blocks and replace them with placeholders.
    data_blocks = {}
    placeholder_prefix = "__DATA_BLOCK_"

    def data_block_replacer(match):
        block = match.group(0)
        key = f"{placeholder_prefix}{len(data_blocks)}__"
        data_blocks[key] = block
        return key

    # Use DOTALL so blocks (which may span multiple lines) are captured.
    content_temp = re.sub(
        r'<data[^>]*>.*?</data>',
        data_block_replacer,
        content,
        flags=re.DOTALL
    )

    # Step 2: Replace 'Bloxstrap' with 'Fishstrap' in content outside <data> blocks.
    content_temp = content_temp.replace("Bloxstrap", "Fishstrap")

    # Step 3: Process each <data> block individually.
    def process_data_block(block):
        m = re.match(r'(<data[^>]*>)(.*?)(</data>)', block, re.DOTALL)
        if m:
            opening = m.group(1)  # e.g. <data name="Menu.Bloxstrap.Title" ...>
            inner = m.group(2)    # The text inside the <data> tag
            closing = m.group(3)  # </data>
            # Replace only inside the inner content, not the attributes
            inner_replaced = inner.replace("Bloxstrap", "Fishstrap")
            return opening + inner_replaced + closing
        return block

    for key, block in data_blocks.items():
        processed_block = process_data_block(block)
        content_temp = content_temp.replace(key, processed_block)

    # Overwrite the original file with the updated content
    with open(file_path, "w", encoding="utf-8") as file:
        file.write(content_temp)

    print(f"Finished processing {file_path}.\n")

print("All matching .resx files have been processed!")
