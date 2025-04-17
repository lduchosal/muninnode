using System.Collections.Concurrent;
using System.Net.Sockets;

namespace Program.AsyncSocket;

class SocketAsyncEventArgsPool : IDisposable
{
    private readonly ConcurrentStack<SocketAsyncEventArgs> _pool;

    public SocketAsyncEventArgsPool()
    {
        _pool = new ConcurrentStack<SocketAsyncEventArgs>();
    }

    public SocketAsyncEventArgs Get()
    {
        if (_pool.TryPop(out SocketAsyncEventArgs args))
        {
            return args;
        }
        
        return new SocketAsyncEventArgs();
    }

    public void Return(SocketAsyncEventArgs item)
    {
        _pool.Push(item);
    }

    public void Dispose()
    {
        while (_pool.TryPop(out SocketAsyncEventArgs args))
        {
            args.Dispose();
        }
    }
}