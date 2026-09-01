namespace QRTwin.Models;

public sealed record ScanResult(string Content, bool IsUrl, DateTime ScannedAt);
