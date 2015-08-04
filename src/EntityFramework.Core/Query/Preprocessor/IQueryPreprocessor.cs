// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.Data.Entity.Query.Preprocessor
{
    public interface IQueryPreprocessor
    {
        Expression Preprocess(Expression query, out IDictionary<string, object> parameters);
    }
}
