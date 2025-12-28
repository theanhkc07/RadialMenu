using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using NLog;
using RadialMenuPlugin.Controls.Buttons.MenuButton;
using RadialMenuPlugin.Data;
using RadialMenuPlugin.Utilities;
using RadialMenuPlugin.Utilities.Events;

namespace RadialMenuPlugin.Controls
{
    #region Lớp xử lý sự kiện và tham số
    using DragDropEventHandler = AppEventHandler<RadialMenuControl, ButtonDragDropEventArgs>; // Delegate cho sự kiện Kéo/Thả
    using MouseEventHandler = AppEventHandler<RadialMenuControl, ButtonMouseEventArgs>;
    /// <summary>
    /// Tham số sự kiện kéo thả nút
    /// </summary>
    public class ButtonDragDropEventArgs
    {
        /// <summary>
        /// Đối tượng nguồn (có thể là MenuButton hoặc RhinoToolbarItem)
        /// </summary>
        public DragEventArgs DragEventSourceArgs;
        /// <summary>
        /// Model dữ liệu của đích thả
        /// </summary>
        public Model TargetModel;
        public ButtonDragDropEventArgs(DragEventArgs dragevent, Model target)
        {
            DragEventSourceArgs = dragevent;
            TargetModel = target;
        }
    }
    /// <summary>
    /// Tham số sự kiện lựa chọn
    /// </summary>
    public struct SelectionEventArgs
    {
        public string ButtonID;
        public Model Model;
        public ButtonProperties Properties;
        public SelectionEventArgs(string buttonID, Model model, ButtonProperties properties)
        {
            ButtonID = buttonID;
            Model = model;
            Properties = properties;
        }
    }
    /// <summary>
    /// Tham số sự kiện chuột trên nút
    /// </summary>
    public struct ButtonMouseEventArgs
    {
        /// <summary>
        /// Sự kiện chuột từ người gửi ban đầu (nút)
        /// </summary>
        public MouseEventArgs MouseEventArgs;
        /// <summary>
        /// Vị trí trên màn hình của sự kiện click
        /// <para>
        /// LƯU Ý: Khi sự kiện chuột được kích hoạt, vị trí nằm trong tọa độ Control. Nhưng rất hữu ích khi có vị trí màn hình của sự kiện click, ví dụ: Để hiển thị thứ gì đó không liên quan đến control
        /// </para>
        /// </summary>
        public Point ScreenLocation;
        /// <summary>
        /// Model liên kết với nút
        /// </summary>
        public Model Model;
        public ButtonMouseEventArgs(MouseEventArgs mouseEventArgs, Point screenlocation, Model model)
        {
            MouseEventArgs = mouseEventArgs;
            Model = model;
            ScreenLocation = screenlocation;
        }
    }
    #endregion
    /// <summary>
    /// Đối tượng PixelLayout chịu trách nhiệm hiển thị nút sector
    /// <para>
    /// CẢNH BÁO: KHÔNG DÙNG thuộc tính "VISIBLE" -> Nó ngăn control layout "NSTrackingArea" cập nhật khi kích thước control thay đổi
    /// Thay vào đó, dùng "AlphaValue" để hiện/ẩn control này
    /// </para>
    /// </summary>
    public class RadialMenuControl : PixelLayout
    {
        public static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #region Khai báo sự kiện
        public event MouseEventHandler MouseEnterButton;
        public event MouseEventHandler MouseMoveButton;
        public event MouseEventHandler MouseLeaveButton;
        public event MouseEventHandler MouseClickButton;
        public event DragDropEventHandler DragDropEnterButton;
        public event DragDropEventHandler DragDropOverButton;
        public event DragDropEventHandler DragDropLeaveButton;
        public event DragDropEventHandler DragDropButton;
        public event DragDropEventHandler RemoveButton;
        public event MouseEventHandler ButtonContextMenu;
        #endregion

