using System.ComponentModel;

namespace QRTwin.Maui.Extensions;

public static class PropertyChangedExtensions
{
    extension(PropertyChangedEventArgs args)
    {
        public bool IsProperty(string propertyName) =>
            args.PropertyName == propertyName;
    }
}
