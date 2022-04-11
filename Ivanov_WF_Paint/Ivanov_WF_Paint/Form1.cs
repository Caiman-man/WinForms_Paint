using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Linq;

/*
    Разработать простой графический редактор, который обладает MDI-интерфейсом или вкладки и имеет следующие функции:
    +	рисует линии, прямоугольники, треугольники, эллипсы, текстовые строки (залитые и не залитые)
    +	выбор кисти для заливки примитивов (градиентная, узорчатая, сплошная)
    +	позволяет в диалоговом окне выбрать цвет линий
    +	позволяет в диалоговом окне выбрать толщину линий при помощи TrackBar
    +	имеет общее для всех документов меню
    -	позволяет сохранять/загружать документы в своём формате (New, Open, Save, Save all, Close, Close all)
    +-	позволяет работать со слоями (добавление слоя, удаление слоя, выбор слоя для рисования, сокрытие/показ слоя, изменение прозрачности слоя)
    +-	реализовать отдельную панель для управления слоями на основе ListView или checkedlistBox
    +	реализовать функцию преобразования изображения в чёрно-белое (быстрый способ I = 0.3*R + 0.4*G + 0.3*B)
    +-	Загрузка/сохрание документов в форматах bmp, jpg, png
*/

namespace Ivanov_WF_Paint
{
    public partial class Form1 : Form
    {
        //создание обьекта для рисования
        Bitmap bmp;
        Graphics gr;

        //список слоев заданного размера
        BitmapLayers bmpLayers = new BitmapLayers(800, 450);
        //переменная для нумерации слоев
        int layersCount = 0;

        //элементы LINQ
        XDocument XDoc;
        XElement XElem;

        //кисти для карандаша и ластика
        Pen pen = new Pen(Color.Black, 3);
        Pen eraser = new Pen(Color.White, 15);
        //режим рисования
        bool paint = false;
        //режим фигур
        int mode;

        //координаты для карандаша и ластика
        Point pX, pY;
        //координаты для фигур
        int X, Y, X1, Y1, X2, Y2;

        //данные для режима заполнения
        Color firstFillModeColor = Color.Black;
        Color secondFillModeColor = Color.White;
        string fillMode = "Fill";

        //данные для режима текста
        string text;
        Color textColor;
        int textSize;

        public Form1()
        {
            InitializeComponent();
            SetPictureSize();
            SetPenOptions();
            fillModeSettingsButton.Enabled = false;
            XDoc = new XDocument();
            XElem = new XElement("picture.xml");
        }

        //установка размера картинки
        public void SetPictureSize()
        {
            bmp = new Bitmap(800, 450);
            gr = Graphics.FromImage(bmp);
            gr.Clear(Color.White);
            pictureBox1.Image = bmp;

            //добавляем начальную картинку как первый слой
            Layer l = new Layer(800, 450, 1f);
            l.img = bmp;
            Graphics g = Graphics.FromImage(l.img);
            bmpLayers.Add(l);
            listBox1.Items.Add($"layer {++layersCount}");
            listBox1.SetSelected(0, true);

            //сразу показываем картинку в окне предпросмотра всех слоев
            bmpLayers.Show(pictureBox2);

            //отключаем кнопку удаления слоя
            deleteLayerButton.Enabled = false;
        }

        //установка параметров кистей
        public void SetPenOptions()
        {
            pen.SetLineCap(System.Drawing.Drawing2D.LineCap.Round, System.Drawing.Drawing2D.LineCap.Round, System.Drawing.Drawing2D.DashCap.Round);
            eraser.SetLineCap(System.Drawing.Drawing2D.LineCap.Round, System.Drawing.Drawing2D.LineCap.Round, System.Drawing.Drawing2D.DashCap.Round);
        }

        //exit
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        //закрытие окна
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = false;
        }

