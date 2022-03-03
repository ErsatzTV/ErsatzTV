﻿using System.Linq.Expressions;
using ErsatzTV.Core;

namespace ErsatzTV;

public static partial class Validators
{
    public static Func<Expression<Func<T, int>>, Validation<BaseError, int>>
        AtLeast<T>(this T input, int minimum) =>
        value => Optional(value)
            .Map(i => i.Compile()(input))
            .Where(i => i >= minimum)
            .ToValidation<BaseError>(
                $"[{GetMemberName(value)}] must be greater or equal to {minimum}");
}