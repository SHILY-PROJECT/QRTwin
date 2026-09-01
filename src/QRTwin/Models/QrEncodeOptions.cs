namespace QRTwin.Models;

public readonly record struct QrEncodeOptions(
    int Size = 512,
    int Margin = 2,
    string CharacterSet = "UTF-8")
{
    public static QrEncodeOptions Default => new();

    public static QrEncodeOptions Preview => new(Size: 280);

    public static QrEncodeOptions Presentation => new(Size: 640);
}
