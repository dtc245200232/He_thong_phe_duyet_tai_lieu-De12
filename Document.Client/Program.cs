using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

// ==============================================================================
// HỌC PHẦN: PHÁT TRIỂN ỨNG DỤNG PHÂN TÁN
// THỰC HÀNH 2.2 - PHẦN A: ĐỌC HIỂU VÀ THỬ NGHIỆM
// CHƯƠNG TRÌNH MÁY KHÁCH CONSOLE CLIENT - ĐỀ TÀI 12: PHÊ DUYỆT TÀI LIỆU
// ==============================================================================

var baseUrl = args.Length > 0 ? args[0] : "http://localhost:5100";

Console.OutputEncoding = System.Text.Encoding.UTF8;
PrintHeader("CHƯƠNG TRÌNH KHÁCH (CONSOLE CLIENT) - HỆ THỐNG PHÊ DUYỆT TÀI LIỆU");
Console.WriteLine($"[+] Địa chỉ máy chủ API mục tiêu: {baseUrl}\n");

using var client = new HttpClient
{
    BaseAddress = new Uri(baseUrl)
};

try
{
    // ==========================================================================
    // KIỂM TRA SỨC KHỎE HỆ THỐNG (GET /)
    // ==========================================================================
    PrintSection("KIỂM TRA TRẠNG THÁI MÁY CHỦ (GET /)");
    var healthResponse = await client.GetAsync("/");
    if (healthResponse.IsSuccessStatusCode)
    {
        var healthJson = await healthResponse.Content.ReadAsStringAsync();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[OK] Máy chủ đang hoạt động bình thường (HTTP {(int)healthResponse.StatusCode} {healthResponse.StatusCode})");
        Console.WriteLine($"     Dữ liệu phản hồi: {healthJson}");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERROR] Không thể kết nối tới server. Mã lỗi: {healthResponse.StatusCode}");
        Console.ResetColor();
        return;
    }

    // ==========================================================================
    // THỬ NGHIỆM 1 (TN1): TRUY XUẤT TÀI NGUYÊN (GET Danh Sách, GET Chi Tiết, GET 404)
    // ==========================================================================
    PrintSection("THỬ NGHIỆM 1 (TN1): TRUY XUẤT TÀI NGUYÊN (GET)");

    // 1.1: GET /api/documents - Lấy toàn bộ danh sách
    Console.WriteLine("[>] 1.1: Gọi GET /api/documents để lấy toàn bộ danh sách...");
    var getListResp = await client.GetAsync("/api/documents");
    Console.WriteLine($"    Mã trạng thái HTTP: {(int)getListResp.StatusCode} {getListResp.StatusCode}");
    var initialDocuments = await getListResp.Content.ReadFromJsonAsync<List<DocumentDto>>();
    if (initialDocuments is not null)
    {
        Console.WriteLine($"    Số lượng tài liệu hiện có: {initialDocuments.Count}");
        PrintDocumentTable(initialDocuments);
    }

    // 1.2: GET /api/documents/1 - Chi tiết tài liệu tồn tại
    Console.WriteLine("\n[>] 1.2: Gọi GET /api/documents/1 (Tài nguyên có tồn tại)...");
    var getDoc1Resp = await client.GetAsync("/api/documents/1");
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"    Mã trạng thái HTTP: {(int)getDoc1Resp.StatusCode} {getDoc1Resp.StatusCode}");
    var doc1 = await getDoc1Resp.Content.ReadFromJsonAsync<DocumentDto>();
    if (doc1 is not null)
    {
        Console.WriteLine($"    [Dữ liệu] ID: {doc1.Id} | Tiêu đề: '{doc1.Title}' | Trạng thái: {doc1.Status} | Người tạo: {doc1.CreatedBy}");
    }
    Console.ResetColor();

    // 1.3: GET /api/documents/999 - Chi tiết tài liệu không tồn tại
    Console.WriteLine("\n[>] 1.3: Gọi GET /api/documents/999 (ID không tồn tại)...");
    var getDoc999Resp = await client.GetAsync("/api/documents/999");
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"    Mã trạng thái HTTP: {(int)getDoc999Resp.StatusCode} {getDoc999Resp.StatusCode}");
    var err999 = await getDoc999Resp.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"    [ApiError] Mã lỗi: [{err999?.Code}] | Thông điệp: '{err999?.Message}'");
    Console.ResetColor();
    Console.WriteLine("    => Giải thích: 200 trả về khi tìm thấy tài nguyên; 404 trả về khi ID hợp lệ về kiểu nhưng không tồn tại trong danh sách.");

    // ==========================================================================
    // THỬ NGHIỆM 2 (TN2): TẠO MỚI TÀI NGUYÊN BẰNG POST (201 Created & Header Location)
    // ==========================================================================
    PrintSection("THỬ NGHIỆM 2 (TN2): TẠO MỚI TÀI NGUYÊN BẰNG POST");

    var newDocRequest = new CreateDocumentRequest(
        Title: "Đơn xin cấp kinh phí thực tập tốt nghiệp năm 2026",
        Content: "Kính gửi Ban Giám hiệu và Khoa CNTT phê duyệt dự toán kinh phí hỗ trợ đoàn sinh viên thực tập.",
        CreatedBy: "nguyenvana",
        AssignedApprover: "tranvanb"
    );

    Console.WriteLine($"[>] Gửi POST /api/documents với dữ liệu: '{newDocRequest.Title}'...");
    var createResponse = await client.PostAsJsonAsync("/api/documents", newDocRequest);

    int createdDocId = 0;
    if (createResponse.StatusCode == HttpStatusCode.Created)
    {
        var location = createResponse.Headers.Location;
        var createdDoc = await createResponse.Content.ReadFromJsonAsync<DocumentDto>();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[OK] Tạo thành công! Mã trạng thái HTTP: {(int)createResponse.StatusCode} Created");
        Console.WriteLine($"     Header Location trả về: {location}");
        if (createdDoc is not null)
        {
            createdDocId = createdDoc.Id;
            Console.WriteLine($"     Body tài nguyên mới: ID={createdDoc.Id} | Tiêu đề: '{createdDoc.Title}' | Trạng thái: {createdDoc.Status}");
        }
        Console.ResetColor();

        // GET lại URL của sản phẩm mới qua Location header để kiểm chứng
        if (location is not null)
        {
            Console.WriteLine($"\n[>] Gọi GET lại theo URI trong header Location ({location})...");
            var getNewDocResp = await client.GetAsync(location);
            var reloadedDoc = await getNewDocResp.Content.ReadFromJsonAsync<DocumentDto>();
            Console.WriteLine($"    Xác nhận đọc lại thành công: ID={reloadedDoc?.Id} | Tiêu đề='{reloadedDoc?.Title}'");
        }
    }

    // ==========================================================================
    // THỬ NGHIỆM 3 (TN3): KIỂM TRA RÀNG BUỘC DỮ LIỆU (VALIDATION & TRIM KHOẢNG TRẮNG)
    // ==========================================================================
    PrintSection("THỬ NGHIỆM 3 (TN3): KIỂM TRA DỮ LIỆU (VALIDATION & TRIM)");

    // 3.1: POST với tiêu đề rỗng
    Console.WriteLine("[>] 3.1: Gửi POST với tiêu đề rỗng (Title = \"\")...");
    var emptyTitleReq = new CreateDocumentRequest(Title: "", Content: "Nội dung kiểm thử", CreatedBy: "nguyenvana", AssignedApprover: null);
    var emptyTitleResp = await client.PostAsJsonAsync("/api/documents", emptyTitleReq);
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"    Mã trạng thái HTTP: {(int)emptyTitleResp.StatusCode} {emptyTitleResp.StatusCode}");
    var errEmpty = await emptyTitleResp.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"    [ApiError] Mã: [{errEmpty?.Code}] | Thông điệp: '{errEmpty?.Message}'");
    Console.ResetColor();

    // 3.2: POST với tiêu đề có khoảng trắng thừa đầu và cuối
    Console.WriteLine("\n[>] 3.2: Gửi POST với tiêu đề có khoảng trắng thừa (\"   Đề xuất trang bị bảng vẽ điện tử   \")...");
    var spacesReq = new CreateDocumentRequest(
        Title: "   Đề xuất trang bị bảng vẽ điện tử   ",
        Content: "Trang bị cho phòng thí nghiệm đồ họa.",
        CreatedBy: "   nguyenvana   ",
        AssignedApprover: "tranvanb"
    );
    var spacesResp = await client.PostAsJsonAsync("/api/documents", spacesReq);
    if (spacesResp.StatusCode == HttpStatusCode.Created)
    {
        var spacesDoc = await spacesResp.Content.ReadFromJsonAsync<DocumentDto>();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"    [OK] Tạo thành công tài liệu ID={spacesDoc?.Id}");
        Console.WriteLine($"    Tiêu đề gốc gửi đi     : '{spacesReq.Title}'");
        Console.WriteLine($"    Tiêu đề sau khi server Trim : '{spacesDoc?.Title}'");
        Console.WriteLine($"    Người tạo sau khi server Trim: '{spacesDoc?.CreatedBy}'");
        Console.WriteLine($"    => Kiểm chứng: Máy chủ API đã tự động loại bỏ toàn bộ khoảng trắng thừa bằng hàm .Trim()!");
        Console.ResetColor();
    }

    // ==========================================================================
    // THỬ NGHIỆM 4 (TN4): PHÂN BIỆT LỖI PHÍA CLIENT & HTTP STATUS CODE
    // ==========================================================================
    PrintSection("THỬ NGHIỆM 4 (TN4): PHÂN BIỆT LỖI HTTP 400 VỚI NGOẠI LỆ MẤT KẾT NỐI");
    Console.WriteLine("    [Giải thích so sánh lý thuyết & thực nghiệm]:");
    Console.WriteLine("    1. Lỗi HTTP 400 Bad Request (đã thấy ở TN3.1):");
    Console.WriteLine("       - Kết nối TCP thành công, giao thức HTTP hoạt động bình thường.");
    Console.WriteLine("       - Máy chủ chủ động trả về Status Code 400 cùng đối tượng lỗi ApiError để hướng dẫn client sửa dữ liệu.");
    Console.WriteLine("    2. Ngoại lệ mất kết nối (khi dừng API hoặc sai cổng):");
    Console.WriteLine("       - Không có kết nối TCP nào được thiết lập.");
    Console.WriteLine("       - HttpClient văng ra ngoại lệ ở tầng mạng: System.Net.Http.HttpRequestException");
    Console.WriteLine("       - Hoàn toàn KHÔNG có mã HTTP Status Code nào được sinh ra từ máy chủ.");

    // ==========================================================================
    // MỞ RỘNG ĐỀ TÀI 12: QUY TRÌNH PHÊ DUYỆT & LỌC QUERY STRING
    // ==========================================================================
    PrintSection("MỞ RỘNG (ĐỀ TÀI 12): QUY TRÌNH PHÊ DUYỆT (SUBMIT -> APPROVE)");

    if (createdDocId > 0)
    {
        // Gửi duyệt tài liệu
        Console.WriteLine($"[>] Gửi duyệt tài liệu ID={createdDocId} (POST /api/documents/{createdDocId}/submit)...");
        var submitResponse = await client.PostAsJsonAsync($"/api/documents/{createdDocId}/submit",
            new SubmitDocumentRequest("nguyenvana", "tranvanb", "Kính gửi Thầy phê duyệt dự toán."));
        var submittedDoc = await submitResponse.Content.ReadFromJsonAsync<DocumentDto>();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"    [OK] Trạng thái sau gửi duyệt: '{submittedDoc?.Status}' | Người duyệt: '{submittedDoc?.AssignedApprover}'");
        Console.ResetColor();

        // Phê duyệt tài liệu
        Console.WriteLine($"\n[>] Người duyệt phê duyệt tài liệu ID={createdDocId} (POST /api/documents/{createdDocId}/approve)...");
        var approveResponse = await client.PostAsJsonAsync($"/api/documents/{createdDocId}/approve",
            new ReviewDocumentRequest("tranvanb", "Hồ sơ đầy đủ, phê duyệt dự toán."));
        var approvedDoc = await approveResponse.Content.ReadFromJsonAsync<DocumentDto>();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"    [OK] Trạng thái sau phê duyệt: '{approvedDoc?.Status}'");
        Console.ResetColor();

        // In dòng thời gian lịch sử xử lý (Audit History Timeline)
        Console.WriteLine($"\n--- Lịch sử các bước xử lý (Audit Trail) của tài liệu ID={createdDocId} ---");
        if (approvedDoc?.Histories is not null)
        {
            foreach (var h in approvedDoc.Histories)
            {
                Console.WriteLine($"   * [{h.Timestamp:yyyy-MM-dd HH:mm:ss}] Thao tác: {h.Action,-10} | Người thực hiện: {h.PerformedBy,-12} | Ghi chú: {h.Comment}");
            }
        }
    }

    // Lọc theo Query String (Bước 13 mở rộng)
    PrintSection("MỞ RỘNG (BƯỚC 13): LỌC TÀI LIỆU THEO QUERY STRING");
    Console.WriteLine("[>] Lọc tài liệu theo trạng thái Approved (GET /api/documents?status=Approved):");
    var approvedDocs = await client.GetFromJsonAsync<List<DocumentDto>>("/api/documents?status=Approved");
    if (approvedDocs is not null)
    {
        PrintDocumentTable(approvedDocs);
    }

    PrintHeader("HOÀN THÀNH TOÀN BỘ CÁC MỤC THỰC HÀNH CỦA BÀI TẬP!");
}
catch (HttpRequestException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n[NGOẠI LỆ MẤT KẾT NỐI (TN4)]: Không thể kết nối tới máy chủ API tại {baseUrl}!");
    Console.WriteLine($"Chi tiết ngoại lệ: {ex.Message}");
    Console.WriteLine($"Nguyên nhân: Máy chủ API chưa khởi động hoặc tiến trình đã bị dừng (Ctrl+C).");
    Console.ResetColor();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n[LỖI NGOẠI LỆ]: {ex.Message}");
    Console.ResetColor();
}

