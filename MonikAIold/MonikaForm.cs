using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MonikAI
{
    public partial class MonikaForm : Form
    {
        public MonikaForm()
        {
            this.InitializeComponent();

            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.BackColor = Color.White;
            this.TransparencyKey = Color.White;
        }

        //The SendMessage function sends a message to a window or windows.
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        //ReleaseCapture releases a mouse capture
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern bool ReleaseCapture();

        private void MonikaForm_Load(object sender, EventArgs e)
        {
            UnSemi((Bitmap) this.pictureBox1.Image);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            //base.OnPaintBackground(e);
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            // drag the form without the caption bar
            // present on left mouse button
            if (e.Button == MouseButtons.Left)
            {
                MonikaForm.ReleaseCapture();
                MonikaForm.SendMessage(this.Handle, 0xa1, 0x2, 0);
            }
        }

        public static void UnSemi(Bitmap bmp)
        {
            Size s = bmp.Size;
            PixelFormat fmt = bmp.PixelFormat;
            Rectangle rect = new Rectangle(Point.Empty, s);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, fmt);
            int size1 = bmpData.Stride * bmpData.Height;
            byte[] data = new byte[size1];
            System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, data, 0, size1);
            for (int y = 0; y < s.Height; y++)
            {
                for (int x = 0; x < s.Width; x++)
                {
                    int index = y * bmpData.Stride + x * 4;
                    // alpha,  threshold = 255
                    data[index + 3] = (data[index + 3] < 255) ? (byte)0 : (byte)255;
                }
            }
            System.Runtime.InteropServices.Marshal.Copy(data, 0, bmpData.Scan0, data.Length);
            bmp.UnlockBits(bmpData);
        }
    }
}