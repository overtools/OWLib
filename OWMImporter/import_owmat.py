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

def read(filename, prefix = ''):
    root, file = os.path.split(filename)
    data = read_owmat.read(filename)
    if not data: return None

    t = {}
    m = {}

    for material in data.materials:
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
                t[fn] = tex
                mattex = mat.texture_slots.add()
                mattex.texture = tex
                mattex.texture_coords = 'UV'
                mattex.use_map_color_diffuse = True
            except: pass
        m[material.key] = mat

    return (t, m)
