open System
open System.IO
open System.IO.Ports
open Spectre.Console

// TODO Rename
// TODO Make it generic instead of only returning a string
let promptUserInfo promptMessage (defaultValue: string) validationFun : string =
    AnsiConsole.Prompt(
        (new TextPrompt<string>(promptMessage)).DefaultValue(defaultValue).Validate(fun input -> validationFun input)
    )

[<EntryPoint>]
let main argv =
    AnsiConsole.MarkupLine("[underline red]Hello World![/]")

    let port: string =
        promptUserInfo "Arduino port" "/dev/ttyUSB0" (fun path ->
            try
                use port = new SerialPort(path)
                port.Open()
                port.Close()

                ValidationResult.Success()

            with ex ->
                ValidationResult.Error($"[red]Invalid port path or Arduino is DISCONNECTED: {ex.Message}[/]"))

    let baudRate  : int =
        promptUserInfo "Baud rate" "9600" (fun baud ->
            match Int32.TryParse(baud) with
            | (true, _) -> ValidationResult.Success()
            | (false, _) -> ValidationResult.Error("[red]Baud rate should be an integer.[/]"))
        |> Int32.Parse



    0
