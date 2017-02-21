using System;
using System.Collections.Generic;
using System.IO;
using OpenTK;
using OWLib.Types;
using OWLib.Types.Chunk;
using OWLib.Types.Map;
using System.Globalization;

namespace OWLib.ModelWriter {
  public class RefPoseWriter : IModelWriter {
    public string Format => ".smd";

    public char[] Identifier => new char[1] { 'R' };

    public string Name => "Reference Pose";

    public ModelWriterSupport SupportLevel => ModelWriterSupport.BONE | ModelWriterSupport.POSE;

    public bool Write(Map10 physics, Stream output, object[] data) {
      return false;
    }

    // Note: id-daemon's code, from overwatch_skeleton.
    private Vector3 ToEulerAngles(float w, float x, float y, float z) {
      double[,] matrix = new double[4, 4];
      {
        double num = x * x + y * y + z * z + w * w;
        double num2;
        if(num > 0.0) {
          num2 = 2.0 / num;
        } else {
          num2 = 0.0;
        }
        double num3 = x * num2;
        double num4 = y * num2;
        double num5 = z * num2;
        double num6 = w * num3;
        double num7 = w * num4;
        double num8 = w * num5;
        double num9 = x * num3;
        double num10 = x * num4;
        double num11 = x * num5;
        double num12 = y * num4;
        double num13 = y * num5;
        double num14 = z * num5;
        matrix[0, 0] = 1.0 - (num12 + num14);
        matrix[0, 1] = num10 - num8;
        matrix[0, 2] = num11 + num7;
        matrix[1, 0] = num10 + num8;
        matrix[1, 1] = 1.0 - (num9 + num14);
        matrix[1, 2] = num13 - num6;
        matrix[2, 0] = num11 - num7;
        matrix[2, 1] = num13 + num6;
        matrix[2, 2] = 1.0 - (num9 + num12);
        matrix[3, 3] = 1.0;
      }
      {
        int i = 0;
        int j = 1;
        int k = 2;
        double[,] M = matrix;
        Vector3 vec = new Vector3();
        double num2 = Math.Sqrt(M[i, i] * M[i, i] + M[j, i] * M[j, i]);
        if(num2 > 0.00016) {
          vec.X = (float)Math.Atan2(M[k, j], M[k, k]);
          vec.Y = (float)Math.Atan2(-M[k, i], num2);
          vec.Z = (float)Math.Atan2(M[j, i], M[i, i]);
        } else {
          vec.X = (float)Math.Atan2(-M[j, k], M[j, j]);
          vec.Y = (float)Math.Atan2(-M[k, i], num2);
          vec.Z = 0f;
        }
        return vec;
      }
    }

    public bool Write(Chunked model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] data) {
      IChunk chunk = model.FindNextChunk("lksm").Value;
      if(chunk == null) {
        return false;
      }
      lksm skeleton = (lksm)chunk;
      using(StreamWriter writer = new StreamWriter(output)) {
        writer.WriteLine("{0}", skeleton.Data.bonesAbs);
        writer.WriteLine("version 1");
        writer.WriteLine("nodes");
        for(int i = 0; i < skeleton.Data.bonesAbs; ++i) {
          writer.WriteLine("{0} \"bone_{1:X4}\" {2}", i, skeleton.IDs[i], skeleton.Hierarchy[i]);
        }
        writer.WriteLine("end");
        writer.WriteLine("skeleton");
        writer.WriteLine("time 0");
        for(int i = 0; i < skeleton.Data.bonesAbs; ++i) {
          Matrix3x4 bone = skeleton.Matrices34Inverted[i];
          Vector3 rot = ToEulerAngles(bone[0, 3], bone[0, 0], bone[0, 1], bone[0, 2]);
          Vector3 pos = new Vector3(bone[2, 0], bone[2, 1], bone[2, 2]);
          writer.WriteLine(String.Format(CultureInfo.InvariantCulture, "{0}  {1:0.000000} {2:0.000000} {3:0.000000}  {4:0.000000} {5:0.000000} {6:0.000000}", i, pos.X, pos.Y, pos.Z, rot.X, rot.Y, rot.Z));
        }
      }
      return true;
    }
  }
}
