# Fluent shell and theme

The WinUI client now includes a small reusable Fluent-oriented design system:

- `Resources/ThemeColors.xaml`: application color and surface tokens.
- `Resources/ThemeControls.xaml`: reusable cards, buttons, captions, and headings.
- `Resources/ThemeTypography.xaml`: shared typography defaults.
- `MainWindow.xaml`: redesigned workbench shell with a header, command bar, progress/status cards, chart workspace, and footer.

The design intentionally uses WinUI `ThemeResource` values for system surfaces and text, so the shell follows the operating system light/dark theme while keeping application accent colors consistent.

The chart mode remains selected from the existing Settings dialog. No application behavior or gRPC contracts were changed by the visual refresh.
