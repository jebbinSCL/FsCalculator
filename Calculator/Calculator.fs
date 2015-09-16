﻿module Calc

open System

type Error = 
| NoInputError
| ExpressionError
| UnsupportedOperatorError of string
| DivideByZeroError

type Result<'S, 'F> =
| Success of 'S
| Failure of 'F

let bind (f : 'S -> Result<'S2,'F2>) m=
    match m with 
    | Success s -> f s
    | Failure f -> Failure f

let (>>=) m f = bind f m 

type CalculationPart =
| Number of decimal
| Operation of (decimal -> decimal -> decimal)

let operations = 
    [ "+", (+)
      "-", (-)
      "*", (*)
      "/", (/)
      "%", (%) ]

let (|StrOp|_|) str = 
    operations
    |> Map.ofList
    |> Map.tryFind str

let (|StrNum|_|) str =
    match Decimal.TryParse(str) with
    | (true , value) -> Some(value)
    | _ -> None

let parsePart part =
    match part with
    | StrNum n      -> Success <| Number(n)
    | StrOp o       -> Success <| Operation(o)
    | unrecognised  -> Failure <| UnsupportedOperatorError(unrecognised)

let pop stack =
    match stack with
    | top :: rest ->
        let newStack = rest
        Success (top, newStack)
    | [] -> Failure ExpressionError

let push part stack = part :: stack

let mergeIfAllSuccess (input : Result<'S,'F> list) : Result<'S list, 'F> =
    let rec foldFunc (results : Result<'S,'F> list) (acc : Result<'S list, 'F>) : Result<'S list, 'F> = 
        match results with
        | head :: tail ->  
            match head with
            | Success s -> 
                match acc with
                | Success list -> 
                    foldFunc tail (Success <| s::list)
                | Failure f -> acc
            | Failure f -> Failure f
        | [] -> acc 
    let result = foldFunc input <| Success []
    match result with
    | Success s -> Success <| List.rev s
    | Failure f -> result

let passStackIfValid (input : string list) : Result<string list, Error> = 
    let dividingByZero = 
        input
        |> List.pairwise
        |> List.tryFind (function | ("0", "/") -> true | _ -> false )
    match dividingByZero with
    | Some _ -> Failure DivideByZeroError
    | None -> Success input


let parseStack (input : string) = 
    let validationResult = 
        input.Split([|' '|], StringSplitOptions.RemoveEmptyEntries) 
        |> Array.toList
        |> passStackIfValid
    match validationResult with
    | Failure f -> Failure f
    | Success s -> 
        s
        |> List.map parsePart
        |> mergeIfAllSuccess

let rec calculate stack =
    match stack with
    | [] -> Failure NoInputError
    | [Number n] -> Success n
    | _ ->
        match pop stack with
        | Failure f -> Failure f
        | Success (a, stack') ->
            match pop stack' with
            | Failure f -> Failure f
            | Success (b, stack'') ->
                match pop stack'' with
                | Failure f -> Failure f
                | Success (op, stack''') ->
                    match (a,b,op) with
                    | Number a', Number b', Operation f' ->
                        let result = Number(f' a' b')
                        push result stack''' |> calculate
                    | _ -> Failure ExpressionError

let tryCalculate input = 
    let parsedResult = input |> parseStack
    bind (fun parsed -> parsed |> calculate) parsedResult
    
let supportedOperators = 
    let symbols = operations |> List.map (fun (c,f) -> c)
    String.Join(" ", symbols)