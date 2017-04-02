using Grpc.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Messages.EmployeeService;
using Messages;

namespace GrpcServer
{
    public class Program
    {
        const int Port = 9001;
        public static void Main(string[] args)
        {
            var cacert = File.ReadAllText(@"ca.crt");
            var cert = File.ReadAllText(@"server.crt");
            var key = File.ReadAllText(@"server.key");
            var keypair = new KeyCertificatePair(cert, key);
            var sslCredentials = new SslServerCredentials(new List<KeyCertificatePair>()
            {
                keypair
            }, cacert, true);

            Server server = new Server
            {
                Ports = { new ServerPort("0.0.0.0", Port, sslCredentials) },
                Services = { BindService(new EmployeeService()) }
            };
            server.Start();

            Console.WriteLine($"Starting server on port {Port}");
            Console.WriteLine("Press any key to stop...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }

        public class EmployeeService : EmployeeServiceBase
        {
            public override async Task<EmployeeResponse> GetByBadgeNumber(GetByBadgeNumberRequest request, ServerCallContext context)
            {
                Metadata md = context.RequestHeaders;
                if (md != null)
                {
                    foreach (var entry in md)
                    {
                        Console.WriteLine($"{entry.Key}: {entry.Value}");
                    }
                }

                foreach (var e in Employees.employees)
                {
                    if (request.BadgeNumber == e.BadgeNumber)
                    {
                        return new EmployeeResponse()
                        {
                            Employee = e
                        };
                    }
                }

                throw new Exception($"Employee not found with Badge Number: {request.BadgeNumber}");
            }

            public override async Task GetAll(GetAllRequest request, IServerStreamWriter<EmployeeResponse> responseStream, ServerCallContext context)
            {
                foreach (var e in Employees.employees)
                {
                    await responseStream.WriteAsync(new EmployeeResponse()
                    {
                        Employee = e
                    });
                }
            }

            public override async Task<AddPhotoResponse> AddPhoto(
                IAsyncStreamReader<AddPhotoRequest> requestStream,
                ServerCallContext context)
            {
                Metadata md = context.RequestHeaders;
                foreach (var entry in md)
                {
                    if (entry.Key.Equals("badgenumber", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Console.WriteLine("Receiving photo for badgenumber: " + entry.Value);
                        break;
                    }
                }

                var data = new List<byte>();
                while (await requestStream.MoveNext())
                {
                    Console.WriteLine("Received " +
                        requestStream.Current.Data.Length + " bytes");
                    data.AddRange(requestStream.Current.Data);
                }
                Console.WriteLine("Received file with " + data.Count + " bytes");

                return new AddPhotoResponse()
                {
                    IsOk = true
                };

            }

            public override async Task SaveAll(
                IAsyncStreamReader<EmployeeRequest> requestStream,
                IServerStreamWriter<EmployeeResponse> responseStream,
                ServerCallContext context)
            {
                while (await requestStream.MoveNext())
                {
                    var employee = requestStream.Current.Employee;
                    lock (this)
                    {
                        Employees.employees.Add(employee);
                    }

                    await responseStream.WriteAsync(new EmployeeResponse()
                    {
                        Employee = employee
                    });
                }

                Console.WriteLine("Employees");
                foreach (var e in Employees.employees)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