        //about
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About about = new About();
            about.Owner = this;
            DialogResult res = about.ShowDialog();
        }

        //MouseDown
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            //включаем режим рисования
            paint = true;

            //координаты карандаша
            pY = e.Location;

            //начальные координаты фигур
            X1 = e.X;
            Y1 = e.Y;

            //режим текста
            if (mode == 7)
            {
                string drawString = text;
                Font drawFont = new Font("Arial", textSize);
                SolidBrush drawBrush = new SolidBrush(textColor);
                gr.DrawString(drawString, drawFont, drawBrush, X1, Y1);
            }
        }

        //MouseMove
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (paint == true)
            {
                if (mode == 1)  //режим карандаша
                {
                    pX = e.Location;
                    gr.DrawLine(pen, pX, pY);
                    addPenOrEraserToXML("pen", pen.Color, pen.Width, pX, pY);
                    pY = pX;
                }
                if (mode == 2)  //режим ластика
                {
                    pX = e.Location;
                    gr.DrawLine(eraser, pX, pY);
                    addPenOrEraserToXML("eraser", eraser.Color, pen.Width, pX, pY);
                    pY = pX;
                }
            }
            pictureBox1.Refresh();

            //текущие координаты
            toolStripStatusLabel1.Text = "X: " + e.X.ToString() + " Y: " + e.Y.ToString();

            //текущие координаты фигур
            X = e.X;
            Y = e.Y;

            //конечные координаты фигур
            X2 = X - X1;
            Y2 = Y - Y1;
        }

        //MouseUp
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            //выключаем режим рисования
            paint = false;

            if (mode == 3)                          //линия
            {
                //рисуем и добавляем данные в XML
                gr.DrawLine(pen, X1, Y1, X, Y);
                addFigureToXML("line", false, pen.Color, pen.Width, X1, Y1, X2, Y2, X, Y);
            }
            if (mode == 4)                          //прямоугольник
            {
                if (fillModeButton.Checked == false)
                {
                    if (X1 > X)
                    {
                        X2 = X1 - X;
                        X1 = X;
                    }
                    else
                    {
                        X2 = X - X1;
                    }
                    if (Y1 > Y)
                    {
                        Y2 = Y1 - Y;
                        Y1 = Y;
                    }
                    else
                    {
                        Y2 = Y - Y1;
                    }
                    gr.DrawRectangle(pen, X1, Y1, X2, Y2);
                    addFigureToXML("rectangle", false, pen.Color, pen.Width, X1, Y1, X2, Y2, X, Y);
                }
                else
                {
                    if (fillMode == "Fill")
                    {
                        Brush brush = new SolidBrush(firstFillModeColor);
                        gr.FillRectangle(brush, X1, Y1, X2, Y2);
                    }
                    else if (fillMode == "Gradient")
                    {
                        LinearGradientBrush linGrBrush = new LinearGradientBrush(new Point(0, 1), new Point(800, 1), firstFillModeColor, secondFillModeColor);
                        gr.FillRectangle(linGrBrush, X1, Y1, X2, Y2);
                    }
                    else if (fillMode == "Design")
                    {
                        HatchBrush hBrush = new HatchBrush(HatchStyle.Percent20, firstFillModeColor, secondFillModeColor);
                        gr.FillRectangle(hBrush, X1, Y1, X2, Y2);
                    }
                    addFigureToXML("rectangle", true, pen.Color, pen.Width, X1, Y1, X2, Y2, X, Y);
                }
            }
            if (mode == 5)                          //эллипс
            {
                if (fillModeButton.Checked == false)
                {
                    gr.DrawEllipse(pen, X1, Y1, X2, Y2);
                    addFigureToXML("ellipse", false, pen.Color, pen.Width, X1, Y1, X2, Y2, X, Y);
                }
                else
                {
                    if (fillMode == "Fill")
                    {
                        Brush brush = new SolidBrush(firstFillModeColor);
                        gr.FillEllipse(brush, X1, Y1, X2, Y2);
                    }
                    else if (fillMode == "Gradient")
                    {
                        LinearGradientBrush linGrBrush = new LinearGradientBrush(new Point(0, 1), new Point(800, 1), firstFillModeColor, secondFillModeColor);
                        gr.FillEllipse(linGrBrush, X1, Y1, X2, Y2);
                    }
                    else if (fillMode == "Design")
                    {
                        HatchBrush hBrush = new HatchBrush(HatchStyle.Percent20, firstFillModeColor, secondFillModeColor);
                        gr.FillEllipse(hBrush, X1, Y1, X2, Y2);
                    }
                    addFigureToXML("ellipse", true, pen.Color, pen.Width, X1, Y1, X2, Y2, X, Y);
                }
            }
            if (mode == 6)                          //прямоугольный треугольник
            {
                if (fillModeButton.Checked == false)
                {
                    gr.DrawPolygon(pen, new Point[] { new Point(X, Y), new Point(X1, Y1), new Point(X1, Y) });
                    addFigureToXML("triangle", false, pen.Color, pen.Width, X1, Y1, X2, Y2, X, Y);
                }
                else
                {
                    if (fillMode == "Fill")
                    {
                        Brush brush = new SolidBrush(firstFillModeColor);
                        gr.FillPolygon(brush, new Point[] { new Point(X, Y), new Point(X1, Y1), new Point(X1, Y) });
                    }
                    else if (fillMode == "Gradient")
                    {
                        LinearGradientBrush linGrBrush = new LinearGradientBrush(new Point(0, 1), new Point(800, 1), firstFillModeColor, secondFillModeColor);
                        gr.FillPolygon(linGrBrush, new Point[] { new Point(X, Y), new Point(X1, Y1), new Point(X1, Y) });
                    }
                    else if (fillMode == "Design")
                    {
                        HatchBrush hBrush = new HatchBrush(HatchStyle.Percent20, firstFillModeColor, secondFillModeColor);
                        gr.FillPolygon(hBrush, new Point[] { new Point(X, Y), new Point(X1, Y1), new Point(X1, Y) });
                    }
                    addFigureToXML("triangle", true, pen.Color, pen.Width, X1, Y1, X2, Y2, X, Y);
                }
            }
            //показываем нарисованную фигуру в окне предпросмотра всех слоёв
            bmpLayers.Show(pictureBox2);
        }

        //Paint - работает, кроме прямоугольника при движении мыши влево или вверх
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            Graphics gr = e.Graphics;
            if (paint == true)
            {
                if (mode == 3)
                {
                    gr.DrawLine(pen, X1, Y1, X, Y);
                }
                if (mode == 4)
                {
                    if (fillModeButton.Checked == false)
                    {
                        gr.DrawRectangle(pen, X1, Y1, X2, Y2);
                    }
                    else
                    {
                        if (fillMode == "Fill")
                        {
                            Brush brush = new SolidBrush(firstFillModeColor);
                            gr.FillRectangle(brush, X1, Y1, X2, Y2);
                        }
                        else if (fillMode == "Gradient")
                        {
                            LinearGradientBrush linGrBrush = new LinearGradientBrush(new Point(0, 1), new Point(800, 1), firstFillModeColor, secondFillModeColor);
                            gr.FillRectangle(linGrBrush, X1, Y1, X2, Y2);
                        }
                        else if (fillMode == "Design")
                        {
                            HatchBrush hBrush = new HatchBrush(HatchStyle.Percent20, firstFillModeColor, secondFillModeColor);
                            gr.FillRectangle(hBrush, X1, Y1, X2, Y2);
                        }
                    }
                }
                if (mode == 5)
                {
                    if (fillModeButton.Checked == false)
                    {
                        gr.DrawEllipse(pen, X1, Y1, X2, Y2);
                    }
                    else
                    {
                        if (fillMode == "Fill")
                        {
                            Brush brush = new SolidBrush(firstFillModeColor);
                            gr.FillEllipse(brush, X1, Y1, X2, Y2);
                        }
                        else if (fillMode == "Gradient")
                        {
                            LinearGradientBrush linGrBrush = new LinearGradientBrush(new Point(0, 1), new Point(800, 1), firstFillModeColor, secondFillModeColor);
                            gr.FillEllipse(linGrBrush, X1, Y1, X2, Y2);
                        }
                        else if (fillMode == "Design")
                        {
                            HatchBrush hBrush = new HatchBrush(HatchStyle.Percent20, firstFillModeColor, secondFillModeColor);
                            gr.FillEllipse(hBrush, X1, Y1, X2, Y2);
                        }
                    }
                }
                if (mode == 6)
                {
                    if (fillModeButton.Checked == false)
                    {
                        gr.DrawPolygon(pen, new Point[] { new Point(X, Y), new Point(X1, Y1), new Point(X1, Y) });
                    }
                    else
                    {
                        if (fillMode == "Fill")
                        {
                            Brush brush = new SolidBrush(firstFillModeColor);
                            gr.FillPolygon(brush, new Point[] { new Point(X, Y), new Point(X1, Y1), new Point(X1, Y) });
                        }
                        else if (fillMode == "Gradient")
                        {
                            LinearGradientBrush linGrBrush = new LinearGradientBrush(new Point(0, 1), new Point(800, 1), firstFillModeColor, secondFillModeColor);
                            gr.FillPolygon(linGrBrush, new Point[] { new Point(X, Y), new Point(X1, Y1), new Point(X1, Y) });
                        }
                        else if (fillMode == "Design")
                        {
                            HatchBrush hBrush = new HatchBrush(HatchStyle.Percent20, firstFillModeColor, secondFillModeColor);
                            gr.FillPolygon(hBrush, new Point[] { new Point(X, Y), new Point(X1, Y1), new Point(X1, Y) });
                        }
                    }
                }
            }
        }

        //--------------------------------------------------------------
        //РЕЖИМЫ

        //pen
        private void pen_Click(object sender, EventArgs e)
        {
            mode = 1;
        }

        //eraser
        private void eraser_Click(object sender, EventArgs e)
        {
            mode = 2;
        }

        //линия
        private void line_Click(object sender, EventArgs e)
        {
            mode = 3;
        }

        //прямоугольник
        private void rectangle_Click(object sender, EventArgs e)
        {
            mode = 4;
        }

        //эллипс
        private void ellipse_Click(object sender, EventArgs e)
        {
            mode = 5;
        }

        //треугольник
        private void triangle_Click(object sender, EventArgs e)
        {
            mode = 6;
        }

        //текст
        private void textButton_Click(object sender, EventArgs e)
        {
            mode = 7;
            TextSettings text = new TextSettings();
            text.Owner = this;
            text.PerformText += GetTextSettings;
            DialogResult res = text.ShowDialog();
        }

        private void GetTextSettings(string txt, Color color, string size)
        {
            text = txt;
            textColor = color;
            textSize = Convert.ToInt32(size);
        }

        //--------------------------------------------------------------
        //МЕТОДЫ

        //open
        private void open_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            open.Title = "Open picture";
            open.Filter = "All pictures (*.*)|*.*";
            open.FilterIndex = 0;
            open.CheckFileExists = true;
            open.Multiselect = false;

            gr.Clear(Color.White);
            pictureBox1.Refresh();

            //открытие диалога
            if (open.ShowDialog() == DialogResult.OK)
            {
                bmp = (Bitmap)Bitmap.FromFile(open.FileName);
                AutoScroll = true;
                AutoScrollMinSize = new Size(Convert.ToInt32(bmp.Width) / 2, Convert.ToInt32(bmp.Height) / 2);
                gr = Graphics.FromImage(bmp);
                pictureBox1.Image = bmp;
                Invalidate();

                //----------------------------------------------------------------------------
                //2 вариант (как на лекции)
                //Bitmap bmp = new Bitmap(open.FileName);
                //pictureBox1.Image = bmp;
                //Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                //bmp2 = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat);
                //BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
                //IntPtr ptr = bmpData.Scan0;
                //int bytes = bmpData.Stride * bmp.Height;
                //ScrPic = new byte[bytes];
                //ResPic = new byte[bytes];
                //Marshal.Copy(ptr, ScrPic, 0, bytes);
                //Marshal.Copy(ptr, ResPic, 0, bytes);
                //bmp.UnlockBits(bmpData);
            }
        }

        //new
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //создаем новый слой
            Layer l = new Layer(800, 450, 1f);
            l.img = bmp;
            gr = Graphics.FromImage(l.img);
            bmpLayers.Add(l);
            listBox1.Items.Add($"layer {++layersCount}");
            listBox1.SetSelected(0, true);

            //устанавливаем белый цвет
            gr.Clear(Color.White);
            pictureBox1.Refresh();
            bmpLayers.Show(pictureBox2);

            //включаем picturebox'ы
            pictureBox1.Enabled = true;
            pictureBox2.Enabled = true;

            //включаем кнопки
            clearButton.Enabled = true;
            newLayerButton.Enabled = true;
            showLayerButton.Enabled = true;

            //отключаем кнопку удаления слоя
            deleteLayerButton.Enabled = false;
        }

        //close
        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //поочередно выбираем каждый слой и очищаем его
            for (int i = 0; i < bmpLayers.Count; i++)
            {
                Image img = bmpLayers.layers[i].img;
                gr = Graphics.FromImage(img);
                pictureBox1.Image = img;

                gr.Clear(Color.White);
                pictureBox1.Refresh();
                bmpLayers.Show(pictureBox2);
            }
            gr.Clear(Color.FromArgb(206, 216, 231));

            //очищаем список слоев
            bmpLayers.Clear();
            listBox1.Items.Clear();
            layersCount = 0;
           
            //выключаем picturebox'ы
            pictureBox1.Enabled = false;
            pictureBox2.Enabled = false;

            //выключаем кнопки
            clearButton.Enabled = false;
            newLayerButton.Enabled = false;
            deleteLayerButton.Enabled = false;
            showLayerButton.Enabled = false;
        }

        //добавить карандаш или ластик в XML
        public void addPenOrEraserToXML(string figure, Color color, float thickness, Point px, Point py)
        {
            XElem.Add(new XElement("TOOL", new XAttribute("figure", figure),
                                             new XAttribute("color", color), new XAttribute("thickness", thickness),
                                             new XAttribute("PX", px), new XAttribute("PY", py)));
        }

        //добавить фугуру в XML
        public void addFigureToXML(string figure, bool fillMode, Color color, float thickness, int x1, int y1, int x2, int y2, int x, int y)
        {
            XElem.Add(new XElement("FIGURE", new XAttribute("figure", figure), new XAttribute("fillMode", fillMode),
                                             new XAttribute("color", color), new XAttribute("thickness", thickness),
                                             new XAttribute("X1", x1), new XAttribute("Y1", y1),
                                             new XAttribute("X2", x2), new XAttribute("Y2", y2),
                                             new XAttribute("X", x), new XAttribute("Y", y)));
        }

        //сохранение XML - сохраняет данные нормально, но если удалить слой, то вылетает
        private void saveXML_Click(object sender, EventArgs e)
        {
            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "XML (*.xml)|*.xml";
            if (save.ShowDialog() == DialogResult.OK)
            {
                XDoc.Add(XElem);
                XDoc.Save(save.FileName);
            }
        }

        //сохранение JPEG, BMP, PNG
        private void saveJpeg_Click(object sender, EventArgs e)
        {
            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "JPEG (*.jpeg)|*.jpeg|BMP (*.bmp)|*.bmp|PNG (*.png)|*.png";
            if (save.ShowDialog() == DialogResult.OK)
            {
                if (pictureBox1.Image != null)
                    pictureBox1.Image.Save(save.FileName);
            }
        }

        //толщина линии
        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            pen.Width = trackBar1.Value;
        }

        //clear
        private void clear_Click(object sender, EventArgs e)
        {
            gr.Clear(Color.White);
            pictureBox1.Refresh();
            bmpLayers.Show(pictureBox2);
        }

        //rotate 90 left
        private void array90LeftButton_Click(object sender, EventArgs e)
        {
            pictureBox1.Image.RotateFlip(RotateFlipType.Rotate90FlipXY);
            pictureBox1.Refresh();
            bmpLayers.Show(pictureBox2);
        }

        //rotate 90 right
        private void array90RightButton_Click(object sender, EventArgs e)
        {
            pictureBox1.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);
            pictureBox1.Refresh();
            bmpLayers.Show(pictureBox2);
        }

        //rotate 180 
        private void array180Button_Click(object sender, EventArgs e)
        {
            pictureBox1.Image.RotateFlip(RotateFlipType.Rotate180FlipNone);
            pictureBox1.Refresh();
            bmpLayers.Show(pictureBox2);
        }

        //rotate 270
        private void array270Button_Click(object sender, EventArgs e)
        {
            pictureBox1.Image.RotateFlip(RotateFlipType.Rotate270FlipNone);
            pictureBox1.Refresh();
            bmpLayers.Show(pictureBox2);
        }

        //black and white
        private void blackAndWhiteButton_Click(object sender, EventArgs e)
        {
            //1 вариант

            //получить размеры изображения
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);

            //получить "сырые данные" изображения
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);

            //получить указатель на начальный байт изображения
            IntPtr ptr = bmpData.Scan0;

            //получить размер изображения в байтах
            int bytes = Math.Abs(bmpData.Stride) * bmp.Height;
            byte[] rgbValues = new byte[bytes];

            //скопировать RGB значения в этот массив
            Marshal.Copy(ptr, rgbValues, 0, bytes);

            //меняем цветовую схему
            for (int i = 0; i < rgbValues.Length; i += 3)
            {
                byte gray = (byte)(rgbValues[i] * 0.21 + rgbValues[i + 1] * 0.71 + rgbValues[i + 2] * 0.071);
                rgbValues[i] = rgbValues[i + 1] = rgbValues[i + 2] = gray;
            }

            //вернуть изменённые байта назад в класс Bitmap
            Marshal.Copy(rgbValues, 0, ptr, bytes);

            //разблокировать изображение
            bmp.UnlockBits(bmpData);
            pictureBox1.Refresh();
            Invalidate();

            //----------------------------------------------------------------------------
            //2 вариант
            //for (int i = 0; i < bmp.Width; i++)
            //{
            //    for (int j = 0; j < bmp.Height; j++)
            //    {
            //        Color c = bmp.GetPixel(i, j);

            //        //Apply conversion equation
            //        byte gray = (byte)(.21 * c.R + .71 * c.G + .071 * c.B);

            //        //Set the color of this pixel
            //        bmp.SetPixel(i, j, Color.FromArgb(gray, gray, gray));
            //    }
            //}
            //Invalidate();
        }

        /*изменение цвета как на лекции
        private void ChannelChange()
        {
            int len = ScrPic.Length;

            for (int counter = 0; counter < len; counter++)
            {
                if (counter % 3 == 0) // Blue
                {
                    double res = (ScrPic[counter]);
                    res *= 0.3;
                    if (res > 255) res = 255;
                    else if (res < 0) res = 0;
                    ResPic[counter] = (byte)res;
                }

                if (counter % 3 == 1) // Green
                {
                    double res = (ScrPic[counter]);
                    res *= 0.4;
                    if (res > 255) res = 255;
                    else if (res < 0) res = 0;
                    ResPic[counter] = (byte)res;
                }

                if (counter % 3 == 2) // Red
                {
                    double res = (ScrPic[counter]);
                    res *= 0.3;
                    if (res > 255) res = 255;
                    else if (res < 0) res = 0;
                    ResPic[counter] = (byte)res;
                }
            }
            Rectangle rect = new Rectangle(0, 0, bmp2.Width, bmp2.Height);
            BitmapData bmpData = bmp2.LockBits(rect, ImageLockMode.WriteOnly, bmp2.PixelFormat);
            IntPtr ptr = bmpData.Scan0;
            Marshal.Copy(ResPic, 0, ptr, ResPic.Length);
            bmp2.UnlockBits(bmpData);
            pictureBox1.Image = bmp2;
        }
        */

        //color
        private void colorButton_Click(object sender, EventArgs e)
        {
            ColorDialog color = new ColorDialog();
            color.FullOpen = true;
            color.AllowFullOpen = true;
            if (color.ShowDialog() == DialogResult.OK)
            {
                pen.Color = color.Color;
            }
        }

        //цвета
        private void whiteButton_Click(object sender, EventArgs e)
        {
            pen.Color = whiteButton.BackColor;
        }
        private void greyButton_Click(object sender, EventArgs e)
        {
            pen.Color = greyButton.BackColor;
        }
        private void blackButton_Click(object sender, EventArgs e)
        {
            pen.Color = blackButton.BackColor;
        }
        private void brownButton_Click(object sender, EventArgs e)
        {
            pen.Color = brownButton.BackColor;
        }
        private void redButton_Click(object sender, EventArgs e)
        {
            pen.Color = redButton.BackColor;
        }
        private void orangeButton_Click(object sender, EventArgs e)
        {
            pen.Color = orangeButton.BackColor;
        }
        private void yellowButton_Click(object sender, EventArgs e)
        {
            pen.Color = yellowButton.BackColor;
        }
        private void greenButton_Click(object sender, EventArgs e)
        {
            pen.Color = greenButton.BackColor;
        }
        private void aquaButton_Click(object sender, EventArgs e)
        {
            pen.Color = aquaButton.BackColor;
        }
        private void blueButton_Click(object sender, EventArgs e)
        {
            pen.Color = blueButton.BackColor;
        }
        private void pinkButton_Click(object sender, EventArgs e)
        {
            pen.Color = pinkButton.BackColor;
        }
        private void purpleButton_Click(object sender, EventArgs e)
        {
            pen.Color = purpleButton.BackColor;
        }

        //fill mode settings
        private void fillModeButton_CheckedChanged(object sender, EventArgs e)
        {
            if (fillModeButton.Checked == false)
                fillModeSettingsButton.Enabled = false;
            else
                fillModeSettingsButton.Enabled = true;
        }

        private void fillModeSettingsButton_Click(object sender, EventArgs e)
        {
            FillModeSettings fill = new FillModeSettings();
            fill.Owner = this;
            fill.PerformFillMode += GetFillModeSettings;
            DialogResult res = fill.ShowDialog();
        }

        private void GetFillModeSettings(string mode, Color firstColor, Color secondColor)
        {
            fillMode = mode;
            firstFillModeColor = firstColor;
            secondFillModeColor = secondColor;    
        }


        //--------------------------------------------------------------
        //LAYERS METHODS

        //show panel
        private void layerButton_Click(object sender, EventArgs e)
        {
            if (layerButton.Checked)
                panel3.Visible = true;
            else
                panel3.Visible = false;
        }

        //создать новый слой
        private void newLayerButton_Click(object sender, EventArgs e)
        {
            Layer l = new Layer(800, 450, 1f);
            Image img = l.img;
            gr = Graphics.FromImage(img);
            gr.Clear(Color.White);
            bmpLayers.Add(l);
            pictureBox1.Image = l.img;

            //добавляем слой в listbox
            listBox1.Items.Add($"layer {++layersCount}");
            listBox1.SetSelected(layersCount - 1, true);
            bmpLayers.Show(pictureBox2);

            //делаем активной кнопку удаления слоёв
            deleteLayerButton.Enabled = true;
        }

        //удалить выбранный слой
        private void deleteLayerButton_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex >= 0)
            {
                bmpLayers.RemoveAt(listBox1.SelectedIndex);
                layersCount--;
                listBox1.Items.RemoveAt(listBox1.SelectedIndex);
                listBox1.SetSelected(layersCount - 1, true);
                bmpLayers.Show(pictureBox2);
            }

            //если остался только один слой, то делаем кнопку удаления слоёв неактивной
            if (listBox1.Items.Count == 1)
                deleteLayerButton.Enabled = false;
        }

        //показать все слои на основной форме:
        //данный метод просто показывает все слои на основной форме, и позволяет редактировать полученную картинку
        //но я не делал для неё отдельный слой
        //если нажать на имя слоя в listbox'е, то картинка исчезнет
        private void showLayerButton_Click(object sender, EventArgs e)
        {
            bmpLayers.Show(pictureBox1);
            Image new_img = pictureBox1.Image;
            gr = Graphics.FromImage(new_img);
            pictureBox1.Image = new_img;
        }

        //отображение выбранного слоя на основной форме
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
            {
                Image img = bmpLayers.layers[listBox1.SelectedIndex].img;
                gr = Graphics.FromImage(img);
                pictureBox1.Image = img;
            }
        }

        //прозрачность
        private void trackBar2_ValueChanged(object sender, EventArgs e)
        {
            bmpLayers.layers[listBox1.SelectedIndex].Transition = (float)trackBar2.Value * 0.1f;

            Image img = bmpLayers.layers[listBox1.SelectedIndex].img;
            gr = Graphics.FromImage(img);
            pictureBox1.Image = img;
            pictureBox1.Refresh();

            bmpLayers.Show(pictureBox2);
            pictureBox2.Refresh();
        }
    }
}
