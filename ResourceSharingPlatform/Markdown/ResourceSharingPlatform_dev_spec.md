# 地方物資管理平台 ASP.NET Core MVC 開發規格書

## 1. 專案目標

本專案為「地方物資管理平台」，主要用於管理地方據點的物資現況、庫存數量、有效期限、所在地點與轉移紀錄。平台需支援地端開發與部署，初期使用 Visual Studio、ASP.NET Core MVC 與 SQL Server 開發，後續可再轉移至 Azure App Service 與 Azure SQL Database。

本平台不是 MES 系統，而是以地方物資、救災物資、社區物資、據點倉儲等管理情境為主。

---

## 2. 開發環境

### 2.1 開發工具

- Visual Studio 2022
- ASP.NET Core MVC
- .NET 8
- SQL Server
- SQL Server Management Studio SSMS
- Bootstrap 5
- Leaflet.js
- OpenStreetMap

### 2.2 專案建議名稱

```text
LocalSupplyManagement
```

### 2.3 初期部署方式

```text
本機 / 公司內部伺服器
↓
IIS 或 Visual Studio Debug
↓
SQL Server
```

### 2.4 未來雲端部署方向

```text
Azure App Service
↓
ASP.NET Core MVC
↓
Azure SQL Database
```

---

## 3. 系統主要功能

### 3.1 物資管理

可管理各類物資的基本資料與庫存狀態。

功能包含：

- 新增物資
- 編輯物資
- 刪除物資
- 查詢物資
- 依種類查詢
- 依據點查詢
- 顯示目前數量
- 顯示有效期限
- 顯示安全庫存水位
- 判斷是否低於警戒水位
- 判斷是否即將過期

---

### 3.2 據點管理

管理地方物資所在據點。

功能包含：

- 新增據點
- 編輯據點
- 刪除據點
- 設定據點名稱
- 設定地址
- 設定經緯度
- 設定聯絡人
- 設定聯絡電話

據點資料會用於地圖顯示。

---

### 3.3 物資轉移

當物資從一個據點轉移到另一個據點時，系統需更新物資目前所在地與數量，並留下完整異動紀錄。

功能包含：

- 選擇來源據點
- 選擇目標據點
- 選擇物資
- 輸入轉移數量
- 檢查來源數量是否足夠
- 扣除來源據點數量
- 增加目標據點數量
- 建立轉移紀錄
- 保留操作人員與備註

---

### 3.4 地圖總覽

系統需提供一個地圖頁面，類似 Google 地圖的圖標效果，顯示所有物資據點。

功能包含：

- 顯示所有據點地標 Marker
- Marker 顯示在對應經緯度
- 點擊 Marker 後顯示該據點物資現況
- Popup 內容包含：
  - 據點名稱
  - 地址
  - 物資種類數
  - 總物資數量
  - 低庫存物資數量
  - 即期物資數量

建議使用：

```text
Leaflet.js + OpenStreetMap
```

---

### 3.5 戰情總覽 Dashboard

提供即時狀態總覽，協助管理人員快速掌握地方物資狀態。

功能包含：

- 總據點數
- 總物資種類數
- 總物資數量
- 低於安全庫存的物資數
- 即將過期物資數
- 已過期物資數
- 各據點物資統計
- 警戒水位清單
- 即期物資清單

狀態判斷建議：

```text
正常：目前數量 > 安全庫存
警戒：目前數量 <= 安全庫存
即期：有效期限 <= 今日 + 30 天
過期：有效期限 < 今日
```

---

## 4. 系統資料表設計

## 4.1 據點資料表：SupplyLocation

```sql
CREATE TABLE SupplyLocation (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    LocationName NVARCHAR(100) NOT NULL,
    Address NVARCHAR(200) NULL,
    Latitude DECIMAL(10,7) NULL,
    Longitude DECIMAL(10,7) NULL,
    ContactPerson NVARCHAR(50) NULL,
    Phone NVARCHAR(30) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL
);
```

---

## 4.2 物資資料表：SupplyItem

```sql
CREATE TABLE SupplyItem (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Category NVARCHAR(50) NOT NULL,
    ItemName NVARCHAR(100) NOT NULL,
    Quantity INT NOT NULL DEFAULT 0,
    Unit NVARCHAR(20) NULL,
    ExpirationDate DATE NULL,
    LocationId INT NOT NULL,
    SafetyStock INT NOT NULL DEFAULT 0,
    Remark NVARCHAR(300) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL,
    CONSTRAINT FK_SupplyItem_SupplyLocation FOREIGN KEY (LocationId)
        REFERENCES SupplyLocation(Id)
);
```

---

## 4.3 物資轉移紀錄表：SupplyTransferLog

