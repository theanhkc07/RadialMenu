using System;
using System.Collections.Generic;
using Eto.Drawing;
using Eto.Forms;

namespace RadialMenuPlugin.Data
{
    /// <summary>
    /// Lớp chuyển đổi giá trị "phím" (int) của Rhino sang giá trị ETO Keys
    /// </summary>
    public static class RhinoKeyToEto
    {
        private static readonly Dictionary<int, Keys> match = new Dictionary<int, Keys>
        {
            {55,Keys.Application},
            {58,Keys.Alt},
            {59,Keys.Control},
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rhinoKey"></param>
        /// <returns></returns>
        public static Keys toEtoKey(int rhinoKey)
        {
            if (match.ContainsKey(rhinoKey))
            {
                return match[rhinoKey];
            }
            return Keys.None;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class SettingsDomain
    {
        public string Name { get; }
        private SettingsDomain(string key) { Name = key; }
        public static SettingsDomain RadialButtonsConfig => new SettingsDomain("Buttons");
        public static SettingsDomain Theme => new SettingsDomain("Theme");
        public static SettingsDomain GeneralSettings => new SettingsDomain("General");

        public override string ToString()
        {
            return Name;
        }
    }
    public enum DragSourceType
    {
        radialMenuItem = 0,
        rhinoItem = 1,
        unknown = 3
    }

    /// <summary>
    /// Lớp cấp menu
    /// </summary>
    public class RadialMenuLevel
    {
        /// <summary>
        /// Số thứ tự cấp
        /// </summary>
        public int Level;

        /// <summary>
        /// Bán kính trong của cấp
        /// </summary>
        public int InnerRadius;

        /// <summary>
        /// Độ dày sector để vẽ cấp này
        /// </summary>
        public int Thickness;

        /// <summary>
        /// Góc bắt đầu cho cấp này
        /// </summary>
        public float StartAngle;

        /// <summary>
        /// Số lượng sector cho cấp này
        /// </summary>
        public int SectorsNumber;


        /// <summary>
        /// Hàm khởi tạo
        /// </summary>
        /// <param name="level"></param>
        /// <param name="innerRadius"></param>
        /// <param name="thickness"></param>
        /// <param name="startAngle"></param>
        /// <param name="sectorsNumber"></param>
        public RadialMenuLevel(int level, int innerRadius, int thickness = 30, float startAngle = 0, int sectorsNumber = 8)
        {
            Level = level; InnerRadius = innerRadius;
            Thickness = thickness; StartAngle = startAngle;
            SectorsNumber = sectorsNumber;
        }
    }


    public class ButtonColor
    {
        public Color Pen;
        public Color Fill;

        public ButtonColor(Color pen, Color fill) { Pen = pen; Fill = fill; }
    }
    public class RadialButtonStateColors
    {
        public ButtonColor Normal = new ButtonColor(Colors.Beige, Colors.DarkGray);
        public ButtonColor Hover = new ButtonColor(Colors.Beige, Colors.LightGrey);
        public ButtonColor Selected = new ButtonColor(Colors.Beige, Colors.WhiteSmoke);
        public ButtonColor Disabled = new ButtonColor(Colors.LightGrey, Colors.SlateGray);
        public ButtonColor Drag = new ButtonColor(Colors.Beige, Colors.DarkKhaki);
    }
    public struct ButtonStateImages
    {
        public Image NormalStateImage;
        public Image OverStateImage;
        public Image DisabledStateImage;
        public Image SelectedStateImage;
        public Image dragStateImage;
        // Hình ảnh mặt nạ để phát hiện xem một điểm có nằm trên hình ảnh sector hay không
        public Bitmap SectorMask;
    }
    public class SectorData
    {
        /// <summary>
        /// Kích thước của hình ảnh sector
        /// </summary>
        public Size Size;

        /// <summary>
        /// Giới hạn cung sector trong tọa độ thực, tức là tọa độ Parent và/hoặc trong vòng tròn bao quanh thực (dựa trên Bán kính trong)
        /// Hữu ích để đặt vị trí icon trong control bao quanh (ví dụ: form)
        /// </summary>
        public RectangleF Bounds;

        // Tâm của cung trong tọa độ "thực"
        public Point ArcCenter;
        public float StartAngle;
        public float SweepAngle;
        public int InnerRadius = 50;
        public int Thickness = 30;

        /// <summary>
        /// Hình ảnh trạng thái
        /// </summary>
        public ButtonStateImages Images;

        #region Arc radius and thickness
        #endregion

        /// <summary>
        /// Tâm của sector trong tọa độ thế giới (hữu ích để đặt icon trong nút sector) 
        /// </summary>
        public PointF SectorCenter()
        {
            var centerRadius = InnerRadius + (Thickness / 2);
            var bisectorAngle = StartAngle + (SweepAngle / 2);
            float X = (float)(ArcCenter.X + centerRadius * Math.Cos(bisectorAngle * (Math.PI / 180)));
            float Y = (float)(ArcCenter.Y + centerRadius * Math.Sin(bisectorAngle * (Math.PI / 180)));
            return new PointF(X, Y);
        }
        /// <summary>
        /// Lấy một điểm trong sector
        /// </summary>
        /// <param name="angleFromStart">Angle from start angle of sector</param>
        /// <param name="radiusFromInner">Radius from inner radius</param>
        /// <returns></returns>
        public PointF GetPoint(float angleFromStart, int radiusFromInner)
        {
            var atAngle = StartAngle + angleFromStart;
            var atRadius = InnerRadius + radiusFromInner;
            float X = (float)(ArcCenter.X + atRadius * Math.Cos(atAngle * (Math.PI / 180)));
            float Y = (float)(ArcCenter.Y + atRadius * Math.Sin(atAngle * (Math.PI / 180)));
            return new PointF(X, Y);
        }
        public float EndAngle { get { return StartAngle + SweepAngle; } }

        public SectorData() { }
        public SectorData(RadialMenuLevel level)
        {
            InnerRadius = level.InnerRadius;
            Thickness = level.Thickness;
        }

        /// <summary>
        /// Chuyển đổi vị trí cục bộ (tức là tọa độ control) sang tọa độ cung thế giới (vì control chỉ hiển thị cung chứ không phải toàn bộ vòng tròn)
        /// </summary>
        /// <param name="localLocation"></param>
        /// <returns></returns>
        public PointF ConvertLocalToWorld(PointF localLocation)
        {
            // convert mouse location to real circle size (i.e. Parent form size)
            var anchorX = (float)Math.Floor(Bounds.TopLeft.X);
            var anchorY = (float)Math.Floor(Bounds.TopLeft.Y);
            var localX = anchorX + localLocation.X;
            var localY = anchorY + localLocation.Y;
            return new PointF(localX, localY);
        }
        /// <summary>
        /// convert real world coordinates to local (i.e. control local coordinates) coordinates
        /// </summary>
        /// <param name="worldLocaltion"></param>
        /// <returns></returns>
        public PointF ConvertWorldToLocal(PointF worldLocaltion)
        {
            var anchorX = (float)Math.Floor(Bounds.TopLeft.X);
            var anchorY = (float)Math.Floor(Bounds.TopLeft.Y);
            var worldX = worldLocaltion.X - anchorX;
            var worldY = worldLocaltion.Y - anchorY;
            return new PointF(worldX, worldY);
        }
        /// <summary>
        /// <para>Kiểm tra xem một Điểm có nằm trong hình dạng cung không</para>
        /// <para>QUAN TRỌNG: Điểm phải nằm trong tọa độ cục bộ của control.</para>
        /// </summary>
        /// <param name="location">Local control coordinates</param>
        /// <returns></returns>
        public bool IsPointInShape(PointF location)
        {
            // Sử dụng kiểm tra va chạm toán học (Atan2) thay vì mặt nạ bitmap để có độ chính xác tốt hơn và tránh các vấn đề về DPI/AntiAlias
            // 1. Chuyển đổi vị trí cục bộ (tương đối với Bounds/Control) sang vị trí Thế giới (tương đối với ArcCenter)
            var worldLocation = ConvertLocalToWorld(location);

            // 2. Tính khoảng cách từ tâm (Kiểm tra Bán kính)
            var dx = worldLocation.X - ArcCenter.X;
            var dy = worldLocation.Y - ArcCenter.Y;
            var dist = Math.Sqrt(dx * dx + dy * dy);

            var outerRadius = InnerRadius + Thickness;
            // Thêm epsilon cho độ chính xác số thực
            if (dist < InnerRadius - 0.1 || dist > outerRadius + 0.1) return false;

            // 3. Tính góc
            var angleRad = Math.Atan2(dy, dx); // -PI to PI
            var angleDeg = angleRad * (180.0 / Math.PI);
            
            // Chuẩn hóa góc về [0, 360]
            if (angleDeg < 0) angleDeg += 360.0;

            // Chuẩn hóa StartAngle và EndAngle để xử lý bao bọc (ví dụ: Start 350, Sweep 20 -> End 10)
            // Dễ dàng hơn khi xoay góc để StartAngle trở thành 0
            var relativeAngle = angleDeg - StartAngle;
            while (relativeAngle < -0.001) relativeAngle += 360.0;
            while (relativeAngle >= 360.0 - 0.001) relativeAngle -= 360.0;

            // Kiểm tra xem góc có nằm trong vùng quét không
            return relativeAngle <= SweepAngle + 0.001;
        }
    }
}
