"""Create a small, single-material VR headset for the Furniture Cute atlas.

Run with Blender 5.2:
    blender -b --python Tools/Blender/generate_vr_headset.py

The script writes an editable .blend, a Unity-ready FBX, and a preview image.
"""

import math
import os
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT = Path(__file__).resolve().parents[2]
SOURCE_DIR = PROJECT / "Tools" / "Blender"
OUTPUT_DIR = PROJECT / "Assets" / "_DinoPortfolio" / "Meshes" / "VRHeadset"
TEXTURE = (
    PROJECT
    / "Assets"
    / "Plugins"
    / "ithappy"
    / "Furniture_Cute"
    / "Textures"
    / "Textures.png"
)
SOURCE_DIR.mkdir(parents=True, exist_ok=True)
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

# All face UVs sample the original atlas; no new image or material is created.
# V is measured upward in Blender/Unity UV coordinates.
UV = {
    "shell": (0.946, 0.920),       # cool off-white plastic
    "shell_side": (0.925, 0.760),  # slightly shaded grey plastic
    "face": (0.229, 0.018),        # darkest blue available in the atlas
    "face_side": (0.223, 0.069),
    "cyan": (0.381, 0.292),        # status light
    "aqua": (0.355, 0.725),        # light cyan inset/lens detail
    "cushion": (0.917, 0.153),     # charcoal face cushion
    "lens": (0.882, 0.091),        # near-black optical surfaces
}

bpy.ops.wm.read_factory_settings(use_empty=True)

verts = []
faces = []
face_colors = []
face_smooth = []


def add_face(indices, color, smooth=False):
    faces.append(tuple(indices))
    face_colors.append(color)
    face_smooth.append(smooth)


def rounded_outline(width, height, radius, steps=5):
    """Clockwise as seen from the rear, CCW from the front (-Y)."""
    points = []
    for cx, cz, start in (
        (width / 2 - radius, height / 2 - radius, 0),
        (-width / 2 + radius, height / 2 - radius, 90),
        (-width / 2 + radius, -height / 2 + radius, 180),
        (width / 2 - radius, -height / 2 + radius, 270),
    ):
        for i in range(steps + 1):
            angle = math.radians(start + 90 * i / steps)
            points.append((cx + radius * math.cos(angle), cz + radius * math.sin(angle)))
    return points


def add_prism(rings, center_z=0.0, front_color="shell", side_color="shell_side", back_color=None):
    """Each ring is (Y, width, height, corner radius), rear to front."""
    ring_ids = []
    for y, width, height, radius in rings:
        ids = []
        for x, z in rounded_outline(width, height, radius):
            ids.append(len(verts))
            verts.append((x, y, z + center_z))
        ring_ids.append(ids)
    for rear, front in zip(ring_ids, ring_ids[1:]):
        for i in range(len(rear)):
            j = (i + 1) % len(rear)
            add_face((rear[i], rear[j], front[j], front[i]), side_color, True)
    add_face(ring_ids[-1], front_color)
    add_face(tuple(reversed(ring_ids[0])), back_color or side_color)


def add_disc(cx, cy, cz, radius, color, segments=14):
    ids = []
    for i in range(segments):
        angle = 2 * math.pi * i / segments
        ids.append(len(verts))
        verts.append((cx + radius * math.cos(angle), cy, cz + radius * math.sin(angle)))
    # +Y normal for the viewing side at the back of the headset.
    add_face(tuple(reversed(ids)), color)


def add_lens_rim(cx, cz):
    outer = []
    inner = []
    segments = 14
    for i in range(segments):
        angle = 2 * math.pi * i / segments
        ca, sa = math.cos(angle), math.sin(angle)
        outer.append(len(verts))
        verts.append((cx + 0.215 * ca, 0.356, cz + 0.215 * sa))
        inner.append(len(verts))
        verts.append((cx + 0.158 * ca, 0.371, cz + 0.158 * sa))
    for i in range(segments):
        j = (i + 1) % segments
        add_face((outer[i], inner[i], inner[j], outer[j]), "aqua", True)
    add_disc(cx, 0.366, cz, 0.163, "lens", segments)


# Rounded shell, with a little taper toward the front and the user's face.
add_prism(
    [
        (0.290, 1.72, 0.750, 0.245),
        (0.195, 1.88, 0.890, 0.278),
        (-0.185, 1.88, 0.890, 0.278),
        (-0.322, 1.78, 0.810, 0.265),
    ],
    front_color="shell",
    side_color="shell_side",
)

# Dark curved-corner front visor. Its edges sit just proud of the white case.
add_prism(
    [
        (-0.323, 1.696, 0.682, 0.300),
        (-0.385, 1.722, 0.705, 0.305),
        (-0.446, 1.665, 0.654, 0.292),
    ],
    center_z=0.032,
    front_color="face",
    side_color="face_side",
)

