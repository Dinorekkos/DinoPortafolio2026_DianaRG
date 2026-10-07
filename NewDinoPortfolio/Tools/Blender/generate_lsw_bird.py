"""Generate a low-poly, idle bird inspired by the supplied Little SimWorld art.

Blender 5.2:
    blender -b --factory-startup --python Tools/Blender/generate_lsw_bird.py

The only material samples the existing Furniture Cute color atlas with UVs.
"""

import math
import os
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT = Path(__file__).resolve().parents[2]
SOURCE_DIR = PROJECT / "Tools" / "Blender"
OUTPUT_DIR = PROJECT / "Assets" / "_DinoPortfolio" / "Meshes" / "LittleSimWorldBird"
TEXTURE = PROJECT / "Assets" / "Plugins" / "ithappy" / "Furniture_Cute" / "Textures" / "Textures.png"
NAME = "LSW_Bird_Idle"
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

# All colors come from stable points inside the existing 1024x1024 atlas.
UV = {
    "head": (0.170, 0.120),
    "head_shadow": (0.160, 0.570),
    "body": (0.170, 0.570),
    "belly": (0.200, 0.310),
    "wing": (0.130, 0.450),
    "wing_highlight": (0.940, 0.450),
    "eye_rim": (0.160, 0.570),
    "eye": (0.920, 0.040),
    "glint": (0.960, 0.465),
    "beak": (0.920, 0.040),
    "mouth": (0.920, 0.040),
    "tongue": (0.040, 0.420),
    "feet": (0.070, 0.380),
}

bpy.ops.wm.read_factory_settings(use_empty=True)

vertices = []
faces = []
face_colors = []
smooth_faces = []


def face(ids, color, smooth=False):
    faces.append(tuple(ids))
    face_colors.append(color)
    smooth_faces.append(smooth)


def ellipsoid(center, radii, color, lon=16, lat=8, pole_top=None, pole_bottom=None):
    """Add a UV-free low-poly ellipsoid; loop UV colors are assigned later."""
    cx, cy, cz = center
    rx, ry, rz = radii
    top = len(vertices)
    vertices.append((cx, cy, cz + rz))
    rings = []
    for j in range(1, lat):
        phi = math.pi * j / lat
        ring = []
        for i in range(lon):
            theta = 2 * math.pi * i / lon
            ring.append(len(vertices))
            vertices.append((
                cx + rx * math.sin(phi) * math.cos(theta),
                cy + ry * math.sin(phi) * math.sin(theta),
                cz + rz * math.cos(phi),
            ))
        rings.append(ring)
    bottom = len(vertices)
    vertices.append((cx, cy, cz - rz))
    for i in range(lon):
        nxt = (i + 1) % lon
        face((top, rings[0][i], rings[0][nxt]), pole_top or color, True)
    for upper, lower in zip(rings, rings[1:]):
        for i in range(lon):
            nxt = (i + 1) % lon
            face((upper[i], lower[i], lower[nxt], upper[nxt]), color, True)
    for i in range(lon):
        nxt = (i + 1) % lon
        face((rings[-1][i], bottom, rings[-1][nxt]), pole_bottom or color, True)


def oval_front(cx, y, cz, rx, rz, color, segments=14):
    """Flat detail facing the front (-Y), suitable for eyes and beak marks."""
    ids = []
    for i in range(segments):
        theta = 2 * math.pi * i / segments
        ids.append(len(vertices))
        vertices.append((cx + rx * math.cos(theta), y, cz + rz * math.sin(theta)))
    face(ids, color)


def wing(sign):
    """An oval, folded wing lies against the side of the body."""
    ellipsoid((sign * 0.355, 0.055, 0.495), (0.100, 0.205, 0.228), "wing", lon=10, lat=5)
    ellipsoid((sign * 0.430, 0.180, 0.355), (0.035, 0.087, 0.060), "wing_highlight", lon=8, lat=4)


def toe(start, end, radius, color, sides=6):
    p0, p1 = Vector(start), Vector(end)
    tangent = (p1 - p0).normalized()
    helper = Vector((0, 0, 1)) if abs(tangent.z) < 0.85 else Vector((1, 0, 0))
    axis_u = tangent.cross(helper).normalized()
    axis_v = tangent.cross(axis_u).normalized()
    rings = []
    for p in (p0, p1):
        ids = []
        for i in range(sides):
            angle = 2 * math.pi * i / sides
            ids.append(len(vertices))
            vertices.append(tuple(p + radius * (math.cos(angle) * axis_u + math.sin(angle) * axis_v)))
        rings.append(ids)
    for i in range(sides):
        nxt = (i + 1) % sides
        face((rings[0][i], rings[1][i], rings[1][nxt], rings[0][nxt]), color, True)
    face(tuple(reversed(rings[0])), color)
    face(rings[1], color)


# Compact pear-shaped silhouette: a large violet head over a softer belly.
ellipsoid((0.0, 0.025, 0.425), (0.425, 0.315, 0.355), "body", lon=14, lat=6)
ellipsoid((0.0, -0.025, 0.790), (0.408, 0.323, 0.432), "head", lon=16, lat=7)
ellipsoid((0.0, -0.140, 0.390), (0.290, 0.185, 0.255), "belly", lon=12, lat=5)

# Symmetrical oversized eyes with highlights, expressive even at prop scale.
for sign in (-1, 1):
    eye_x = sign * 0.224
    ellipsoid((eye_x, -0.282, 0.910), (0.105, 0.016, 0.126), "eye_rim", lon=10, lat=5)
    ellipsoid((eye_x, -0.297, 0.913), (0.089, 0.019, 0.106), "eye", lon=10, lat=5)
    oval_front(eye_x - 0.028, -0.318, 0.958, 0.020, 0.024, "glint", segments=8)
    oval_front(eye_x + 0.024, -0.318, 0.880, 0.007, 0.009, "glint", segments=8)

