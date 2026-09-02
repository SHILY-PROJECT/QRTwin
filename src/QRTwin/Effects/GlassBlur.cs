namespace QRTwin.Effects;

public static partial class GlassBlur
{
    public static void Apply(VisualElement element, float radius)
    {
        if (radius <= 0)
        {
            Clear(element);
            return;
        }

        ApplyPlatform(element, radius);
    }

    public static void Clear(VisualElement element) =>
        ClearPlatform(element);

    static partial void ApplyPlatform(VisualElement element, float radius);

    static partial void ClearPlatform(VisualElement element);
}