```sql
CREATE TABLE SupplyTransferLog (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SupplyItemId INT NOT NULL,
    FromLocationId INT NOT NULL,
    ToLocationId INT NOT NULL,
    TransferQuantity INT NOT NULL,
    TransferTime DATETIME NOT NULL DEFAULT GETDATE(),
    Operator NVARCHAR(50) NULL,
    Remark NVARCHAR(300) NULL,
    CONSTRAINT FK_TransferLog_SupplyItem FOREIGN KEY (SupplyItemId)
        REFERENCES SupplyItem(Id),
    CONSTRAINT FK_TransferLog_FromLocation FOREIGN KEY (FromLocationId)
        REFERENCES SupplyLocation(Id),
    CONSTRAINT FK_TransferLog_ToLocation FOREIGN KEY (ToLocationId)
        REFERENCES SupplyLocation(Id)
);
```

---

## 4.4 使用者帳號表：UserAccount

```sql
CREATE TABLE UserAccount (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserName NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(300) NOT NULL,
    DisplayName NVARCHAR(50) NULL,
    RoleName NVARCHAR(30) NOT NULL DEFAULT 'User',
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL
);
```

---

## 5. 初始測試資料

```sql
INSERT INTO SupplyLocation (LocationName, Address, Latitude, Longitude, ContactPerson, Phone)
VALUES
(N'第一物資據點', N'雲林縣斗六市', 23.7078, 120.5439, N'王先生', N'05-0000001'),
(N'第二物資據點', N'雲林縣虎尾鎮', 23.7092, 120.4313, N'陳小姐', N'05-0000002'),
(N'第三物資據點', N'雲林縣西螺鎮', 23.8000, 120.4600, N'林先生', N'05-0000003');

INSERT INTO SupplyItem (Category, ItemName, Quantity, Unit, ExpirationDate, LocationId, SafetyStock, Remark)
VALUES
(N'食品', N'飲用水', 500, N'瓶', '2026-12-31', 1, 100, N'箱裝飲用水'),
(N'食品', N'泡麵', 120, N'箱', '2026-08-31', 1, 50, N'緊急糧食'),
(N'醫療', N'急救包', 30, N'組', '2027-01-31', 2, 20, N'基本急救用品'),
(N'防護', N'口罩', 2000, N'片', '2026-06-30', 2, 500, N'一般醫療口罩'),
(N'生活', N'毛毯', 80, N'件', NULL, 3, 30, N'保暖用品');
```

---

## 6. ASP.NET Core MVC 專案結構建議

```text
LocalSupplyManagement
│
├── Controllers
│   ├── HomeController.cs
│   ├── DashboardController.cs
│   ├── SupplyItemController.cs
│   ├── SupplyLocationController.cs
│   ├── SupplyTransferController.cs
│   └── MapController.cs
│
├── Models
│   ├── SupplyItem.cs
│   ├── SupplyLocation.cs
│   ├── SupplyTransferLog.cs
│   ├── UserAccount.cs
│   └── ViewModels
│       ├── DashboardViewModel.cs
│       ├── MapLocationViewModel.cs
│       └── TransferViewModel.cs
│
├── Views
│   ├── Dashboard
│   │   └── Index.cshtml
│   ├── SupplyItem
│   │   ├── Index.cshtml
│   │   ├── Create.cshtml
│   │   ├── Edit.cshtml
│   │   └── Details.cshtml
│   ├── SupplyLocation
│   │   ├── Index.cshtml
│   │   ├── Create.cshtml
│   │   ├── Edit.cshtml
│   │   └── Details.cshtml
│   ├── SupplyTransfer
│   │   ├── Index.cshtml
│   │   └── Create.cshtml
│   ├── Map
│   │   └── Index.cshtml
│   └── Shared
│       └── _Layout.cshtml
│
├── Data
│   └── ApplicationDbContext.cs
│
├── Services
│   ├── DashboardService.cs
│   ├── SupplyItemService.cs
│   ├── SupplyLocationService.cs
│   └── SupplyTransferService.cs
│
└── appsettings.json
```

---

## 7. Model 設計

### 7.1 SupplyLocation.cs

```csharp
public class SupplyLocation
{
    public int Id { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<SupplyItem> SupplyItems { get; set; } = new();
}
```

### 7.2 SupplyItem.cs

```csharp
public class SupplyItem
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Unit { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public int LocationId { get; set; }
    public int SafetyStock { get; set; }
    public string? Remark { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public SupplyLocation? Location { get; set; }
}
```

### 7.3 SupplyTransferLog.cs

```csharp
public class SupplyTransferLog
{
    public int Id { get; set; }
    public int SupplyItemId { get; set; }
    public int FromLocationId { get; set; }
    public int ToLocationId { get; set; }
    public int TransferQuantity { get; set; }
    public DateTime TransferTime { get; set; }
    public string? Operator { get; set; }
    public string? Remark { get; set; }

    public SupplyItem? SupplyItem { get; set; }
    public SupplyLocation? FromLocation { get; set; }
    public SupplyLocation? ToLocation { get; set; }
}
```

---

## 8. DbContext 設計

