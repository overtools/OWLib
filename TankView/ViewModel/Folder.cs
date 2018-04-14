using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TankView.ViewModel
{
    public class Folder
    {
        public Folder(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
        public List<Folder> Folders { get; set; } = new List<Folder>();
        public List<GUIDEntry> Files { get; set; } = new List<GUIDEntry>();

        public Folder this[string name] {
            get {
                return Folders.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            }
            set {
                Folders.Add(new Folder(name));
            }
        }

        public bool HasFolder(string part)
        {
            return Folders.Any(x => x.Name.Equals(part, StringComparison.InvariantCultureIgnoreCase));
        }

        public void Add(string part)
        {
            if (!HasFolder(part))
            {
                Folders.Add(new Folder(part));
            }
        }
    }
}