        #region Thuộc tính công khai
        public RadialMenuLevel Level;
        public bool IsVisible
        {
            get => Visible;
        }
        public string SelectedButtonID
        {
            get => _SelectedButtonID;
            set
            {
                MenuButton btn = null;
                var raiseEvent = SelectedButtonID != value;
                _SelectedButtonID = value;

                try
                {
                    btn = _Buttons.First(keyvaluepair => keyvaluepair.Key.ID == _SelectedButtonID).Key;
                }
                catch { }
                finally
                {
                    if (value != "")
                    {
                        if (btn != null)
                        {
                            btn.States.IsSelected = true;
                            _ClearSelection(_SelectedButtonID); // Xóa lựa chọn của các nút khác
                        }
                    }
                    else // nếu không có lựa chọn, xóa trạng thái lựa chọn của tất cả nút
                    {
                        _ClearSelection();
                    }

                    // Kích hoạt "selectionChanged" nếu lựa chọn thay đổi
                    if (raiseEvent)
                    {
                        // TODO: Triển khai sự kiện thay đổi lựa chọn nếu cần
                    }
                }
            }
        }
        #endregion

        #region Thuộc tính Bảo vệ/Riêng tư
        /// <summary>
        /// Liên kết nút với model
        /// </summary>
        protected Dictionary<MenuButton, Model> _Buttons = new Dictionary<MenuButton, Model>();
        /// <summary>
        /// Liên kết nút với hình ảnh dữ liệu sector
        /// </summary>
        protected Dictionary<MenuButton, SectorData> _ButtonsImages = new Dictionary<MenuButton, SectorData>();
        /// <summary>
        /// ID nút được chọn hiện tại
        /// </summary>
        protected string _SelectedButtonID;
        /// <summary>
        /// ID nút chuột đang trỏ vào
        /// <para>
        /// HACK: Sự kiện chuột enter/leave có thể không theo thứ tự (ví dụ "leave" nút #1 kích hoạt sau "enter" nút #2). Thuộc tính này cho phép chúng ta đảm bảo sự kiện leave khớp với nút "mouse over" hiện tại
        /// </para>
        /// </summary>
        protected string _HoverButtonID;
        #endregion

        #region Hoạt hình (Animations)
        /// <summary>
        /// Hiệu ứng hiện và ẩn
        /// </summary>
        /// <param name="show"></param>
        protected void _AnimateShowHideEffect(bool show = true)
        {
            Visible = show;
        }
        #endregion

