using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OWLib.Types;
using OWLib.Types.Map;

namespace OWLib.Writer {
    public class SEAnimWriter : IDataWriter {
        public string Format => ".seanim";

        public char[] Identifier => new char[1] { 'S' };

        public string Name => "SEAnim";

        public WriterSupport SupportLevel => WriterSupport.BONE | WriterSupport.POSE | WriterSupport.ANIM;

        public bool Write(Animation anim, Stream output, object[] data) {
            return false;
        }

        public bool Write(Map10 physics, Stream output, object[] data) {
            return false;
        }

        public bool Write(Chunked model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] data) {
            throw new NotImplementedException();
        }

        public Dictionary<ulong, List<string>>[] Write(Stream output, Map map, Map detail1, Map detail2, Map props, Map lights, string name = "") {
            return null;
        }
    }
}
