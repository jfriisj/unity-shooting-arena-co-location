@workspace /agent **GENERATE DYNAMIC SCRIPT OVERVIEW**

**Objective:**
Scan the entire `Assets/Scripts/` directory and generate an up-to-date `Docs/cs-files-overview.md`. The documentation must be based on the *current code content*, not filenames alone.

**Execution Strategy:**
1.  **File Crawl:**
    * List all `.cs` files recursively in `Assets/Scripts/`.
2.  **Content Analysis:**
    * For *each* file: Read the class summary (/// summary) and the public methods.
    * Determine the script's primary responsibility (e.g., "Manages Networking", "Controls AI").
3.  **Document Generation:**
    * Rewrite `Docs/cs-files-overview.md`.
    * Group scripts by their Folder names (e.g., ## Colocation, ## Shooting).
    * Format: `- `Filename.cs` — [Auto-generated description of what the code actually does].`
    * *Crucial:* Ensure NO deleted scripts remain in the list, and ALL new scripts are included.

**Output:** The updated Markdown file content.

**Action:** Scan the codebase and rewrite the overview document.