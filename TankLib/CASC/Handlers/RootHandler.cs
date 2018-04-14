using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CMFLib;
using TankLib.CASC.Helpers;

namespace TankLib.CASC.Handlers {
    public class RootHandler {
        public readonly List<ApplicationPackageManifest> APMFiles = new List<ApplicationPackageManifest>();

        public readonly Dictionary<string, MD5Hash> RootFiles = new Dictionary<string, MD5Hash>();

        public readonly bool LoadedAPMWithoutErrors;
        
        public RootHandler(BinaryReader stream, ProgressReportSlave worker, CASCHandler casc) {
            worker?.ReportProgress(0, "Loading APM data...");

            string str = Encoding.ASCII.GetString(stream.ReadBytes((int)stream.BaseStream.Length));

            string[] array = str.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            List<string> components = array[0].Substring(1).ToUpper().Split('|').ToList();
            components = components.Select(c => c.Split('!')[0]).ToList();
            int nameComponentIdx = components.IndexOf("FILENAME");
            if (nameComponentIdx == -1) {
                nameComponentIdx = 0;
            }
            int md5ComponentIdx = components.IndexOf("MD5");
            if (md5ComponentIdx == -1) {
                md5ComponentIdx = 1;
            }
            components.Clear();

            Dictionary<string, MD5Hash> cmfHashes = new Dictionary<string, MD5Hash>();
            for (int i = 1; i < array.Length; i++) {
                string[] filedata = array[i].Split('|');
                string name = filedata[nameComponentIdx];

                MD5Hash md5 = filedata[md5ComponentIdx].ToByteArray().ToMD5();

                RootFiles[name] = md5;

                if (Path.GetExtension(name) != ".cmf" || !name.Contains("RDEV")) continue;
                if (casc.Config.Languages != null) {
                    bool @break = true;
                    foreach (string lang in casc.Config.Languages) {
                        if (name.Contains("L" + lang)) {
                            @break = false;
                        }
                    }
                    if (@break) {
                        continue;
                    }
                }

                if (!casc.EncodingHandler.GetEntry(md5, out _)) {
                    continue;
                }

                cmfHashes.Add(name, md5);
            }

            LoadedAPMWithoutErrors = true;

            for (int i = 1; i < array.Length; i++) {
                string[] filedata = array[i].Split('|');
                string name = filedata[nameComponentIdx];

                if (Path.GetExtension(name) == ".apm") {
                    MD5Hash apmMD5 = filedata[md5ComponentIdx].ToByteArray().ToMD5();
                    LocaleFlags apmLang = HeurFigureLangFromName(Path.GetFileNameWithoutExtension(name));
                    
                    if (!name.Contains("RDEV")) {
                        continue;
                    }
                    if (casc.Config.Languages != null) {
                        bool skip = true;
                        foreach (string lang in casc.Config.Languages) {
                            if (name.Contains("L" + lang)) {
                                skip = false;
                                break;
                            }
                        }
                        if (skip) {
                            continue;
                        }
                    }

                    if (!casc.EncodingHandler.GetEntry(apmMD5, out EncodingEntry apmEnc)) {
                        continue;
                    }

                    MD5Hash cmf;
                    string cmfname = $"{Path.GetDirectoryName(name)}/{Path.GetFileNameWithoutExtension(name)}.cmf";
                    if (cmfHashes.ContainsKey(cmfname)) {
                        cmfHashes.TryGetValue(cmfname, out cmf);
                    }

                    if (casc.Config.LoadPackageManifest) {
                        using (Stream apmStream = casc.OpenFile(apmEnc.Key)) {
                            ApplicationPackageManifest apm = new ApplicationPackageManifest();
                            try {
                                Console.Out.WriteLine("Loading APM {0}", name);
                                worker?.ReportProgress(0, $"Loading APM {name}...");
                                apm.Load(name, cmf, apmStream, casc, cmfname, worker);
                            } catch(CryptographicException) {
                                LoadedAPMWithoutErrors = false;
                                if (!casc.Config.APMFailSilent) {
                                    worker?.ReportProgress(0, "CMF decryption failed");
                                    Console.Error.WriteLine("CMF Procedure is outdated, cannot parse {0}\r\nPlease update CMFLib", name);
                                    Debugger.Log(0, "CASC", $"RootHandler: CMF decryption procedure outdated, unable to parse {name}\r\n");
                                    if (Debugger.IsAttached) {
                                        Debugger.Break();
                                    }
                                    Environment.Exit(0x636D6614);
                                    //Logger.GracefulExit(0x636D6614);
                                }
                            }
                            APMFiles.Add(apm);
                        }
                    }
                }

                worker?.ReportProgress((int)(i / (array.Length / 100f)));
            }
        }
        
        private static LocaleFlags HeurFigureLangFromName(string name) {
            string tag = name.Split('_').Reverse().Single(v => v[0] == 'L' && v.Length == 5);
            if (tag == null) {
                return LocaleFlags.All;
            }
            Enum.TryParse(tag.Substring(1), out LocaleFlags flags);
            if (flags == 0) {
                flags = LocaleFlags.All;
            }
            return flags;
        }
    }
}