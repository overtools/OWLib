using System;
using System.Collections;
using System.ComponentModel;
using TACTLib.Container;
using TankLib;

namespace TankView.ViewModel {
    public class GUIDEntry : INotifyPropertyChanged, INotifyDataErrorInfo, IDataErrorInfo {
        public string this[string columnName] => string.Empty;

        public string Filename { get; set; }
        public ulong GUID { get; set; }
        public string FullPath { get; set; }
        public int Size { get; set; }
        public string Locale { get; set; }
        public CKey ContentKey { get; set; }
        public ContentFlags Flags { get; set; }
        public string StringValue { get; set; }
        public bool IsNew { get; set; }

        public bool HasErrors => false;

        public string Error => string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public IEnumerable GetErrors(string propertyName) {
            return Array.Empty<string>();
        }

        public override string ToString() {
            return teResourceGUID.AsString(GUID);
        }

        public static implicit operator ulong(GUIDEntry guid) {
            return guid.GUID;
        }

        public static implicit operator GUIDEntry(teResourceGUID guid) {
            return new GUIDEntry {
                GUID = guid
            };
        }
    }
}
