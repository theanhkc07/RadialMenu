using System;
using Eto.Drawing;

namespace RadialMenuPlugin.Utilities
{

    public class ArcSectorDrawer
    {
        Data.RadialButtonStateColors theme = new Data.RadialButtonStateColors();

        public ArcSectorDrawer(Data.RadialButtonStateColors theme = null)
        {
            if (theme != null) { this.theme = theme; }
        }

        public GraphicsPath CreateSectorPath(int x, int y, Data.RadialMenuLevel level, float startAngle, float sweepAngle)
        {
            var gp = new GraphicsPath();
            var center = new Point(x, y);
            var outerRadius = level.InnerRadius + level.Thickness;
            var innerRadius = level.InnerRadius;
            var outerRectangle = new Rectangle(center.X - outerRadius, center.Y - outerRadius, outerRadius * 2, outerRadius * 2);
            var innerRectangle = new Rectangle(center.X - innerRadius, center.Y - innerRadius, innerRadius * 2, innerRadius * 2);

            gp.AddArc(outerRectangle, startAngle, sweepAngle);
            gp.AddArc(innerRectangle, startAngle + sweepAngle, -sweepAngle);
            gp.CloseFigure();
            return gp;
        }

        public Data.SectorData CreateSectorImages(int x, int y, Data.RadialMenuLevel level, float startAngle, float sweepAngle)
        {
            var gp1 = CreateSectorPath(x, y, level, startAngle, sweepAngle);
            var gp2 = new GraphicsPath();
            var center = new Point(x, y);

            // Dữ liệu cho đường dẫn sector (phần được hiển thị)
            var outerRadius = level.InnerRadius + level.Thickness;
            var innerRadius = level.InnerRadius;

            var insetGap = 4; // in pixels
            var insetInnerRadius = innerRadius + insetGap;
            var insetOuterRadius = outerRadius - insetGap;
            var insetInnerRectangle = new Rectangle(center.X - insetInnerRadius, center.Y - insetInnerRadius, insetInnerRadius * 2, insetInnerRadius * 2);
            var insetOuterRectangle = new Rectangle(center.X - insetOuterRadius, center.Y - insetOuterRadius, insetOuterRadius * 2, insetOuterRadius * 2);

            float innerSin = (float)insetGap / insetInnerRadius;
            var innerAsin = Math.Asin(innerSin);
            var insetInnerAngle = innerAsin * (180 / Math.PI);
            var insetInnerStartAngle = startAngle + insetInnerAngle;
            var insetInnerSweepAngle = sweepAngle - 2 * insetInnerAngle;

            float outerSin = (float)insetGap / insetOuterRadius;
            var outerAsin = Math.Asin(outerSin);
            var insetOuterAngle = outerAsin * (180 / Math.PI);
            var insetOuterStartAngle = startAngle + insetOuterAngle;
            var insetOuterSweepAngle = sweepAngle - 2 * insetOuterAngle;

            gp2.AddArc(insetOuterRectangle.Left, insetOuterRectangle.Top, insetOuterRectangle.Width, insetOuterRectangle.Height, (float)insetOuterStartAngle, (float)insetOuterSweepAngle);
            gp2.AddArc(insetInnerRectangle.Left, insetInnerRectangle.Top, insetInnerRectangle.Width, insetInnerRectangle.Height, (float)(insetInnerStartAngle + insetInnerSweepAngle), (float)-insetInnerSweepAngle);
            gp2.CloseFigure();

            var paths = new GraphicsPath[] { gp1, gp2 };
            return BuildImages(paths, x, y, level, startAngle, sweepAngle);
        }

