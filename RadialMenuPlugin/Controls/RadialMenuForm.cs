using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using Rhino;
using Rhino.PlugIns;
using NLog;
using RadialMenuPlugin.Data;
using RadialMenuPlugin.Controls.Buttons.MenuButton;
using RadialMenuPlugin.Controls.ContextMenu.MenuButton;
using RadialMenuPlugin.Controls.Buttons.Shaped.Base;
using RadialMenuPlugin.Controls.Buttons.Shaped.Form.Center;
using SysDrawing = System.Drawing;
using SysDrawing2D = System.Drawing.Drawing2D;

namespace RadialMenuPlugin.Controls
{
    public class RadialMenuForm : TransparentForm
    {
        public static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #region Cấu hình Tham số (Configuration Parameters)
        /// <summary>
        /// Bán kính vòng tròn trong cùng (mặc định: 30)
        /// </summary>
        protected static int s_defaultInnerRadius = 48;

        /// <summary>
        /// Độ dày của mỗi vòng tròn (mặc định: 48)
        /// </summary>
        protected static int s_defaultThickness = 48;

        /// <summary>
        /// Số cấp độ tối đa của menu (mặc định: 3)
        /// </summary>
        protected static int s_maxLevels = 3;

        /// <summary>
        /// Kích thước tổng thể của menu (mặc định: 400x400)
        /// </summary>
        protected static Size s_menuSize = new Size(400, 400);

        /// <summary>
        /// Khoảng cách giữa các vòng tròn (mặc định: 4)
        /// </summary>
        protected static int s_levelSpacing = 4;

        /// <summary>
        /// Số lượng icon (sector) cho vòng tròn cấp 2 (mặc định: 16)
        /// </summary>
        protected static int s_level2SectorCount = 16;

        /// <summary>
        /// Số lượng icon (sector) cho vòng tròn cấp 3 (mặc định: 24)
        /// </summary>
        protected static int s_level3SectorCount = 24;

        /// <summary>
        /// Dịch chuyển tâm vòng tròn theo phương X (mặc định: 0)
        /// </summary>
        protected static int s_centerOffsetX = 0;

        /// <summary>
        /// Dịch chuyển tâm vòng tròn theo phương Y (mặc định: 0)
        /// </summary>
        protected static int s_centerOffsetY = 0;
        #endregion

        #region Thuộc tính công khai (Public properties)
        /// <summary>
        /// Cập nhật vùng hiển thị của cửa sổ (Window Region) để tạo hình dạng trong suốt
        /// </summary>
        private void _UpdateWindowRegion()
        {
            if (Rhino.Runtime.HostUtils.RunningOnWindows)
            {
                try
                {
                    var centerX = Size.Width / 2;
                    var centerY = Size.Height / 2;

                    var region = new SysDrawing.Region(new SysDrawing.Rectangle(0, 0, 0, 0));
                    region.MakeEmpty();

                    // 1. Nút trung tâm
                    // Nút trung tâm nằm ở (centerX - width/2, centerY - height/2)
                    // Thu nhỏ 1px để tránh viền đen (halo)
                    var centerBtnRect = new SysDrawing.Rectangle(
                        centerX - _CenterMenuButton.Size.Width / 2 + 1,
                        centerY - _CenterMenuButton.Size.Height / 2 + 1,
                        _CenterMenuButton.Size.Width - 2,
                        _CenterMenuButton.Size.Height - 2
                    );

                    using (var path = new SysDrawing2D.GraphicsPath())
                    {
                        path.AddEllipse(centerBtnRect);
                        region.Union(path);
                    }

                    // 2. Các vòng tròn (Levels/Rings)
                    foreach (var level in _Levels)
                    {
                        // Thu nhỏ vùng một chút (1px) để tránh hiện tượng halo đen 
                        // do khử răng cưa (anti-aliasing) hòa trộn với nền đen của form ở các cạnh.
                        var inner = level.InnerRadius + 1;
                        var outer = level.InnerRadius + level.Thickness - 1;

                        using (var pathOuter = new SysDrawing2D.GraphicsPath())
                        using (var pathInner = new SysDrawing2D.GraphicsPath())
                        {
                            pathOuter.AddEllipse(centerX - outer, centerY - outer, outer * 2, outer * 2);
                            pathInner.AddEllipse(centerX - inner, centerY - inner, inner * 2, inner * 2);

                            var ringRegion = new SysDrawing.Region(pathOuter);
                            ringRegion.Exclude(pathInner);
                            region.Union(ringRegion);
                        }
                    }

                    dynamic native = this.NativeHandle;
                    native.Region = region;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to set window region");
                }
            }
        }
        #endregion

