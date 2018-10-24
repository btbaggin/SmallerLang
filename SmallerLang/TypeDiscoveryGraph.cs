using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;

namespace SmallerLang
{
    class DiscoveryNode
    {
        public TypeDefinitionSyntax Node { get; private set; }
        public bool Permanent { get; internal set; }
        public bool Temporary { get; internal set; }

        public DiscoveryNode(TypeDefinitionSyntax pNode)
        {
            Node = pNode;
            Permanent = false;
            Temporary = false;
        }
    }

    class TypeDiscoveryGraph
    {
        readonly List<DiscoveryNode>[] _structs;
        readonly Dictionary<string, int>[] _structIndex;
        readonly Dictionary<string, int> _namespaceIndex;

        public TypeDiscoveryGraph()
        {
            _namespaceIndex = new Dictionary<string, int>();
            _structs = new List<DiscoveryNode>[8];
            _structIndex = new Dictionary<string, int>[8];
        }

        public void AddNode(string pNamespace, TypeDefinitionSyntax pNode)
        {
            if (!_namespaceIndex.ContainsKey(pNamespace))
            {
                var i = _namespaceIndex.Count;
                _namespaceIndex.Add(pNamespace, i);
                _structs[i] = new List<DiscoveryNode>();
                _structIndex[i] = new Dictionary<string, int>();
            }

            var idx = GetNamespaceIndex(pNamespace);
            _structIndex[idx].Add(pNode.Name, _structs[idx].Count);
            _structs[idx].Add(new DiscoveryNode(pNode));
        }

        private int GetNamespaceIndex(string pNamespace)
        {
            if (!_namespaceIndex.ContainsKey(pNamespace)) return _namespaceIndex.Count - 1;
            return _namespaceIndex[pNamespace];
        }

        public DiscoveryNode GetNode(string pName)
        {
            var ns = SmallTypeCache.GetNamespace(ref pName);
            var i = GetNamespaceIndex(ns);

            return _structs[i][_structIndex[i][pName]];
        }

        public bool NodeExists(string pName)
        {
            var i = _namespaceIndex.Count - 1;

            return _structIndex[i].ContainsKey(pName);
        }

        public IList<DiscoveryNode> GetNodes(string pNamespace)
        {
            var i = GetNamespaceIndex(pNamespace);

            return _structs[i];
        }
    }
}
