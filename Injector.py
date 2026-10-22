"""
GL Map Packer - Maya material importer
======================================

Reads the JSON preset written by the GL Map Packer "Injector" tab and builds a
shading network in Maya.

Pairs with the exporter contract:
    %TEMP%/maya_bridge/material_preset.json
    {
      "message":      "...",
      "materialName": "M_red_rock",
      "shaderType":   "openPBRSurface" | "usdPreviewSurface",
      "maps": { "albedo": "...", "roughness": "...", "normal": "...", ... }
    }

Usage inside Maya (Script Editor, Python tab):
    import maya_material_importer as imp
    imp.import_preset()                 # reads the default temp path
    # or an explicit file:
    imp.import_preset(r"C:/path/to/material_preset.json")
    # assign to current selection as you import:
    imp.import_preset(assign_to_selection=True)

Tested against Maya 2023+ (Python 3). No external dependencies.
"""

import os
import json
import tempfile

import maya.cmds as cmds


# --------------------------------------------------------------------------- #
#  Paths / IO
# --------------------------------------------------------------------------- #

def default_preset_path():
    """The fixed path the exporter always writes to: %TEMP%/maya_bridge/material_preset.json"""
    return os.path.join(tempfile.gettempdir(), "maya_bridge", "material_preset.json")


def read_preset(path=None):
    """Load and lightly validate the preset dict. Raises on missing file / bad JSON."""
    path = path or default_preset_path()
    if not os.path.isfile(path):
        raise IOError("Preset file not found: {}".format(path))

    with open(path, "r", encoding="utf-8") as fh:
        data = json.load(fh)

    if not isinstance(data, dict):
        raise ValueError("Preset root is not a JSON object.")
    if "shaderType" not in data:
        raise ValueError("Preset is missing 'shaderType'.")
    data.setdefault("maps", {})
    data.setdefault("materialName", "M_material")
    return data


# --------------------------------------------------------------------------- #
#  Shader-type routing
# --------------------------------------------------------------------------- #

# Each entry maps the exporter's shaderType string to the Maya node type plus the
# plug names to use for each semantic slot on that shader. Keeping this as data
# means adding a new shader later is a table edit, not new branching code.
SHADER_TABLE = {
    "openPBRSurface": {
        "node_type": "openPBRSurface",
        "plugs": {
            "albedo":    ("baseColor", "color"),
            "roughness": ("specularRoughness", "scalar"),
            "metallic":  ("baseMetalness", "scalar"),
            "normal":    ("normalCamera", "normal"),
            "opacity":   ("geometryOpacity", "scalar"),
            "emission":  ("emissionColor", "color"),
            "ao":        ("baseWeight", "scalar"),
        },
    },
    "usdPreviewSurface": {
        "node_type": "usdPreviewSurface",
        "plugs": {
            "albedo":    ("diffuseColor", "color"),
            "roughness": ("roughness", "scalar"),
            "metallic":  ("metallic", "scalar"),
            "normal":    ("normal", "normal"),
            "opacity":   ("opacity", "scalar"),
            "emission":  ("emissiveColor", "color"),
        },
    },
}

# Slots whose data is linear (not color-managed as sRGB).
LINEAR_SLOTS = {"roughness", "metallic", "normal", "ao", "opacity"}
# Slots that carry color and should be read as sRGB / Raw-aware.
COLOR_SLOTS = {"albedo", "emission"}


def _fallback_node_type(shader_type):
    """If the studio's Maya lacks the requested node type, degrade gracefully."""
    if cmds.objExists("defaultShaderList1") and shader_type in cmds.allNodeTypes():
        return shader_type
    # openPBRSurface ships with recent Maya; fall back to aiStandardSurface, then Lambert.
    for candidate in (shader_type, "aiStandardSurface", "standardSurface", "lambert"):
        if candidate in cmds.allNodeTypes():
            return candidate
    return "lambert"


# --------------------------------------------------------------------------- #
#  Node creation helpers
# --------------------------------------------------------------------------- #

def _unique(name):
    base, i = name, 1
    while cmds.objExists(name):
        i += 1
        name = "{}{}".format(base, i)
    return name


def _make_file_node(tex_path, slot, material_name):
    """Create a file + place2dTexture pair, set colorspace by slot, return the file node."""
    file_node = cmds.shadingNode(
        "file", asTexture=True, isColorManaged=True,
        name=_unique("{}_{}_file".format(material_name, slot)))
    place = cmds.shadingNode(
        "place2dTexture", asUtility=True,
        name=_unique("{}_{}_place2d".format(material_name, slot)))

    # Standard place2d -> file connections.
    for attr in ("coverage", "translateFrame", "rotateFrame", "mirrorU", "mirrorV",
                 "stagger", "wrapU", "wrapV", "repeatUV", "offset", "rotateUV",
                 "noiseUV", "vertexUvOne", "vertexUvTwo", "vertexUvThree",
                 "vertexCameraOne"):
        try:
            cmds.connectAttr(place + "." + attr, file_node + "." + attr, force=True)
        except Exception:
            pass
    cmds.connectAttr(place + ".outUV", file_node + ".uvCoord", force=True)
    cmds.connectAttr(place + ".outUvFilterSize", file_node + ".uvFilterSize", force=True)

    cmds.setAttr(file_node + ".fileTextureName", tex_path, type="string")

    # Colour management: albedo/emission = sRGB, data maps = Raw/linear.
    try:
        if slot in COLOR_SLOTS:
            cmds.setAttr(file_node + ".colorSpace", "sRGB", type="string")
        else:
            cmds.setAttr(file_node + ".colorSpace", "Raw", type="string")
            cmds.setAttr(file_node + ".ignoreColorSpaceFileRules", True)
    except Exception:
        pass

    return file_node


