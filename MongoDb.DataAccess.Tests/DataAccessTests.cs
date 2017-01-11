using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDb.DataAccess.Exceptions;
using MongoDb.DataAccess.Tests.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDb.DataAccess.Tests
{
    [TestClass]
    public class DataAccessTests : TestBase
    {
        private readonly IMongoRepository<Employee> employeeRepo;

        public DataAccessTests()
        {
            Init();
            employeeRepo = mongoTestDbContext.GetRepository<Employee>();
        }

        [TestInitialize]
        public void TestInitialize()
        {
            //cleanup
            employeeRepo.DeleteAll();
        }

        [TestMethod]
        public void test_insert()
        {
            var tasks = new List<Task>();
            var taskFactory = new TaskFactory();
            for (var i = 0; i < 10; i++)
            {
                var i1 = i;
                tasks.Add(taskFactory.StartNew(() =>
                {
                    var emp = new Employee
                    {
                        FirstName = "John" + i1,
                        LastName = "Smith" + i1
                    };

                    employeeRepo.Insert(emp);
                }));
            }

            Task.WaitAll(tasks.ToArray());
            Thread.Sleep(new TimeSpan(0, 0, 10));

            IEnumerable<Employee> list = employeeRepo.Find(e => e.FirstName.Contains("John")).ToList();
            list.Count().Should().Be(10);
        }

        [TestMethod]
        public void test_insert_async()
        {
            var tasks = new List<Task>();
            for (var i = 0; i < 10; i++)
            {
                var i1 = i;

                var emp = new Employee
                {
                    FirstName = "John" + i1,
                    LastName = "Smith" + i1
                };

                tasks.Add(employeeRepo.InsertAsync(emp));
            }

            Task.WaitAll(tasks.ToArray());
            Thread.Sleep(new TimeSpan(0, 0, 10));

            IEnumerable<Employee> list = employeeRepo.Find(e => e.FirstName.Contains("John")).ToList();
            list.Count().Should().Be(10);
        }

        [TestMethod]
        public void test_upsert()
        {
            var tasks = new List<Task>();
            var taskFactory = new TaskFactory();
            //insert
            for (var i = 0; i < 10; i++)
            {
                var i1 = i;
                tasks.Add(taskFactory.StartNew(() =>
                {
                    var emp = new Employee
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        FirstName = "John" + i1,
                        LastName = "Smith" + i1
                    };

                    employeeRepo.Upsert(emp);
                }));
            }

            Task.WaitAll(tasks.ToArray());
            tasks.Clear();

            IEnumerable<Employee> list = employeeRepo.Find(e => e.FirstName.Contains("John")).ToList();
            list.Count().Should().Be(10);
            //update
            foreach (var employee in list)
                tasks.Add(taskFactory.StartNew(() =>
                {
                    employee.LastName += employee.LastName + "Last";
                    employeeRepo.Upsert(employee);
                }));
            Task.WaitAll(tasks.ToArray());
            list = employeeRepo.Find(e => e.LastName.Contains("Last")).ToList();
            list.Count().Should().Be(10);
        }

        [TestMethod]
        public void test_upsert_async()
        {
            var tasks = new List<Task>();
            for (var i = 0; i < 10; i++)
            {
                var i1 = i;

                var emp = new Employee
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    FirstName = "John" + i1,
                    LastName = "Smith" + i1
                };

                tasks.Add(employeeRepo.UpsertAsync(emp));
            }

            Task.WaitAll(tasks.ToArray());

            IEnumerable<Employee> list = employeeRepo.Find(e => e.FirstName.Contains("John")).ToList();
            list.Count().Should().Be(10);
            list.First().LastUpdateDateTime.Should().BeBefore(DateTime.UtcNow);
            list.Last().LastUpdateDateTime.Should().BeBefore(DateTime.UtcNow);
        }

        [TestMethod]
        public void test_upsert_async_concurrency()
        {
            var emp = new Employee
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FirstName = "Atherton",
                LastName = "Smith",
                Address =
                    new Address {Street = "123 main st", City = "Salt Lake City", State = "UT", Zip = "12456"}
            };

            var res = employeeRepo.UpsertAsync(emp, true, false).Result;
            IEnumerable<Employee> list = employeeRepo.Find(e => e.FirstName == "Atherton").ToList();
            list.Count().Should().Be(1);
            var orgEmp = list.First();
            var orgEmp1 = new Employee
            {
                Id = orgEmp.Id,
                LastUpdateDateTime = orgEmp.LastUpdateDateTime,
                Address =
                    new Address
                    {
                        City = orgEmp.Address.City,
                        State = orgEmp.Address.State,
                        Street = orgEmp.Address.Street,
                        Zip = orgEmp.Address.Zip
                    },
                CustomData = orgEmp.CustomData,
                FirstName = orgEmp.FirstName,
                LastName = orgEmp.LastName
            };

            orgEmp.FirstName = "Michael";
            var result = employeeRepo.UpsertAsync(orgEmp, true, false).Result;

            try
            {
                var res1 = employeeRepo.UpsertAsync(orgEmp1, true, false).Result;
            }
            catch (Exception excep)
            {
                excep.InnerException.Should().BeOfType<MongoConcurrencyException>();
            }
        }

        [TestMethod]
        public void test_upsert_concurrency()
        {
            var emp = new Employee
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FirstName = "Atherton",
                LastName = "Smith",
                Address =
                    new Address {Street = "123 main st", City = "Salt Lake City", State = "UT", Zip = "12456"}
            };

            var res = employeeRepo.Upsert(emp, true, false);
            IEnumerable<Employee> list = employeeRepo.Find(e => e.FirstName == "Atherton").ToList();
            list.Count().Should().Be(1);
            var orgEmp = list.First();
            var orgEmp1 = new Employee
            {
                Id = orgEmp.Id,
                LastUpdateDateTime = orgEmp.LastUpdateDateTime,
                Address =
                    new Address
                    {
                        City = orgEmp.Address.City,
                        State = orgEmp.Address.State,
                        Street = orgEmp.Address.Street,
                        Zip = orgEmp.Address.Zip
                    },
                CustomData = orgEmp.CustomData,
                FirstName = orgEmp.FirstName,
                LastName = orgEmp.LastName
            };

            orgEmp.FirstName = "Michael";
            var result = employeeRepo.Upsert(orgEmp, true, false);

            try
            {
                var res1 = employeeRepo.Upsert(orgEmp1, true, false);
            }
            catch (Exception excep)
            {
                excep.Should().BeOfType<MongoConcurrencyException>();
            }
        }

        [TestMethod]
        public void test_update()
        {
            var emp = new Employee
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FirstName = "John",
                LastName = "Smith"
            };

            employeeRepo.Upsert(emp);

            IEnumerable<Employee> list = employeeRepo.Find(e => e.FirstName.Contains("John")).ToList();
            list.Count().Should().Be(1);

            employeeRepo.Update(e => e.FirstName.Contains("John"),
                new Dictionary<string, object> {{"FirstName", "Mike"}});
            list = employeeRepo.Find(e => e.FirstName.Contains("Mike")).ToList();
            list.Count().Should().Be(1);
            list.First().LastUpdateDateTime.Should().BeBefore(DateTime.UtcNow);
        }

        [TestMethod]
        public void test_update_async()
        {
            var emp = new Employee
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FirstName = "John",
                LastName = "Smith"
            };


            employeeRepo.Upsert(emp);

            IEnumerable<Employee> list = employeeRepo.Find(e => e.FirstName.Contains("John")).ToList();
            list.Count().Should().Be(1);

            var updateAsync = employeeRepo.UpdateAsync(e => e.FirstName.Contains("John"),
                new Dictionary<string, object> {{"FirstName", "Mike"}}).Result;

            list = employeeRepo.Find(e => e.FirstName.Contains("Mike")).ToList();
            list.Count().Should().Be(1);
        }

        [TestMethod]
        public void test_multifield_multidocs_update()
        {
            var tasks = new List<Task>();
            var taskFactory = new TaskFactory();
            for (var i = 0; i < 10; i++)
            {
                var i1 = i;
                tasks.Add(taskFactory.StartNew(() =>
                {
                    var emp = new Employee
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        FirstName = "John" + i1,
                        LastName = "Smith" + i1,
                        Address =
                            new Address {Street = "123 main st", City = "Salt Lake City", State = "UT", Zip = "12456"}
                    };

                    employeeRepo.Upsert(emp);
                }));
            }

            Task.WaitAll(tasks.ToArray());

            IEnumerable<Employee> list = employeeRepo.Find(e => e.FirstName.Contains("John")).ToList();
            list.Count().Should().Be(10);


            employeeRepo.Update(e => e.FirstName.Contains("John"),
                new Dictionary<string, object> {{"LastName", "Barry"}, {"FirstName", "Bush"}, {"Address.State", "Utah"}});
            var employees = employeeRepo.Find(e => e.LastName.Contains("Barry")).ToList();
            employees.Count().Should().Be(10);
            employees.First().Address.State.Should().Be("Utah");
            employees.Last().Address.State.Should().Be("Utah");
            employeeRepo.Find(e => e.FirstName.Contains("Bush")).ToList().Count().Should().Be(10);
        }

        [TestMethod]
        public void test_delete()
        {
            var tasks = new List<Task>();
            var taskFactory = new TaskFactory();
            for (var i = 0; i < 10; i++)
            {
                var i1 = i;
                tasks.Add(taskFactory.StartNew(() =>
                {
                    var emp = new Employee
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        FirstName = "John" + i1,
                        LastName = "Smith" + i1
                    };

                    employeeRepo.Upsert(emp);
                }));
            }

            Task.WaitAll(tasks.ToArray());

            IEnumerable<Employee> list = employeeRepo.Find(e => e.FirstName.Contains("John")).ToList();
            list.Count().Should().Be(10);

            employeeRepo.Delete(list.First().Id);

            list = employeeRepo.Find(e => e.FirstName.Contains("John")).ToList();
            list.Count().Should().Be(9);
        }

        [TestMethod]
        public void test_delete_async()
        {
            var tasks = new List<Task>();
            for (var i = 0; i < 10; i++)
            {
                var i1 = i;
                var emp = new Employee
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    FirstName = "John" + i1,
                    LastName = "Smith" + i1
                };

                tasks.Add(employeeRepo.UpsertAsync(emp));
            }

            Task.WaitAll(tasks.ToArray());

            IEnumerable<Employee> list = employeeRepo.Find(e => e.FirstName.Contains("John")).ToList();
            list.Count().Should().Be(10);

            employeeRepo.Delete(list.First().Id);

            list = employeeRepo.Find(e => e.FirstName.Contains("John")).ToList();
            list.Count().Should().Be(9);
        }

        [TestMethod]
        public void test_delete_with_filter()
        {
            var emp = new Employee
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FirstName = "John1",
                LastName = "Smith",
                Address = new Address {Street = "123 main st", City = "Salt Lake City", State = "UT", Zip = "12456"},
                CustomData =
                    BsonDocument.Parse(
                        @"{ ""MedicalProvider"" : ""CIGNA"", ""GroupNo"" : ""34556"", ""ID"" : ""ABC1234""}")
            };

            employeeRepo.Upsert(emp);

            emp = new Employee
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FirstName = "John2",
                LastName = "Smith",
                Address = new Address {Street = "123 main st", City = "Salt Lake City", State = "UT", Zip = "12456"},
                CustomData =
                    BsonDocument.Parse(
                        @"{ ""MedicalProvider"" : ""CIGNA"", ""GroupNo"" : ""34556"", ""ID"" : ""ABC1234""}")
            };

            employeeRepo.Upsert(emp);

            emp = new Employee
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FirstName = "John3",
                LastName = "Smith",
                Address = new Address {Street = "123 main st", City = "Salt Lake City", State = "UT", Zip = "12456"},
                CustomData =
                    BsonDocument.Parse(
                        @"{ ""MedicalProvider"" : ""KISER"", ""GroupNo"" : ""34556"", ""ID"" : ""ABC1234""}")
            };

            employeeRepo.Upsert(emp);

            employeeRepo.Find(e => e.FirstName.Contains("John")).ToList().Count.Should().Be(3);

            employeeRepo.DeleteWithFilter("{\"CustomData.MedicalProvider\" :\"KISER\" }").Should().Be(1);

            employeeRepo.Find(e => e.FirstName.Contains("John")).ToList().Count.Should().Be(2);
        }

        [TestMethod]
        public void test_delete_with_filter_async()
        {
            var emp = new Employee
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FirstName = "John1",
                LastName = "Smith",
                Address = new Address {Street = "123 main st", City = "Salt Lake City", State = "UT", Zip = "12456"},
                CustomData =
                    BsonDocument.Parse(
                        @"{ ""MedicalProvider"" : ""CIGNA"", ""GroupNo"" : ""34556"", ""ID"" : ""ABC1234""}")
            };

            employeeRepo.Upsert(emp);

            emp = new Employee
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FirstName = "John2",
                LastName = "Smith",
                Address = new Address {Street = "123 main st", City = "Salt Lake City", State = "UT", Zip = "12456"},
                CustomData =
                    BsonDocument.Parse(
                        @"{ ""MedicalProvider"" : ""CIGNA"", ""GroupNo"" : ""34556"", ""ID"" : ""ABC1234""}")
            };

            employeeRepo.Upsert(emp);

            emp = new Employee
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FirstName = "John3",
                LastName = "Smith",
                Address = new Address {Street = "123 main st", City = "Salt Lake City", State = "UT", Zip = "12456"},
                CustomData =
                    BsonDocument.Parse(
                        @"{ ""MedicalProvider"" : ""KISER"", ""GroupNo"" : ""34556"", ""ID"" : ""ABC1234""}")
            };

            employeeRepo.Upsert(emp);

            employeeRepo.DeleteWithFilterAsync("{\"CustomData.MedicalProvider\" :\"CIGNA\" }")
                .ContinueWith(
                    task =>
                        employeeRepo.Find("{\"CustomData.MedicalProvider\" :\"CIGNA\" }")
                            .ToList()
                            .Count.Should()
                            .Be(0));
        }

        [TestMethod]
        public void test_deleteall()
        {
            var tasks = new List<Task>();
            var taskFactory = new TaskFactory();
            for (var i = 0; i < 10; i++)
            {
                var i1 = i;
                tasks.Add(taskFactory.StartNew(() =>
                {
                    var emp = new Employee
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        FirstName = "John" + i1,
                        LastName = "Smith" + i1
                    };

                    employeeRepo.Upsert(emp);
                }));
            }

            Task.WaitAll(tasks.ToArray());

            IEnumerable<Employee> list = employeeRepo.Find(e => e.FirstName.Contains("John")).ToList();
            list.Count().Should().Be(10);

            employeeRepo.DeleteAll();

            list = employeeRepo.Find(e => true).ToList();
            list.Count().Should().Be(0);
        }

        [TestMethod]
        public void test_deleteall_aysnc()
        {
            var tasks = new List<Task>();
            var taskFactory = new TaskFactory();
            for (var i = 0; i < 10; i++)
            {
                var i1 = i;
                tasks.Add(taskFactory.StartNew(() =>
                {
                    var emp = new Employee
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        FirstName = "John" + i1,
                        LastName = "Smith" + i1
                    };

                    employeeRepo.Upsert(emp);
                }));
            }

            Task.WaitAll(tasks.ToArray());

            IEnumerable<Employee> list = employeeRepo.Find(e => e.FirstName.Contains("John")).ToList();
            list.Count().Should().Be(10);

            employeeRepo.DeleteAllAsync().ContinueWith(task =>
            {
                list = employeeRepo.Find(e => true).ToList();
                list.Count().Should().Be(0);
            });
        }

        [TestMethod]
        public void test_find()
        {
            var emp = new Employee
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FirstName = "John",
                LastName = "Rubbish"
            };

            employeeRepo.Upsert(emp);

            emp = new Employee
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FirstName = "John",
                LastName = "Cool"
            };

            employeeRepo.Upsert(emp);

            emp = new Employee
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FirstName = "Barry",
                LastName = "Cool"
            };

            employeeRepo.Upsert(emp);

            IEnumerable<Employee> list = employeeRepo.Find(e => e.FirstName == "John").ToList();
            list.Count().Should().Be(2);

            list = employeeRepo.Find(e => e.LastName == "Cool").ToList();
            list.Count().Should().Be(2);
        }

        [TestMethod]
        public void test_find_async()
        {
            var emp = new Employee
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FirstName = "John",
                LastName = "Rubbish"
            };

            employeeRepo.Upsert(emp);

            emp = new Employee
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FirstName = "John",
                LastName = "Cool"
            };

            employeeRepo.Upsert(emp);

            emp = new Employee
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FirstName = "Barry",
                LastName = "Cool"
            };

            employeeRepo.Upsert(emp);

            var list = employeeRepo.FindAsync(e => e.FirstName == "John").Result;
            list.Count().Should().Be(2);

            list = employeeRepo.Find(e => e.LastName == "Cool").ToList();
            list.Count().Should().Be(2);
        }

        [TestMethod]
        public void test_find_with_filter()
        {
            var emp = new Employee
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FirstName = "John1",
                LastName = "Smith",
                Address = new Address {Street = "123 main st", City = "Salt Lake City", State = "UT", Zip = "12456"},
                CustomData =
                    BsonDocument.Parse(
                        @"{ ""MedicalProvider"" : ""CIGNA"", ""GroupNo"" : ""34556"", ""ID"" : ""ABC1234""}")
            };

            employeeRepo.Upsert(emp);

            emp = new Employee
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FirstName = "John2",
                LastName = "Smith",
                Address = new Address {Street = "123 main st", City = "Salt Lake City", State = "UT", Zip = "12456"},
                CustomData =
                    BsonDocument.Parse(
                        @"{ ""MedicalProvider"" : ""CIGNA"", ""GroupNo"" : ""34556"", ""ID"" : ""ABC1234""}")
            };

            employeeRepo.Upsert(emp);

            emp = new Employee
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FirstName = "John3",
                LastName = "Smith",
                Address = new Address {Street = "123 main st", City = "Salt Lake City", State = "UT", Zip = "12456"},
                CustomData =
                    BsonDocument.Parse(
                        @"{ ""MedicalProvider"" : ""KISER"", ""GroupNo"" : ""34556"", ""ID"" : ""ABC1234""}")
            };

            employeeRepo.Upsert(emp);


            employeeRepo.Find(Builders<BsonDocument>.Filter.Eq("CustomData.MedicalProvider", "CIGNA"))
                .ToList()
                .Count.Should()
                .Be(2);

            employeeRepo.Find(@"{""CustomData.MedicalProvider"": ""CIGNA""}")
                .ToList()
                .Count.Should()
                .Be(2);

            employeeRepo.Find(@"{""CustomData.MedicalProvider"": ""CGNA""}")
                .ToList()
                .Count.Should()
                .Be(0);
        }

        [TestMethod]
        public void test_filter_and_page_with_IQueryable()
        {
            for (var i = 0; i < 100; i++)
            {
                var emp = new Employee
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    FirstName = "John" + i,
                    LastName = "Rubbish"
                };

                employeeRepo.Upsert(emp);
            }

            var employees = employeeRepo.Entities.Where(e => e.LastName == "Rubbish").Take(100);
            employees.Count().Should().Be(100);

            employees = employeeRepo.Entities.Where(e => e.FirstName.Contains("John")).Skip(80).Take(100);
            employees.Count().Should().Be(20);
        }
    }
}