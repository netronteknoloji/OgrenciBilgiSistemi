using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.DTOs;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using System.Linq;
using System.Security.Claims;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public class MenuService : IMenuService
    {
        private static readonly List<MenuOgeModel> ListEmpty = new(0);

        private readonly AppDbContext _db;
        private readonly ILogger<MenuService> _log;

        public MenuService(AppDbContext db, ILogger<MenuService> log)
        {
            _db = db;
            _log = log;
        }

        public async Task<List<MenuOgeDto>> GetSidebarForUserAsync(
    int kullaniciId,
    ClaimsPrincipal user,
    CancellationToken ct = default)
        {
            // 1) Kullanıcı & atanmış menüler
            var kullanici = await _db.Kullanicilar
                .AsNoTracking()
                .Include(k => k.KullaniciMenuler)
                .FirstOrDefaultAsync(k => k.KullaniciId == kullaniciId, ct);

            if (kullanici is null)
            {
                _log.LogWarning("Kullanıcı bulunamadı: {Id}", kullaniciId);
                return new();
            }

            // 2) Admin / roller / atamalar (rol >= atama politikası korunuyor)
            var isAdmin = kullanici.AdminMi || user.IsInRole("Admin");
            var assignedMenuIds = kullanici.KullaniciMenuler.Select(x => x.MenuOgeId).ToHashSet();
            var userRoleSet = GetUserRoles(user); // lower-case set

            // 3) Tüm menüler
            var allMenus = await _db.MenuOgeler.AsNoTracking().ToListAsync(ct);
            if (allMenus.Count == 0) return new();

            // 4) ToLookup ile ağaç: null kökler güvenli, gruplar O(1)
            var lookup = allMenus
                .OrderBy(m => m.Sirala)
                .ToLookup(m => m.AnaMenuId);          // ILookup<int?, MenuOgeModel>

            // Yardımcılar
            IEnumerable<MenuOgeModel> Roots() => lookup[null];       // kökler (AnaMenuId == null)
            IEnumerable<MenuOgeModel> ChildrenOf(int id) => lookup[id];

            // 5) Görünürlük + memoization + cycle-guard
            var canSeeMemo = new Dictionary<int, bool>();
            var visibleMemo = new Dictionary<int, bool>();

            bool CanSeeNode(MenuOgeModel m)
            {
                if (canSeeMemo.TryGetValue(m.Id, out var cached)) return cached;

                bool result;
                if (isAdmin)
                {
                    result = true;
                }
                else
                {
                    // Rol kısıtı varsa ve kullanıcı rolü taşımıyorsa: görünmez
                    if (!string.IsNullOrWhiteSpace(m.GerekliRole))
                    {
                        var required = SplitRoles(m.GerekliRole);
                        if (!userRoleSet.Overlaps(required))
                        {
                            canSeeMemo[m.Id] = false;
                            return false;
                        }
                    }
                    // Rol uygun ise: doğrudan ataması varsa görülebilir
                    result = assignedMenuIds.Contains(m.Id);
                }

                canSeeMemo[m.Id] = result;
                return result;
            }

            bool IsVisibleWithChildrenFlat(MenuOgeModel node, HashSet<int>? visited = null)
            {
                if (visibleMemo.TryGetValue(node.Id, out var cached)) return cached;

                visited ??= new HashSet<int>();
                if (!visited.Add(node.Id))
                {
                    _log.LogWarning("Menu döngüsü tespit edildi. NodeId={Id}", node.Id);
                    visibleMemo[node.Id] = false;
                    return false;
                }

                if (CanSeeNode(node))
                {
                    visibleMemo[node.Id] = true;
                    visited.Remove(node.Id);
                    return true;
                }

                foreach (var child in ChildrenOf(node.Id))
                {
                    if (IsVisibleWithChildrenFlat(child, visited))
                    {
                        visibleMemo[node.Id] = true;
                        visited.Remove(node.Id);
                        return true;
                    }
                }

                visibleMemo[node.Id] = false;
                visited.Remove(node.Id);
                return false;
            }

            MenuOgeDto ToDtoFilteredFlat(MenuOgeModel node, HashSet<int>? visited = null)
            {
                visited ??= new HashSet<int>();
                if (!visited.Add(node.Id))
                {
                    _log.LogWarning("Menu DTO dönüşümünde döngü tespit edildi. NodeId={Id}", node.Id);
                    return new MenuOgeDto
                    {
                        Id = node.Id,
                        Baslik = node.Baslik,
                        Controller = node.Controller,
                        Action = node.Action,
                        AnaMenuId = node.AnaMenuId,
                        Sirala = node.Sirala,
                        AltOgeler = new()
                    };
                }

                var dto = new MenuOgeDto
                {
                    Id = node.Id,
                    Baslik = node.Baslik,
                    Controller = node.Controller,
                    Action = node.Action,
                    AnaMenuId = node.AnaMenuId,
                    Sirala = node.Sirala
                };

                var kids = ChildrenOf(node.Id)
                    .Where(m => IsVisibleWithChildrenFlat(m))
                    .Select(m => ToDtoFilteredFlat(m, visited))
                    .ToList();

                dto.AltOgeler = kids;

                visited.Remove(node.Id);
                return dto;
            }

            // 6) Kökler → görünür filtre → DTO (method group yerine lambda!)
            var visibleRoots = Roots().Where(m => IsVisibleWithChildrenFlat(m));
            var result = visibleRoots.Select(r => ToDtoFilteredFlat(r)).ToList();
            return result;
        }

        private static HashSet<string> GetUserRoles(ClaimsPrincipal user)
        {
            var roles = user.Claims
                .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim().ToLowerInvariant())
                .ToHashSet();

            return roles;
        }

        private static HashSet<string> SplitRoles(string rolesCsv)
        {
            var roles = (rolesCsv ?? string.Empty)
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim().ToLowerInvariant())
                .ToHashSet();

            return roles;
        }
    }
}
