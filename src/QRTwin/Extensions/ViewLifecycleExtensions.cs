namespace QRTwin.Extensions;

internal static class ViewLifecycleExtensions
{
    internal static bool IsShutdownException(Exception exception) =>
        exception is ObjectDisposedException or TaskCanceledException or InvalidOperationException;

    internal static void StopAnimations(this VisualElement? element) =>
        element?.CancelAnimations();
}
