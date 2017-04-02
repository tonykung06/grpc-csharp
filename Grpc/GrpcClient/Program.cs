using Google.Protobuf;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Messages.EmployeeService;
using Messages;

namespace GrpcClient
{
    public class Program
    {
        const int Port = 9001;
        public static void Main(string[] args)
        {
            var option = int.Parse(args[0]);

            var cacert = File.ReadAllText(@"ca.crt");
            var cert = File.ReadAllText(@"client.crt");
            var key = File.ReadAllText(@"client.key");
            var keypair = new KeyCertificatePair(cert, key);
            SslCredentials creds = new SslCredentials(cacert, keypair);
            var channel = new Channel("127.0.0.1", Port, ChannelCredentials.Insecure);
            var client = new EmployeeServiceClient(channel);

            Console.WriteLine($"Make request number {option}");
            switch (option)
            {
                case 1:
                    SendMetadataAsync(client).Wait();
                    break;
                case 2:
                    GetByBadgeNumber(client).Wait();
                    break;
                case 3:
                    GetAll(client).Wait();
                    break;
                case 4:
                    AddPhoto(client).Wait();
                    break;
                case 5:
                    SaveAll(client).Wait();
                    break;
            }
        }

        public static async Task SendMetadataAsync(EmployeeServiceClient client)
        {
            Metadata md = new Metadata();
            md.Add("username", "tony");
            md.Add("password", "password1");
            try
            {
                var response = await client.GetByBadgeNumberAsync(new GetByBadgeNumberRequest(), md);
                Console.WriteLine($"{response.Employee.FirstName} {response.Employee.LastName}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to send matadata, error: {e.Message}");
            }

        }
        public static async Task GetByBadgeNumber(EmployeeServiceClient client)
        {
            var res = await client.GetByBadgeNumberAsync(new Messages.GetByBadgeNumberRequest() { BadgeNumber = 2080 });
            Console.WriteLine(res.Employee);
        }

        public static async Task GetAll(EmployeeServiceClient client)
        {
            using (var call = client.GetAll(new Messages.GetAllRequest()))
            {
                var responseStream = call.ResponseStream;
                while (await responseStream.MoveNext())
                {
                    Console.WriteLine(responseStream.Current.Employee);
                }
            }
        }

        public static async Task AddPhoto(EmployeeServiceClient client)
        {
            Metadata md = new Metadata();
            md.Add("badgenumber", "2080");

            FileStream fs = File.OpenRead("Penguins.jpg");
            using (var call = client.AddPhoto())
            {
                var stream = call.RequestStream;
                while (true)
                {
                    byte[] buffer = new byte[64 * 1024];
                    int numRead = await fs.ReadAsync(buffer, 0, buffer.Length);
                    if (numRead == 0)
                    {
                        break;
                    }
                    if (numRead < buffer.Length)
                    {
                        Array.Resize(ref buffer, numRead);
                    }

                    await stream.WriteAsync(new Messages.AddPhotoRequest() { Data = ByteString.CopyFrom(buffer) });
                }
                await stream.CompleteAsync();

                var res = await call.ResponseAsync;

                Console.WriteLine(res.IsOk);
            }

        }

        private static async Task SaveAll(EmployeeServiceClient client)
        {
            var employees = new List<Employee>()
            {
                new Employee{
                    BadgeNumber= 123,
                    FirstName= "John",
                    LastName= "Smith",
                    VacationAccrualRate= 1.2f,
                    VacationAccrued= 0,
                },
                new Employee{
                    BadgeNumber= 234,
                    FirstName= "Lisa",
                    LastName= "Wu",
                    VacationAccrualRate= 1.7f,
                    VacationAccrued= 10,
                }
            };
            using (var call = client.SaveAll())
            {
                var requestStream = call.RequestStream;
                var responseStream = call.ResponseStream;

                var responseTask = Task.Run(async () =>
                {
                    while (await responseStream.MoveNext())
                    {
                        Console.WriteLine("Saved: " + responseStream.Current.Employee);
                    }
                });

                foreach (var e in employees)
                {
                    await requestStream.WriteAsync(new EmployeeRequest() { Employee = e });
                }
                await call.RequestStream.CompleteAsync();
                await responseTask;
            }
        }
    }
}
