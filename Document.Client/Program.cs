using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

// ==============================================================================
// HỌC PHẦN: PHÁT TRIỂN ỨNG DỤNG PHÂN TÁN
// THỰC HÀNH 3.2 – A: SIGNALR THỜI GIAN THỰC & ĐỀ TÀI 12 (PHÊ DUYỆT TÀI LIỆU)
// ==============================================================================

var baseUrl = args.Length > 0 ? args[0] : "http://localhost:5100";
var clientName = args.Length > 1 ? args[1] : $"Client-{new Random().Next(100, 999)}";
var mode = args.Length > 2 ? args[2].ToLower() : "auto"; // "auto" hoặc "interactive"

Console.OutputEncoding = System.Text.Encoding.UTF8;
PrintHeader($"MÁY KHÁCH SIGNALR & REST API: [{clientName}]");
Console.WriteLine($"[+] Địa chỉ máy chủ API & Hub: {baseUrl}");
Console.WriteLine($"[+] Chế độ hoạt động        : {(mode == "interactive" ? "Tương tác thủ công (Interactive)" : "Tự động kiểm thử (Automated)")}\n");

using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };

// ==============================================================================
// 1. Khởi tạo và cấu hình kết nối SignalR Hub
// ==============================================================================
var hubUrl = $"{baseUrl}/hubs/approval";
var hubConnection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) }) // TN4: Phục hồi kết nối tự động
    .Build();

// Đăng ký bắt các sự kiện vòng đời kết nối để phục vụ TN4 (Mất kết nối & Tự phục hồi)
hubConnection.Reconnecting += error =>
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"\n[CẢNH BÁO MẤT KẾT NỐI - TN4] Đang mất liên lạc với máy chủ SignalR! Lý do: {error?.Message}");
    Console.WriteLine("[TN4] Đang thử kết nối lại tự động (Reconnecting)... Vui lòng khởi động lại API để kiểm chứng!");
    Console.ResetColor();
    return Task.CompletedTask;
};

hubConnection.Reconnected += connectionId =>
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"\n[PHỤC HỒI THÀNH CÔNG - TN4] Đã kết nối lại thành công tới máy chủ SignalR!");
    Console.WriteLine($"[TN4] ConnectionId mới: {connectionId}\n");
    Console.ResetColor();
    return Task.CompletedTask;
};

hubConnection.Closed += error =>
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n[KẾT NỐI ĐÃ ĐÓNG - TN4] Kết nối SignalR đã bị đóng hoàn toàn! Lỗi: {error?.Message}\n");
    Console.ResetColor();
    return Task.CompletedTask;
};

// ==============================================================================
// 2. Đăng ký nhận các sự kiện thời gian thực từ Server Hub
// ==============================================================================

// TN1: Nhận phản hồi Echo (chỉ Caller nhận)
hubConnection.On<string, string>("ReceiveEcho", (connId, message) =>
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"\n<< [SIGNALR ECHO NHẬN VỀ (TN1)] Từ Hub tới riêng ConnectionId [{connId}]: '{message}'");
    Console.ResetColor();
});

// TN2: Nhận tin nhắn Broadcast (Tất cả All nhận)
hubConnection.On<string, string>("ReceiveBroadcast", (sender, message) =>
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"\n<< [SIGNALR BROADCAST (TN2)] [Phát tới Tất Cả Client] Từ '{sender}': {message}");
    Console.ResetColor();
});

// TN3: Nhận thông báo tham gia/rời nhóm
hubConnection.On<string, string>("ReceiveGroupNotification", (groupName, note) =>
{
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine($"\n<< [SIGNALR GROUP NOTIFICATION (TN3)] Nhóm [{groupName}]: {note}");
    Console.ResetColor();
});

// TN3: Nhận tin nhắn gửi riêng cho nhóm
hubConnection.On<string, string, string>("ReceiveGroupMessage", (groupName, sender, message) =>
{
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.WriteLine($"\n<< [SIGNALR GROUP MESSAGE (TN3)] [Nhóm: {groupName}] Từ '{sender}': {message}");
    Console.ResetColor();
});

// Thông báo sự kiện quy trình phê duyệt tài liệu thời gian thực
hubConnection.On<DocumentDto, string>("ReceiveDocumentNotification", (doc, action) =>
{
    Console.ForegroundColor = ConsoleColor.DarkGreen;
    Console.WriteLine($"\n<< [THÔNG BÁO TÀI LIỆU REALTIME] Thao tác: [{action}] | Mã tài liệu #{doc.Id}: '{doc.Title}' | Trạng thái: {doc.Status}");
    Console.ResetColor();
});