def _connect_scalar(file_node, shader, plug):
    """Drive a single float plug from the texture's red channel."""
    cmds.connectAttr(file_node + ".outColorR", shader + "." + plug, force=True)


def _connect_color(file_node, shader, plug):
    cmds.connectAttr(file_node + ".outColor", shader + "." + plug, force=True)


def _connect_normal(file_node, shader, plug, material_name):
    """Build the file -> bump2d(tangent-space normal) -> shader.normal chain."""
    bump = cmds.shadingNode("bump2d", asUtility=True,
                            name=_unique("{}_normal_bump2d".format(material_name)))
    # 1 = Tangent Space Normals
    cmds.setAttr(bump + ".bumpInterp", 1)
    # Normal maps are raw data; feed the red channel to bumpValue and outColor to normalmap input.
    try:
        cmds.connectAttr(file_node + ".outColorR", bump + ".bumpValue", force=True)
    except Exception:
        pass
    # Newer Maya exposes 'normalMap'/'outNormal'; connect what exists.
    if cmds.attributeQuery("outNormal", node=bump, exists=True):
        cmds.connectAttr(bump + ".outNormal", shader + "." + plug, force=True)
    else:
        cmds.connectAttr(bump + ".outNormal" if cmds.objExists(bump + ".outNormal")
                         else bump + ".outNormalX", shader + "." + plug, force=True)
    return bump


# --------------------------------------------------------------------------- #
#  Main
# --------------------------------------------------------------------------- #

def _normalize_material_name(name):
    """Ensure the material is prefixed 'M_' and never carries a texture 'T_' prefix.

    Textures are commonly named 'T_red_rock_albedo', so the derived material can
    arrive as 'M_T_red_rock' (or plain 'T_red_rock'). Collapse any leading
    T_/M_ prefixes down to a single 'M_'. Case-insensitive on the prefix.
    """
    if not name:
        return "M_material"
    stem = name
    # Peel off any leading run of 'M_' / 'T_' prefixes (in any order/case).
    changed = True
    while changed:
        changed = False
        for pfx in ("m_", "t_"):
            if stem.lower().startswith(pfx):
                stem = stem[len(pfx):]
                changed = True
    stem = stem.strip("_") or "material"
    return "M_" + stem


def build_material(preset):
    """Create the shader + shading group + texture network from a preset dict.
    Returns (shader_node, shading_group)."""
    shader_type = preset["shaderType"]
    material_name = _normalize_material_name(preset.get("materialName"))
    maps = preset.get("maps", {})

    node_type = _fallback_node_type(shader_type)
    table = SHADER_TABLE.get(shader_type, SHADER_TABLE["usdPreviewSurface"])
    plug_map = table["plugs"]

    shader = cmds.shadingNode(node_type, asShader=True, name=_unique(material_name))
    sg = cmds.sets(renderable=True, noSurfaceShader=True, empty=True,
                   name=_unique(material_name + "SG"))
    # Connect surface output to the shading group.
    out_attr = "outColor"
    cmds.connectAttr(shader + "." + out_attr, sg + ".surfaceShader", force=True)

    made = []
    for slot, tex_path in maps.items():
        slot = slot.lower()
        if not tex_path:
            continue
        # Normalise slashes for the current OS.
        tex_path = tex_path.replace("\\", "/")

        # AO on shaders without a dedicated input: skip wiring but keep a file node
        # so the artist can multiply it into baseColor manually if wanted.
        if slot not in plug_map:
            if slot == "ao":
                fn = _make_file_node(tex_path, slot, material_name)
                made.append(fn)
                cmds.warning("AO map loaded as '{}' but '{}' has no AO input; "
                             "wire it into base color manually if needed."
                             .format(fn, node_type))
            else:
                cmds.warning("Slot '{}' not supported by shader '{}', skipped."
                             .format(slot, node_type))
            continue

        plug, kind = plug_map[slot]
        # Guard: the fallback shader may not have this plug.
        if not cmds.attributeQuery(plug, node=shader, exists=True):
            cmds.warning("Shader '{}' has no plug '{}' for slot '{}', skipped."
                         .format(node_type, plug, slot))
            continue

        fn = _make_file_node(tex_path, slot, material_name)
        made.append(fn)

        if kind == "color":
            _connect_color(fn, shader, plug)
        elif kind == "scalar":
            _connect_scalar(fn, shader, plug)
        elif kind == "normal":
            _connect_normal(fn, shader, plug, material_name)

    print("[GL Map Packer] Built '{}' ({}) with {} texture node(s)."
          .format(shader, node_type, len(made)))
    return shader, sg


def import_preset(path=None, assign_to_selection=False):
    """Read the preset JSON and build the material. Optionally assign to selection.
    Returns (shader, shading_group)."""
    preset = read_preset(path)
    msg = preset.get("message")
    if msg:
        print("[GL Map Packer] {}".format(msg))

    shader, sg = build_material(preset)

    if assign_to_selection:
        sel = cmds.ls(selection=True, long=True) or []
        meshes = cmds.ls(sel, dag=True, type="mesh", long=True) or []
        targets = sel + meshes
        if targets:
            cmds.sets(targets, edit=True, forceElement=sg)
            print("[GL Map Packer] Assigned '{}' to {} object(s)."
                  .format(shader, len(set(targets))))
        else:
            cmds.warning("assign_to_selection=True but nothing suitable is selected.")

    return shader, sg


# Allow "run script" directly in the editor.
if __name__ == "__main__":
    import_preset()
