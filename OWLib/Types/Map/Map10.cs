using System.IO;

namespace OWLib.Types.Map {
    public class Map10 {
        public bool HasSTUD => false;

        private MapPhysicsHeader header;
        private MapPhysicsFooter footer;
        private MapPhysicsVertex[] vertices;
        private MapPhysicsIndex[] indices;
        private MapPhysicsBoundingBox[] boundingBoxes;

        public MapPhysicsHeader Header => header;
        public MapPhysicsFooter Footer => footer;
        public MapPhysicsVertex[] Vertices => vertices;
        public MapPhysicsIndex[] Indices => indices;
        public MapPhysicsBoundingBox[] BoundingBoxes => boundingBoxes;

        public Map10(Stream input) {
            using (BinaryReader reader = new BinaryReader(input)) {
                header = reader.Read<MapPhysicsHeader>();
                input.Position = (long)header.footerOffset;
                footer = reader.Read<MapPhysicsFooter>();

                vertices = new MapPhysicsVertex[footer.vertexCount];
                input.Position = (long)footer.vertexOffset;
                for (uint i = 0; i < footer.vertexCount; ++i) {
                    vertices[i] = reader.Read<MapPhysicsVertex>();
                }

                indices = new MapPhysicsIndex[footer.indexCount];
                input.Position = (long)footer.indexOffset;
                for (uint i = 0; i < footer.indexCount; ++i) {
                    indices[i] = reader.Read<MapPhysicsIndex>();
                }

                boundingBoxes = new MapPhysicsBoundingBox[footer.bboxCount];
                input.Position = (long)footer.bboxOffset;
                for (uint i = 0; i < footer.bboxCount; ++i) {
                    boundingBoxes[i] = reader.Read<MapPhysicsBoundingBox>();
                }
            }
        }
    }
}
