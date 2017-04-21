import mathutils

OWMATTypes = {
    "ALBEDO": 0x00,
    "NORMAL": 0x01,
    "SHADER": 0x02
}

class OWSettings:
    def __init__(self, filename, uvDisplaceX, uvDisplaceY, autoIk, importNormals, importEmpties, importMaterial, importSkeleton, importTexNormal, importTexEffect):
        self.filename = filename
        self.uvDisplaceX = uvDisplaceX
        self.uvDisplaceY = uvDisplaceY
        self.autoIk = autoIk
        self.importNormals = importNormals
        self.importEmpties = importEmpties
        self.importMaterial = importMaterial
        self.importSkeleton = importSkeleton
        self.importTexNormal = importTexNormal
        self.importTexEffect = importTexEffect

    def mutate(self, filename):
        return OWSettings(filename, self.uvDisplaceX, self.uvDisplaceY, self.autoIk, self.importNormals, self.importEmpties, self.importMaterial, self.importSkeleton, self.importTexNormal, self.importTexEffect)

class OWMDLFile:
    def __init__(self, header, bones, meshes, empties):
        self.header = header
        self.bones = bones
        self.meshes = meshes
        self.empties = empties

class OWMATFile:
    def __init__(self, header, materials):
        self.header = header
        self.materials = materials

class OWMAPFile:
    def __init__(self, header, objects, details, lights = []):
        self.header = header
        self.objects = objects
        self.details = details
        self.lights = lights

class OWMDLHeader:
    structFormat = ['<HH', str, str, '<HII']
    def __init__(self, major, minor, material, name, boneCount, meshCount, emptyCount):
        self.major = major
        self.minor = minor
        self.material = material
        self.name = name
        self.boneCount = boneCount
        self.meshCount = meshCount
        self.emptyCount = emptyCount

class OWMATHeader:
    structFormat = ['<HHQ']
    def __init__(self, major, minor, materialCount):
        self.major = major
        self.minor = minor
        self.materialCount = materialCount

class OWMAPHeader:
    structFormat = ['<HH', str, '<II']
    structFormat11 = ['<I']
    def __init__(self, major, minor, name, objectCount, detailCount, lightCount = 0):
        self.major = major
        self.minor = minor
        self.name = name
        self.objectCount = objectCount
        self.detailCount = detailCount
        self.lightCount = lightCount

class OWMDLBone:
    structFormat = [str, '<h', '<fff', '<fff', '<ffff']
    def __init__(self, name, parent, pos, scale, rot):
        self.name = name
        self.parent = parent
        self.pos = pos
        self.scale = scale
        self.rot = rot

class OWMDLMesh:
    structFormat = [str, '<QBII']
    def __init__(self, name, materialKey, uvCount, vertexCount, indexCount, vertices, indices):
        self.name = name
        self.materialKey = materialKey
        self.uvCount = uvCount
        self.vertexCount = vertexCount
        self.indexCount = indexCount
        self.vertices = vertices
        self.indices = indices

class OWMDLVertex:
    structFormat = ['<fff', '<fff']
    exFormat = ['<ff', 'B', '<H', '<f']
    def __init__(self, position, normal, uvs, boneCount, boneIndices, boneWeights):
        self.position = position
        self.normal = normal
        self.uvs = uvs
        self.boneCount = boneCount
        self.boneIndices = boneIndices
        self.boneWeights = boneWeights

class OWMDLIndex:
    structFormat = ['B']
    exFormat = ['<I']
    def __init__(self, pointCount, points):
        self.pointCount = pointCount
        self.points = points

class OWMDLEmpty:
    structFormat = [str, '<fff', '<ffff']
    exFormat = [str]
    def __init__(self, name, position, rotation, hardpoint = ""):
        self.name = name
        self.position = position
        self.rotation = rotation
        self.hardpoint = hardpoint

class OWMATMaterial:
    structFormat = ['<QI']
    exFormat = [str]
    def __init__(self, key, textureCount, textures):
        self.key = key
        self.textureCount = textureCount
        self.textures = textures

class OWMAPObject:
    structFormat = [str, '<I']
    def __init__(self, model, entityCount, entities):
        self.model = model
        self.entityCount = entityCount
        self.entities = entities

class OWMAPEntity:
    structFormat = [str, '<I']
    def __init__(self, material, recordCount, records):
        self.material = material
        self.recordCount = recordCount
        self.records = records

class OWMAPRecord:
    structFormat = ['<fff', '<fff', '<ffff']
    def __init__(self, position, scale, rotation):
        self.position = position
        self.scale = scale
        self.rotation = rotation

class OWMAPDetail:
    structFormat = [str, str]
    exFormat = ['<fff', '<fff', '<ffff']
    def __init__(self, model, material, position, scale, rotation):
        self.model = model
        self.material = material
        self.position = position
        self.scale = scale
        self.rotation = rotation

class OWMAPLight:
    structFormat = ['<fff', '<ffff', '<I', '<f', '<fff']
    exFormat = ['<IIBBBBII', '<fff', '<ffff', '<fff', '<ffff', '<fff', '<ffff', '<ffIHHII']
    def __init__(self, position, rotation, typ, fov, color):
        self.position = position
        self.rotation = rotation
        self.type = typ
        self.fov = fov
        self.color = color
