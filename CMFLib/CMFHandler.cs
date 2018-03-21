using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace CMFLib {
    public class CMFHandler {
        #region Helpers
        // ReSharper disable once InconsistentNaming
        internal const uint SHA1_DIGESTSIZE = 20;
        
        internal static uint Constrain(long value) {
            return (uint)(value % uint.MaxValue);
        }
        
        internal static long SignedMod(long a, long b) {
            return a % b < 0 ? a % b + b : a % b;
        }
        #endregion
        
        private static readonly Dictionary<CMFApplication, Dictionary<uint, ICMFProvider>> Providers = new Dictionary<CMFApplication, Dictionary<uint, ICMFProvider>>();
        
        private static void FindProviders(CMFApplication app) {
            Providers[app] = new Dictionary<uint, ICMFProvider>();
            Assembly asm = typeof(ICMFProvider).Assembly;
            AddProviders(asm);
        }
        
        public static KeyValuePair<byte[], byte[]> GenerateKeyIV(string name, CMFHeader header, CMFApplication app) {
            if (!Providers.ContainsKey(app)) {
                FindProviders(app);
            }

            byte[] digest = CreateDigest(name);

            ICMFProvider provider;
            if (Providers[app].ContainsKey(header.BuildVersion)) {
                Console.Out.WriteLine($"Using CMF procedure {header.BuildVersion}");
                provider = Providers[app][header.BuildVersion];
            } else {
                Console.Error.WriteLine($"No CMF procedure for build {header.BuildVersion}, trying closest version");
                try {
                    KeyValuePair<uint, ICMFProvider> pair = Providers[app].Where(it => it.Key < header.BuildVersion).OrderByDescending(it => it.Key).First();
                    Console.Out.WriteLine($"Using CMF procedure {pair.Key}");
                    provider = pair.Value;
                } catch {
                    throw new CryptographicException("Missing CMF generators");
                }
            }

            byte[] iv = provider.IV(header, name, digest, 16);
            
            //Console.Out.WriteLine($"{name}: {string.Join(" ", iv.Select(x => x.ToString("X2")))}");
            
            return new KeyValuePair<byte[], byte[]>(provider.Key(header, name, digest, 32), iv);
        }
        
        private static byte[] CreateDigest(string value) {
            byte[] digest;
            using (SHA1 shaM = new SHA1Managed()) {
                byte[] stringBytes = Encoding.ASCII.GetBytes(value);
                digest = shaM.ComputeHash(stringBytes);
            }
            return digest;
        }

        public static void AddProviders(Assembly asm) {
            Type t = typeof(ICMFProvider);
            List<Type> types = asm.GetTypes().Where(tt => tt != t && t.IsAssignableFrom(tt)).ToList();
            foreach (Type tt in types) {
                if (tt.IsInterface) {
                    continue;
                }
                CMFMetadataAttribute metadata = tt.GetCustomAttribute<CMFMetadataAttribute>();
                if (metadata == null) {
                    continue;
                }

                if (!Providers.ContainsKey(metadata.App)) {
                    Providers[metadata.App] = new Dictionary<uint, ICMFProvider>();
                }
                ICMFProvider provider = (ICMFProvider)Activator.CreateInstance(tt);
                if (metadata.AutoDetectVersion) {
                    Providers[metadata.App][uint.Parse(tt.Name.Split('_')[1])] = provider;
                }

                if (metadata.BuildVersions != null) {
                    foreach (uint buildVersion in metadata.BuildVersions) {
                        Providers[metadata.App][buildVersion] = provider;
                    }
                }
            }
        }
    }
}