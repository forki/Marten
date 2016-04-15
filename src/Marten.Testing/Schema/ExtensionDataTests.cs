using System;
using System.Collections.Generic;
using Marten.Schema;
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
    }
}
