// ═══════════════════════════════════════════════════════════════════════
//  Ted's PC Tweaker — TweakEngine
//  Developed by tedisanxd
// ═══════════════════════════════════════════════════════════════════════
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text;

namespace TedsPCTweaker.Services;

public enum TweakCategory { Performance, Privacy, Gaming, Network, Cleanup, Display, Security }
public enum TweakRisk    { Safe, Low, Medium, High }

public class TweakResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool RequiresReboot { get; init; }
}

public class TweakDefinition
{
    public string  Id             { get; init; } = string.Empty;
    public string  Title          { get; init; } = string.Empty;
    public string  Description    { get; init; } = string.Empty;
    public TweakCategory Category { get; init; }
    public TweakRisk     Risk     { get; init; }
    public bool RequiresReboot    { get; init; }
    public bool IsCleanupAction   { get; init; } // one-shot, no revert
}

public static class TweakEngine
{
    // ─────────────────────────────────────────────────────────────────
    //  Tweak catalogue
    // ─────────────────────────────────────────────────────────────────
    public static readonly IReadOnlyList<TweakDefinition> AllTweaks = new List<TweakDefinition>
    {
        // ── PERFORMANCE ──────────────────────────────────────────────
        new() {
            Id = "perf_powerplan",
            Title = "Ultimate Performance Power Plan",
            Description = "Activates the hidden Ultimate Performance power plan for maximum CPU/GPU performance. Falls back to High Performance if not available.",
            Category = TweakCategory.Performance, Risk = TweakRisk.Low
        },
        new() {
            Id = "perf_visualfx",
            Title = "Disable Visual Animations",
            Description = "Adjusts Windows visual effects to best performance — removes window animations, shadows, and transitions for snappier UI response.",
            Category = TweakCategory.Performance, Risk = TweakRisk.Safe
        },
        new() {
            Id = "perf_sysmain",
            Title = "Disable SysMain (SuperFetch)",
            Description = "Stops and disables the SysMain service. Reduces disk activity on SSDs where prefetching provides no benefit.",
            Category = TweakCategory.Performance, Risk = TweakRisk.Low, RequiresReboot = true
        },
        new() {
            Id = "perf_wsearch",
            Title = "Disable Windows Search Indexing",
            Description = "Stops the Windows Search service. Frees CPU/disk resources. Use Windows Search box less frequently if enabled.",
            Category = TweakCategory.Performance, Risk = TweakRisk.Low, RequiresReboot = true
        },
        new() {
            Id = "perf_hags",
            Title = "Hardware GPU Scheduling (HAGS)",
            Description = "Enables HAGS, letting the GPU manage its own memory scheduling. Reduces CPU overhead and can improve frame times.",
            Category = TweakCategory.Performance, Risk = TweakRisk.Low, RequiresReboot = true
        },
        new() {
            Id = "perf_ntfs",
            Title = "NTFS: Disable Last Access Time",
            Description = "Stops Windows updating the 'last accessed' timestamp on every file read. Reduces unnecessary disk writes on SSDs.",
            Category = TweakCategory.Performance, Risk = TweakRisk.Safe, RequiresReboot = false
        },
        new() {
            Id = "perf_procpriority",
            Title = "Prioritise Foreground Programs",
            Description = "Sets the processor scheduling policy to give foreground applications maximum CPU priority over background tasks.",
            Category = TweakCategory.Performance, Risk = TweakRisk.Safe
        },
        new() {
            Id = "perf_startupdelay",
            Title = "Remove Startup Delay",
            Description = "Eliminates the built-in 10-second delay before Windows loads startup programs, so your apps launch faster after boot.",
            Category = TweakCategory.Performance, Risk = TweakRisk.Safe
        },
        new() {
            Id = "perf_hibernate",
            Title = "Disable Hibernation",
            Description = "Turns off hibernation and removes the hiberfil.sys file, freeing gigabytes of disk space equal to your RAM size.",
            Category = TweakCategory.Performance, Risk = TweakRisk.Medium
        },

        // ── PRIVACY ──────────────────────────────────────────────────
        new() {
            Id = "priv_telemetry",
            Title = "Disable Telemetry & Data Collection",
            Description = "Blocks Microsoft from collecting diagnostic and usage data. Disables the DiagTrack and dmwappushservice background services.",
            Category = TweakCategory.Privacy, Risk = TweakRisk.Low, RequiresReboot = true
        },
        new() {
            Id = "priv_cortana",
            Title = "Disable Cortana",
            Description = "Prevents Cortana from running or collecting data. Cortana will no longer load in the background.",
            Category = TweakCategory.Privacy, Risk = TweakRisk.Safe
        },
        new() {
            Id = "priv_adid",
            Title = "Disable Advertising ID",
            Description = "Stops Windows assigning you a unique advertising ID that tracks you across apps for targeted ads.",
            Category = TweakCategory.Privacy, Risk = TweakRisk.Safe
        },
        new() {
            Id = "priv_activity",
            Title = "Disable Activity History & Timeline",
            Description = "Disables Windows Timeline and prevents your app/browsing activity from being stored and sent to Microsoft.",
            Category = TweakCategory.Privacy, Risk = TweakRisk.Safe
        },
        new() {
            Id = "priv_location",
            Title = "Disable Location Tracking",
            Description = "Denies all apps access to your device's location by default.",
            Category = TweakCategory.Privacy, Risk = TweakRisk.Safe
        },
        new() {
            Id = "priv_wer",
            Title = "Disable Windows Error Reporting",
            Description = "Stops crash dumps and error reports from being sent to Microsoft.",
            Category = TweakCategory.Privacy, Risk = TweakRisk.Safe
        },

        // ── GAMING ───────────────────────────────────────────────────
        new() {
            Id = "game_gamemode",
            Title = "Enable Game Mode",
            Description = "Activates Windows Game Mode, which prioritises game processes and suspends Windows Update delivery optimisation during gameplay.",
            Category = TweakCategory.Gaming, Risk = TweakRisk.Safe
        },
        new() {
            Id = "game_gamebar",
            Title = "Disable Xbox Game Bar & DVR",
            Description = "Disables the Xbox overlay (Win+G) and background game capture. Removes significant CPU/GPU overhead during gaming.",
            Category = TweakCategory.Gaming, Risk = TweakRisk.Safe
        },
        new() {
            Id = "game_fso",
            Title = "Disable Fullscreen Optimisations",
            Description = "Forces games to run in true exclusive fullscreen mode globally, reducing input latency compared to the FSO compatibility layer.",
            Category = TweakCategory.Gaming, Risk = TweakRisk.Low
        },
        new() {
            Id = "game_gpuprio",
            Title = "GPU & Multimedia Scheduling Priority",
            Description = "Sets the Games multimedia task to High scheduling priority and raises GPU priority to 8. Improves frame pacing.",
            Category = TweakCategory.Gaming, Risk = TweakRisk.Low, RequiresReboot = true
        },
        new() {
            Id = "game_mouseaccel",
            Title = "Disable Mouse Acceleration",
            Description = "Turns off 'Enhance Pointer Precision', giving 1:1 mouse-to-cursor mapping essential for FPS and competitive games.",
            Category = TweakCategory.Gaming, Risk = TweakRisk.Safe
        },
        new() {
            Id = "game_transparency",
            Title = "Disable Transparency Effects",
            Description = "Disables the frosted-glass transparency on taskbar and Start menu. Reduces GPU load and VRAM usage during gaming.",
            Category = TweakCategory.Gaming, Risk = TweakRisk.Safe
        },

        // ── NETWORK ──────────────────────────────────────────────────
        new() {
            Id = "net_nagle",
            Title = "Disable Nagle's Algorithm",
            Description = "Sets TCPNoDelay and TcpAckFrequency=1 on all adapters. Reduces latency (ping) in online games by sending packets immediately.",
            Category = TweakCategory.Network, Risk = TweakRisk.Low
        },
        new() {
            Id = "net_tcp",
            Title = "Optimise TCP Global Settings",
            Description = "Enables TCP Fast Open, Receive Side Scaling, and sets auto-tuning to Normal for better throughput and lower latency.",
            Category = TweakCategory.Network, Risk = TweakRisk.Low
        },
        new() {
            Id = "net_throttle",
            Title = "Disable Network Throttling",
            Description = "Removes the 10-packet-per-ms network throttle Windows applies to non-multimedia apps. Improves game and download performance.",
            Category = TweakCategory.Network, Risk = TweakRisk.Low
        },
        new() {
            Id = "net_rss",
            Title = "Enable Receive Side Scaling (RSS)",
            Description = "Distributes network processing across multiple CPU cores, reducing single-core bottlenecks for high-bandwidth connections.",
            Category = TweakCategory.Network, Risk = TweakRisk.Safe
        },

        // ── CLEANUP ──────────────────────────────────────────────────
        new() {
            Id = "clean_temp",
            Title = "Clear User Temp Files",
            Description = "Deletes all files from %TEMP%. Frees disk space and can speed up some programs.",
            Category = TweakCategory.Cleanup, Risk = TweakRisk.Safe, IsCleanupAction = true
        },
        new() {
            Id = "clean_wtemp",
            Title = "Clear Windows Temp Folder",
            Description = "Deletes files from C:\\Windows\\Temp. Safe to remove — Windows recreates what it needs.",
            Category = TweakCategory.Cleanup, Risk = TweakRisk.Safe, IsCleanupAction = true
        },
        new() {
            Id = "clean_recycle",
            Title = "Empty Recycle Bin",
            Description = "Permanently deletes everything in the Recycle Bin to free disk space.",
            Category = TweakCategory.Cleanup, Risk = TweakRisk.Safe, IsCleanupAction = true
        },
        new() {
            Id = "clean_dns",
            Title = "Flush DNS Cache",
            Description = "Clears the DNS resolver cache. Useful after changing DNS servers or fixing connection issues.",
            Category = TweakCategory.Cleanup, Risk = TweakRisk.Safe, IsCleanupAction = true
        },
        new() {
            Id = "clean_prefetch",
            Title = "Clear Prefetch Files",
            Description = "Removes prefetch (.pf) files. Windows will rebuild them on next boot. Minor disk space recovery.",
            Category = TweakCategory.Cleanup, Risk = TweakRisk.Safe, IsCleanupAction = true
        },
        new() {
            Id = "clean_wucache",
            Title = "Clear Windows Update Cache",
            Description = "Stops Windows Update and deletes downloaded update files. Frees significant disk space. Updates will re-download as needed.",
            Category = TweakCategory.Cleanup, Risk = TweakRisk.Low, IsCleanupAction = true
        },
        new() {
            Id = "clean_eventlogs",
            Title = "Clear Windows Event Logs",
            Description = "Wipes all Windows event logs (Application, System, Security). Frees disk space and gives a clean baseline for troubleshooting.",
            Category = TweakCategory.Cleanup, Risk = TweakRisk.Safe, IsCleanupAction = true
        },
        new() {
            Id = "clean_thumbcache",
            Title = "Clear Thumbnail Cache",
            Description = "Deletes Explorer thumbnail database files. They rebuild automatically next time folders are browsed.",
            Category = TweakCategory.Cleanup, Risk = TweakRisk.Safe, IsCleanupAction = true
        },

        // ── EXTRA PERFORMANCE ─────────────────────────────────────────
        new() {
            Id = "perf_pagingexec",
            Title = "Keep Kernel in RAM (Disable Paging Executive)",
            Description = "Forces Windows kernel code and drivers to remain in physical RAM rather than being swapped to disk. Improves system responsiveness.",
            Category = TweakCategory.Performance, Risk = TweakRisk.Medium, RequiresReboot = true
        },
        new() {
            Id = "perf_menudelay",
            Title = "Eliminate Menu Show Delay",
            Description = "Sets the Windows menu popup delay to 0 ms, making context menus and submenus appear instantly on click.",
            Category = TweakCategory.Performance, Risk = TweakRisk.Safe
        },

        // ── EXTRA PRIVACY ─────────────────────────────────────────────
        new() {
            Id = "priv_cloudclipboard",
            Title = "Disable Cloud Clipboard Sync",
            Description = "Prevents clipboard history from syncing to Microsoft's servers. Your clipboard stays local only.",
            Category = TweakCategory.Privacy, Risk = TweakRisk.Safe
        },
        new() {
            Id = "priv_feedback",
            Title = "Disable Feedback Notifications",
            Description = "Stops Windows from showing feedback prompts and sending usage feedback to Microsoft.",
            Category = TweakCategory.Privacy, Risk = TweakRisk.Safe
        },

        // ── EXTRA GAMING ──────────────────────────────────────────────
        new() {
            Id = "game_mpo",
            Title = "Disable Multi-Plane Overlay (MPO)",
            Description = "Disables MPO which causes screen flickering, black flashes and stutter with many GPU/driver combinations.",
            Category = TweakCategory.Gaming, Risk = TweakRisk.Medium, RequiresReboot = true
        },
        new() {
            Id = "game_bg_record",
            Title = "Disable Background Game Recording",
            Description = "Prevents Xbox Game Bar from recording gameplay clips in the background, freeing CPU and disk resources.",
            Category = TweakCategory.Gaming, Risk = TweakRisk.Safe
        },

        // ── EXTRA NETWORK ─────────────────────────────────────────────
        new() {
            Id = "net_qos",
            Title = "Remove QoS Bandwidth Reservation",
            Description = "Removes the 20% bandwidth that Windows QoS reserves by default. Makes full network capacity available to all apps.",
            Category = TweakCategory.Network, Risk = TweakRisk.Low
        },
        new() {
            Id = "net_dns",
            Title = "Set DNS to Cloudflare (1.1.1.1)",
            Description = "Configures all active network adapters to use Cloudflare's 1.1.1.1 and 1.0.0.1 DNS for faster lookups and better privacy.",
            Category = TweakCategory.Network, Risk = TweakRisk.Low
        },

        // ── DISPLAY ───────────────────────────────────────────────────
        new() {
            Id = "disp_cleartype",
            Title = "Enable ClearType Text Rendering",
            Description = "Activates ClearType sub-pixel font rendering for sharper, easier-to-read text on LCD and OLED displays.",
            Category = TweakCategory.Display, Risk = TweakRisk.Safe
        },
        new() {
            Id = "disp_mpo",
            Title = "Disable Multi-Plane Overlay (MPO)",
            Description = "Disables DWM multi-plane overlay. Fixes flickering and black screens seen with certain GPU and driver combinations.",
            Category = TweakCategory.Display, Risk = TweakRisk.Medium, RequiresReboot = true
        },
        new() {
            Id = "disp_cursorshadow",
            Title = "Disable Cursor Drop Shadow",
            Description = "Removes the subtle drop shadow rendered below the mouse cursor. Minor GPU savings and a cleaner cursor appearance.",
            Category = TweakCategory.Display, Risk = TweakRisk.Safe
        },
        new() {
            Id = "disp_vrr",
            Title = "Enable Variable Refresh Rate (VRR)",
            Description = "Enables Windows VRR optimisations for monitors that support FreeSync or G-Sync in both windowed and fullscreen modes.",
            Category = TweakCategory.Display, Risk = TweakRisk.Low, RequiresReboot = true
        },
        new() {
            Id = "disp_taskbarthumbs",
            Title = "Disable Taskbar Live Thumbnails",
            Description = "Sets the thumbnail hover delay to 30 seconds, effectively disabling live window previews on the taskbar. Reduces GPU load.",
            Category = TweakCategory.Display, Risk = TweakRisk.Safe
        },
        new() {
            Id = "disp_menuanim",
            Title = "Disable Menu & Tooltip Animations",
            Description = "Turns off fade/slide animations for menus and tooltips. Makes the UI feel more immediate and snappy.",
            Category = TweakCategory.Display, Risk = TweakRisk.Safe
        },

        // ── SECURITY ──────────────────────────────────────────────────
        new() {
            Id = "sec_smb1",
            Title = "Disable SMBv1 Protocol",
            Description = "Disables the legacy SMBv1 file-sharing protocol — the primary attack vector for WannaCry and NotPetya ransomware.",
            Category = TweakCategory.Security, Risk = TweakRisk.Safe, RequiresReboot = true
        },
        new() {
            Id = "sec_autoplay",
            Title = "Disable AutoPlay & AutoRun",
            Description = "Prevents Windows from automatically executing programs when a USB drive or disc is inserted. Blocks a common malware vector.",
            Category = TweakCategory.Security, Risk = TweakRisk.Safe
        },
        new() {
            Id = "sec_rdp",
            Title = "Disable Remote Desktop (RDP)",
            Description = "Disables the Remote Desktop Protocol listener. Reduces attack surface if you do not use remote access to this PC.",
            Category = TweakCategory.Security, Risk = TweakRisk.Safe
        },
        new() {
            Id = "sec_uac_nodim",
            Title = "UAC: Disable Secure Desktop Dimming",
            Description = "Keeps the UAC prompt on the regular desktop without dimming the screen. Retains UAC elevation with less visual interruption.",
            Category = TweakCategory.Security, Risk = TweakRisk.Medium
        },
        new() {
            Id = "sec_defender_cloud",
            Title = "Disable Defender Cloud Protection",
            Description = "Disables Windows Defender cloud-delivered protection and sample submission. Local AV scanning remains fully active.",
            Category = TweakCategory.Security, Risk = TweakRisk.Medium
        },
        new() {
            Id = "sec_spectre",
            Title = "Disable Spectre/Meltdown Mitigations",
            Description = "Disables CPU vulnerability mitigations. Recovers 5–15% performance on older CPUs. Only suitable for isolated offline/gaming systems.",
            Category = TweakCategory.Security, Risk = TweakRisk.High, RequiresReboot = true
        },
    };

