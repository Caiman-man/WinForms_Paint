using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ivanov_WF_Paint
{
    public class Picture
    {
        public List<Graphics> g;
        Bitmap b;

        string name = "";
        int width, height;
        public BitmapLayers bmpLayersList;
        int layersListCount;

        //свойства
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public int Width
        {
            get { return width; }
            set { width = value; }
        }

        public int Height
        {
            get { return height; }
            set { height = value; }
        }

        public int LayersListCount
        {
            get { return layersListCount; }
        }

        public Bitmap BMP
        {
            get { return b; }
            set { b = value; }
        }

        //public Graphics GR
        //{
        //    get { return g; }
        //    set { g = value; }
        //}

        //конструктор
        public Picture(string _name, int _width, int _height)
        {
            this.name = _name;
            this.width = _width;
            this.height = _height;
            this.bmpLayersList = new BitmapLayers(_width, _height);
            this.layersListCount = 0;
            b = new Bitmap(width, height);
            //g = Graphics.FromImage(b);
            g = new List<Graphics>();
        }
    }
}
