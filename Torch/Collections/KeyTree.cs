using System.Collections.Generic;

namespace Torch.Collections
{
    public class KeyTree<TKey, TValue>
    {
        private Dictionary<TKey, KeyTreeNode<TKey, TValue>> _nodes = new Dictionary<TKey, KeyTreeNode<TKey, TValue>>();

        public KeyTreeNode<TKey, TValue> this[TKey key] => _nodes[key];

        public void AddNode(TKey key, TValue value)
        {
            _nodes.Add(key, new KeyTreeNode<TKey, TValue>(key, value));
        }

        public bool RemoveNode(TKey key)
        {
            return _nodes.Remove(key);
        }

        public IEnumerable<KeyTreeNode<TKey, TValue>> Traverse()
        {
            foreach (var node in _nodes.Values)
                foreach (var child in node.Traverse())
                    yield return child;
        }
    }

    public class KeyTreeNode<TKey, TValue>
    {
        public TKey Key { get; }
        public TValue Value { get; set; }
        public KeyTreeNode<TKey, TValue> Parent { get; private set; }
        private readonly Dictionary<TKey, KeyTreeNode<TKey, TValue>> _children = new Dictionary<TKey, KeyTreeNode<TKey, TValue>>();

        public IEnumerable<KeyTreeNode<TKey, TValue>> Children => _children.Values;

        public KeyTreeNode(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public KeyTreeNode<TKey, TValue> this[TKey key] => _children[key];

        public KeyTreeNode<TKey, TValue> GetChild(TKey key)
        {
            if (_children.TryGetValue(key, out KeyTreeNode<TKey, TValue> value))
                return value;

            return null;
        }

        public bool AddChild(TKey key, TValue value)
        {
            if (_children.ContainsKey(key))
                return false;

            var node = new KeyTreeNode<TKey, TValue>(key, value) { Parent = this };
            _children.Add(key, node);
            return true;
        }

        public bool AddChild(KeyTreeNode<TKey, TValue> node)
        {
            if (node.Parent != null || _children.ContainsKey(node.Key))
                return false;

            node.Parent = this;
            _children.Add(node.Key, node);
            return true;
        }

        public bool RemoveChild(TKey key)
        {
            if (!_children.TryGetValue(key, out KeyTreeNode<TKey, TValue> value))
                return false;

            value.Parent = null;
            _children.Remove(key);
            return true;
        }

        public IEnumerable<KeyTreeNode<TKey, TValue>> Traverse()
        {
            foreach (var node in Children)
            {
                yield return node;
                foreach (var child in node.Traverse())
                {
                    yield return child;
                }
            }
        }
    }
}