    // ─────────────────────────────────────────────────────────────────
    //  Apply / Revert dispatcher
    // ─────────────────────────────────────────────────────────────────
    public static TweakResult Apply(string id) => id switch
    {
        "perf_powerplan"    => ApplyPowerPlan(),
        "perf_visualfx"     => ApplyVisualFX(),
        "perf_sysmain"      => SetService("SysMain",  start: false),
        "perf_wsearch"      => SetService("WSearch",  start: false),
        "perf_hags"         => SetReg(Registry.LocalMachine,
                                   @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
                                   "HwSchMode", 2),
        "perf_ntfs"         => ApplyNTFS(),
        "perf_procpriority" => SetReg(Registry.LocalMachine,
                                   @"SYSTEM\CurrentControlSet\Control\PriorityControl",
                                   "Win32PrioritySeparation", 38),
        "perf_startupdelay" => SetReg(Registry.CurrentUser,
                                   @"Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize",
                                   "StartupDelayInMSec", 0),
        "perf_hibernate"    => RunCmd("powercfg", "/hibernate off"),

        "priv_telemetry"    => ApplyTelemetry(),
        "priv_cortana"      => SetReg(Registry.LocalMachine,
                                   @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                                   "AllowCortana", 0),
        "priv_adid"         => SetReg(Registry.CurrentUser,
                                   @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo",
                                   "Enabled", 0),
        "priv_activity"     => ApplyActivityHistory(),
        "priv_location"     => SetReg(Registry.LocalMachine,
                                   @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location",
                                   "Value", "Deny"),
        "priv_wer"          => ApplyWER(),

        "game_gamemode"     => ApplyGameMode(),
        "game_gamebar"      => ApplyGameBar(),
        "game_fso"          => ApplyFSO(),
        "game_gpuprio"      => ApplyGpuPriority(),
        "game_mouseaccel"   => ApplyMouseAccel(),
        "game_transparency" => SetReg(Registry.CurrentUser,
                                   @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                                   "EnableTransparency", 0),

        "net_nagle"         => ApplyNagle(),
        "net_tcp"           => ApplyTCP(),
        "net_throttle"      => ApplyNetThrottle(),
        "net_rss"           => RunCmd("netsh", "int tcp set global rss=enabled"),

        "clean_temp"        => CleanTemp(),
        "clean_wtemp"       => CleanWindowsTemp(),
        "clean_recycle"     => CleanRecycle(),
        "clean_dns"         => RunCmd("ipconfig", "/flushdns"),
        "clean_prefetch"    => CleanFolder(@"C:\Windows\Prefetch"),
        "clean_wucache"     => CleanWUCache(),
        "clean_eventlogs"   => CleanEventLogs(),
        "clean_thumbcache"  => CleanThumbCache(),

        "perf_pagingexec"   => SetReg(Registry.LocalMachine,
                                   @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
                                   "DisablePagingExecutive", 1),
        "perf_menudelay"    => SetReg(Registry.CurrentUser,
                                   @"Control Panel\Desktop",
                                   "MenuShowDelay", "0"),

        "priv_cloudclipboard" => ApplyCloudClipboard(),
        "priv_feedback"       => ApplyFeedback(),

        "game_mpo"          => SetReg(Registry.LocalMachine,
                                   @"SOFTWARE\Microsoft\Windows\Dwm",
                                   "OverlayTestMode", 5),
        "game_bg_record"    => SetReg(Registry.CurrentUser,
                                   @"Software\Microsoft\Windows\CurrentVersion\GameDVR",
                                   "HistoricalCaptureEnabled", 0),

        "net_qos"           => SetReg(Registry.LocalMachine,
                                   @"SOFTWARE\Policies\Microsoft\Windows\Psched",
                                   "NonBestEffortLimit", 0),
        "net_dns"           => ApplyDNS(),

        "disp_cleartype"    => ApplyClearType(),
        "disp_mpo"          => SetReg(Registry.LocalMachine,
                                   @"SOFTWARE\Microsoft\Windows\Dwm",
                                   "OverlayTestMode", 5),
        "disp_cursorshadow" => SetReg(Registry.CurrentUser,
                                   @"Control Panel\Desktop",
                                   "CursorShadow", "0"),
        "disp_vrr"          => SetReg(Registry.LocalMachine,
                                   @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
                                   "VrrOptimizeEnable", 1),
        "disp_taskbarthumbs"=> SetReg(Registry.CurrentUser,
                                   @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                                   "ExtendedUIHoverTime", 30000),
        "disp_menuanim"     => ApplyMenuAnim(),

        "sec_smb1"          => SetReg(Registry.LocalMachine,
                                   @"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters",
                                   "SMB1", 0),
        "sec_autoplay"      => SetReg(Registry.LocalMachine,
                                   @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                                   "NoDriveTypeAutoRun", 0xFF),
        "sec_rdp"           => SetReg(Registry.LocalMachine,
                                   @"SYSTEM\CurrentControlSet\Control\Terminal Server",
                                   "fDenyTSConnections", 1),
        "sec_uac_nodim"     => SetReg(Registry.LocalMachine,
                                   @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
                                   "PromptOnSecureDesktop", 0),
        "sec_defender_cloud"=> ApplyDefenderCloud(),
        "sec_spectre"       => ApplySpectreMitigations(),

        _ => new TweakResult { Success = false, Message = $"Unknown tweak: {id}" }
    };

