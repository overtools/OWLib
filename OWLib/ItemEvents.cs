using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace OWLib {
    public class ItemEvents {
        private readonly Dictionary<uint, string> events;
        private readonly Dictionary<uint, string> _eventsNormal;
        public IReadOnlyDictionary<uint, string> Events => events;
        public IReadOnlyDictionary<uint, string> EventsNormal => _eventsNormal;

        private static ItemEvents Instance;

        private static Regex REPLACE;

        public static ItemEvents GetInstance() {
            if (Instance == null) {
                REPLACE = new Regex("[^a-zA-Z0-9_]", RegexOptions.CultureInvariant);
                Instance = new ItemEvents();
            }
            return Instance;
        }

        public ItemEvents() {
            events = new Dictionary<uint, string>();
            _eventsNormal = new Dictionary<uint, string>();
            if (File.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\ow.events")) {
                using (Stream f = File.OpenRead(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\ow.events")) {
                    using (TextReader r = new StreamReader(f)) {
                        string line = null;
                        uint idx = 0;
                        while ((line = r.ReadLine()) != null) {
                            line = line.Split('#')[0].Trim();
                            if (line.Length > 0) {
                                _eventsNormal[idx] = line;
                                string @event = REPLACE.Replace(line.Replace(' ', '_').ToUpper(), "");
                                if (@event.Length > 0) {
                                    events[idx++] = @event;
                                }
                            }
                        }
                    }
                }
            }
        }

        public string GetEventNormal(uint id) => events.ContainsKey(id) ? _eventsNormal[id] : $"Event {id}";
        public string GetEvent(uint id) => events.ContainsKey(id) ? events[id] : $"EVENT_{id}";
    }
}