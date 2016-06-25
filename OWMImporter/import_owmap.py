import os

from OWMImporter import read_owmap
from OWMImporter import import_owmdl
from OWMImporter import import_owmat
from OWMImporter import owm_types
from mathutils import *
import bpy

def copy(obj, parent):
    cpy = None
    if obj.type == 'MESH':
        msh = bpy.data.meshes.new(obj.name)
        cpy = bpy.data.objects.new(obj.name, msh)
    elif obj.type == 'ARMATURE':
        arm = bpy.data.armatures.new(obj.name)
        cpy = bpy.data.objects.new(obj.name, arm)
    else:
        cpy = bpy.data.objects.new(obj.name, None)
    try:
        cpy.data = obj.data.copy()
    except: pass
    bpy.context.scene.objects.link(cpy)
    cpy.parent = parent
    cpy.hide = obj.hide
    for child in obj.children:
        copy(child, cpy)
    return cpy

def remove(obj):
    for child in obj.children:
        remove(child)
    obj.select = True
    bpy.ops.object.delete()

def read(settings):
    root, file = os.path.split(settings.filename)

    data = read_owmap.read(settings.filename)
    if not data: return None

    name = data.header.name
    if len(name) == 0:
        name = os.path.splitext(file)[0]
    rootObj = bpy.data.objects.new(name, None)
    rootObj.hide = True
    bpy.context.scene.objects.link(rootObj)

    prc = len(data.objects)
    for ob in data.objects:
        obpath = ob.model
        if not os.path.isabs(obpath):
            obpath = os.path.normpath('%s/%s' % (root, obpath))

        obn = os.path.splitext(os.path.basename(obpath))[0]
        print(obn)
        obnObj = bpy.data.objects.new(obn, None)
        obnObj.parent = rootObj
        obnObj.hide = True
        bpy.context.scene.objects.link(obnObj)

        mutated = settings.mutate(obpath)
        mutated.importMaterial = False
        bpy.ops.object.select_all(action = 'DESELECT')

        obj = import_owmdl.read(mutated, None)

        for idx, ent in enumerate(ob.entities):
            matpath = ent.material
            if not os.path.isabs(matpath):
                matpath = os.path.normpath('%s/%s' % (root, matpath))

            matObj = bpy.data.objects.new('%s_%X' % (obn, idx), None)
            matObj.parent = obnObj
            matObj.hide = True
            bpy.context.scene.objects.link(matObj)

            material = None
            if settings.importMaterial:
                material = import_owmat.read(matpath, '%s_%s:%X_' % (name, obn, idx))
                import_owmdl.bindMaterials(obj[2], obj[4], material)

            for idx2, rec in enumerate(ent.records):
                nobj = copy(obj[0], matObj)
                nobj.location = Vector(import_owmdl.xzy(rec.position))
                nobj.scale = Vector(import_owmdl.xzy(rec.scale))
                nobj.rotation_mode = 'QUATERNION'
                nobj.rotation_quaternion = (rec.rotation[3], rec.rotation[0], -rec.rotation[2], rec.rotation[1])
            bpy.ops.object.select_all(action = 'DESELECT')

        bpy.ops.object.select_all(action = 'DESELECT')
        remove(obj[0])
    bpy.context.scene.update()
