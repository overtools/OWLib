using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using TankLib;
using TankLib.STU.Types;
using Utf8Json;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-anim-nodes", Description = "Extract anim nodes (debug)", TrackTypes = new ushort[] {0x20}, CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugAnimNodes : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        // nocaps is intentional
        [DataContract]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "CollectionNeverQueried.Global")]
        public class GraphRoot {
            public List<GraphNode> nodes;
            public List<GraphEdge> edges;
        }

        [DataContract]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public class GraphNode {
            public float x;
            public float y;
            public string @class;
            public string uuid;
            public string name;
        }
        
        [DataContract]
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
            if (!(toolFlags is ExtractFlags flags)) {
                throw new Exception("wat");
            }
            string path = Path.Combine(flags.OutputPath, "DebugAnimNodes");
            IO.CreateDirectorySafe(path);

            // I didn't want to write code for a viewer so use: https://github.com/cb109/qtnodes
            
            foreach (ulong key in TrackedFiles[0x20]) {
                /*if (key != 1116892707587883363) continue; // 020 reaper hero select
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
                OutputJSON(root, flags);*/
                
                // notes: (all oldhash)
                // {STU_2D84E49E} for strafe stuff
                
                // <= breakpoint here
                GraphRoot root = new GraphRoot {nodes = new List<GraphNode>(), edges = new List<GraphEdge>()};

                STUAnimBlendTree blendTree = GetInstance<STUAnimBlendTree>(key);
                if (blendTree.m_animNodes == null) continue;
                foreach (STUAnimNode_Base animNode in blendTree.m_animNodes) {
                    ParseNode(root, animNode);
                }

                byte[] json = JsonSerializer.PrettyPrintByteArray(JsonSerializer.Serialize(root));
                string output = Path.Combine(path, $"{teResourceGUID.AsString(key)}.json");
                using (Stream file = File.OpenWrite(output)) {
                    file.SetLength(0);
                    file.Write(json, 0, json.Length);
                }
            }
        }

        public void ParseNode(GraphRoot root, STUAnimNode_Base animNode) {
            if (animNode == null) return;
            
            string name = $"{animNode.GetType().Name} - {animNode.m_uniqueID}";

            if (animNode.m_layers != null) {
                foreach (STUAnimNode_LayerChild layer in animNode.m_layers) {
                    AddChild(root, layer, animNode.m_uniqueID);
                }
            }

            if (animNode is STUAnimNode_StateMachine stateMachine) {
                if (stateMachine.m_children != null) {
                    foreach (STU_74173BA8 child in stateMachine.m_children) {
                        AddChild(root, child, animNode.m_uniqueID);
                    }
                }
            }
            if (animNode is STUAnimNode_Random unk1) {
                if (unk1.m_children != null) {
                    foreach (STU_597F8A0B child in unk1.m_children) {
                        AddChild(root, child, animNode.m_uniqueID);
                    }
                }
            }
            if (animNode is STUAnimNode_Sequence unk2) {
                if (unk2.m_children != null) {
                    foreach (STU_F632355B child in unk2.m_children) {
                        AddChild(root, child, animNode.m_uniqueID);
                    }
                }
            }
            if (animNode is STU_58C5699C unk3) {
                if (unk3.m_child != null) {
                    AddChild(root, unk3.m_child, animNode.m_uniqueID);
                }
            }
            if (animNode is STUAnimNode_BranchByCategory byCategory) {
                if (byCategory.m_children != null) {
                    foreach (STU_C9E2FF36 child in byCategory.m_children) {
                        AddChild(root, child, animNode.m_uniqueID);
                    }
                }
            }

            if (animNode is STUAnimNode_Animation anim) {
                if (anim.m_animation != null) {
                    if (anim.m_animation.m_63DEDAC0 != 0) {
                        name += $" - {anim.m_animation.m_63DEDAC0-1}";
                    }
                }
            }
            
            root.nodes.Add(new GraphNode {x = animNode.m_pos.X, y = animNode.m_pos.Y, 
                uuid = animNode.m_uniqueID.ToString(), @class = "MaxObject", name = name});
        }

        public void AddChild(GraphRoot root, STU_2F6BD485 child, uint parentId) {
            if (child.m_AF632ACD != null) {
                uint parId = child.m_AF632ACD.m_uniqueID;
                root.edges.Add(new GraphEdge {source_nodeId = parentId.ToString(), target_name = "parent", source_name = "children", target_nodeId = parId.ToString()});
            }
        }
    }
}