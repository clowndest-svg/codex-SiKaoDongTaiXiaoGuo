using System;using System.IO;using System.Linq;using System.Reflection;using System.Threading.Tasks;using DragonCompanion;
class ContractTests {
 static int count;static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;}
 static void Main(){try{string first="11111111-1111-4111-8111-111111111111",second="22222222-2222-4222-8222-222222222222",id;
 string prefix="2026-01-01T00:00:00.000Z info [electron-message-handler] IAB_LIFECYCLE received browser sidebar owner sync browserTabId=null conversationId=client-new-thread:synthetic originWebContentsId=1 ownerRoutePath=",suffix=" windowId=1";
 Check(AutoFollow.Parse(prefix+"/local/"+first+suffix,out id)&&id==first,"selected local chat");Check(AutoFollow.Parse(prefix+"/local/"+second+suffix,out id)&&id==second,"switch to cached chat");
 foreach(string path in new[]{"/","/settings","/remote/"+first,"/local/not-a-uuid","/local/"+first+"extra"})Check(AutoFollow.Parse(prefix+path+suffix,out id)&&id==null,"unsupported route must clear target");
 Check(!AutoFollow.Parse("thread_stream_view_activity_changed active=true conversationId="+first,out id),"retained background view is not selection");Check(!AutoFollow.Parse("ownerRoutePath=/local/"+first,out id),"unrelated text ignored");Check(!AutoFollow.Parse(null,out id),"null input");Check(!AutoFollow.Parse(new string('x',32769),out id),"bounded input");
 Check(AutoFollow.Parse(prefix.Replace("originWebContentsId=1","originWebContentsId=2")+"/local/"+first+suffix,out id)&&id==null,"auxiliary renderer pauses");Check(AutoFollow.Parse(prefix+"/local/"+first+" windowId=2",out id)&&id==null,"ambiguous window pauses");
 var levels=new[]{"low","medium","high","xhigh","max","ultra"};var forms=new[]{"chibi","medium","full","xhigh","max","ultra"};Check(levels.Select(DisplayState.Form).SequenceEqual(forms),"six intended forms");Check(DisplayState.Form("future-value")=="unknown","unknown effort fails closed");
 foreach(string method in new[]{"thread/settings/update","thread-follower-update-thread-settings","thread-follower-start-turn","thread-follower-interrupt-turn","thread/resume","config/write"}){using(var c=new DesktopLink()){bool denied=false;try{((Task)typeof(DesktopLink).GetMethod("Request",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new object[]{method,new{},0})).GetAwaiter().GetResult();}catch(InvalidOperationException){denied=true;}Check(denied,"deny mutation before connection: "+method);}}
 Check(AnimatedPortrait.ReplyEnvelope(-1)==0&&AnimatedPortrait.ReplyEnvelope(2)==0&&AnimatedPortrait.ReplyEnvelope(.45)>.5,"click response ends and is visible");
 Console.WriteLine("PASS "+count+" offline contracts; no desktop connection or configuration writes");}catch(Exception e){Console.WriteLine(e.ToString());Environment.ExitCode=1;}}
}
