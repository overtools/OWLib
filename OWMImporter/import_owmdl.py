import os

from OWMImporter import read_owmdl
from OWMImporter import import_owmat
from OWMImporter import owm_types
from mathutils import *
import bpy, mathutils, bmesh

root = ''
settings = None
data = None
rootObject = None

def importArmature(autoIk): pass # TODO: Generate Armature

def xzy(vec):
    return (vec[0], -vec[2], vec[1])

def segregate(vertex):
    pos = []
    norms = []
    uvs = []
    boneData = []
    for vert in vertex:
        pos += [Vector(xzy(vert.position))]
        norm = Vector(vert.normal).normalized()
        norm[0] = -norm[0]
        norm[1] = -norm[1]
        norm[2] = -norm[2]
        norms += [xzy(norm)]
        uvs += [vert.uvs]
        boneData += [[vert.boneIndices, vert.boneWeights]]
    return (pos, norms, uvs, boneData)

def detach(faces):
    f = []
    for face in faces:
        f += [face.points]
    return f

def importMesh(armature, materials, meshData):
    global settings
    global rootObject
    mesh = bpy.data.meshes.new(meshData.name)
    obj = bpy.data.objects.new(mesh.name, mesh)
    obj.parent = rootObject
    bpy.context.scene.objects.link(obj)
    bpy.context.scene.update()

    pos, norms, uvs, boneData = segregate(meshData.vertices)
    faces = detach(meshData.indices)
    mesh.from_pydata(pos, [], faces)
    mesh.polygons.foreach_set('use_smooth', [True] * len(mesh.polygons))
    for i in range(meshData.uvCount):
        mesh.uv_textures.new(name="UV" + str(i + 1))

    bm = bmesh.new()
    bm.from_mesh(mesh)
    for fidx, face in enumerate(bm.faces):
        fraw = faces[fidx]
        for vidx, vert in enumerate(face.loops):
            ridx = fraw[vidx]
            for idx in range(len(mesh.uv_layers)):
                layer = bm.loops.layers.uv[idx]
                vert[layer].uv = Vector([uvs[ridx][idx][0] + settings.uvDisplaceX, 1 + settings.uvDisplaceY - uvs[ridx][idx][1]])
    bm.to_mesh(mesh)

    if materials != None and meshData.materialKey in materials:
        obj.materials += materials[meshData.materialKey]

        if len(obj.materials) > 0 and len(obj.materials[0].texture_slots) > 0:
            texture = obj.materials[0].texture_slots[0].texture.image
            if obj.uv_textures.active:
                for uvf in obj.uv_textures.active.data:
                    uvf.image = texture

    if armature:
        mod = obj.modifiers.new(type="ARMATURE", name="Armature")
        mod.use_vertex_groups = True
        mod.object = armature
        obj.parent = armature
        # TODO: makeVertexGroups
        # TODO: makeBoneGroups

    mesh.update()

    obj.select = True
    if settings.importNormals:
        mesh.create_normals_split()
        mesh.validate(clean_customdata = False)
        mesh.update(calc_edges = True)
        mesh.normals_split_custom_set_from_vertices(norms)
        mesh.use_auto_smooth = True
    else:
        mesh.validate()

    bpy.context.scene.update()

    return obj


def importMeshes(armature, materials):
    global data
    meshes = [importMesh(armature, materials, meshData) for meshData in data.meshes]
    return meshes

def importEmpties():
    global data
    global settings
    global rootObject

    if not settings.importEmpties:
        return []

    att = bpy.data.objects.new('Empties', None)
    att.parent = rootObject
    att.hide = att.hide_render = True
    bpy.context.scene.objects.link(att)
    bpy.context.scene.update()

    e = []
    for emp in data.empties:
        empty = bpy.data.objects.new(emp.name, None)
        bpy.context.scene.objects.link(empty)
        bpy.context.scene.update()
        empty.parent = att
        empty.location = xzy(emp.position)
        empty.rotation_mode = 'XYZ'
        empty.rotation_quaternion = emp.rotation
        empty.select = True
        bpy.context.scene.update()
        e += [empty]
    return e

def hideUnusedBones(armature): pass # TODO

def boneTailMiddleObject(armature): pass # TODO

def readmdl(material_dat = None):
    global root
    global data
    global rootObject
    root, file = os.path.split(settings.filename)

    data = read_owmdl.read(settings.filename)
    if not data: return '{NONE}'

    if len(data.header.name) > 0:
        rootObject = bpy.data.objects.new(data.header.name, None)
        rootObject.hide = rootObject.hide_render = True
        bpy.context.scene.objects.link(rootObject)
        bpy.context.scene.update()

    armature = None
    if settings.importSkeleton:
        armature = importArmature(settings.autoIk)

    if material_dat == None and settings.importMaterial:
        material_dat = import_owmat.read(data.header.material)

    mesh_obs = importMeshes(armature, material_dat)

    empties_ob = []
    if settings.importEmpties:
        empties_ob = importEmpties()

    if armature:
        hideUnusedBones(armature)
        boneTailMiddleObject(armature)

    bpy.context.scene.update()

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

if __name__ == '__main__':
    settings = owm_types.OWSettings('C:\\ow\\overtooltest\\D.Va\\Skin\\Classic\\0000000011CE.owmdl', 0, 0, True, True, True, True, True)
    read(settings)
