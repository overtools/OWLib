using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using DataTool;

namespace TankView {
    public partial class DataToolListView : INotifyPropertyChanged {
        public List<AwareToolEntry> Tools { get; set; } = new List<AwareToolEntry>();

        public DataToolListView() {
            InitializeComponent();
            Type t = typeof(IAwareTool);
            Assembly asm = t.Assembly;
            List<Type> types = asm.GetTypes().Where(tt => tt.IsClass && t.IsAssignableFrom(tt)).ToList();
            foreach (Type tt in types) {
                ToolAttribute attribute = tt.GetCustomAttribute<ToolAttribute>();
                if (tt.IsInterface || attribute == null) {
                    continue;
                }

                Tools.Add(new AwareToolEntry(attribute, tt));
            }

            NotifyPropertyChanged(nameof(Tools));
        }

        private void ActivateTool(object sender, RoutedEventArgs e) {
            Type t = ((AwareToolEntry) ToolList.SelectedItem).Type;
            IAwareTool tool = Activator.CreateInstance(t) as IAwareTool;
            var transition = new DataToolProgressTransition(tool);
            transition.Show();
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class AwareToolEntry {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Keyword { get; set; }
        public Type Type { get; set; }
        
        public AwareToolEntry(ToolAttribute attribute, Type tt) {
            Type = tt;
            Name = attribute.Name;
            Description = attribute.Description;
            Keyword = attribute.Keyword;
        }
    }
}

