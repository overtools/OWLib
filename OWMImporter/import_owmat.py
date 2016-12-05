import os

from OWMImporter import read_owmat
from OWMImporter import owm_types
import bpy

def cleanUnusedMaterials(materials):
    if materials == None:
        return
    m = {}
    for name in materials[1]:
        mat = materials[1][name]
        if mat.users == 0:
            bpy.data.materials.remove(mat)
        else:
            m[name] = mat
    bpy.context.scene.update()
    t = {}
    for name in materials[0]:
        tex = materials[0][name]
        if tex.users == 0:
            bpy.data.textures.remove(tex)
        else:
            t[name] = tex
    bpy.context.scene.update()
    return (t, m)

def read(filename, prefix = '', importNormal, importEffect):
    root, file = os.path.split(filename)
    data = read_owmat.read(filename)
    if not data: return None

    t = {}
    m = {}

    for i in range(len(data.materials)):
        if i < len(data.types):
            if data.types[i] == owm_types.OWMATTypes['NORMAL'] and not importNormal: continue
            if data.types[i] == owm_types.OWMATTypes['EFFECT'] and not importEffect: continue
        material = data.materials[i]
        mat = bpy.data.materials.new('%s%016X' % (prefix, material.key))
        mat.diffuse_intensity = 1.0
        for texture in material.textures:
            realpath = texture
            if not os.path.isabs(realpath):
                realpath = os.path.normpath('%s/%s' % (root, realpath))
            try:
                fn = os.path.splitext(os.path.basename(realpath))[0]
                tex = None
                if fn in t:
                    tex = t[fn]
                else:
                    img = None
                    for eimg in bpy.data.images:
                        if eimg.name == fn or eimg.filepath == realpath:
                            img = eimg
                    if img == None:
                        img = bpy.data.images.load(realpath)
                        img.name = fn
                    tex = None
                    for etex in bpy.data.textures:
                        if etex.name == fn:
                            tex = etex
                    if tex == None:
                        tex = bpy.data.textures.new(fn, type = 'IMAGE')
                        tex.image = img
                mattex = mat.texture_slots.add()
                mattex.use_map_color_diffuse = True
                mattex.diffuse_factor = 1
                if i < len(data.types):
                    if data.types[i] == owm_types.OWMATTypes['NORMAL']:
                        tex.use_alpha = False
                        tex.use_normal_map = True
                        mattex.use_map_color_diffuse = False
                        mattex.use_map_normal = True
                        mattex.normal_factor = -1
                        mattex.diffuse_factor = 0
                mattex.texture = tex
                mattex.texture_coords = 'UV'
                t[fn] = tex
            except: pass
        m[material.key] = mat

    return (t, m)
