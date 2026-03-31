using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace OgrenciBilgiSistemi.Helpers
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum value)
        {
            var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
            var attr = member?.GetCustomAttribute<DisplayAttribute>();
            return attr?.Name ?? value.ToString();
        }
    }
}
