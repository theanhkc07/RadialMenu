using System;
using System.Collections.Generic;
using System.ComponentModel;
using Eto.Drawing;
using Eto.Forms;
using RadialMenuPlugin.Data;
using RadialMenuPlugin.Utilities.Events;

namespace RadialMenuPlugin.Controls.Buttons.MenuButton
{
    #region Event Handler and Args classes
    /// <summary>
    /// Các lớp xử lý sự kiện và tham số
    /// </summary>
    using DragDropHandler = AppEventHandler<MenuButton, DragEventArgs>; // Dùng cho sự kiện kéo thả
    using DragDropStartHandler = AppEventHandler<MenuButton>; // Dùng cho sự kiện bắt đầu kéo
    using MouseEventHandler = AppEventHandler<MenuButton, MouseEventArgs>; // Dùng cho sự kiện chuột

    /// <summary>
    /// Tham số cho mục tiêu thả (Drop Target)
    /// </summary>
    public class DropTargetArgs : DragEventArgs
    {
        /// <summary>
        /// Chấp nhận thả hay không
        /// </summary>
        public bool acceptTarget = true;

        public DropTargetArgs(DragEventArgs d) : base(d.Source, d.Data, d.AllowedEffects, d.Location, d.Modifiers, d.Buttons, d.ControlObject)
        { }
    }
    #endregion

    /// <summary>
    /// Lớp nút menu (Menu Button)
    /// </summary>
    public class MenuButton : PixelLayout
    {
        #region Khai báo sự kiện
        /// <summary>
        /// Sự kiện click vào nút
        /// </summary>
        public event MouseEventHandler OnButtonClickEvent;
        /// <summary>
        /// Sự kiện khi một icon được thả vào nút
        /// </summary>
        public event DragDropHandler OnButtonDragDrop;
        /// <summary>
        /// Sự kiện khi kéo vào vùng nút
        /// </summary>
        public event DragDropHandler OnButtonDragEnter;
        /// <summary>
        /// Sự kiện khi kéo qua vùng nút
        /// </summary>
        public event DragDropHandler OnButtonDragOver;
        /// <summary>
        /// Sự kiện khi kéo rời khỏi vùng nút
        /// </summary>
        public event DragDropHandler OnButtonDragLeave;
        /// <summary>
        /// Sự kiện bắt đầu kéo icon của nút
        /// </summary>
        public event DragDropStartHandler OnButtonDragDropStart;
        /// <summary>
        /// Sự kiện kết thúc kéo icon của nút
        /// </summary>
        public event DragDropHandler OnButtonDragDropEnd;
        /// <summary>
        /// Sự kiện chuột di chuyển trên nút
        /// </summary>
        public event MouseEventHandler OnButtonMouseMove;
        /// <summary>
        /// Sự kiện chuột rời khỏi nút
        /// </summary>
        public event MouseEventHandler OnButtonMouseLeave;
        /// <summary>
        /// Sự kiện chuột đi vào nút
        /// </summary>
        public event MouseEventHandler OnButtonMouseEnter;
        /// <summary>
        /// Sự kiện yêu cầu hiển thị menu ngữ cảnh (chuột phải)
        /// </summary>
        public event MouseEventHandler OnButtonContextMenu;
        #endregion

        #region Trạng thái nút
        public struct ButtonStates : INotifyPropertyChanged
        {

            public event PropertyChangedEventHandler PropertyChanged;

