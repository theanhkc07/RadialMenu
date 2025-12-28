# Rhino Radial menu cho Rhino3D (Chỉ dành cho Windows)
<br/><br/>

## Tính năng
Radial Menu cho Rhinoceros V8.0 trên Windows
Mục đích của plugin này là cung cấp một menu dạng tròn để truy cập nhanh các lệnh thường dùng chỉ với một cú nhấp chuột hoặc phím tắt (phím kích hoạt)
<br>
Menu có thể được cấu hình bằng cách kéo & thả các mục, từ thanh công cụ Rhino hoặc ngay bên trong menu radial để sắp xếp lại các mục.

Menu radial hỗ trợ 3 cấp độ menu con.

Bạn có thể xem video demo trên youtube https://www.youtube.com/watch?v=viOj8lNkz2s

## Cài đặt
   
Có 3 cách để tải và cài đặt plugin.

1. Từ github releases
* Vào mục "releases", tải xuống file .yak binary
* Kéo thả file .yak vào cửa sổ Rhino hoặc copy vào thư mục Plugins của Rhino

<br/><br/>

## Khởi chạy menu
- Trong dòng lệnh Rhino, gõ lệnh "RadialMenu" để mở menu radial.
- Bạn cũng có thể tạo phím tắt Rhino cho lệnh này
- Bạn có thể gán lệnh này cho nút chuột giữa trong cài đặt Rhino
<br/><br/>

## Quản lý icon menu

   1. Vào chế độ chỉnh sửa
      * Nhấp chuột phải vào nút "đóng" (nút ở giữa menu radial)
      
   2. Kéo icon từ thanh công cụ Rhino vào menu radial
      * Nhấp chuột phải vào nút "đóng" ở giữa để vào "chế độ chỉnh sửa"
      * Giữ Shift + Nhấp chuột trái vào icon Rhino để kéo nó
      * Kéo mục Rhino vào các nút trên menu radial
      * Thả chuột và phím Shift
      * Mục đã được thêm vào
      
   3. Di chuyển (Kéo) các mục trong menu radial
      * Giữ Ctrl + Nhấp chuột trái vào một mục menu radial để kéo nó (LƯU Ý: Hiện tại bạn chưa thể di chuyển "mục thư mục")
      * Di chuyển mục trên menu radial
      * Thả chuột và phím Ctrl để xác nhận vị trí thả
   4. Xóa icon
      * Kéo icon ra ngoài "khe chứa" : Nhấp chuột trái (giữ) + Nhấn phím Ctrl (giữ) vào một icon để bắt đầu Kéo
      * **LƯU Ý:** Bạn có thể hủy việc xóa nếu đưa icon trở lại vị trí ban đầu trong menu
   5. Cài đặt phím kích hoạt
      * Bạn có thể cài đặt phím kích hoạt cho mỗi menu. Khi menu đang mở, nếu bạn gõ ký tự kích hoạt (hiển thị trên nút menu), menu sẽ chạy lệnh rhino của nút đó hoặc mở menu con nếu nút đó là "thư mục"
      * Nhấp chuột phải vào một nút, menu ngữ cảnh sẽ mở ra
      * Nhập một ký tự vào trường "Trigger". Ký tự này sẽ là phím kích hoạt cho nút menu đó
<br/><br/>

## Cách sử dụng menu radial
   1. Nhấn phím ESC để đóng bất kỳ menu con nào đang mở. Nếu nhấn ESC khi menu radial đang hiển thị cấp menu đầu tiên, nó sẽ đóng menu radial
   2. Nhấp vào nút đóng ở giữa để đóng menu
   3. Nhấp vào một nút để chạy lệnh
   4. Nhấn phím kích hoạt để
     * Chạy lệnh Rhino (macro chuột trái) nếu nút là lệnh Rhino
     * Mở menu con tiếp theo nếu nút là "thư mục"
<br/><br/>

## Việc cần làm (không theo thứ tự ưu tiên)
   * [ ] Cho phép chọn menu con dạng "vòng tròn đầy đủ" hoặc "vòng tròn phân đoạn"
   * [x] Ẩn menu và trả focus cho cửa sổ rhino khi một nút được nhấp (Chạy lệnh)
   * [x] Lưu cài đặt để giữ cấu hình menu giữa các lần khởi chạy Rhino
   * [x] Thêm khả năng xóa icon khỏi menu
   * [x] Cải thiện hiệu ứng hình ảnh UI khi chuyển menu con (ví dụ: Thêm hiệu ứng mờ dần/hiện dần)
   * [x] Cải thiện tooltip. Bây giờ chúng được hiển thị ở trung tâm menu radial, với chỉ báo nút chuột (trái / phải) 
   * [x] Menu ngữ cảnh để cấu hình thủ công một nút : Chọn icon khác, nhập lệnh Rhino
     * [x] Khả năng đặt phím kích hoạt cho từng nút
     * [x] Khả năng đặt thủ công lệnh Rhino để thực thi cho chuột trái và phải
     * [x] Khả năng đặt thủ công văn bản tooltip Rhino cho "lệnh" trái và phải
     * [x] Khả năng đặt thủ công icon cho nút
     * [ ] Khả năng chọn màu menu
     * Các cải tiến khác sẽ đến sau
   * [ ] Tạo bảng Cài đặt để chọn phím tắt khởi chạy menu radial
   * [x] Tạo bộ cài đặt gói plugin
