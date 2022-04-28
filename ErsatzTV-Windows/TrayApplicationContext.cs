using ErsatzTV.Core;
using System.Diagnostics;
using CliWrap;

namespace ErsatzTV_Windows;

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly CancellationTokenSource _tokenSource;

    public TrayApplicationContext()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = new Icon("./Ersatztv.ico"),
            ContextMenuStrip = new ContextMenuStrip(),
            Visible = true
        };

        _tokenSource = new CancellationTokenSource();

        AddMenuItem("Launch Web UI", LaunchWebUI);
        AddMenuItem("Show Logs", ShowLogs);
        _trayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
        AddMenuItem("Exit", Exit);

        string folder = AppContext.BaseDirectory;
        string exe = Path.Combine(folder, "ErsatzTV.exe");

        if (File.Exists(exe))
        {

            Cli.Wrap(exe)
                .WithWorkingDirectory(folder)
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync(_tokenSource.Token);
        }
    }

    private void AddMenuItem(string name, EventHandler action)
    {
        var item = new ToolStripMenuItem(name);
        item.Click += action;
        _trayIcon.ContextMenuStrip.Items.Add(item);
    }

    private void LaunchWebUI(object? sender, EventArgs e)
    {
        var process = new Process();
        process.StartInfo.UseShellExecute = true;
        process.StartInfo.FileName = "http://localhost:8409";
        process.Start();
    }

    private void ShowLogs(object? sender, EventArgs e)
    {
        if (!Directory.Exists(FileSystemLayout.LogsFolder))
        {
            Directory.CreateDirectory(FileSystemLayout.LogsFolder);
        }

        var process = new Process();
        process.StartInfo.UseShellExecute = true;
        process.StartInfo.FileName = FileSystemLayout.LogsFolder;
        process.Start();
    }

    protected override void Dispose(bool disposing)
    {
        _tokenSource?.Cancel();
        base.Dispose(disposing);
    }

    private void Exit(object? sender, EventArgs e)
    {
        // Hide tray icon, otherwise it will remain shown until user mouses over it
        _trayIcon.Visible = false;
        Application.Exit();
    }
}
