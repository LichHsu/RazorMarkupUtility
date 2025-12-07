# RazorMarkupUtility (Razor 標記工具)

這是一個強大的 MCP 伺服器，專門用於分析、檢視與重構 Razor (.razor) 與 HTML 檔案。
支援強型別參數，提升 AI 互動的準確性。

## 可用工具 (Tools)

### 1. `analyze_razor`
分析 Razor 結構與依賴關係。
*   **參數**:
    *   `path` (string): 目標檔案或目錄。
    *   `analysisType` (string):
        *   `TagHelpers`: 識別自定義組件與 TagHelpers。
        *   `Dependencies`: 繪製 `_Layout` 與 `partial` 視圖依賴關係。
        *   `ImplicitDeps`: 繪製 `_ViewImports` 與 `App.razor` 作用域。
        *   `UsedClasses`: 列出檔案/專案中使用的所有 CSS 類別。
        *   `Orphans`: 識別已使用但未在 Scoped CSS 中定義的 (孤兒) 類別。
        *   `Validation`: 檢查 HTML 語法錯誤。
        *   `Patterns`: 識別重複的 HTML 結構 (DRY 分析)。
    *   `options`: (Object, 選填)
        *   `recursive` (boolean, 預設: true): 是否掃描子目錄。
        *   `globalCssPath` (string): 全域 CSS 檔案路徑 (用於 Orphans 白名單)。
        *   `ignoreFilePath` (string): 忽略規則檔案路徑 (預設自動讀取 `tailwind-ignore.txt`)。

### 2. `inspect_razor_dom`
使用 XPath 檢視 Razor 檔案的 DOM 結構。
*   **參數**:
    *   `path` (string): Razor 檔案路徑。
    *   `xpath` (string, 選填): XPath 查詢字串。若省略則回傳完整 DOM。

### 3. `edit_razor_dom`
批次修改 Razor 檔案的 DOM 結構。
*   **參數**:
    *   `path` (string): Razor 檔案路徑。
    *   `operations`: (List of Objects, 操作列表)
        *   `type`: 操作類型 (`Update`, `Wrap`, `Append`)。
        *   `xpath`: 目標 XPath。
        *   `content`: HTML 內容或包裝標籤。
        *   `attributes`: 屬性字典 (Key-Value)。

### 4. `refactor_razor`
執行高階重構操作。
*   **參數**:
    *   `path` (string): 目標檔案或目錄。
    *   `refactoringType` (string): 重構類型 (`Split` 拆分檔案, `BatchRenameClass` 批次更名)。
    *   `options`: (Object, 選填)
        *   `oldClass`: 原類別名稱 (用於更名)。
        *   `newClass`: 新類別名稱 (用於更名)。
        *   `recursive`: 是否遞迴 (boolean)。

### 5. `merge_razor`
將設計檔 (Design HTML) 的樣式透過 `data-mcp-id` 合併回邏輯檔 (Logic Razor)。
*   **參數**:
    *   `logicPath` (string): 原始 Razor 檔案路徑 (骨架)。
    *   `designPath` (string): 新 HTML 檔案路徑 (皮膚)。
    *   `options`: (Object, 選填)
        *   `idAttribute`: 自定義 ID 屬性 (預設: `data-mcp-id`)。
        *   `attributesToMerge`: 要合併的屬性列表 (預設: `["class", "style"]`)。

## 命令列介面 (CLI)

本工具支援強大的 CLI 審查功能：

```bash
# 執行 Razor CSS 孤兒審查 (支援白名單與忽略規則)
RazorMarkupUtility.exe audit razor \
  --path "專案路徑" \
  --global-css "全域樣式檔路徑.css" \
  --ignore-file "規則檔路徑.txt"
```

## 開發與測試
*   執行 `dotnet build` 進行編譯。
*   執行 `RazorMarkupUtility.exe --test` 運行內部單元測試。
