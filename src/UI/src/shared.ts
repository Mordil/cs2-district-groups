import { MarkdownRenderer } from "cs2/ui"
import { bindValue } from "cs2/api"
import mod from "../mod.json"

// FormattedParagraphs + MarkdownRenderer is how we get nicely
// formatted and spaced tooltip content (or anything else that supports it).
export const markdownRenderer = new MarkdownRenderer()

// True only in a Debug build of the C# side
export const isDebugBuild$ = bindValue<boolean>(mod.id, "isDebugBuild", false)

// True when the currently selected entity has an active district-group assignment
export const selectedBuildingHasGroupAssignment$ = bindValue<boolean>(
    mod.id,
    "selectedBuildingHasGroupAssignment",
    false
)
