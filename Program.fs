open System
open System.IO
open System.IO.Ports
open System.Threading
open Spectre.Console

type Frequency = int

// TODO add boundaries
let getNoteFrequency position =
    let baseFrequency: float = 440.0 // A4
    let basePosition: int = 49

    let frequency = baseFrequency * 2.0 ** ((float (position - basePosition)) / 12.0)
    System.Math.Round(frequency) |> int



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
        promptUserInfo "Arduino port" "/dev/ttyUSB0" (fun (p: string) ->
            try
                use port = new SerialPort(p)
                port.Open()
                port.Close()
                ValidationResult.Success()
            with ex ->
                ValidationResult.Error("[red]Couldn't connect to port[/]"))

    let baudRate: int =
        promptUserInfo "Baud rate" "9600" (fun baud ->
            match Int32.TryParse(baud) with
            | (true, _) -> ValidationResult.Success()
            | (false, _) -> ValidationResult.Error("[red]Baud rate must be an integer.[/]"))
        |> Int32.Parse


    let BuzzerPin: int =
        promptUserInfo "Buzzer PIN [red](MUST be PWM)[/]" "9" (fun pin ->
            match Int32.TryParse(pin) with
            | (true, _) -> ValidationResult.Success()
            | (false, _) -> ValidationResult.Error("[red]Buzzer pin must be an integer.[/]"))
        |> Int32.Parse

    0
