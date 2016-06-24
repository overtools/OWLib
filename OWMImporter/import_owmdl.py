import os

from OWMImporter import read_owmdl
from OWMImporter import import_owmat
from OWMImporter import owm_types
from mathutils import *
import bpy, mathutils

root = ''
settings = None
data = None

def importArmature(autoIk): pass

def importMeshes(armature_ob, materials): pass

def importEmpties(): pass

def readmdl(material_dat = None):
    global root
    global data
    root, file = os.path.split(settings.filename)

    data = read_owmdl.read(settings.filename)
    if not data: return '{NONE}'

    armature_ob = None
    if settings.importSkeleton:
        armature_ob = importArmature(settings.autoIk)

    if material_dat == None and settings.importMaterial:
        material_dat = import_owmat.read(data.header.material)

    mesh_obs = importMeshes(armature_ob, material_dat)

    empties_ob = []
    if settings.importEmpties:
        empties_ob = importEmpties()

    if armature_ob:
        boneTailMiddleObject(armature_ob)

    for emp in empties_ob:
        emp.select = True

    return '{FINISHED}'

def read(aux, material_dat = None):
    global settings
    settings = aux

    setup()
    status = readmdl(material_dat)
    finalize()
    return status

def setup():
    mode()
    bpy.ops.object.select_all(action='DESELECT')

def finalize():
    mode()

def mode():
    currentMode = bpy.context.mode
    if bpy.context.scene.objects.active and currentMode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT', toggle=False)
