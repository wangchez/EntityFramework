// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.Data.Entity.Query.Preprocessor.ExpressionFilters;
using Microsoft.Data.Entity.Query.Preprocessor.ExpressionVisitors;
using Remotion.Linq.Parsing.ExpressionVisitors.TreeEvaluation;

namespace Microsoft.Data.Entity.Query.Preprocessor
{
    public class QueryPreprocessor : IQueryPreprocessor
    {
        public QueryPreprocessor()
        {
        }

        public virtual Expression Preprocess(Expression query, QueryContext queryContext)
        {

            query = new QueryAnnotatingExpressionVisitor().Visit(query);

            query = new FunctionEvaluationDisablingVisitor().Visit(query);

            var partialEvaluationInfo = EvaluatableTreeFindingExpressionVisitor.Analyze(query, new NullEvaluatableExpressionFilter());

            query = new ParameterExtractingExpressionVisitor(partialEvaluationInfo, queryContext).Visit(query);

            return query;
        }
    }
}
