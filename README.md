# KuronekoTrackingLib  
 This library provides kuroneko's tracking info in html/json/object format.  
 Compatible with C#. Coded with F#.  
  
# Example (C#)  
```csharp
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Console.OutputEncoding = Encoding.GetEncoding("shift_jis");  // kuroneko's site is encoded by shift_jis

var kuroneko = new KuronekoTracking("1111-2222-3333");

var previnfo = new ShipmentInfo();
var requestcount = 0;
while (true)
{
    var shipmenthtml = await kuroneko.GetShipmentWithHtmlAsync();
    var info = kuroneko.GetShipmentInfo(shipmenthtml);

    if (!info.Equals(previnfo))
    {
        Console.Clear();
        Console.WriteLine(kuroneko.GetShipmentInfoWithJson(shipmenthtml) + '\n');
        Console.Write("Request Count : ");
    }

    previnfo = info;

    requestcount++;
    Console.SetCursorPosition("Request Count : ".Length, Console.CursorTop);
    Console.Write(requestcount);

    if (info.ShipmentStatusCode == 0)
    {
        Console.WriteLine($"\n配達完了!");
        break;
    }
    else if (info.ShipmentStatusCode == 1)
    {
        Console.WriteLine($"\n配達中に突入した!");
        break;
    }
    else
    {
        await Task.Delay(3000);
    }
}
```