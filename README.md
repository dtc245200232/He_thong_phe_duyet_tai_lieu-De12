# HỆ THỐNG PHÊ DUYỆT TÀI LIỆU PHÂN TÁN (DOCUMENT APPROVAL RESTFUL API & CLIENT)

**Học phần:** Phát triển ứng dụng phân tán  
**Nền tảng:** ASP.NET Core Minimal API — .NET 10 (C#)  
**Đề tài:** Đề tài 12 — Quản lý quy trình gửi và phê duyệt tài liệu  
**Mã nguồn:** [GitHub Repository](https://github.com/dtc245200232/He_thong_phe_duyet_tai_lieu-De12.git)

---

## 1. Cấu Trúc Dự Án (Project Structure)
Dự án được tổ chức theo đúng chuẩn hướng dẫn thực hành của học phần:
```text
DocumentApproval/
├── Document.sln                 # Solution quản lý toàn bộ bài tập
├── nuget.config                 # Cấu hình nguồn gói NuGet chính thức
├── .gitignore                   # Loại trừ file tạm, cache, thư mục bin/obj
├── README.md                    # Báo cáo và hướng dẫn thực hành chi tiết
├── Document.Api/                # Dự án Web API (ASP.NET Core Minimal API - .NET 10)
│   ├── Document.Api.csproj
│   └── Program.cs               # Khởi tạo WebApplication, Minimal API endpoints, In-memory List, DTOs
└── Document.Client/             # Dự án Máy khách (Console Client - .NET 10)
    ├── Document.Client.csproj
    └── Program.cs               # HttpClient gọi 6 hoạt động tuần tự (GET, POST, Submit, Approve, Error, Query)
```

---

## 2. Hướng Dẫn Biên Dịch Và Chạy Thử Nghiệm

### Bước 1: Biên dịch toàn bộ Solution
Mở PowerShell tại thư mục gốc của bài làm:
```powershell
dotnet build Document.sln
```
*Đảm bảo không có lỗi biên dịch nào.*

### Bước 2: Khởi chạy Máy chủ API (Terminal 1)
Chạy API lắng nghe tại cổng `http://localhost:5100`:
```powershell
dotnet run --project Document.Api --urls http://localhost:5100
```
- Mở trình duyệt truy cập: [http://localhost:5100/](http://localhost:5100/) để kiểm tra trạng thái máy chủ.
- Xem danh sách tài liệu: [http://localhost:5100/api/documents](http://localhost:5100/api/documents).
- Giữ Terminal này tiếp tục chạy.

### Bước 3: Chạy chương trình Máy khách Console Client (Terminal 2)
Mở cửa sổ PowerShell thứ hai tại thư mục dự án và chạy:
```powershell
dotnet run --project Document.Client -- http://localhost:5100
```
Chương trình Client sẽ tự động thực hiện tuần tự các hoạt động nghiệp vụ và hiển thị kết quả trực quan trên Console.

---

## 3. Bảng Tổng Hợp Kết Quả Kiểm Thử (Bước 14)

| STT | Tình huống kiểm thử | Phương thức & URL | Kết quả quan sát (HTTP Status) | Cấu trúc dữ liệu / Phản hồi |
| :---: | :--- | :--- | :---: | :--- |
| **1** | Kiểm tra sức khỏe API | `GET /` | `200 OK` | JSON `{ service, status = "Healthy", version, timestamp }` |
| **2** | Lấy toàn bộ danh sách | `GET /api/documents` | `200 OK` | Mảng JSON chứa danh sách các `DocumentDto` hiện có. |
| **3** | Xem chi tiết tài liệu hợp lệ | `GET /api/documents/1` | `200 OK` | Chi tiết tài liệu mã 1 kèm lịch sử xử lý `Histories`. |
| **4** | Xem tài liệu không tồn tại | `GET /api/documents/999` | `404 Not Found` | `ApiError` (`Code: DOCUMENT_NOT_FOUND`, `Message`). |
| **5** | Ràng buộc tuyến sai kiểu | `GET /api/documents/abc` | `404 Not Found` *(Route Mismatch)* | Không khớp route constraint `{id:int}`, không đi vào handler. |
| **6** | Tạo mới tài liệu hợp lệ | `POST /api/documents` | `201 Created` | Header `Location: /api/documents/4` và body `DocumentDto` mới sinh. |
| **7** | Tạo tài liệu tiêu đề rỗng | `POST /api/documents` | `400 Bad Request` | `ApiError` (`Code: INVALID_DOCUMENT`, thông báo lỗi). |
| **8** | Gửi duyệt tài liệu (Submit) | `POST /api/documents/4/submit` | `200 OK` | Trạng thái chuyển từ `Draft` $\rightarrow$ `Submitted`, thêm lịch sử. |
| **9** | Phê duyệt tài liệu (Approve) | `POST /api/documents/4/approve`| `200 OK` | Trạng thái chuyển từ `Submitted` $\rightarrow$ `Approved`, thêm lịch sử. |
| **10**| Vi phạm quy trình duyệt | `POST /api/documents/3/approve`| `400 Bad Request` | `ApiError` (`Code: INVALID_STATE_TRANSITION`, báo đã Approved). |
| **11**| Lọc tài liệu theo từ khóa | `GET /api/documents?keyword=báo cáo` | `200 OK` | Danh sách tài liệu có chứa từ khóa "báo cáo". |
| **12**| Lọc tài liệu theo trạng thái | `GET /api/documents?status=Approved` | `200 OK` | Danh sách các tài liệu đã được phê duyệt. |

---

## 4. Trả Lời Các Câu Hỏi Lý Thuyết (Bước 14)

### (a) Vai trò của DTO đầu vào (`CreateDocumentRequest`) và DTO đầu ra (`DocumentDto`)
- **Vì sao Client không được tự chọn Id?**  
  Mã định danh (Id) là khóa chính xác định duy nhất thực thể trong hệ thống. Nếu cho phép Client tự chọn Id, sẽ dễ dẫn đến xung đột khóa (trùng ID giữa các client) hoặc vi phạm tính toàn vẹn dữ liệu. Server là nơi duy nhất có thẩm quyền sinh và quản lý Id một cách tuần tự hoặc định danh duy nhất (Guid).
- **Vì sao DTO đầu vào khác DTO đầu ra?**  
  - **DTO đầu vào (`CreateDocumentRequest`)**: Chỉ chứa các trường dữ liệu mà Client được quyền cung cấp khi khởi tạo (ví dụ: `Title`, `Content`, `CreatedBy`, `AssignedApprover`). DTO này không chứa các trường do hệ thống tự sinh như `Id`, `Status`, `CreatedAt`, hay danh sách lịch sử xử lý `Histories`.  
  - **DTO đầu ra (`DocumentDto`)**: Đại diện cho thông tin tài nguyên đầy đủ trả về cho Client, bao gồm cả trạng thái hiện tại, thời gian tạo, thời gian cập nhật và toàn bộ lịch sử các bước phê duyệt (`Histories`).  
  - Sự tách biệt này giúp bảo vệ dữ liệu (chống over-posting attack), giảm tải băng thông và phân định rõ ràng trách nhiệm giữa client và server.

### (b) Tại sao trả mã 201 Created thay vì 200 OK khi tạo tài nguyên?
- Theo đặc tả chuẩn **RFC 9110 của HTTP/REST**:
  - `200 OK`: Chỉ ra rằng yêu cầu đã được xử lý thành công chung, thường dùng cho thao tác đọc (`GET`) hoặc cập nhật trạng thái (`PUT`, `POST` xử lý action).
  - `201 Created`: Thông báo rằng yêu cầu đã thành công **và dẫn đến việc tạo ra một hoặc nhiều tài nguyên mới trên máy chủ**.
  - Khi trả về `201 Created`, máy chủ thường gửi kèm **header `Location`** chứa URI trực tiếp của tài nguyên vừa được tạo (ví dụ: `Location: /api/documents/4`), giúp máy khách có thể dễ dàng truy xuất lại tài nguyên mới mà không cần phân tích body.

### (c) Ý nghĩa của ràng buộc tuyến `{id:int}`
- Trong ASP.NET Core Minimal API, `{id:int}` là một **Route Constraint (ràng buộc tuyến)**.
- Nó chỉ định rằng đoạn URI tương ứng phải là một số nguyên hợp lệ thì routing engine mới điều hướng yêu cầu vào hàm xử lý đó:
  - Khi người dùng gửi `/api/documents/1`, giá trị `1` là số nguyên hợp lệ nên handler được gọi và trả về kết quả hoặc lỗi 404 nghiệp vụ (`DOCUMENT_NOT_FOUND` nếu không có ID đó).
  - Khi người dùng gửi `/api/documents/abc`, chuỗi `abc` không thỏa mãn kiểu `:int`. Routing engine sẽ từ chối ngay lập tức ở tầng định tuyến và trả về mã lỗi HTTP 404 (Endpoint Not Found), không chuyển tiếp vào hàm xử lý của API, giúp bảo vệ ứng dụng khỏi lỗi parse kiểu dữ liệu và tăng hiệu năng.

### (d) Cách máy khách Console Client xử lý phản hồi lỗi HTTP
- Trong `Document.Client/Program.cs`, Client sử dụng `HttpResponseMessage` để kiểm tra thuộc tính `IsSuccessStatusCode` hoặc so sánh trực tiếp `StatusCode` (ví dụ: `HttpStatusCode.Created`, `HttpStatusCode.BadRequest`, `HttpStatusCode.NotFound`):
  - **Khi thành công (`StatusCode == 200` hoặc `201`)**: Client gọi `ReadFromJsonAsync<DocumentDto>()` để giải tuần tự (deserialize) dữ liệu tài liệu nhận được và tiếp tục quy trình.
  - **Khi thất bại (`StatusCode >= 400`)**: Thay vì bị crash hoặc đọc nhầm kiểu, Client bắt lỗi và giải tuần tự nội dung body sang đối tượng lỗi chuẩn `ApiError` bằng `ReadFromJsonAsync<ApiError>()`. Nhờ đó, Client trích xuất được chính xác mã lỗi nghiệp vụ (`Code`) và thông điệp giải thích (`Message`) để hiển thị cảnh báo cho người dùng một cách rõ ràng.

---

## 5. Vòng Đời Dữ Liệu Trong Bộ Nhớ (Bước 12)
- Dữ liệu được lưu trong danh sách in-memory `List<DocumentDto>` thuộc bộ nhớ tiến trình (process memory) của ứng dụng `Document.Api`.
- **Quan sát:** Khi thêm một tài liệu mới (ví dụ ID = 4), tài liệu này tồn tại và truy vấn được khi API đang chạy.
- **Hiện tượng khi khởi động lại:** Khi dừng máy chủ bằng `Ctrl+C` và chạy lại, danh sách tài liệu được khởi tạo lại về đúng 3 tài liệu mẫu ban đầu; tài liệu ID = 4 thêm trước đó sẽ bị mất.  
- **Kết luận:** Bộ nhớ tiến trình chỉ mang tính tạm thời trong phiên chạy, không có tính lưu trữ bền vững (persistence). Trong các giai đoạn nâng cao tiếp theo, hệ thống có thể kết nối với Cơ sở dữ liệu phân tán (PostgreSQL) và Message Broker (RabbitMQ) để lưu trữ bền vững và chịu lỗi.
