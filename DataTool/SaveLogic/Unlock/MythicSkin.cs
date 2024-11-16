using System;
using System.Collections.Generic;
using System.IO;
using DataTool.ConvertLogic;
using DataTool.Flag;
using DataTool.Helper;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TankLib;
using TankLib.Helpers;
using TankLib.Helpers.Hash;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;
using Image = SixLabors.ImageSharp.Image;

namespace DataTool.SaveLogic.Unlock {
    public static class MythicSkin {
        public record PartTexture(byte[] data, int width, int height);

        public static void SaveMythicSkin(ICLIFlags flags, string directory, teResourceGUID mythicSkinGUID, STU_EF85B312 mythicSkin, STUHero hero) {
            var wasDeduping = Program.Flags.Deduplicate;
            if (!wasDeduping) {
                Logger.Warn("\t\tTemporarily enabling texture deduplication");
            }
            Program.Flags.Deduplicate = true;
            Logger.Log("\t\tFinding");

            var findInfo = new FindLogic.Combo.ComboInfo();
            var saveContext = new Combo.SaveContext(findInfo);
            var partTextures = LoadPartTextures(mythicSkin, findInfo);

            foreach (var partVariantIndices in IteratePermutations(mythicSkin)) {
                var variantSkinGUID = BuildVariantGUID(mythicSkinGUID, mythicSkin, partVariantIndices, 0xA6);
                //Console.Out.WriteLine(teResourceGUID.AsString(variantSkinGUID));

                var variantSkin = GetInstance<STUSkinBase>(variantSkinGUID);
                if (variantSkin == null) {
                    Logger.Warn("SkinTheme", $"couldn't load mythic skin permutation {variantSkinGUID} for {teResourceGUID.AsString(mythicSkinGUID)}. shouldn't happen");
                    return;
                }

                //Console.Out.WriteLine(variantSkin);
                var variantDirectoryName = BuildVariantName(mythicSkin, partVariantIndices);
                Logger.Debug("SkinTheme", $"Processing mythic variant {variantDirectoryName}");
                var variantDirectory = Path.Combine(directory, variantDirectoryName);

                findInfo.m_entities.Clear(); // sanity
                SkinTheme.FindEntities(findInfo, variantSkinGUID, hero);
                SaveAndFlushEntities(flags, findInfo, saveContext, variantDirectory);

                // save any sounds to main skin dir..
                // todo: there arent any. probably replacing effect. just for sanity
                SkinTheme.FindSoundFiles(flags, directory, SkinTheme.GetReplacements(variantSkinGUID));

                using var infoTexture = BuildVariantInfoImage(partVariantIndices, partTextures);
                infoTexture?.SaveAsPng(Path.Combine(variantDirectory, "Info.png"));
            }

            // todo: anim effect broken
            SkinTheme.SaveCore(flags, directory, mythicSkinGUID, findInfo);
            Program.Flags.Deduplicate = wasDeduping;
        }

        public static void SaveAndFlushEntities(ICLIFlags flags, FindLogic.Combo.ComboInfo findInfo, Combo.SaveContext saveContext, string variantDirectory) {
            foreach (var entity in findInfo.m_entities) {
                Combo.SaveEntity(flags, variantDirectory, saveContext, entity.Key, "../../..");
            }
            findInfo.m_entities.Clear();
        }

        public static teResourceGUID BuildVariantGUID(teResourceGUID mythicSkinGUID, STU_4BC3E632 mythicSkin, int[] partVariantIndices, ushort finalSkinType) {
            var hash = (ulong)mythicSkinGUID;
            for (int partIndex = 0; partIndex < partVariantIndices.Length; partIndex++) {
                var partVariantIndex = partVariantIndices[partIndex];

                var part = mythicSkin.m_942A6CCA[partIndex];
                var partVariant = part.m_57CE9041[partVariantIndex];

                var intermediateHash = CRC64NoInout(BitConverter.GetBytes(mythicSkinGUID), 0);
                intermediateHash = CRC64NoInout(BitConverter.GetBytes(part.m_id.GUID), intermediateHash);
                intermediateHash = CRC64NoInout(BitConverter.GetBytes(partVariant.m_id.GUID), intermediateHash);

                var intermediateSkinGUID = intermediateHash & 0x7FFFFFFFFFFF;
                intermediateSkinGUID |= 0x8000000000000 >> 4;
                {
                    var intermediateSkinGUID_B = new teResourceGUID(intermediateSkinGUID);
                    intermediateSkinGUID_B.SetType(teResourceGUID.Type(mythicSkinGUID)); // mythic = 0x103, mythic weapon = 0x173
                    intermediateSkinGUID = intermediateSkinGUID_B;
                }

                hash += intermediateSkinGUID;
            }

            var finalSkinGUIDRaw = hash & 0x7FFFFFFFFFFF;
            finalSkinGUIDRaw|= 0x8000000000000 >> 4;
            {
                var finalSkinGUID_B = new teResourceGUID(finalSkinGUIDRaw);
                finalSkinGUID_B.SetType(finalSkinType); // skin = 0xA6, weapon skin = 0x167
                finalSkinGUIDRaw = finalSkinGUID_B;
            }
            var finalSkinGUID = (teResourceGUID)finalSkinGUIDRaw;
            return finalSkinGUID;
        }

