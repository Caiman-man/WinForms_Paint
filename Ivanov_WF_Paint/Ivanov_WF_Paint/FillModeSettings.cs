using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ivanov_WF_Paint
{
    public delegate void FillModeDelegate(string mode, Color first, Color second);

    public partial class FillModeSettings : Form
    {
        public event FillModeDelegate PerformFillMode;

        Color color1 = Color.Black;
        Color color2 = Color.White;

        public FillModeSettings()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            PerformFillMode?.Invoke(comboBox1.Text, firstColorButton.BackColor, secondColorButton.BackColor);
            this.Close();
        }

        private void firstColorButton_Click(object sender, EventArgs e)
        {
            ColorDialog color = new ColorDialog();
            color.FullOpen = true;
            color.AllowFullOpen = true;
            if (color.ShowDialog() == DialogResult.OK)
            {
                firstColorButton.BackColor = color.Color;
            }
        }

        private void secondColorButton_Click(object sender, EventArgs e)
        {
            ColorDialog color = new ColorDialog();
            color.FullOpen = true;
            color.AllowFullOpen = true;
            if (color.ShowDialog() == DialogResult.OK)
            {
                secondColorButton.BackColor = color.Color;
            }
        }
    }
}
