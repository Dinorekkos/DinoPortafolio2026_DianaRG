"""Create single-material VR headset variants for the Furniture Cute atlas.

Run with Blender 5.2:
    blender -b --python Tools/Blender/generate_vr_headset.py
    blender -b --python Tools/Blender/generate_vr_headset.py -- --with-straps

Each run writes an editable .blend, a Unity-ready FBX, and a preview image.
"""

import math
import os
import sys
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT = Path(__file__).resolve().parents[2]
SOURCE_DIR = PROJECT / "Tools" / "Blender"
OUTPUT_DIR = PROJECT / "Assets" / "_DinoPortfolio" / "Meshes" / "VRHeadset"
WITH_STRAPS = "--with-straps" in sys.argv
ASSET_NAME = "VR_Headset_Straps" if WITH_STRAPS else "VR_Headset"
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
    "strap": (0.190, 0.460),       # light lavender fabric
    "strap_edge": (0.180, 0.720),  # darker purple edge and hardware
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


def add_band(points, width_axis, width, thickness, color="strap", edge_color="strap_edge"):
    """Low-poly closed ribbon following a curved center line."""
    centers = [Vector(point) for point in points]
    axis = Vector(width_axis).normalized()
    sections = []
    for i, center in enumerate(centers):
        prev = centers[max(i - 1, 0)]
        following = centers[min(i + 1, len(centers) - 1)]
        tangent = (following - prev).normalized()
        normal = axis.cross(tangent).normalized()
        corners = (
            center - axis * width / 2 + normal * thickness / 2,
            center + axis * width / 2 + normal * thickness / 2,
            center + axis * width / 2 - normal * thickness / 2,
            center - axis * width / 2 - normal * thickness / 2,
        )
        ids = []
        for corner in corners:
            ids.append(len(verts))
            verts.append(tuple(corner))
        sections.append(ids)
    for rear, front in zip(sections, sections[1:]):
        for side in range(4):
            next_side = (side + 1) % 4
            add_face(
                (rear[side], rear[next_side], front[next_side], front[side]),
                color if side in (0, 2) else edge_color,
                side == 0,
            )
    add_face(tuple(reversed(sections[0])), edge_color)
    add_face(sections[-1], edge_color)


def add_side_hinge(sign):
    """Small round strap mount on either side of the headset case."""
    cy, cz = 0.145, 0.015
    segments = 12
    inner_ids = []
    outer_ids = []
    inset_ids = []
    for i in range(segments):
        angle = 2 * math.pi * i / segments
        dy, dz = math.cos(angle), math.sin(angle)
        for ids, x, radius in (
            (inner_ids, sign * 0.930, 0.145),
            (outer_ids, sign * 0.992, 0.145),
            (inset_ids, sign * 0.997, 0.079),
        ):
            ids.append(len(verts))
            verts.append((x, cy + radius * dy, cz + radius * dz))
    for i in range(segments):
        j = (i + 1) % segments
        if sign > 0:
            add_face((inner_ids[i], inner_ids[j], outer_ids[j], outer_ids[i]), "strap_edge", True)
            add_face((outer_ids[i], outer_ids[j], inset_ids[j], inset_ids[i]), "strap")
        else:
            add_face((inner_ids[j], inner_ids[i], outer_ids[i], outer_ids[j]), "strap_edge", True)
            add_face((outer_ids[j], outer_ids[i], inset_ids[i], inset_ids[j]), "strap")
    add_face(inset_ids if sign > 0 else tuple(reversed(inset_ids)), "cushion")


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

if WITH_STRAPS:
    # The lateral band wraps behind the head and attaches to the visor sides.
    add_band(
        [
            (-0.885, 0.145, 0.105),
            (-0.935, 0.500, 0.125),
            (-0.870, 0.940, 0.160),
            (-0.665, 1.290, 0.190),
            (-0.340, 1.465, 0.200),
            (0.000, 1.510, 0.205),
            (0.340, 1.465, 0.200),
            (0.665, 1.290, 0.190),
            (0.870, 0.940, 0.160),
            (0.935, 0.500, 0.125),
            (0.885, 0.145, 0.105),
        ],
        (0, 0, 1),
        width=0.176,
        thickness=0.042,
    )

    # Arched strap across the crown, reaching the rear lateral band.
    add_band(
        [
            (0.000, -0.120, 0.428),
            (0.000, 0.185, 0.535),
            (0.000, 0.500, 0.770),
            (0.000, 0.860, 0.900),
            (0.000, 1.185, 0.835),
            (0.000, 1.425, 0.555),
            (0.000, 1.505, 0.230),
        ],
        (1, 0, 0),
        width=0.176,
        thickness=0.039,
    )

    # Shallow lower support behind the face-side insert.
    add_band(
        [
            (-0.845, 0.430, -0.205),
            (-0.855, 0.820, -0.275),
            (-0.670, 1.135, -0.315),
            (-0.350, 1.325, -0.330),
            (0.000, 1.375, -0.330),
            (0.350, 1.325, -0.330),
            (0.670, 1.135, -0.315),
            (0.855, 0.820, -0.275),
            (0.845, 0.430, -0.205),
        ],
        (0, 0, 1),
        width=0.120,
        thickness=0.033,
    )
    add_side_hinge(-1)
    add_side_hinge(1)

mesh = bpy.data.meshes.new(ASSET_NAME + "_Mesh")
mesh.from_pydata(verts, [], faces)
mesh.update(calc_edges=True)
obj = bpy.data.objects.new(ASSET_NAME, mesh)
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

fbx_path = OUTPUT_DIR / (ASSET_NAME + ".fbx")
bpy.ops.export_scene.fbx(
    filepath=str(fbx_path),
    use_selection=True,
    object_types={"MESH"},
    # Unity's FBX importer reads these Blender coordinates as centimetres.
    # 1.88 source units become a roughly 30 cm wide device in Unity.
    global_scale=20.0 if WITH_STRAPS else 16.0,
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
camera.location = (2.65, -3.30, 2.05) if WITH_STRAPS else (2.05, -3.25, 1.55)
camera.rotation_euler = (Vector((0.0, 0.40, 0.15)) - camera.location).to_track_quat("-Z", "Y").to_euler()
camera_data.type = "ORTHO"
camera_data.ortho_scale = 3.35 if WITH_STRAPS else 2.75
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
scene.render.filepath = str(SOURCE_DIR / (ASSET_NAME + "_preview.png"))
scene.world = bpy.data.worlds.new("Preview_World")
scene.world.use_nodes = True
scene.world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.55, 0.67, 0.78, 1.0)
scene.world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.5
scene.view_settings.view_transform = "Standard"
bpy.ops.render.render(write_still=True)

if WITH_STRAPS:
    camera.location = (-2.55, 3.40, 1.85)
    camera.rotation_euler = (Vector((0.0, 0.55, 0.15)) - camera.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = str(SOURCE_DIR / (ASSET_NAME + "_back.png"))
    bpy.ops.render.render(write_still=True)
    camera.location = (2.65, -3.30, 2.05)
    camera.rotation_euler = (Vector((0.0, 0.40, 0.15)) - camera.location).to_track_quat("-Z", "Y").to_euler()

blend_path = SOURCE_DIR / (ASSET_NAME + ".blend")
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
