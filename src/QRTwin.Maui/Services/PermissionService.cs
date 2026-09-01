namespace QRTwin.Maui.Services;

public sealed class PermissionService() : IPermissionService
{
    public async Task<bool> EnsureCameraPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>().ConfigureAwait(false);
        status = status switch
        {
            PermissionStatus.Granted => status,
            _ => await Permissions.RequestAsync<Permissions.Camera>().ConfigureAwait(false)
        };

        return status is PermissionStatus.Granted;
    }
}
