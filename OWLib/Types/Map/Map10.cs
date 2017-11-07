using System.IO;

namespace OWLib.Types.Map {
    public class Map10 {
        public bool HasSTUD => false;

        public MapPhysicsHeader Header { get; }
        public MapPhysicsFooter Footer { get; }
        public MapPhysicsVertex[] Vertices { get; }
        public MapPhysicsIndex[] Indices { get; }
        public MapPhysicsBoundingBox[] BoundingBoxes { get; }

        public Map10(Stream input) {
            using (BinaryReader reader = new BinaryReader(input)) {
                Header = reader.Read<MapPhysicsHeader>();
                input.Position = (long)Header.footerOffset;
                Footer = reader.Read<MapPhysicsFooter>();

                Vertices = new MapPhysicsVertex[Footer.vertexCount];
                input.Position = (long)Footer.vertexOffset;
                for (uint i = 0; i < Footer.vertexCount; ++i) {
                    Vertices[i] = reader.Read<MapPhysicsVertex>();
                }

                Indices = new MapPhysicsIndex[Footer.indexCount];
                input.Position = (long)Footer.indexOffset;
                for (uint i = 0; i < Footer.indexCount; ++i) {
                    Indices[i] = reader.Read<MapPhysicsIndex>();
                }

                BoundingBoxes = new MapPhysicsBoundingBox[Footer.bboxCount];
                input.Position = (long)Footer.bboxOffset;
                for (uint i = 0; i < Footer.bboxCount; ++i) {
                    BoundingBoxes[i] = reader.Read<MapPhysicsBoundingBox>();
                }
            }
        }
    }
}
