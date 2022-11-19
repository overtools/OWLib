using System;
using System.IO;
using DataTool.ConvertLogic;
using DataTool.Flag;
using DataTool.Helper;
using DirectXTexNet;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TACTLib.Helpers;
using TankLib;
using TankLib.Helpers.Hash;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;
using Image = SixLabors.ImageSharp.Image;
using Logger = TACTLib.Logger;

namespace DataTool.SaveLogic.Unlock {
    public static class MythicSkin {
        private record PartTexture(byte[] data, int width, int height);

        public static void SaveMythicSkin(ICLIFlags flags, string directory, ulong guid, STU_EF85B312 mythicSkin, STUHero hero) {
            var partVariantIndices = new int[mythicSkin.m_942A6CCA.Length];

            var findInfo = new FindLogic.Combo.ComboInfo();
            var saveContext = new Combo.SaveContext(findInfo);

            var partTextures = LoadPartTextures(mythicSkin, findInfo);

            void SavePermutation() {
                var hash = CRC64NoInout(BitConverter.GetBytes(guid), 0);
                for (int partIndex = 0; partIndex < partVariantIndices.Length; partIndex++) {
                    var partVariantIndex = partVariantIndices[partIndex];

                    var part = mythicSkin.m_942A6CCA[partIndex];
                    var partVariant = part.m_57CE9041[partVariantIndex];

                    var intermediateHash = CRC64NoInout(BitConverter.GetBytes(guid), 0);
                    intermediateHash = CRC64NoInout(BitConverter.GetBytes(part.m_id.GUID), intermediateHash);
                    intermediateHash = CRC64NoInout(BitConverter.GetBytes(partVariant.m_id.GUID), intermediateHash);

                    var intermediateSkinGUID = intermediateHash & 0x7FFFFFFFFFFF;
                    intermediateSkinGUID |= 0x8000000000000 >> 4;
                    {
                        var intermediateSkinGUID_B = new teResourceGUID(intermediateSkinGUID);
                        intermediateSkinGUID_B.SetType(0x103);
                        intermediateSkinGUID = intermediateSkinGUID_B;
                    }

                    hash = CRC64NoInout(BitConverter.GetBytes(intermediateSkinGUID), hash);
                }

                var finalSkinGUID = hash & 0x7FFFFFFFFFFF;
                finalSkinGUID |= 0x8000000000000 >> 4;
                {
                    var finalSkinGUID_B = new teResourceGUID(finalSkinGUID);
                    finalSkinGUID_B.SetType(0xA6);
                    finalSkinGUID = finalSkinGUID_B;
                }

                //Console.Out.WriteLine(teResourceGUID.AsString(finalSkinGUID));

                var variantSkin = GetInstance<STUSkinBase>(finalSkinGUID);
                if (variantSkin == null) {
                    Logger.Warn("SkinTheme", $"couldn't load mythic skin permutation {teResourceGUID.AsString(finalSkinGUID)} for {teResourceGUID.AsString(guid)}. shouldn't happen");
                    return;
                }

                //Console.Out.WriteLine(variantSkin);

                findInfo.m_entities.Clear();
                SkinTheme.FindEntities(findInfo, variantSkin, hero);

                var variantDirectoryName = "";
                for (int partIndex = 0; partIndex < partVariantIndices.Length; partIndex++) {
                    variantDirectoryName += $"{IO.GetString(mythicSkin.m_942A6CCA[partIndex].m_displayText)}-{partVariantIndices[partIndex]} ";
                }
                variantDirectoryName = variantDirectoryName.Trim();

                var variantDirectory = Path.Combine(directory, variantDirectoryName);
                foreach (var entity in findInfo.m_entities) {
                    Combo.SaveEntity(flags, variantDirectory, saveContext, entity.Key, "../../..");
                }
                findInfo.m_entities.Clear();

                // save any sounds to main skin dir..
                // todo: there arent any. probably replacing effect. just for sanity
                SkinTheme.FindSoundFiles(flags, directory, SkinTheme.GetReplacements(variantSkin));

                // calculate a proper sizes for sanity...
                // for now they are all 256x256
                var widthSum = 0;
                var largestHeight = 0;
                for (int partIndex = 0; partIndex < partVariantIndices.Length; partIndex++) {
                    var partVariantIndex = partVariantIndices[partIndex];
                    var partTexture = partTextures[partIndex][partVariantIndex];

                    widthSum += partTexture.width;
                    largestHeight = Math.Max(largestHeight, partTexture.height);
                }

                using var infoTexture = new Image<Rgba32>(widthSum, largestHeight);

                var xPos = 0;
                for (int partIndex = 0; partIndex < partVariantIndices.Length; partIndex++) {
                    var partVariantIndex = partVariantIndices[partIndex];
                    var partTexture = partTextures[partIndex][partVariantIndex];

                    using Image<Rgba32> colorImage = Image.Load<Rgba32>(partTexture.data);
                    // ReSharper disable once AccessToDisposedClosure
                    // ReSharper disable once AccessToModifiedClosure
                    infoTexture.Mutate(o => o.DrawImage(colorImage, new Point(xPos, 0), 1));
                    xPos += partTexture.width;
                }
                infoTexture.SaveAsPng(Path.Combine(variantDirectory, "Info.png"));
            }

            void PermuteMythic(int thisPartIndex) {
                if (thisPartIndex < mythicSkin.m_942A6CCA.Length) {
                    var thisPart = mythicSkin.m_942A6CCA[thisPartIndex];
                    for (int i = 0; i < thisPart.m_57CE9041.Length; i++) {
                        partVariantIndices[thisPartIndex] = i;
                        PermuteMythic(thisPartIndex + 1);
                    }
                    return;
                }
                // for n*m*o combinations...
                SavePermutation();
            }

            Helper.Logger.LoudLog("\t\tFinding");
            PermuteMythic(0);

            // todo: anim effect broken
            SkinTheme.SaveCore(flags, directory, mythicSkin, findInfo);
        }

