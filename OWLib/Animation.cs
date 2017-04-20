using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OWLib.Types;

namespace OWLib {
  public class Animation {
    // Public items, for writing
    public string Name;                 // The name of the animation. If no name can be provided, use the filename.
    public float Duration;              // How long the animtion is, in seconds.
    public float FramesPerSecond;       // The Number of frames per second. Duration * FPS = Total number of frames.
    public List<Keyframe> Animations;

    // Private Items
    private AnimHeader header;
    private List<int> BoneList = new List<int>();
    private List<AnimInfoTable> InfoTables = new List<AnimInfoTable>();
    private int InfoTableSize = 0;

    private Vec4d UnpackRotation(ushort a, ushort b, ushort c) {
      Vec4d q = new Vec4d();
      int axis1 = a >> 15;
      int axis2 = b >> 15;
      int axis = axis1 << 1 | axis2;
      axis = axis2 * 2 + axis1;

      a = (ushort)(a & 0x7FFF);
      b = (ushort)(b & 0x7FFF);

      double x, y, z, w;
      x = 1.41421 * (a - 0x4000) / 0x8000;
      y = 1.41421 * (b - 0x4000) / 0x8000;
      z = 1.41421 * (c - 0x8000) / 0x10000;
      w = Math.Pow(1.0 - x * x - y * y - z * z, 0.5);

      Console.Out.WriteLine("Unpack Values: X: {0}, Y: {1}, Z: {2}, W: {3}, Axis: {4}", x, y, z, w, axis);

      if (axis == 0) {
        q = new Vec4d(w, x, y, z);
      } else if (axis == 1) {
        q = new Vec4d(x, w, y, z);
      } else if (axis == 2) {
        q = new Vec4d(x, y, w, z);
      } else if (axis == 3) {
        q = new Vec4d(x, y, z, w);
      } else {
        Console.Out.WriteLine("Unknown Axis detected! Axis: %s", axis);
      }

      return q;
    }
    private Vec3d UnpackScale(ushort x, ushort y, ushort z) {
      double xd = (double)x / 1024.0;
      double yd = (double)y / 1024.0;
      double zd = (double)z / 1024.0;

      Vec3d value = new Vec3d(xd, yd, zd);
      return value;
    }

