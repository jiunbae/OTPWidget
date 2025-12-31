using ObjCRuntime;
using UIKit;

namespace OtpAuthenticator.macOS;

public class Program
{
    static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
