using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang.Lexer
{
    class Trie
    {
        public TrieNode Root { get; private set; }

        public Trie()
        {
            Root = new TrieNode(' ', 0, false, TokenType.EndOfFile);
        }

        public TrieNode Prefix(ReadOnlySpan<char> s)
        {
            var currentNode = Root;
            var result = currentNode;

            foreach (var c in s)
            {
                currentNode = currentNode.FindChild(c);
                if (currentNode == null)
                    break;
                result = currentNode;
            }

            return result;
        }

        public void Insert(string s, TokenType pType)
        {
            var current = Prefix(s.AsSpan());

            //This allows items that are substrings of already added items to work
            //This ensures we don't need to be careful about how we add the items to the Trie
            if(current.Depth == s.Length)
            {
                current.MakeLeaf(pType);
                return;
            }

            //We still have characters we need to add to the Trie, add the levels until we reach the end
            TrieNode newNode;
            for (var i = current.Depth; i < s.Length - 1; i++)
            {
                newNode = new TrieNode(s[i], i + 1, false, TokenType.EndOfFile);
                current.Children.Add(newNode);
                current = newNode;
            }

            newNode = new TrieNode(s[s.Length - 1], s.Length, true, pType);
            current.Children.Add(newNode);
        }
    }

    class TrieNode
    {
        public char Value { get; private set; }
        public List<TrieNode> Children { get; private set; }
        public int Depth { get; private set; }
        public bool Leaf { get; private set; }
        public TokenType Type { get; private set; }

        public TrieNode(char pValue, int pDepth, bool pLeaf, TokenType pType)
        {
            Value = pValue;
            Children = new List<TrieNode>();
            Depth = pDepth;
            Leaf = pLeaf;
            Type = pType;
        }

        public TrieNode FindChild(char pC)
        {
            foreach (var c in Children)
            {
                if(c.Value == pC) return c;
            }
                
            return null;
        }

        internal void MakeLeaf(TokenType pType)
        {
            Leaf = true;
            Type = pType;
        }
    }
}
