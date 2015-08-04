// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Data.Entity.Metadata.Internal;
using Microsoft.Data.Entity.Utilities;

namespace Microsoft.Data.Entity.Metadata.Conventions.Internal
{
    public class RequiredNavigationAttributeConvention : NavigationAttributeConvention<RequiredAttribute>
    {
        public override InternalRelationshipBuilder Apply(InternalRelationshipBuilder relationshipBuilder, Navigation navigation, RequiredAttribute attribute)
        {
            Check.NotNull(relationshipBuilder, nameof(relationshipBuilder));
            Check.NotNull(navigation, nameof(navigation));
            Check.NotNull(attribute, nameof(attribute));

            if (!navigation.PointsToPrincipal() || navigation.DeclaringEntityType.ClrType?.GetProperty(navigation.Name).PropertyType.TryGetSequenceType() != null)
            {
                return relationshipBuilder;
            }
            return relationshipBuilder.Required(true, ConfigurationSource.DataAnnotation) ?? relationshipBuilder;
        }
    }
}
