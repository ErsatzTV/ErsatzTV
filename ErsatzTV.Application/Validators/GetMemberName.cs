using System.Linq.Expressions;
using System.Reflection;

namespace ErsatzTV;

public static partial class Validators
{
    private static string GetMemberName<T, T2>(Expression<Func<T, T2>> expression)
    {
        var member = expression.Body as MemberExpression;
        if (member?.Member is PropertyInfo propertyInfo)
        {
            return propertyInfo.Name;
        }

        throw new ArgumentException("Expression is not a property");
    }
}
