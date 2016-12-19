using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OWLib {
  public class ItemEvents {
    private Dictionary<ulong, string> events;
    public IReadOnlyDictionary<ulong, string> Events => events;

    private static ItemEvents Instance;

    private static Regex REPLACE;

    public static ItemEvents GetInstance() {
      if(Instance == null) {
        REPLACE = new Regex("[^a-zA-Z0-9_]", RegexOptions.CultureInvariant);
        Instance = new ItemEvents();
      }
      return Instance;
    }

    public ItemEvents() {
      events = new Dictionary<ulong, string>();
      if(File.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\ow.events")) {
        using(Stream f = File.OpenRead(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\ow.events")) {
          using(TextReader r = new StreamReader(f)) {
            string line = null;
            ulong idx = 0;
            while((line = r.ReadLine()) != null) {
              line = line.Split('#')[0].Trim();
              if(line.Length > 0) {
                string @event = REPLACE.Replace(line.Replace(' ', '_').ToUpper(), "");
                if(@event.Length > 0) {
                  events[idx++] = @event;
                }
              }
            }
          }
        }
      }
    }

    public string GetEvent(ulong id) {
      if(events.ContainsKey(id)) {
        return events[id];
      } else {
        return $"EVENT_{id}";
      }
    }
  }
}