        #region Phương thức công khai
        /// <summary>
        /// Hàm khởi tạo
        /// </summary>
        /// <param name="level"></param>
        public RadialMenuControl(RadialMenuLevel level) : base()
        {
            Level = level;
            Size = new Size((level.InnerRadius + level.Thickness) * 2, (level.InnerRadius + level.Thickness) * 2);
            var sectors = _BuildSectors();

            // Khởi tạo các nút sectorArc rỗng
            for (var i = 0; i < level.SectorsNumber; i++)
            {
                var sector = sectors[i];
                // Khởi tạo nút
                var btn = new MenuButton();
                btn.ID = i.ToString();
                btn.SectorData = sector; // Hình ảnh nút

                btn.OnButtonMouseEnter += _OnMouseEnter;
                btn.OnButtonMouseMove += _OnMouseMove;
                btn.OnButtonMouseLeave += _OnMouseLeave;
                btn.OnButtonClickEvent += _OnMouseClick;

                btn.OnButtonDragEnter += _OnDragDropEnter;
                btn.OnButtonDragOver += _OnDragOver;
                btn.OnButtonDragLeave += _onDragLeave;
                btn.OnButtonDragDrop += _OnDragDrop;
                btn.OnButtonDragDropStart += _OnDoDragStart;
                btn.OnButtonDragDropEnd += _OnDoDragEnd;

                btn.OnButtonContextMenu += _OnButtonContextMenu;

                _Buttons.Add(btn, null); // Cập nhật từ điển nút
                _ButtonsImages.Add(btn, sector); // Lưu hình ảnh cho nút này
                Add(btn, (int)Math.Floor(sector.Bounds.X), (int)Math.Floor(sector.Bounds.Y)); // Thêm nút vào layout
            }
            Visible = true;
        }
        /// <summary>
        /// Chuyển đổi chế độ chỉnh sửa
        /// </summary>
        /// <param name="editMode"></param>
        public void SwitchEditMode(bool editMode)
        {
            _ClearSelection();
            _EnableButtons();
            foreach (var button in _Buttons.Keys)
            {
                button.States.IsSelected = false;
                button.States.IsEditMode = editMode;
            }
        }
        /// <summary>
        /// Lấy các model hiện đang liên kết với các nút control
        /// </summary>
        /// <param name="buttonID"></param>
        /// <returns></returns>
        public List<Model> GetModels()
        {
            return _Buttons.Values.ToList();
        }
        public ButtonProperties GetButtonProperties(string buttonID)
        {
            try
            {
                return _Buttons.First(entry => entry.Key.ID == buttonID).Value.Data.Properties;
            }
            catch (Exception e)
            {
                Logger.Error(e, $"SectorArcRadialControl get button {buttonID} properties error");
                return null;
            }
        }
        public void SetButtonProperties(string buttonID, Data.ButtonProperties properties)
        {
            try
            {
                _Buttons.First(entry => entry.Key.ID == buttonID).Value.Data.Properties = properties;
            }
            catch (Exception e)
            {
                Logger.Error(e, $"SectorArcRadialControl set button {buttonID} properties error");
            }
        }
        public void Show(bool show = true)
        {
            if (!show) // Khi ẩn, đặt lại trạng thái nút (đã chọn và đã bật)
            {
                _ClearSelection(); // khi control hiện, đặt lại tất cả nút đã chọn thành false
                _EnableButtons(); // Khi control ẩn, đặt lại lựa chọn và bật các nút con
            }
            _SetChildrenVisibleState(show); // Cập nhật trạng thái hiển thị nút sector con để ngăn sự kiện "onMouseOver" không mong muốn
            _AnimateShowHideEffect(show); // Hiệu ứng hoạt hình
        }

        /// <summary>
        /// Kích hoạt hoặc vô hiệu hóa tất cả các nút ngoại trừ nút được chọn
        /// Nếu không có nút nào được chọn, bật tất cả các nút
        /// </summary>
        /// <param name="buttonID"></param>
        public void DisableButtonsExceptSelection()
        {
            if (SelectedButtonID == "") // Nếu không có lựa chọn nào kích hoạt, bật tất cả các nút
            {
                _EnableButtons();
            }
            else // Nếu một nút được chọn, vô hiệu hóa tất cả các nút khác
            {
                _DisableButtonsExcept(SelectedButtonID);
            }
        }

        /// <summary>
        /// Control có chứa nút được cung cấp không
        /// </summary>
        /// <param name="btn"></param>
        /// <returns></returns>
        public bool HasButton(MenuButton btn)
        {
            return _Buttons.ContainsKey(btn);
        }

        /// <summary>
        /// Hiển thị danh sách các nút cho ID được chỉ định (tức là ID của nút cấp trước đó kích hoạt mở menu)
        /// </summary>
        /// <param name="forButtonID"></param>
        public void SetMenuForButtonID(Model parent = null)
        {
            SelectedButtonID = ""; // Vì chúng ta xây dựng layout mới, bỏ chọn bất kỳ nút nào

            // Duyệt qua từng dữ liệu sector để cập nhật nút
            foreach (MenuButton button in _Buttons.Keys)
            {
                button.Unbind(); // Hủy liên kết bất kỳ binding nào
                var model = ModelController.Instance.Find(button.ID, parent, true);
                _Buttons[button] = model; // cập nhật model
                button.ButtonModelBinding.Bind(_Buttons, bntCollection => bntCollection[button].Data); // Bind model nút
                button.ID = model.Data.ButtonID; // Cập nhật ID nút

            }
        }
        #endregion

