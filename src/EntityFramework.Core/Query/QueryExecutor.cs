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
using System.Linq;

namespace Microsoft.Data.Entity.Query
{
    public class QueryExecutor : IQueryExecutor
    {
        private readonly IQueryContextFactory _contextFactory;
        private readonly IQueryPreprocessor _preprocessor;
        private readonly IQueryCompiler _queryCompiler;
        private readonly IServiceProvider _serviceProvider;

        public QueryExecutor(
            [NotNull] IQueryContextFactory contextFactory,
            [NotNull] IQueryPreprocessor preprocessor,
            [NotNull] IQueryCompiler queryCompiler,
            IServiceProvider serviceProvider)
        {
            Check.NotNull(contextFactory, nameof(contextFactory));
            Check.NotNull(preprocessor, nameof(preprocessor));
            Check.NotNull(queryCompiler, nameof(queryCompiler));

            _contextFactory = contextFactory;
            _preprocessor = preprocessor;
            _queryCompiler = queryCompiler;
            _serviceProvider = serviceProvider;
        }

        public TResult Execute<TResult>(Expression query)
        {
            Check.NotNull(query, nameof(query));

            var queryContext = _contextFactory.Create();

            query = _preprocessor.Preprocess(query, queryContext);

            var compiledQuery = _queryCompiler.CompileQuery<TResult>(query);


            return
                typeof(TResult) == compiledQuery.ResultItemType
                    ? ((Func<QueryContext, IEnumerable<TResult>>)compiledQuery.Executor)(queryContext).First()
                    : ((Func<QueryContext, TResult>)compiledQuery.Executor)(queryContext);
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
