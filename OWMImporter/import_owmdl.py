import os

from OWMImporter import read_owmdl
from OWMImporter import import_owmat
from OWMImporter import owm_types
from mathutils import *
import bpy, mathutils, bmesh, random

root = ''
settings = None
data = None
rootObject = None
blenderBoneNames = []

def newBoneName():
    global blenderBoneNames
    blenderBoneNames = []
def addBoneName(newName):
    global blenderBoneNames
    blenderBoneNames += [newName]
def getBoneName(originalIndex):
    if originalIndex < len(blenderBoneNames):
        return blenderBoneNames[originalIndex]
    else:
        return None

def fixLength(bone):
    default_length = 0.005
    if bone.length == 0:
        bone.tail = bone.head - Vector((0, .001, 0))
    if bone.length < default_length:
        bone.length = default_length

def importArmature(autoIk):
    bones = data.bones
    armature = None
    if len(bones) > 0:
        armData = bpy.data.armatures.new("Armature")
        armData.draw_type = 'STICK'
        armature = bpy.data.objects.new("Armature", armData)
        armature.show_x_ray = True

        bpy.context.scene.objects.link(armature)

        bpy.context.scene.objects.active = armature
        bpy.ops.object.mode_set(mode='EDIT')

        newBoneName()
        for bone in bones:
            bbone = armature.data.edit_bones.new(bone.name)
            addBoneName(bbone.name)

            mpos = Matrix.Translation(xzy(bone.pos))
            mscl = Matrix.Scale(1, 4, xzy(bone.scale))
            mrot = Matrix.Rotation(bone.rot[3], 4, bone.rot[0:3])
            m = mpos * mrot * mscl

            bbone.transform(m)
            fixLength(bbone)

        for i, bone in enumerate(bones):
            if (bone.parent >= 0):
                bbone = armData.edit_bones[i]
                bbone.parent = armData.edit_bones[bone.parent]
        armature.select = True
        bpy.ops.object.mode_set(mode='OBJECT')
        armature.data.use_auto_ik = autoIk
    return armature

def xzy(vec):
    return (vec[0], -vec[2], vec[1])

def wxzy(vec):
    return (vec[3], vec[0], -vec[2], vec[1])

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

def makeVertexGroups(mesh, boneData):
    for vidx in range(len(boneData)):
        indices, weights = boneData[vidx]
        for idx in range(len(indices)):
            i = indices[idx]
            w = weights[idx]

            if w != 0:
                name = getBoneName(i)
                if name != None:
                    vgrp = mesh.vertex_groups.get(name)
                    if vgrp == None:
                        vgrp = mesh.vertex_groups.new(name)
                    vgrp.add([vidx], w, 'REPLACE')

def randomColor():
    randomR = random.random()
    randomG = random.random()
    randomB = random.random()
    return (randomR, randomG, randomB)

def bindMaterials(meshes, data, materials):
    if materials == None:
        return
    for i, obj in enumerate(meshes):
        mesh = obj.data
        meshData = data.meshes[i]
        if materials != None and meshData.materialKey in materials[1]:
            mesh.materials.clear()
            mesh.materials.append(materials[1][meshData.materialKey])

def importMesh(armature, meshData):
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

    if armature:
        mod = obj.modifiers.new(type="ARMATURE", name="Armature")
        mod.use_vertex_groups = True
        mod.object = armature
        obj.parent = armature

        makeVertexGroups(obj, boneData)

        current_theme = bpy.context.user_preferences.themes.items()[0][0]
        theme = bpy.context.user_preferences.themes[current_theme]

        bgrp = armature.pose.bone_groups.new(obj.name)
        bgrp.color_set = 'CUSTOM'
        bgrp.colors.normal = (randomColor())
        bgrp.colors.select = theme.view_3d.bone_pose
        bgrp.colors.active = theme.view_3d.bone_pose_active

        vgrps = obj.vertex_groups.keys()
        pbones = armature.pose.bones
        for bname in vgrps:
            pbones[bname].bone_group = bgrp

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


