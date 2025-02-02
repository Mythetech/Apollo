# Architecture

## Overview
Apollo is a c# code editor running client-side in the browser with Blazor WebAssembly. This enables us to dynamically compile and execute code without a server, however it does come with its own unique restrictions and constraints.

## Modular Design
Apollo is organized into several projects to promote testability, separation of concerns, and help provide clear boundaries between functionality.

### Component Based Design
The UI is primarily contained in the `Components` project. With Client/Server projects this became a very prevalent pattern with Blazor. Even though our primary target is WebAssembly, because the components are isolated and depend on interfaces, it would be feasible to have a desktop target (Photino).

### WebWorker Processing
A limitation of Blazor WebAssembly as of DotNet 9 is limited support around multi threading. Apollo unfortunately has a lot of blocking code because of its requirements to compile code, run code analysis, dynamically execute assemblies, emulate a webhost, and more. 

There are also always inherent risks to executing scripts/code dynamically.

WebWorkers can help address both issues by offloading blocking work to other processes. If something goes wrong with a worker we can just create a new one.

### Compilation
The compilation service compiles and executes c# code that is a script or executable format.

### Code Analysis
The code analysis service helps provide intelligent features to the UI editor based on the current open solution. 

### Hosting
The hosting services help us create a development experience around modern web workloads. Allows running Web API projects and emulating http communication from the UI and the Worker service.