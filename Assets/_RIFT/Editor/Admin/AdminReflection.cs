using System.Reflection;

namespace Rift.EditorTools
{
    /// <summary>
    /// Editor-only access to private members, so the admin panel can poke gameplay
    /// without adding debug code to runtime scripts.
    /// </summary>
    internal static class AdminReflection
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static object Call(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, Any)?.Invoke(target, args);

        public static T Get<T>(object target, string member)
        {
            var type = target.GetType();
            FieldInfo f = type.GetField(member, Any);
            if (f != null) return (T)f.GetValue(target);
            PropertyInfo p = type.GetProperty(member, Any);
            return p != null ? (T)p.GetValue(target) : default;
        }

        public static void Set(object target, string member, object value)
        {
            var type = target.GetType();
            FieldInfo f = type.GetField(member, Any);
            if (f != null) { f.SetValue(target, value); return; }
            type.GetProperty(member, Any)?.SetValue(target, value);
        }
    }
}
