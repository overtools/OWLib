
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TankLib.Test
{
    [TestClass]
    public class teTextureTest
    {
        public byte[] TestPayloadNoSurfaceBytes = new byte[] {
            //    Unknown1          Surfaces    Payloads    Width
            // Flags    Mips  Format      Unknown2    Unknown3          Height      DataSize
            0x02, 0x00, 0x01, 0x3C, 0x01, 0x00, 0x04, 0x00, 0x04, 0x00, 0x04, 0x00, 0x00, 0x00, 
            // Reference                                    Unknown4
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        [TestMethod]
        public void TestGetPayloadGUID()
        {
            using (var ms = new MemoryStream(TestPayloadNoSurfaceBytes) {Position = 0}) {
                var tex = new teTexture(ms);

                const ulong baseGuid = 0x0C00000000001234UL;
                Assert.AreEqual(0x0320000300001234UL, tex.GetPayloadGUID(baseGuid, 1, 0), "GetPayloadGuid(1, 0) != 000300001234.04D");
                Assert.AreEqual(0x0320000200001234UL, tex.GetPayloadGUID(baseGuid, 1, 1), "GetPayloadGuid(1, 1) != 000200001234.04D");
                Assert.AreEqual(0x0320000100001234UL, tex.GetPayloadGUID(baseGuid, 1, 2), "GetPayloadGuid(1, 2) != 000100001234.04D");
                Assert.AreEqual(0x0320000000001234UL, tex.GetPayloadGUID(baseGuid, 1, 3), "GetPayloadGuid(1, 3) != 000000001234.04D");
            }
        }
    }
}