```csharp
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<SupplyLocation> SupplyLocations { get; set; }
    public DbSet<SupplyItem> SupplyItems { get; set; }
    public DbSet<SupplyTransferLog> SupplyTransferLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SupplyLocation>().ToTable("SupplyLocation");
        modelBuilder.Entity<SupplyItem>().ToTable("SupplyItem");
        modelBuilder.Entity<SupplyTransferLog>().ToTable("SupplyTransferLog");

        modelBuilder.Entity<SupplyTransferLog>()
            .HasOne(x => x.FromLocation)
            .WithMany()
            .HasForeignKey(x => x.FromLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SupplyTransferLog>()
            .HasOne(x => x.ToLocation)
            .WithMany()
            .HasForeignKey(x => x.ToLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

---

## 9. appsettings.json 連線字串

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=LocalSupplyDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

## 10. Program.cs 設定

```csharp
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<SupplyItemService>();
builder.Services.AddScoped<SupplyLocationService>();
builder.Services.AddScoped<SupplyTransferService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
```

---

## 11. Dashboard ViewModel

```csharp
public class DashboardViewModel
{
    public int TotalLocationCount { get; set; }
    public int TotalItemTypeCount { get; set; }
    public int TotalQuantity { get; set; }
    public int LowStockCount { get; set; }
    public int ExpiringSoonCount { get; set; }
    public int ExpiredCount { get; set; }

    public List<SupplyItem> LowStockItems { get; set; } = new();
    public List<SupplyItem> ExpiringSoonItems { get; set; } = new();
    public List<LocationSummaryItem> LocationSummaries { get; set; } = new();
}

public class LocationSummaryItem
{
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public int ItemTypeCount { get; set; }
    public int TotalQuantity { get; set; }
    public int LowStockCount { get; set; }
}
```

---

## 12. DashboardService

```csharp
using Microsoft.EntityFrameworkCore;

public class DashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardViewModel> GetDashboardAsync()
    {
        var today = DateTime.Today;
        var expiringDate = today.AddDays(30);

        var items = await _context.SupplyItems
            .Include(x => x.Location)
            .Where(x => x.IsActive)
            .ToListAsync();

        var locations = await _context.SupplyLocations
            .Where(x => x.IsActive)
            .ToListAsync();

        return new DashboardViewModel
        {
            TotalLocationCount = locations.Count,
            TotalItemTypeCount = items.Count,
            TotalQuantity = items.Sum(x => x.Quantity),
            LowStockCount = items.Count(x => x.Quantity <= x.SafetyStock),
            ExpiringSoonCount = items.Count(x => x.ExpirationDate != null && x.ExpirationDate >= today && x.ExpirationDate <= expiringDate),
            ExpiredCount = items.Count(x => x.ExpirationDate != null && x.ExpirationDate < today),
            LowStockItems = items.Where(x => x.Quantity <= x.SafetyStock).ToList(),
            ExpiringSoonItems = items.Where(x => x.ExpirationDate != null && x.ExpirationDate >= today && x.ExpirationDate <= expiringDate).ToList(),
            LocationSummaries = locations.Select(l => new LocationSummaryItem
            {
                LocationId = l.Id,
                LocationName = l.LocationName,
                ItemTypeCount = items.Count(i => i.LocationId == l.Id),
                TotalQuantity = items.Where(i => i.LocationId == l.Id).Sum(i => i.Quantity),
                LowStockCount = items.Count(i => i.LocationId == l.Id && i.Quantity <= i.SafetyStock)
            }).ToList()
        };
    }
}
```

---

## 13. DashboardController

```csharp
using Microsoft.AspNetCore.Mvc;

public class DashboardController : Controller
{
    private readonly DashboardService _dashboardService;

    public DashboardController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index()
    {
        var model = await _dashboardService.GetDashboardAsync();
        return View(model);
    }
}
```

---

## 14. Dashboard 頁面設計 Index.cshtml

```html
@model DashboardViewModel