# Short turquoise status indicator on the lower front, matching the reference.
add_prism(
    [(-0.448, 0.238, 0.026, 0.012), (-0.454, 0.238, 0.026, 0.012)],
    center_z=-0.223,
    front_color="cyan",
    side_color="cyan",
)

# Soft face-side insert and two shallow optical lenses; neither is a strap.
add_prism(
    [(0.292, 1.46, 0.625, 0.260), (0.339, 1.40, 0.585, 0.252)],
    front_color="cushion",
    side_color="cushion",
    back_color="cushion",
)
for eye_x in (-0.365, 0.365):
    add_lens_rim(eye_x, 0.005)

mesh = bpy.data.meshes.new("VR_Headset_Mesh")
mesh.from_pydata(verts, [], faces)
mesh.update(calc_edges=True)
obj = bpy.data.objects.new("VR_Headset", mesh)
bpy.context.collection.objects.link(obj)

material = bpy.data.materials.new("Color")
material.use_nodes = True
nodes = material.node_tree.nodes
bsdf = nodes.get("Principled BSDF")
image_node = nodes.new("ShaderNodeTexImage")
image_node.image = bpy.data.images.load(str(TEXTURE), check_existing=True)
material.node_tree.links.new(image_node.outputs["Color"], bsdf.inputs["Base Color"])
bsdf.inputs["Roughness"].default_value = 0.87
mesh.materials.append(material)

uv_layer = mesh.uv_layers.new(name="UVMap")
for poly, color, smooth in zip(mesh.polygons, face_colors, face_smooth):
    poly.use_smooth = smooth
    for loop_index in poly.loop_indices:
        uv_layer.data[loop_index].uv = UV[color]

# Give the mesh triangles only, matching the final WebGL topology.
bpy.context.view_layer.objects.active = obj
obj.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.mesh.quads_convert_to_tris(quad_method="BEAUTY", ngon_method="BEAUTY")
bpy.ops.object.mode_set(mode="OBJECT")

fbx_path = OUTPUT_DIR / "VR_Headset.fbx"
bpy.ops.export_scene.fbx(
    filepath=str(fbx_path),
    use_selection=True,
    object_types={"MESH"},
    # Unity's FBX importer reads these Blender coordinates as centimetres.
    # 1.88 source units become a roughly 30 cm wide device in Unity.
    global_scale=16.0,
    apply_unit_scale=True,
    axis_forward="-Z",
    axis_up="Y",
    path_mode="AUTO",
    embed_textures=False,
    add_leaf_bones=False,
    bake_anim=False,
)

# Preview camera and lights stay in the editable Blender file, but are excluded
# from the FBX export. The transparent render makes visual review easy.
camera_data = bpy.data.cameras.new("Preview_Camera")
camera = bpy.data.objects.new("Preview_Camera", camera_data)
bpy.context.collection.objects.link(camera)
camera.location = (2.05, -3.25, 1.55)
camera.rotation_euler = (Vector((0.0, 0.0, 0.0)) - camera.location).to_track_quat("-Z", "Y").to_euler()
camera_data.type = "ORTHO"
camera_data.ortho_scale = 2.75
bpy.context.scene.camera = camera

for name, location, power, size in (
    ("Preview_Key", (0.2, -3.0, 3.0), 360, 4.0),
    ("Preview_Fill", (-2.2, -1.0, 0.2), 190, 3.0),
):
    light_data = bpy.data.lights.new(name, "AREA")
    light_data.energy = power
    light_data.shape = "DISK"
    light_data.size = size
    light = bpy.data.objects.new(name, light_data)
    bpy.context.collection.objects.link(light)
    light.location = location
    light.rotation_euler = (Vector((0.0, 0.0, 0.0)) - light.location).to_track_quat("-Z", "Y").to_euler()

scene = bpy.context.scene
scene.render.engine = "CYCLES"
scene.cycles.samples = 24
scene.render.resolution_x = 900
scene.render.resolution_y = 680
scene.render.resolution_percentage = 100
scene.render.film_transparent = False
scene.render.image_settings.file_format = "PNG"
scene.render.filepath = str(SOURCE_DIR / "VR_Headset_preview.png")
scene.world = bpy.data.worlds.new("Preview_World")
scene.world.use_nodes = True
scene.world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.55, 0.67, 0.78, 1.0)
scene.world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.5
scene.view_settings.view_transform = "Standard"
bpy.ops.render.render(write_still=True)

blend_path = SOURCE_DIR / "VR_Headset.blend"
image_node.image.filepath = "//" + os.path.relpath(TEXTURE, SOURCE_DIR).replace("\\", "/")
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))

print("VR_HEADSET_RESULT", {
    "blend": str(blend_path),
    "fbx": str(fbx_path),
    "vertices": len(mesh.vertices),
    "triangles": len(mesh.polygons),
    "materials": [m.name for m in mesh.materials],
    "texture": str(TEXTURE),
    "uv_samples": UV,
})
