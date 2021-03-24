namespace KuronekoTrackingLib

[<Struct>]
type ShipmentDetail
    (
        status: string,
        date: string,
        time: string,
        name: string,
        code: string
    ) =
    member this.ShipmentStatus = status
    member this.Date = date
    member this.Time = time
    member this.StoreName = name
    member this.StoreCode = code

[<Struct>]
type ShipmentInfo
    (
        number: string,
        statusCode: int,
        status: string,
        name: string,
        dateTime: string,
        details: ShipmentDetail array
    ) =
    member this.TrackingNumber = number
    member this.ShipmentStatusCode = statusCode
    member this.LastStatus = status
    member this.ProductName = name
    member this.DesiredDateTime = dateTime
    member this.ShipmentDetails = details