// Bắt đầu kết nối tới Hub
try
{
    await hubConnection.StartAsync();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"[OK] Đã kết nối thành công tới SignalR Hub: {hubUrl}");
    Console.WriteLine($"     Mã ConnectionId của máy khách: {hubConnection.ConnectionId}\n");
    Console.ResetColor();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"[LỖI] Không thể kết nối tới SignalR Hub tại {hubUrl}: {ex.Message}");
    Console.WriteLine("Vui lòng đảm bảo rằng Document.Api đang chạy trên cổng 5100!\n");
    Console.ResetColor();
    return;
}

// ==============================================================================
// 3. Thực thi kịch bản theo chế độ: Tự động (Auto) hoặc Tương tác (Interactive)
// ==============================================================================
if (mode == "interactive")
{
    await RunInteractiveMenuAsync(hubConnection, httpClient, clientName);
}
else
{
    await RunAutomatedTestsAsync(hubConnection, httpClient, clientName);
}

// Giữ ứng dụng tiếp tục lắng nghe các sự kiện thời gian thực
Console.WriteLine("\n[i] Client đang giữ kết nối để tiếp tục nhận thông báo thời gian thực. Bấm Enter để thoát...");
Console.ReadLine();

await hubConnection.StopAsync();
Console.WriteLine("[i] Đã ngắt kết nối an toàn.");

// ==============================================================================
// Kịch bản tự động kiểm thử toàn diện TN1 - TN4 và RESTful API
// ==============================================================================
static async Task RunAutomatedTestsAsync(HubConnection hub, HttpClient http, string name)
{
    // TN1: Kết nối Hub và gọi Echo
    PrintSection("THỬ NGHIỆM 1 (TN1): KẾT NỐI HUB & GỌI PHƯƠNG THỨC ECHO (CALLER ONLY)");
    Console.WriteLine($"[>] Gửi lời gọi Echo: 'Xin chào từ {name} - Yêu cầu phản hồi riêng...'");
    await hub.InvokeAsync("Echo", $"Tin nhắn kiểm thử Echo từ {name}");
    await Task.Delay(500); // Đợi nhận phản hồi

    // TN2: Gửi Broadcast tới tất cả các Client đang kết nối
    PrintSection("THỬ NGHIỆM 2 (TN2): GỬI THÔNG ĐIỆP BROADCAST (TẤT CẢ CLIENT - ALL)");
    Console.WriteLine($"[>] Gửi lời gọi Broadcast: '{name} thông báo: Cuộc họp phê duyệt tài liệu sẽ bắt đầu lúc 14h00.'");
    await hub.InvokeAsync("Broadcast", name, "Cuộc họp phê duyệt tài liệu sẽ bắt đầu lúc 14h00.");
    await Task.Delay(500);

    // TN3: Quản lý nhóm (Groups)
    PrintSection("THỬ NGHIỆM 3 (TN3): QUẢN LÝ NHÓM (GROUPS: JOIN VÀ SEND TO GROUP)");
    Console.WriteLine($"[>] {name} tham gia nhóm 'Approvers' (Người phê duyệt)...");
    await hub.InvokeAsync("JoinGroup", "Approvers");
    await Task.Delay(500);

    Console.WriteLine($"[>] Gửi tin nhắn nội bộ riêng cho nhóm 'Approvers'...");
    await hub.InvokeAsync("SendToGroup", "Approvers", name, "Kính nhờ các anh/chị xem xét hồ sơ mới vừa gửi.");
    await Task.Delay(500);

    // Tích hợp RESTful API & SignalR Notification (Đề tài 12)
    PrintSection("TÍCH HỢP ĐỀ TÀI 12: TẠO MỚI & PHÊ DUYỆT TÀI LIỆU KÈM SIGNALR REALTIME");
    Console.WriteLine($"[>] Gửi yêu cầu REST API tạo mới tài liệu...");
    var newDocReq = new CreateDocumentRequest(
        Title: $"Đề xuất trang bị thiết bị văn phòng - do {name} tạo",
        Content: "Cần mua bổ sung máy in mạng và máy scan tài liệu.",
        CreatedBy: name,
        AssignedApprover: "tranvanb"
    );
    var createResp = await http.PostAsJsonAsync("/api/documents", newDocReq);
    if (createResp.StatusCode == HttpStatusCode.Created)
    {
        var created = await createResp.Content.ReadFromJsonAsync<DocumentDto>();
        Console.WriteLine($"    [REST OK] Tài liệu mới đã tạo: ID={created?.Id} | Tiêu đề: '{created?.Title}'");
        await Task.Delay(500);

        if (created is not null)
        {
            Console.WriteLine($"\n[>] Gửi duyệt tài liệu #{created.Id} (sẽ kích hoạt SignalR thông báo tới nhóm 'Approvers')...");
            await http.PostAsJsonAsync($"/api/documents/{created.Id}/submit", new SubmitDocumentRequest(name, "tranvanb", "Gửi duyệt hồ sơ thiết bị."));
            await Task.Delay(500);

            Console.WriteLine($"\n[>] Người duyệt phê duyệt tài liệu #{created.Id} (sẽ kích hoạt SignalR broadcast tới tất cả client)...");
            await http.PostAsJsonAsync($"/api/documents/{created.Id}/approve", new ReviewDocumentRequest("tranvanb", "Đã phê duyệt mua sắm."));
            await Task.Delay(500);
        }
    }

    // TN4: Hướng dẫn quan sát mất kết nối và tự động phục hồi
    PrintSection("THỬ NGHIỆM 4 (TN4): MẤT KẾT NỐI VÀ TỰ ĐỘNG PHỤC HỒI (RECONNECTING / RECONNECTED)");
    Console.WriteLine("    [HƯỚNG DẪN THỰC NGHIỆM TN4]:");
    Console.WriteLine("    1. Tại Terminal đang chạy Document.Api, nhấn tổ hợp phím [Ctrl + C] để tắt máy chủ.");
    Console.WriteLine("    2. Quan sát cửa sổ này: Sự kiện Reconnecting sẽ lập tức kích hoạt và in cảnh báo vàng.");
    Console.WriteLine("    3. Khởi chạy lại API (dotnet run --project Document.Api --urls http://localhost:5100).");
    Console.WriteLine("    4. Quan sát cửa sổ này: Sự kiện Reconnected sẽ kích hoạt và in thông báo xanh báo phục hồi kết nối thành công!");
}

