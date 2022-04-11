using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ivanov_WF_Paint
{
    /// <summary>
    /// Layer - один слой
    /// </summary>
    public class Layer
    {
        //картинка со слоем
        public Bitmap img;
        private float transition;

        //свойство прозрачночти
        public float Transition
        {
            get { return transition; }
            set { transition = value; }
        }

        //конструктор задаёт размеры картинки слоя и прозрачность
        public Layer(int w, int h, float tr)
        {
            img = new Bitmap(w, h);
            transition = tr;
        }
    }


    /// <summary>
    /// BitmapLayers - список слоев
    /// </summary>
    public class BitmapLayers
    {
        //размеры картинок слоёв
        int width, height;
        int count = 0;

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

        public int Count
        {
            get { return count; }
        }

        //список слоёв
        public List<Layer> layers = new List<Layer>();

        //конструктор задаёт размеры и количество слоёв
        public BitmapLayers(int w, int h)
        {
            width = w;
            height = h;
        }

        //добавление слоя в список
        public void Add(Layer l)
        {
            layers.Add(l);
            count++;
        }

        //удаление слоя по номеру
        public void RemoveAt(int n)
        {
            if (n < layers.Count)
            {
                layers.RemoveAt(n);
                count--;
            }
        }

        //очистка списка
        public void Clear()
        {
            for (int i = 0; i < layers.Count; i++)
            {
                layers.Clear();
                count = 0;
            }
        }

        //показ всех слоёв в picturebox
        public void Show(PictureBox pic)
        {
            //создание результирующей картинки
            Bitmap res = new Bitmap(width, height);

            //graphics для результирующей картинки
            Graphics resgr = Graphics.FromImage(res);

            //первый слой является фоном общей картинки
            Graphics gr = Graphics.FromImage(layers[0].img);

            //создание атрибутов изображения
            ImageAttributes attr = new ImageAttributes();
            //белый цвет делаем прозрачным
            attr.SetColorKey(Color.FromArgb(255, 255, 255), Color.FromArgb(255, 255, 255));

            //рисование фона на картинке
            resgr.DrawImage(layers[0].img, new Rectangle(0, 0, 800, 450), 0, 0, 800, 450, GraphicsUnit.Pixel);

            //отображение всех слоёв на результирующей картинке с учётом прозрачности
            for (int k = 1; k < layers.Count; k++)
            {
                //матрица цветов задаёт прозрачноть для каждого слоя
                ColorMatrix myColorMatrix = new ColorMatrix();
                myColorMatrix.Matrix00 = 1.00f;
                myColorMatrix.Matrix11 = 1.00f;
                myColorMatrix.Matrix22 = 1.00f;
                myColorMatrix.Matrix33 = layers[k].Transition;

                //применение матрицы
                attr.SetColorMatrix(myColorMatrix);

                //отображение слоя
                resgr.DrawImage(layers[k].img, new Rectangle(0, 0, 800, 450), 0, 0, 800, 450, GraphicsUnit.Pixel, attr);
            }

            //выбор результирующей картинки для показа в picturebox
            pic.Image = res;

            gr.Dispose();
            resgr.Dispose();
        }
    }
}