            private void OnPropertyChanged(string info)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(info));
                }
            }

            /// <summary>
            /// Thuộc tính "isVisible" tùy chỉnh. Dùng cái này vì thuộc tính "Visible" của ETO ngăn control cập nhật NSTrackingArea đúng cách
            /// Xem thêm: https://github.com/picoe/Eto/issues/2704
            /// </summary>
            public bool IsVisible = true;

            private bool _IsHovering = false;
            private bool _IsSelected = false;
            private bool _IsEditMode = false;

            public bool IsHovering
            {
                get { return _IsHovering; }
                set
                {
                    // Thay đổi giá trị và thông báo thay đổi
                    _IsHovering = value;
                    OnPropertyChanged(nameof(IsHovering));
                }
            }

            public bool IsEditMode
            {
                get => _IsEditMode;
                set
                {
                    _IsEditMode = value;
                    OnPropertyChanged(nameof(IsEditMode));
                }
            }
            public bool IsSelected
            {
                get { return _IsSelected; }
                set
                {
                    // Thay đổi giá trị và thông báo thay đổi
                    _IsSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }

            public ButtonStates() { }
        }
        #endregion

        #region Hiệu ứng nút
        /// <summary>
        /// Độ trong suốt khi nút bị vô hiệu hóa
        /// </summary>
        private float _DisabledAlpha = (float)0.1;

        /// <summary>
        /// Độ trong suốt của nút
        /// </summary>
        private float _ButtonAlpha = (float)0.4;

        private void _AnimateHoverEffect()
        {
            if (!States.IsHovering)
            {
                _Buttons[ButtonType.over].Alpha = 0;
                _Buttons[ButtonType.normal].Alpha = Enabled ? (States.IsSelected ? 0 : _ButtonAlpha) : 0;
                _Buttons[ButtonType.selected].Alpha = Enabled ? (States.IsSelected ? _ButtonAlpha : 0) : 0;
                _Buttons[ButtonType.disabled].Alpha = Enabled ? 0 : _DisabledAlpha;
            }
            else
            {
                _Buttons[ButtonType.over].Alpha = _ButtonAlpha;
                _Buttons[ButtonType.normal].Alpha = 0;
                _Buttons[ButtonType.selected].Alpha = 0;
                _Buttons[ButtonType.disabled].Alpha = 0;
            }
            _Buttons[ButtonType.editmode].Alpha = States.IsEditMode ? 1 : 0;
        }
        private void _AnimateSelectedEffect()
        {
            _Buttons[ButtonType.over].Alpha = 0;
            _Buttons[ButtonType.normal].Alpha = States.IsSelected ? 0 : _ButtonAlpha;
            _Buttons[ButtonType.selected].Alpha = States.IsSelected ? _ButtonAlpha : 0;
            _Buttons[ButtonType.disabled].Alpha = 0;
            _Buttons[ButtonType.editmode].Alpha = States.IsEditMode ? 1 : 0;
        }
        private void _AnimateDisableEffect()
        {
            _Buttons[ButtonType.normal].Alpha = 0;
            _Buttons[ButtonType.over].Alpha = 0;
            _Buttons[ButtonType.selected].Alpha = 0;
            _Buttons[ButtonType.disabled].Alpha = _DisabledAlpha;
            _Buttons[ButtonType.editmode].Alpha = States.IsEditMode ? _ButtonAlpha : 0;
        }
        private void _AnimateEnableEffect()
        {
            if (States.IsHovering)
            {
                _Buttons[ButtonType.normal].Alpha = 0;
                _Buttons[ButtonType.over].Alpha = _ButtonAlpha;
                _Buttons[ButtonType.selected].Alpha = 0;
                _Buttons[ButtonType.disabled].Alpha = 0;
                _Buttons[ButtonType.editmode].Alpha = States.IsEditMode ? 1 : 0;
            }
            else
            {
                _Buttons[ButtonType.normal].Alpha = States.IsSelected ? 0 : _ButtonAlpha;
                _Buttons[ButtonType.over].Alpha = 0;
                _Buttons[ButtonType.selected].Alpha = States.IsSelected ? _ButtonAlpha : 0;
                _Buttons[ButtonType.disabled].Alpha = 0;
                _Buttons[ButtonType.editmode].Alpha = States.IsEditMode ? 1 : 0;
            }
        }
        private void _AnimateEditmode()
        {
            _Buttons[ButtonType.normal].Alpha = _ButtonAlpha;
            _Buttons[ButtonType.over].Alpha = 0;
            _Buttons[ButtonType.selected].Alpha = 0;
            _Buttons[ButtonType.disabled].Alpha = 0;
            _Buttons[ButtonType.editmode].Alpha = States.IsEditMode ? 1 : 0;
        }
        #endregion

        #region Thuộc tính nút (Button Properties)
        /// <summary>
        /// Tùy chỉnh và ghi đè ID để tạo ID cho nút
        /// </summary>
        public new string ID
        {
            get { return base.ID; }
            set
            {
                base.ID = value;
                _Model.ButtonID = value;// Cần đặt buttonID cho model để lấy cài đặt Plugin
            }
        }

        public override bool Enabled
        {
            get => Handler.Enabled;
            set
            {
                Handler.Enabled = value;
                if (value == false) _AnimateDisableEffect(); else _AnimateEnableEffect();
            }
        }
        private enum ButtonType
        {
            normal = 0,
            over = 1,
            selected = 2,
            disabled = 3,
            icon = 4,
            folderIcon = 5,
            editmode = 6,
            trigger = 7
        }

        private SectorData _SectorData = new SectorData();
        public SectorData SectorData
        {
            get => _SectorData;
            set
            {
                _UpdateSectorData(value);
                _SectorData = value;
            }
        }
        /// <summary>
        /// Các đối tượng vẽ dùng cho hoạt ảnh thay đổi trạng thái UI nút
        /// </summary>
        private Dictionary<ButtonType, ImageButton> _Buttons = new Dictionary<ButtonType, ImageButton>();

        /// <summary>
        /// Ràng buộc dữ liệu Model
        /// </summary>
        private ButtonModelData _Model = new ButtonModelData();
        public ButtonStates States = new ButtonStates();

        public BindableBinding<MenuButton, ButtonModelData> ButtonModelBinding => new BindableBinding<MenuButton, ButtonModelData>(
            this,
            (MenuButton obj) => obj._Model,
            // Cập nhật thuộc tính "model" với giá trị mới và đăng ký bộ xử lý sự kiện thay đổi thuộc tính của đối tượng "model"
            delegate (MenuButton obj, ButtonModelData value)
            {
                // Xóa bộ xử lý sự kiện thay đổi thuộc tính trên đối tượng "model" hiện tại
                _Model.PropertyChanged -= _ModelChangedHandler;
                _Model.Properties.PropertyChanged -= _ModelChangedHandler;
                // cập nhật thuộc tính
                obj._Model = value;
                // Thêm bộ xử lý sự kiện thay đổi trên "model"
                _Model.PropertyChanged += _ModelChangedHandler;
                _Model.Properties.PropertyChanged += _ModelChangedHandler;
                _UpdateIcon();
                _UpdateTriggerIcon();
            },
            // Thêm bộ xử lý sự kiện thay đổi
            delegate (MenuButton btn, EventHandler<EventArgs> changeEventHandler)
            { },
            // xóa bộ xử lý sự kiện thay đổi
            delegate (MenuButton btn, EventHandler<EventArgs> changeEventHandler)
            { });

        #endregion

        #region Constructor
        public MenuButton() : base()
        {
            Size = _SectorData.Size;
            _InitButtons();
            _InitEventHandlers();
            _InitStatesChangeHandler();
        }
        #endregion

        #region Phương thức riêng tư (Private Methods)
        private void _InitStatesChangeHandler()
        {
            // Bộ xử lý thay đổi thuộc tính trạng thái
            States.PropertyChanged += (obj, prop) =>
            {
                switch (prop.PropertyName)
                {
                    case nameof(States.IsSelected): // Hoạt ảnh thay đổi thuộc tính <selected>
                        _AnimateSelectedEffect();
                        break;
                    case nameof(States.IsEditMode):
                        _UpdateIconEditmode();
                        _AnimateEditmode();
                        break;
                    case nameof(States.IsHovering):
                        _AnimateHoverEffect();
                        break;
                    default: break;
                }
            };
        }
        /// <summary>
        /// Khởi tạo layout các nút
        /// </summary>
        private void _InitButtons()
        {
            // Tạo các nút
            _Buttons[ButtonType.normal] = new ImageButton();
            _Buttons[ButtonType.over] = new ImageButton();
            _Buttons[ButtonType.disabled] = new ImageButton();
            _Buttons[ButtonType.icon] = new ImageButton();
            _Buttons[ButtonType.selected] = new ImageButton();
            _Buttons[ButtonType.trigger] = new ImageButton();
            // ảnh chế độ chỉnh sửa (edit mode)
            var iconSize = new Size(20, 20);
            var img = Bitmap.FromResource("RadialMenu.Bitmaps.dashed-circle.png").WithSize(iconSize);
            _Buttons[ButtonType.editmode] = new ImageButton(img, iconSize);
            // ảnh icon thư mục
            var folderIconSize = new Size(16, 16);
            var folderIconimg = Bitmap.FromResource("RadialMenu.Bitmaps.plus_icon.png").WithSize(folderIconSize);
            _Buttons[ButtonType.folderIcon] = new ImageButton(folderIconimg, folderIconSize);

            // Thêm nút vào layout
            Add(_Buttons[ButtonType.normal], 0, 0);
            Add(_Buttons[ButtonType.over], 0, 0);
            Add(_Buttons[ButtonType.disabled], 0, 0);
            Add(_Buttons[ButtonType.selected], 0, 0);
            Add(_Buttons[ButtonType.icon], 0, 0);
            Add(_Buttons[ButtonType.folderIcon], 0, 0);
            Add(_Buttons[ButtonType.editmode], 0, 0);
            Add(_Buttons[ButtonType.trigger], 0, 0);

            _Buttons[ButtonType.over].Alpha = 0;
            _Buttons[ButtonType.disabled].Alpha = 0;
            _Buttons[ButtonType.selected].Alpha = 0;
            _Buttons[ButtonType.normal].Alpha = _ButtonAlpha;
            _Buttons[ButtonType.editmode].Alpha = 0;
        }
        /// <summary>
        /// Khởi tạo bộ xử lý sự kiện
        /// </summary>
        private void _InitEventHandlers()
        {
            // Sự kiện chuột
            MouseMove += _MouseMoveHandler;
            MouseLeave += _MouseLeaveHandler;
            MouseDown += _MouseDownHandler;

            // Sự kiện Kéo Thả cho chế độ chỉnh sửa
            AllowDrop = false;
            _Buttons[ButtonType.editmode].AllowDrop = true;
            _Buttons[ButtonType.editmode].DragEnter += _DragEnterHandler;
            _Buttons[ButtonType.editmode].DragOver += _DragOverHandler;
            _Buttons[ButtonType.editmode].DragLeave += _DragLeaveHandler;
            _Buttons[ButtonType.editmode].DragDrop += _DragDropHandler;
            DragEnd += (s, e) =>
            {
                if (e.Effects == DragEffects.None) // Không có mục tiêu thả nào chấp nhận icon: Chúng ta nên xóa icon
                {
                    _RaiseEvent(OnButtonDragDropEnd, e);
                }
            };
        }
        /// <summary>
        /// Cập nhật hiển thị nút ngay khi một Model mới được bind vào lớp này. 
        /// Khi xảy ra, thuộc tính "model" đã được cập nhật, nên chúng ta có thể sử dụng nó
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ModelChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ButtonModelData.Properties):
                case nameof(ButtonProperties.Icon):
                case nameof(ButtonProperties.IsActive):
                case nameof(ButtonProperties.IsFolder):
                    _UpdateIcon();
                    _UpdateTriggerIcon();
                    break;
                case nameof(ButtonProperties.Trigger):
                    _UpdateTriggerIcon();
                    break;
                default: break;
            }
        }
        /// <summary>
        /// Xử lý sự kiện nhấn chuột
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseDownHandler(object sender, MouseEventArgs e)
        {
            if (!States.IsHovering) return; // Chỉ phản hồi nếu chuột đang nằm trên nút này (do các nút có thể chồng lên nhau)
            switch (e.Buttons)
            {
                case MouseButtons.Primary:
                    // Chúng ta có thể kéo icon bằng cách nhấn phím Control + Chuột trái => Cấm kéo nếu nút là "folder"
                    // Fix: Dùng Keys.Control thay vì Keys.Application (dành cho Mac/Context)
                    if (e.Modifiers.HasFlag(Keys.Control))
                    {
                        if (_Model.Properties.IsActive && States.IsEditMode)
                        {
                            _RaiseEvent(OnButtonDragDropStart); // Kích hoạt sự kiện thông báo bắt đầu kéo
                        }
                    }
                    else
                    {
                        if (_Model.Properties.IsActive && !States.IsEditMode) // Lệnh CHỈ được thực thi khi KHÔNG ở chế độ chỉnh sửa
                        {
                            _RaiseEvent(OnButtonClickEvent, e);
                        }
                    }
                    break;
                case MouseButtons.Alternate:
                    // Hiển thị menu ngữ cảnh trong chế độ chỉnh sửa khi click chuột phải
                    if (States.IsEditMode)
                    {
                        _RaiseEvent(OnButtonContextMenu, e);
                    }
                    else
                    {
                        _RaiseEvent(OnButtonClickEvent, e);
                    }

                    break;
            }
        }
        /// <summary>
        /// Xử lý di chuyển chuột trong control. Lưu ý rằng chúng ta kiểm tra và kích hoạt các sự kiện chuột tùy chỉnh "leave", "over" và "enter" ở đây vì
        /// hình dạng không phải là hình chữ nhật. Vì vậy chúng ta chỉ muốn kích hoạt sự kiện cho hình dạng tùy chỉnh (cung tròn)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseMoveHandler(object sender, MouseEventArgs e)
        {
            if (States.IsVisible == true)
            {
                // Đảm bảo cửa sổ plugin chính có tiêu điểm -> khắc phục sự kiện click không hoạt động nếu cửa sổ chính không có tiêu điểm
                // Nếu cửa sổ chính không có tiêu điểm, sự kiện click sẽ chuyển tiêu điểm cho cửa sổ chính và không có sự kiện click nào trên nút xảy ra
                var oldHovering = _MouseMoveUpdate(e);

                if (States.IsHovering) // Chuột đang ở trên nút
                {
                    if (oldHovering) // Chuột đã ở trên nút từ trước -> Kích hoạt sự kiện mouse over
                    {
                        _RaiseEvent(OnButtonMouseMove, e); // Thông báo chuột đang ở trên nút
                    }
                    else // Chuột mới đi vào nút
                    {
                        _RaiseEvent(OnButtonMouseEnter, e);
                    }
                }
                else // Chuột không ở trên nút
                {
                    if (oldHovering) // chuột đã ở trên nút trước đó -> Kích hoạt sự kiện leave
                    {
                        ToolTip = "";
                        _RaiseEvent(OnButtonMouseLeave, e);
                    }
                }
            }
        }
        /// <summary>
        /// Xử lý khi chuột rời khỏi control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseLeaveHandler(object sender, MouseEventArgs e)
        {
            if (States.IsHovering) // Tránh gửi sự kiện "leave" hai lần
            {
                States.IsHovering = false;
                _RaiseEvent(OnButtonMouseLeave, e);
            }
        }
        private void _DragEnterHandler(object sender, DragEventArgs e)
        {
            _RaiseEvent(OnButtonDragEnter, e);
            if (e.Effects != DragEffects.None)
            {
                States.IsHovering = true;
            }
        }
        /// <summary>
        /// Xử lý khi kéo rời khỏi. Ngay khi kéo ra khỏi hình chữ nhật khung, chúng ta chắc chắn kéo đã thoát khỏi nút.
        /// LƯU Ý rằng chúng ta phải kiểm tra trạng thái "leave" chưa được kích hoạt bởi phương thức @dragOverHandler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DragLeaveHandler(object sender, DragEventArgs e)
        {
            _RaiseEvent(OnButtonDragLeave, e);
            States.IsHovering = false;
        }
        private void _DragOverHandler(object sender, DragEventArgs e)
        {
            _RaiseEvent(OnButtonDragOver, e);
            if (e.Effects != DragEffects.None)
            {
                States.IsHovering = true;
            }
        }
        private void _DragDropHandler(object sender, DragEventArgs e)
        {
            _RaiseEvent(OnButtonDragDrop, e);
            States.IsHovering = false;
        }
        /// <summary>
        /// Kích hoạt sự kiện (helper)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        private void _RaiseEvent<T>(AppEventHandler<T> action) where T : MenuButton
        {
            action?.Invoke(this as T);
        }
        /// <summary>
        /// Kích hoạt sự kiện với tham số (helper)
        /// </summary>
        /// <param name="action"></param>
        /// <param name="e"></param>
        private void _RaiseEvent<T, E>(AppEventHandler<T, E> action, E e) where T : MenuButton
        {
            switch (typeof(E).Name)
            {
                //
                // Sự kiện Kéo/Thả
                //
                case nameof(DragEventArgs):
                    // Kích hoạt sự kiện nếu đang ở chế độ chỉnh sửa VÀ nút đang hiển thị
                    // LƯU Ý: Sự kiện với nsView alpha=0, sự kiện kéo vẫn được kích hoạt
                    if (States.IsEditMode && States.IsVisible)
                    {
                        if (action != null)
                        {
                            action.Invoke(this as T, e);
                        }
                    }
                    break;
                //
                // Sự kiện Chuột
                //
                case nameof(MouseEventArgs):
                    action?.Invoke(this as T, e);
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// Cập nhật trạng thái Hover của nút khi chuột di chuyển qua nút.
        /// Trả về trạng thái "hovering" cũ để so sánh với trạng thái mới
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool _MouseMoveUpdate(MouseEventArgs e)
        {
            var oldHovering = States.IsHovering ? true : false;

            if (States.IsVisible == true)
            {
                var new_isHovering = _SectorData.IsPointInShape(e.Location);

                if (new_isHovering)  // Chuột đang ở trên nút
                {
                    if (!States.IsHovering) // Chuột mới đi vào nút
                    {
                        States.IsHovering = true;
                        _AnimateHoverEffect();
                    }
                }
                else // Chuột không ở trên nút
                {
                    if (States.IsHovering) // chuột đã ở trên nút trước đó
                    {
                        States.IsHovering = false;
                        _AnimateHoverEffect();
                    }
                }
            }
            return oldHovering;
        }
        /// <summary>
        /// Cập nhật hiển thị và vị trí icon
        /// </summary>
        private void _UpdateIcon()
        {
            // Cập nhật ảnh icon. đặt thành "null" nếu không nên hiển thị icon
            if (_Model.Properties.IsActive) _Buttons[ButtonType.icon].SetImage(_Model.Properties.Icon); else _Buttons[ButtonType.icon].SetImage(null);

            // nếu icon tồn tại, cập nhật vị trí của nó
            if (_Model.Properties.Icon != null && _Model.Properties.IsActive)
            {
                var arcCenterWorld = _SectorData.SectorCenter();
                var arcCenterLocal = _SectorData.ConvertWorldToLocal(arcCenterWorld);
                var posX = arcCenterLocal.X - (_Model.Properties.Icon.Size.Width / 2.0);
                var posY = arcCenterLocal.Y - (_Model.Properties.Icon.Size.Height / 2.0);

                Move(_Buttons[ButtonType.icon], (int)Math.Round(posX), (int)Math.Round(posY)); // cập nhật vị trí icon
                if (_Model.Properties.IsFolder)
                {
                    var outerCenterLocationWorld = _SectorData.GetPoint(_SectorData.SweepAngle / 2, _SectorData.Thickness - (_Buttons[ButtonType.folderIcon].Width / 2) - 2);
                    var outerCenterLocationLocal = _SectorData.ConvertWorldToLocal(outerCenterLocationWorld);
                    posX = outerCenterLocationLocal.X - (_Buttons[ButtonType.folderIcon].Width / 2);
                    posY = outerCenterLocationLocal.Y - (_Buttons[ButtonType.folderIcon].Height / 2);
                    _Buttons[ButtonType.folderIcon].Alpha = (float)0.4;
                    Move(_Buttons[ButtonType.folderIcon], (int)posX, (int)posY);
                }
                else
                {
                    _Buttons[ButtonType.folderIcon].Alpha = 0;
                }
            }
            else
            {
                _Buttons[ButtonType.folderIcon].Alpha = 0;
            }
        }
        /// <summary>
        /// Cập nhật hình ảnh và kích thước nút
        /// </summary>
        /// <param name="data"></param>
        private void _UpdateSectorData(SectorData data)
        {
            // Cập nhật dữ liệu bản thân
            Size = data.Size;
            // Cập nhật hình ảnh các nút
            _Buttons[ButtonType.normal].SetImage(data.Images.NormalStateImage, data.Size);
            // _Buttons[_ButtonType.normal].SetImage(data.Images.SectorMask, data.Size);
            _Buttons[ButtonType.over].SetImage(data.Images.OverStateImage, data.Size);
            _Buttons[ButtonType.disabled].SetImage(data.Images.DisabledStateImage, data.Size);
            _Buttons[ButtonType.selected].SetImage(data.Images.SelectedStateImage, data.Size);
            _Buttons[ButtonType.icon].SetImage(_Model.Properties.Icon, data.Size);

            // Cập nhật vị trí
            _UpdateIcon();
            _UpdateTriggerIcon();
        }
        /// <summary>
        /// Cập nhật vị trí icon chế độ chỉnh sửa
        /// </summary>
        private void _UpdateIconEditmode()
        {
            // Khi ở chế độ chỉnh sửa, cập nhật vị trí icon chỉnh sửa
            if (States.IsEditMode)
            {
                var arcCenterWorld = _SectorData.SectorCenter();
                var arcCenterLocal = _SectorData.ConvertWorldToLocal(arcCenterWorld);
                var posX = arcCenterLocal.X - (_Buttons[ButtonType.editmode].Width / 2f);
                var posY = arcCenterLocal.Y - (_Buttons[ButtonType.editmode].Height / 2f);
                Move(_Buttons[ButtonType.editmode], (int)Math.Round(posX), (int)Math.Round(posY));
            }
        }
        /// <summary>
        /// Cập nhật icon phím kích hoạt (trigger key)
        /// </summary>
        private void _UpdateTriggerIcon()
        {
            Bitmap bm = null;
            if (_Model.Properties.Trigger != "")
            {
                var fontSize = 8;
                var bitmapSize = new Size(fontSize + 2, fontSize + 2); // +2 vì font gạch chân
                bm = new Bitmap(bitmapSize, PixelFormat.Format32bppRgba);
                var g = new Graphics(bm); g.PixelOffsetMode = PixelOffsetMode.Half; g.AntiAlias = true;
                var font = Fonts.Sans(fontSize, FontStyle.None, FontDecoration.Underline);
                g.DrawText(font, Colors.Black, 0, 0, _Model.Properties.Trigger.ToUpper());
                g.Dispose();
                _Buttons[ButtonType.trigger].SetImage(bm, new Size(16, 16));
                var worldPt = _SectorData.GetPoint(_SectorData.SweepAngle / 2, 10);
                var localPt = _SectorData.ConvertWorldToLocal(worldPt);
                var posX = localPt.X - (_Buttons[ButtonType.trigger].Width / 2f);
                var posY = localPt.Y - (_Buttons[ButtonType.trigger].Height / 2f);
                Move(_Buttons[ButtonType.trigger], (int)Math.Round(posX), (int)Math.Round(posY));
            }

            if (_Model.Properties.Trigger == "")
            {
                _Buttons[ButtonType.trigger].SetImage(null, new Size(16, 16));
            }
        }

        #endregion
    }
}
