using System.Security.Claims;
using OgrenciBilgiSistemi.DTOs;

namespace OgrenciBilgiSistemi.Services.Interfaces
{
    public interface IMenuService
    {
        Task<List<MenuOgeDto>> GetSidebarForUserAsync(
            int kullaniciId,
            ClaimsPrincipal user,
            CancellationToken ct = default);
    }
}
