using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DataTool.Flag;
using DataTool.ToolLogic.List;
using Newtonsoft.Json;
using STULib.Types;
using STULib.Types.AnimationList.x020;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-anim-nodes", Description = "Extract anim nodes (debug)", TrackTypes = new ushort[] {0x20}, CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ExtractDebugAnimNodes : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        // nocaps is intentional
        [JsonObject(MemberSerialization.OptOut)]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "CollectionNeverQueried.Global")]
        public class GraphRoot {
            public List<GraphNode> nodes;
            public List<GraphEdge> edges;
        }

        [JsonObject(MemberSerialization.OptOut)]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public class GraphNode {
            public float x;
            public float y;
            public string @class;
            public string uuid;
            public string name;
        }
        
        [JsonObject(MemberSerialization.OptOut)]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public class GraphEdge {
            public string source_nodeId;
            public string source_name;
            public string target_nodeId;
            public string target_name;
            
        }

        public void Parse(ICLIFlags toolFlags) {
            SaveAnimNodes(toolFlags);
        }

        public void SaveAnimNodes(ICLIFlags toolFlags) {
            ListFlags flags = toolFlags as ListFlags;
            
            // I didn't want to write code for a viewer so use: https://github.com/cb109/qtnodes
            
            foreach (ulong key in TrackedFiles[0x20]) {
                if (key != 1116892707587883363) continue; // 020 reaper hero select
                STUAnimBlendTree tree = GetInstance<STUAnimBlendTree>(key);
                GraphRoot root = new GraphRoot {nodes = new List<GraphNode>(), edges = new List<GraphEdge>()};
                if (tree?.AnimNodes == null) continue;
                List<uint> doneNodes = new List<uint>();
                foreach (STUAnimNode_Base node in tree.AnimNodes) {
                    if (node == null) continue;
                    string uuid = node.UniqueID.ToString();
                    if (doneNodes.Contains(node.UniqueID)) continue;
                    doneNodes.Add(node.UniqueID);
                    root.nodes.Add(new GraphNode {x = node.Pos.X, y = node.Pos.Y, uuid = uuid, @class = "MaxObject", name = node.GetType().Name});
                    
                    
                    if (node.m_0DE1BA16 == null) continue;
                    foreach (STU_40274C18 stu_6C0Bbd69 in node.m_0DE1BA16) {
                        if (stu_6C0Bbd69.m_AF632ACD != null) {
                            uint parId = stu_6C0Bbd69.m_AF632ACD.UniqueID;
                            root.edges.Add(new GraphEdge {source_nodeId = node.UniqueID.ToString(), target_name = "parent", source_name = "children", target_nodeId = parId.ToString()});
                        }
                    }
                }
                ParseJSON(root, flags);
                
                // notes: (all oldhash)
                // {STU_2D84E49E} for strafe stuff
                
                // <= breakpoint here
            }
        }
    }
}