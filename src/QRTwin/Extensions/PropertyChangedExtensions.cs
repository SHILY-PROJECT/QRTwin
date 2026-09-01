using System.ComponentModel;

namespace QRTwin.Extensions;

public static class PropertyChangedExtensions
{
    extension(PropertyChangedEventArgs args)
    {
        public bool IsProperty(string propertyName) =>
            args.PropertyName == propertyName;
    }
}
