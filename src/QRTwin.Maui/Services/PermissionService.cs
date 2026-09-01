namespace QRTwin.Maui.Services;

public sealed class PermissionService : IPermissionService
{
    public async Task<bool> EnsureCameraPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>().ConfigureAwait(false);
        if (status == PermissionStatus.Granted)
        {
            return true;
        }

        status = await Permissions.RequestAsync<Permissions.Camera>().ConfigureAwait(false);
        return status == PermissionStatus.Granted;
    }
}
