import os

from OWMImporter import read_owmap
from OWMImporter import import_owmdl
from OWMImporter import import_owmat
from OWMImporter import owm_types
from mathutils import *
import bpy

def copy(obj, parent):
    cpy = bpy.data.objects.new(obj.name, obj.data)
    bpy.context.scene.objects.link(cpy)
    cpy.parent = parent
    cpy.hide = obj.hide
    for child in obj.children:
        copy(child, cpy)
    return cpy

def remove(obj):
    for child in obj.children:
        remove(child)
    try:
        bpy.context.scene.objects.unlink(obj)
    except: pass

def read(settings, importObjects = False, importDetails = True, importPhysics = False):
    root, file = os.path.split(settings.filename)

    data = read_owmap.read(settings.filename)
    if not data: return None

    name = data.header.name
    if len(name) == 0:
        name = os.path.splitext(file)[0]
    rootObj = bpy.data.objects.new(name, None)
    rootObj.hide = True
    bpy.context.scene.objects.link(rootObj)

    if importObjects:
        for ob in data.objects:
            obpath = ob.model
            if not os.path.isabs(obpath):
                obpath = os.path.normpath('%s/%s' % (root, obpath))

            obn = os.path.splitext(os.path.basename(obpath))[0]
            print(obn)

            mutated = settings.mutate(obpath)
            mutated.importMaterial = False
            mutated.importSkeleton = False

            obj = import_owmdl.read(mutated, None)

            for idx, ent in enumerate(ob.entities):
                matpath = ent.material
                if not os.path.isabs(matpath):
                    matpath = os.path.normpath('%s/%s' % (root, matpath))

                material = None
                if settings.importMaterial:
                    material = import_owmat.read(matpath, '%s_%s:%X_' % (name, obn, idx))
                    import_owmdl.bindMaterials(obj[2], obj[4], material)

                for idx2, rec in enumerate(ent.records):
                    nobj = copy(obj[0], rootObj)
                    nobj.location = import_owmdl.xzy(rec.position)
                    nobj.scale = import_owmdl.xzy(rec.scale)
                    nobj.rotation_mode = 'QUATERNION'
                    nobj.rotation_quaternion = import_owmdl.wxzy(rec.rotation)
                    nobj.rotation_mode = 'XYZ'
            remove(obj[0])

    if importDetails:
        objCache = {}
        for ob in data.details:
            obpath = ob.model
            if not os.path.isabs(obpath):
                obpath = os.path.normpath('%s/%s' % (root, obpath))

            obn = os.path.splitext(os.path.basename(obpath))[0]
            if not importPhysics and obn == 'physics':
                continue

            mutated = settings.mutate(obpath)
            mutated.importMaterial = False
            mutated.importSkeleton = False

            if len(ob.material) == 0:
                mutated.importNormals = False

            objnode = None
            if obn not in objCache:
                obj = import_owmdl.read(mutated, None)
                objCache[obn] = obj
                objnode = obj[0]
                print(obn)
            else:
                objnode = copy(objCache[obn][0], rootObj)

            material = None
            if settings.importMaterial:
                material = import_owmat.read(matpath, '%s_%s' % (name, obn))
                import_owmdl.bindMaterials(obj[2], obj[4], material)

            objnode.location = import_owmdl.xzy(ob.position)
            objnode.scale = import_owmdl.xzy(ob.scale)
            objnode.rotation_mode = 'QUATERNION'
            objnode.rotation_quaternion = import_owmdl.wxzy(ob.rotation)
            objnode.rotation_mode = 'XYZ'
            objnode.parent = rootObj
    bpy.context.scene.update()

if __name__ == '__main__':
    read(owm_types.OWSettings('C:\\ow\\overtooltest\\HANAMURA\\165\HANAMURA.owmap', 0, 0, True, True, True, False, True), True, True, True)
