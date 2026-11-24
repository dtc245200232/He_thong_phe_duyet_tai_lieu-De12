using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký dịch vụ SignalR phục vụ truyền thông thời gian thực (Thực hành 3.2)
builder.Services.AddSignalR();

var app = builder.Build();

// ==============================================================================
// Khởi tạo danh sách tài liệu mẫu trong bộ nhớ (In-memory storage)
// ==============================================================================
var documents = new List<DocumentDto>
{
    new(
        Id: 1,
        Title: "Kế hoạch nâng cấp hệ thống máy chủ phòng thực hành",
        Content: "Đề xuất mua sắm bổ sung 20 máy trạm phục vụ học phần Phát triển ứng dụng phân tán.",
        Status: "Draft",
        CreatedBy: "nguyenvana",
        AssignedApprover: "tranvanb",
        CreatedAt: DateTime.UtcNow.AddHours(-5),
        UpdatedAt: null,
        Histories: new List<DocumentHistoryDto>
        {
            new(1, "Created", "nguyenvana", "Khởi tạo tài liệu dự thảo.", DateTime.UtcNow.AddHours(-5))
        }
    ),
    new(
        Id: 2,
        Title: "Báo cáo tiến độ đồ án phân tán tuần 8",
        Content: "Nhóm 12 đã hoàn thành xây dựng Minimal API .NET 10 và Console Client theo đúng yêu cầu giáo trình.",
        Status: "Submitted",
        CreatedBy: "nguyenvana",
        AssignedApprover: "tranvanb",
        CreatedAt: DateTime.UtcNow.AddHours(-3),
        UpdatedAt: DateTime.UtcNow.AddHours(-1),
        Histories: new List<DocumentHistoryDto>
        {
            new(1, "Created", "nguyenvana", "Tạo báo cáo tuần 8.", DateTime.UtcNow.AddHours(-3)),
            new(2, "Submitted", "nguyenvana", "Kính gửi Thầy/Cô duyệt tiến độ bài tập lớn.", DateTime.UtcNow.AddHours(-1))
        }
    ),
    new(
        Id: 3,
        Title: "Kế hoạch tổ chức hội thảo khoa học sinh viên 2026",
        Content: "Hội thảo chuyên đề: Ứng dụng kiến trúc Microservices và Event-Driven trong hệ thống phân tán.",
        Status: "Approved",
        CreatedBy: "levanc",
        AssignedApprover: "tranvanb",
        CreatedAt: DateTime.UtcNow.AddDays(-2),
        UpdatedAt: DateTime.UtcNow.AddDays(-1),
        Histories: new List<DocumentHistoryDto>
        {
            new(1, "Created", "levanc", "Tạo kế hoạch hội thảo.", DateTime.UtcNow.AddDays(-2)),
            new(2, "Submitted", "levanc", "Trình duyệt kế hoạch tổ chức.", DateTime.UtcNow.AddDays(-1).AddHours(-4)),
            new(3, "Approved", "tranvanb", "Đồng ý phê duyệt kế hoạch, triển khai theo đúng dự toán.", DateTime.UtcNow.AddDays(-1))
        }
    )
};

// ==============================================================================
// 1. API Kiểm tra trạng thái và GET danh sách tài liệu
// ==============================================================================

// GET / - Kiểm tra trạng thái máy chủ
app.MapGet("/", () => Results.Ok(new
{
    service = "Document Approval API with SignalR",
    status = "Healthy",
    version = "2.0 (.NET 10 Minimal API & SignalR)",
    signalRHub = "/hubs/approval",
    timestamp = DateTime.UtcNow
}));

// GET /api/documents - Lấy danh sách tài liệu (kèm lọc theo query string)
RouteGroupBuilder documentApi = app.MapGroup("/api/documents");

