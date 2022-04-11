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
    public delegate void TextSettingsDelegate(string text, Color color, string size);

    public partial class TextSettings : Form
    {
        public event TextSettingsDelegate PerformText;

        public TextSettings()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            PerformText?.Invoke(textBox1.Text, colorButton.BackColor, textBox2.Text);
            this.Close();
        }

        private void colorButton_Click(object sender, EventArgs e)
        {
            ColorDialog color = new ColorDialog();
            color.FullOpen = true;
            color.AllowFullOpen = true;
            if (color.ShowDialog() == DialogResult.OK)
            {
                colorButton.BackColor = color.Color;
            }
        }
    }
}
