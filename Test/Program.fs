open System
open Microsoft.Win32
open System.Diagnostics;
open System.IO
open System.Runtime.InteropServices;
open System.Text
open System.Security.Cryptography.X509Certificates

[<EntryPoint>]
let main argv =
    
    Array.ForEach(argv, fun e -> Console.WriteLine(e))


    Console.ReadLine() |> ignore

    0