<div class="container-fluid mt-4">
    <h2 class="mb-4">地方物資戰情總覽</h2>

    <div class="row g-3 mb-4">
        <div class="col-md-2">
            <div class="card shadow-sm border-0">
                <div class="card-body">
                    <h6>據點數</h6>
                    <h2>@Model.TotalLocationCount</h2>
                </div>
            </div>
        </div>
        <div class="col-md-2">
            <div class="card shadow-sm border-0">
                <div class="card-body">
                    <h6>物資種類</h6>
                    <h2>@Model.TotalItemTypeCount</h2>
                </div>
            </div>
        </div>
        <div class="col-md-2">
            <div class="card shadow-sm border-0">
                <div class="card-body">
                    <h6>總數量</h6>
                    <h2>@Model.TotalQuantity</h2>
                </div>
            </div>
        </div>
        <div class="col-md-2">
            <div class="card shadow-sm border-danger">
                <div class="card-body text-danger">
                    <h6>低庫存</h6>
                    <h2>@Model.LowStockCount</h2>
                </div>
            </div>
        </div>
        <div class="col-md-2">
            <div class="card shadow-sm border-warning">
                <div class="card-body text-warning">
                    <h6>即將過期</h6>
                    <h2>@Model.ExpiringSoonCount</h2>
                </div>
            </div>
        </div>
        <div class="col-md-2">
            <div class="card shadow-sm border-dark">
                <div class="card-body text-dark">
                    <h6>已過期</h6>
                    <h2>@Model.ExpiredCount</h2>
                </div>
            </div>
        </div>
    </div>

    <div class="row">
        <div class="col-md-6">
            <div class="card shadow-sm">
                <div class="card-header bg-danger text-white">低於警戒水位物資</div>
                <div class="card-body">
                    <table class="table table-bordered table-hover">
                        <thead>
                            <tr>
                                <th>據點</th>
                                <th>物資</th>
                                <th>目前數量</th>
                                <th>安全庫存</th>
                            </tr>
                        </thead>
                        <tbody>
                        @foreach (var item in Model.LowStockItems)
                        {
                            <tr>
                                <td>@item.Location?.LocationName</td>
                                <td>@item.ItemName</td>
                                <td>@item.Quantity</td>
                                <td>@item.SafetyStock</td>
                            </tr>
                        }
                        </tbody>
                    </table>
                </div>
            </div>
        </div>

        <div class="col-md-6">
            <div class="card shadow-sm">
                <div class="card-header bg-warning">即將過期物資</div>
                <div class="card-body">
                    <table class="table table-bordered table-hover">
                        <thead>
                            <tr>
                                <th>據點</th>
                                <th>物資</th>
                                <th>數量</th>
                                <th>有效期限</th>
                            </tr>
                        </thead>
                        <tbody>
                        @foreach (var item in Model.ExpiringSoonItems)
                        {
                            <tr>
                                <td>@item.Location?.LocationName</td>
                                <td>@item.ItemName</td>
                                <td>@item.Quantity</td>
                                <td>@item.ExpirationDate?.ToString("yyyy-MM-dd")</td>
                            </tr>
                        }
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </div>
</div>
```

---

## 15. 地圖 ViewModel

```csharp
public class MapLocationViewModel
{
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int ItemTypeCount { get; set; }
    public int TotalQuantity { get; set; }
    public int LowStockCount { get; set; }
    public int ExpiringSoonCount { get; set; }
}
```

---

## 16. MapController

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class MapController : Controller
{
    private readonly ApplicationDbContext _context;

    public MapController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetLocations()
    {
        var today = DateTime.Today;
        var expiringDate = today.AddDays(30);

        var locations = await _context.SupplyLocations
            .Where(x => x.IsActive && x.Latitude != null && x.Longitude != null)
            .ToListAsync();

        var items = await _context.SupplyItems
            .Where(x => x.IsActive)
            .ToListAsync();

        var result = locations.Select(l => new MapLocationViewModel
        {
            LocationId = l.Id,
            LocationName = l.LocationName,
            Address = l.Address,
            Latitude = l.Latitude,
            Longitude = l.Longitude,
            ItemTypeCount = items.Count(i => i.LocationId == l.Id),
            TotalQuantity = items.Where(i => i.LocationId == l.Id).Sum(i => i.Quantity),
            LowStockCount = items.Count(i => i.LocationId == l.Id && i.Quantity <= i.SafetyStock),
            ExpiringSoonCount = items.Count(i => i.LocationId == l.Id && i.ExpirationDate != null && i.ExpirationDate >= today && i.ExpirationDate <= expiringDate)
        }).ToList();

        return Json(result);
    }
}
```

---

## 17. 地圖頁面 Map/Index.cshtml

```html
@{
    ViewData["Title"] = "據點地圖總覽";
}

<link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
<script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>

<div class="container-fluid mt-4">
    <h2 class="mb-3">據點地圖總覽</h2>
    <div id="map" style="height: 700px; border-radius: 12px;"></div>
</div>

<script>
    const map = L.map('map').setView([23.7078, 120.5439], 11);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap'
    }).addTo(map);

    fetch('/Map/GetLocations')
        .then(response => response.json())
        .then(data => {
            data.forEach(location => {
                const popupContent = `
                    <strong>${location.locationName}</strong><br/>
                    地址：${location.address ?? ''}<br/>
                    物資種類：${location.itemTypeCount}<br/>
                    總物資數量：${location.totalQuantity}<br/>
                    低庫存項目：${location.lowStockCount}<br/>
                    即期物資：${location.expiringSoonCount}<br/>
                    <a href="/SupplyItem?locationId=${location.locationId}">查看物資明細</a>
                `;

                L.marker([location.latitude, location.longitude])
                    .addTo(map)
                    .bindPopup(popupContent);
            });
        });
</script>
```

---

## 18. 物資轉移 ViewModel

```csharp
public class TransferViewModel
{
    public int SupplyItemId { get; set; }
    public int FromLocationId { get; set; }
    public int ToLocationId { get; set; }
    public int TransferQuantity { get; set; }
    public string? Operator { get; set; }
    public string? Remark { get; set; }
}
```

---

## 19. 物資轉移 Service

