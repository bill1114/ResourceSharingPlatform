# 地方物資管理平台（ResourceSharingPlatform）

以 ASP.NET Core MVC 開發的地方物資管理系統，用於管理各據點的物資庫存、轉移、出庫（發放）、捐贈與報廢，並提供角色權限、戰情總覽與地圖總覽等功能。

## 技術架構

- **.NET 8** / ASP.NET Core MVC
- **Entity Framework Core 8** + SQL Server（`LocalSupplyDB`）
- Cookie-based 驗證，三種角色：管理人員（Admin）／幹部（Cadre）／社工（SocialWorker）
- [ClosedXML](https://github.com/ClosedXML/ClosedXML)：Excel 匯出

## 主要功能

- **戰情總覽 Dashboard**：低庫存、即期／過期物資彙總
- **據點管理** ＋ **地圖總覽**（Leaflet + OpenStreetMap）
- **物資主檔**：物資定義／多規格、圖片、全系統與各據點兩層安全庫存
- **物資轉移**：一次可調撥多筆品項（批次），採「建立→對方確認送達」兩階段流程，確認／取消權限限定在轉移的目標據點人員
- **物資出庫（發放）**：登記領用人、即期品優先顯示與警示、Excel 匯出、領取者頻率分析
- **物資捐贈** ／ **物資報廢**：異動紀錄與 Excel 匯出
- **帳號管理**：角色權限 ＋ 帳號可綁定所屬據點（用於限制轉移確認等操作範圍）
- **AI 智慧入庫**：資料層與操作介面已完成，尚未串接正式外部 AI 模型
- **LINE OA 通知**：設定頁與模擬測試，尚未串接正式 LINE API

完整開發進度與功能清單請見 [`ResourceSharingPlatform/Markdown/DevelopmentProgress.md`](ResourceSharingPlatform/Markdown/DevelopmentProgress.md)。

## 快速開始（本機開發）

```bash
Start.bat   # 還原套件、建置、（可選）建立/更新本機資料庫、以 Kestrel 啟動
Stop.bat    # 停止服務
```

啟動後開啟 <http://localhost:5140>。首次啟動會自動建立預設管理員帳號：

- 帳號：`admin`　密碼：`admin`
- **正式使用前請務必更改預設密碼**，並視需要建立其他角色帳號（帳號管理頁面，僅管理人員可操作）。

## 部署

本機另外配置了一份獨立的 IIS 部署（<http://localhost:8081>），與 `Start.bat` 的開發用 Kestrel（port 5140）互不影響，可同時並存。更新步驟、其他主機部署方式與已知注意事項，請見：

- [`ResourceSharingPlatform/Markdown/IISDeployment.md`](ResourceSharingPlatform/Markdown/IISDeployment.md)
- [`ResourceSharingPlatform/Markdown/IntegrationGuide.md`](ResourceSharingPlatform/Markdown/IntegrationGuide.md)

## 文件索引

| 文件 | 說明 |
|---|---|
| [ResourceSharingPlatform_dev_spec.md](ResourceSharingPlatform/Markdown/ResourceSharingPlatform_dev_spec.md) | 開發規格書 |
| [DevelopmentProgress.md](ResourceSharingPlatform/Markdown/DevelopmentProgress.md) | 開發進度與功能清單 |
| [DatabaseSchemaAndUiMapping.md](ResourceSharingPlatform/Markdown/DatabaseSchemaAndUiMapping.md) | 資料庫結構與畫面對照 |
| [IntegrationGuide.md](ResourceSharingPlatform/Markdown/IntegrationGuide.md) | 資料整合／串接參考 |
| [IISDeployment.md](ResourceSharingPlatform/Markdown/IISDeployment.md) | IIS 部署筆記 |
| [BackupPlan.md](ResourceSharingPlatform/Markdown/BackupPlan.md) | 資料庫／圖片備份計畫 |
| [ExecutionGuide.md](ResourceSharingPlatform/Markdown/ExecutionGuide.md) | 執行手冊 |
| [CompletionReport.md](ResourceSharingPlatform/Markdown/CompletionReport.md) | 完成報告 |

## 版本紀錄

每個完成的功能／修正都會建立對應的 [GitHub Release](https://github.com/bill1114/ResourceSharingPlatform/releases)（語意化版本號），可在該頁面查閱各版本異動內容與回溯歷史版本。
