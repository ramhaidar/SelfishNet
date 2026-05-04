using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace AdvancedDataGridView
{
    public sealed class TreeGridColumn : DataGridViewTextBoxColumn
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Image DefaultNodeImage { get; set; }
    }

    public sealed class TreeGridView : DataGridView
    {
        public TreeGridView()
        {
            Nodes = new TreeGridNodeCollection(this, null);
        }

        public TreeGridNodeCollection Nodes { get; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ImageList ImageList { get; set; }
    }

    public sealed class TreeGridNode
    {
        private int height = -1;
        private int imageIndex = -1;

        public TreeGridNode()
        {
            Nodes = new TreeGridNodeCollection(null, this);
        }

        internal TreeGridView Owner { get; private set; }

        internal TreeGridNode Parent { get; private set; }

        internal DataGridViewRow Row { get; private set; }

        public TreeGridNodeCollection Nodes { get; }

        public DataGridViewCellCollection Cells
        {
            get
            {
                EnsureAttached();
                return Row!.Cells;
            }
        }

        public int Height
        {
            get => Row?.Height ?? height;
            set
            {
                height = value;
                if (Row != null)
                {
                    Row.Height = value;
                }
            }
        }

        public int ImageIndex
        {
            get => imageIndex;
            set => imageIndex = value;
        }

        public void Expand()
        {
            foreach (var node in Nodes)
            {
                if (node.Row != null)
                {
                    node.Row.Visible = true;
                }
            }
        }

        internal void Attach(TreeGridView owner, TreeGridNode parent, object[] values)
        {
            Owner = owner;
            Parent = parent;
            Nodes.Owner = owner;

            Row = new DataGridViewRow();
            Row.CreateCells(owner, NormalizeValues(owner, values));
            Row.Tag = this;
            if (height > 0)
            {
                Row.Height = height;
            }

            owner.Rows.Add(Row);
        }

        internal void Detach()
        {
            foreach (var child in Nodes.ToArray())
            {
                child.Detach();
            }

            if (Owner != null && Row != null && Owner.Rows.Contains(Row))
            {
                Owner.Rows.Remove(Row);
            }

            Row = null;
            Owner = null;
            Parent = null;
        }

        private void EnsureAttached()
        {
            if (Row == null)
            {
                throw new InvalidOperationException("TreeGridNode is not attached to a TreeGridView.");
            }
        }

        private static object[] NormalizeValues(TreeGridView owner, object[] values)
        {
            var normalized = new object[owner.Columns.Count];
            for (var i = 0; i < normalized.Length && i < values.Length; i++)
            {
                normalized[i] = values[i];
            }

            return normalized;
        }
    }

    public sealed class TreeGridNodeCollection : IReadOnlyList<TreeGridNode>
    {
        private readonly List<TreeGridNode> nodes = new();
        private readonly TreeGridNode parent;

        internal TreeGridNodeCollection(TreeGridView owner, TreeGridNode parent)
        {
            Owner = owner;
            this.parent = parent;
        }

        internal TreeGridView Owner { get; set; }

        public int Count => nodes.Count;

        public TreeGridNode this[int index] => nodes[index];

        public TreeGridNode Add(params object[] values)
        {
            var node = new TreeGridNode();
            Add(node, values);
            return node;
        }

        public void Add(TreeGridNode node)
        {
            Add(node, Array.Empty<object>());
        }

        private void Add(TreeGridNode node, object[] values)
        {
            var owner = Owner ?? parent?.Owner;
            if (owner == null)
            {
                throw new InvalidOperationException("TreeGridNodeCollection is not attached to a TreeGridView.");
            }

            nodes.Add(node);
            node.Attach(owner, parent, values);
        }

        public void RemoveAt(int index)
        {
            var node = nodes[index];
            nodes.RemoveAt(index);
            node.Detach();
        }

        public TreeGridNode[] ToArray() => nodes.ToArray();

        public IEnumerator<TreeGridNode> GetEnumerator() => nodes.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
