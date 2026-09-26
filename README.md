# Volatility POC

This POC demonstrates a WinUI 3 desktop client that calls a local ComputeHost gRPC server (loopback) to compute a volatility surface. The compute engine is mocked. The client supports two chart rendering modes (WebView2 Chart.js and native Win2D) selectable from a Settings dialog.

Prerequisites
- .NET 10 SDK
- Visual Studio 2022/2023 with WinUI 3 / Windows App SDK support OR the appropriate .NET/WinUI toolchain
- WebView2 runtime (Evergreen) installed for the Web chart mode
- If Win2D package varies for your WinUI/Win2D combo, adjust csproj package versions.

Solution layout
- src/Shared: gRPC proto & generated C# types
- src/ComputeHost: ASP.NET Core console app (gRPC server + market quote REST stub + mock quant engine)
- src/WinUIClient: WinUI 3 desktop app with WebView2 + Win2D chart options, Settings dialog, and ViewModel.

Build & run (quick)
1. Build Shared project first to generate gRPC code:
   dotnet build src/Shared

2. Start ComputeHost:
   dotnet run --project src/ComputeHost
   The server listens on http://127.0.0.1:5001 and exposes:
   - gRPC ComputeService at that address (HTTP/2)
   - REST market quote stub at /api/marketquote/{instrument}

3. Start the WinUI client (from Visual Studio or CLI):
   dotnet run --project src/WinUIClient

4. In the client:
   - Click Settings to choose chart mode (Web or Native)
   - Choose an instrument and click "Fetch Quotes & Compute"
   - Observe streaming progress and the chart updating

Swap mock to real managed wrapper
- Implement IQuantEngine in a ManagedWrapperAdapter class that calls your existing managed C++ wrapper.
- Register it in ComputeHost Program.cs replacing MockQuantEngine:
  builder.Services.AddSingleton<IQuantEngine, ManagedWrapperAdapter>();

Notes
- POC uses gRPC server-streaming for progress updates.
- Current transport is loopback HTTP/2 for simplicity; you can later switch to named-pipes for local-only transport if desired.
- ComputeHost is a console ASP.NET Core app for dev agility; convert to Windows Service when packaging for production.
