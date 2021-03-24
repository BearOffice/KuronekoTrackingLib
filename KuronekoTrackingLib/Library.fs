namespace KuronekoTrackingLib

open System.Net.Http
open System.IO
open System.Text
open System.Text.Json
open System.Text.Encodings.Web
open System.Text.RegularExpressions
open System.Text.Unicode

[<Class>]
type KuronekoTracking(slipNumber: string) =
    member this.GetShipmentWithHtmlAsync() =
        async {
            let param = dict [ "number00", "1"; "number01", slipNumber ]
            let content = new FormUrlEncodedContent(param)

            use client = new HttpClient()

            let! response =
                client.PostAsync("https://toi.kuronekoyamato.co.jp/cgi-bin/tneko", content)
                |> Async.AwaitTask

            let! contentStream = response.Content.ReadAsStreamAsync() |> Async.AwaitTask

            // Content is encoded by shift-jis
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
            let encode = Encoding.GetEncoding("shift_jis")

            let! contentString =
                use reader = new StreamReader(contentStream, encode, true)
                reader.ReadToEndAsync() |> Async.AwaitTask

            return contentString
        } |> Async.StartAsTask

    member this.GetShipmentInfoWithHtml(shipmentHtml:string) =
        // Get shipmentinfo block
        let blockMatch =
            Regex.Match(shipmentHtml, @"function PRINT_0\(\){(.*?)}", RegexOptions.Singleline)
        if not blockMatch.Success then
            raise <| new WrongResponseContentException("Failed to get shipment info.")

        let infoBlock = blockMatch.Groups.[1].Value
        let infoRegex = new Regex(@"swd\.writeln\('(.*?)'\);")

        infoRegex.Matches(infoBlock)
        |> Seq.cast
        |> Seq.map (fun (item: Match) -> infoRegex.Replace(item.Value, "$1"))
        |> Seq.reduce (fun acc item -> acc + item + "\n")

    member this.GetShipmentInfoWithHtmlAsync() =
        async {
            let! shipmentHtml = this.GetShipmentWithHtmlAsync() |> Async.AwaitTask
            return this.GetShipmentInfoWithHtml(shipmentHtml)
        } |> Async.StartAsTask
    
    member this.GetShipmentInfo(shipmentHtml:string) = 
        let info = 
            this.GetShipmentInfoWithHtml(shipmentHtml).Split('\n')
            |> Array.map (fun item -> item.TrimStart().TrimEnd())
            |> Array.reduce (fun acc item -> acc + item)

        // Get tracking number
        let numberMatch = Regex.Match(info, @"<center><font.+?/font><font.+?/font><font.*?><b>(.+?)</b></font>")
        if not numberMatch.Success then
            raise <| new WrongResponseContentException("Unknown error.")

        let trackingNumber = numberMatch.Groups.[1].Value
        
        // Get overview
        let overviewMatch =
            Regex.Match(info,
                @"<table.*?><tr><th.+?/th><th.+?/th></tr><tr><td.*?>(.+?)<br></td><td.*?>(.+?)<br></td></tr></table>")
        if not overviewMatch.Success then
            raise <| new TrackingNumberNotFoundException("Tracking number not found.")
        
        let productName = overviewMatch.Groups.[1].Value
        let desiredDateTime = overviewMatch.Groups.[2].Value

        // Get details
        let detailsMatch =
            Regex.Match(info, @"<table.*?><tr><th.+?/th><th.+?/th><th.+?/th><th.+?/th><th.+?/th></tr>"
                + @"(<tr.+?/tr>)</table>"
            )
        if detailsMatch.Success then
            let detailMatches = Regex.Matches(detailsMatch.Groups.[1].Value, @"<tr><td>(.+?)<br></td><td>(.+?)<br></td>"
                    + @"<td>(.+?)<br></td><td>(.+?)<br></td><td>(.+?)<br></td></tr>")

            let shipmentDetails =
                detailMatches
                |> Seq.cast
                |> Seq.map
                    (fun (item: Match) ->
                        new ShipmentDetail(
                            item.Groups.[1].Value,
                            item.Groups.[2].Value,
                            item.Groups.[3].Value,
                            item.Groups.[4].Value,
                            item.Groups.[5].Value
                        ))
                |> Seq.toArray
                
            let lastStatus = shipmentDetails.[shipmentDetails.Length - 1].ShipmentStatus

            let (|ContainsKeyword|) (keyword:string) (input:string) = input.Contains(keyword)

            let statusCode = 
                match lastStatus with
                | ContainsKeyword "配達完了" true -> 0
                | ContainsKeyword "配達中" true -> 1
                | _ -> 2

            new ShipmentInfo(trackingNumber, statusCode, lastStatus, productName, desiredDateTime, shipmentDetails)
        else
            new ShipmentInfo(trackingNumber, -1, null, productName, desiredDateTime, null)

    member this.GetShipmentInfoAsync() =
        async {
            let! shipmentHtml = this.GetShipmentWithHtmlAsync() |> Async.AwaitTask
            return this.GetShipmentInfo(shipmentHtml)
        } |> Async.StartAsTask

    member this.GetShipmentInfoWithJson(shipmentHtml:string) =
        let options =
            new JsonSerializerOptions(
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            )
        JsonSerializer.Serialize(this.GetShipmentInfo(shipmentHtml), options)

    member this.GetShipmentInfoWithJsonAsync() =
        async {
            let! shipmentHtml = this.GetShipmentWithHtmlAsync() |> Async.AwaitTask
            return this.GetShipmentInfoWithJson(shipmentHtml)
        } |> Async.StartAsTask