```csharp
using Microsoft.EntityFrameworkCore;

public class SupplyTransferService
{
    private readonly ApplicationDbContext _context;

    public SupplyTransferService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message)> TransferAsync(TransferViewModel model)
    {
        if (model.TransferQuantity <= 0)
        {
            return (false, "轉移數量必須大於 0");
        }

        if (model.FromLocationId == model.ToLocationId)
        {
            return (false, "來源據點與目標據點不可相同");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var sourceItem = await _context.SupplyItems
                .FirstOrDefaultAsync(x => x.Id == model.SupplyItemId && x.LocationId == model.FromLocationId && x.IsActive);

            if (sourceItem == null)
            {
                return (false, "找不到來源物資");
            }

            if (sourceItem.Quantity < model.TransferQuantity)
            {
                return (false, "來源物資數量不足");
            }

            sourceItem.Quantity -= model.TransferQuantity;
            sourceItem.UpdatedAt = DateTime.Now;

            var targetItem = await _context.SupplyItems
                .FirstOrDefaultAsync(x =>
                    x.ItemName == sourceItem.ItemName &&
                    x.Category == sourceItem.Category &&
                    x.LocationId == model.ToLocationId &&
                    x.ExpirationDate == sourceItem.ExpirationDate &&
                    x.IsActive);

            if (targetItem == null)
            {
                targetItem = new SupplyItem
                {
                    Category = sourceItem.Category,
                    ItemName = sourceItem.ItemName,
                    Quantity = model.TransferQuantity,
                    Unit = sourceItem.Unit,
                    ExpirationDate = sourceItem.ExpirationDate,
                    LocationId = model.ToLocationId,
                    SafetyStock = sourceItem.SafetyStock,
                    Remark = sourceItem.Remark,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.SupplyItems.Add(targetItem);
            }
            else
            {
                targetItem.Quantity += model.TransferQuantity;
                targetItem.UpdatedAt = DateTime.Now;
            }

            var log = new SupplyTransferLog
            {
                SupplyItemId = sourceItem.Id,
                FromLocationId = model.FromLocationId,
                ToLocationId = model.ToLocationId,
                TransferQuantity = model.TransferQuantity,
                TransferTime = DateTime.Now,
                Operator = model.Operator,
                Remark = model.Remark
            };

            _context.SupplyTransferLogs.Add(log);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "物資轉移完成");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, "轉移失敗：" + ex.Message);
        }
    }
}
```

---

## 20. 物資轉移 Controller

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

public class SupplyTransferController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly SupplyTransferService _transferService;

    public SupplyTransferController(ApplicationDbContext context, SupplyTransferService transferService)
    {
        _context = context;
        _transferService = transferService;
    }

    public async Task<IActionResult> Create(int? supplyItemId)
    {
        ViewBag.Items = new SelectList(await _context.SupplyItems.Where(x => x.IsActive).ToListAsync(), "Id", "ItemName", supplyItemId);
        ViewBag.Locations = new SelectList(await _context.SupplyLocations.Where(x => x.IsActive).ToListAsync(), "Id", "LocationName");
        return View(new TransferViewModel { SupplyItemId = supplyItemId ?? 0 });
    }

    [HttpPost]
    public async Task<IActionResult> Create(TransferViewModel model)
    {
        var result = await _transferService.TransferAsync(model);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Index", "SupplyItem");
        }

        TempData["ErrorMessage"] = result.Message;
        ViewBag.Items = new SelectList(await _context.SupplyItems.Where(x => x.IsActive).ToListAsync(), "Id", "ItemName", model.SupplyItemId);
        ViewBag.Locations = new SelectList(await _context.SupplyLocations.Where(x => x.IsActive).ToListAsync(), "Id", "LocationName");
        return View(model);
    }
}
```

---

## 21. 物資轉移頁面 SupplyTransfer/Create.cshtml

```html
@model TransferViewModel

<div class="container mt-4">
    <h2>物資轉移</h2>

    @if (TempData["ErrorMessage"] != null)
    {
        <div class="alert alert-danger">@TempData["ErrorMessage"]</div>
    }

    <form asp-action="Create" method="post">
        <div class="mb-3">
            <label class="form-label">物資</label>
            <select asp-for="SupplyItemId" asp-items="ViewBag.Items" class="form-select"></select>
        </div>

        <div class="mb-3">
            <label class="form-label">來源據點</label>
            <select asp-for="FromLocationId" asp-items="ViewBag.Locations" class="form-select"></select>
        </div>

        <div class="mb-3">
            <label class="form-label">目標據點</label>
            <select asp-for="ToLocationId" asp-items="ViewBag.Locations" class="form-select"></select>
        </div>

        <div class="mb-3">
            <label class="form-label">轉移數量</label>
            <input asp-for="TransferQuantity" class="form-control" type="number" min="1" />
        </div>

        <div class="mb-3">
            <label class="form-label">操作人員</label>
            <input asp-for="Operator" class="form-control" />
        </div>

        <div class="mb-3">
            <label class="form-label">備註</label>
            <textarea asp-for="Remark" class="form-control"></textarea>
        </div>

        <button type="submit" class="btn btn-primary">確認轉移</button>
        <a href="/SupplyItem" class="btn btn-secondary">返回</a>
    </form>
