using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

// ==============================================================================
// Bước 6. Chương trình Console Client tương tác với Document Approval API
// ==============================================================================

// 1. Nhận URL API từ tham số dòng lệnh hoặc mặc định http://localhost:5100
var baseUrl = args.Length > 0 ? args[0] : "http://localhost:5100";

Console.OutputEncoding = System.Text.Encoding.UTF8;
PrintHeader("CHƯƠNG TRÌNH KHÁCH (CONSOLE CLIENT) - HỆ THỐNG PHÊ DUYỆT TÀI LIỆU");
Console.WriteLine($"[+] Kết nối tới API Server tại: {baseUrl}\n");

using var client = new HttpClient
{
    BaseAddress = new Uri(baseUrl)
};

try
{
    // --------------------------------------------------------------------------
    // Hoạt động 0: Kiểm tra kết nối máy chủ
    // --------------------------------------------------------------------------
    PrintSection("HOẠT ĐỘNG 0: KIỂM TRA TRẠNG THÁI SERVER (GET /)");
    var healthResponse = await client.GetAsync("/");
    if (healthResponse.IsSuccessStatusCode)
    {
        var healthJson = await healthResponse.Content.ReadAsStringAsync();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[OK] Server đang hoạt động bình thường (HTTP {(int)healthResponse.StatusCode} {healthResponse.StatusCode})");
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

    // --------------------------------------------------------------------------
    // Hoạt động 1: Đọc danh sách tài liệu ban đầu (GET /api/documents)
    // --------------------------------------------------------------------------
    PrintSection("HOẠT ĐỘNG 1: ĐỌC DANH SÁCH TÀI LIỆU (GET /api/documents)");
    var initialDocuments = await client.GetFromJsonAsync<List<DocumentDto>>("/api/documents");
    if (initialDocuments is not null)
    {
        Console.WriteLine($"Tổng số tài liệu hiện có trong hệ thống: {initialDocuments.Count}\n");
        PrintDocumentTable(initialDocuments);
    }

    // --------------------------------------------------------------------------
    // Hoạt động 2: Tạo mới tài liệu dự thảo (POST /api/documents)
    // --------------------------------------------------------------------------
    PrintSection("HOẠT ĐỘNG 2: TẠO MỚI TÀI LIỆU HỢP LỆ (POST /api/documents)");
    var newDocRequest = new CreateDocumentRequest(
        Title: "Đơn xin cấp kinh phí thực tập tốt nghiệp năm 2026",
        Content: "Kính gửi Ban Giám hiệu và Khoa CNTT phê duyệt dự toán kinh phí hỗ trợ đoàn sinh viên thực tập.",
        CreatedBy: "nguyenvana",
        AssignedApprover: "tranvanb"
    );

    Console.WriteLine($"[>] Gửi yêu cầu tạo tài liệu: '{newDocRequest.Title}' - Người tạo: '{newDocRequest.CreatedBy}'");
    var createResponse = await client.PostAsJsonAsync("/api/documents", newDocRequest);

    int createdDocId = 0;
    if (createResponse.StatusCode == HttpStatusCode.Created)
    {
        var location = createResponse.Headers.Location;
        var createdDoc = await createResponse.Content.ReadFromJsonAsync<DocumentDto>();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[OK] Tạo tài liệu thành công! (Mã HTTP: {(int)createResponse.StatusCode} Created)");
        Console.WriteLine($"     Header Location: {location}");
        if (createdDoc is not null)
        {
            createdDocId = createdDoc.Id;
            Console.WriteLine($"     Tài liệu vừa tạo: ID={createdDoc.Id} | Trạng thái: {createdDoc.Status} | Người tạo: {createdDoc.CreatedBy}");
        }
        Console.ResetColor();
    }
    else
    {
        var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[FAILED] Tạo thất bại! Mã: {createResponse.StatusCode} | Lỗi: [{error?.Code}] {error?.Message}");
        Console.ResetColor();
    }

    // --------------------------------------------------------------------------
    // Hoạt động 3: Thao tác quy trình phê duyệt (Gửi duyệt -> Phê duyệt)
    // --------------------------------------------------------------------------
    if (createdDocId > 0)
    {
        PrintSection($"HOẠT ĐỘNG 3: QUY TRÌNH PHÊ DUYỆT TÀI LIỆU VỪA TẠO (ID: {createdDocId})");

        // Bước 3.1: Gửi duyệt (Draft -> Submitted)
        Console.WriteLine($"[>] Bước 3.1: Gửi tài liệu vào quy trình duyệt (POST /api/documents/{createdDocId}/submit)...");
        var submitReq = new SubmitDocumentRequest(
            PerformedBy: "nguyenvana",
            AssignedApprover: "tranvanb",
            Comment: "Em đã hoàn thiện hồ sơ, kính nhờ Thầy phê duyệt giúp em ạ."
        );
        var submitResponse = await client.PostAsJsonAsync($"/api/documents/{createdDocId}/submit", submitReq);
        if (submitResponse.IsSuccessStatusCode)
        {
            var docSubmitted = await submitResponse.Content.ReadFromJsonAsync<DocumentDto>();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[OK] Gửi duyệt thành công! Trạng thái mới: '{docSubmitted?.Status}' | Người duyệt: '{docSubmitted?.AssignedApprover}'");
            Console.ResetColor();
        }

        // Bước 3.2: Người duyệt chấp thuận tài liệu (Submitted -> Approved)
        Console.WriteLine($"\n[>] Bước 3.2: Người duyệt phê duyệt tài liệu (POST /api/documents/{createdDocId}/approve)...");
        var approveReq = new ReviewDocumentRequest(
            PerformedBy: "tranvanb",
            Comment: "Hồ sơ hợp lệ, đồng ý cấp kinh phí theo đề xuất."
        );
        var approveResponse = await client.PostAsJsonAsync($"/api/documents/{createdDocId}/approve", approveReq);
        if (approveResponse.IsSuccessStatusCode)
        {
            var docApproved = await approveResponse.Content.ReadFromJsonAsync<DocumentDto>();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[OK] Phê duyệt thành công! Trạng thái mới: '{docApproved?.Status}'");
            Console.ResetColor();

            // In lịch sử xử lý của tài liệu vừa được duyệt
            Console.WriteLine($"\n--- Lịch sử các bước xử lý (Audit Trail) của tài liệu ID={createdDocId} ---");
            if (docApproved?.Histories is not null)
            {
                foreach (var h in docApproved.Histories)
                {
                    Console.WriteLine($"   * [{h.Timestamp:yyyy-MM-dd HH:mm:ss}] Thao tác: {h.Action,-10} | Người thực hiện: {h.PerformedBy,-12} | Ghi chú: {h.Comment}");
                }
            }
        }
    }

    // --------------------------------------------------------------------------
    // Hoạt động 4: Đọc chi tiết tài liệu theo ID (GET /api/documents/1)
    // --------------------------------------------------------------------------
    PrintSection("HOẠT ĐỘNG 4: XEM CHI TIẾT TÀI LIỆU MÃ 1 (GET /api/documents/1)");
    var doc1Response = await client.GetAsync("/api/documents/1");
    if (doc1Response.IsSuccessStatusCode)
    {
        var doc1 = await doc1Response.Content.ReadFromJsonAsync<DocumentDto>();
        if (doc1 is not null)
        {
            Console.WriteLine($"Mã tài liệu : {doc1.Id}");
            Console.WriteLine($"Tiêu đề     : {doc1.Title}");
            Console.WriteLine($"Nội dung    : {doc1.Content}");
            Console.WriteLine($"Trạng thái  : {doc1.Status}");
            Console.WriteLine($"Người tạo   : {doc1.CreatedBy}");
            Console.WriteLine($"Người duyệt : {doc1.AssignedApprover ?? "Chưa phân công"}");
            Console.WriteLine($"Thời gian   : {doc1.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        }
    }

    // --------------------------------------------------------------------------
    // Hoạt động 5: Kiểm tra phản hồi lỗi và ràng buộc tuyến (Validation & Error Testing)
    // --------------------------------------------------------------------------
    PrintSection("HOẠT ĐỘNG 5: KIỂM THỬ XỬ LÝ LỖI HTTP VÀ RÀNG BUỘC (400, 404)");

    // Tình huống 5.1: Cố tình gửi POST với tiêu đề rỗng
    Console.WriteLine("[>] Tình huống 5.1: Cố tình gửi POST với tiêu đề rỗng...");
    var invalidReq = new CreateDocumentRequest(Title: "", Content: "Nội dung", CreatedBy: "testuser", AssignedApprover: null);
    var invalidResponse = await client.PostAsJsonAsync("/api/documents", invalidReq);
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"    Mã trạng thái nhận được: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
    var err1 = await invalidResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"    Nội dung lỗi ApiError  : Mã=[{err1?.Code}] | Thông điệp: '{err1?.Message}'");
    Console.ResetColor();

    // Tình huống 5.2: Cố tình GET tài liệu không tồn tại (ID 999)
    Console.WriteLine("\n[>] Tình huống 5.2: Cố tình gọi GET /api/documents/999 (ID không tồn tại)...");
    var notFoundResponse = await client.GetAsync("/api/documents/999");
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"    Mã trạng thái nhận được: {(int)notFoundResponse.StatusCode} {notFoundResponse.StatusCode}");
    var err2 = await notFoundResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"    Nội dung lỗi ApiError  : Mã=[{err2?.Code}] | Thông điệp: '{err2?.Message}'");
    Console.ResetColor();

    // Tình huống 5.3: Cố tình gọi phê duyệt lại một tài liệu đã Approved (Vi phạm quy trình)
    Console.WriteLine("\n[>] Tình huống 5.3: Cố tình gọi POST /api/documents/3/approve (Tài liệu 3 đã Approved)...");
    var invalidTransitionResp = await client.PostAsJsonAsync("/api/documents/3/approve", new ReviewDocumentRequest("tranvanb", "Duyệt lại"));
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"    Mã trạng thái nhận được: {(int)invalidTransitionResp.StatusCode} {invalidTransitionResp.StatusCode}");
    var err3 = await invalidTransitionResp.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"    Nội dung lỗi ApiError  : Mã=[{err3?.Code}] | Thông điệp: '{err3?.Message}'");
    Console.ResetColor();

    // --------------------------------------------------------------------------
    // Hoạt động 6: Thực hành mở rộng - Lọc tài liệu theo Query String (Bước 13)
    // --------------------------------------------------------------------------
    PrintSection("HOẠT ĐỘNG 6 (BƯỚC 13 MỞ RỘNG): LỌC TÀI LIỆU THEO QUERY STRING");

    Console.WriteLine("[>] 6.1: Lọc tài liệu có từ khóa 'báo cáo' (GET /api/documents?keyword=báo cáo):");
    var filteredByKeyword = await client.GetFromJsonAsync<List<DocumentDto>>("/api/documents?keyword=b%C3%A1o%20c%C3%A1o");
    if (filteredByKeyword is not null)
    {
        PrintDocumentTable(filteredByKeyword);
    }

    Console.WriteLine("\n[>] 6.2: Lọc tài liệu theo trạng thái 'Approved' (GET /api/documents?status=Approved):");
    var filteredByStatus = await client.GetFromJsonAsync<List<DocumentDto>>("/api/documents?status=Approved");
    if (filteredByStatus is not null)
    {
        PrintDocumentTable(filteredByStatus);
    }

    PrintHeader("HOÀN THÀNH TOÀN BỘ CÁC BƯỚC THỰC HÀNH CỦA BÀI TẬP!");
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
// Khai báo DTOs tương thích với API trong dự án Client (theo Bước 6)
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
