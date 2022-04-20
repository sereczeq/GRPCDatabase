// See https://aka.ms/new-console-template for more information

using Grpc.Core;
using Grpc.Net.Client;
using Server;

var channel = GrpcChannel.ForAddress("http://localhost:5266");
var client = new Greeter.GreeterClient(channel);

Console.WriteLine($"Client started at channel {channel.Target}");
Console.WriteLine("Press any key to send request");
Console.ReadKey();

BasicGreet(client);
Task.Run(() => GetPersonData(client)).ConfigureAwait(false);
// var customerData = GetPersonData(client);
await GetFibonacciSequence(client);
await GetFile(client, @"D:\Uni\Distributed\GRPCDatabase\GRPCDatabase\Server\Database\cheese.png");

Console.WriteLine("Press any key to quit...");
Console.ReadKey();


void BasicGreet(Greeter.GreeterClient greeterClient)
{
    var reply = greeterClient.SayHello(new HelloRequest {Name = "hey"});
    Console.WriteLine(reply.Message);
}

async Task GetPersonData(Greeter.GreeterClient greeterClient)
{
    uint index = 2;
    Console.WriteLine($"Client Requesting Person data with index {index} Asynchronously");
    Thread.Sleep(2000);
    var response = await greeterClient.GetPersonDataAsync(new PersonDataRequest {Id = index});
    Console.WriteLine("Server responded with person data:\n" +
                      "Person\n" +
                      "{\n" +
                      $"\tPESEl\t{response.Pesel}\n" +
                      $"\tName\t{response.Name}\n" +
                      $"\tSurname\t{response.Surname}\n" +
                      $"\tAge\t{response.Age}\n" +
                      $"\tHeight\t{response.Height}\n" +
                      "}");
}

async Task GetFibonacciSequence(Greeter.GreeterClient client2)
{
    uint length = 10;
    Console.WriteLine($"Client requesting fibonacci sequence of length {length}");
    var response = client2.CalculateFibonacci(new FibonacciRequest {Number = length});
    var stream = response.ResponseStream;

    Console.WriteLine("Server starting to return a stream containing fibonacci sequence:");

    while (await stream.MoveNext(CancellationToken.None))
    {
        var number = stream.Current.Number;
        Console.WriteLine(number);
    }

    Console.WriteLine("Server returned all numbers");
}

static async Task GetFile(Greeter.GreeterClient client1, string filePath)
{
    var tempFileName = $"temp_{DateTime.UtcNow:yyyyMMdd_HHmmss}.tmp";
    const string saveFilePath = $@"D:\Uni\Distributed\GRPCDatabase\GRPCDatabase\Client\ReceivedFiles";
    var tempFilePath = saveFilePath + @"\" + tempFileName;
    
    var request = new FileRequest { FilePath = filePath };
    var finalFileName = tempFilePath;

    Console.WriteLine($"Requesting a file {filePath} and trying to store it at {tempFilePath}");
    
    
    using (var call = client1.DownloadFile(request))
    {
        await using (Stream fs = File.OpenWrite(tempFilePath))
        {
            await foreach (var chunkMsg in call.ResponseStream.ReadAllAsync().ConfigureAwait(false))
            {
                var tempFinalFilePath = chunkMsg.FileName;

                if (!string.IsNullOrEmpty(tempFinalFilePath))
                {
                    finalFileName = chunkMsg.FileName;
                }

                fs.Write(chunkMsg.Chunk.ToByteArray());
            }
        }
    }
    
    Console.WriteLine($"Finished downloading {tempFilePath}");
    
    var finalPath = saveFilePath + @"\" + finalFileName;

    if (File.Exists(finalPath))
    {
        Console.WriteLine($"File {filePath} already exists. Removing");
        File.Delete(finalPath);
    }
    
    File.Move(tempFilePath, finalPath);
    Console.WriteLine($"File saved as {finalPath}");
    
    
}