using Google.Protobuf;
using Grpc.Core;
using Server;

namespace Server.Services;

public class GreeterService : Greeter.GreeterBase
{
    private readonly ILogger<GreeterService> _logger;

    public GreeterService(ILogger<GreeterService> logger)
    {
        _logger = logger;
    }

    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HelloReply
        {
            Message = "Hello " + request.Name
        });
    }

    public override Task<PersonDataResponse> GetPersonData(PersonDataRequest request, ServerCallContext context)
    {
        var people = new List<PersonDataResponse>
        {
            new()
            {
                Pesel = "0000000000",
                Name = "Aaron",
                Surname = "Ageist",
                Age = 50,
                Height = 1.78f
            },
            new()
            {
                Pesel = "1111111111",
                Name = "Barney",
                Surname = "Barn",
                Age = 60,
                Height = 1.50f
            },
            new()
            {
                Pesel = "2222222222",
                Name = "Carol",
                Surname = "Charles",
                Age = 34,
                Height = 2f
            }
        };
        
        var id = (int) request.Id;
        id = Math.Clamp(id, 0, people.Count - 1);
        var person = people[id];
        return Task.FromResult(person);
    }

    public override Task CalculateFibonacci(FibonacciRequest request, IServerStreamWriter<FibonacciResponse> responseStream, ServerCallContext context)
    {
        var length = request.Number;
        int a = 0, b = 1;
        for (var i = 2; i < length; i++)  
        {  
            var fibonacci = a + b;
            responseStream.WriteAsync(new FibonacciResponse {Number = (uint)fibonacci});
            a= b;  
            b= fibonacci;  
        }
        
        return Task.CompletedTask;
    }
    
    public override async Task DownloadFile(FileRequest request, IServerStreamWriter<ChunkMsg> responseStream, ServerCallContext context)
    {
        string filePath = request.FilePath;

        if (!File.Exists(filePath)) { return; }

        FileInfo fileInfo = new FileInfo(filePath);

        ChunkMsg chunk = new ChunkMsg();
        chunk.FileName = Path.GetFileName(filePath);
        chunk.FileSize = fileInfo.Length;

        int fileChunkSize = 64 * 1024;

        byte[] fileByteArray = File.ReadAllBytes(filePath);
        byte[] fileChunk = new byte[fileChunkSize];
        int fileOffset = 0;

        while (fileOffset < fileByteArray.Length && !context.CancellationToken.IsCancellationRequested)
        {
            int length = Math.Min(fileChunkSize, fileByteArray.Length - fileOffset);
            Buffer.BlockCopy(fileByteArray, fileOffset, fileChunk, 0, length);
            fileOffset += length;
            ByteString byteString = ByteString.CopyFrom(fileChunk);

            chunk.Chunk = byteString;
            await responseStream.WriteAsync(chunk).ConfigureAwait(false);
        }            
    }
}