        #region Thuộc tính Bảo vệ/Riêng tư (Protected/Private properties)
        protected PixelLayout _Layout = new PixelLayout();
        protected List<RadialMenuLevel> _Levels = new List<RadialMenuLevel>() {
                new RadialMenuLevel(1,s_defaultInnerRadius,s_defaultThickness),
                new RadialMenuLevel(2,s_defaultInnerRadius+s_defaultThickness+s_levelSpacing,s_defaultThickness,0, s_level2SectorCount), // Level 2
                new RadialMenuLevel(3,s_defaultInnerRadius+s_defaultThickness+s_levelSpacing+s_defaultThickness+s_levelSpacing,s_defaultThickness,0, s_level3SectorCount), // Level 3
            };
        /// <summary>
        /// Theo dõi Control nguồn kéo để đăng ký/hủy đăng ký sự kiện "dragEnd"
        /// </summary>
        protected Control _Dragsource;
        /// <summary>
        /// Menu ngữ cảnh (Context menu)
        /// </summary>
        protected ButtonSettingEditorForm _ContextMenuForm;
        /// <summary>
        /// Model hiện tại của nút đang được chuột trỏ vào
        /// <para>
        /// LƯU Ý:
        /// </para>
        /// <para>
        /// Chúng ta sử dụng cái này vì sự kiện chuột enter/leave của nút không xảy ra tuần tự, và đôi khi sự kiện leave xảy ra sau sự kiện enter.
        /// </para>
        /// <para>
        /// Điều đó dẫn đến việc tooltip "nút trung tâm" không hiển thị. Vì vậy chúng ta dùng thuộc tính này trong <see cref="_UpdateTooltipBinding"/> để kiểm tra xem có nên cập nhật binding cho tooltip nút trung tâm hay không
        /// </para>
        /// </summary>
        protected Model _CurrentButtonModel;

        /// <summary>
        /// Chế độ menu radial hiện tại: true=>Chế độ chỉnh sửa, false=>Chế độ bình thường
        /// </summary>
        protected bool _EditMode = false;
        /// <summary>
        /// Liên kết một cấp menu với một Control kiểu <SectorArcRadialControl>
        /// </summary>
        protected Dictionary<RadialMenuLevel, RadialMenuControl> _Controls = new Dictionary<RadialMenuLevel, RadialMenuControl>();
        protected FormCenterButton _CenterMenuButton;
        protected readonly Macro MouseHoverLeftTooltip = new Macro("", "Đóng");
        protected readonly Macro MouseHoverRightTooltip = new Macro("", "Chỉnh sửa menu");
        #endregion

        #region Phương thức công khai (Public methods)