# A short closed beak keeps the expression calm for an idle prop.
ellipsoid((0.000, -0.335, 0.741), (0.069, 0.055, 0.037), "beak", lon=8, lat=4)

wing(-1)
wing(1)

# A short fan of tail feathers projects behind the folded wings.
for x, length in ((-0.090, 0.125), (0.000, 0.160), (0.090, 0.125)):
    ellipsoid((x, 0.290 + length / 2, 0.320), (0.070, length, 0.050), "body", lon=8, lat=4)

# Small three-toed pink feet rest on the ground at z≈0.
for sign in (-1, 1):
    base_x = sign * 0.155
    toe((base_x, -0.035, 0.175), (base_x, -0.044, 0.055), 0.027, "feet")
    for spread in (-0.045, 0.0, 0.045):
        toe((base_x, -0.045, 0.052), (base_x + spread, -0.163, 0.025), 0.024, "feet")

# Three tiny feathers form the crown tuft.
for x, z, rx, rz in ((-0.037, 1.237, 0.029, 0.055), (0.009, 1.250, 0.027, 0.063), (0.049, 1.230, 0.027, 0.053)):
    ellipsoid((x, 0.012, z), (rx, 0.033, rz), "head_shadow", lon=8, lat=4)

mesh = bpy.data.meshes.new(NAME + "_Mesh")
mesh.from_pydata(vertices, [], faces)
mesh.update(calc_edges=True)
bird = bpy.data.objects.new(NAME, mesh)
bpy.context.collection.objects.link(bird)

material = bpy.data.materials.new("Color")
material.use_nodes = True
bsdf = material.node_tree.nodes.get("Principled BSDF")
atlas_node = material.node_tree.nodes.new("ShaderNodeTexImage")
atlas_node.image = bpy.data.images.load(str(TEXTURE), check_existing=True)
material.node_tree.links.new(atlas_node.outputs["Color"], bsdf.inputs["Base Color"])
bsdf.inputs["Roughness"].default_value = 0.88
mesh.materials.append(material)

uv_layer = mesh.uv_layers.new(name="UVMap")
for polygon, color, smooth in zip(mesh.polygons, face_colors, smooth_faces):
    polygon.use_smooth = smooth
    for loop_index in polygon.loop_indices:
        uv_layer.data[loop_index].uv = UV[color]

bpy.context.view_layer.objects.active = bird
bird.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.mesh.normals_make_consistent(inside=False)
bpy.ops.mesh.quads_convert_to_tris(quad_method="BEAUTY", ngon_method="BEAUTY")
bpy.ops.object.mode_set(mode="OBJECT")

fbx_path = OUTPUT_DIR / (NAME + ".fbx")
bpy.ops.export_scene.fbx(
    filepath=str(fbx_path),
    use_selection=True,
    object_types={"MESH"},
    global_scale=20.0,
    apply_unit_scale=True,
    axis_forward="-Z",
    axis_up="Y",
    path_mode="AUTO",
    embed_textures=False,
    add_leaf_bones=False,
    bake_anim=False,
)

camera_data = bpy.data.cameras.new("Preview_Camera")
camera = bpy.data.objects.new("Preview_Camera", camera_data)
bpy.context.collection.objects.link(camera)
camera_data.type = "ORTHO"
camera_data.ortho_scale = 2.30
bpy.context.scene.camera = camera

for name, location, power, size in (
    ("Preview_Key", (0.8, -2.8, 3.2), 330, 3.2),
    ("Preview_Fill", (-2.0, -1.2, 1.2), 170, 2.6),
):
    lamp_data = bpy.data.lights.new(name, "AREA")
    lamp_data.energy = power
    lamp_data.shape = "DISK"
    lamp_data.size = size
    lamp = bpy.data.objects.new(name, lamp_data)
    bpy.context.collection.objects.link(lamp)
    lamp.location = location
    lamp.rotation_euler = (Vector((0, 0, 0.65)) - lamp.location).to_track_quat("-Z", "Y").to_euler()

scene = bpy.context.scene
scene.render.engine = "CYCLES"
scene.cycles.samples = 24
scene.render.resolution_x = 850
scene.render.resolution_y = 850
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.world = bpy.data.worlds.new("Preview_World")
scene.world.use_nodes = True
scene.world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.67, 0.77, 0.85, 1)
scene.world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.6
scene.view_settings.view_transform = "Standard"

for suffix, location in (
    ("preview", (1.5, -3.0, 1.55)),
    ("side", (3.3, 0.1, 1.45)),
):
    camera.location = location
    camera.rotation_euler = (Vector((0.0, 0.0, 0.65)) - camera.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = str(SOURCE_DIR / (NAME + "_" + suffix + ".png"))
    bpy.ops.render.render(write_still=True)

camera.location = (1.5, -3.0, 1.55)
camera.rotation_euler = (Vector((0.0, 0.0, 0.65)) - camera.location).to_track_quat("-Z", "Y").to_euler()
atlas_node.image.filepath = "//" + os.path.relpath(TEXTURE, SOURCE_DIR).replace("\\", "/")
bpy.context.preferences.filepaths.save_version = 0
blend_path = SOURCE_DIR / (NAME + ".blend")
bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))

print("LSW_BIRD_RESULT", {
    "blend": str(blend_path),
    "fbx": str(fbx_path),
    "vertices": len(mesh.vertices),
    "triangles": len(mesh.polygons),
    "materials": [m.name for m in mesh.materials],
    "texture": str(TEXTURE),
})
