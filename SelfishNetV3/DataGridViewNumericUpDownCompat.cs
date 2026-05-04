using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace DataGridViewNumericUpDownElements
{
    public sealed class DataGridViewNumericUpDownColumn : DataGridViewColumn
    {
        public DataGridViewNumericUpDownColumn()
            : base(new DataGridViewNumericUpDownCell())
        {
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public decimal Minimum
        {
            get => NumericCell.Minimum;
            set => NumericCell.Minimum = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public decimal Maximum
        {
            get => NumericCell.Maximum;
            set => NumericCell.Maximum = value;
        }

        private DataGridViewNumericUpDownCell NumericCell => (DataGridViewNumericUpDownCell)CellTemplate!;

        public override object Clone()
        {
            var clone = (DataGridViewNumericUpDownColumn)base.Clone();
            clone.Minimum = Minimum;
            clone.Maximum = Maximum;
            return clone;
        }
    }

    public sealed class DataGridViewNumericUpDownCell : DataGridViewTextBoxCell
    {
        public decimal Minimum { get; set; }

        public decimal Maximum { get; set; } = 99999999M;

        public override Type EditType => typeof(DataGridViewNumericUpDownEditingControl);

        public override Type ValueType => typeof(decimal);

        public override object DefaultNewRowValue => 0M;

        public override object Clone()
        {
            var clone = (DataGridViewNumericUpDownCell)base.Clone();
            clone.Minimum = Minimum;
            clone.Maximum = Maximum;
            return clone;
        }

        public override void InitializeEditingControl(int rowIndex, object initialFormattedValue, DataGridViewCellStyle dataGridViewCellStyle)
        {
            base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);

            if (DataGridView?.EditingControl is not DataGridViewNumericUpDownEditingControl control)
            {
                return;
            }

            control.Minimum = Minimum;
            control.Maximum = Maximum;
            control.DecimalPlaces = 0;
            control.ThousandsSeparator = false;

            var value = Value ?? DefaultNewRowValue;
            if (!decimal.TryParse(Convert.ToString(value), out var decimalValue))
            {
                decimalValue = 0M;
            }

            control.Value = Math.Min(control.Maximum, Math.Max(control.Minimum, decimalValue));
        }
    }

    public sealed class DataGridViewNumericUpDownEditingControl : NumericUpDown, IDataGridViewEditingControl
    {
        private DataGridView dataGridView;
        private bool valueChanged;
        private int rowIndex;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object EditingControlFormattedValue
        {
            get => Value.ToString();
            set
            {
                if (decimal.TryParse(Convert.ToString(value), out var parsed))
                {
                    Value = Math.Min(Maximum, Math.Max(Minimum, parsed));
                }
            }
        }

        public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context) => EditingControlFormattedValue;

        public void ApplyCellStyleToEditingControl(DataGridViewCellStyle dataGridViewCellStyle)
        {
            Font = dataGridViewCellStyle.Font;
            ForeColor = dataGridViewCellStyle.ForeColor;
            BackColor = dataGridViewCellStyle.BackColor;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int EditingControlRowIndex
        {
            get => rowIndex;
            set => rowIndex = value;
        }

        public bool EditingControlWantsInputKey(Keys keyData, bool dataGridViewWantsInputKey)
        {
            return (keyData & Keys.KeyCode) switch
            {
                Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown => true,
                _ => !dataGridViewWantsInputKey
            };
        }

        public void PrepareEditingControlForEdit(bool selectAll)
        {
            if (selectAll)
            {
                Select(0, Text.Length);
            }
        }

        public bool RepositionEditingControlOnValueChange => false;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DataGridView EditingControlDataGridView
        {
            get => dataGridView;
            set => dataGridView = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EditingControlValueChanged
        {
            get => valueChanged;
            set => valueChanged = value;
        }

        public Cursor EditingPanelCursor => base.Cursor;

        protected override void OnValueChanged(EventArgs e)
        {
            valueChanged = true;
            dataGridView?.NotifyCurrentCellDirty(true);
            base.OnValueChanged(e);
        }
    }
}
