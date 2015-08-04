// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity.FunctionalTests;

namespace Microsoft.Data.Entity.InMemory.FunctionalTests
{
    public class QueryInMemoryTest : QueryTestBase<NorthwindQueryInMemoryFixture>
    {
        public QueryInMemoryTest(NorthwindQueryInMemoryFixture fixture)
            : base(fixture)
        {
        }

        public override void Where_simple_closure_via_query_cache_nullable_type()
        {
            base.Where_simple_closure_via_query_cache_nullable_type();
        }
    }
}