        protected Data.SectorData BuildImages(GraphicsPath[] graphicPaths, int x, int y, Data.RadialMenuLevel level, float startAngle, float sweepAngle)
        {
            var sectorPath = graphicPaths[0];
            var maskPath = graphicPaths[1];

            var pen = new Pen(theme.Normal.Pen, 1);
            var fillColor = theme.Normal.Fill;

            var pathSize = new Size((int)sectorPath.Bounds.Size.Width + 3, (int)sectorPath.Bounds.Size.Height + 3);

            // Tính toán offset số nguyên cho phép tịnh tiến
            // Điều này PHẢI khớp với logic định vị trong RadialMenuControl để tránh bị lệch
            var offsetX = (int)Math.Floor(sectorPath.Bounds.Left);
            var offsetY = (int)Math.Floor(sectorPath.Bounds.Top);

            // Tạo hình ảnh nút cho trạng thái bình thường
            var normalStateImage = new Bitmap(pathSize, PixelFormat.Format32bppRgba);
            var _graphics = new Graphics(normalStateImage);
            _graphics.TranslateTransform(-offsetX, -offsetY);
            _graphics.FillPath(fillColor, sectorPath);
            _graphics.DrawPath(pen, sectorPath);
            _graphics.Dispose();
            pen.Dispose();

            // Tạo hình ảnh nút cho trạng thái hover (di chuột qua)
            pen = new Pen(theme.Hover.Pen, 1);
            fillColor = theme.Hover.Fill;
            var overStateImage = new Bitmap(pathSize, PixelFormat.Format32bppRgba);
            _graphics = new Graphics(overStateImage);
            _graphics.TranslateTransform(-offsetX, -offsetY);
            _graphics.FillPath(fillColor, sectorPath);
            _graphics.DrawPath(pen, sectorPath);
            _graphics.Dispose();
            pen.Dispose();


            // Create button image for disable state
            pen = new Pen(theme.Disabled.Pen, 1);
            fillColor = theme.Disabled.Fill;
            var disabledImage = new Bitmap(pathSize, PixelFormat.Format32bppRgba);
            _graphics = new Graphics(disabledImage);
            _graphics.TranslateTransform(-offsetX, -offsetY);
            _graphics.FillPath(fillColor, sectorPath);
            _graphics.DrawPath(pen, sectorPath);
            _graphics.Dispose();
            pen.Dispose();

            // Create button image for selected state
            pen = new Pen(theme.Selected.Pen, 1);
            fillColor = theme.Selected.Fill;
            var selectedImage = new Bitmap(pathSize, PixelFormat.Format32bppRgba);
            _graphics = new Graphics(selectedImage);
            _graphics.TranslateTransform(-offsetX, -offsetY);
            _graphics.FillPath(fillColor, sectorPath);
            _graphics.DrawPath(pen, sectorPath);
            _graphics.Dispose();
            pen.Dispose();

            // Tạo hình ảnh mặt nạ
            pen = new Pen(Colors.Blue, 1);
            var maskImage = new Bitmap(pathSize, PixelFormat.Format32bppRgba);
            _graphics = new Graphics(maskImage);
            _graphics.TranslateTransform(-offsetX, -offsetY);
            _graphics.FillPath(Colors.Blue, maskPath); // không vẽ đường dẫn vì trong UI có thể có 2 nút được "hover", nên chỉ phần tô bên trong là một phần của hình ảnh mặt nạ
            _graphics.Dispose();
            pen.Dispose();

            // LƯU Ý: không có hình ảnh "drag" cụ thể. Nó giống với trạng thái "bình thường"
            return new Data.SectorData(level)
            {
                // Dữ liệu cung tròn
                ArcCenter = new Point(x, y),
                Bounds = sectorPath.Bounds,
                Size = pathSize,
                StartAngle = startAngle,
                SweepAngle = sweepAngle,
                // Hình ảnh
                Images = new Data.ButtonStateImages()
                {
                    NormalStateImage = normalStateImage,
                    OverStateImage = overStateImage,
                    DisabledStateImage = disabledImage,
                    dragStateImage = normalStateImage,
                    SelectedStateImage = selectedImage,
                    SectorMask = maskImage,
                },
            };
        }
    }
}
