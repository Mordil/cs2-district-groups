import { Color } from "cs2/bindings"
import { MarkdownRenderer } from "cs2/ui"

// FormattedParagraphs + MarkdownRenderer is how we get nicely
// formatted and spaced tooltip content (or anything else that supports it).
export const markdownRenderer = new MarkdownRenderer()

// A Color, whose channels arrive as 0-1 floats, as a CSS rgba() string at its own alpha or one we pass in.
export const colorToCss = (c: Color, alpha: number = c.a): string =>
    `rgba(${Math.round(c.r * 255)}, ${Math.round(c.g * 255)}, ${Math.round(c.b * 255)}, ${alpha})`