// ==============================================================================
// Các hàm tiện ích định dạng hiển thị Console
// ==============================================================================
static void PrintHeader(string title)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("================================================================================");
    Console.WriteLine($"  {title}");
    Console.WriteLine("================================================================================");
    Console.ResetColor();
}

static void PrintSection(string section)
{
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine("\n--------------------------------------------------------------------------------");
    Console.WriteLine($"  {section}");
    Console.WriteLine("--------------------------------------------------------------------------------");
    Console.ResetColor();
}

static void PrintDocumentTable(List<DocumentDto> docs)
{
    Console.WriteLine("{0,-4} | {1,-35} | {2,-12} | {3,-12} | {4,-10}", "ID", "Tiêu đề", "Người tạo", "Người duyệt", "Trạng thái");
    Console.WriteLine(new string('-', 85));
    foreach (var d in docs)
    {
        var title = d.Title.Length > 33 ? d.Title[..30] + "..." : d.Title;
        Console.WriteLine("{0,-4} | {1,-35} | {2,-12} | {3,-12} | {4,-10}",
            d.Id, title, d.CreatedBy, d.AssignedApprover ?? "Chưa có", d.Status);
    }
}

// ==============================================================================
// Khai báo DTOs tương thích với API trong dự án Client
// ==============================================================================
public record DocumentDto(
    int Id,
    string Title,
    string Content,
    string Status,
    string CreatedBy,
    string? AssignedApprover,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<DocumentHistoryDto> Histories
);

public record DocumentHistoryDto(
    int Id,
    string Action,
    string PerformedBy,
    string? Comment,
    DateTime Timestamp
);

public record CreateDocumentRequest(
    string Title,
    string Content,
    string CreatedBy,
    string? AssignedApprover
);

public record SubmitDocumentRequest(
    string? PerformedBy,
    string? AssignedApprover,
    string? Comment
);

public record ReviewDocumentRequest(
    string? PerformedBy,
    string? Comment
);

public record ApiError(
    string Code,
    string Message
);