        private static PartTexture[][] LoadPartTextures(STU_EF85B312 mythicSkin, FindLogic.Combo.ComboInfo findInfo) {
            var partTextures = new PartTexture[mythicSkin.m_942A6CCA.Length][];
            for (int partIndex = 0; partIndex < partTextures.Length; partIndex++) {
                var part = mythicSkin.m_942A6CCA[partIndex];

                partTextures[partIndex] = new PartTexture[part.m_57CE9041.Length];

                for (int partVariantIndex = 0; partVariantIndex < part.m_57CE9041.Length; partVariantIndex++) {
                    var partVariant = part.m_57CE9041[partVariantIndex];
                    FindLogic.Combo.Find(findInfo, partVariant.m_texture); // for loose

                    teTexture texture;
                    using (Stream textureStream = IO.OpenFile(partVariant.m_texture)) {
                        if (textureStream == null) continue;
                        texture = new teTexture(textureStream);
                    }

                    using var convertedStream = texture.SaveToDDS(1);
                    using var dds = new DDSConverter(convertedStream, DXGI_FORMAT.R8G8B8A8_UNORM, false);
                    using var whyIsThisAStream = dds.GetFrame(WICCodecs.PNG, 0, 1); // "png" pepega
                    var theArray = new byte[whyIsThisAStream.Length];
                    whyIsThisAStream.DefinitelyRead(theArray);

                    partTextures[partIndex][partVariantIndex] = new PartTexture(theArray, texture.Header.Width, texture.Header.Height);
                }
            }

            return partTextures;
        }

        private static ulong CRC64NoInout(ReadOnlySpan<byte> span, ulong crc=0) {
            // just cos our normal crc64 is dank

            //Console.Out.WriteLine(Encoding.ASCII.GetString(span));
            foreach (var b in span) {
                crc = CRC.CRC64Tab[(crc ^ b) & 0xFF] ^ (crc >> 8);
            }
            return crc;
        }
    }
}