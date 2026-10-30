using System;using System.Collections.Generic;using System.Diagnostics;using System.Globalization;using System.IO;using System.Linq;using System.Runtime.InteropServices;using System.Text;using System.Text.RegularExpressions;
namespace DragonCompanion {
public sealed class HostSnapshot {public string Identity="";public int Pid;public int VisibleWindows;public DateTime StartedUtc;}
public static class DesktopHost {
 delegate bool EnumProc(IntPtr h,IntPtr data);
 [DllImport("user32.dll")]static extern bool EnumWindows(EnumProc callback,IntPtr data);
 [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")]static extern bool IsIconic(IntPtr h);
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr h,StringBuilder b,int n);
 public static HostSnapshot Scan(){var result=new HostSnapshot();var roots=new List<HostSnapshot>();try{foreach(var p in Process.GetProcessesByName("ChatGPT")){using(p){try{if(p.MainWindowHandle==IntPtr.Zero)continue;string exe=p.MainModule.FileName;if(exe.IndexOf("OpenAI.Codex_",StringComparison.OrdinalIgnoreCase)<0)continue;roots.Add(new HostSnapshot{Pid=p.Id,Identity=p.Id+"/"+exe,StartedUtc=p.StartTime.ToUniversalTime()});}catch{}}}if(roots.Count!=1)return result;result=roots[0];EnumWindows((h,d)=>{uint pid;GetWindowThreadProcessId(h,out pid);if(pid!=result.Pid||!IsWindowVisible(h)||IsIconic(h))return true;var c=new StringBuilder(128);GetClassName(h,c,c.Capacity);if(c.ToString()=="Chrome_WidgetWin_1")result.VisibleWindows++;return true;},IntPtr.Zero);}catch{}return result;}
}
// Observe existing application logs. Never enables logging or opens a database for writing.
public sealed class AutoFollow {
 public string ThreadId{get;private set;}public string Hint{get;private set;}public string Source{get{return "desktop-route-log";}}
 string identity="",path="",partial="";long position;DateTime nextDiscovery=DateTime.MinValue;readonly string[] roots;
 static readonly Regex route=new Regex(@"(?:^|\s)ownerRoutePath=(\S+)",RegexOptions.CultureInvariant);
 static readonly Regex id=new Regex(@"^/local/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})(?:[/?#].*)?$",RegexOptions.CultureInvariant);
 public AutoFollow(){string local=Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);roots=new[]{Path.Combine(local,@"Packages\OpenAI.Codex_2p2nqsd0c76g0\LocalCache\Local\Codex\Logs"),Path.Combine(local,@"Codex\Logs")};Hint="等待侧栏聊天";}
 public static bool Parse(string line,out string thread){thread=null;if(line==null||line.Length>32768||!line.Contains("[electron-message-handler] IAB_LIFECYCLE received browser sidebar owner sync "))return false;
  // Only the verified primary surface is supported; auxiliary/multi-window ambiguity pauses auto-follow.
  if(!Regex.IsMatch(line,@"\boriginWebContentsId=1(?:\s|$)")||!Regex.IsMatch(line,@"\bwindowId=1(?:\s|$)"))return true;
  var m=route.Match(line);if(!m.Success)return true;var match=id.Match(m.Groups[1].Value);if(match.Success)thread=match.Groups[1].Value.ToLowerInvariant();return true;}
 public void Update(HostSnapshot host){try{if(identity!=host.Identity){identity=host.Identity;path="";partial="";position=0;ThreadId=null;nextDiscovery=DateTime.MinValue;}if(host.Pid==0){ThreadId=null;Hint="等待桌面端";return;}if(host.VisibleWindows!=1){if(host.VisibleWindows>1)ThreadId=null;Hint=host.VisibleWindows>1?"多窗口 · 自动跟随暂停":"等待桌面端显示";return;}
  if(DateTime.UtcNow>=nextDiscovery){nextDiscovery=DateTime.UtcNow.AddSeconds(4);var found=new List<FileInfo>();foreach(string root in roots){for(int day=0;day<2;day++){DateTime date=DateTime.UtcNow.AddDays(-day);string dir=Path.Combine(root,date.ToString("yyyy"),date.ToString("MM"),date.ToString("dd"));if(!Directory.Exists(dir))continue;foreach(string file in Directory.GetFiles(dir,"codex-desktop-*-"+host.Pid+"-t0-*.log")){var info=new FileInfo(file);if(info.LastWriteTimeUtc>=host.StartedUtc.AddSeconds(-5))found.Add(info);}}}var newest=found.OrderByDescending(f=>f.LastWriteTimeUtc).FirstOrDefault();if(newest!=null&&newest.FullName!=path){path=newest.FullName;position=0;partial="";}}
  if(path.Length==0){ThreadId=null;Hint="未识别到兼容的桌面状态";return;}
  using(var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)){if(f.Length<position){position=0;partial="";ThreadId=null;}long available=f.Length-position;if(available==0){Hint=ThreadId==null?"等待侧栏聊天信号":"自动跟随";return;}
   int cap=position==0?2097152:131072;if(available>cap){position=f.Length-cap;partial="";ThreadId=null;}f.Position=position;var buffer=new byte[(int)Math.Min(cap,f.Length-position)];int n=f.Read(buffer,0,buffer.Length);position+=n;string text=partial+Encoding.UTF8.GetString(buffer,0,n);int end=text.LastIndexOf('\n');if(end<0){partial=text.Length<32768?text:"";return;}partial=text.Substring(end+1);foreach(string line in text.Substring(0,end).Split('\n')){string thread;if(Parse(line,out thread)){DateTime timestamp;if(line.Length<24||!DateTime.TryParse(line.Substring(0,24),CultureInfo.InvariantCulture,DateTimeStyles.AdjustToUniversal|DateTimeStyles.AssumeUniversal,out timestamp)||timestamp<host.StartedUtc.AddSeconds(-5))continue;ThreadId=thread;}}}
  Hint=ThreadId==null?"当前页面不支持自动跟随":"自动跟随";
 }catch{ThreadId=null;Hint="桌面状态暂不可读 · 自动重试";nextDiscovery=DateTime.MinValue;}}
}
}