// ==============================================================================
// Chế độ tương tác thủ công (Interactive Menu) để test giữa nhiều terminal
// ==============================================================================
static async Task RunInteractiveMenuAsync(HubConnection hub, HttpClient http, string name)
{
    while (true)
    {
        Console.WriteLine("\n================== MENU ĐIỀU KHIỂN SIGNALR ==================");
        Console.WriteLine("1. Gửi Echo (TN1 - Chỉ bản thân nhận)");
        Console.WriteLine("2. Gửi Broadcast (TN2 - Tất cả các client nhận)");
        Console.WriteLine("3. Tham gia nhóm 'Approvers' (TN3)");
        Console.WriteLine("4. Tham gia nhóm 'Creators' (TN3)");
        Console.WriteLine("5. Gửi tin nhắn tới nhóm (TN3)");
        Console.WriteLine("6. Rời nhóm hiện tại (TN3)");
        Console.WriteLine("7. Xem danh sách tài liệu hiện có (REST API)");
        Console.WriteLine("0. Thoát chương trình");
        Console.Write("Lựa chọn của bạn [0-7]: ");

        var choice = Console.ReadLine()?.Trim();
        if (choice == "0") break;

        try
        {
            switch (choice)
            {
                case "1":
                    Console.Write("Nhập nội dung Echo: ");
                    var echoMsg = Console.ReadLine() ?? "Echo test";
                    await hub.InvokeAsync("Echo", echoMsg);
                    break;
                case "2":
                    Console.Write("Nhập nội dung Broadcast tới tất cả: ");
                    var bcastMsg = Console.ReadLine() ?? "Broadcast message";
                    await hub.InvokeAsync("Broadcast", name, bcastMsg);
                    break;
                case "3":
                    await hub.InvokeAsync("JoinGroup", "Approvers");
                    Console.WriteLine(">> Đã gửi yêu cầu tham gia nhóm 'Approvers'");
                    break;
                case "4":
                    await hub.InvokeAsync("JoinGroup", "Creators");
                    Console.WriteLine(">> Đã gửi yêu cầu tham gia nhóm 'Creators'");
                    break;
                case "5":
                    Console.Write("Nhập tên nhóm muốn gửi (Approvers/Creators): ");
                    var grp = Console.ReadLine()?.Trim() ?? "Approvers";
                    Console.Write("Nhập nội dung tin nhắn gửi nhóm: ");
                    var grpMsg = Console.ReadLine() ?? "Hello Group";
                    await hub.InvokeAsync("SendToGroup", grp, name, grpMsg);
                    break;
                case "6":
                    Console.Write("Nhập tên nhóm muốn rời: ");
                    var leaveGrp = Console.ReadLine()?.Trim() ?? "Approvers";
                    await hub.InvokeAsync("LeaveGroup", leaveGrp);
                    break;
                case "7":
                    var list = await http.GetFromJsonAsync<List<DocumentDto>>("/api/documents");
                    if (list is not null)
                    {
                        Console.WriteLine($"\n--- Danh sách tài liệu hiện có ({list.Count}) ---");
                        foreach (var d in list)
                        {
                            Console.WriteLine($"ID: {d.Id,-3} | {d.Title,-35} | Trạng thái: {d.Status,-10} | Người tạo: {d.CreatedBy}");
                        }
                    }
                    break;
                default:
                    Console.WriteLine("Lựa chọn không hợp lệ!");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[LỖI THAO TÁC]: {ex.Message}");
            Console.ResetColor();
        }
    }
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

// ==============================================================================
// DTOs
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
