namespace Incognito
open System;
open Microsoft.Win32;
open System.Linq;
open System.Threading;
open System.Runtime.InteropServices;
open System.Runtime.CompilerServices;
open System.Security.Principal;
open System.Diagnostics;
open System.IO;
open System.Collections.Generic;


module win32api =

    type HChangeNotifyEventID =
        | SHCNE_ASSOCCHANGED = 0x08000000

    [<Flags>]
    type HChangeNotifyFlags =
        | SHCNF_DWORD = 0x0003
        | SHCNF_FLUSH = 0x1000

    [<DllImport(@"Kernel32.dll")>]
    extern void FreeConsole()

    [<DllImport(@"Shell32.dll")>]
    extern void SHChangeNotify(HChangeNotifyEventID wEventId,HChangeNotifyFlags uFlags,IntPtr dwItem1,IntPtr dwItem2)

    let flushOS() =
        let b = (HChangeNotifyFlags.SHCNF_DWORD ||| HChangeNotifyFlags.SHCNF_FLUSH)

        SHChangeNotify(HChangeNotifyEventID.SHCNE_ASSOCCHANGED, b, IntPtr.Zero, IntPtr.Zero);
   




module Incognito = 
    type RegistryTreeData =
        | Str of s:string
        | Int of n:int


    type RegistryKeyValue ={
        Key: string
        Value : RegistryTreeData
    }

    type RegistryTree = {
        Name :string

        Value: list<RegistryKeyValue>

        SubTree: list<RegistryTree>
    }

    let rec openToRegistry (reg:RegistryKey) list openFunc openLastFunc : RegistryKey =

        if List.isEmpty list then
            reg
        else
            let head = List.head list
            let tail = List.tail list
            
            if List.isEmpty tail then
                openLastFunc reg head
            else
                let subReg = openFunc reg head
                openToRegistry subReg tail openFunc openLastFunc

            
   
    let rec createRegistry (reg: RegistryKey ) (tree:RegistryTree) =
    
        let rec setValue (reg:RegistryKey) (kvs:list<RegistryKeyValue>) =
        
            let set (reg:RegistryKey) (v:RegistryKeyValue) =
                match v.Value with
                | Str s -> reg.SetValue(v.Key, s)
                | Int n -> reg.SetValue(v.Key, n)

            if List.isEmpty kvs then
                ()
            else
                let head = List.head kvs
                let tail = List.tail kvs
                set reg head
                setValue reg tail

   
        let subReg = reg.CreateSubKey(tree.Name, true)

        setValue subReg tree.Value

        createSubRegistry subReg tree.SubTree


    and createSubRegistry(reg:RegistryKey) (kvs:list<RegistryTree>) =
    
        if List.isEmpty kvs then
            ()
        else
            let head = List.head kvs
            let tail = List.tail kvs

            createRegistry reg head |> ignore

            createSubRegistry reg tail



    let createFileTypeRegistry appPath appPathArgs appId appName pointName =
       
        let command = {Name = "command"; SubTree = list.Empty; 
    
            Value = [{Key = ""; Value = Str(appPathArgs)} ]
        }



        let open_ = {Name = "open"; Value = list.Empty; SubTree = [command]}

        let shell = {Name = "shell"; Value = list.Empty; SubTree = [open_]}


        let application = {Name = "Application"; SubTree = list.Empty;
        
                Value = [
                        {Key = "ApplicationCompany"; Value = Str("LeiKaiFeng LLC")};
                        {Key = "ApplicationDescription"; Value = Str("访问互联网")};
                        {Key = "ApplicationIcon"; Value = Str(appPath + ",0")};
                        {Key = "ApplicationName"; Value = Str(appName)};
                        {Key = "AppUserModelId"; Value = Str(appId)};
                ]
        }

        let defaultIcon = {Name = "DefaultIcon"; SubTree = list.Empty;
        
                Value = [{Key = ""; Value = Str(appPath + ",0")}]
        }

        {Name = pointName;
        
            Value = [
                {Key = ""; Value = Str(appName + " HTML Document")}
                {Key = "AppUserModelId"; Value = Str(appId)}
            ];

            SubTree = [ application; defaultIcon; shell; ]
        }


    let createAppRegistry appPath'' appPath appOpenPointName appId appName =
        let command = {Name = "command"; SubTree = list.Empty; Value = [{Key = ""; Value = Str(appPath'')}]}

        let open_ = {Name = "open"; Value = list.Empty; SubTree = [command]}

        let shell = {Name = "shell"; Value = list.Empty; SubTree = [open_]}

        let InstallInfo = {Name = "InstallInfo"; SubTree=list.Empty; 
            Value = [
                    {Key = "HideIconsCommand"; Value = Str(appPath'')}
                    {Key = "IconsVisible"; Value = Int(1)}
                    {Key = "ReinstallCommand"; Value = Str(appPath'')}
                    {Key = "ShowIconsCommand"; Value = Str(appPath'')}

            ]
        }

        let defaultIcon = {Name = "DefaultIcon"; SubTree = list.Empty;
            Value = [
                    {Key = ""; Value = Str(appPath + ", 0")}
            ]
        }

        let fileAssociations = {Name = "FileAssociations"; SubTree = list.Empty;
            Value = [
                    {Key = ".html"; Value = Str(appOpenPointName)}
            ]
        }

        let startmenu = {Name = "Startmenu"; SubTree = list.Empty;
            Value = [
                    {Key = "StartMenuInternet"; Value = Str(appId)}
            ]
        }

        let urlAssociations = {Name = "URLAssociations"; SubTree = list.Empty;
            Value = [
                    {Key = "http"; Value = Str(appOpenPointName)}
                    {Key = "https"; Value = Str(appOpenPointName)}
            ]
        }

        let capabilities = {Name = "Capabilities"; SubTree = [fileAssociations; startmenu; urlAssociations];
    
            Value = [
                    {Key = "ApplicationDescription"; Value = Str("默认浏览器中介")}
                    {Key = "ApplicationIcon"; Value = Str(appPath + ",0")}
                    {Key = "ApplicationName"; Value = Str(appName)}
            ]
        }


    

        {Name = appId; Value = [{Key = ""; Value = Str(appName)}]; SubTree = [capabilities; defaultIcon; InstallInfo; shell]}

    let createAppRegistryPath appId =
        let vs = [| "Software"; "Clients"; "StartMenuInternet"; appId; "Capabilities"|]
        let path = String.Join('\\', vs)

        {Name = "RegisteredApplications"; SubTree = List.empty;
            Value = [
                {Key = appId; Value = Str(path)}
            ]
        }

    let createAppInstallRegistryPath appNameWithExe appPath basePath =
        
         
        {Name = appNameWithExe; SubTree = list.Empty;
                       
            Value = [
                            
                    {Key = ""; Value = Str(appPath)}
                    {Key = "Path"; Value = Str(basePath)}
            ]
        }
        

    let RegistryDefaultBrowser(rootReg:RegistryKey) =
        
        
        let id = "386b1b0d89187978";
        
        let appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
             
        let basePath = AppDomain.CurrentDomain.BaseDirectory;

        let appNameWithExe = appName + ".exe"
        
        let appId = appName + "." + id 
          
        let appPath = Path.Combine(basePath, appNameWithExe)
        
        let appPath'' =  "\"" + appPath + "\""
        
        let appPathArgs = appPath'' + " %1"
        
        let openPointName = "HTML." + appId
   
        let writeOpen (reg:RegistryKey) p = reg.CreateSubKey(p)
        
        let readOpen = writeOpen


       

        let registryOpenFileCommand () =
            let v = createFileTypeRegistry appPath appPathArgs appId appName openPointName
            let vs = ["SOFTWARE"; "Classes"]
            createRegistry (openToRegistry rootReg vs readOpen writeOpen) v
        
        let registryApp() =
            let v = createAppRegistry appPath'' appPath openPointName appId appName         
            let vs = ["SOFTWARE"; "Clients"; "StartMenuInternet"]
            createRegistry (openToRegistry rootReg vs readOpen writeOpen) v


        let registryAppPath() =
            
            let v = createAppRegistryPath appId
            let vs = ["SOFTWARE"]
            
            createRegistry (openToRegistry rootReg vs readOpen writeOpen) v
                    
                    
                    
        let registryAppInstallPath()=
            let v = createAppInstallRegistryPath appNameWithExe appPath basePath
            let vs = ["SOFTWARE"; "Microsoft"; "Windows"; "CurrentVersion"; "App Paths"]
            
            createRegistry (openToRegistry rootReg vs readOpen writeOpen) v    
                          
        
        
        registryOpenFileCommand()

        registryApp()

        registryAppPath()

        //registryAppInstallPath()
         
        win32api.flushOS()


        ()


    let rec inputExePath() =
        Console.WriteLine("请输入浏览器的完整路径")
        let s = Console.ReadLine()
        if File.Exists(s) then
            s
        else
           Console.WriteLine("文件不存在")
           inputExePath()

    let createInfoPath() =
        let basePath = AppDomain.CurrentDomain.BaseDirectory;
        
        Path.Combine(basePath, "path")

    let getExePath() =
        File.ReadAllText(createInfoPath(), System.Text.Encoding.UTF8)

    let setExePath(s) =
        File.WriteAllText(createInfoPath(), s, System.Text.Encoding.UTF8)
    
    
    let install() =
        let path = inputExePath();
        setExePath(path)
        RegistryDefaultBrowser(Registry.CurrentUser)
        ()


    let openChrome url =
        let appPath = getExePath()

        let workDir = Path.GetDirectoryName(appPath)

        let vs = [|"-incognito"; "--single-argument"; url|]

        let args = String.Join(' ', vs)

        let info = new ProcessStartInfo()
        
        info.Arguments <- args
        
        info.FileName <- appPath
        
        info.UseShellExecute <- false
        
        info.WorkingDirectory <- workDir
        
        Process.Start(info)

   

    [<EntryPoint>]
    let main argv =
        if argv.Length <> 0 then
            openChrome(argv.[0]) |> ignore
            0
        else       
            install()
            0