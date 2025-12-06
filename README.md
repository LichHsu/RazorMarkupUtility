# RazorMarkupUtility

A powerful MCP server for analyzing, inspecting, and refactoring Razor (.razor) and HTML files.

## Tools

### 1. `analyze_razor`
Analyzes Razor structure and dependencies.
*   **Parameters**:
    *   `path` (string): Path to file or directory.
    *   `analysisType` (string):
        *   `TagHelpers`: Identifies Custom Components and TagHelpers.
        *   `Dependencies`: Maps `_Layout` and `partial` view dependencies.
        *   `ImplicitDeps`: Maps `_ViewImports` and `App.razor` scopes.
        *   `UsedClasses`: Lists all CSS classes used in the file/project.
        *   `Orphans`: Identifies classes used but not defined in scoped CSS.
        *   `Validation`: (**NEW**) Checks for HTML syntax errors.
        *   `Patterns`: (**NEW**) Identifies duplicate HTML structures.
    *   `options` (json string): `{ "recursive": true }`

### 2. `inspect_razor_dom`
Inspects the DOM structure of a Razor file using XPath.
*   **Parameters**:
    *   `path` (string): Path to the Razor file.
    *   `xpath` (string, optional): XPath query. Returns full DOM if omitted.

### 3. `edit_razor_dom`
Modifies the DOM structure of a Razor file.
*   **Parameters**:
    *   `path` (string): Path to the Razor file.
    *   `operationsJson` (json string): (**NEW**) List of operations for batch processing.
        *   `[{ "type": "Update", "xpath": "...", "content": "..." }, ...]`

### 4. `refactor_razor`
Performs high-level refactoring operations.
*   **Parameters**:
    *   `path` (string): Target file or directory.
    *   `refactoringType` (string):
        *   `Split`: Splits `.razor` into `.razor.cs` and `.razor.css`.
        *   `BatchRenameClass`: Renames a CSS class across multiple files.
    *   `optionsJson` (json string): `{ "oldClass": "...", "newClass": "..." }`.

## Development
Run `dotnet build` to compile.
Run `RazorMarkupUtility.exe --test` to execute internal unit tests.