        public static IEnumerable<int[]> IteratePermutations(STU_4BC3E632 mythicSkin) {
            var partVariantIndices = new int[mythicSkin.m_942A6CCA.Length];

            // todo: iterative algorithm..
            // and well... this is naughty
            // retuning the same array over and over again means ToArray on the IEnumerable would be useless

            foreach (var permutation in IteratePermutations(mythicSkin, partVariantIndices, 0)) {
                yield return permutation;
            }
        }

        private static IEnumerable<int[]> IteratePermutations(STU_4BC3E632 mythicSkin, int[] partVariantIndices, int thisPartIndex) {
            if (thisPartIndex < mythicSkin.m_942A6CCA.Length) {
                var thisPart = mythicSkin.m_942A6CCA[thisPartIndex];
                for (int i = 0; i < thisPart.m_57CE9041.Length; i++) {
                    partVariantIndices[thisPartIndex] = i;
                    foreach (var permutation in IteratePermutations(mythicSkin, partVariantIndices, thisPartIndex + 1)) {
                        yield return permutation;
                    }
                }
                yield break;
            }

            // for n*m*o combinations...
            yield return partVariantIndices;
        }

        public static string BuildVariantName(STU_4BC3E632 mythicSkin, int[] partVariantIndices) {
            var variantDirectoryName = "";
            for (int partIndex = 0; partIndex < partVariantIndices.Length; partIndex++) {
                variantDirectoryName += $"{IO.GetString(mythicSkin.m_942A6CCA[partIndex].m_displayText)}-{partVariantIndices[partIndex]} ";
            }
            variantDirectoryName = variantDirectoryName.Trim();
            return variantDirectoryName;
        }

        public static Image<Rgba32> BuildVariantInfoImage(int[] partVariantIndices, PartTexture[][] partTextures)
        {
            // calculate a proper sizes for sanity...
            // for now they are all 256x256
            var widthSum = 0;
            var largestHeight = 0;
            for (int partIndex = 0; partIndex < partVariantIndices.Length; partIndex++) {
                var partVariantIndex = partVariantIndices[partIndex];
                var partTexture = partTextures[partIndex][partVariantIndex];
                if (partTexture == null) continue; // okay... weapon skin problems

                widthSum += partTexture.width;
                largestHeight = Math.Max(largestHeight, partTexture.height);
            }

            if (widthSum == 0) return null; // okay... weapon skin problems
            var infoTexture = new Image<Rgba32>(widthSum, largestHeight);

            var xPos = 0;
            for (int partIndex = 0; partIndex < partVariantIndices.Length; partIndex++) {
                var partVariantIndex = partVariantIndices[partIndex];
                var partTexture = partTextures[partIndex][partVariantIndex];
                if (partTexture == null) continue; // okay... weapon skin problems

                using Image<Bgra32> colorImage = Image.LoadPixelData<Bgra32>(partTexture.data, partTexture.width, partTexture.height);
                // ReSharper disable once AccessToDisposedClosure
                // ReSharper disable once AccessToModifiedClosure
                infoTexture.Mutate(o => o.DrawImage(colorImage, new Point(xPos, 0), 1));
                xPos += partTexture.width;
            }

            return infoTexture;
        }

        public static PartTexture[][] LoadPartTextures(STU_4BC3E632 mythicSkin, FindLogic.Combo.ComboInfo findInfo) {
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

                    var convertedTexture = new TexDecoder(texture, false);
                    partTextures[partIndex][partVariantIndex] = new PartTexture(convertedTexture.PixelData, texture.Header.Width, texture.Header.Height);
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