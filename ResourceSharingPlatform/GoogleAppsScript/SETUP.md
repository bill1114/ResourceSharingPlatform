# Google Sheets 後端設定步驟

這份文件說明如何把 `Code.gs` 部署成一個 Web App，讓 ASP.NET Core 程式改用 Google Sheets 當暫時資料庫。

## 1. 建立 Google Sheet

1. 到 [Google Sheets](https://sheets.google.com) 新增一份空白試算表，取個名字（例如「物資平台資料庫」）。
2. 點 **檔案 > 設定**，把「地區設定／時區」設成你所在的時區（例如 `台灣 GMT+8`）。這會影響日期時間欄位的顯示。
3. 從網址列複製這份試算表的 ID（網址 `https://docs.google.com/spreadsheets/d/《這一段》/edit` 中間那一段）。

## 2. 建立獨立的 Apps Script 專案並貼上程式碼

用獨立（standalone）專案比綁定在試算表裡更好操作：

1. 開新分頁到 [script.new](https://script.new)，會自動建立一個新的 Apps Script 專案。
2. 把預設的 `程式碼.gs` 內容全部刪除，貼上本資料夾 [`Code.gs`](./Code.gs) 的全部內容。
3. 把檔案最上面的 `var SPREADSHEET_ID = 'REPLACE_WITH_YOUR_SPREADSHEET_ID';` 換成第 1 步複製的試算表 ID。
4. 存檔（Ctrl+S 或按左上角的雲端圖示）。

## 3. 設定密鑰（API_SECRET）

這個密鑰用來防止陌生人呼叫你的 Web App 亂寫資料。

1. Apps Script 編輯器左側齒輪圖示 **專案設定**。
2. 捲到「指令碼屬性」，點「新增指令碼屬性」。
3. 屬性名稱填 `API_SECRET`，值填一組你自己想的亂數字串（例如用密碼產生器產生一組 32 碼英數字），存檔。
4. **記下這組值**，等一下要填到 ASP.NET Core 專案的設定裡。

## 4. 部署為 Web App

1. Apps Script 編輯器右上角「部署」→「新增部署作業」。
2. 齒輪圖示選擇類型：**網頁應用程式**。
3. 設定：
   - 說明：隨意（例如 `v1`）
   - **執行身分：我**
   - **具有存取權的使用者：任何人**（重點是不要選只有自己，否則 C# 端無法呼叫）
4. 點「部署」，第一次會跳出授權畫面：
   - 選擇你的 Google 帳號
   - 出現「Google 尚未驗證這個應用程式」時，點「進階」→「前往 (專案名稱)（不安全）」→「允許」
   （這是正常的，因為這是你自己寫的私人腳本，不是要跑公開發佈的程式）
5. 完成後會顯示一組「網頁應用程式」網址，格式類似：
   `https://script.google.com/macros/s/AKfycb.../exec`
   **複製這個網址**，等一下要填到 ASP.NET Core 專案。

> 之後如果你修改了 `Code.gs`，要讓改動生效，記得「部署」→「管理部署作業」→ 選現有部署 → 點編輯（鉛筆）→ 版本選「新版本」→ 部署。單純存檔不會更新已部署的網址內容。

## 5. 初始化 5 個工作表

不需要在編輯器裡手動選函式執行，部署好之後直接用瀏覽器造訪這個網址（把佔位文字換成你自己的值，`{...}` 部分要做 URL encode）：

```
你的網頁應用程式網址?body={"action":"setupSheetsHttp","secret":"你的API_SECRET"}
```

第一次執行會回傳 `{"success":true,"message":"sheets ready"}`，回到試算表應該會看到 5 個分頁：`SupplyLocation`、`SupplyItem`、`SupplyTransferLog`、`SupplyOutboundLog`、`UserAccount`，且都已經有表頭列（原本的空白「Sheet1／工作表1」分頁會被自動清掉）。

## 6. 把網址跟密鑰交給 ASP.NET Core 專案

**本機開發**，在專案目錄下執行（不要把密鑰寫進 `appsettings.json`，那會被推上 GitHub）：

```bash
dotnet user-secrets init
dotnet user-secrets set "GoogleSheets:WebAppUrl" "貼上你的網頁應用程式網址"
dotnet user-secrets set "GoogleSheets:ApiSecret" "貼上你的 API_SECRET"
```

**部署到雲端主機時**，在該平台的環境變數設定裡新增：

```
GoogleSheets__WebAppUrl = 貼上你的網頁應用程式網址
GoogleSheets__ApiSecret = 貼上你的 API_SECRET
```

（注意是兩個底線 `__`，這是 .NET 設定系統讀取巢狀設定值的慣例寫法）

## 7. 測試連線

程式啟動後，第一次啟動會自動檢查 `UserAccount` 分頁是否為空，若是空的會自動建立預設帳號：
- 帳號：`admin`
- 密碼：`admin`

用這組帳密登入，確認可以正常瀏覽 Dashboard，並且試算表的 `UserAccount` 分頁多了一列 admin 資料，代表串接成功。

## 已知限制

- Google Sheets 沒有關聯式資料庫的交易保護，`Code.gs` 用 `LockService` 讓同一時間只有一個寫入請求在執行，避免併發衝突，但仍不是正式資料庫等級的保證，僅適合小規模試用。
- 日期時間欄位使用試算表所在時區格式化，若程式部署主機跟試算表時區不同，顯示時間可能有些微落差，屬已知限制，不影響功能操作邏輯。
