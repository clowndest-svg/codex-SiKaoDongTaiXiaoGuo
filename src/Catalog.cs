using System;using System.Collections.Generic;using System.IO;using System.Linq;using System.Runtime.InteropServices;using System.Text;using System.Text.RegularExpressions;
namespace DragonCompanion {
public sealed class ThreadEntry {public string Id;public string Title;public override string ToString(){return Title;}}
public static class Catalog {
 [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_open_v2(byte[] p,out IntPtr db,int flags,IntPtr vfs);
 [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_close(IntPtr db);
 [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_prepare_v2(IntPtr db,byte[] sql,int n,out IntPtr statement,IntPtr tail);
 [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_step(IntPtr statement);
 [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_finalize(IntPtr statement);
 [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern IntPtr sqlite3_column_text(IntPtr statement,int col);
 [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_column_bytes(IntPtr statement,int col);
 [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_db_readonly(IntPtr db,byte[] schema);
 [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_busy_timeout(IntPtr db,int ms);
 static byte[] Utf8(string s){return Encoding.UTF8.GetBytes(s+"\0");}
 static string Text(IntPtr st,int col){IntPtr p=sqlite3_column_text(st,col);if(p==IntPtr.Zero)return "";int len=sqlite3_column_bytes(st,col);var b=new byte[len];Marshal.Copy(p,b,0,len);return Encoding.UTF8.GetString(b);}
 public static List<ThreadEntry> Threads(string home){var result=new List<ThreadEntry>();IntPtr db=IntPtr.Zero,st=IntPtr.Zero;try{if(sqlite3_open_v2(Utf8(Path.Combine(home,"state_5.sqlite")),out db,1,IntPtr.Zero)!=0)throw new IOException("无法只读打开聊天目录");if(sqlite3_db_readonly(db,Utf8("main"))!=1)throw new IOException("目录只读校验失败");sqlite3_busy_timeout(db,120);string sql="SELECT id,COALESCE(NULLIF(name,''),NULLIF(title,''),'未命名聊天') FROM threads WHERE archived=0 AND (thread_source='user' OR thread_source IS NULL) AND attached_thread_parent_id IS NULL ORDER BY updated_at DESC LIMIT 40";if(sqlite3_prepare_v2(db,Utf8(sql),-1,out st,IntPtr.Zero)!=0)throw new IOException("聊天目录格式已变化");int rc;while((rc=sqlite3_step(st))==100){string id=Text(st,0);if(Regex.IsMatch(id,@"^[0-9a-fA-F-]{36}$")){string title=Text(st,1).Replace("\r"," ").Replace("\n"," ");result.Add(new ThreadEntry{Id=id,Title=title.Length>44?title.Substring(0,44)+"…":title});}}if(rc!=101)throw new IOException("聊天目录读取忙碌");return result;}finally{if(st!=IntPtr.Zero)sqlite3_finalize(st);if(db!=IntPtr.Zero)sqlite3_close(db);}}
 public static string DefaultEffort(string home){try{foreach(string line in File.ReadLines(Path.Combine(home,"config.toml"))){string t=line.Trim();if(t.StartsWith("["))break;var m=Regex.Match(t,"^model_reasoning_effort\\s*=\\s*\"([a-z]+)\"");if(m.Success&&DisplayState.Known(m.Groups[1].Value))return m.Groups[1].Value;}}catch{}return null;}
}
public static class DisplayState {
 public static bool Known(string e){return new[]{"none","minimal","low","medium","high","xhigh","max","ultra"}.Contains(e);}
 public static string Form(string e){if(!Known(e))return "unknown";return new[]{"none","minimal","low"}.Contains(e)?"chibi":e=="high"?"full":e;}
 public static string Label(string e){switch(e){case "none":return "即时";case "minimal":return "轻度";case "low":return "低";case "medium":return "中等";case "high":return "高";case "xhigh":return "极高";case "max":return "Max";case "ultra":return "Ultra";default:return "待同步";}}
 public static string FormLabel(string e){switch(Form(e)){case "chibi":return "Q 版";case "full":return "全身";case "medium":return "持书";case "xhigh":return "施法";case "max":return "龙后";case "ultra":return "大翅膀";default:return "待同步";}}
}
}
