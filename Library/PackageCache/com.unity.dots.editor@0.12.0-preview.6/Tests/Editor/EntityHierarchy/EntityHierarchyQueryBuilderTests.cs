using NUnit.Framework;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.PerformanceTesting;

namespace Unity.Entities.Editor.Tests
{
    class EntityHierarchyQueryBuilderTests
    {
        EntityHierarchyQueryBuilder m_Builder;

        [OneTimeSetUp]
        public void Setup()
        {
            m_Builder = new EntityHierarchyQueryBuilder();
        }

        public static IEnumerable ParseQueryCaseSource()
        {
            yield return new TestCaseData("       ", false);
            yield return new TestCaseData("", false);
            yield return new TestCaseData(string.Empty, false);
            yield return new TestCaseData("some random string", false);
            yield return new TestCaseData("c:SomeTypeThatDoesntExist", false);
            yield return new TestCaseData($"c:{nameof(EcsTestData)}", true);
            yield return new TestCaseData($"c: {nameof(EcsTestData)}", true);
            yield return new TestCaseData($"C:{nameof(EcsTestData)}", true);
            yield return new TestCaseData($"C: {nameof(EcsTestData)}", true);
            yield return new TestCaseData($"someothertextc:{nameof(EcsTestData)}", false);
            yield return new TestCaseData($"c:!{nameof(EcsTestData)}", false);
            yield return new TestCaseData($"C:!{nameof(EcsTestData)}", false);
            yield return new TestCaseData($"c: !{nameof(EcsTestData)}", false);
            yield return new TestCaseData($"c:{nameof(EcsTestData2)} c:{nameof(EcsTestData)}", true);
            yield return new TestCaseData($"c:{nameof(EcsTestData2)} c:{nameof(EcsTestSharedComp)}", true);
            yield return new TestCaseData($"with c:{nameof(EcsTestData2)} some c:{nameof(EcsTestData)} text c:{nameof(EcsTestSharedComp)} in between", true);
        }

        [TestCaseSource(nameof(ParseQueryCaseSource))]
        public void QueryBuilder_ParseQuery(string input, bool isQueryExpected)
        {
            var r = m_Builder.BuildQuery(input);
            if (isQueryExpected)
                Assert.That(r.QueryDesc, Is.Not.Null, $"input \"{input}\" should be a valid input to build a query");
            else
                Assert.That(r.QueryDesc, Is.Null, $"input \"{input}\" should not be a valid input to build a query");
        }

        [Test]
        [TestCaseSource(nameof(EnumerateAllCategories))]
        public void QueryBuilder_ResultQueryContainsExpectedTypes(TypeManager.TypeCategory typeCategory)
        {
            var componentType = TypeManager.AllTypes.First(x => x.Category == typeCategory && x.Type != null && x.Type.Name != x.Type.FullName).Type;

            var r = m_Builder.BuildQuery($"c:{componentType.FullName}");
            Assert.That(r.IsValid, Is.True);
            Assert.That(r.QueryDesc.Any, Is.EquivalentTo(new ComponentType[] { componentType }));
            Assert.That(r.QueryDesc.None, Is.Empty);
            Assert.That(r.QueryDesc.All, Is.Empty);
        }

        [Test]
        public void QueryBuilder_ResultQueryIncludesPrefabsAndDisabledEntities()
        {
            var r = m_Builder.BuildQuery($"c:{typeof(EntityGuid).FullName}");
            Assert.That(r.QueryDesc.Options & EntityQueryOptions.IncludeDisabled, Is.EqualTo(EntityQueryOptions.IncludeDisabled));
            Assert.That(r.QueryDesc.Options & EntityQueryOptions.IncludePrefab, Is.EqualTo(EntityQueryOptions.IncludePrefab));
        }

        [Test]
        public void QueryBuilder_ExtractUnmatchedString()
        {
            var r = m_Builder.BuildQuery($"with c:{nameof(EcsTestData2)} some c:{nameof(EcsTestData)} text c:{nameof(EcsTestSharedComp)} in between");
            Assert.That(r.Filter, Is.EqualTo("with  some  text  in between"));
        }

        [Test]
        public void QueryBuilder_ReportStatusAndErrorComponentType()
        {
            var erroneousComponentType = nameof(EcsTestData) + Guid.NewGuid().ToString("N");
            var r = m_Builder.BuildQuery($"c:{erroneousComponentType}");

            Assert.That(r, Is.EqualTo(EntityHierarchyQueryBuilder.Result.Invalid(erroneousComponentType)));
        }

        [Test]
        public void QueryBuilder_ExtractUnmatchedStringWhenNotMatchingAnything()
        {
            var r = m_Builder.BuildQuery($"hola");
            Assert.That(r.QueryDesc, Is.Null);
            Assert.That(r.Filter, Is.EqualTo("hola"));
        }

        [Test, Performance]
        public void QueryBuilder_PerformanceTests()
        {
            var types = TypeManager
                .GetAllTypes()
                .Where(t => t.Type != null && (t.Category == TypeManager.TypeCategory.ComponentData || t.Category == TypeManager.TypeCategory.ISharedComponentData)).Take(50).ToArray();
            var inputString = new StringBuilder();
            for (var i = 0; i < types.Length; i++)
            {
                inputString.AppendFormat("c:{0}{1} ble ", i % 2 == 0 ? "!" : string.Empty, types[i].Type.Namespace);
            }

            var input = inputString.ToString();

            Measure.Method(() =>
                {
                    m_Builder.BuildQuery(input);
                })
                .SampleGroup($"Build query from string input containing {types.Length} type constraints")
                .WarmupCount(10)
                .MeasurementCount(1000)
                .Run();
        }

        static IEnumerable EnumerateAllCategories()
        {
            var values = Enum.GetValues(typeof(TypeManager.TypeCategory));
            foreach (var value in values)
            {
                yield return value;
            }
        }

        [Test]
        [TestCaseSource(nameof(EnumerateAllCategories))]
        public void QueryBuilder_EnsureAtLeastOneTypeExistsPerCategory(TypeManager.TypeCategory category)
        {
            Assert.That(TypeManager.AllTypes.Where(x => x.Category == category), Is.Not.Empty);
        }

        [Test]
        [TestCaseSource(nameof(EnumerateAllCategories))]
        public void QueryBuilder_EnsureAllCategoriesAreSupportedInQuery(TypeManager.TypeCategory category)
        {
            var w = new World("test");
            try
            {
                var t = TypeManager.AllTypes.First(x => x.Category == category && x.Type != null);
                using (var q = w.EntityManager.CreateEntityQuery(t.Type))
                {
                    Assert.DoesNotThrow(() =>
                    {
                        var entities = q.ToEntityArray(Allocator.TempJob);
                        entities.Dispose();
                    });
                }
            }
            finally
            {
                w.Dispose();
            }
        }
    }
}
