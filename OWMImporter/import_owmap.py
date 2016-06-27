import os

from OWMImporter import read_owmap
from OWMImporter import import_owmdl
from OWMImporter import import_owmat
from OWMImporter import owm_types
from mathutils import *
import bpy, bpy_extras, mathutils

acm = bpy_extras.io_utils.axis_conversion(from_forward='-Z', from_up='Y').to_4x4()

def posMatrix(pos):
    global acm
    posMtx = mathutils.Matrix.Translation(pos)
    mtx = acm * posMtx
    return mtx.to_translation()

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

    globObj = bpy.data.objects.new(name + '_OBJECTS', None)
    globObj.hide = True
    globObj.parent = rootObj
    bpy.context.scene.objects.link(globObj)

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

            obnObj = bpy.data.objects.new(obn + '_COLLECTION', None)
            obnObj.hide = True
            obnObj.parent = globObj
            bpy.context.scene.objects.link(obnObj)

            for idx, ent in enumerate(ob.entities):
                matpath = ent.material
                if not os.path.isabs(matpath):
                    matpath = os.path.normpath('%s/%s' % (root, matpath))

                material = None
                if settings.importMaterial:
                    material = import_owmat.read(matpath, '%s_%s:%X_' % (name, obn, idx))
                    import_owmdl.bindMaterials(obj[2], obj[4], material)

                matObj = bpy.data.objects.new(obn + '_' + os.path.splitext(os.path.basename(matpath))[0], None)
                matObj.hide = True
                matObj.parent = obnObj
                bpy.context.scene.objects.link(matObj)

                for idx2, rec in enumerate(ent.records):
                    nobj = copy(obj[0], matObj)
                    nobj.location = posMatrix(rec.position)
                    nobj.rotation_euler = Quaternion(import_owmdl.wxzy(rec.rotation)).to_euler('XYZ')
                    nobj.scale = rec.scale
            remove(obj[0])

    globDet = bpy.data.objects.new(name + '_DETAILS', None)
    globDet.hide = True
    globDet.parent = rootObj
    bpy.context.scene.objects.link(globDet)

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
            print(obn)
            if obn not in objCache:
                obj = import_owmdl.read(mutated, None)
                objCache[obn] = obj
                objnode = obj[0]
            else:
                objnode = copy(objCache[obn][0], globDet)

            material = None
            if settings.importMaterial:
                material = import_owmat.read(matpath, '%s_%s' % (name, obn))
                import_owmdl.bindMaterials(obj[2], obj[4], material)

            objnode.location = posMatrix(ob.position)
            objnode.rotation_euler = Quaternion(import_owmdl.wxzy(ob.rotation)).to_euler('XYZ')
            objnode.scale = import_owmdl.xzy(ob.scale)
            objnode.parent = globDet
    bpy.context.scene.update()

if __name__ == '__main__':
    read(owm_types.OWSettings('C:\\ow\\overtooltest\\HANAMURA\\165\\HANAMURA.owmap', 0, 0, True, True, True, False, True), False, True, False)