</div>
```

---

## 22. 物資管理頁面欄位建議

SupplyItem Index 頁面應顯示：

```text
種類
物資名稱
數量
單位
有效期限
目前據點
安全庫存
狀態
操作
```

狀態顯示邏輯：

```csharp
if (item.Quantity <= item.SafetyStock)
{
    狀態 = "低庫存";
}
else if (item.ExpirationDate != null && item.ExpirationDate < DateTime.Today)
{
    狀態 = "已過期";
}
else if (item.ExpirationDate != null && item.ExpirationDate <= DateTime.Today.AddDays(30))
{
    狀態 = "即將過期";
}
else
{
    狀態 = "正常";
}
```

---

## 23. 共用 Layout 導覽列建議

`Views/Shared/_Layout.cshtml` 導覽列包含：

```text
地方物資管理平台
- 戰情總覽
- 據點地圖
- 物資管理
- 據點管理
- 物資轉移
- 轉移紀錄
```

---

## 24. UI 風格建議

整體風格建議：

- 使用 Bootstrap 5
- Dashboard 使用卡片式統計
- 警戒水位用紅色
- 即期物資用黃色
- 正常狀態用綠色
- 地圖頁面使用滿版高度
- 表格支援查詢與篩選

狀態顏色建議：

```text
正常：綠色 badge
低庫存：紅色 badge
即將過期：黃色 badge
已過期：黑色或深紅色 badge
```

---

## 25. 開發順序建議

### 第一階段：基礎資料建置

1. 建立 SQL Server 資料庫 `LocalSupplyDB`
2. 建立資料表
3. 建立 ASP.NET Core MVC 專案
4. 設定 EF Core 與 SQL Server 連線
5. 建立 SupplyLocation CRUD
6. 建立 SupplyItem CRUD

### 第二階段：核心功能

1. 建立物資轉移功能
2. 建立轉移紀錄查詢
3. 建立安全庫存判斷
4. 建立即期與過期判斷

### 第三階段：視覺化功能

1. 建立 Dashboard 戰情總覽
2. 建立 Leaflet 據點地圖
3. 點擊地標顯示據點物資摘要
4. 地圖 Popup 連結至物資明細

### 第四階段：權限與上線準備

1. 建立登入頁面
2. 加入角色權限
3. 加入操作紀錄
4. 加入資料備份機制
5. 準備 IIS 或 Azure 部署

---

## 26. 未來上 Azure 注意事項

未來若要上 Azure，建議調整如下：

```text
SQL Server
→ Azure SQL Database

IIS / 本機
→ Azure App Service

appsettings.json 連線字串
→ Azure App Service Configuration

本機檔案儲存
→ Azure Blob Storage
```

注意：

- 不要把正式資料庫密碼寫死在程式碼
- Azure 上應使用 HTTPS
- 需設定備份機制
- 需設定 App Service 環境變數
- 若有檔案上傳，建議改用 Blob Storage

---

## 27. Copilot Agent 開發指令建議

可以將以下指令貼給 GitHub Copilot Agent：

```text
請依據本 MD 規格，使用 ASP.NET Core MVC + .NET 8 + Entity Framework Core + SQL Server 開發地方物資管理平台。

