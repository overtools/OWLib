import os

from OWMImporter import read_owmap
from OWMImporter import import_owmdl
from OWMImporter import import_owmat
import bpy

def read(settings):
    root, file = os.path.split(settings.filename)

    data = read_owmap.read(settings.filename)
    if not data: return None

    name = data.header.name
    if len(name) == 0:
        name = os.path.splitext(file)[0]
    rootObj = bpy.data.objects.new(name, None)
    rootObj.hide = True
    bpy.context.scene.link(rootObj)

    for ob in data.objects:
        obpath = ob.model
        if not os.path.isabs(obpath):
            obpath = os.path.normpath('%s/%s', root, obpath)

        obn = os.path.splitext(os.path.basename(obpath))[0]
        obnObj = bpy.data.objects.new(obn, None)
        obnObj.parent = rootObj
        bpy.context.scene.link(obnObj)

        mutated = settings.mutate(obpath)
        mutated.importMaterial = False

        for idx, ent in enumerate(ob.entities):
            matpath = ent.material
            if not os.path.isabs(matpath):
                matpath = os.path.normpath('%s/%s', root, matpath)

            matObj = bpy.data.objects.new('%s_%X' % (obn, idx), None)
            matObj.parent = obnObj
            bpy.context.scene.link(matObj)

            material = import_owmat.read(matpath, '%s_%s:%X_' % (name, obn, idx))

            obj = import_owmdl.read(mutated, material)

            for idx2, rec in enumerate(ent.records):
                nobj = obj.copy()
                nobj.position = Vector(rec.position)
                nobj.scale = Vector(rec.scale)
                noby.rotation_mode = 'XYZ'
                nobj.rotation_quaternion = rec.rotation
                nobj.parent = matObj
            bpy.data.objects.remove(obj)
    bpy.context.scene.update()