documentApi.MapGet("/", (string? keyword, string? status, string? createdBy, string? assignedApprover) =>
{
    IEnumerable<DocumentDto> query = documents;

    // Lọc theo keyword trong tiêu đề hoặc nội dung
    if (!string.IsNullOrWhiteSpace(keyword))
    {
        var term = keyword.Trim().ToLower();
        query = query.Where(d => d.Title.ToLower().Contains(term) || d.Content.ToLower().Contains(term));
    }

    // Lọc theo trạng thái
    if (!string.IsNullOrWhiteSpace(status))
    {
        query = query.Where(d => string.Equals(d.Status, status.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    // Lọc theo người tạo
    if (!string.IsNullOrWhiteSpace(createdBy))
    {
        query = query.Where(d => string.Equals(d.CreatedBy, createdBy.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    // Lọc theo người duyệt
    if (!string.IsNullOrWhiteSpace(assignedApprover))
    {
        query = query.Where(d => d.AssignedApprover != null && string.Equals(d.AssignedApprover, assignedApprover.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    return Results.Ok(query.ToList());
});

// GET /api/documents/{id} - Xem chi tiết tài liệu
documentApi.MapGet("/{id:int}", (int id) =>
{
    var doc = documents.FirstOrDefault(d => d.Id == id);
    if (doc is null)
    {
        return Results.NotFound(new ApiError("DOCUMENT_NOT_FOUND", $"Không tìm thấy tài liệu có mã ID: {id}."));
    }
    return Results.Ok(doc);
});

// POST /api/documents - Tạo tài liệu mới (Draft)
documentApi.MapPost("/", async (CreateDocumentRequest request, IHubContext<ApprovalHub> hubContext) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new ApiError("INVALID_DOCUMENT", "Tiêu đề tài liệu không được để trống hoặc chỉ chứa khoảng trắng."));
    }

    if (string.IsNullOrWhiteSpace(request.CreatedBy))
    {
        return Results.BadRequest(new ApiError("INVALID_DOCUMENT", "Người tạo tài liệu không được để trống hoặc chỉ chứa khoảng trắng."));
    }

    var nextId = documents.Count > 0 ? documents.Max(d => d.Id) + 1 : 1;

    var newDoc = new DocumentDto(
        Id: nextId,
        Title: request.Title.Trim(),
        Content: (request.Content ?? string.Empty).Trim(),
        Status: "Draft",
        CreatedBy: request.CreatedBy.Trim(),
        AssignedApprover: string.IsNullOrWhiteSpace(request.AssignedApprover) ? null : request.AssignedApprover.Trim(),
        CreatedAt: DateTime.UtcNow,
        UpdatedAt: null,
        Histories: new List<DocumentHistoryDto>
        {
            new(1, "Created", request.CreatedBy.Trim(), "Khởi tạo tài liệu dự thảo.", DateTime.UtcNow)
        }
    );

    documents.Add(newDoc);

    // Phát thông báo thời gian thực qua SignalR tới tất cả Client
    await hubContext.Clients.All.SendAsync("ReceiveDocumentNotification", newDoc, "Created");

    return Results.Created($"/api/documents/{newDoc.Id}", newDoc);
});

// POST /api/documents/{id}/submit - Gửi phê duyệt
documentApi.MapPost("/{id:int}/submit", async (int id, SubmitDocumentRequest? request, IHubContext<ApprovalHub> hubContext) =>
{
    var doc = documents.FirstOrDefault(d => d.Id == id);
    if (doc is null)
    {
        return Results.NotFound(new ApiError("DOCUMENT_NOT_FOUND", $"Không tìm thấy tài liệu có mã ID: {id}."));
    }

    if (doc.Status != "Draft")
    {
        return Results.BadRequest(new ApiError("INVALID_STATE_TRANSITION",
            $"Chỉ tài liệu ở trạng thái 'Draft' mới có thể gửi phê duyệt. Trạng thái hiện tại: '{doc.Status}'."));
    }

    var performedBy = !string.IsNullOrWhiteSpace(request?.PerformedBy) ? request.PerformedBy.Trim() : doc.CreatedBy;
    var assignedApprover = !string.IsNullOrWhiteSpace(request?.AssignedApprover) ? request.AssignedApprover.Trim() : doc.AssignedApprover;
    var comment = !string.IsNullOrWhiteSpace(request?.Comment) ? request.Comment.Trim() : "Gửi tài liệu vào quy trình phê duyệt.";

    var nextHistoryId = doc.Histories.Count > 0 ? doc.Histories.Max(h => h.Id) + 1 : 1;
    doc.Histories.Add(new DocumentHistoryDto(nextHistoryId, "Submitted", performedBy, comment, DateTime.UtcNow));

    var updatedDoc = doc with
    {
        Status = "Submitted",
        AssignedApprover = assignedApprover,
        UpdatedAt = DateTime.UtcNow
    };

    var index = documents.FindIndex(d => d.Id == id);
    documents[index] = updatedDoc;

    // Phát thông báo thời gian thực tới nhóm Người phê duyệt (Approvers)
    await hubContext.Clients.Group("Approvers").SendAsync("ReceiveDocumentNotification", updatedDoc, "Submitted");
    await hubContext.Clients.All.SendAsync("ReceiveBroadcast", "Hệ Thống", $"Tài liệu mã #{updatedDoc.Id} ('{updatedDoc.Title}') đã được gửi phê duyệt.");

    return Results.Ok(updatedDoc);
});

// POST /api/documents/{id}/approve - Phê duyệt tài liệu
documentApi.MapPost("/{id:int}/approve", async (int id, ReviewDocumentRequest? request, IHubContext<ApprovalHub> hubContext) =>
{
    var doc = documents.FirstOrDefault(d => d.Id == id);
    if (doc is null)
    {
        return Results.NotFound(new ApiError("DOCUMENT_NOT_FOUND", $"Không tìm thấy tài liệu có mã ID: {id}."));
    }

    if (doc.Status != "Submitted")
    {
        return Results.BadRequest(new ApiError("INVALID_STATE_TRANSITION",
            $"Chỉ tài liệu ở trạng thái 'Submitted' mới có thể phê duyệt. Trạng thái hiện tại: '{doc.Status}'."));
    }

    var performedBy = !string.IsNullOrWhiteSpace(request?.PerformedBy) ? request.PerformedBy.Trim() : (doc.AssignedApprover ?? "Approver");
    var comment = !string.IsNullOrWhiteSpace(request?.Comment) ? request.Comment.Trim() : "Đã đồng ý phê duyệt tài liệu.";

    var nextHistoryId = doc.Histories.Count > 0 ? doc.Histories.Max(h => h.Id) + 1 : 1;
    doc.Histories.Add(new DocumentHistoryDto(nextHistoryId, "Approved", performedBy, comment, DateTime.UtcNow));

    var updatedDoc = doc with
    {
        Status = "Approved",
        UpdatedAt = DateTime.UtcNow
    };

    var index = documents.FindIndex(d => d.Id == id);
    documents[index] = updatedDoc;

    // Phát thông báo thời gian thực tới tất cả các client
    await hubContext.Clients.All.SendAsync("ReceiveDocumentNotification", updatedDoc, "Approved");
    await hubContext.Clients.All.SendAsync("ReceiveBroadcast", "Hệ Thống", $"Tài liệu mã #{updatedDoc.Id} đã được '{performedBy}' phê duyệt thành công!");

    return Results.Ok(updatedDoc);
});

// POST /api/documents/{id}/reject - Từ chối tài liệu
documentApi.MapPost("/{id:int}/reject", async (int id, ReviewDocumentRequest? request, IHubContext<ApprovalHub> hubContext) =>
{
    var doc = documents.FirstOrDefault(d => d.Id == id);
    if (doc is null)
    {
        return Results.NotFound(new ApiError("DOCUMENT_NOT_FOUND", $"Không tìm thấy tài liệu có mã ID: {id}."));
    }

    if (doc.Status != "Submitted")
    {
        return Results.BadRequest(new ApiError("INVALID_STATE_TRANSITION",
            $"Chỉ tài liệu ở trạng thái 'Submitted' mới có thể từ chối. Trạng thái hiện tại: '{doc.Status}'."));
    }

    if (string.IsNullOrWhiteSpace(request?.Comment))
    {
        return Results.BadRequest(new ApiError("INVALID_COMMENT", "Bắt buộc phải cung cấp lý do (comment) khi từ chối tài liệu."));
    }

    var performedBy = !string.IsNullOrWhiteSpace(request.PerformedBy) ? request.PerformedBy.Trim() : (doc.AssignedApprover ?? "Approver");
    var comment = request.Comment.Trim();

    var nextHistoryId = doc.Histories.Count > 0 ? doc.Histories.Max(h => h.Id) + 1 : 1;
    doc.Histories.Add(new DocumentHistoryDto(nextHistoryId, "Rejected", performedBy, comment, DateTime.UtcNow));

    var updatedDoc = doc with
    {
        Status = "Rejected",
        UpdatedAt = DateTime.UtcNow
    };

    var index = documents.FindIndex(d => d.Id == id);
    documents[index] = updatedDoc;

    await hubContext.Clients.All.SendAsync("ReceiveDocumentNotification", updatedDoc, "Rejected");
    await hubContext.Clients.All.SendAsync("ReceiveBroadcast", "Hệ Thống", $"Tài liệu mã #{updatedDoc.Id} đã bị từ chối bởi '{performedBy}'. Lý do: {comment}");

    return Results.Ok(updatedDoc);
});

// ==============================================================================
// 2. Đăng ký Tuyến SignalR Hub (Thực hành 3.2)
// ==============================================================================
app.MapHub<ApprovalHub>("/hubs/approval");

// Chạy ứng dụng
app.Run();

// ==============================================================================
// SignalR Hub: Lớp quản lý kết nối và điều hướng sự kiện thời gian thực
// ==============================================================================
public class ApprovalHub : Hub
{
    private readonly ILogger<ApprovalHub> _logger;

    public ApprovalHub(ILogger<ApprovalHub> logger)
    {
        _logger = logger;
    }

    // TN1: Echo - Phản hồi chỉ gửi lại riêng cho máy khách gọi (Caller)
    public async Task Echo(string message)
    {
        _logger.LogInformation("[SignalR Echo] Từ ConnectionId: {ConnectionId} - Nội dung: {Message}", Context.ConnectionId, message);
        await Clients.Caller.SendAsync("ReceiveEcho", Context.ConnectionId, message);
    }

    // TN2: Broadcast - Phát thông điệp tới TẤT CẢ máy khách đang kết nối (All)
    public async Task Broadcast(string sender, string message)
    {
        _logger.LogInformation("[SignalR Broadcast] Từ '{Sender}' ({ConnectionId}): {Message}", sender, Context.ConnectionId, message);
        await Clients.All.SendAsync("ReceiveBroadcast", sender, message);
    }

    // TN3: Quản lý nhóm (Groups)
    public async Task JoinGroup(string groupName)
    {
        groupName = groupName.Trim();
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("[SignalR Group] Client {ConnectionId} đã tham gia nhóm '{GroupName}'", Context.ConnectionId, groupName);
        
        await Clients.Caller.SendAsync("JoinedGroup", groupName, Context.ConnectionId);
        await Clients.Group(groupName).SendAsync("ReceiveGroupNotification", groupName, $"Thành viên [{Context.ConnectionId}] vừa tham gia nhóm '{groupName}'.");
    }

    public async Task LeaveGroup(string groupName)
    {
        groupName = groupName.Trim();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("[SignalR Group] Client {ConnectionId} đã rời nhóm '{GroupName}'", Context.ConnectionId, groupName);
        
        await Clients.Caller.SendAsync("LeftGroup", groupName, Context.ConnectionId);
        await Clients.Group(groupName).SendAsync("ReceiveGroupNotification", groupName, $"Thành viên [{Context.ConnectionId}] đã rời nhóm '{groupName}'.");
    }

    public async Task SendToGroup(string groupName, string sender, string message)
    {
        groupName = groupName.Trim();
        _logger.LogInformation("[SignalR SendToGroup] Nhóm '{GroupName}' từ '{Sender}': {Message}", groupName, sender, message);
        await Clients.Group(groupName).SendAsync("ReceiveGroupMessage", groupName, sender, message);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("[SignalR OnConnected] Client mới kết nối: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("[SignalR OnDisconnected] Client ngắt kết nối: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}

// ==============================================================================
// Khai báo các Record DTOs và ApiError
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
