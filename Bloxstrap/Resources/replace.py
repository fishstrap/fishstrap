

import re
import glob
import os

pattern = "Strings.*.resx"

for file_path in glob.glob(pattern):
    print(f"Processing {file_path}...")
    
    with open(file_path, "r", encoding="utf-8") as file:
        content = file.read()

    data_blocks = {}
    placeholder_prefix = "__DATA_BLOCK_"

    def data_block_replacer(match):
        block = match.group(0)
        key = f"{placeholder_prefix}{len(data_blocks)}__"
        data_blocks[key] = block
        return key

    content_temp = re.sub(
        r'<data[^>]*>.*?</data>',
        data_block_replacer,
        content,
        flags=re.DOTALL
    )

    content_temp = content_temp.replace("Bloxstrap", "Fishstrap")

    def process_data_block(block):
        m = re.match(r'(<data[^>]*>)(.*?)(</data>)', block, re.DOTALL)
        if m:
            opening = m.group(1) 
            inner = m.group(2)    
            closing = m.group(3)  

            inner_replaced = inner.replace("Bloxstrap", "Fishstrap")
            return opening + inner_replaced + closing
        return block

    for key, block in data_blocks.items():
        processed_block = process_data_block(block)
        content_temp = content_temp.replace(key, processed_block)

    with open(file_path, "w", encoding="utf-8") as file:
        file.write(content_temp)

    print(f"Finished processing {file_path}.\n")

print("All matching .resx files have been processed!")
