using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TankView.ViewModel {
    public class Folder {
        public Folder(string name, string fullPath) {
            Name = name;
            FullPath = fullPath;
        }

        public string Name { get; set; }
        public string FullPath { get; set; }
        public List<Folder> Folders { get; set; } = new List<Folder>();
        public List<GUIDEntry> Files { get; set; } = new List<GUIDEntry>();

        public Folder this[string name] {
            get { return Folders.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)); }
            set { throw new NotImplementedException("this[]"); }
        }

        public bool HasFolder(string part) {
            return Folders.Any(x => x.Name.Equals(part, StringComparison.InvariantCultureIgnoreCase));
        }

        public void Add(string part) {
            if (!HasFolder(part)) {
                Folders.Add(new Folder(part, Path.Combine(FullPath, part)));
            }
        }
    }
}
