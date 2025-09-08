using System;
using System.Collections.Generic;

namespace DataTool.Helper {
    public class IgnoreCaseSet : HashSet<string> {
        public IgnoreCaseSet() : base(StringComparer.InvariantCultureIgnoreCase) {
        }
    }
}
