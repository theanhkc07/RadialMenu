using Eto.Drawing;
using Eto.Forms;

namespace RadialMenuPlugin.Controls.Buttons
{
    public class ImageButton : Drawable
    {
#nullable enable
        private Image? _CurrentImage;
#nullable disable

        public ImageButton() : base() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="image"></param>
        /// <param name="size"></param>
        public ImageButton(Image image, Size size) : this()
        {
            _CurrentImage = image;
            Size = size;
        }
        public void SetImage(Image image, Size? size = null)
        {
            if (size != null)
            {
                Size = (Size)size;
            }
            _CurrentImage = image;
            Invalidate();
        }

        private float _Alpha = 1.0f;
        public float Alpha
        {
            get => _Alpha;
            set
            {
                _Alpha = value;
                Visible = _Alpha > 0.01f;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // e.Graphics.DrawRectangle(Colors.Black,0,0,Parent.Size.Width-1,Parent.Size.Height-1);
            if (_CurrentImage != null) // Could be null, so draw nothing
            {
                e.Graphics.DrawImage(_CurrentImage, 0, 0);
            }
        }
    }
}