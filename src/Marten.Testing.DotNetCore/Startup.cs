
using System;
using Marten.Codegen;

namespace Marten.Testing.DotNetCore
{
    public static class Program
    {
        static void Main(string[] args)
        {
            var store = DocumentStore.For("Server=127.0.0.1;Port=5432;User Id=test_store_user;password=testStoreUserPassword;database=test_store;");
            AssemblyGenerator.HintPaths = new String[]
            {
                @"C:\_\Marten\git-tim-cools\src\artifacts\bin\Marten.Testing.DotNetCore\Debug\dnx451"
            };
            var a = typeof(Program).GetType();

            var user = new User();
            using (var session = store.DirtyTrackedSession())
            {
                session.Store(user);
                session.SaveChanges();
            }

            using (var session = store.DirtyTrackedSession())
            {
                var loaded = session.Load<User>(user.Id);
            }
            Console.ReadLine();
        }
    }
    public class User
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool Internal { get; set; }
        public string UserName { get; set; }
    }
}
