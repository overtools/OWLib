using System;
using System.Collections.Generic;

namespace DataTool.Helper {
    public class IgnoreCaseDict<TValue> : Dictionary<string, TValue> {
        public IgnoreCaseDict() : base(StringComparer.InvariantCultureIgnoreCase) {
        }
    }
}
