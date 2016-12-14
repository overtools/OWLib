import os

from OWMImporter import read_owmap
from OWMImporter import import_owmdl
from OWMImporter import import_owmat
from OWMImporter import owm_types
from mathutils import *
import bpy, bpy_extras, mathutils

sameMeshData = False
sets = None
acm = bpy_extras.io_utils.axis_conversion(from_forward='-Z', from_up='Y').to_4x4()

def posMatrix(pos):
    global acm
    posMtx = mathutils.Matrix.Translation(pos)
    mtx = acm * posMtx
    return mtx.to_translation()

def select_all(obj):
    obj.select = True
    for child in obj.children:
        select_all(child)

def copy(obj, parent, deep = False):
    global sameMeshData
    if not deep:
        cpy = None
        if sameMeshData:
            cpy = bpy.data.objects.new(obj.name, obj.data)
        else:
            if obj.data != None:
                cpy = bpy.data.objects.new(obj.name, obj.data.copy())
            else:
                cpy = bpy.data.objects.new(obj.name, None)
        bpy.context.scene.objects.link(cpy)
        try:
            cpy.parent = parent
        except: pass
        cpy.hide = obj.hide
        for child in obj.children:
            copy(child, cpy)
        return cpy
    else:
        v = obj.hide
        obj.hide = False
        bpy.context.scene.objects.active = obj
        bpy.ops.object.mode_set(mode='OBJECT', toggle=False)
        bpy.ops.object.select_all(action='DESELECT')
        select_all(obj)
        bpy.ops.object.duplicate()
        try:
            bpy.context.active_object.parent = parent
        except: pass
        bpy.context.active_object.hide = v
        obj.hide = v
        return bpy.context.active_object

def remove(obj):
    for child in obj.children:
        remove(child)
    try:
        bpy.context.scene.objects.unlink(obj)
    except: pass

def xpzy(vec):
    return (vec[0], vec[2], vec[1])

def read(settings, importObjects = False, importDetails = True, importPhysics = False, sMD = False, reimport = True, importLights = True):
    global sets
    global sameMeshData
    sets = settings
    sameMeshData = sMD

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

    matCache = {}

    if importObjects:
        total = len(data.objects)
        total_C = 0
        for ob in data.objects:
            obpath = ob.model
            if not os.path.isabs(obpath):
                obpath = os.path.normpath('%s/%s' % (root, obpath))

            obn = os.path.splitext(os.path.basename(obpath))[0]
            print("%s (%d%%)" % (obn, (total_C/total) * 100))
            total_C = total_C + 1

            mutated = settings.mutate(obpath)
            mutated.importMaterial = False

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
                if settings.importMaterial and len(ent.material) > 0:
                    if matpath not in matCache:
                        material = import_owmat.read(matpath, '%s:%X_' % (name, idx), settings.importTexNormal, settings.importTexEffect)
                        import_owmdl.bindMaterials(obj[2], obj[4], material)
                        matCache[matpath] = material
                    else:
                        material = matCache[matpath]
                        import_owmdl.bindMaterials(obj[2], obj[4], material)

                matObj = bpy.data.objects.new(obn + '_' + os.path.splitext(os.path.basename(matpath))[0], None)
                matObj.hide = True
                matObj.parent = obnObj
                bpy.context.scene.objects.link(matObj)

                for idx2, rec in enumerate(ent.records):
                    nobj = copy(obj[0], matObj)
                    nobj.location = posMatrix(rec.position)
                    nobj.rotation_euler = Quaternion(import_owmdl.wxzy(rec.rotation)).to_euler('XYZ')
                    nobj.scale = xpzy(rec.scale)
            remove(obj[0])

    globDet = bpy.data.objects.new(name + '_DETAILS', None)
    globDet.hide = True
    globDet.parent = rootObj
    bpy.context.scene.objects.link(globDet)

    if importDetails:
        objCache = {}
        total = len(data.details)
        total_C = 0
        for ob in data.details:
            obpath = ob.model
            if not os.path.isabs(obpath):
                obpath = os.path.normpath('%s/%s' % (root, obpath))

            obn = os.path.splitext(os.path.basename(obpath))[0]
            if not importPhysics and obn == 'physics':
                continue

            mutated = settings.mutate(obpath)
            mutated.importMaterial = False

            if len(ob.material) == 0:
                mutated.importNormals = False

            print("%s (%d%%)" % (obn, (total_C/total) * 100))
            total_C = total_C + 1
            obj = None
            if not reimport:
                if obn not in objCache:
                    objCache[obn] = import_owmdl.read(mutated, None)
                obj = objCache[obn]
            else:
                obj = import_owmdl.read(mutated, None)

            material = None
            if settings.importMaterial and len(ob.material) > 0:
                man = '%s_' % (name)
                if man not in matCache:
                    matpath = ob.material
                    if not os.path.isabs(matpath):
                        matpath = os.path.normpath('%s/%s' % (root, matpath))
                    material = import_owmat.read(matpath, man, settings.importTexNormal, settings.importTexEffect)
                    import_owmdl.bindMaterials(obj[2], obj[4], material)
                    matCache[man] = material
                else:
                    material = matCache[man]
                    import_owmdl.bindMaterials(obj[2], obj[4], material)
            objnode = None
            if not reimport:
                objnode = copy(obj[0], globDet, settings.importSkeleton)
            else:
                objnode = obj[0]
            objnode.location = posMatrix(ob.position)
            objnode.rotation_euler = Quaternion(import_owmdl.wxzy(ob.rotation)).to_euler('XYZ')
            objnode.scale = xpzy(ob.scale)
        for ob in objCache:
            remove(objCache[ob][0])

    if importLights:
        total = len(data.lights)
        total_C = 0
        for light in data.lights:
            print("light, fov: %s, type: %s (%d%%)" % (light.fov, light.type, (total_C/total) * 100))
            total_C = total_C + 1
            lamp_data = bpy.data.lamps.new(name = "%s_LAMP" % (name), type = 'POINT')
            lamp_ob = bpy.data.objects.new(name = "%s_LAMP" % (name), object_data = lamp_data)
            bpy.context.scene.objects.link(lamp_ob)
            lamp_ob.location = posMatrix(light.position)
            lamp_ob.rotation_euler = Quaternion(import_owmdl.wxzy(light.rotation)).to_euler('XYZ')
            lamp_data.color = light.color

    for man in matCache:
        try:
            import_owmat.cleanUnusedMaterials(matCache[man])
        except: pass
    bpy.context.scene.update()