        #region Phương thức Bảo vệ/Riêng tư
        /// <summary>
        /// Vô hiệu hóa tất cả các nút ngoại trừ nút được chỉ định
        /// <para>Sử dụng phương thức này khi một nút mở thư mục</para>
        /// </summary>
        /// <param name="buttonID"></param>
        protected void _DisableButtonsExcept(string buttonID)
        {
            foreach (var btn in _Buttons.Keys)
            {
                if (btn.ID == buttonID) btn.Enabled = true; else btn.Enabled = false;
            }
        }

        /// <summary>
        /// Bật tất cả các nút control
        /// </summary>
        protected void _EnableButtons()
        {
            foreach (var btn in _Buttons.Keys)
            {
                btn.Enabled = true;
            }
        }

        /// <summary>
        /// Đặt lại tất cả các nút đã chọn
        /// </summary>
        protected void _ClearSelection(string exceptButtonID = "")
        {
            foreach (var btn in _Buttons.Keys)
            {
                if (exceptButtonID != "") // Kiểm tra xem có nút nào không nên xóa lựa chọn
                {
                    if (_Buttons[btn].Data.ButtonID != exceptButtonID) btn.States.IsSelected = false;
                }
                else
                {
                    btn.States.IsSelected = false;
                }

            }
        }
        /// <summary>
        /// Xây dựng dữ liệu sector mới
        /// </summary>
        /// <param name="sectorsNumber"></param>
        /// <param name="startAngle"></param>
        /// <returns></returns>
        protected List<SectorData> _BuildSectors()
        {
            float angleStart;
            List<SectorData> sectors = new List<SectorData>();

            var sectorDrawer = new ArcSectorDrawer();
            var sweepAngle = 360f / Level.SectorsNumber;

            for (int i = 0; i < Level.SectorsNumber; i++)
            {
                // tính góc. Nếu > 360, đặt lại về 0
                angleStart = Level.StartAngle + (i * sweepAngle);
                if (angleStart > 360) Level.StartAngle -= 360;

                // Vẽ một sector

                var sectorData = sectorDrawer.CreateSectorImages(Size.Width / 2, Size.Height / 2, Level, angleStart, sweepAngle);

                // thêm thông tin sector vào danh sách
                sectors.Add(sectorData);
            }
            return sectors;
        }

        /// <summary>
        /// Ẩn/Hiện nút
        /// </summary>
        /// <param name="show"></param>
        protected void _SetChildrenVisibleState(bool show)
        {
            foreach (var ctrl in _Buttons.Keys)
            {
                ctrl.States.IsVisible = show;
            }
        }
        #endregion

