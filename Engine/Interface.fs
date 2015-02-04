namespace RetroCalc.Engine

open System
open Emulator

type Calc(input : Func<char>, output : Action<Tuple<string,string,string,string,string>, int[][], int[]>, r : int[][], s : int[]) =
    member x.Emulate () =
        let i () =
            let c = input.Invoke()
            if c <> (char 0) then Some c else None
        let o disp r s =
            let x, y, z, t, m = disp
            output.Invoke(new Tuple<string, string, string, string, string>(x, y, z, t, m), r, s)
        let r' = if r <> null then r else Array.init 8 (fun _ -> Array.create 14 0)
        let s' = if s <> null then s else Array.create 12 0
        calculator r' s' i o rom

[<assembly:System.Resources.NeutralResourcesLanguageAttribute("en-US")>]
do()