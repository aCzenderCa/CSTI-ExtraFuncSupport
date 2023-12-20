using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CSTI_LuaActionSupport.LuaCodeHelper;
using UnityEngine;

namespace CSTI_LuaActionSupport.AllPatcher;

public struct DataNode
{
    [StructLayout(LayoutKind.Explicit)]
    public struct DataNodeDataUnion
    {
        [FieldOffset(8)] public double number;
        [FieldOffset(8)] public bool _bool;
        [FieldOffset(8)] public Vector2 vector2;

        [FieldOffset(0)] public string? str;
        [FieldOffset(0)] public Dictionary<string, DataNode>? table;

        public DataNodeDataUnion(Dictionary<int, DataNode> intTable)
        {
            table = intTable.ToDictionary(pair => pair.Key.ToString(), pair => pair.Value);
        }

        public DataNodeDataUnion(double num)
        {
            number = num;
        }

        public DataNodeDataUnion(string str)
        {
            this.str = str;
        }

        public DataNodeDataUnion(bool b)
        {
            _bool = b;
        }

        public DataNodeDataUnion(Vector2 vector2)
        {
            this.vector2 = vector2;
        }

        public DataNodeDataUnion(Dictionary<string, DataNode> table)
        {
            this.table = table;
        }
    }

    public enum DataNodeType
    {
        Number,
        Str,
        Bool,
        Table,
        Nil,
        Vector2,
        IntTable
    }

    public DataNodeType NodeType;
    public DataNodeDataUnion NodeData;

    public double number => NodeData.number;
    public string? str => NodeData.str;
    public bool _bool => NodeData._bool;
    public Vector2 vector2 => NodeData.vector2;
    public Dictionary<string, DataNode>? table => NodeData.table;
    public Dictionary<string, DataNode>? INTTable => NodeData.table;

    public static DataNode EmptyTable => new(new Dictionary<string, DataNode>());

    public static DataNode Nil
    {
        get
        {
            DataNode nil = default;
            nil.NodeType = DataNodeType.Nil;
            return nil;
        }
    }

    public DataNode(double number)
    {
        NodeType = DataNodeType.Number;
        NodeData = new DataNodeDataUnion(number);
    }

    public DataNode(string str)
    {
        NodeType = DataNodeType.Str;
        NodeData = new DataNodeDataUnion(str);
    }

    public DataNode(bool b)
    {
        NodeType = DataNodeType.Bool;
        NodeData = new DataNodeDataUnion(b);
    }

    public DataNode(Vector2 vector2)
    {
        NodeType = DataNodeType.Vector2;
        NodeData = new DataNodeDataUnion(vector2);
    }

    public DataNode(Dictionary<string, DataNode> dataNodes)
    {
        NodeType = DataNodeType.Table;
        NodeData = new DataNodeDataUnion(dataNodes);
    }

    public DataNode(Dictionary<int, DataNode> dataNodes)
    {
        NodeType = DataNodeType.IntTable;
        NodeData = new DataNodeDataUnion(dataNodes);
    }

    public void Save(BinaryWriter binaryWriter)
    {
        binaryWriter.Write((int) NodeType);
        switch (NodeType)
        {
            case DataNodeType.Number:
                binaryWriter.Write(number);
                break;
            case DataNodeType.Str:
                binaryWriter.Write(str ?? "");
                break;
            case DataNodeType.Bool:
                binaryWriter.Write(_bool);
                break;
            case DataNodeType.Table:
                if (table == null)
                {
                    binaryWriter.Write(0);
                }
                else
                {
                    binaryWriter.Write(table.Count(pair => pair.Value.NodeType != DataNodeType.Nil));
                    foreach (var (key, node) in table)
                    {
                        if (node.NodeType == DataNodeType.Nil) continue;
                        binaryWriter.Write(key);
                        node.Save(binaryWriter);
                    }
                }

                break;
            case DataNodeType.Nil:
                break;
            case DataNodeType.Vector2:
                binaryWriter.Write(vector2.x);
                binaryWriter.Write(vector2.y);
                break;
            case DataNodeType.IntTable:
                if (INTTable == null) binaryWriter.Write(0);
                else
                {
                    binaryWriter.Write(INTTable.Count);
                    foreach (var (key, node) in INTTable)
                    {
                        if (!int.TryParse(key, out var ikey)) continue;
                        binaryWriter.Write(ikey);
                        node.Save(binaryWriter);
                    }
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static DataNode Load(BinaryReader binaryReader)
    {
        DataNode node = default;
        node.NodeType = (DataNodeType) binaryReader.ReadInt32();
        switch (node.NodeType)
        {
            case DataNodeType.Number:
                node.NodeData = new DataNodeDataUnion(binaryReader.ReadDouble());
                break;
            case DataNodeType.Str:
                node.NodeData = new DataNodeDataUnion(binaryReader.ReadString());
                break;
            case DataNodeType.Bool:
                node.NodeData = new DataNodeDataUnion(binaryReader.ReadBoolean());
                break;
            case DataNodeType.Table:
                var count = binaryReader.ReadInt32();
                var dataNodes = new Dictionary<string, DataNode>();
                for (var i = 0; i < count; i++)
                {
                    var key = binaryReader.ReadString();
                    var dataNode = Load(binaryReader);
                    dataNodes[key] = dataNode;
                }

                node.NodeData = new DataNodeDataUnion(dataNodes);
                break;
            case DataNodeType.Nil:
                break;
            case DataNodeType.Vector2:
                var x = binaryReader.ReadSingle();
                var y = binaryReader.ReadSingle();
                node.NodeData = new DataNodeDataUnion(new Vector2(x, y));
                break;
            case DataNodeType.IntTable:
                var l_count = binaryReader.ReadInt32();
                var nodes = new Dictionary<int, DataNode>(l_count);
                for (int i = 0; i < l_count; i++)
                {
                    var key = binaryReader.ReadInt32();
                    nodes[key] = Load(binaryReader);
                }

                node.NodeData = new DataNodeDataUnion(nodes);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return node;
    }
}