請完成以下項目：
1. 建立 Models：SupplyLocation、SupplyItem、SupplyTransferLog、UserAccount。
2. 建立 ApplicationDbContext 並設定資料表關聯。
3. 建立 SupplyLocation CRUD。
4. 建立 SupplyItem CRUD，並支援依據點與種類查詢。
5. 建立 SupplyTransferService，處理物資跨據點轉移邏輯，並使用 Transaction 確保資料一致性。
6. 建立 SupplyTransferController 與 Create 頁面。
7. 建立 DashboardController、DashboardService、DashboardViewModel。
8. 建立 Dashboard/Index.cshtml，顯示總據點數、物資總數、低庫存、即期物資與過期物資。
9. 建立 MapController，提供 /Map/GetLocations JSON API。
10. 建立 Map/Index.cshtml，使用 Leaflet.js 與 OpenStreetMap 顯示據點 Marker，點擊 Marker 顯示物資摘要。
11. 使用 Bootstrap 5 美化 UI。
12. 將預設首頁改為 Dashboard/Index。
13. 程式需可在 Visual Studio 2022 直接執行。
14. 資料庫使用 SQL Server，連線字串放在 appsettings.json。
```

---

## 28. 驗收項目

完成後需確認：

- 可以新增、修改、刪除據點
- 可以新增、修改、刪除物資
- 物資可設定所在據點
- 物資可設定有效期限
- 物資可設定安全庫存
- 可以從 A 據點轉移物資到 B 據點
- 轉移後來源數量會減少
- 轉移後目標據點數量會增加
- 系統會建立轉移紀錄
- Dashboard 可以顯示低庫存物資
- Dashboard 可以顯示即期物資
- Dashboard 可以顯示已過期物資
- 地圖可以顯示據點 Marker
- 點擊 Marker 可以看到據點物資摘要

---

## 29. 後續可擴充功能

未來可增加：

- QR Code 盤點
- 條碼掃描入庫
- 物資領用紀錄
- 物資報廢流程
- 庫存異常通知
- LINE Notify 或 Email 通知
- 權限分級
- 操作紀錄 Audit Log
- Excel 匯入物資資料
- Excel 匯出庫存清單
- 手機版 RWD 介面
- Azure 雲端部署

---

## 30. 功能擴充：物資規格／圖片、分類管理、出庫即期警示、批次轉移與到貨確認

本節記錄第 4 階段之後新增的功能擴充。

### 30.1 物資（SupplyItem）新增欄位

```sql
ALTER TABLE SupplyItem ADD Specification NVARCHAR(200) NULL;      -- 規格
ALTER TABLE SupplyItem ADD ImagePath NVARCHAR(300) NULL;          -- 物資圖片相對路徑
ALTER TABLE SupplyItem ADD StockType NVARCHAR(20) NOT NULL DEFAULT 'HasExpiry';
```

- `StockType` 分類：`NoExpiry`（無效期物資）／`HasExpiry`（有效期物資）／`Frozen`（冷凍食品），對應 `Models/StockTypes.cs`
- 新增/編輯物資表單以大按鈕單選分類，選擇「無效期物資」時「有效期限」欄位自動隱藏且非必填；其餘兩類必填
- 物資圖片上傳後存放在專案資料夾內的 `wwwroot/uploads/items/`，透過既有的 `UseStaticFiles()` 直接以 `/uploads/items/xxx.jpg` 存取，檔名以 GUID 產生，副檔名僅允許 jpg/jpeg/png/webp，大小上限 5MB
- 物資管理 Index 頁新增「分類」快速切換鈕（全部／無效期物資／有效期物資／冷凍食品）

### 30.2 出庫即期優先與警示

- 出庫（`SupplyOutboundController`）選單排序改為「有效期限由近到遠優先，無期限排最後」，並在選單文字加註「⚠即將過期／已過期」
- 出庫「新增出庫」「出庫紀錄」頁面上方新增即期／已過期物資警示區塊，列出物資、據點、庫存、有效期限與狀態，可直接點擊帶入出庫表單

### 30.3 物資轉移（SupplyTransferLog）：批次轉移與到貨確認

```sql
ALTER TABLE SupplyTransferLog ADD BatchId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID();
ALTER TABLE SupplyTransferLog ADD Status NVARCHAR(20) NOT NULL DEFAULT 'Pending';
ALTER TABLE SupplyTransferLog ADD ConfirmedBy NVARCHAR(50) NULL;
ALTER TABLE SupplyTransferLog ADD ConfirmedAt DATETIME NULL;
```

**多品項轉移**：「物資轉移」表單改為可重複新增列（品項＋數量），同一次送出的所有品項共用同一個 `BatchId`（GUID），僅作為「這些是同一次送出」的參考標記，不影響確認/取消的操作範圍。

**到貨確認狀態機**（對應 `Models/TransferStatuses.cs`）：

```text
Pending（待確認）→ Confirmed（已確認）
                 → Cancelled（已取消）
