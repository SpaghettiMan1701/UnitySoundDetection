using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers;
using UnityEngine;

namespace Octrees
{
    public class Graph
    {
        public readonly Dictionary<OctreeNode, Node> nodes = new();
        public readonly HashSet<Edge> edges = new();

        List<Node> pathList = new();

        public void AddNode(OctreeNode octreeNode)
        {
            if (!nodes.ContainsKey(octreeNode))
            {
                nodes.Add(octreeNode, new Node(octreeNode));
            }
        }

        public void AddEdge(OctreeNode a, OctreeNode b)
        {
            Node nodeA = FindNode(a);
            Node nodeB = FindNode(b);

            if(nodeA == null || nodeB == null)
                return;

            Edge edge = new(nodeA, nodeB);
            if (edges.Add(edge))
            {
                nodeA.edges.Add(edge);
                nodeB.edges.Add(edge);
            }
        }

        public void DrawGraph()
        {
            Gizmos.color = Color.yellow;
            foreach (Edge edge in edges)
            {
                Gizmos.DrawLine(edge.a.octreeNode.bounds.center, edge.b.octreeNode.bounds.center);
            }
            Gizmos.color = Color.blue;
            foreach(Node node in nodes.Values)
            {
                Gizmos.DrawWireSphere(node.octreeNode.bounds.center, 0.2f);
            }
        }

        Node FindNode(OctreeNode octreeNode)
        {
            nodes.TryGetValue(octreeNode, out Node node);
            return node;
        }
    }
    public class Node
    {
        static int nextId;
        public readonly int id;

        public float f, g, h;
        public Node from;

        public List<Edge> edges = new();

        public OctreeNode octreeNode;

        public Node(OctreeNode octreeNode)
        {
            this.octreeNode = octreeNode;
            this.id = nextId++;
        }

        public override bool Equals(object obj) => obj is Node other && id == other.id;
        public override int GetHashCode() => id.GetHashCode();
    }
    public class Edge
    {
        public readonly Node a, b;
        public Edge(Node a, Node b)
        {
            this.a = a;
            this.b = b;
        }

        public override bool Equals(object obj)
        {
            return obj is Edge other && ((a == other.a && b == other.b) || (a == other.b && b == other.a));
        }
        public override int GetHashCode() => a.GetHashCode() ^ b.GetHashCode();
    }
}