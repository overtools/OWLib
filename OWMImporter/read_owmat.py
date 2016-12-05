from OWMImporter import bin_ops
from OWMImporter import owm_types
import io

def openStream(filename):
    stream = None
    with open(filename, "rb") as f:
        stream = io.BytesIO(f.read())
    return stream

def read(filename):
    stream = openStream(filename)
    if stream == None:
        return False

    major, minor, materialCount = bin_ops.readFmtFlat(stream, owm_types.OWMATHeader.structFormat)
    header = owm_types.OWMATHeader(major, minor, materialCount)

    materials = []
    for i in range(materialCount):
        key, textureCount = bin_ops.readFmtFlat(stream, owm_types.OWMATMaterial.structFormat)
        textures = []
        for j in range(textureCount):
            t = bin_ops.readFmtFlat(stream, [owm_types.OWMATMaterial.exFormat[0]])
            y = 0
            if major >= 1 and minor >= 1:
                y = ord(stream.read(1))
            textures += [(t, y)]
        materials += [owm_types.OWMATMaterial(key, textureCount, textures)]

    return owm_types.OWMATFile(header, materials)