```

1. **建立轉移**：立即扣減來源據點庫存，寫入 `Pending` 狀態紀錄（每個品項各自一筆）；目標據點庫存**尚未**增加
2. **確認送達**：管理人員／幹部於「轉移紀錄」對**單一品項**點擊「確認送達」，才將該筆數量加入目標據點庫存，並記錄 `ConfirmedBy`／`ConfirmedAt`
3. **取消轉移**：針對單一品項把數量退回來源據點，記錄取消者與時間

同一批（`BatchId`）送出的多個品項**各自獨立確認／取消**，例如同一批有 A、B 兩項物資，可以先確認 A 已送達、B 仍待確認或直接取消，兩者互不影響。既有資料庫升級時，舊的轉移紀錄（在此機制上線前建立）會一次性回填為 `Confirmed`，避免被誤重複加到目標據點庫存。

### 30.4 權限對照

沿用既有角色設計（見第 4 階段權限系統，`Models/Roles.cs`）：

| 功能 | 管理人員 | 幹部 | 社工 |
|---|---|---|---|
| 物資／據點／轉移 新增/編輯/刪除 | ✓ | ✓ | 唯讀 |
| 轉移「確認送達／取消」 | ✓（不限據點） | 僅限本人所屬據點 | 僅限本人所屬據點 |
| 物資出庫（發放） | ✓（不限據點） | 僅限本人所屬據點 | 僅限本人所屬據點 |
| 帳號管理 | ✓ | - | - |

### 30.5 帳號綁定據點（轉移確認的據點權限）

`UserAccount` 新增 `LocationId`（可為 NULL）欄位，代表該帳號所屬的據點：

```sql
ALTER TABLE UserAccount ADD LocationId INT NULL;
ALTER TABLE UserAccount ADD CONSTRAINT FK_UserAccount_SupplyLocation FOREIGN KEY (LocationId) REFERENCES SupplyLocation(Id);
```

- 管理人員在「帳號管理」新增/編輯帳號時可指定「所屬據點」（非必填）
- 登入時，`LocationId` 會寫進登入 Cookie 的 Claims 裡（`AccountController.Login`）
- 「物資轉移」的「確認送達」「取消」動作（`SupplyTransferController.ConfirmReceipt` / `CancelTransfer`）現在會檢查：
  - 管理人員：不限據點，任何轉移都可以操作
  - 幹部／社工：只有當自己的 `LocationId` 等於該筆轉移的**目標據點**時才能操作，否則會被伺服器端擋下（`CanResolveTransfer` 方法）
- 尚未指定所屬據點的幹部／社工帳號，將無法確認或取消任何轉移，需由管理人員到「帳號管理」設定所屬據點

### 30.6 物資出庫：據點篩選、分類篩選與據點權限

`OutboundViewModel` 新增 `LocationId`（出庫的來源據點，必填）：

- **據點篩選**：新增出庫頁面最上方新增「據點」欄位
  - 管理人員：一般下拉選單，可自由選擇任一據點
  - 幹部／社工：鎖定為自己 `UserAccount.LocationId` 所屬據點（唯讀顯示，不能改選其他據點）
  - 尚未指定所屬據點的幹部／社工，無法進入出庫頁面（會被導回出庫紀錄並顯示提示訊息）
- **物資選單連動據點**：選擇物資的下拉選單只會顯示所選據點的物資（沿用物資轉移頁面的前端篩選寫法，不用額外呼叫 API），並依有效期限由近到遠排序
- **分類快速篩選**：物資選單上方新增「全部／無效期物資／有效期物資／冷凍食品」快速切換鈕（沿用 `Models/StockTypes.cs`），與據點篩選同時作用在同一個物資選單上
- **權限驗證**：`SupplyOutboundController` 的 Create（GET/POST）與 `SupplyOutboundService.IssueAsync` 都會檢查所選物資確實屬於所選據點，且幹部／社工只能對自己所屬據點執行出庫，伺服器端強制驗證（不只是前端隱藏）
- 「即期／已過期物資，建議優先出庫」提醒區塊也會依據使用者所屬據點篩選（管理人員看全部，幹部／社工只看自己據點）

---

## 31. 全站關鍵字搜尋與同品項統計

### 31.1 分類快速切換鈕配色

`Views/SupplyItem/Index.cshtml` 的「全部／無效期物資／有效期物資／冷凍食品」切換鈕，選中樣式由 `btn-dark`/`btn-outline-dark` 改為 `btn-primary`/`btn-outline-primary`，與全站主色一致。

### 31.2 全站關鍵字搜尋盤點

盤點結果：除了「物資管理」原本就有的據點/種類/分類下拉篩選，其餘清單頁完全沒有任何篩選功能。統一比照「用 query string `keyword` 觸發伺服器端 `Contains` 查詢」的寫法補上：

| 頁面 | 搜尋欄位 |
|---|---|
| `SupplyItem/Index` | 物資名稱、規格、種類、備註 |
| `SupplyLocation/Index` | 據點名稱、地址、聯絡人、電話 |
| `SupplyTransfer/Index` | 物資名稱、操作人員、備註 |
| `SupplyOutbound/Index` | 物資名稱、領用人姓名、聯絡方式、操作人員、備註 |
| `UserAccount/Index` | 帳號、顯示名稱 |

### 31.3 同品項重複資料：合併與統計

**問題**：「新增物資」原本永遠新建一筆資料，即使同據點已存在名稱/種類/規格/分類/效期完全相同的物資，也會產生重複列，庫存數字因此被拆散、無法一眼看出真實總量。

**設計**：

1. **建立時自動合併**（`SupplyItemController.Create`）：新增前先比對是否已有 `ItemName`、`Category`、`Specification`、`LocationId`、`ExpirationDate`、`StockType` 皆相同且啟用中的物資；找到就把數量加進既有那筆（訊息顯示「已合併數量」），找不到才新建一筆。圖片：若既有物資尚未設圖，才補上這次上傳的圖片，避免覆蓋既有圖片。
2. **既有重複資料一次性清理**（`Data/DbInitializer.MergeDuplicateItemsAsync`，於 `Program.cs` 啟動時與 `SeedAdminAsync` 一起呼叫）：依同一組比對鍵把現有重複的啟用中物資分組，同組只留最早建立的一筆、其餘數量併入、其餘停用。此方法是 idempotent 的，沒有重複資料時完全不動作，可以安全地每次啟動都執行。
3. **依物資名稱統計**（`SupplyItemController.Index` + `Models/ViewModels/SupplyItemSummaryViewModel.cs`）：在目前篩選/搜尋結果之上，依 `ItemName` 分組計算跨據點總量、據點數、是否含低庫存、最近效期，顯示在物資管理頁明細表格上方一個可收合的「依物資統計」卡片，讓同名物資即使分散在多筆資料/多據點，也能立即看到彙總數字。

