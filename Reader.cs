using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace RustRcon
{
    /// <summary>
    /// Continuous reading of RCON packages.
    /// </summary>
    class Reader
    {
        NetworkStream stream = null;

        /// <summary>
        /// Get or set the running state. Set to false to abort.
        /// </summary>
        public bool Running = true;

        /// <summary>
        /// Initialise continuous reader.
        /// </summary>
        /// <param name="stream"></param>
        public Reader(NetworkStream stream)
        {
            this.stream = stream;
        }

        /// <summary>
        /// Start the reader.
        /// </summary>
        public void Run(Rcon rcon)
        {
            while (Running)
            {
                Int32 size = readSize();
                Int32 id = readId();
                Int32 type = readType();
                string body = readEnd();
                if (id == 1) // Ignore all packages with ID 1 (buffer duplicate)
                    continue;
                rcon.UpdatePackage(new Package(id, type, body));
            }
        }

        private byte[] read(int amount)
        {
            byte[] read = new byte[amount];
            int answer = stream.Read(read, 0, read.Length);
            if (answer != read.Length)
            {
                if (answer != 0)
                    Console.WriteLine("# Expected {0} but got {1} bytes.", amount, answer);

                byte[] trim = new byte[answer];
                for (int i = 0; i < answer; i++)
                    trim[i] = read[i];
                read = trim;
            }
            return read;
        }

        private Int32 readInt32()
        {
            byte[] read = this.read(4);

            if (read.Length != 4)
                throw new IllegalProtocolException();

            return BitConverter.ToInt32(read, 0);
        }

        private Int32 readSize()
        {
            byte[] read = this.read(4);

            if (read.Length != 4)
                throw new IllegalProtocolException();

            if (read[0] == 239 && read[1] == 191) // This is an issue in the protocol
            {
                read[0] = read[2];
                read[1] = read[3];
                byte[] add = this.read(2);

                if (add.Length != 2)
                    throw new IllegalProtocolException();

                read[2] = add[0];
                read[3] = add[1];
            }

            return BitConverter.ToInt32(read, 0);
        }

        private Int32 readId()
        {
            return readInt32();
        }

        private Int32 readType()
        {
            return readInt32();
        }

        private string readEnd()
        {
            string body = "";
            while (!body.EndsWith("\x00"))
                body += Encoding.UTF8.GetString(new byte[] { (byte)stream.ReadByte() });
            body = body.Substring(0, body.Length - 1);

            byte[] end = read(1);
            if (end[0] != 0)
                throw new IllegalProtocolException();

            return body;
        }
    }
}
