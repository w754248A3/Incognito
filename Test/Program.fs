// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open Microsoft.Win32

// Define a function to construct a message to print
let from whom =
    sprintf "from %s" whom

[<EntryPoint>]
let main argv =
    
    let win10 = new OperatingSystem(PlatformID.Win32NT, new Version(10,0))

    let win7 = new OperatingSystem(PlatformID.Win32NT, new Version(6,1))

    let v = Environment.OSVersion
    Console.WriteLine((v = win10):bool)


    0 // return an integer exit code