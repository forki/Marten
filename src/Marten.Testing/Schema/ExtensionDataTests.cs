using System;
using System.Collections.Generic;
using System.Linq;
using Marten.Util;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace Marten.Testing.Schema
{
    public class Subscription
    {
        public long Id { get; set; }
        public string Title { get; set; }

        [JsonExtensionData]
        public IDictionary<string, object> Tags { get; }

        public Subscription()
        {
            Tags = new Dictionary<string, object>();
        }
    }

    public class when_storing_a_document_with_extension_data : IntegratedFixture
    {
        [Fact]
        public void then_the_exetension_properties_should_be_reladed()
        {
            var now = DateTime.Now;

            var id = StoreDocument(now);

            using (var session = theStore.OpenSession())
            {
                var subscription = session.Load<Subscription>(id);

                subscription.Tags.ShouldContainKeyAndValue("string", "string");
                subscription.Tags.ShouldContainKeyAndValue("int", 12L);          // ints are stored as long
                subscription.Tags.ShouldContainKeyAndValue("long", 34L);
                subscription.Tags.ShouldContainKeyAndValue("float", 0.9);        // float is stored as double
                subscription.Tags.ShouldContainKeyAndValue("double", 1.2);
                subscription.Tags.ShouldContainKeyAndValue("decimal", 3.4);      // decimal is stored as double
                subscription.Tags.ShouldContainKeyAndValue("dateTime", now);
            }
        }

        private long StoreDocument(DateTime now)
        {
            using (var session = theStore.OpenSession())
            {
                var subscription = new Subscription
                {
                    Title = "title",
                    Tags =
                    {
                        {"string", "string"},
                        {"int", 12},
                        {"long", 34L},
                        {"float", 0.9f},
                        {"double", 1.2},
                        {"decimal", 3.4m},
                        {"dateTime", now}
                    }
                };

                session.Store(subscription);
                session.SaveChanges();

                return subscription.Id;
            }
        }

        [Fact]
        public void then_it_should_be_queryable_by_tag()
        {
            var tag = Guid.NewGuid().ToString("N");
            var id = StoreDocumentWithTag(tag);

            using (var session = theStore.OpenSession())
            {
                var subscription = session.Query<Subscription>()
                    .FirstOrDefault(where => where.Tags.ContainsKey(tag));

                subscription.Id.ShouldBe(id);
                subscription.Tags.ShouldContainKeyAndValue(tag, tag);
            }
        }

        [Fact]
        public void then_the_document_json_should_contain_the_tag()
        {
            var tag = Guid.NewGuid().ToString("N");
            var id = StoreDocumentWithTag(tag);

            using (var session = theStore.OpenSession())
            {
                var json = session.Query<Subscription>()
                    .FirstJson(where => where.Tags.ContainsKey(tag));

                json.ShouldBe("{\"Id\": 1, \"Title\": null, \"ed7f0e423f08412fa4acf294016734f7\": \"ed7f0e423f08412fa4acf294016734f7\"}");
            }
        }

        [Fact]
        public void then_it_should_not_be_queried_by_index()
        {
            var tag = Guid.NewGuid().ToString("N");
            var id = StoreDocument(DateTime.Now);

            using (var session = theStore.OpenSession())
            {
                var queryExplained = session.Query<Subscription>()
                    .Where(where => where.Tags.ContainsKey(tag))
                    .Explain();

                queryExplained.NodeType.ShouldBe("Seq Scan");
            }
        }

        [Fact]
        public void then_it_should_be_queryable_by_tag_by_constant()
        {
            var tag = "additional-key";
            var id = StoreDocumentWithTag(tag);

            using (var session = theStore.OpenSession())
            {
                var subscription = session.Query<Subscription>()
                    .FirstOrDefault(where => where.Tags.ContainsKey("additional-key"));

                subscription.Id.ShouldBe(id);
                subscription.Tags.ShouldContainKeyAndValue("tag", tag);
            }
        }

        [Fact]
        public void then_it_should_be_queryable_by_value()
        {
            var tag = Guid.NewGuid().ToString("N");
            var id = StoreDocumentWithTag(tag);

            using (var session = theStore.OpenSession())
            {
                var subscription = session.Query<Subscription>()
                    .FirstOrDefault(where => where.Tags[tag] == tag);

                subscription.Id.ShouldBe(id);
                subscription.Tags.ShouldContainKeyAndValue("tag", tag);
            }
        }

        private long StoreDocumentWithTag(string tag)
        {
            using (var session = theStore.OpenSession())
            {
                var subscription = new Subscription
                {
                    Tags = {{tag, tag}}
                };

                session.Store(subscription);
                session.SaveChanges();

                return subscription.Id;
            }
        }
    }

    public class when_storing_a_document_with_extension_data_and_index : IntegratedFixture
    {
        private readonly string _tag;

        public when_storing_a_document_with_extension_data_and_index()
        {
            _tag = Guid.NewGuid().ToString("N");
            StoreOptions(_ => _.Schema.For<Subscription>().GinIndexJsonData(definition => definition.Columns.Add("data -> '" + _tag + "'")));
        }

        [Fact]
        public void then_it_should_not_be_queried_by_index()
        {
            using (var session = theStore.OpenSession())
            {
                var subscription = new Subscription
                {
                    Tags = { { _tag, _tag } }
                };

                session.Store(subscription);
                session.SaveChanges();
            }

            using (var session = theStore.OpenSession())
            {
                var queryExplained = session.Query<Subscription>()
                    .Where(where => where.Tags.ContainsKey(_tag))
                    .Explain();

                queryExplained.NodeType.ShouldNotBe("Seq Scan");
            }
        }
    }
}
