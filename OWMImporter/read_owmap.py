from OWMImporter import bin_ops
from OWMImporter import owm_types
import io, bpy

def openStream(filename):
    stream = None
    with open(filename, "rb") as f:
        stream = io.BytesIO(f.read())
    return stream

def read(filename):
    stream = openStream(filename)
    if stream == None:
        return False

    major, minor, name, objectCount, detailCount = bin_ops.readFmtFlat(stream, owm_types.OWMAPHeader.structFormat)
    lightCount = 0;
    if major + 0xFFFF + minor >= 0x10001:
        lightCount = bin_ops.readFmtFlat(stream, owm_types.OWMAPHeader.structFormat11)
    header = owm_types.OWMAPHeader(major, minor, name, objectCount, detailCount, lightCount)

    objects = []
    for i in range(objectCount):
        model, entityCount = bin_ops.readFmtFlat(stream, owm_types.OWMAPObject.structFormat)

        entities = []
        for j in range(entityCount):
            material, recordCount = bin_ops.readFmtFlat(stream, owm_types.OWMAPEntity.structFormat)

            records = []
            for k in range(recordCount):
                position, scale, rotation = bin_ops.readFmt(stream, owm_types.OWMAPRecord.structFormat)
                records += [owm_types.OWMAPRecord(position, scale, rotation)]
            entities += [owm_types.OWMAPEntity(material, recordCount, records)]
        objects += [owm_types.OWMAPObject(model, entityCount, entities)]
    details = []
    for i in range(detailCount):
        model, material = bin_ops.readFmtFlat(stream, owm_types.OWMAPDetail.structFormat)
        position, scale, rotation = bin_ops.readFmt(stream, owm_types.OWMAPDetail.exFormat)
        details += [owm_types.OWMAPDetail(model, material, position, scale, rotation)]
    lights = []
    if major + 0xFFFF + minor >= 0x10001:
        for i in range(lightCount):
            position, rotation, typ, fov, color  = bin_ops.readFmt(stream, owm_types.OWMAPLight.structFormat)
            ex = bin_ops.readFmtFlat(stream, owm_types.OWMAPLight.exFormat)
            lights += [owm_types.OWMAPLight(position, rotation, typ[0], fov[0], color)]
    return owm_types.OWMAPFile(header, objects, details, lights)
