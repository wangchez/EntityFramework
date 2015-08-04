// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Query.Compiler;
using Microsoft.Data.Entity.Query.Preprocessor;
using Microsoft.Data.Entity.Utilities;

namespace Microsoft.Data.Entity.Query
{
    public class QueryExecutor : IQueryExecutor
    {
        private readonly IQueryPreprocessor _preprocessor;
        private readonly IQueryCompiler _queryCompiler;
        private readonly IServiceProvider _serviceProvider;

        public QueryExecutor(
            [NotNull] IQueryPreprocessor preprocessor,
            [NotNull] IQueryCompiler queryCompiler,
            IServiceProvider serviceProvider)
        {
            Check.NotNull(preprocessor, nameof(preprocessor));
            Check.NotNull(queryCompiler, nameof(queryCompiler));

            _preprocessor = preprocessor;
            _queryCompiler = queryCompiler;
            _serviceProvider = serviceProvider;
        }

        public TResult Execute<TResult>(Expression query)
        {
            Check.NotNull(query, nameof(query));

            IDictionary<string, object> parameters;

            query = _preprocessor.Preprocess(query, out parameters);

            var compiledQuery = _queryCompiler.CompileQuery<TResult>(query);

            return compiledQuery.Executor(parameters);
        }

        public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
        {
            throw new NotImplementedException();
        }

        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
