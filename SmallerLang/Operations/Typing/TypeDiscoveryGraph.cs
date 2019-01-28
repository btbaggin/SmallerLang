using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Utils;

namespace SmallerLang.Operations.Typing
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
        readonly Dictionary<string, int> _structIndex;
        readonly List<DiscoveryNode> _structs;

        public TypeDiscoveryGraph()
        {
            _structs = new List<DiscoveryNode>();
            _structIndex = new Dictionary<string, int>();
        }

        public void AddNode(TypeDefinitionSyntax pNode)
        {
            var name = SyntaxHelper.GetFullTypeName(pNode.GetApplicableType());

            var idx = _structs.Count;
            _structIndex.Add(name, idx);
            _structs.Add(new DiscoveryNode(pNode));
        }

        public DiscoveryNode GetNode(string pName)
        {

            var i = _structIndex[pName];
            return _structs[i];
        }

        public bool NodeExists(string pName)
        {
            return _structIndex.ContainsKey(pName);
        }

        public IList<DiscoveryNode> GetNodes()
        {
            return _structs;
        }
    }
}
