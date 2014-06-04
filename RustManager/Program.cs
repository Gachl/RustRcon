using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RustRcon;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace RustManager
{
    class Program
    {
        static void Main(string[] args)
        {
            // Initialise connection
            Rcon rcon = new Rcon("rust.yourserver.com", 26101, "your rcon passphrase");
            
            // Single command
            rcon.Send("say \"Thank [color#FF0000]you [color#FFFFFF]for choosing this RCON class.\"");

            // Command with (multipart) reply
            Package command = new Package("banlistex");
            command.RegisterCallback((Package result) => { rcon.Send(String.Format("say \"There are {0} entries on your ban list.\"", result.Response.Split('\n').Length)); }); // Note that result and command are the same object.

            // Handle passive traffic (eg. chat, join and quit messages, ...)
            Task passive = new Task(() =>
                {
                    while (true)
                    {
                        Package package = rcon.ReadPackage();
                        if (Regex.IsMatch(package.Response, "^\\[CHAT\\] \\\".*\\\":\\\".*\\\"$"))
                            Console.WriteLine("Chat message received.");
                        else if (Regex.IsMatch(package.Response, "^User Connected: .* \\(\\d+\\)$"))
                            Console.WriteLine("User connected.");
                    }
                });
            passive.Start();
            Task.WaitAll(new Task[] { passive });
        }
    }
}
