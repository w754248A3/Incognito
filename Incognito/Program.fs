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
open System.Text
open System.Security.Cryptography.X509Certificates

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
   

    [<Flags>]
    type AssocF =
    |None = 0
    
    type AssocStr =
    |Executable = 2

    [<DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Unicode)>]
    extern uint32 AssocQueryStringW(AssocF flags, AssocStr str, string pszAssoc, string pszExtra, [<Out>] StringBuilder pszOut, uint& pcchOut);


    let getExePath() =
        let name = "https"

        let mutable length = uint 0

        let mutable ret = AssocQueryStringW(AssocF.None, AssocStr.Executable, name, null, null, &length)
       
        if ret <> (uint 1) then
            raise (InvalidOperationException(""))

        let sb = new StringBuilder(int length)

        ret <- AssocQueryStringW(AssocF.None, AssocStr.Executable, name, null, sb, &length)


        if ret <> (uint 0) then
            raise (InvalidOperationException(""))

        sb.ToString()


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
        

    let id = "386b1b0d89187978";
    
    let appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
         
    let basePath = AppDomain.CurrentDomain.BaseDirectory;

    let appNameWithExe = appName + ".exe"
    
    let appId = appName + "." + id 
      
    let appPath = Path.Combine(basePath, appNameWithExe)
    
    let appPath'' =  "\"" + appPath + "\""
    
    let appPathArgs = appPath'' + " %1"
    
    let openPointName = "HTML." + appId

    let RegistryDefaultBrowser(rootReg:RegistryKey) =
        
        
        
   
        let writeOpen (reg:RegistryKey) p = reg.OpenSubKey(p, true)
        
        let readOpen (reg:RegistryKey) p = reg.OpenSubKey(p, false)


       

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
    
    let isos (os:OperatingSystem) pID major minor =
        os.Platform = pID && os.Version.Major = major && os.Version.Minor = minor
    let iswin7() = isos Environment.OSVersion PlatformID.Win32NT 6 1
    
    let iswin10() = isos Environment.OSVersion PlatformID.Win32NT 10 0
    
    
    let runApp appPath args =
        
        let workDic = Path.GetDirectoryName(appPath:string)
    
        let info = new ProcessStartInfo()
        
        info.Arguments <- args
        
        info.FileName <- appPath
        
        info.UseShellExecute <- false
        
        info.WorkingDirectory <- workDic
        
        Process.Start(info) |> ignore

        ()
    
    let getArgsFunc appPath =
        match X509Certificate.CreateFromSignedFile(appPath).Subject with
        | a when a.StartsWith("CN=Microsoft Corporation") -> fun url -> String.Join(' ', ["-inprivate"; "--single-argument"; url])
        | a when a.StartsWith("CN=Google LLC") -> fun url -> String.Join(' ', ["-incognito"; "--single-argument"; url])
        | a -> raise (ArgumentException("不支持该浏览器"))

    let rec inputExePath() =
        Console.WriteLine("请输入浏览器的完整路径")
        let s = Console.ReadLine()
        if File.Exists(s) then
            try
                getArgsFunc(s) |> ignore
                s
            with 
            | e -> Console.WriteLine("该浏览器不受支持") 
                   inputExePath()         
        else
           Console.WriteLine("文件不存在")
           inputExePath()


    let getBrowserPath() =
        let browserPath = win32api.getExePath()

        if browserPath.Equals(appPath, StringComparison.OrdinalIgnoreCase) then
            Console.WriteLine("自动获取的浏览器路径是我自己的路径,所以需要手动输入一个浏览器的路径")
            inputExePath()
        else
            try
                getArgsFunc(browserPath) |> ignore
                browserPath
            with
            | e -> Console.WriteLine("自动获取的浏览器不受支持,请手动输入一个浏览器路径")
                   inputExePath()
            

    
    let openDefaultProgramUI() =
        runApp @"C:\Windows\System32\control.exe" "/name Microsoft.DefaultPrograms /page pageDefaultProgram"
  

    
    let createInfoPath() =
        let basePath = AppDomain.CurrentDomain.BaseDirectory;
        
        Path.Combine(basePath, "path")

    let getSaveBrowserPath() =
        File.ReadAllText(createInfoPath(), System.Text.Encoding.UTF8)

    let setSaveBrowserPath(s) =
        File.WriteAllText(createInfoPath(), s, System.Text.Encoding.UTF8)
    
    
    
    let install() =
        let path = getBrowserPath();
        setSaveBrowserPath(path)

        if iswin7() then
            Console.WriteLine("当前系统为Windows 7 请确保以管理员权限运行否则请重新运行")
            Console.WriteLine("已是管理员权限运行请按回车键继续")
            Console.ReadLine() |> ignore
            RegistryDefaultBrowser(Registry.LocalMachine)
        else
            RegistryDefaultBrowser(Registry.CurrentUser)


        Console.WriteLine("已经注册为候选默认浏览器,请在默认程序面板中将该应用选择为默认浏览器")
        openDefaultProgramUI()
        Console.WriteLine("按下回车键继续")
        Console.ReadLine() |> ignore
        ()

    
    let openBrowser url =
        let appPath = getSaveBrowserPath()

        let args = getArgsFunc(appPath) url

        runApp appPath args

   
    let isUri(s) =
        try
            new Uri(s) |> ignore
            true
        with
        | :? UriFormatException -> false

    [<EntryPoint>]
    let main argv =
        if argv.Length <> 0 then
            argv
            |> Array.filter(fun p -> isUri(p))
            |> Array.head
            |> openBrowser
            |> ignore
            0
        else       
            install()
            0