open System
open System.IO.Ports

[<EntryPoint>]
let main argv =
    let portName : string = "/dev/ttyUSB0"
    let baudRate : int = 9600

    try
        use serialPort = new SerialPort(portName, baudRate)

        serialPort.Open()

        printf "Serial port opened on %s at %d baud." portName baudRate

        System.Threading.Thread.Sleep(2000)

        printfn "Sending message to Arduino..." 

        let messageToSend = "Hello Arduino from F#!\n"


        serialPort.WriteLine(messageToSend)

        printfn "Message sent: %s" messageToSend

        printfn "Waiting for a response..."

        let response = serialPort.ReadLine()
        
        printfn "Response from arduino: %s" response

        serialPort.Close()
        printfn "Serial port closed."

        0

    with
    | (ex: exn) -> 
        eprintf "Error: %s\n" ex.Message
        1
        


