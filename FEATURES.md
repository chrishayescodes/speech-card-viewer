# Speech Card Viewer - Features

## Overview

Speech Card Viewer is a cross-platform desktop app for creating speech outlines and practicing with virtual 3x5 index cards. Built with Avalonia UI, .NET 8, and QuestPDF.

## Editor

### Text Mode
- Monospace text editor for writing outlines using markdown headers (`#`, `##`, `###`) or indentation
- Supports markdown list prefixes (`-`, `*`)
- Live preview panel showing the parsed outline as an interactive tree
- Preview button on each tree node to jump directly into practice mode at that point
- Parse status bar showing item and card counts
- 150ms debounced auto-parse as you type

### Structured Mode
- Interactive tree editor with inline text editing
- Add sibling, add child, and remove node operations
- Promote (un-nest) and demote (nest) nodes in the hierarchy
- Move nodes up/down within their sibling list
- Delete button on each node
- Preview button on each node to jump to practice mode

### Keyboard Shortcuts (Structured Mode)
| Shortcut | Action |
|---|---|
| Ctrl+Enter | Add sibling |
| Tab | Demote (nest under previous sibling) |
| Shift+Tab | Promote (move up a level) |
| Delete | Remove selected node |
| Ctrl+Backspace | Remove selected node |
| Ctrl+Up | Move node up |
| Ctrl+Down | Move node down |
| Enter | Exit text editing, return focus to tree |

### Show Title
- Editable title field at the top of the editor
- Auto-detected from the first `#` markdown header or single root node
- Displayed at the top of every card in practice mode and PDF export

## Practice Mode

### Card Display
- Virtual 3x5 index card layout
- Hierarchy breadcrumb with escalating size and weight toward the current topic
- Large, bold topic text that scales down to fit
- Bullet points for depth 3+ child items
- Show title with horizontal rule at the top of each card
- Card number indicator

### Navigation
| Control | Action |
|---|---|
| Right / Down / Space / Enter | Next card |
| Left / Up / Backspace | Previous card |
| `<<` button | Previous chapter/section |
| `>>` button | Next chapter/section |

### Outline Sidebar
- Toggleable sidebar showing the full outline tree
- Current card highlighted in the tree
- Click any node to jump to the first card in that section
- Expand All / Collapse All buttons
- Auto-expands to show the current card's position

### Smart Chapter Navigation
- Automatically determines the chapter level based on the shallowest breadcrumb with multiple distinct values
- Previous/Next chapter jumps intelligently across outline structure

## File Operations

### Formats
| Format | Extension | Operations |
|---|---|---|
| Card Viewer | `.cdv` | Open, Save, Save As |
| Legacy Card Viewer | `.cardviewer.json` | Open |
| Markdown | `.md` | Import |
| Text | `.txt` | Import |
| PDF | `.pdf` | Export |

### Save Behavior
- **Save** (Ctrl+S) saves silently to the current file path, or prompts if unsaved
- **Save As** (Ctrl+Shift+S) always prompts for a new location
- JSON format with outline name, timestamps, and full tree structure

### Menu Bar
- **File**: Open, Import Markdown, Save, Save As, Export PDF
- **View**: Editor, Practice

### Toolbar
- Icon buttons for Open, Import, Save, and Export PDF

## PDF Export
- Exports all cards to a multi-page PDF
- 5x3 inch pages (standard index card size)
- Includes show title, hierarchy, topic, bullets, and card numbers
- Uses QuestPDF library

## Other
- Cross-platform (macOS, Windows, Linux)
- `.cdv` file association on macOS via Info.plist
- Follows system light/dark theme
- Caches practice view when switching between Editor and Practice tabs
- Status bar with operation feedback
