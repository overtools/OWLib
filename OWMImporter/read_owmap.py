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

    major, minor, name, objectCount = bin_ops.readFmtFlat(stream, owm_types.OWMAPHeader.structFormat)
    header = owm_types.OWMAPHeader(major, minor, name, objectCount)

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
    return owm_types.OWMAPFile(header, objects)
