// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open Microsoft.Win32
open System.Diagnostics;
open System.IO
open System.Runtime.InteropServices;
open System.Text
open System.Security.Cryptography.X509Certificates
// Define a function to construct a message to print
let from whom =
    sprintf "from %s" whom

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
    
    Process.Start(info)


module win32api =

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

let openDefaultProgramUI() =
    runApp @"C:\Windows\System32\control.exe" "/name Microsoft.DefaultPrograms /page pageDefaultProgram"

let getArgsFunc appPath =
    match X509Certificate.CreateFromSignedFile(appPath).Subject with
    | a when a.StartsWith("CN=Microsoft Corporation") -> fun url -> String.Join(' ', ["-inprivate"; "--single-argument"; url])
    | a when a.StartsWith("CN=Google LLC") -> fun url -> String.Join(' ', ["-incognito"; "--single-argument"; url])
    | a when a.StartsWith("CN=Mozilla Corporation") -> fun url -> String.Join(' ', ["-private"; "-osint"; "-url"; url])
    | a -> raise (ArgumentException("不支持该浏览器"))
[<EntryPoint>]
let main argv =
    
    Console.WriteLine(getArgsFunc(@"C:\Users\LeiKaiFeng\AppData\Local\Google\Chrome\Application\chrome.exe")("a"))

    0 // return an integer exit code