# Apollo Code Editor

Apollo is a web-based C# code editor powered by Roslyn that enables writing, testing, and running C# code directly in the browser using WebAssembly. It provides real-time compilation, diagnostics, and IntelliSense features, but runs entirely in the browser.

[![Deploy Apollo](https://github.com/Mythetech/Apollo/actions/workflows/static.yml/badge.svg)](https://github.com/Mythetech/Apollo/actions/workflows/static.yml)
[![MIT License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

<img width="1728" alt="image" src="https://github.com/user-attachments/assets/3b4682ed-3289-4ef4-83e6-e6c36c2a4ca6" />

## Features

### Code Editing
- Real-time C# syntax highlighting and error detection
- IntelliSense support 
- Multi-file project support
- WebAssembly-based compilation and execution
- Offline support

### Project Types
- Console Applications
- Web APIs (ASP.NET Core Minimal APIs)
- Test Projects

### Development Experience
- Real-time error detection and diagnostics
- Integrated console output
- API endpoint testing for Web APIs
- Test runners
- Solution management

## Quick Start

1. Visit [Apollo Editor](https://mythetech.github.io/Apollo/)
2. Choose a project template or start with a blank console application
3. Write your C# code
4. Click Run (or press Ctrl+Enter) to execute

### Key Technologies
- Blazor WebAssembly for the UI
- Monaco Editor for code editing
- Roslyn for code analysis and compilation
- WebWorkers for processing

## Development Setup

### Prerequisites
- .NET 9.0 SDK
- A modern web browser with WebAssembly support

### Getting Started
1. Clone the repository
    ```bash
    git clone https://github.com/mythetech/apollo.git
    cd apollo
    ```

2. Install dependencies
    ```bash
    dotnet restore
    ```

3. Build the solution
    ```bash
    dotnet build
    ```

4. Run the development server
    ```bash
    dotnet run --project Apollo.Web
    ```

5. Navigate to `https://localhost:7092`

## Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details on:
- Code style and conventions
- Development workflow
- Testing requirements
- Pull request process

### Development Workflow
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

Response time:
- Issues: Usually within 48 hours
- Pull requests: Review within 1 week

## Project Status

Apollo is currently in alpha. While it's stable enough for learning and experimentation, we recommend against production use. Key areas under development:

- [ ] Expanded project support for class libraries
- [ ] Enhanced Web API testing features
- [ ] Improved intellisense
- [ ] Additional testing frameworks
- [ ] Performance optimizations
- [ ] Overall Application Stability 

Please see our [Roadmap](Roadmap.md) for details

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details. 
