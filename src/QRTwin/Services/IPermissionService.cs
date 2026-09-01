namespace QRTwin.Services;

public interface IPermissionService
{
    Task<bool> EnsureCameraPermissionAsync();
}