    public static TweakResult Revert(string id) => id switch
    {
        "perf_powerplan"    => RunCmd("powercfg", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e"),
        "perf_visualfx"     => RevertVisualFX(),
        "perf_sysmain"      => SetService("SysMain",  start: true),
        "perf_wsearch"      => SetService("WSearch",  start: true),
        "perf_hags"         => SetReg(Registry.LocalMachine,
                                   @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
                                   "HwSchMode", 1),
        "perf_ntfs"         => RevertNTFS(),
        "perf_procpriority" => SetReg(Registry.LocalMachine,
                                   @"SYSTEM\CurrentControlSet\Control\PriorityControl",
                                   "Win32PrioritySeparation", 2),
        "perf_startupdelay" => DeleteValue(Registry.CurrentUser,
                                   @"Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize",
                                   "StartupDelayInMSec"),
        "perf_hibernate"    => RunCmd("powercfg", "/hibernate on"),

        "priv_telemetry"    => RevertTelemetry(),
        "priv_cortana"      => DeleteValue(Registry.LocalMachine,
                                   @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                                   "AllowCortana"),
        "priv_adid"         => SetReg(Registry.CurrentUser,
                                   @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo",
                                   "Enabled", 1),
        "priv_activity"     => RevertActivityHistory(),
        "priv_location"     => SetReg(Registry.LocalMachine,
                                   @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location",
                                   "Value", "Allow"),
        "priv_wer"          => RevertWER(),

        "game_gamemode"     => SetReg(Registry.CurrentUser,
                                   @"Software\Microsoft\GameBar",
                                   "AutoGameModeEnabled", 0),
        "game_gamebar"      => RevertGameBar(),
        "game_fso"          => RevertFSO(),
        "game_gpuprio"      => RevertGpuPriority(),
        "game_mouseaccel"   => RevertMouseAccel(),
        "game_transparency" => SetReg(Registry.CurrentUser,
                                   @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                                   "EnableTransparency", 1),

        "net_nagle"         => RevertNagle(),
        "net_tcp"           => RevertTCP(),
        "net_throttle"      => RevertNetThrottle(),
        "net_rss"           => RunCmd("netsh", "int tcp set global rss=enabled"),
        "net_qos"           => DeleteValue(Registry.LocalMachine,
                                   @"SOFTWARE\Policies\Microsoft\Windows\Psched",
                                   "NonBestEffortLimit"),
        "net_dns"           => RevertDNS(),

        "perf_pagingexec"   => SetReg(Registry.LocalMachine,
                                   @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
                                   "DisablePagingExecutive", 0),
        "perf_menudelay"    => DeleteValue(Registry.CurrentUser,
                                   @"Control Panel\Desktop",
                                   "MenuShowDelay"),

        "priv_cloudclipboard" => RevertCloudClipboard(),
        "priv_feedback"       => RevertFeedback(),

        "game_mpo"          => DeleteValue(Registry.LocalMachine,
                                   @"SOFTWARE\Microsoft\Windows\Dwm",
                                   "OverlayTestMode"),
        "game_bg_record"    => SetReg(Registry.CurrentUser,
                                   @"Software\Microsoft\Windows\CurrentVersion\GameDVR",
                                   "HistoricalCaptureEnabled", 1),

        "disp_cleartype"    => RevertClearType(),
        "disp_mpo"          => DeleteValue(Registry.LocalMachine,
                                   @"SOFTWARE\Microsoft\Windows\Dwm",
                                   "OverlayTestMode"),
        "disp_cursorshadow" => SetReg(Registry.CurrentUser,
                                   @"Control Panel\Desktop",
                                   "CursorShadow", "1"),
        "disp_vrr"          => DeleteValue(Registry.LocalMachine,
                                   @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
                                   "VrrOptimizeEnable"),
        "disp_taskbarthumbs"=> DeleteValue(Registry.CurrentUser,
                                   @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                                   "ExtendedUIHoverTime"),
        "disp_menuanim"     => RevertMenuAnim(),

        "sec_smb1"          => SetReg(Registry.LocalMachine,
                                   @"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters",
                                   "SMB1", 1),
        "sec_autoplay"      => DeleteValue(Registry.LocalMachine,
                                   @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                                   "NoDriveTypeAutoRun"),
        "sec_rdp"           => SetReg(Registry.LocalMachine,
                                   @"SYSTEM\CurrentControlSet\Control\Terminal Server",
                                   "fDenyTSConnections", 0),
        "sec_uac_nodim"     => SetReg(Registry.LocalMachine,
                                   @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
                                   "PromptOnSecureDesktop", 1),
        "sec_defender_cloud"=> RevertDefenderCloud(),
        "sec_spectre"       => RevertSpectreMitigations(),

        _ => new TweakResult { Success = false, Message = $"No revert available for: {id}" }
    };

    // ─────────────────────────────────────────────────────────────────
    //  Check whether a tweak is currently applied
    // ─────────────────────────────────────────────────────────────────
    public static bool IsApplied(string id)
    {
        try
        {
            return id switch
            {
                "perf_powerplan"    => IsUltimatePowerPlanActive(),
                "perf_visualfx"     => GetDword(Registry.CurrentUser,
                                          @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
                                          "VisualFXSetting") == 2,
                "perf_sysmain"      => IsServiceDisabled("SysMain"),
                "perf_wsearch"      => IsServiceDisabled("WSearch"),
                "perf_hags"         => GetDword(Registry.LocalMachine,
                                          @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
                                          "HwSchMode") == 2,
                "perf_ntfs"         => GetDword(Registry.LocalMachine,
                                          @"SYSTEM\CurrentControlSet\Control\FileSystem",
                                          "NtfsDisableLastAccessUpdate") == 1,
                "perf_procpriority" => GetDword(Registry.LocalMachine,
                                          @"SYSTEM\CurrentControlSet\Control\PriorityControl",
                                          "Win32PrioritySeparation") == 38,
                "perf_startupdelay" => GetDword(Registry.CurrentUser,
                                          @"Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize",
                                          "StartupDelayInMSec") == 0,
                "perf_hibernate"    => !HibernationEnabled(),

                "priv_telemetry"    => GetDword(Registry.LocalMachine,
                                          @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                                          "AllowTelemetry") == 0,
                "priv_cortana"      => GetDword(Registry.LocalMachine,
                                          @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                                          "AllowCortana") == 0,
                "priv_adid"         => GetDword(Registry.CurrentUser,
                                          @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo",
                                          "Enabled") == 0,
                "priv_activity"     => GetDword(Registry.LocalMachine,
                                          @"SOFTWARE\Policies\Microsoft\Windows\System",
                                          "EnableActivityFeed") == 0,
                "priv_location"     => GetString(Registry.LocalMachine,
                                          @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location",
                                          "Value") == "Deny",
                "priv_wer"          => GetDword(Registry.LocalMachine,
                                          @"SOFTWARE\Microsoft\Windows\Windows Error Reporting",
                                          "Disabled") == 1,

                "game_gamemode"     => GetDword(Registry.CurrentUser,
                                          @"Software\Microsoft\GameBar",
                                          "AutoGameModeEnabled") == 1,
                "game_gamebar"      => GetDword(Registry.CurrentUser,
                                          @"Software\Microsoft\Windows\CurrentVersion\GameDVR",
                                          "AppCaptureEnabled") == 0,
                "game_fso"          => GetDword(Registry.CurrentUser,
                                          @"System\GameConfigStore",
                                          "GameDVR_FSEBehaviorMode") == 2,
                "game_gpuprio"      => GetDword(Registry.LocalMachine,
                                          @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games",
                                          "GPU Priority") == 8,
                "game_mouseaccel"   => GetString(Registry.CurrentUser,
                                          @"Control Panel\Mouse",
                                          "MouseSpeed") == "0",
                "game_transparency" => GetDword(Registry.CurrentUser,
                                          @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                                          "EnableTransparency") == 0,

                "net_nagle"         => IsNagleDisabled(),
                "net_tcp"           => IsTcpFastOpenEnabled(),
                "net_throttle"      => IsNetThrottleDisabled(),
                "net_rss"           => false,
                "net_qos"           => GetDword(Registry.LocalMachine,
                                          @"SOFTWARE\Policies\Microsoft\Windows\Psched",
                                          "NonBestEffortLimit") == 0,
                "net_dns"           => false, // dynamic query not cached

                "perf_pagingexec"   => GetDword(Registry.LocalMachine,
                                          @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
                                          "DisablePagingExecutive") == 1,
                "perf_menudelay"    => GetString(Registry.CurrentUser,
                                          @"Control Panel\Desktop",
                                          "MenuShowDelay") == "0",

                "priv_cloudclipboard" => GetDword(Registry.LocalMachine,
                                          @"SOFTWARE\Policies\Microsoft\Windows\System",
                                          "AllowClipboardHistory") == 0,
                "priv_feedback"       => GetDword(Registry.LocalMachine,
                                          @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                                          "DoNotShowFeedbackNotifications") == 1,

                "game_mpo"          => GetDword(Registry.LocalMachine,
                                          @"SOFTWARE\Microsoft\Windows\Dwm",
                                          "OverlayTestMode") == 5,
                "game_bg_record"    => GetDword(Registry.CurrentUser,
                                          @"Software\Microsoft\Windows\CurrentVersion\GameDVR",
                                          "HistoricalCaptureEnabled") == 0,

                "disp_cleartype"    => GetDword(Registry.CurrentUser,
                                          @"Control Panel\Desktop",
                                          "FontSmoothingType") == 2,
                "disp_mpo"          => GetDword(Registry.LocalMachine,
                                          @"SOFTWARE\Microsoft\Windows\Dwm",
                                          "OverlayTestMode") == 5,
                "disp_cursorshadow" => GetString(Registry.CurrentUser,
                                          @"Control Panel\Desktop",
                                          "CursorShadow") == "0",
                "disp_vrr"          => GetDword(Registry.LocalMachine,
                                          @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
                                          "VrrOptimizeEnable") == 1,
                "disp_taskbarthumbs"=> GetDword(Registry.CurrentUser,
                                          @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                                          "ExtendedUIHoverTime") == 30000,
                "disp_menuanim"     => GetString(Registry.CurrentUser,
                                          @"Control Panel\Desktop",
                                          "MenuAnimation") == "0",

                "sec_smb1"          => GetDword(Registry.LocalMachine,
                                          @"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters",
                                          "SMB1") == 0,
                "sec_autoplay"      => GetDword(Registry.LocalMachine,
                                          @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                                          "NoDriveTypeAutoRun") == 0xFF,
                "sec_rdp"           => GetDword(Registry.LocalMachine,
                                          @"SYSTEM\CurrentControlSet\Control\Terminal Server",
                                          "fDenyTSConnections") == 1,
                "sec_uac_nodim"     => GetDword(Registry.LocalMachine,
                                          @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
                                          "PromptOnSecureDesktop") == 0,
                "sec_defender_cloud"=> GetDword(Registry.LocalMachine,
                                          @"SOFTWARE\Policies\Microsoft\Windows Defender\Spynet",
                                          "SpynetReporting") == 0,
                "sec_spectre"       => GetDword(Registry.LocalMachine,
                                          @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
                                          "FeatureSettingsOverride") == 3,

                _ => false
            };
        }
        catch { return false; }
    }

    // ─────────────────────────────────────────────────────────────────
    //  Performance implementations
    // ─────────────────────────────────────────────────────────────────
    static TweakResult ApplyPowerPlan()
    {
        var sb = new StringBuilder();
        // Try to activate or create Ultimate Performance
        var p = RunProcess("powercfg", "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61");
        if (p.success)
        {
            // Parse GUID from output
            var line = p.output.Split('\n').FirstOrDefault(l => l.Contains("GUID:") || l.Contains("(")) ?? "";
            var guid = System.Text.RegularExpressions.Regex.Match(line,
                @"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase).Value;
            if (!string.IsNullOrEmpty(guid))
            {
                RunProcess("powercfg", $"-setactive {guid}");
                sb.Append("Ultimate Performance plan activated.");
                return new TweakResult { Success = true, Message = sb.ToString() };
            }
        }
        // Fall back to High Performance
        var r = RunCmd("powercfg", "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
        return new TweakResult { Success = r.Success, Message = r.Success ? "High Performance plan activated." : r.Message };
    }

    static TweakResult ApplyVisualFX()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects");
            key.SetValue("VisualFXSetting", 2, RegistryValueKind.DWord);

            using var desktop = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop");
            desktop.SetValue("DragFullWindows",   "0");
            desktop.SetValue("MenuShowDelay",      "0");
            desktop.SetValue("UserPreferencesMask",
                new byte[] { 0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00 },
                RegistryValueKind.Binary);

            using var dwa = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\DWM");
            dwa.SetValue("EnableAeroPeek",  0, RegistryValueKind.DWord);
            dwa.SetValue("AlwaysHibernateThumbnails", 0, RegistryValueKind.DWord);

            return new TweakResult { Success = true, Message = "Visual effects set to best performance." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult RevertVisualFX()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects");
            key.SetValue("VisualFXSetting", 0, RegistryValueKind.DWord);

            using var desktop = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop");
            desktop.SetValue("DragFullWindows", "1");
            desktop.DeleteValue("MenuShowDelay", false);
            desktop.SetValue("UserPreferencesMask",
                new byte[] { 0x9E, 0x1E, 0x07, 0x80, 0x12, 0x00, 0x00, 0x00 },
                RegistryValueKind.Binary);

            using var dwa = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\DWM");
            dwa.SetValue("EnableAeroPeek", 1, RegistryValueKind.DWord);
            return new TweakResult { Success = true, Message = "Visual effects restored to default." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult ApplyNTFS()
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(
                @"SYSTEM\CurrentControlSet\Control\FileSystem");
            key.SetValue("NtfsDisableLastAccessUpdate", 1, RegistryValueKind.DWord);
            key.SetValue("NtfsDisable8dot3NameCreation", 1, RegistryValueKind.DWord);
            RunCmd("fsutil", "behavior set disablelastaccess 1");
            return new TweakResult { Success = true, Message = "NTFS last-access timestamp disabled." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult RevertNTFS()
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(
                @"SYSTEM\CurrentControlSet\Control\FileSystem");
            key.SetValue("NtfsDisableLastAccessUpdate", 0, RegistryValueKind.DWord);
            key.SetValue("NtfsDisable8dot3NameCreation", 0, RegistryValueKind.DWord);
            RunCmd("fsutil", "behavior set disablelastaccess 0");
            return new TweakResult { Success = true, Message = "NTFS last-access restored." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    // ─────────────────────────────────────────────────────────────────
    //  Privacy implementations
    // ─────────────────────────────────────────────────────────────────
    static TweakResult ApplyTelemetry()
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Policies\Microsoft\Windows\DataCollection");
            key.SetValue("AllowTelemetry", 0, RegistryValueKind.DWord);

            SetService("DiagTrack",        start: false);
            SetService("dmwappushservice", start: false);
            return new TweakResult { Success = true, Message = "Telemetry disabled.", RequiresReboot = true };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult RevertTelemetry()
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Policies\Microsoft\Windows\DataCollection");
            key.SetValue("AllowTelemetry", 3, RegistryValueKind.DWord);
            SetService("DiagTrack",        start: true);
            SetService("dmwappushservice", start: true);
            return new TweakResult { Success = true, Message = "Telemetry restored." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult ApplyActivityHistory()
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Policies\Microsoft\Windows\System");
            key.SetValue("EnableActivityFeed",   0, RegistryValueKind.DWord);
            key.SetValue("PublishUserActivities", 0, RegistryValueKind.DWord);
            key.SetValue("UploadUserActivities",  0, RegistryValueKind.DWord);
            return new TweakResult { Success = true, Message = "Activity history disabled." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult RevertActivityHistory()
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Policies\Microsoft\Windows\System");
            key.SetValue("EnableActivityFeed",    1, RegistryValueKind.DWord);
            key.SetValue("PublishUserActivities",  1, RegistryValueKind.DWord);
            key.SetValue("UploadUserActivities",   1, RegistryValueKind.DWord);
            return new TweakResult { Success = true, Message = "Activity history restored." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult ApplyWER()
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Microsoft\Windows\Windows Error Reporting");
            key.SetValue("Disabled", 1, RegistryValueKind.DWord);
            SetService("WerSvc", start: false);
            return new TweakResult { Success = true, Message = "Windows Error Reporting disabled." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult RevertWER()
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Microsoft\Windows\Windows Error Reporting");
            key.SetValue("Disabled", 0, RegistryValueKind.DWord);
            SetService("WerSvc", start: true);
            return new TweakResult { Success = true, Message = "Windows Error Reporting restored." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    // ─────────────────────────────────────────────────────────────────
    //  Gaming implementations
    // ─────────────────────────────────────────────────────────────────
    static TweakResult ApplyGameMode()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\GameBar");
            key.SetValue("AutoGameModeEnabled",  1, RegistryValueKind.DWord);
            key.SetValue("AllowAutoGameMode",    1, RegistryValueKind.DWord);
            key.SetValue("ShowStartupPanel",     0, RegistryValueKind.DWord);
            return new TweakResult { Success = true, Message = "Game Mode enabled." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult ApplyGameBar()
    {
        try
        {
            using var dvr = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\GameDVR");
            dvr.SetValue("AppCaptureEnabled",   0, RegistryValueKind.DWord);
            dvr.SetValue("GameDVR_Enabled",     0, RegistryValueKind.DWord);

            using var pol = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Policies\Microsoft\Windows\GameDVR");
            pol.SetValue("AllowGameDVR", 0, RegistryValueKind.DWord);

            using var bar = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\GameBar");
            bar.SetValue("UseNexusForGameBarEnabled", 0, RegistryValueKind.DWord);
            return new TweakResult { Success = true, Message = "Xbox Game Bar & DVR disabled." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult RevertGameBar()
    {
        try
        {
            using var dvr = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\GameDVR");
            dvr.SetValue("AppCaptureEnabled", 1, RegistryValueKind.DWord);
            dvr.SetValue("GameDVR_Enabled",   1, RegistryValueKind.DWord);

            using var pol = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Policies\Microsoft\Windows\GameDVR");
            pol.SetValue("AllowGameDVR", 1, RegistryValueKind.DWord);
            return new TweakResult { Success = true, Message = "Xbox Game Bar restored." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult ApplyFSO()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore");
            key.SetValue("GameDVR_FSEBehaviorMode",              2, RegistryValueKind.DWord);
            key.SetValue("GameDVR_HonorUserFSEBehaviorMode",     0, RegistryValueKind.DWord);
            key.SetValue("GameDVR_FSEBehavior",                  2, RegistryValueKind.DWord);
            key.SetValue("GameDVR_DXGIHonorFSEWindowsCompatible",1, RegistryValueKind.DWord);
            key.SetValue("GameDVR_EFSEFeatureFlags",             0, RegistryValueKind.DWord);
            return new TweakResult { Success = true, Message = "Fullscreen Optimisations disabled globally." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult RevertFSO()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore");
            key.SetValue("GameDVR_FSEBehaviorMode",               0, RegistryValueKind.DWord);
            key.SetValue("GameDVR_HonorUserFSEBehaviorMode",      1, RegistryValueKind.DWord);
            key.SetValue("GameDVR_FSEBehavior",                   0, RegistryValueKind.DWord);
            key.SetValue("GameDVR_DXGIHonorFSEWindowsCompatible", 0, RegistryValueKind.DWord);
            return new TweakResult { Success = true, Message = "Fullscreen Optimisations restored." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult ApplyGpuPriority()
    {
        try
        {
            using var prof = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile");
            prof.SetValue("NetworkThrottlingIndex", unchecked((int)0xffffffff), RegistryValueKind.DWord);
            prof.SetValue("SystemResponsiveness",   0, RegistryValueKind.DWord);

            using var games = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games");
            games.SetValue("GPU Priority",         8,      RegistryValueKind.DWord);
            games.SetValue("Priority",             6,      RegistryValueKind.DWord);
            games.SetValue("Scheduling Category", "High",  RegistryValueKind.String);
            games.SetValue("SFIO Priority",        "High", RegistryValueKind.String);
            return new TweakResult { Success = true, Message = "GPU & multimedia priorities set for gaming.", RequiresReboot = true };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult RevertGpuPriority()
    {
        try
        {
            using var prof = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile");
            prof.SetValue("NetworkThrottlingIndex", 10, RegistryValueKind.DWord);
            prof.SetValue("SystemResponsiveness",   20, RegistryValueKind.DWord);

            using var games = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games");
            games.SetValue("GPU Priority",         8,        RegistryValueKind.DWord);
            games.SetValue("Priority",             2,        RegistryValueKind.DWord);
            games.SetValue("Scheduling Category", "Medium",  RegistryValueKind.String);
            games.SetValue("SFIO Priority",        "Normal", RegistryValueKind.String);
            return new TweakResult { Success = true, Message = "GPU & multimedia priorities restored." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult ApplyMouseAccel()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\Mouse");
            key.SetValue("MouseSpeed",      "0");
            key.SetValue("MouseThreshold1", "0");
            key.SetValue("MouseThreshold2", "0");
            return new TweakResult { Success = true, Message = "Mouse acceleration disabled." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult RevertMouseAccel()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\Mouse");
            key.SetValue("MouseSpeed",      "1");
            key.SetValue("MouseThreshold1", "6");
            key.SetValue("MouseThreshold2", "10");
            return new TweakResult { Success = true, Message = "Mouse acceleration restored." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    // ─────────────────────────────────────────────────────────────────
    //  Network implementations
    // ─────────────────────────────────────────────────────────────────
    static TweakResult ApplyNagle()
    {
        try
        {
            var ifaces = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces", writable: true);
            if (ifaces == null) return Fail("Could not open Tcpip Interfaces key.");
            foreach (var name in ifaces.GetSubKeyNames())
            {
                using var sub = ifaces.OpenSubKey(name, writable: true);
                if (sub == null) continue;
                sub.SetValue("TcpAckFrequency", 1, RegistryValueKind.DWord);
                sub.SetValue("TCPNoDelay",      1, RegistryValueKind.DWord);
            }
            return new TweakResult { Success = true, Message = "Nagle's Algorithm disabled on all adapters." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult RevertNagle()
    {
        try
        {
            var ifaces = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces", writable: true);
            if (ifaces == null) return Fail("Could not open Tcpip Interfaces key.");
            foreach (var name in ifaces.GetSubKeyNames())
            {
                using var sub = ifaces.OpenSubKey(name, writable: true);
                if (sub == null) continue;
                sub.DeleteValue("TcpAckFrequency", throwOnMissingValue: false);
                sub.DeleteValue("TCPNoDelay",      throwOnMissingValue: false);
            }
            return new TweakResult { Success = true, Message = "Nagle's Algorithm restored." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult ApplyTCP()
    {
        var results = new[]
        {
            RunCmd("netsh", "int tcp set global fastopen=enabled"),
            RunCmd("netsh", "int tcp set global rss=enabled"),
            RunCmd("netsh", "int tcp set global autotuninglevel=normal"),
            RunCmd("netsh", "int tcp set global timestamps=disabled"),
            RunCmd("netsh", "int tcp set global ecncapability=enabled"),
        };
        bool ok = results.All(r => r.Success);
        return new TweakResult { Success = ok, Message = ok ? "TCP parameters optimised." : "Some TCP settings failed (may already be set)." };
    }

    static TweakResult RevertTCP()
    {
        RunCmd("netsh", "int tcp set global fastopen=disabled");
        RunCmd("netsh", "int tcp set global autotuninglevel=normal");
        RunCmd("netsh", "int tcp set global timestamps=enabled");
        return new TweakResult { Success = true, Message = "TCP parameters restored to defaults." };
    }

    static TweakResult ApplyNetThrottle()
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile");
            key.SetValue("NetworkThrottlingIndex", unchecked((int)0xffffffff), RegistryValueKind.DWord);
            return new TweakResult { Success = true, Message = "Network throttling disabled." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult RevertNetThrottle()
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile");
            key.SetValue("NetworkThrottlingIndex", 10, RegistryValueKind.DWord);
            return new TweakResult { Success = true, Message = "Network throttling restored." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    // ─────────────────────────────────────────────────────────────────
    //  Extra Privacy implementations
    // ─────────────────────────────────────────────────────────────────
    static TweakResult ApplyCloudClipboard()
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Policies\Microsoft\Windows\System");
            key.SetValue("AllowClipboardHistory",      0, RegistryValueKind.DWord);
            key.SetValue("AllowCrossDeviceClipboard",  0, RegistryValueKind.DWord);
            return new TweakResult { Success = true, Message = "Cloud clipboard sync disabled." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult RevertCloudClipboard()
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Policies\Microsoft\Windows\System");
            key.DeleteValue("AllowClipboardHistory",     throwOnMissingValue: false);
            key.DeleteValue("AllowCrossDeviceClipboard", throwOnMissingValue: false);
            return new TweakResult { Success = true, Message = "Cloud clipboard restored." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult ApplyFeedback()
    {
        try
        {
            using var dc = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Policies\Microsoft\Windows\DataCollection");
            dc.SetValue("DoNotShowFeedbackNotifications", 1, RegistryValueKind.DWord);

            using var siuf = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Siuf\Rules");
            siuf.SetValue("NumberOfSIUFInPeriod", 0,  RegistryValueKind.DWord);
            siuf.SetValue("PeriodInNanoSeconds",  0,  RegistryValueKind.DWord);
            return new TweakResult { Success = true, Message = "Feedback notifications disabled." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult RevertFeedback()
    {
        try
        {
            using var dc = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Policies\Microsoft\Windows\DataCollection");
            dc.DeleteValue("DoNotShowFeedbackNotifications", throwOnMissingValue: false);

            using var siuf = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Siuf\Rules");
            siuf.DeleteValue("NumberOfSIUFInPeriod", throwOnMissingValue: false);
            siuf.DeleteValue("PeriodInNanoSeconds",  throwOnMissingValue: false);
            return new TweakResult { Success = true, Message = "Feedback notifications restored." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    // ─────────────────────────────────────────────────────────────────
    //  Extra Network implementations
    // ─────────────────────────────────────────────────────────────────
    static TweakResult ApplyDNS()
    {
        var r = RunCmd("powershell",
            "-NoProfile -Command \"Get-NetAdapter | Where-Object {$_.Status -eq 'Up'} | " +
            "Set-DnsClientServerAddress -ServerAddresses @('1.1.1.1','1.0.0.1')\"");
        return r.Success
            ? new TweakResult { Success = true, Message = "DNS set to Cloudflare 1.1.1.1 on all active adapters." }
            : r;
    }

    static TweakResult RevertDNS()
    {
        var r = RunCmd("powershell",
            "-NoProfile -Command \"Get-NetAdapter | Where-Object {$_.Status -eq 'Up'} | " +
            "Set-DnsClientServerAddress -ResetServerAddresses\"");
        return r.Success
            ? new TweakResult { Success = true, Message = "DNS reset to automatic (DHCP)." }
            : r;
    }

    // ─────────────────────────────────────────────────────────────────
    //  Display implementations
    // ─────────────────────────────────────────────────────────────────
    static TweakResult ApplyClearType()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop");
            key.SetValue("FontSmoothing",            "2");
            key.SetValue("FontSmoothingType",        2,    RegistryValueKind.DWord);
            key.SetValue("FontSmoothingGamma",       1200, RegistryValueKind.DWord);
            key.SetValue("FontSmoothingOrientation", 1,    RegistryValueKind.DWord);
            return new TweakResult { Success = true, Message = "ClearType text rendering enabled." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult RevertClearType()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop");
            key.SetValue("FontSmoothing",     "2");
            key.SetValue("FontSmoothingType", 1, RegistryValueKind.DWord); // standard smoothing
            return new TweakResult { Success = true, Message = "ClearType reverted to standard smoothing." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult ApplyMenuAnim()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop");
            key.SetValue("MenuAnimation",    "0");
            key.SetValue("MenuShowDelay",    "0");
            key.SetValue("TooltipAnimation", "0");
            return new TweakResult { Success = true, Message = "Menu and tooltip animations disabled." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult RevertMenuAnim()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop");
            key.SetValue("MenuAnimation",    "1");
            key.DeleteValue("MenuShowDelay", throwOnMissingValue: false);
            key.SetValue("TooltipAnimation", "1");
            return new TweakResult { Success = true, Message = "Menu animations restored." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    // ─────────────────────────────────────────────────────────────────
    //  Security implementations
    // ─────────────────────────────────────────────────────────────────
    static TweakResult ApplyDefenderCloud()
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Policies\Microsoft\Windows Defender\Spynet");
            key.SetValue("SpynetReporting",       0, RegistryValueKind.DWord);
            key.SetValue("SubmitSamplesConsent",  2, RegistryValueKind.DWord); // Never send
            return new TweakResult { Success = true, Message = "Defender cloud protection disabled." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult RevertDefenderCloud()
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Policies\Microsoft\Windows Defender\Spynet");
            key.DeleteValue("SpynetReporting",      throwOnMissingValue: false);
            key.DeleteValue("SubmitSamplesConsent", throwOnMissingValue: false);
            return new TweakResult { Success = true, Message = "Defender cloud protection restored." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult ApplySpectreMitigations()
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(
                @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management");
            key.SetValue("FeatureSettingsOverride",     3, RegistryValueKind.DWord);
            key.SetValue("FeatureSettingsOverrideMask", 3, RegistryValueKind.DWord);
            return new TweakResult { Success = true, Message = "Spectre/Meltdown mitigations disabled.", RequiresReboot = true };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult RevertSpectreMitigations()
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(
                @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management");
            key.DeleteValue("FeatureSettingsOverride",     throwOnMissingValue: false);
            key.DeleteValue("FeatureSettingsOverrideMask", throwOnMissingValue: false);
            return new TweakResult { Success = true, Message = "Spectre/Meltdown mitigations restored.", RequiresReboot = true };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    // ─────────────────────────────────────────────────────────────────
    //  Cleanup implementations
    // ─────────────────────────────────────────────────────────────────
    static TweakResult CleanTemp()
    {
        var tempPath = Path.GetTempPath();
        return CleanFolder(tempPath);
    }

    static TweakResult CleanWindowsTemp()
        => CleanFolder(@"C:\Windows\Temp");

    static TweakResult CleanRecycle()
    {
        try
        {
            // SHEmptyRecycleBin via shell command
            var r = RunCmd("cmd", "/c rd /s /q C:\\$Recycle.Bin 2>nul & exit 0");
            return new TweakResult { Success = true, Message = "Recycle Bin emptied." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult CleanWUCache()
    {
        try
        {
            RunCmd("net", "stop wuauserv");
            RunCmd("net", "stop bits");
            var path = @"C:\Windows\SoftwareDistribution\Download";
            if (Directory.Exists(path))
            {
                var deleted = 0;
                foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                    TryDelete(f, ref deleted);
                foreach (var d in Directory.EnumerateDirectories(path))
                    TryDeleteDir(d);
            }
            RunCmd("net", "start wuauserv");
            RunCmd("net", "start bits");
            return new TweakResult { Success = true, Message = "Windows Update cache cleared." };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult CleanEventLogs()
    {
        var logs    = new[] { "Application", "System", "Security", "Setup" };
        var cleared = 0;
        foreach (var log in logs)
        {
            var r = RunProcess("wevtutil", $"cl \"{log}\"");
            if (r.success) cleared++;
        }
        return new TweakResult { Success = true, Message = $"Cleared {cleared} event log(s)." };
    }

    static TweakResult CleanThumbCache()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Microsoft\Windows\Explorer");
        var deleted = 0;
        if (Directory.Exists(path))
        {
            foreach (var f in Directory.GetFiles(path, "thumbcache_*.db"))
            {
                try { File.Delete(f); deleted++; } catch { }
            }
            foreach (var f in Directory.GetFiles(path, "iconcache_*.db"))
            {
                try { File.Delete(f); deleted++; } catch { }
            }
        }
        return new TweakResult { Success = true, Message = $"Cleared {deleted} cache file(s). Changes visible after Explorer restart." };
    }

    static TweakResult CleanFolder(string path)
    {
        if (!Directory.Exists(path))
            return new TweakResult { Success = true, Message = $"Folder not found: {path}" };
        var deleted = 0;
        var failed  = 0;
        foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            if (!TryDelete(f, ref deleted)) failed++;
        foreach (var d in Directory.EnumerateDirectories(path))
            TryDeleteDir(d);
        return new TweakResult
        {
            Success = true,
            Message = $"Deleted {deleted} file(s) from {path}.{(failed > 0 ? $" {failed} in use (skipped)." : "")}"
        };
    }

    static bool TryDelete(string path, ref int counter)
    {
        try { File.Delete(path); counter++; return true; }
        catch { return false; }
    }

    static void TryDeleteDir(string path)
    {
        try { Directory.Delete(path, recursive: true); } catch { }
    }

    // ─────────────────────────────────────────────────────────────────
    //  Check helpers
    // ─────────────────────────────────────────────────────────────────
    static bool IsUltimatePowerPlanActive()
    {
        var p = RunProcess("powercfg", "/getactivescheme");
        return p.output.Contains("Ultimate", StringComparison.OrdinalIgnoreCase)
            || p.output.Contains("e9a42b02", StringComparison.OrdinalIgnoreCase);
    }

    static bool HibernationEnabled()
    {
        var r = RunProcess("powercfg", "/a");
        return r.output.Contains("Hibernate", StringComparison.OrdinalIgnoreCase)
            && !r.output.Contains("not available", StringComparison.OrdinalIgnoreCase);
    }

    static bool IsServiceDisabled(string name)
    {
        try
        {
            using var sc = new ServiceController(name);
            return sc.StartType == ServiceStartMode.Disabled;
        }
        catch { return false; }
    }

    static bool IsNagleDisabled()
    {
        try
        {
            var ifaces = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces");
            if (ifaces == null) return false;
            var first = ifaces.GetSubKeyNames().FirstOrDefault();
            if (first == null) return false;
            using var sub = ifaces.OpenSubKey(first);
            return (int)(sub?.GetValue("TCPNoDelay") ?? 0) == 1;
        }
        catch { return false; }
    }

    static bool IsTcpFastOpenEnabled()
    {
        var r = RunProcess("netsh", "int tcp show global");
        return r.output.Contains("enabled", StringComparison.OrdinalIgnoreCase)
            && r.output.Contains("Fast Open", StringComparison.OrdinalIgnoreCase);
    }

    static bool IsNetThrottleDisabled()
    {
        return GetDword(Registry.LocalMachine,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
            "NetworkThrottlingIndex") == unchecked((int)0xffffffff);
    }

    // ─────────────────────────────────────────────────────────────────
    //  Low-level helpers
    // ─────────────────────────────────────────────────────────────────
    static TweakResult SetReg(RegistryKey hive, string path, string name, object value)
    {
        try
        {
            using var key = hive.CreateSubKey(path, writable: true);
            var kind = value is int or uint ? RegistryValueKind.DWord
                     : value is string     ? RegistryValueKind.String
                     : RegistryValueKind.QWord;
            key.SetValue(name, value, kind);
            return new TweakResult { Success = true, Message = $"Set {name} = {value}" };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult DeleteValue(RegistryKey hive, string path, string name)
    {
        try
        {
            using var key = hive.OpenSubKey(path, writable: true);
            key?.DeleteValue(name, throwOnMissingValue: false);
            return new TweakResult { Success = true, Message = $"Removed {name}" };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult SetService(string name, bool start)
    {
        try
        {
            // Registry approach for startup type (more reliable than ServiceController for disabled)
            var regPath = $@"SYSTEM\CurrentControlSet\Services\{name}";
            using var key = Registry.LocalMachine.OpenSubKey(regPath, writable: true);
            if (key != null)
                key.SetValue("Start", start ? 2 : 4, RegistryValueKind.DWord); // 2=auto, 4=disabled

            try
            {
                using var sc = new ServiceController(name);
                if (!start && sc.Status == ServiceControllerStatus.Running)
                    sc.Stop();
                else if (start && sc.Status == ServiceControllerStatus.Stopped)
                    sc.Start();
            }
            catch { /* service might not exist */ }

            return new TweakResult
            {
                Success = true,
                Message = $"Service '{name}' {(start ? "enabled" : "disabled")}.",
                RequiresReboot = true
            };
        }
        catch (Exception ex) { return Fail(ex); }
    }

    static TweakResult RunCmd(string exe, string args)
    {
        var (success, output, error) = RunProcess(exe, args);
        return new TweakResult
        {
            Success = success,
            Message = success ? output.Trim() : $"Error: {error.Trim()}"
        };
    }

    static (bool success, string output, string error) RunProcess(string exe, string args)
    {
        try
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };
            using var p = Process.Start(psi)!;
            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();
            p.WaitForExit(10_000);
            return (p.ExitCode == 0, stdout, stderr);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, ex.Message);
        }
    }

    static int? GetDword(RegistryKey hive, string path, string name)
    {
        try
        {
            using var key = hive.OpenSubKey(path);
            var v = key?.GetValue(name);
            return v is int i ? i : (int?)null;
        }
        catch { return null; }
    }

    static string? GetString(RegistryKey hive, string path, string name)
    {
        try
        {
            using var key = hive.OpenSubKey(path);
            return key?.GetValue(name) as string;
        }
        catch { return null; }
    }

    static TweakResult Fail(Exception ex) =>
        new() { Success = false, Message = ex.Message };
    static TweakResult Fail(string msg) =>
        new() { Success = false, Message = msg };
}