        #region Xử lý sự kiện nút
        /// <summary>
        /// Xử lý sự kiện chuột vào nút
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void _OnMouseEnter(MenuButton sender, MouseEventArgs e)
        {
            _HoverButtonID = sender.ID;
            _RaiseEvent(MouseEnterButton, new ButtonMouseEventArgs(e, new Point(sender.PointToScreen(e.Location)), _Buttons[sender]));
        }
        /// <summary>
        /// Xử lý sự kiện chuột di chuyển trên nút
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void _OnMouseMove(MenuButton sender, MouseEventArgs e)
        {
            _RaiseEvent(MouseMoveButton, new ButtonMouseEventArgs(e, new Point(sender.PointToScreen(e.Location)), _Buttons[sender]));
        }
        /// <summary>
        /// Xử lý sự kiện chuột rời khỏi nút
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void _OnMouseLeave(MenuButton sender, MouseEventArgs e)
        {
            /// HACK: Ngăn kích hoạt sự kiện "leave" của nút khác với nút đang "hover"
            /// Nếu di chuyển chuột nhanh từ nút này sang nút khác, sự kiện "leave" kích hoạt SAU sự kiện "enter"
            /// Điều này gây lỗi ví dụ như cập nhật và hiển thị tooltip <seealso cref="RadialMenuForm"/>
            if (sender.ID != _HoverButtonID)
            {
                Logger.Debug($"Mouse leave not triggered");
            }
            else
            {
                _HoverButtonID = null;
                _RaiseEvent(MouseLeaveButton, new ButtonMouseEventArgs(e, new Point(sender.PointToScreen(e.Location)), _Buttons[sender]));
            }
        }
        /// <summary>
        /// Xử lý sự kiện click chuột
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void _OnMouseClick(MenuButton sender, MouseEventArgs e)
        {
            _RaiseEvent(MouseClickButton, new ButtonMouseEventArgs(e, new Point(sender.PointToScreen(e.Location)), _Buttons[sender]));
        }
        /// <summary>
        /// Xử lý sự kiện kéo thả vào nút
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void _OnDragDropEnter(MenuButton sender, DragEventArgs e)
        {
            _RaiseEvent(DragDropEnterButton, new ButtonDragDropEventArgs(e, _Buttons[sender]));
        }
        /// <summary>
        /// Xử lý sự kiện kéo qua nút
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void _OnDragOver(MenuButton sender, DragEventArgs e)
        {
            _RaiseEvent(DragDropOverButton, new ButtonDragDropEventArgs(e, _Buttons[sender]));
        }
        /// <summary>
        /// Xử lý sự kiện kéo rời khỏi nút
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void _onDragLeave(MenuButton sender, DragEventArgs e)
        {
            _RaiseEvent(DragDropLeaveButton, new ButtonDragDropEventArgs(e, _Buttons[sender]));
        }
        /// <summary>
        /// Xử lý sự kiện thả vào nút
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void _OnDragDrop(MenuButton sender, DragEventArgs e)
        {
            _RaiseEvent(DragDropButton, new ButtonDragDropEventArgs(e, _Buttons[sender]));
        }
        /// <summary>
        /// Xử lý bắt đầu kéo nút
        /// </summary>
        /// <param name="sender"></param>
        protected void _OnDoDragStart(MenuButton sender)
        {
            var eventObj = new DataObject(); // Empty dataobject
            eventObj.SetString(_Buttons[sender].GUID.ToString(), "MODEL_GUID");
            sender.DoDragDrop(eventObj, DragEffects.All, _Buttons[sender].Data.Properties.Icon, new PointF(10, 10));
        }
        /// <summary>
        /// Xử lý khi kéo nút kết thúc và không có đích thả nào chấp nhận icon
        /// <para>Điều này có nghĩa là icon được kéo thả ở bất cứ đâu nên cần bị xóa</para>
        /// </summary>
        /// <param name="sender"></param>
        protected void _OnDoDragEnd(MenuButton sender, DragEventArgs eventArgs)
        {
            _RaiseEvent(RemoveButton, new ButtonDragDropEventArgs(eventArgs, _Buttons[(sender)]));
        }
        /// <summary>
        /// Xử lý sự kiện menu ngữ cảnh của nút
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void _OnButtonContextMenu(MenuButton sender, MouseEventArgs e)
        {
            var model = _Buttons[sender];
            if (model == null) return; // Ngăn crash nếu model là null

            var location = sender.PointToScreen(e.Location); // chuyển đổi vị trí sang tọa độ màn hình

            _RaiseEvent(ButtonContextMenu, new ButtonMouseEventArgs(e, new Point(location), model));
        }
        /// <summary>
        /// Kích hoạt sự kiện (helper)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="EVENT"></typeparam>
        /// <param name="action"></param>
        /// <param name="e"></param>
        protected void _RaiseEvent<T, EVENT>(AppEventHandler<T, EVENT> action, EVENT e) where T : RadialMenuControl
        {
            action?.Invoke(this as T, e);
        }
        #endregion
    }
}
