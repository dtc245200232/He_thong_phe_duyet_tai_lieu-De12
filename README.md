# Hệ Thống Phê Duyệt Tài Liệu Phân Tán (Distributed Document Approval System)

**Học phần:** Phát triển ứng dụng phân tán  
**Đề tài:** Đề tài 12  
**Nền tảng & Công nghệ:** .NET 10 (C#), ASP.NET Core Minimal API, RabbitMQ, PostgreSQL, SignalR, Docker & Docker Compose.

---

## 1. Tổng Quan Kiến Trúc
Hệ thống được xây dựng theo mô hình **hướng sự kiện (Event-Driven Architecture)** gồm các thành phần độc lập:
```
Client (Web App) 
  --> Document API (.NET 10 Minimal API) 
  --> PostgreSQL Database 
  --> RabbitMQ (Message Broker) 
  --> Notification Service (.NET 10 Worker + SignalR) 
  --> Client (Realtime Notification)
```

---

## 2. Cấu Trúc Dự Án (Project Structure)
```text
.
├── DistributedDocumentApproval.sln     # Solution quản lý toàn bộ hệ thống
├── docker-compose.yml                 # Triển khai toàn bộ hạ tầng & microservices
├── .env.example                       # Cấu hình biến môi trường mẫu
├── nuget.config                       # Cấu hình nguồn gói NuGet
├── src/
│   ├── BuildingBlocks/
│   │   └── DistributedApp.Shared/     # Thư viện dùng chung (Contracts, Events, Models)
│   │       ├── Events/                # Sự kiện: DocumentSubmittedEvent, DocumentApprovedEvent, ...
│   │       ├── Messaging/             # Constants: Exchange, Queues, RoutingKeys
│   │       └── Models/                # DocumentStatus, DocumentAction, NotificationDto
│   ├── Services/                      # Các dịch vụ phân tán (sẽ phát triển ở Phase 2 & 3)
│   │   ├── DocumentService/           # Dịch vụ CRUD và phê duyệt tài liệu
│   │   └── NotificationService/       # Dịch vụ nhận message RabbitMQ và push SignalR
│   └── Clients/
│       └── WebApp/                    # Giao diện người dùng Web trực quan
└── docs/                              # Báo cáo và tài liệu kỹ thuật
```

---

## 3. Khởi Chạy Hạ Tầng (Phase 1)
Yêu cầu: Đã cài đặt [Docker](https://www.docker.com/) và [.NET 10 SDK](https://dotnet.microsoft.com/).

### Khởi động Database & Message Broker:
```bash
docker compose up -d
```
- **PostgreSQL:** `localhost:5432` (Database: `document_db`, User: `postgres`, Pass: `postgres`)
- **RabbitMQ AMQP:** `localhost:5672`
- **RabbitMQ Management Dashboard:** `http://localhost:15672` (User: `guest`, Pass: `guest`)

### Kiểm tra build Shared Library:
```bash
dotnet build src/BuildingBlocks/DistributedApp.Shared/DistributedApp.Shared.csproj
```