def importMeshes(armature):
    global data
    meshes = [importMesh(armature, meshData) for meshData in data.meshes]
    return meshes

def importEmpties(armature = None):
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
        bpy.ops.object.empty_add(type='CIRCLE', radius=0.05 )
        empty = bpy.context.active_object
        empty.parent = att
        empty.name = emp.name
        empty.show_x_ray = True
        empty.location = xzy(emp.position)
        empty.rotation_euler = Quaternion(wxzy(emp.rotation)).to_euler('XYZ')
        empty.select = True
        bpy.context.scene.update()
        if len(emp.hardpoint) > 0 and armature is not None:
            childOf = empty.constraints.new("CHILD_OF")
            childOf.name = "ChildOfHardpoint%s" % (empty.name)
            childOf.target = armature
            childOf.subtarget = emp.hardpoint
            bpy.context.scene.update()
            context_cpy = bpy.context.copy()
            context_cpy["constraint"] = childOf
            empty.update_tag({"DATA"})
            bpy.ops.constraint.childof_set_inverse(context_cpy, constraint=childOf.name, owner="OBJECT")
            empty.update_tag({"DATA"})
        bpy.context.scene.update()
        e += [empty]
    return e

def boneTailMiddleObject(armature):
    bpy.context.scene.objects.active = armature
    bpy.ops.object.mode_set(mode='EDIT', toggle=False)
    eb = armature.data.edit_bones
    boneTailMiddle(eb)
    bpy.ops.object.mode_set(mode='OBJECT', toggle=False)

def boneTailMiddle(eb):
    for bone in eb:
        if len(bone.children) > 0:
            bone.tail = Vector(map(sum,zip(*(child.head.xyz for child in bone.children))))/len(bone.children)
        else:
            if bone.parent != None:
                if bone.head.xyz != bone.parent.tail.xyz:
                    delta = bone.head.xyz - bone.parent.tail.xyz
                else:
                    delta = bone.parent.tail.xyz - bone.parent.head.xyz
                bone.tail = bone.head.xyz + delta
    for bone in eb:
        fixLength(bone)
        if bone.parent:
            if bone.head == bone.parent.tail:
                bone.use_connect = True

def select_all(ob):
    ob.select = True
    for obj in ob.children: select_all(obj)

def readmdl(materials = None):
    global root
    global data
    global rootObject
    root, file = os.path.split(settings.filename)

    data = read_owmdl.read(settings.filename)
    if not data: return None

    rootName = os.path.splitext(file)[0]
    if len(data.header.name) > 0:
        rootName = data.header.name

    rootObject = bpy.data.objects.new(rootName, None)
    rootObject.hide = rootObject.hide_render = True
    bpy.context.scene.objects.link(rootObject)
    bpy.context.scene.update()

    armature = None
    if settings.importSkeleton and data.header.boneCount > 0:
        armature = importArmature(settings.autoIk)
        armature.name = rootName + '_Skeleton'
        armature.parent = rootObject

    meshes = importMeshes(armature)

    impMat = False
    materials = None
    if materials == None and settings.importMaterial and len(data.header.material) > 0:
        impMat = True
        matpath = data.header.material
        if not os.path.isabs(matpath):
            matpath = os.path.normpath('%s/%s' % (root, matpath))
        materials = import_owmat.read(matpath, '', settings.importTexNormal, settings.importTexEffect)
        bindMaterials(meshes, data, materials)

    empties = []
    if settings.importEmpties and data.header.emptyCount > 0:
        empties = importEmpties(armature)

    if armature:
        boneTailMiddleObject(armature)

    if impMat:
        import_owmat.cleanUnusedMaterials(materials)
    
    bpy.ops.object.select_all(action='DESELECT')
    select_all(rootObject)

    bpy.context.scene.update()

    return (rootObject, armature, meshes, empties, data)

def read(aux, materials = None):
    global settings
    settings = aux

    setup()
    status = readmdl(materials)
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
