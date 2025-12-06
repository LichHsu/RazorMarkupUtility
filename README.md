# RazorMarkupUtility - Razor DOM 處理 MCP 伺服器

> **Part of Lichs.MCP Workspace**

`RazorMarkupUtility` 專為 AI Agent 設計，解決「在不破壞 Razor 語法的前提下安全修改 HTML 結構」的難題。它使用 `HtmlAgilityPack` 解析 Razor，並提供結構化的 DOM 操作能力。

本專案基於 **Lichs.MCP.Core** 構建。

## 🌟 核心理念：Agent-First

AI 不再需要處理脆弱的純文字 Regex 替換，而是透過 **DOM 樹** (`get_razor_dom`) 與 **XPath** (`query_razor_elements`) 來精確定位與修改元素。

## 🚀 主要功能

*   **DOM 解析**: `get_razor_dom` 回傳簡化的 DOM 結構。
*   **精確查詢**: `query_razor_elements` 支援 XPath 搜尋。
*   **安全修改**: 
    *   `update_razor_element`: 修改 InnerHTML 或屬性。
    *   `wrap_razor_element`: 包裹元素 (如增加 Card 容器)。
    *   `append_razor_element`: 添加子元素。
*   **檔案拆分**: `split_razor_file` / `split_razor_batch` 將 `.razor` 拆分為 Code-behind 與 Scoped CSS。
*   **Class 重構**: `batch_rename_class_usage` 跨檔案批次更名 CSS Class。
*   **孤兒分析**: `scan_razor_orphans` 找出使用但未定義的 CSS Class。

## 📦 安裝與配置

### 建置
```bash
cd "d:\Lichs Projects\MCP"
dotnet build Lichs.MCP.slnx
```

### MCP 客戶端配置
```json
{
  "mcpServers": {
    "razor-utility": {
      "command": "dotnet",
      "args": ["d:\\Lichs Projects\\MCP\\RazorMarkupUtility\\bin\\Debug\\net10.0\\RazorMarkupUtility.dll"]
    }
  }
}
```

## 💻 CLI 模式

支援以下 CLI 指令：
- **批次拆分**: `dotnet run -- split-batch <directory> [recursive]`
- **批次更名**: `dotnet run -- rename-class <directory> <oldClass> <newClass>`

---
*Powered by Lichs.MCP.Core*
