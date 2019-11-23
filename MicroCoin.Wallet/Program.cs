using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Avalonia;
using Avalonia.Logging.Serilog;
using MicroCoin.Cryptography;
using MicroCoin.Types;

namespace MicroCoin.Wallet
{
    class Program
    {
        static BigInteger ModInverse(BigInteger a, BigInteger n)
        {
            BigInteger i = n, v = 0, d = 1;
            while (a > 0)
            {
                BigInteger t = i / a, x = a;
                a = i % x;
                i = x;
                x = d;
                d = v - t * x;
                v = x;
            }
            v %= n;
            if (v < 0) v = (v + n) % n;
            return v;
        }

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            ECKeyPair pair = ECKeyPair.Import("0000000000000000000000000000000000000000000000000000000000000001");
            var x1 = new BigInteger(pair.X) ;
            var y = new BigInteger(pair.Y);
            Debug.WriteLine((Hash)pair.PublicKey.X);
            pair = ECKeyPair.Import("0000000000000000000000000000000000000000000000000000000000000002");
            var x2 = new BigInteger(pair.X);
            Debug.WriteLine((Hash)pair.PublicKey.X);
            pair = ECKeyPair.Import("0000000000000000000000000000000000000000000000000000000000000003");
            var x3 = new BigInteger(pair.X);
            Debug.WriteLine((Hash)pair.PublicKey.X);
            var g  = BigInteger.Parse("79BE667EF9DCBBAC55A06295CE870B07029BFCDB2DCE28D959F2815B16F81798", System.Globalization.NumberStyles.HexNumber);
            var y1 = BigInteger.Parse("483ADA7726A3C4655DA4FBFC0E1108A8FD17B448A68554199C47D08FFB10D4B8", System.Globalization.NumberStyles.HexNumber);
            var p  = BigInteger.Parse("115792089237316195423570985008687907853269984665640564039457584007908834671663", System.Globalization.NumberStyles.Integer);
            byte[] pb = p.ToByteArray();
            var s = (g * g * 3 * ModInverse(2 * y1, p));
            s = (s * s - 2 * g) % p;
            Debug.WriteLine((Hash)(s.ToByteArray().Take(32).Reverse().ToArray()));
            Debug.WriteLine(x1);
            Debug.WriteLine(x2);
            Debug.WriteLine(x3);
            Debug.WriteLine(x2 / BigInteger.Parse("55066263022277343669578718895168534326250603453777594175500187360389116729240",System.Globalization.NumberStyles.Integer));
            Debug.WriteLine(x1 / BigInteger.Parse("32670510020758816978083085130507043184471273380659243275938904335757337482424", System.Globalization.NumberStyles.Integer));
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()      
                .LogToDebug();
    }
}
