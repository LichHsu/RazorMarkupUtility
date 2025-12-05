# RazorMarkupUtility - Razor DOM 處理 MCP 伺服器

`RazorMarkupUtility` 是一個專為 AI Agent 設計的 MCP 伺服器，旨在解決「在不破壞 Razor 語法的前提下修改 HTML 結構」的難題。它使用 `HtmlAgilityPack` 來解析 Razor 檔案，並提供了一套安全的 DOM 操作工具。

## 設計理念：Agent-First (AI 優先)

1.  **結構化視角**：AI 不再需要面對雜亂的純文字，而是透過 `get_razor_dom` 看到清晰的 DOM 樹。
2.  **安全修改**：`update_razor_element` 允許 AI 僅修改特定節點的屬性或內容，而不會誤刪周圍的 `@if` 或 `@foreach` 區塊。
3.  **精確定位**：支援 XPath 查詢，讓 AI 能像使用 jQuery 一樣精準選取目標。

## 功能特色

*   **DOM 解析 (`get_razor_dom`)**：回傳簡化的 DOM 結構 (Tag, ID, Class, XPath)。
*   **元素查詢 (`query_razor_elements`)**：使用 XPath 搜尋特定元素。
*   **屬性/內容更新 (`update_razor_element`)**：安全地修改 InnerHTML 或屬性 (如 `class`, `@onclick`)。
*   **元素包裹 (`wrap_razor_element`)**：將現有元素包裹在新的父容器中 (例如 `<div class="card">...</div>`)。

## 安裝與執行

本專案為 .NET 10.0 Console 應用程式。

### 建置
```bash
dotnet build
```

### 執行測試
```bash
dotnet run -- --test
```

### 作為 MCP 伺服器執行
```bash
dotnet run
```

## 可用工具 (Tools)

### 1. `get_razor_dom`
取得 Razor 檔案的 DOM 結構。
*   **參數**: `path`

### 2. `query_razor_elements`
搜尋元素。
*   **參數**: `path`, `xpath`

### 3. `update_razor_element`
更新元素。
*   **參數**:
    *   `path`, `xpath`
    *   `newInnerHtml` (optional)
    *   `attributes` (optional Dictionary)

### 4. `wrap_razor_element`
包裹元素。
*   **參數**:
    *   `wrapperTag` (e.g. "div")
    *   `attributes` (optional)

### 5. `append_razor_element`
在指定元素內部添加新的 HTML 子節點。
*   **參數**:
    *   `path`
    *   `xpath`
    *   `newHtml`

### 6. `split_razor_file`
拆分單個 Razor 檔案 (HTML/C#/CSS)。
*   **參數**:
    *   `path`: Razor 檔案路徑

### 7. `split_razor_batch`
批次拆分 Razor 檔案 (HTML/C#/CSS)。
*   **參數**:
    *   `directory`
    *   `recursive` (default: false)

### 8. `batch_rename_class_usage`
批次重新命名 CSS Class 使用 (跨檔案)。
*   **參數**:
    *   `directory`
    *   `oldClass`
    *   `newClass`
    *   `recursive` (default: true)

## 💻 CLI 命令列模式 (CLI Mode)

本工具支援直接透過命令列執行批次任務：

### 1. 批次拆分 Razor 檔案
```bash
dotnet run -- split-batch --path "d:\project\components" [--recursive]
```

### 2. 批次重新命名 Class
```bash
dotnet run -- rename-class --path "d:\project" --old "btn-primary" --new "btn-main" [--recursive]
```


## 使用範例

### 將所有按鈕改為 MudButton
```json
// 1. 查詢所有 button
{
  "name": "query_razor_elements",
  "arguments": { "path": "...", "xpath": "//button" }
}

// 2. 更新特定 button (假設 xpath 為 /div/button[1])
{
  "name": "update_razor_element",
  "arguments": {
    "path": "...",
    "xpath": "/div/button[1]",
    "attributes": { "class": "mud-button-filled" }
  }
}
```