    public Animation(Stream animStream, string userName = "", bool leaveOpen = true) {
      Name = animStream.ToString();
      if (userName != "") {
        Name = userName;
      }
      Animations = new List<Keyframe>();
      // Convert OW Animation to our Animation Type
      using (BinaryReader animReader = new BinaryReader(animStream, Encoding.Default, leaveOpen)) {
        header = animReader.Read<AnimHeader>();

        Duration = header.duration;
        FramesPerSecond = header.fps;
        InfoTableSize = (int)(header.fps * header.duration) + 1;
        uint bonecount = header.bonecount;

        animStream.Seek((long)header.boneListOffset, SeekOrigin.Begin);
        for (uint i = 0; i < header.bonecount; i++) {
          int boneID = animReader.ReadInt32();
          BoneList.Add(boneID);
        }

        Vec3d[,] ScaleValues = new Vec3d[bonecount, InfoTableSize];
        Vec3d[,] PositionValues = new Vec3d[bonecount, InfoTableSize];
        Vec4d[,] RotationValues = new Vec4d[bonecount, InfoTableSize];
        bool[,] hasScale = new bool[bonecount, InfoTableSize];
        bool[,] hasPosition = new bool[bonecount, InfoTableSize];
        bool[,] hasRotation = new bool[bonecount, InfoTableSize];

        animStream.Seek((long)header.infoTableOffset, SeekOrigin.Begin);
        for (int boneid = 0; boneid < header.bonecount; boneid++) {
          long animStreamPos = animStream.Position;
          AnimInfoTable it = animReader.Read<AnimInfoTable>();
          long SIO = (long)it.ScaleIndicesOffset * 4 + animStreamPos;
          long PIO = (long)it.PositionIndicesOffset * 4 + animStreamPos;
          long RIO = (long)it.RotationIndicesOffset * 4 + animStreamPos;
          long SDO = (long)it.ScaleDataOffset * 4 + animStreamPos;
          long PDO = (long)it.PositionDataOffset * 4 + animStreamPos;
          long RDO = (long)it.RotationDataOffset * 4 + animStreamPos;
          InfoTables.Add(it);

          // Read Indices
          List<int> ScaleIndexList = new List<int>();
          animStream.Seek(SIO, SeekOrigin.Begin);
          for (int j = 0; j < it.ScaleCount; j++) {
            if (InfoTableSize < 255) {
              ScaleIndexList.Add((int)animReader.ReadByte());
            } else {
              ScaleIndexList.Add((int)animReader.ReadInt16());
            }
          }
          List<int> PositonIndexList = new List<int>();
          animStream.Seek(PIO, SeekOrigin.Begin);
          for (int j = 0; j < it.PositionCount; j++) {
            if (InfoTableSize < 255) {
              PositonIndexList.Add((int)animReader.ReadByte());
            } else {
              PositonIndexList.Add((int)animReader.ReadInt16());
            }
          }
          List<int> RotationIndexList = new List<int>();
          animStream.Seek(RIO, SeekOrigin.Begin);
          for (int j = 0; j < it.RotationCount; j++) {
            if (InfoTableSize < 255) {
              RotationIndexList.Add((int)animReader.ReadByte());
            } else {
              RotationIndexList.Add((int)animReader.ReadInt16());
            }
          }
          // Read Data
          animStream.Seek(SDO, SeekOrigin.Begin);
          for (int j = 0; j < it.ScaleCount; j++) {
            int Index = ScaleIndexList[j];
            hasScale[boneid, Index] = true;
            ushort x = animReader.ReadUInt16();
            ushort y = animReader.ReadUInt16();
            ushort z = animReader.ReadUInt16();

            Vec3d values = UnpackScale(x, y, z);
            ScaleValues[boneid, Index] = values;
          }
          animStream.Seek(PDO, SeekOrigin.Begin);
          for (int j = 0; j < it.PositionCount; j++) {
            int Index = PositonIndexList[j];
            hasPosition[boneid, Index] = true;
            float x = animReader.ReadSingle();
            float y = animReader.ReadSingle();
            float z = animReader.ReadSingle();

            Vec3d values = new Vec3d(x, y, z);
            PositionValues[boneid, Index] = values;
          }
          animStream.Seek(RDO, SeekOrigin.Begin);
          for (int j = 0; j < it.RotationCount; j++) {
            int Index = RotationIndexList[j];
            hasRotation[boneid, Index] = true;
            ushort x = animReader.ReadUInt16();
            ushort y = animReader.ReadUInt16();
            ushort z = animReader.ReadUInt16();

            Vec4d values = UnpackRotation(x, y, z);
            RotationValues[boneid, Index] = values;
          }
          animStream.Seek(animStreamPos + 32L, SeekOrigin.Begin);
        }


        for (int frame = 0; frame < InfoTableSize; frame++) {
          Keyframe kf = new Keyframe();
          kf.FramePosition = ((float)frame / FramesPerSecond);
          kf.BoneFrames = new List<BoneAnimation>();
          for (int bone = 0; bone < header.bonecount; bone++) {
            // Build Value Data
            BoneAnimation ba = new BoneAnimation();
            ba.BoneID = BoneList[bone];
            ba.Values = new List<FrameValue>();
            
            if (hasScale[bone, frame]) {
              Vec3d v = ScaleValues[bone, frame];
              FrameValue fv = new FrameValue(AnimChannelID.SCALE, v);
              ba.Values.Add(fv);
            }
            if (hasPosition[bone, frame]) {
              Vec3d v = PositionValues[bone, frame];
              FrameValue f = new FrameValue(AnimChannelID.POSITION, v);
              ba.Values.Add(f);
            }
            if (hasRotation[bone, frame]) {
              Vec4d v = RotationValues[bone, frame];
              FrameValue f = new FrameValue(AnimChannelID.ROTATION, v);
              ba.Values.Add(f);
            }
            if (ba.Values.Count > 0) {
              kf.BoneFrames.Add(ba);
            }
          }
          if (kf.BoneFrames.Count > 0) {
            Animations.Add(kf);
          }
        }
      }
    }
  }
}