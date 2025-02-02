# Apollo Roadmap

> **Note**: This roadmap outlines the general direction of Apollo's development but should not be considered a firm commitment. Features and timelines may change based on community feedback, technical constraints, and project priorities.

## Status Definitions

| Status | Description |
|--------|-------------|
| 💭 Idea | Feature under consideration or planning |
| 🚧 Developing | Actively being worked on |
| 🧪 Alpha | Available but may be unstable/incomplete |
| ✅ Complete | Stable and well-tested |

## Editor Features

| Feature | Status | Notes |
|---------|--------|-------|
| Basic C# syntax highlighting | ✅ | Using Monaco editor |
| Multi-file support | ✅ | |
| Split-pane layout | ✅ | |
| Draggable tab layout with view menu options | ✅ | |
| Theme options and Light/Dark mode | ✅ | |

## Code Analysis

| Feature | Status | Notes |
|---------|--------|-------|
| "Real-time" syntax error detection | ✅ | Delay clearing markers |
| Semantic analysis | ✅ | |
| Code completion | 🧪 | |
| Reference highlighting | 🧪 | Basic implementation |
| IntelliSense completion | 🧪 | Doesn't support user's types |
| Quick Info (hover) | 🧪 | |
| Parameter info | 🧪 | Basic support implemented |
| Code actions/quick fixes | 💭 | |
| Code formatting | 💭 | |
| Refactoring support | 💭 | |

## Compilation & Execution

| Feature | Status | Notes |
|---------|--------|-------|
| Console app compilation | ✅ | |
| Console output capture | ✅ | |
| Basic WASM execution | ✅ | |
| Assembly reference resolution | 🧪 | Some issues in cloud |
| Debug support | 💭 | Long-term goal |

## Web API Support

| Feature | Status | Notes |
|---------|--------|-------|
| Minimal API templates | ✅ | |
| Endpoint testing | 🧪 | Basic implementation |
| Middleware support | 💭 | |
| OpenAPI support | 💭 | |
| Request/response templates | 💭 | |
| Authentication support | 💭 | |

## Testing Support

| Feature | Status | Notes |
|---------|--------|-------|
| xUnit test running | 🧪 | Basic support |
| Test explorer | 🧪 | |
| Test debugging | 💭 | |
| MSTest support | 💭 | |
| NUnit support | 💭 | |
| TUnit support | 💭 | |
| Code coverage | 💭 | |

## Project Management

| Feature | Status | Notes |
|---------|--------|-------|
| Project templates | ✅ | Basic templates available |
| Solution export to various formats | ✅ | |
| Local storage backups | ✅ | |
| Solution explorer | 🧪 | Basic implementation |
| Project settings | 🚧 | In progress |
| GitHub import | 🧪 | Only imports .cs |
| Project sharing | 🧪 | Base64 string is long |

## Infrastructure

| Feature | Status | Notes |
|---------|--------|-------|
| WebAssembly compilation | ✅ | |
| Azure deployment | ✅ | Basic Tier |
| GitHub deployment | 💭 | |
| Offline support | 🚧 | Webmanifest isn't publishing |
| Performance optimization | 🚧 | Ongoing work |
| Performance monitoring | 💭 | |
| Error tracking | 💭 | |
| Startup Time Improvements | 💭 | |

## Razor Support
| Feature | Status | Notes |
|---------|--------|-------|
| Razor Support | 💭 | In DotNet 10 razor compilation may be simplified
| Preview pane for components | 💭 | |

## Full Stack Emulation
| Feature | Status | Notes |
|---------|--------|-------|
| Blazor client with api calls proxied to emulated web api | 💭 | |

## AI Features
| Feature | Status | Notes |
|---------|--------|-------|
| Allow users to provide an AI key to unlock some suggestions, completions, etc | 💭 | |
| Make a new webworker responsible for a workspace experience where the ai can get its own copy of the solution model and iterate | 💭 | |


## Plugin System
| Feature | Status | Notes |
|---------|--------|-------|
| Plugin system | 💭 | Provide some interfaces to allow user plugins/extensions to register in the editor to provide additional functionality/services |
| Plugin template | 💭 | A template to start making plugins for the editor in the editor

## Documentation
| Feature | Status | Notes |
|---------|--------|-------|
| API Documentation | 🚧 | XML docs in progress |
| Architecture Guide | 🧪 | Basic overview available |
| Sample Projects | 🧪 | Few basic examples |
| Video Tutorials | 💭 | |
| Interactive Tutorials | 💭 | In-editor guided learning |

## Community Features
| Feature | Status | Notes |
|---------|--------|-------|
| Public Template Gallery | 💭 | Share project templates |
| User Snippets | 💭 | Save and share code snippets |
| Community Plugins | 💭 | Depends on plugin system |