        /// <summary>
        /// Khởi tạo form menu radial
        /// </summary>
        /// <param name="plugin"></param>
        public RadialMenuForm(PlugIn plugin) : base(plugin)
        {
            Size = new Size(s_menuSize.Width, s_menuSize.Height);
            _InitLevels(); // Tạo các control menu radial "trống" (tức là không có ID nút, model trống)
            KeyDown += (s, e) =>
            {
                // Chỉ phản hồi phím nhấn khi không có menu ngữ cảnh nào đang hiển thị và phím không phải là "escape"
                // Chúng ta sẽ quản lý phím nhấn cho nút kích hoạt bằng bàn phím ở đây
                // LƯU Ý: sự kiện keyup "escape" được quản lý trong bộ xử lý "_OnEscapePressed" và cũng được kích hoạt bởi lớp RadialMenuCommand
                if (e.Key == Keys.Escape) return;
                if (_ContextMenuForm.Visible == false)
                {
                    _OnKeyPressed(s, e);
                }
            };
            // Khởi tạo Form menu ngữ cảnh cho nút
            _ContextMenuForm = new ButtonSettingEditorForm();
            _ContextMenuForm.TriggerTextChanging += _OnContextMenuTriggerChanging; // Kiểm tra phím kích hoạt chưa được sử dụng trong cấp menu radial

            // Đảm bảo khi form được hiển thị, chúng ta chỉ hiển thị cấp menu đầu tiên
            Shown += (o, e) =>
            {
                _UpdateWindowRegion();
                foreach (var ctrl in _Controls.Values)
                {
                    ctrl.Show(false); // Ẩn và reset kích hoạt và lựa chọn của radial control
                    ctrl.SwitchEditMode(false); // đảm bảo tắt chế độ chỉnh sửa
                }
                var radialControl = _Controls.First(obj => obj.Key.Level == 1).Value;
                radialControl.SwitchEditMode(false);
                _EditMode = false;
                radialControl.Show(true);
                Focus(); // Đảm bảo form có focus khi hiển thị
            };

            // Tự động đóng form khi mất focus (click ra ngoài), trừ khi đang mở menu ngữ cảnh
            LostFocus += (o, e) =>
            {
                if (_ContextMenuForm != null && _ContextMenuForm.Visible) return;
                _OnCloseClickEvent(this);
            };

            // Tạo nút đóng (close button)
            _InitCenterButton();
            _Layout.Add(_CenterMenuButton, (Size.Width / 2) - _CenterMenuButton.Size.Width / 2, (Size.Height / 2) - (_CenterMenuButton.Size.Height / 2));

            // Tạo và thêm RadialMenu Control cho cấp 1
            var ctrl = _Controls.First(level => level.Key.Level == 1).Value;
            ctrl.SetMenuForButtonID(null); // Khởi tạo nút và model cho cấp menu 1

            // Thêm layout vào nội dung của form
            Content = _Layout;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="owner"></param>
        public RadialMenuForm(PlugIn plugin, Window owner) : this(plugin)
        {
            Owner = owner;
        }

        #endregion

        #region Phương thức Bảo vệ/Riêng tư (Protected/Private methods)
        /// <summary>
        /// Khởi tạo các control cấp menu
        /// </summary>
        protected void _InitLevels()
        {
            for (int i = 0; i < s_maxLevels; i++)
            {
                var ctrl = new RadialMenuControl(_Levels[i]);

                // Sự kiện chuột
                ctrl.MouseEnterButton += _RadialControlMouseEnterButtonHandler;
                ctrl.MouseMoveButton += _RadialControlMouseMoveButtonHandler;
                ctrl.MouseLeaveButton += _RadialControlMouseLeaveButtonHandler;
                ctrl.MouseClickButton += _RadialControlMouseClickHandler;
                ctrl.ButtonContextMenu += _RadialControlContextMenu;

                // Sự kiện Kéo Thả
                ctrl.DragDropEnterButton += _RadialControlDragEnterHandler;
                ctrl.DragDropOverButton += _RadialControlDragOverHandler;
                ctrl.DragDropButton += _RadialControlDragDropHandler;
                ctrl.RemoveButton += _RadialControlRemoveButton;
                _Controls.Add(_Levels[i], ctrl);
            }
            //
            // LƯU Ý Thứ tự RẤT quan trọng vì nút cung tròn là hình chữ nhật. Vì vậy nếu submenu cấp 2 không phải là "trên cùng" (tức là vị trí cuối cùng), drag over sẽ không hoạt động và đôi khi
            // drag over không phát hiện được nút cấp "2"
            //
            var cx = (Size.Width / 2) + s_centerOffsetX;
            var cy = (Size.Height / 2) + s_centerOffsetY;
            _Layout.Add(_Controls.ElementAt(0).Value, cx - (_Controls.ElementAt(0).Value.Size.Width / 2), cy - (_Controls.ElementAt(0).Value.Size.Height / 2));
            _Layout.Add(_Controls.ElementAt(2).Value, cx - (_Controls.ElementAt(2).Value.Size.Width / 2), cy - (_Controls.ElementAt(2).Value.Size.Height / 2));
            _Layout.Add(_Controls.ElementAt(1).Value, cx - (_Controls.ElementAt(1).Value.Size.Width / 2), cy - (_Controls.ElementAt(1).Value.Size.Height / 2));
        }
        /// <summary>
        /// Chấp nhận DragDrop
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        protected bool _AcceptDragDrop(ButtonDragDropEventArgs eventArgs)
        {
            var sourceType = Utilities.DragDropUtilities.dragSourceType(eventArgs.DragEventSourceArgs.Source);
            switch (sourceType)
            {
                case DragSourceType.radialMenuItem:
                    var guid = eventArgs.DragEventSourceArgs.Data.GetString("MODEL_GUID");
                    var sourceDragModel = ModelController.Instance.Find(guid);
                    var ret = false;
                    if (sourceDragModel != null)
                    {
                        if (sourceDragModel.Data.Properties.IsFolder)
                        {
                            if (sourceDragModel == eventArgs.TargetModel) // Nếu nguồn được kéo vào vị trí ban đầu, chấp nhận thả
                            {
                                eventArgs.DragEventSourceArgs.Effects = DragEffects.All;
                                ret = true;
                            }
                            else
                            {
                                eventArgs.DragEventSourceArgs.Effects = DragEffects.None;
                            }
                        }
                        else
                        {
                            eventArgs.DragEventSourceArgs.Effects = DragEffects.All;
                            ret = true;
                        }
                    }
                    return ret;
                case DragSourceType.rhinoItem:
                    eventArgs.DragEventSourceArgs.Effects = DragEffects.All;
                    return true;
                case DragSourceType.unknown:
                default:
                    eventArgs.DragEventSourceArgs.Effects = DragEffects.None; // Từ chối thả nếu nguồn không xác định
                    return false;
            }
        }
        /// <summary>
        /// Tạo bộ xử lý sự kiện DragEnd cho control nguồn bắt đầu kéo/thả. Sẽ xóa bộ xử lý DragEnd hiện có nếu tồn tại
        /// </summary>
        /// <param name="sourceObject"></param>
        protected void _RegisterDragEndHandler(Control sourceObject)
        {
            if (_Dragsource != null)
            {
                _Dragsource.DragEnd -= _RadialControlDragEndHandler; // Xóa bộ xử lý sự kiện cho Control nguồn kéo trước đó
            }
            _Dragsource = sourceObject;
            _Dragsource.DragEnd += _RadialControlDragEndHandler;
        }
        /// <summary>
        /// Cập nhật model sau khi kéo/thả hoàn tất
        /// </summary>
        /// <param name="updatedProperties"></param>
        /// <param name="targetModel"></param>
        /// <param name="targetLevel"></param>
        protected void _UpdateDropItem(ButtonProperties updatedProperties, Model targetModel, int targetLevel)
        {
            targetModel.Data.Properties.Icon = updatedProperties.Icon;
            targetModel.Data.Properties.IsActive = true;
            targetModel.Data.Properties.IsFolder = false;
            targetModel.Data.Properties.LeftMacro = updatedProperties.LeftMacro;
            targetModel.Data.Properties.RightMacro = updatedProperties.RightMacro;
            targetModel.Data.Properties.CommandGUID = updatedProperties.CommandGUID;

            if (targetLevel > 1) // Nếu icon được thả vào menu con -> Cập nhật model menu cha
            {
                var parentModel = targetModel.Parent;
                do
                {
                    // Giữ nguyên icon cha nếu đã được đặt
                    if (parentModel.Data.Properties.Icon == null)
                    {
                        parentModel.Data.Properties.Icon = updatedProperties.Icon;
                    }
                    // Giữ nguyên "isFolder" cha nếu đã được đặt
                    if (parentModel.Data.Properties.IsFolder == false)
                    {
                        parentModel.Data.Properties.IsFolder = true;
                    }
                    parentModel.Data.Properties.IsActive = true; // Đảm bảo icon hoạt động
                    parentModel.Data.Properties.CommandGUID = updatedProperties.CommandGUID;
                    parentModel = parentModel.Parent;
                } while (parentModel != null);
            }
        }
        /// <summary>
        /// Tìm và trả về control đầu tiên khớp với <paramref name="predicate"/>
        /// </summary>
        /// <param name="predicate">hàm để tìm đối tượng</param>
        /// <returns>Instance Control nếu tìm thấy, null nếu không tìm thấy</returns>
        protected RadialMenuControl _GetControl(Func<RadialMenuControl, bool> predicate)
        {
            RadialMenuControl ctrl = null;
            foreach (RadialMenuControl menuControl in _Controls.Values)
            {
                if (predicate(menuControl))
                {
                    ctrl = menuControl;
                    break;
                }
            }
            return ctrl;
        }
        protected void _RunRhinoCommand(string command)
        {
            _OnCloseClickEvent(this); // đóng menu radial
            RhinoApp.SetFocusToMainWindow();
            RhinoApp.RunScript(command, false); // Chạy lệnh Rhino
        }
        /// <summary>
        /// Mở menu con từ control menu đang mở và ID nút đã cho
        /// </summary>
        /// <param name="radialMenuControl">Menu con đang mở hiện tại. Được cung cấp để tính toán cấp menu con tiếp theo</param>
        /// <param name="model">Model chứa ID nút hiện tại để mở menu con cho nó</param>
        /// <returns>Menu con <see cref="RadialMenuControl"/> mới được mở hoặc null nếu không có menu con mới nào được mở</returns>
        protected RadialMenuControl _ShowSubmenu(RadialMenuControl radialMenuControl, Model model)
        {
            var nextLevel = radialMenuControl.Level.Level + 1; // Tính số cấp tiếp theo
            var ctrl = _GetControl(element => element.Level.Level == nextLevel);
            if (ctrl != null)
            {
                ctrl.SetMenuForButtonID(model); // Cập nhật các nút control radial
                ctrl.Show(true); // Đảm bảo control hiển thị
            }
            return ctrl;
        }

        /// <summary>
        /// Ẩn tất cả các menu con từ cấp cuối/lớn nhất đến cấp của <paramref name="radialMenuControl"/>
        /// <para>Nếu không cung cấp <paramref name="radialMenuControl"/>, đóng tất cả menu con ngoại trừ menu gốc</para>
        /// </summary>
        /// <param name="radialMenuControl">Control menu cần giữ lại</param>
        private void _HideSubmenu(RadialMenuControl radialMenuControl = null)
        {
            var levelToClose = radialMenuControl == null ? 1 : radialMenuControl.Level.Level;
            foreach (var control in _Controls.Values)
            {
                // Ẩn control menu nếu cấp cao hơn cấp được cung cấp (hoặc cao hơn menu control "gốc")
                if (control.Level.Level > levelToClose)
                {
                    control.Show(false);
                    control.DisableButtonsExceptSelection(); // Bật tất cả các nút
                    control.SelectedButtonID = ""; // Đặt lại lựa chọn
                }
            }
        }
        /// <summary>
        /// Thực hiện dọn dẹp visual control trước khi thoát (Ẩn) đối tượng Form plugin
        /// </summary>
        /// <param name="sender"></param>
        protected override void _OnCloseClickEvent(object sender)
        {
            foreach (var control in _Controls.Values)
            {
                control.Show(false);
                control.DisableButtonsExceptSelection();
            }
            base._OnCloseClickEvent(sender);
        }
        /// <summary>
        /// Cập nhật binding cho nút menu trung tâm
        /// <para>
        /// LƯU Ý: model có thể null để xóa văn bản hiển thị
        /// </para>
        /// </summary>
        /// <param name="model"></param>
        protected void _UpdateTooltipBinding(Model model = null)
        {
            if (model == null)
            {
                _CenterMenuButton.SetTooltip();
            }
            else
            {
                // Hiển thị tooltip macro trái/phải cho cả nút thường và nút thư mục (nếu có lệnh)
                _CenterMenuButton.SetTooltip(model.Data.Properties.LeftMacro, model.Data.Properties.RightMacro);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected bool _TriggerMenuItem(char key)
        {
#nullable enable
            RadialMenuControl? openedControl = null; // Menu mở cao nhất hiện tại
#nullable disable
            // Lấy cấp hiện đang hiển thị và model cha từ nút được chọn của model cha
            foreach (var control in _Controls)
            {
                if (control.Value.IsVisible)
                {
                    if (control.Value.Level.Level > (openedControl?.Level.Level ?? 0))
                    {
                        openedControl = control.Value;
                    }
                }
            }
            if (openedControl != null)
            {
                foreach (var model in openedControl.GetModels())
                {
                    if (model.Data.Properties.Trigger.ToUpper() == key.ToString().ToUpper())
                    {
                        if (model.Data.Properties.IsFolder)
                        {
                            _ShowSubmenu(openedControl, model);
                            return true;
                        }
                        else
                        {
                            if (model.Data.Properties.LeftMacro.Script != "")
                            {
                                _RunRhinoCommand(model.Data.Properties.LeftMacro.Script);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        #endregion

        #region Event handlers
        /// <summary>
        /// Mỗi lần nhấn phím ESC sẽ đóng menu con cho đến khi đạt đến menu con cấp 1 => Đóng menu radial
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void _OnEscapePressed(object sender, KeyEventArgs e)
        {
            // Trước tiên kiểm tra xem contextmenu có đang mở không. Nếu có, đóng nó
            if (_ContextMenuForm.Visible)
            {
                _ContextMenuForm.Close();
                return;
            }
            if (Visible) // Đảm bảo menu radial hiện đang chạy
            {
                RadialMenuLevel topMostOpenedLevel = null;
                foreach (var control in _Controls)
                {
                    if (control.Value.IsVisible && (control.Key.Level > (topMostOpenedLevel == null ? 0 : topMostOpenedLevel.Level)))
                    {
                        topMostOpenedLevel = control.Key;
                    }
                }
                // Ẩn menu con cao nhất nếu tìm thấy
                if (topMostOpenedLevel != null)
                {
                    if (topMostOpenedLevel.Level == 1)
                    {
                        // Đóng menu radial
                        _OnCloseClickEvent(this);
                    }
                    else
                    {
                        _Controls[topMostOpenedLevel].Show(false);
                    }
                }
            }
        }
        /// <summary>
        /// Bộ xử lý sự kiện cho thay đổi văn bản kích hoạt menu ngữ cảnh: Không cho phép cùng một phím kích hoạt ở cùng một cấp
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnContextMenuTriggerChanging(object sender, TextChangingEventArgs e)
        {
            if (e.NewText != "" && _ContextMenuForm.Model.Data.Properties.Trigger != e.NewText)
            {
                foreach (var model in ModelController.Instance.GetSiblings(_ContextMenuForm.Model))
                {
                    if (model.Data.Properties.Trigger == e.NewText.ToUpper())
                    {
                        e.Cancel = true;
                    }
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void _OnKeyPressed(object sender, KeyEventArgs e)
        {
            if (e.IsChar)
            {
                e.Handled = _TriggerMenuItem(e.KeyChar);
            }
        }
        //
        //  Bộ xử lý chuột enter, over và leave để ngăn hành vi mặc định của control radial khi cần (ví dụ: Tránh bỏ chọn nút)
        //  Những hành vi tùy chỉnh này sẽ ảnh hưởng đến việc kích hoạt sự kiện "SelectionChanged", vì vậy logic hiển thị các cấp menu con được quản lý trong sự kiện "onSelectionChanged"
        //
        /// <summary>
        /// Chuột đi vào một nút: Quản lý hiện/ẩn menu con
        /// </summary>
        /// <param name="radialMenuControl"></param>
        protected void _RadialControlMouseEnterButtonHandler(RadialMenuControl radialMenuControl, ButtonMouseEventArgs e)
        {
            Logger.Debug($"Mouse enter button {e.Model.Data.ButtonID}");
            if (e.Model.Data.Properties.IsFolder)
            {
                var newSubmenu = _ShowSubmenu(radialMenuControl, e.Model);
                // Ẩn các cấp cao hơn từ menu con mới mở nếu có
                if (newSubmenu != null)
                {
                    _HideSubmenu(newSubmenu);
                }
            }
            else // Nếu chúng ta vào một mục không phải thư mục, ẩn các menu con cao hơn
            {
                _HideSubmenu(radialMenuControl); // Ẩn bất kỳ menu con nào cao hơn đang mở
            }
            _UpdateTooltipBinding(e.Model); // Cập nhật tooltip nút trung tâm
        }

        /// <summary>
        /// Chuột di chuyển trên một nút
        /// </summary>
        /// <param name="radialMenuControl"></param>
        protected void _RadialControlMouseMoveButtonHandler(RadialMenuControl radialMenuControl, ButtonMouseEventArgs e)
        {
            if (_ContextMenuForm.Visible == false)
            {
                Focus(); // Cấp focus cho menu radial khi chuột di chuyển qua một nút
            }
            // _UpdateTooltipBinding(e.Model); // update tooltip
        }
        protected void _RadialControlMouseClickHandler(RadialMenuControl radialMenuControl, ButtonMouseEventArgs e)
        {
            // Đã bỏ chặn click vào thư mục để cho phép lệnh chạy ngay cả trên nút cha
            // if (e.Model.Data.Properties.IsFolder) return;

            switch (e.MouseEventArgs.Buttons)
            {
                case MouseButtons.Primary:
                    if (e.Model.Data.Properties.LeftMacro.Script != "")
                    {
                        _RunRhinoCommand(e.Model.Data.Properties.LeftMacro.Script);
                    }
                    break;
                case MouseButtons.Alternate:
                    if (e.Model.Data.Properties.RightMacro.Script != "")
                    {
                        _RunRhinoCommand(e.Model.Data.Properties.RightMacro.Script);
                    }
                    break;
            }
        }
        /// <summary>
        /// Chuột rời khỏi một nút
        /// </summary>
        /// <param name="radialMenuControl"></param>
        protected void _RadialControlMouseLeaveButtonHandler(RadialMenuControl radialMenuControl, ButtonMouseEventArgs e)
        {
            Logger.Debug($"Mouse leave button {e.Model.Data.ButtonID}");

            //HACK: Chúng ta không thể rời khỏi một nút nếu chuột đang di chuột qua nút trung tâm. Hack này là vì đôi khi sự kiện "leave" của một nút có thể kích hoạt SAU sự kiện "ENTER" của nút trung tâm
            if (_CenterMenuButton.CurrentButtonState.GetType() != typeof(HoverState))
            {
                _UpdateTooltipBinding(); // Cập nhật tooltip
            }
            if (_ContextMenuForm.Visible) return; // Không cấp focus cho cửa sổ chính Rhino nếu chúng ta đang hiển thị menu ngữ cảnh
        }


        /// <summary>
        /// Thuộc tính nút đã được cập nhật, chế độ kéo đã kết thúc
        /// </summary>
        /// <param name="radialMenuControl"></param>
        /// <param name="eventArgs"></param>
        protected void _RadialControlDragEnterHandler(RadialMenuControl radialMenuControl, ButtonDragDropEventArgs eventArgs)
        {
            if (_AcceptDragDrop(eventArgs))
            {
                if (radialMenuControl.Level.Level < s_maxLevels) radialMenuControl.SelectedButtonID = eventArgs.TargetModel.Data.ButtonID; // Cập nhật lựa chọn ngoại trừ cấp menu cuối cùng (vô ích)
                var newMenuControl = _ShowSubmenu(radialMenuControl, eventArgs.TargetModel); // Mở menu con nếu việc thả được chấp nhận
                if (newMenuControl != null) _HideSubmenu(newMenuControl); // Ẩn các menu con cao hơn đang mở
                _RegisterDragEndHandler(eventArgs.DragEventSourceArgs.Source); // Đăng ký bộ xử lý sự kiện "DragEnd"
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="radialMenuControl"></param>
        /// <param name="eventArgs"></param>
        protected void _RadialControlDragOverHandler(RadialMenuControl radialMenuControl, ButtonDragDropEventArgs eventArgs)
        {
            if (_AcceptDragDrop(eventArgs))
            {
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="radialMenuControl"></param>
        /// <param name="eventArgs"></param>
        private void _RadialControlDragLeaveHandler(RadialMenuControl radialMenuControl, ButtonDragDropEventArgs eventArgs)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="radialMenuControl"></param>
        /// <param name="eventArgs"></param>
        protected void _RadialControlDragDropHandler(RadialMenuControl radialMenuControl, ButtonDragDropEventArgs eventArgs)
        {
            // Cập nhật dữ liệu nút menu
            if (_AcceptDragDrop(eventArgs))
            {
                switch (Utilities.DragDropUtilities.dragSourceType(eventArgs.DragEventSourceArgs.Source))
                {
                    case DragSourceType.rhinoItem:
                        ButtonProperties toolbaritem = Utilities.DragDropUtilities.getDroppedToolbarItem(eventArgs.DragEventSourceArgs);
                        _UpdateDropItem(toolbaritem, eventArgs.TargetModel, radialMenuControl.Level.Level);
                        break;
                    case DragSourceType.radialMenuItem:
                        var guid = eventArgs.DragEventSourceArgs.Data.GetString("MODEL_GUID");
                        var sourceDragModel = ModelController.Instance.Find(guid);
                        if (sourceDragModel != null)
                        {
                            if (sourceDragModel == eventArgs.TargetModel) // Nếu nguồn và đích giống nhau, không làm gì cả
                            {
                                return;
                            }
                            else
                            {
                                if (eventArgs.TargetModel.Parent == sourceDragModel.Parent) // Mục được di chuyển trên cùng một menu con
                                {
                                    // Cập nhật model đích
                                    eventArgs.TargetModel.Data.Properties.Icon = sourceDragModel.Data.Properties.Icon;
                                    eventArgs.TargetModel.Data.Properties.IsFolder = false;
                                    eventArgs.TargetModel.Data.Properties.IsActive = true;
                                    eventArgs.TargetModel.Data.Properties.LeftMacro = sourceDragModel.Data.Properties.LeftMacro;
                                    eventArgs.TargetModel.Data.Properties.RightMacro = sourceDragModel.Data.Properties.RightMacro;
                                    eventArgs.TargetModel.Data.Properties.CommandGUID = sourceDragModel.Data.Properties.CommandGUID;
                                    // Xóa dữ liệu model nguồn
                                    sourceDragModel.Data.Properties.Icon = null;
                                    sourceDragModel.Data.Properties.IsActive = false;
                                    sourceDragModel.Data.Properties.IsFolder = false;
                                    sourceDragModel.Data.Properties.LeftMacro = new Macro();
                                    sourceDragModel.Data.Properties.RightMacro = new Macro();
                                    sourceDragModel.Data.Properties.CommandGUID = Guid.Empty;
                                }
                                else // Mục được di chuyển sang menu con cấp khác
                                {
                                    // Tạo thuộc tính model mới từ Model nguồn
                                    ButtonProperties data = new ButtonProperties(
                                        new Macro(sourceDragModel.Data.Properties.LeftMacro.Script, sourceDragModel.Data.Properties.LeftMacro.Tooltip),
                                        new Macro(sourceDragModel.Data.Properties.RightMacro.Script, sourceDragModel.Data.Properties.RightMacro.Tooltip),
                                        new Icon(sourceDragModel.Data.Properties.Icon.Frames), true, false,
                                        sourceDragModel.Data.Properties.CommandGUID);

                                    // Xóa dữ liệu model nguồn
                                    sourceDragModel.Data.Properties.CommandGUID = Guid.Empty;
                                    sourceDragModel.Data.Properties.Icon = null;
                                    sourceDragModel.Data.Properties.IsActive = false;
                                    sourceDragModel.Data.Properties.IsFolder = false;
                                    sourceDragModel.Data.Properties.LeftMacro = new Macro();
                                    sourceDragModel.Data.Properties.RightMacro = new Macro();

                                    // (Đệ quy) Kiểm tra xem mỗi menu con cha của mục nguồn còn chứa con không. Nếu không, xóa model cha
                                    var parent = sourceDragModel.Parent;
                                    while (parent != null)
                                    {
                                        var sourceModelChildren = ModelController.Instance.GetChildren(parent); var activeIcon = 0;
                                        foreach (var model in sourceModelChildren)
                                        {
                                            if (model.Data.Properties.Icon != null) activeIcon++;
                                        }
                                        if (activeIcon == 0) // Model cha không còn con -> Xóa icon và cờ isFolder cho model cha
                                        {
                                            parent.Data.Properties.Icon = null;
                                            parent.Data.Properties.IsFolder = false;
                                            parent.Data.Properties.IsActive = false;
                                            parent.Data.Properties.CommandGUID = Guid.Empty;
                                        }
                                        parent = parent.Parent;

                                    }
                                    // Xây dựng lại/Cập nhật phân cấp menu con cho mục tiêu thả
                                    _UpdateDropItem(data, eventArgs.TargetModel, radialMenuControl.Level.Level);
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Khi việc kéo control nguồn kết thúc, xóa tất cả các menu con đang mở
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        protected void _RadialControlDragEndHandler(object sender, DragEventArgs eventArgs)
        {
            // Đóng các menu con ngoại trừ menu cấp 1
            foreach (var radialMenuControl in _Controls.Values)
            {
                switch (radialMenuControl.Level.Level)
                {
                    case > 1:
                        radialMenuControl.Show(false);
                        break;
                    default:
                        radialMenuControl.SelectedButtonID = "";
                        break;
                }
            }
            // Dọn dẹp bộ xử lý DragEnd
            if (_Dragsource != null)
            {
                _Dragsource.DragEnd -= _RadialControlDragEndHandler;
                _Dragsource = null;
            }
        }
        /// <summary>
        /// Yêu cầu xóa nút. Xóa các model nút và tất cả các con nếu chúng tồn tại
        /// <para>
        /// Không thực sự xóa các đối tượng model, chỉ xóa giá trị
        /// </para>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        protected void _RadialControlRemoveButton(object sender, ButtonDragDropEventArgs eventArgs)
        {
            _ClearModel(eventArgs.TargetModel);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        protected void _ClearModel(Model model)
        {
            var children = ModelController.Instance.GetChildren(model);
            if (children.Count > 0)
            {
                foreach (var child in children)
                {
                    _ClearModel(child); // Xóa con nếu có
                }
            }
            model.Clear();

            // Cập nhật thuộc tính "isFolder" của model cha tùy thuộc vào việc nó còn ít nhất một nút con hoạt động hay không
            var parent = model.Parent;
            if (parent != null)
            {
                children = ModelController.Instance.GetChildren(parent);
                var isFolder = false;
                foreach (var child in children)
                {
                    if (child.Data.Properties.IsActive) { isFolder = true; break; }
                }
                parent.Data.Properties.IsFolder = isFolder;
            }
        }
        /// <summary>
        /// Bộ xử lý sự kiện hiển thị menu ngữ cảnh
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        protected void _RadialControlContextMenu(object sender, ButtonMouseEventArgs eventArgs)
        {
            if (eventArgs.Model == null) return;
            _ContextMenuForm.Model = eventArgs.Model;
            var location = new Point(eventArgs.ScreenLocation);
            location.X = location.X - 8;
            location.Y = location.Y - 8;
            _ContextMenuForm.Show(location);
        }
        private void _InitCenterButton()
        {
            _CenterMenuButton = new FormCenterButton(new Size(s_defaultInnerRadius * 2, s_defaultInnerRadius * 2));
            _CenterMenuButton.OnButtonMouseEnter += (o, e) =>
            {
                Logger.Debug($"Center button mouse Enter");
                Focus(); // Lấy focus menu
                _CenterMenuButton.SetTooltip(MouseHoverLeftTooltip, MouseHoverRightTooltip);
            };
            _CenterMenuButton.OnButtonMouseLeave += (o, e) =>
            {
                Logger.Debug($"Center button mouse Leave");
                _CenterMenuButton.SetTooltip(); // Xóa tooltip khi chúng ta rời khỏi nút
            };
            _CenterMenuButton.OnButtonClickEvent += (sender, args) =>
            {
                // Chuột phải vào nút đóng để bật/tắt chế độ chỉnh sửa
                if (args.Buttons == MouseButtons.Alternate)
                {
                    _EditMode = !_EditMode;
                    foreach (var control in _Controls.Values)
                    {
                        if (control.Level.Level != 1) control.Show(false); // Ẩn menu con đang mở hiện tại
                        control.SwitchEditMode(_EditMode);
                    }
                }
                else if (args.Buttons == MouseButtons.Primary)
                {
                    _OnCloseClickEvent(sender);
                }
            };

        }
        #endregion
    }
}
