namespace KuronekoTrackingLib

open System

[<Class>]
type WrongResponseContentException =
    inherit Exception

    new() = { inherit Exception() }

    new(message) = { inherit Exception(message) }

    new(message, (innerException: Exception)) = { inherit Exception(message, innerException) }

[<Class>]
type TrackingNumberNotFoundException =
    inherit Exception

    new() = { inherit Exception() }

    new(message) = { inherit Exception(message) }

    new(message, (innerException: Exception)) = { inherit Exception(message, innerException) }
