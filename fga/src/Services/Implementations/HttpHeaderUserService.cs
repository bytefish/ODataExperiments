using ODataFga.Models;

namespace ODataFga.Services.Implementations;

public class HttpHeaderUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _ctx;
    public HttpHeaderUserService(IHttpContextAccessor ctx) => _ctx = ctx;

    public string? UserId => _ctx.HttpContext?.Request.Headers["X-User-Id"] is var v && !string.IsNullOrEmpty(v) ? $"user:{v}" : null;

    public int RequiredMask
    {
        get
        {
            var req = _ctx.HttpContext?.Request.Headers["X-Require-Relation"].ToString();

            return string.IsNullOrEmpty(req) ? (int)DocPermissions.Viewer : (int)PermissionMapper.FromString(req);
        }
    }
}