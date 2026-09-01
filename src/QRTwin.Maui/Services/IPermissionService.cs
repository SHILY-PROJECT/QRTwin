namespace QRTwin.Maui.Services;

public interface IPermissionService
{
    Task<bool> EnsureCameraPermissionAsync();
}
