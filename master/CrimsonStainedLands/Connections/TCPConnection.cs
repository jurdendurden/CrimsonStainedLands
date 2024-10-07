using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace CrimsonStainedLands.Connections;

public class TCPConnection : BaseConnection
{
    byte[] buffer = new byte[1024];

    public Encoding Encoding {get;set;}

    public NetworkStream Stream {get;set;}

    public TCPConnection(Socket socket) : base(socket)
    {
        this.Stream = new NetworkStream(socket);
        this.Status = ConnectionStatus.Connected;
    }

    public override async Task<byte[]> Read()
    {
        try 
        {
            if(!this.Stream.DataAvailable)
                return null;
            int read = await this.Stream.ReadAsync(buffer);
            
            if(read > 0)
            {
                return buffer.Take(read).ToArray();
            }
            else
                return null;
        }
        catch 
        {
            this.Status = ConnectionStatus.Disconnected;
            Cleanup();
            return null;
        }
    }

    public override async Task<int> Write(byte[] data) 
    {
        try 
        {

            if(data.Length > 0)
            {
                if(data[0] == (byte) TelnetNegotiator.Options.InterpretAsCommand)
                {
                    //var written = this.Socket.Send(data);
                    await this.Stream.WriteAsync(data);
                    return data.Length;
                }
                else
                {
                    //var written = this.Socket.Send(data);
                    await this.Stream.WriteAsync(data);
                    return data.Length;
                }
            }
            else
                return 0;
        }
        catch
        {
            this.Status = ConnectionStatus.Disconnected;
            return 0;
        }
    }

    public override BaseConnection.ConnectionStatus Status
    {
        get;set;
    }

    public override void Cleanup()
    {
        try
        {
            this.Stream.Dispose();
        }
        catch 
        {

        }
        try
        {
            this.Socket.Dispose();
        }
        catch
        {
        }
    }
}