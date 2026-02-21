# Speech Card Viewer

Cross-platform desktop app for creating speech outlines and generating 3x5 index cards.

## Stack
- Avalonia UI 11 / .NET 8 / CommunityToolkit.Mvvm
- QuestPDF for PDF export
- Targets Mac, Windows, Linux

## Features
- **Outline Editor**: Type hierarchical outlines using indentation; live tree preview
- **Card Generation**: Leaf nodes become 3x5 cards with full breadcrumb path
- **Practice Mode**: Flip through cards on-screen with keyboard navigation (arrow keys, space)
- **Shuffle**: Randomize card order for practice
- **Save/Open**: Persist outlines as `.cardviewer.json` files
- **Import**: Load `.md` or `.txt` markdown outline files
- **Export PDF**: Generate 3x5 index card PDFs for printing

## Card Logic
Only leaf nodes (nodes with no children) get cards. Each card shows:
- Breadcrumb path (ancestors) in small text at top
- Main topic centered in large text
- Card number / total

## Example
```
How to draw
   Understanding perspective
      1 point perspective
      2 point perspective
   Color theory
      Warm vs cool colors
```
Generates 3 cards:
1. How to draw > Understanding perspective > 1 point perspective
2. How to draw > Understanding perspective > 2 point perspective
3. How to draw > Color theory > Warm vs cool colors

## Running
```
dotnet run --project src/CardViewer
```

## Testing
```
dotnet test
```
