using System.Linq;
using Marten.Services;
using Marten.Testing.Documents;
using Shouldly;
using Xunit;

namespace Marten.Testing.Bugs
{
    public class Bug_260_skip_take_projection_query_fails : DocumentSessionFixture<NulloIdentityMap>
    {
        [Fact]
        public void thr_wrong_number_of_records_are_returned()
        {
            Add100Users();

            var ids = theSession.Query<User>()
                        .Skip(10)
                        .Take(10)
                        .Select(entity => entity.Id)
                        .ToArray();

            ids.Length.ShouldBe(10);
        }

        private void Add100Users()
        {
            for (int i = 0; i < 100; i++)
            {
                theSession.Store(new User());
            }
            theSession.SaveChanges();
        }
    }
}