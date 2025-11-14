# YouTube Tool

這個專案提供一個基於 React (前端) 與 .NET 8 Web API (後端) 的 YouTube 小工具，可撈取指定影片的主要留言，每頁顯示 40 筆且針對相同使用者去重，留言內容超過 50 個字會自動截斷。後端使用 SQLite 儲存查詢紀錄。

## 專案結構

```
backend/   # ASP.NET Core Web API 專案
frontend/  # React + Vite 前端專案
```

## 需求準備

1. 申請 YouTube Data API v3 金鑰，將金鑰填入 `backend/appsettings.json` 的 `YoutubeApi:ApiKey` 欄位，或使用環境變數覆寫。
2. 於本機安裝 .NET 8 SDK 與 Node.js (建議 v18 以上)。

## 後端使用方式

```bash
cd backend
# 還原相依套件並啟動 API
 dotnet restore
 dotnet run
```

預設 API 會在 `https://localhost:7063` 與 `http://localhost:5063` 監聽，並自動建立同目錄下的 SQLite 資料庫 `YoutubeTool.db`。

### 環境變數覆寫

可使用下列環境變數覆寫設定：

- `YoutubeApi__ApiKey`
- `YoutubeApi__BaseUrl`
- `YoutubeApi__MaxPageSize`

## 前端使用方式

```bash
cd frontend
npm install
npm run dev
```

開發伺服器預設埠為 `5173`，若需修改 API 位址，可於啟動前設定環境變數 `VITE_API_BASE_URL`，預設為 `http://localhost:5063/api`。

若要產生正式版建置：

```bash
npm run build
```

## API 說明

- `GET /api/youtubecomments?videoId={videoId}&page={page}`
  - 每頁回傳 40 筆主留言
  - 同一使用者僅保留第一筆留言
  - 回應欄位：`videoId`, `page`, `pageSize`, `hasMore`, `comments`

## SQLite 記錄

每次呼叫 API 會將查詢的影片 ID、頁數、回傳筆數與時間記錄在 `ApiRequestLogs` 資料表中，可用來追蹤使用狀況。

## 注意事項

- 專案預設不會提交實際的 API 金鑰，請自行設定。
- 由於環境限制，範例程式碼未在此環境中執行 `dotnet` 或 `npm` 指令，請於本機執行相關指令以確保依賴安裝與建置成功。
