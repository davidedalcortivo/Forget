using Forget.Core.Caching;
using Forget.Core.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;


namespace Forget.Tests.Core
{
    /// <summary>
    /// Exercises the entity-mapping rules enforced by <see cref="EntityInfoCache{TEntity}"/>'s static constructor.
    /// Every failure scenario uses its own private entity type: the cache is keyed per closed generic type, and once
    /// a type's static constructor throws, every later access to that same type re-throws
    /// <see cref="TypeInitializationException"/> rather than the original exception, so scenarios must not share a type.
    /// </summary>
    public class EntityInfoCacheTests
    {
        private sealed class EntityWithIdByName
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        [Fact]
        public void EntityWithIdProperty_UsesItAsIdentifierWithoutKeyAttribute()
        {
            Assert.Equal("Id", EntityInfoCache<EntityWithIdByName>.IdProperty.Name);
            Assert.Equal(nameof(EntityWithIdByName), EntityInfoCache<EntityWithIdByName>.TableName);
        }

        private sealed class EntityWithExplicitKey
        {
            [Key]
            public Guid Code { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        [Fact]
        public void EntityWithKeyAttribute_UsesItAsIdentifierRegardlessOfName()
        {
            Assert.Equal("Code", EntityInfoCache<EntityWithExplicitKey>.IdProperty.Name);
        }

        private sealed class EntityWithMultipleKeys
        {
            [Key]
            public int Id { get; set; }

            [Key]
            public int OtherId { get; set; }
        }

        [Fact]
        public void EntityWithMultipleKeyAttributes_ThrowsOnFirstAccess()
        {
            TypeInitializationException ex = Assert.Throws<TypeInitializationException>(
                () => _ = EntityInfoCache<EntityWithMultipleKeys>.IdProperty);

            Assert.IsType<InvalidOperationException>(ex.InnerException);
        }

        private sealed class EntityWithoutIdOrKey
        {
            public string Name { get; set; } = string.Empty;
        }

        [Fact]
        public void EntityWithoutIdOrKeyAttribute_ThrowsOnFirstAccess()
        {
            TypeInitializationException ex = Assert.Throws<TypeInitializationException>(
                () => _ = EntityInfoCache<EntityWithoutIdOrKey>.IdProperty);

            Assert.IsType<InvalidOperationException>(ex.InnerException);
        }

        private sealed class EntityWithReservedPropertyName
        {
            public int Id { get; set; }

#pragma warning disable IDE1006
            public string p_0 { get; set; } = string.Empty;
#pragma warning restore IDE1006
        }

        // p0 was the name of the first filter value until filter values took an underscore (p_0); it is an ordinary
        // property name now.
        private sealed class EntityWithAPropertyNamedLikeAnOldParameter
        {
            public int Id { get; set; }

#pragma warning disable IDE1006
            public string p0 { get; set; } = string.Empty;
#pragma warning restore IDE1006
        }

        [Fact]
        public void EntityWithReservedParameterName_ThrowsOnFirstAccess()
        {
            TypeInitializationException ex = Assert.Throws<TypeInitializationException>(
                () => _ = EntityInfoCache<EntityWithReservedPropertyName>.IdProperty);

            Assert.IsType<InvalidOperationException>(ex.InnerException);
        }

        [Theory]
        [InlineData("p_0", true)]
        [InlineData("P_12", true)]
        [InlineData("p_1_", true)]
        [InlineData("p_1_1", true)]
        [InlineData("p_1_1_", true)]
        [InlineData("p_1_1_1", true)]
        [InlineData("p0", false)]
        [InlineData("P1", false)]
        [InlineData("p_", false)]
        [InlineData("p", false)]
        [InlineData("p_a", false)]
        [InlineData("p1_1", false)]
        [InlineData("Address_1", false)]
        public void TheReservedParameterNames_AreEveryNameShapedLikeAFilterParameter(string name, bool reserved)
        {
            Assert.Equal(reserved, SqlTranslationContext.ParameterRegex().IsMatch(name));
        }

        [Fact]
        public void EntityWithAPropertyNamedLikeAnOldParameter_IsAccepted()
        {
            Assert.Contains(EntityInfoCache<EntityWithAPropertyNamedLikeAnOldParameter>.Properties, x => x.Name == "p0");
        }

        [Table("Widgets", Schema = "inventory")]
        private sealed class EntityWithTableAttribute
        {
            public int Id { get; set; }
        }

        [Fact]
        public void EntityWithTableAttribute_OverridesTableAndSchemaName()
        {
            Assert.Equal("Widgets", EntityInfoCache<EntityWithTableAttribute>.TableName);
            Assert.Equal("inventory", EntityInfoCache<EntityWithTableAttribute>.SchemaName);
        }

        // Every name is quoted, with the closing quote character doubled, so any name the database accepts can be mapped.
        [Table("Order Details", Schema = "Odd Schema#1")]
        private sealed class EntityWithNamesOutsideLettersAndDigits
        {
            public int Id { get; set; }

            [Column("Line Name")]
            public string Name { get; set; } = string.Empty;

            [Column("Quote\"Bracket]Tick`")]
            public string Quoted { get; set; } = string.Empty;
        }

        [Fact]
        public void NamesWithSpacesOrQuoteCharacters_AreAccepted()
        {
            Assert.Equal("Order Details", EntityInfoCache<EntityWithNamesOutsideLettersAndDigits>.TableName);
            Assert.Equal("Odd Schema#1", EntityInfoCache<EntityWithNamesOutsideLettersAndDigits>.SchemaName);
            Assert.Equal("Line Name", EntityInfoCache<EntityWithNamesOutsideLettersAndDigits>.ColumnNamesByPropertyName["Name"]);
            Assert.Equal("Quote\"Bracket]Tick`", EntityInfoCache<EntityWithNamesOutsideLettersAndDigits>.ColumnNamesByPropertyName["Quoted"]);
        }

        [Table("Totals{}")]
        private sealed class EntityWithABraceInTheTableName
        {
            public int Id { get; set; }
        }

        [Table("Totals", Schema = "Odd{Schema")]
        private sealed class EntityWithABraceInTheSchemaName
        {
            public int Id { get; set; }
        }

        private sealed class EntityWithWhatDapperTakesForAParameterInAColumnName
        {
            public int Id { get; set; }

            [Column("Total@p_1_")]
            public int Total { get; set; }
        }

        [Fact]
        public void ANameForgetOrDapperWouldRewrite_ThrowsOnFirstAccess()
        {
            TypeInitializationException table = Assert.Throws<TypeInitializationException>(() => _ = EntityInfoCache<EntityWithABraceInTheTableName>.IdProperty);
            TypeInitializationException schema = Assert.Throws<TypeInitializationException>(() => _ = EntityInfoCache<EntityWithABraceInTheSchemaName>.IdProperty);
            TypeInitializationException column = Assert.Throws<TypeInitializationException>(() => _ = EntityInfoCache<EntityWithWhatDapperTakesForAParameterInAColumnName>.IdProperty);

            Assert.StartsWith("The table name 'Totals{}'", Assert.IsType<InvalidOperationException>(table.InnerException).Message);
            Assert.StartsWith("The schema name 'Odd{Schema'", Assert.IsType<InvalidOperationException>(schema.InnerException).Message);
            Assert.StartsWith("The column name 'Total@p_1_'", Assert.IsType<InvalidOperationException>(column.InnerException).Message);
        }

        private sealed class EntityWithNotMappedProperty
        {
            public int Id { get; set; }

            [NotMapped]
            public string Computed { get; set; } = string.Empty;
        }

        [Fact]
        public void NotMappedProperty_IsExcludedFromMappedProperties()
        {
            Assert.DoesNotContain(EntityInfoCache<EntityWithNotMappedProperty>.Properties, p => p.Name == "Computed");
        }

        private sealed class EntityWithColumnAttribute
        {
            public int Id { get; set; }

            [Column("full_name")]
            public string Name { get; set; } = string.Empty;
        }

        [Fact]
        public void ColumnAttribute_OverridesColumnName()
        {
            Assert.Equal("full_name", EntityInfoCache<EntityWithColumnAttribute>.ColumnNamesByPropertyName["Name"]);
        }

        private sealed class EntityWithDatabaseGeneratedIdentity
        {
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        [Fact]
        public void DatabaseGeneratedIdentityProperty_IsExcludedFromInsertAndUpdateProperties()
        {
            Assert.DoesNotContain(EntityInfoCache<EntityWithDatabaseGeneratedIdentity>.InsertProperties, p => p.Name == "Id");
            Assert.Contains(EntityInfoCache<EntityWithDatabaseGeneratedIdentity>.InsertProperties, p => p.Name == "Name");
            Assert.DoesNotContain(EntityInfoCache<EntityWithDatabaseGeneratedIdentity>.UpdateProperties, p => p.Name == "Id");
        }

        private sealed class EntityWithUpsertKey
        {
            public int Id { get; set; }

            [UpsertKey]
            public string Code { get; set; } = string.Empty;
        }

        [Fact]
        public void UpsertKeyAttribute_IsUsedInsteadOfIdentifier()
        {
            PropertyInfo item = Assert.Single(EntityInfoCache<EntityWithUpsertKey>.UpsertKeyProperties);
            Assert.Equal("Code", item.Name);
        }

        private sealed class EntityWithoutUpsertKey
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        [Fact]
        public void NoUpsertKeyAttribute_DefaultsToIdentifierProperty()
        {
            PropertyInfo item = Assert.Single(EntityInfoCache<EntityWithoutUpsertKey>.UpsertKeyProperties);
            Assert.Equal("Id", item.Name);
        }
    }
}
