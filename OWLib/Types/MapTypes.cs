using System;
using System.Runtime.InteropServices;

namespace OWLib.Types {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct MapHeader {
    public uint recordCount;
    public uint sizes;
    public uint offset;
    public uint unk;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct MapPhysicsHeader {
    public MapHeader core;
    public ulong unkOffset1;
    public ulong unkOffset2;
    public ulong unkOffset3;
    public ulong footerOffset;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct MapPhysicsFooter {
    public ulong bboxOffset;
    public ulong vertexOffset;
    public ulong indexOffset;
    public uint bboxCount;
    public uint vertexCount;
    public uint indexCount;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct MapVec3 {
    public float x;
    public float y;
    public float z;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct MapQuat {
    public float x;
    public float y;
    public float z;
    public float w;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct MapPhysicsVertex {
    public MapVec3 position;
    public float unk;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct MapPhysicsIndex {
    public MapPhysicsSlice3 index;
    public MapPhysicsSlice3 unk1;
    public MapPhysicsSlice2 unk2;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct MapPhysicsSlice3 {
    public int v1;
    public int v2;
    public int v3;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct MapPhysicsSlice2 {
    public int v1;
    public int v2;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct MapPhysicsBoundingBox {
    public MapVec3 minimum;
    public MapVec3 maximum;
    public uint unk1;
    public uint unk2;
  }

  public enum MAP_MANAGER_ERROR {
    E_SUCCESS = 0x00,
    E_ALREADY_ADDED = 0x01,
    E_FAULT = 0x02,
    E_FAULT_AT_ID = 0x03,
    E_FAULT_AT_NAME = 0x04,
    E_UNKNOWN_TYPE = 0x05,
    E_GENERIC = 0x06,
    E_DUPLICATE = 0x07
  }
  
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct MapCommonHeader {
    public GuidT guid;
    public ushort mask;
    public byte unk;
    public byte type;
    public uint size;
  }
}
