from PIL import Image, ImageOps
import os

textures_dir = "cloth"   # folder with textures
masks_dir = "patterns"         # folder with masks
output_dir = "overlays"       # where to save results

os.makedirs(output_dir, exist_ok=True)

textures = [f for f in os.listdir(textures_dir) if f.lower().endswith(".png")]
masks = [f for f in os.listdir(masks_dir) if f.lower().endswith(".png")]

for tex_file in textures:
    tex = Image.open(os.path.join(textures_dir, tex_file)).convert("RGBA")
    for mask_file in masks:
        mask = Image.open(os.path.join(masks_dir, mask_file)).convert("RGBA")

        # Resize mask if needed
        if mask.size != tex.size:
            mask = mask.resize(tex.size, Image.LANCZOS)

        # Split channels
        r, g, b, a_tex = tex.split()
        _, _, _, a_mask = mask.split()

        # Normal masked version
        tex_masked = Image.merge("RGBA", (r, g, b, a_mask))
        out_name = f"{os.path.splitext(tex_file)[0]}_{os.path.splitext(mask_file)[0]}_str.png"
        tex_masked.save(os.path.join(output_dir, out_name))

        # Inverted masked version
        a_mask_inverted = ImageOps.invert(a_mask)
        tex_masked_inv = Image.merge("RGBA", (r, g, b, a_mask_inverted))
        out_name_inv = f"{os.path.splitext(tex_file)[0]}_{os.path.splitext(mask_file)[0]}_inv.png"
        tex_masked_inv.save(os.path.join(output_dir, out_name_inv))
