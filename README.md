# RazorMarkupUtility

A powerful MCP server for analyzing, inspecting, and refactoring Razor (.razor) and HTML files.
Now supports strongly-typed parameters for enhanced AI interaction.

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
        *   `Validation`: Checks for HTML syntax errors.
        *   `Patterns`: Identifies duplicate HTML structures.
    *   `options`: (Object)
        *   `recursive` (boolean, default: true): Whether to scan subdirectories.

### 2. `inspect_razor_dom`
Inspects the DOM structure of a Razor file using XPath.
*   **Parameters**:
    *   `path` (string): Path to the Razor file.
    *   `xpath` (string, optional): XPath query. Returns full DOM if omitted.

### 3. `edit_razor_dom`
Modifies the DOM structure of a Razor file with batch support.
*   **Parameters**:
    *   `path` (string): Path to the Razor file.
    *   `operations`: (List of Objects)
        *   `type`: `Update`, `Wrap`, `Append`
        *   `xpath`: Target XPath.
        *   `content`: HTML content or wrapper tag.
        *   `attributes`: Key-value dictionary for attributes.

### 4. `refactor_razor`
Performs high-level refactoring operations.
*   **Parameters**:
    *   `path` (string): Target file or directory.
    *   `refactoringType` (string): `Split` or `BatchRenameClass`.
    *   `options`: (Object)
        *   `oldClass`: Source class name (for Rename).
        *   `newClass`: Target class name (for Rename).
        *   `recursive`: Boolean.

## Development
Run `dotnet build` to compile.
Run `RazorMarkupUtility.exe --test` to execute internal unit tests.
