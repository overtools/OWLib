from inc_noesis import *
import noesis

def registerNoesisTypes():
    handle = noesis.register("Overwatch Model", ".00C")
    noesis.setHandlerTypeCheck(handle, owCheckType)
    noesis.setHandlerLoadModel(handle, owLoadModel)
    return 1

def owCheckType(data):
    if len(data) < 480: # length of header
        return 0
    bs = NoeBitStream(data)
    bs.setOffset(0x150)
    if bs.readByte() > 0x7F:
        return 0
    if bs.readByte() > 0x7F:
        return 0
    bs.setOffset(0x168)
    if bs.readUInt64() > len(data):
        return 0
    if bs.readUInt64() > len(data):
        return 0
    if bs.readUInt64() > len(data):
        return 0
    return 1

def owLoadModel(data, mdlList):
    if not owCheckType(data):
        return 0
    bs = NoeBitStream(data)

    # mesh
    meshes = {}
    mesh_desc = []
    materials = []
    
    # vertex buffer
    vbo_desc = []
    vbo_stride = []
    vbo = []
    
    # index buffer
    ibo_desc = []
    ibo = []

    # bone data
    bones = []
    bone_desc = []
    bone_mat44 = []
    bone_ids = []
    bone_lookup = []

    # init couters + offsets
    bs.setOffset(80)
    bone_desc_offset = bs.readUInt64();
    bone_mat44_offset1 = bs.readUInt64();
    bone_mat44_offset2 = bs.readUInt64();
    bone_mat43_offset1 = bs.readUInt64();
    bone_mat43_offset2 = bs.readUInt64();
    bs.setOffset(128)
    bone_id_offset = bs.readUInt64();
    bs.setOffset(152)
    bone_lookup_offset = bs.readUInt64();
    bs.setOffset(172)
    bone_count = bs.readUShort()
    bs.setOffset(178)
    bone_lookup_count = bs.readUShort()
    bs.setOffset(324)
    material_count = bs.readUShort();
    bs.setOffset(336)
    vbo_desc_count = bs.readByte()
    ibo_desc_count = bs.readByte()
    bs.setOffset(339)
    mesh_desc_count = bs.readByte()
    bs.setOffset(352)
    materials_offset = bs.readUInt64()
    mesh_desc_offset = bs.readUInt64()
    vbo_desc_offset = bs.readUInt64()
    ibo_desc_offset = bs.readUInt64()

    # init bones
    if bone_count > 0:
        bs.setOffset(bone_desc_offset)
        for i in range(0, bone_count):
            bs.readBytes(4)
            bone_desc.append(bs.readShort())
        bs.setOffset(bone_mat44_offset1)
        for i in range(0, bone_count):
            bone_mat44.append(NoeMat44.fromBytes(bs.readBytes(16 * 4)))
        bs.setOffset(bone_id_offset)
        for i in range(0, bone_count):
            bone_ids.append(bs.readUInt())
        for i in range(0, bone_count):
            pi = bone_desc[i]
            bones.append(NoeBone(i, "bone" + hex(bone_ids[i]), bone_mat44[i].toMat43(), None, pi))
        bs.setOffset(bone_lookup_offset)
        for i in range(0, bone_lookup_count):
            bone_lookup.append(bs.readUShort())
    
    # init vbo_desc
    bs.setOffset(vbo_desc_offset)
    for i in range(0, vbo_desc_count):
        vbo_desc.append(bs.read("<IIBBBBIQQQQQ"))
        vbo_stride.append([[], []])
        vbo.append([[], []])
    
    # init vbo_stride + vbo
    for i in range(0, vbo_desc_count):
        bs.setOffset(vbo_desc[i][9]);
        for j in range(0, vbo_desc[i][4]):
            stride = bs.read("<BBBBHH")
            vbo_stride[i][stride[3]].append(stride)
        for j in range(0, 2):
            count = vbo_desc[i][2 + j]
            start = vbo_desc[i][10 + j]
            for k in range(0, vbo_desc[i][0]):
                offset = start + (k * count)
                entry = []
                for l in range(0, len(vbo_stride[i][j])):
                    stride = vbo_stride[i][j][l]
                    bs.seek(offset + stride[5])
                    v = []
                    if stride[2] == 2:
                        v.append(bs.read("<fff"))
                    elif stride[2] == 4:
                        v.append((bs.readHalfFloat(), bs.readHalfFloat()))
                    elif stride[2] == 6:
                        v.append(bs.read("<BBB"))
                    elif stride[2] == 8:
                        value = bs.read("<BBB")
                        vma = []
                        for m in range(0, 3):
                            vm = value[m]
                            vm = float(vm)/255
                            vma.append(vm)
                        v.append(vma)
                    elif stride[2] == 9:
                        value = bs.read("<BBB")
                        vma = []
                        for m in range(0, 3):
                            vm = value[m]
                            if vm > 127:
                                vm = (256 - vm) * (-1)
                            vm = float(vm)/128
                            vma.append(vm)
                        v.append(vma)
                    elif stride[2] == 12:
                        v.append(bs.readUInt())
                    else:
                        continue
                    v.append(stride[0])
                    v.append(stride[1])
                    entry.append(v);
                vbo[i][j].append(entry);

    # init ibo_desc
    bs.setOffset(ibo_desc_offset)
    for i in range(0, ibo_desc_count):
        ibo_desc.append(bs.read("<IIQQ"))
        ibo.append([])

    # init ibo
    for i in range(0, ibo_desc_count):
        bs.setOffset(ibo_desc[i][3])
        for j in range(0, ibo_desc[i][0]):
            ibo[i].append(bs.readUShort())
    
    # init materials
    bs.setOffset(materials_offset);
    for i in range(0, material_count):
        materials.append(bs.readUInt64());

    # init mesh_desc
    bs.setOffset(mesh_desc_offset)
    for i in range(0, mesh_desc_count):
        mesh_desc.append(bs.read("<QI" + ("f" * 10) + "IHHHHHBBBBBB"))

    # init meshes
    for i in range(0, mesh_desc_count):
        vertex_start = mesh_desc[i][12]
        index_start = mesh_desc[i][13]
        index_count = mesh_desc[i][15]
        vertex_count = mesh_desc[i][16]
        bone_offset = mesh_desc[i][17]
        m_vbo = vbo[mesh_desc[i][18]]
        m_ibo = ibo[mesh_desc[i][19]]
        mat = mesh_desc[i][21]
        lod = mesh_desc[i][22]

        mesh_name = "Submesh_" + hex(lod) + "_" + hex(materials[mat]) + "_" + hex(i)
        mat_name = hex(materials[mat])

        faces = []
        jiggled_ibo = []
        ibo_chunk = m_ibo[index_start:index_start+index_count]
        for j in range(0, index_count, 3):
            v1, v2, v3 = ibo_chunk[j:j + 3]
            if v1 in faces:
                v1 = faces.index(v1)
            else:
                faces.append(v1)
                v1 = len(faces) - 1
            jiggled_ibo.append(v1)
            if v2 in faces:
                v2 = faces.index(v2)
            else:
                faces.append(v2)
                v2 = len(faces) - 1
            jiggled_ibo.append(v2)
            if v3 in faces:
                v3 = faces.index(v3) 
            else:
                faces.append(v3)
                v3 = len(faces) - 1
            jiggled_ibo.append(v3)

        vert = []
        norm = []
        uv = []
        weights = []
        for j in range(0, vertex_count):
            offset = vertex_start + faces[j];
            bw = []
            bi = []
            for k in range(0, 2):
                entries = m_vbo[k][offset]
                for l in range(0, len(entries)):
                    entry = entries[l]
                    typ = entry[1]
                    value = entry[0]
                    if typ == 0:
                        vert.append(NoeVec3(value))
                    elif typ == 1:
                        norm.append(NoeVec3([-value[0], -value[1], -value[2]]))
                    elif typ == 4:
                        for m in range(0, len(value)):
                            bi.append(bone_lookup[value[m] + bone_offset])
                    elif typ == 5:
                        for m in range(0, len(value)):
                            bw.append(value[m])
                    elif typ == 9:
                        if entry[2] > 0: continue # only one layer?
                        uv.append(NoeVec3([value[0], value[1], entry[2]]))
            if bone_count > 0:
                weights.append(NoeVertWeight(bi, bw))
        mesh = NoeMesh(jiggled_ibo, vert, mesh_name, mat_name)
        mesh.uvs = uv
        mesh.normals = norm
        mesh.weights = weights
        if not hex(lod) in meshes:
            meshes[hex(lod)] = []
        meshes[hex(lod)].append(mesh)
    for key in meshes:
        mdlList.append(NoeModel(meshes[key], bones))
    return 1
