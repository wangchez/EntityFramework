// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Data.Entity.Query
{
    public interface IQueryExecutor
    {
        TResult Execute<TResult>(Expression expression);

        Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken);

        IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression);
    }
}
