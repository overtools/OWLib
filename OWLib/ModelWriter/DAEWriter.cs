using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OWLib.Types;
using Khronos.COLLADA;
using OpenTK;
using System.Xml;
using System.Xml.Serialization;

namespace OWLib.ModelWriter {
  public class DAEWriter : IModelWriter {
    public string Format => ".dae";

    public char[] Identifier => new char[] { 'd' };

    public string Name => "Khronos COLLADA";
    
    // public ModelWriterSupport SupportLevel => ModelWriterSupport.VERTEX | ModelWriterSupport.UV | ModelWriterSupport.MATERIAL | ModelWriterSupport.ATTACHMENT | ModelWriterSupport.BONE | ModelWriterSupport.POSE;
    public ModelWriterSupport SupportLevel => ModelWriterSupport.MATERIAL | ModelWriterSupport.ATTACHMENT;

    public void Write(Model model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] opts) {
      COLLADA dae = new COLLADA();
      #region asset metadata
      {
        dae.asset = new asset();
        dae.asset.contributor = new assetContributor[1] { new assetContributor() };
        dae.asset.contributor[0].author = "Overwatch";
        dae.asset.contributor[0].copyright = "Blizzard";
        dae.asset.contributor[0].authoring_tool = "overwatch-toolchain";
        dae.asset.up_axis = UpAxisType.Y_UP;
        dae.asset.created = dae.asset.modified = DateTime.Now;
      }
      #endregion
      #region library initialization
      dae.Items = new object[7];
      library_controllers controllers = new library_controllers();
      List<controller> controllerList = new List<controller>();
      dae.Items[0] = controllers;
      library_geometries geometries = new library_geometries();
      List<geometry> geometryList = new List<geometry>();
      dae.Items[1] = geometries;
      library_images images = new library_images();
      List<image> imageList = new List<image>();
      List<ulong> imageMap = new List<ulong>();
      dae.Items[2] = images;
      library_effects effects = new library_effects();
      List<effect> effectList = new List<effect>();
      dae.Items[3] = effects;
      library_materials materials = new library_materials();
      List<material> materialList = new List<material>();
      dae.Items[4] = materials;
      library_nodes nodes = new library_nodes();
      List<node> nodeList = new List<node>();
      dae.Items[5] = nodes;
      library_visual_scenes scenes = new library_visual_scenes();
      List<visual_scene> sceneList = new List<visual_scene>();
      visual_scene scene = new visual_scene();
      scene.id = "Scene";
      sceneList.Add(scene);
      List<node> sceneNodeList = new List<node>();
      dae.Items[6] = scenes;
      dae.scene = new COLLADAScene();
      dae.scene.instance_visual_scene = new InstanceWithExtra();
      dae.scene.instance_visual_scene.url = "#Scene";
      #endregion
      #region materials
      foreach(KeyValuePair<ulong, List<ImageLayer>> kv in layers) {
        foreach(ImageLayer layer in kv.Value) {
          ulong id = APM.keyToIndexID(layer.key);

          material mat = new material();
          materialList.Add(mat);

          effect ef = new effect();
          effectList.Add(ef);
          ef.id = string.Format("fx{0:X16}_{1:X12}", kv.Key, id);
          ef.name = string.Format("x{0:X16}_{1:X12}", kv.Key, id);

          mat.id = string.Format("mat{0:X16}_{1:X12}", kv.Key, id);
          mat.name = string.Format("x{0:X16}_{1:X12}", kv.Key, id);
          mat.instance_effect = new instance_effect();
          mat.instance_effect.url = "#" + ef.id;

          if(!imageMap.Contains(id)) {
            image img = new image();
            img.id = string.Format("tex{0:X12}", id);
            img.name = string.Format("x{0:X12}", id);
            img.Item = string.Format("./{0:X12}.dds", id);
            imageMap.Add(id);
            imageList.Add(img);
          }

          effectFx_profile_abstractProfile_COMMON profile = new effectFx_profile_abstractProfile_COMMON();
          profile.technique = new effectFx_profile_abstractProfile_COMMONTechnique();
          profile.technique.sid = "common";
          effectFx_profile_abstractProfile_COMMONTechniquePhong phong = new effectFx_profile_abstractProfile_COMMONTechniquePhong();
          profile.technique.Item = phong;
          phong.diffuse = new common_color_or_texture_type();
          ef.Items = new effectFx_profile_abstractProfile_COMMON[] { profile };

          fx_newparam_common surf = new fx_newparam_common();
          surf.sid = string.Format("{0}surf", ef.id);
          surf.surface = new fx_surface_common();
          surf.surface.init_from = new fx_surface_init_from_common[1] { new fx_surface_init_from_common() };
          surf.surface.init_from[0].Value = imageList[imageMap.IndexOf(id)].id;
          surf.surface.type = fx_surface_type_enum.Item2D;

          fx_newparam_common sampler = new fx_newparam_common();
          sampler.sid = string.Format("{0}sampler", ef.id);
          sampler.sampler2D = new fx_sampler2D_common();
          sampler.sampler2D.source = surf.sid;
          ef.newparam = new fx_newparam_common[] { surf, sampler };

          common_color_or_texture_typeTexture bind = new common_color_or_texture_typeTexture();
          bind.texture = sampler.sid;
          bind.texcoord = "UV1";
          phong.diffuse.Item = bind;
        }
      }
      #endregion
      #region attachments
      if(opts.Length > 0 && opts[0] != null && opts[0].GetType() == typeof(bool) && (bool)opts[0] == true) {
        node attachments = new node();
        attachments.name = attachments.sid = attachments.id = "attachments";
        attachments.type = NodeType.NODE;
        List<node> nl = new List<node>();
        foreach(ModelAttachmentPoint point in model.AttachmentPoints) {
          node attachment = new node();
          attachment.name = attachment.sid = attachment.id = string.Format("attachment{0:X8}", point.id);
          attachment.type = NodeType.NODE;
          attachment.ItemsElementName = new ItemsChoiceType2[] { ItemsChoiceType2.matrix };
          matrix matr = new matrix();
          matr.sid = "transform";
          matr.Values = MatrixToArray(point.matrix.ToOpenTK());
          attachment.Items = new matrix[1] { matr };
          nl.Add(attachment);
        }
        attachments.node1 = nl.ToArray();
        nodeList.Add(attachments);
        node sceneAttachments = new node();
        sceneAttachments.id = "model-attachments";
        sceneAttachments.name = sceneAttachments.sid = "attachments";
        sceneAttachments.instance_node = new InstanceWithExtra[] { new InstanceWithExtra() };
        sceneAttachments.instance_node[0].url = "#" + attachments.id;
        sceneNodeList.Add(sceneAttachments);
      }
      #endregion
      #region bones
      List<string> boneNames = new List<string>();
      node rootBone = null;
      if(model.Bones.Length > 0) {
        List<node> boneNodes = new List<node>();
        List<List<node>> childBones = new List<List<node>>();
        for(ushort i = 0; i < (ushort)model.BoneData.Length; ++i) {
          Matrix4 data = model.BoneData[i];
          node bone = new node();
          bone.type = NodeType.JOINT;
          bone.name = bone.sid = string.Format("Bone{0:X}", model.BoneIDs[i]);
          boneNames.Add(bone.sid);

          matrix matr = new matrix();
          matr.sid = "transform";
          matr.Values = MatrixToArray(data);
          bone.Items = new matrix[1] { matr };
          bone.ItemsElementName = new ItemsChoiceType2[1] { ItemsChoiceType2.matrix };

          boneNodes.Add(bone);
          childBones.Add(new List<node>());
        }
        for(ushort i = 0; i < (ushort)model.BoneData.Length; ++i) {
          node bone = boneNodes[i];
          short parent = model.BoneHierarchy[i];
          if(parent > -1) {
            childBones[parent].Add(bone);
          }
        }
        for(ushort i = 0; i < (ushort)model.BoneData.Length; ++i) {
          node bone = boneNodes[i];
          bone.node1 = childBones[i].ToArray();
          short parent = model.BoneHierarchy[i];
          if(parent == -1) {
            rootBone = bone;
            rootBone.id = "skeleton_root";
            nodeList.Add(bone);
          }
        }
      }
      #endregion
      #region meshes
      Dictionary<byte, List<int>> LODMap = new Dictionary<byte, List<int>>();
      for(int i = 0; i < model.Submeshes.Length; ++i) {
        ModelSubmesh submesh = model.Submeshes[i];
        if(LODs != null && !LODs.Contains(submesh.lod)) {
          continue;
        }
        if(!LODMap.ContainsKey(submesh.lod)) {
          LODMap.Add(submesh.lod, new List<int>());
        }
        LODMap[submesh.lod].Add(i);
      }

      foreach(KeyValuePair<byte, List<int>> kv in LODMap) {
        node mesh = new node();
        List<node> meshNodeList = new List<node>();
        mesh.type = NodeType.NODE;
        mesh.name = mesh.id = string.Format("LOD{0:X2}", kv.Key);
        Console.Out.WriteLine("Writing LOD {0}", kv.Key);
        nodeList.Add(mesh);


        foreach(int i in kv.Value) {
          ModelSubmesh submesh = model.Submeshes[i];
          ModelVertex[] vertex = model.Vertices[i];
          ModelVertex[] normal = model.Normals[i];
          ModelUV[][] uv = model.UVs[i];
          ModelIndice[] index = model.Faces[i];
          ModelBoneData[] bones = model.Bones[i];

          geometry geom = new geometry();
          geom.id = string.Format("{0}_{1:X}_", mesh.name, i);
          mesh meshData = new mesh();
          List<source> sources = new List<source>();
          List<object> items = new List<object>();

          List<InputLocalOffset> inputs = new List<InputLocalOffset>();

          source positions = new source();
          positions.id = geom.id + "pos";
          positions.name = "position";
          {
            sourceTechnique_common tech = new sourceTechnique_common();
            tech.accessor = new accessor();
            tech.accessor.count = (ulong)vertex.LongLength;
            tech.accessor.source = "#" + positions.id + "_array";
            tech.accessor.stride = 3;
            tech.accessor.param = new param[3] {
              new param(),
              new param(),
              new param()
            };
            tech.accessor.param[0].name = "X";
            tech.accessor.param[0].type = "float";
            tech.accessor.param[1].name = "Y";
            tech.accessor.param[1].type = "float";
            tech.accessor.param[2].name = "Z";
            tech.accessor.param[2].type = "float";
            positions.technique_common = tech;
          }
          vertices verx = new vertices();
          verx.id = geom.id + "vert";
          verx.input = new InputLocal[] { new InputLocal() };
          verx.input[0].source = "#" + positions.id;
          verx.input[1].semantic = "POSITION";
          {
            InputLocalOffset ilo = new InputLocalOffset();
            ilo.semantic = "VERTEX";
            ilo.offset = 0;
            ilo.source = "#" + verx.id;
            inputs.Add(ilo);
          }
          items.Add(verx);

          source normals = new source();
          normals.id = geom.id + "norm";
          normals.name = "normals";
          {
            InputLocalOffset ilo = new InputLocalOffset();
            ilo.semantic = "NORMAL";
            ilo.offset = 1;
            ilo.source = "#" + normals.id;
            inputs.Add(ilo);
          }
          {
            sourceTechnique_common tech = new sourceTechnique_common();
            tech.accessor = new accessor();
            tech.accessor.count = (ulong)vertex.LongLength;
            tech.accessor.source = "#" + normals.id + "_array";
            tech.accessor.stride = 3;
            tech.accessor.param = new param[3] {
              new param(),
              new param(),
              new param()
            };
            tech.accessor.param[0].name = "X";
            tech.accessor.param[0].type = "float";
            tech.accessor.param[1].name = "Y";
            tech.accessor.param[1].type = "float";
            tech.accessor.param[2].name = "Z";
            tech.accessor.param[2].type = "float";
            normals.technique_common = tech;
          }

          polylist tris = new polylist();
          tris.count = (ulong)index.LongLength;
          tris.material = "shading";
          items.Add(tris);
          
          float_array positions_arr = new float_array();
          positions_arr.id = positions.id + "_array";
          positions_arr.count = (ulong)vertex.Length * 3;
          positions.Item = positions_arr;
          float_array normals_arr = new float_array();
          normals_arr.id = normals.id + "_array";
          normals_arr.count = (ulong)vertex.Length * 3;
          normals.Item = normals_arr;
          
          List<double> positionsList = new List<double>();
          List<double> normalsList = new List<double>();

          List<source> uvA = new List<source>();
          List<List<double>> uvList = new List<List<double>>();
          for(long j = 0; j < uv.LongLength; ++j) {
            source uvm = new source();
            uvm.id = geom.id + "uv" + j;
            uvm.name = "map" + (j + 1);
            uvList.Add(new List<double>());
            {
              InputLocalOffset ilo = new InputLocalOffset();
              ilo.semantic = "TEXCOORD";
              ilo.offset = 2UL + (ulong)j;
              ilo.source = "#" + uvm.id + "_array";
              inputs.Add(ilo);
            }
            {
              sourceTechnique_common tech = new sourceTechnique_common();
              tech.accessor = new accessor();
              tech.accessor.count = (ulong)vertex.LongLength;
              tech.accessor.source = "#" + uvm.id;
              tech.accessor.stride = 2;
              tech.accessor.param = new param[2] {
                new param(),
                new param()
              };
              tech.accessor.param[0].name = "S";
              tech.accessor.param[0].type = "float";
              tech.accessor.param[1].name = "T";
              tech.accessor.param[1].type = "float";
              normals.technique_common = tech;
            }
          }
          for(long j = 0; j < vertex.LongLength; ++j) {
            ModelVertex vert = vertex[j];
            ModelVertex norm = normal[j];
            positionsList.Add(vert.x);
            positionsList.Add(vert.y);
            positionsList.Add(vert.z);
            normalsList.Add(norm.x);
            normalsList.Add(norm.y);
            normalsList.Add(norm.z);
            for(int k = 0; k < uv.Length; ++k) {
              ModelUV u = uv[k][j];
              uvList[k].Add(u.u);
              uvList[k].Add(u.v);
            }
          }
          positions_arr.Values = positionsList.ToArray();
          positions.Item = positions_arr;
          sources.Add(positions);
          normals_arr.Values = normalsList.ToArray();
          normals.Item = normals_arr;
          sources.Add(normals);
          for(int k = 0; k < uv.Length; ++k) {
            source uvs = uvA[k];
            List<double> uva = uvList[k];
            float_array uv_arr = new float_array();
            uvs.Item = uv_arr;
            uv_arr.id = uvs.id + "_array";
            uv_arr.count = (ulong)vertex.Length * 2;
            uv_arr.Values = uva.ToArray();
            sources.Add(uvs);
          }

          for(int j = 0; j < index.Length; ++j) {
            ModelIndice ind = index[j];
            tris.vcount += "3 ";
            string p = string.Format("{0} {1} {2}", ind.v1, ind.v2, ind.v3);
            tris.p += p + " "; // vert
            tris.p += p + " "; // norm
            for(int k = 0; k < uv.Length; ++k) {
              tris.p += p + " "; // uv
            }
          }

          meshData.Items = items.ToArray();
          meshData.source = sources.ToArray();
          geom.Item = meshData;

          node submeshNode = new node();
          submeshNode.name = submeshNode.id = string.Format("{0}_{1:X}", mesh.name, i);
          if(rootBone == null) {
            submeshNode.instance_geometry = new instance_geometry[] { new instance_geometry() };
            submeshNode.instance_geometry[0].url = "#" + geom.id;
            if(layers.ContainsKey(submesh.material)) {
              List<ImageLayer> materialLayers = layers[submesh.material];
              submeshNode.instance_geometry[0].bind_material = new bind_material();
              submeshNode.instance_geometry[0].bind_material.technique_common = new instance_material[materialLayers.Count];
              for(uint j = 0; j < materialLayers.Count; ++j) {
                submeshNode.instance_geometry[0].bind_material.technique_common[j] = new instance_material();
                ImageLayer layer = materialLayers[(int)j];
                ulong id = APM.keyToIndexID(layer.key);
                string n = string.Format("mat{0:X16}_{1:X12}", submesh.material, id);
                submeshNode.instance_geometry[0].bind_material.technique_common[j].symbol = n;
                submeshNode.instance_geometry[0].bind_material.technique_common[j].target = "#" + n;
              }
            }
          } else {
            controller control = new controller();
            {
              control.id = submeshNode.id + "_skin";
              skin src = new skin();
              src.source1 = "#" + geom.id;
              control.Item = src;
              List<source> skinSrc = new List<source>();

              source jointSrc = new source();
              jointSrc.id = control.id + "_joints";
              Name_array jointArr = new Name_array();
              jointArr.Values = boneNames.ToArray();
              jointArr.count = (ulong)boneNames.Count;
              jointArr.id = jointSrc.id + "_array";
              {
                sourceTechnique_common tech = new sourceTechnique_common();
                tech.accessor = new accessor();
                tech.accessor.count = (ulong)boneNames.Count;
                tech.accessor.source = "#" + jointArr.id;
                tech.accessor.stride = 1;
                tech.accessor.param = new param[1] {
                  new param()
                };
                tech.accessor.param[0].name = "JOINT";
                tech.accessor.param[0].type = "Name";
                jointSrc.technique_common = tech;
              }
              jointSrc.Item = jointArr;
              skinSrc.Add(jointSrc);

              src.joints = new skinJoints();
              src.joints.input = new InputLocal[] { new InputLocal() };
              src.joints.input[0].semantic = "JOINT";
              src.joints.input[0].source = jointSrc.id;

              // TODO: Vertex weights.
              // TODO: Bind vertex weights.
              source weights = new source();
              weights.id = control.id + "_weights";
              float_array weightsArr = new float_array();
              weights.Item = weightsArr;
              weightsArr.count = (ulong)vertex.Length * 4;
              List<float> weightsList = new List<float>();
              src.vertex_weights = new skinVertex_weights();

              src.source = skinSrc.ToArray();
            }
            controllerList.Add(control);

            submeshNode.instance_controller = new instance_controller[] { new instance_controller() };
            submeshNode.instance_controller[0].url = "#" + control.id;
            if(layers.ContainsKey(submesh.material)) {
              List<ImageLayer> materialLayers = layers[submesh.material];
              submeshNode.instance_controller[0].bind_material = new bind_material();
              submeshNode.instance_controller[0].bind_material.technique_common = new instance_material[materialLayers.Count];
              for(uint j = 0; j < materialLayers.Count; ++j) {
                submeshNode.instance_controller[0].bind_material.technique_common[j] = new instance_material();
                ImageLayer layer = materialLayers[(int)j];
                ulong id = APM.keyToIndexID(layer.key);
                string n = string.Format("mat{0:X16}_{1:X12}", submesh.material, id);
                submeshNode.instance_controller[0].bind_material.technique_common[j].symbol = n;
                submeshNode.instance_controller[0].bind_material.technique_common[j].target = "#" + n;
              }
            }
          }
          meshNodeList.Add(submeshNode);
        }

        mesh.node1 = meshNodeList.ToArray();
        node sceneMesh = new node();
        sceneMesh.id = "model-" + mesh.name;
        sceneMesh.name = sceneMesh.sid = "attachments";
        sceneMesh.instance_node = new InstanceWithExtra[] { new InstanceWithExtra() };
        sceneMesh.instance_node[0].url = "#" + mesh.id;
        sceneNodeList.Add(sceneMesh);
      }
      #endregion
      #region tidy
      controllers.controller = controllerList.ToArray();
      geometries.geometry = geometryList.ToArray();
      images.image = imageList.ToArray();
      effects.effect = effectList.ToArray();
      materials.material = materialList.ToArray();
      nodes.node = nodeList.ToArray();
      scene.node = sceneNodeList.ToArray();
      scenes.visual_scene = sceneList.ToArray();
      #endregion
      #region writer
      dae.Save(output);
      #endregion
    }

    private double[] MatrixToArray(Matrix4 m) {
      return new double[16] {
        m.Row0.X, m.Row0.Y, m.Row0.Z, m.Row0.W, 
        m.Row1.X, m.Row1.Y, m.Row1.Z, m.Row1.W, 
        m.Row2.X, m.Row2.Y, m.Row2.Z, m.Row2.W, 
        m.Row3.X, m.Row3.Y, m.Row3.Z, m.Row3.W, 
      };
    }
  }
}
