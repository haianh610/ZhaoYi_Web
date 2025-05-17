# ZhaoYi - Nền tảng Kết nối Phiên dịch viên và Nhà tuyển dụng 

Link Demo: [ZhaoYi Web](https://zhaoyi-web-476648599554.asia-southeast1.run.app/)


## Giới thiệu

ZhaoYi là một ứng dụng web được phát triển bằng ASP.NET Core MVC, với mục tiêu tạo ra một cầu nối hiệu quả giữa các Phiên dịch viên tự do và Nhà tuyển dụng. Dự án tập trung vào việc cung cấp một giao diện trực quan, dễ sử dụng, đặc biệt tối ưu cho trải nghiệm trên thiết bị di động, giúp cả hai đối tượng người dùng dễ dàng tìm kiếm cơ hội và ứng viên phù hợp.

Ứng dụng cho phép người dùng đăng ký tài khoản với vai trò là Phiên dịch viên hoặc Nhà tuyển dụng, mỗi vai trò sẽ có những tính năng chuyên biệt để đáp ứng nhu cầu cụ thể, từ việc tạo và quản lý hồ sơ, tìm kiếm và đăng tin tuyển dụng, đến việc ứng tuyển và quản lý ứng viên.

## Tính năng chính

ZhaoYi cung cấp một bộ công cụ toàn diện cho cả Phiên dịch viên và Nhà tuyển dụng, giúp quá trình tìm việc và tuyển dụng trở nên nhanh chóng và hiệu quả hơn.

### Tính năng chung:

*   **Đăng ký & Đăng nhập linh hoạt:**
    *   Tạo tài khoản mới hoặc đăng nhập bằng Email/Password.
    *   Hỗ trợ đăng nhập nhanh chóng và tiện lợi thông qua tài khoản Google (Google OAuth 2.0).
*   **Lựa chọn vai trò:** Người dùng có thể chọn vai trò (Phiên dịch viên hoặc Nhà tuyển dụng) sau khi đăng ký để sử dụng các tính năng phù hợp.
*   **Giao diện tối ưu cho di động:** Thiết kế responsive, ưu tiên trải nghiệm người dùng trên các thiết bị di động.


###  Dành cho Phiên Dịch Viên:

*   **Quản lý Hồ sơ chuyên nghiệp:**
    *   Tạo và cập nhật thông tin cá nhân chi tiết.
    *   Thêm và quản lý các kỹ năng chuyên môn.
    *   Trình bày kinh nghiệm làm việc, học vấn.
    *   Giới thiệu các dự án đã tham gia và thành tựu đạt được.
    *   Tải lên ảnh đại diện cá nhân.
*   **Tìm kiếm Việc làm:**
    *   Xem danh sách các bài đăng tuyển dụng mới nhất, bao gồm cả các **việc làm cấp tốc**.
    *   Xem chi tiết thông tin từng tin tuyển dụng.
*   **Ứng tuyển dễ dàng:**
    *   Nộp hồ sơ ứng tuyển trực tiếp trên nền tảng.
    *   Tùy chọn đính kèm CV (file PDF, Word) khi ứng tuyển.
*   **Quản lý Việc làm:**
    *   Lưu lại các tin tuyển dụng yêu thích/quan tâm để xem lại sau.
    *   Theo dõi trạng thái các đơn ứng tuyển đã gửi.


###  Dành cho Nhà Tuyển Dụng:

*   **Quản lý Hồ sơ Nhà tuyển dụng:**
    *   Tạo và cập nhật thông tin công ty/tổ chức.
    *   Tải lên logo hoặc ảnh đại diện của công ty.
*   **Đăng tin Tuyển dụng hiệu quả:**
    *   Tạo mới các bài đăng tuyển dụng với đầy đủ thông tin: tiêu đề, mô tả công việc, yêu cầu, mức lương, địa điểm, số lượng cần tuyển, hạn nộp hồ sơ.
    *   Chỉnh sửa hoặc xóa các tin tuyển dụng đã đăng.
*   **Quản lý Tin đăng:**
    *   Xem danh sách tất cả các tin đã đăng.
    *   Theo dõi số lượt xem, số lượng ứng viên cho mỗi tin.
    *   Cập nhật trạng thái tin đăng.
*   **Quản lý Ứng viên:**
    *   Xem danh sách các ứng viên đã nộp hồ sơ cho từng tin tuyển dụng.
    *   Xem chi tiết hồ sơ của ứng viên, bao gồm CV đính kèm.
    *   Cập nhật trạng thái ứng tuyển của ứng viên.
*   **Tìm kiếm Phiên dịch viên:**
    *   Khả năng tìm kiếm và xem hồ sơ của các phiên dịch viên trên nền tảng.

## Công nghệ sử dụng

*   **Ngôn ngữ lập trình:** C#
*   **Framework & Nền tảng:**
    *   ASP.NET Core MVC 8.0 
    *   Entity Framework Core (Code-First)
*   **Cơ sở dữ liệu:**
    *   Microsoft SQL Server
*   **Giao diện người dùng (Frontend):**
    *   HTML5, CSS3, JavaScript
    *   Razor Views
    *   Bootstrap (hoặc thư viện CSS tương tự nếu có)
    *   jQuery (cho các tương tác AJAX và DOM manipulation)
*   **Xác thực & Phân quyền:**
    *   ASP.NET Core Identity
    *   Google OAuth 2.0 (Đăng nhập bằng Google)
*   **Lưu trữ file:**
    *   Lưu trữ trên server (cho avatar, CV).

*   **Quản lý phiên bản:** Git, GitHub

## Hướng dẫn cài đặt và Chạy thử

1.  **Clone Repository:**
    ```bash
    git clone [URL_REPOSITORY_CUA_BAN]
    cd ZhaoYi_Test2 
    ```
2.  **Mở bằng Visual Studio:** Mở file `.sln` bằng Visual Studio.
3.  **Cấu hình Connection String:**
    *   Mở file `appsettings.json` (hoặc `appsettings.Development.json`).
    *   Cập nhật chuỗi `DefaultConnection` để trỏ tới instance SQL Server của bạn.
    ```json
    "ConnectionStrings": {
      "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ZhaoYiDb;Trusted_Connection=True;MultipleActiveResultSets=true"
    }
    ```
4.  **Database Migrations:**
    *   Mở Package Manager Console trong Visual Studio (`View > Other Windows > Package Manager Console`).
    *   Chọn project chứa `DbContext` (thường là project chính).
    *   Chạy lệnh để áp dụng migrations:
        ```powershell
        Update-Database
        ```

5.  **Cấu hình Google Authentication :**
    *   Truy cập [Google Cloud Console](https://console.cloud.google.com/) và tạo credentials cho OAuth 2.0 client.
    *   Lấy `ClientId` và `ClientSecret`.
    *   Thêm vào User Secrets (chuột phải vào project > Manage User Secrets) hoặc `appsettings.json` (không khuyến khích cho production):
        ```json
        // Trong secrets.json
        "Authentication": {
          "Google": {
            "ClientId": "YOUR_GOOGLE_CLIENT_ID",
            "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
          }
        }
        ```
    *   Đảm bảo cấu hình callback URL chính xác trong Google Cloud Console.
6.  **Build và Chạy:**
    *   Build solution (Ctrl+Shift+B).
    *   Chạy ứng dụng. Ứng dụng sẽ mở trên trình duyệt mặc định.
