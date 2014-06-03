using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace RustRcon
{
    /// <summary>
    /// Manage the remote connection to a Rust server.
    /// </summary>
    public class Rcon
    {
        private TcpClient client = null;
        private NetworkStream stream = null;
        private Reader reader = null;

        private List<Package> packages;

        /// <summary>
        /// Initialise new remote connection to a specified server.
        /// </summary>
        /// <param name="host">IP, hostname or FQDN of the host</param>
        /// <param name="port">Query port, usualy game port + 1</param>
        /// <param name="rconPassword">RCON passphrase</param>
        public Rcon(string host, int port, string rconPassword)
        {
            if (String.IsNullOrEmpty(host) || String.IsNullOrEmpty(rconPassword) || port < 1 || port > short.MaxValue)
                throw new ArgumentException("Not all arguments have been supplied.");

            this.packages = new List<Package>();

            this.client = new TcpClient();
            this.client.Connect(host, port);

            if (!this.client.Connected)
                throw new SocketException((int)SocketError.ConnectionRefused);

            this.stream = this.client.GetStream();
            this.reader = new Reader(this.stream);
            Task readerTask = new Task(() => { this.reader.Run(this); });
            readerTask.Start();

            this.SendPackage(new Package(rconPassword, PackageType.Auth));
        }

        /// <summary>
        /// Process received packages from server
        /// </summary>
        /// <param name="package">Package from the server</param>
        public void UpdatePackage(Package package)
        {
            if (package.ID == 1)
                return;

            if (package.ID > 1)
            {
                // Validating package?
                Package validator = this.packages.Where(match => match.ValidationPackage != null && match.ValidationPackage.ID == package.ID).FirstOrDefault();
                if (validator != null) // Validator received, completing package
                {
                    if (validator.Complete)
                        throw new IllegalProtocolException();

                    validator.Complete = true;
                    return;
                }

                // Matching package?
                Package matched = this.packages.Where(match => match.ID == package.ID).FirstOrDefault();
                if (matched != null) // Found a matching package
                {
                    matched.Response += package.Response; // Append response to original
                    return;
                }

                // It's either with an ID > 1
                throw new IllegalProtocolException();
            }

            // Passive traffic
            this.packages.Add(package);
        }

        /// <summary>
        /// Read any passive package in order
        /// </summary>
        /// <returns></returns>
        public Package ReadPackage()
        {
            return ReadPackage(0);
        }

        /// <summary>
        /// Read a specific package by ID
        /// </summary>
        /// <param name="id">ID of the package</param>
        /// <returns>Package if found, null of not</returns>
        public Package ReadPackage(int id)
        {
            List<Package> matches = this.packages.Where(match => match.ID == id).ToList();
            if (matches.Count == 0)
                return null;

            Package matched = matches[0];
            if (id == 0 || matched.Complete)
                this.packages.Remove(matched); // Discard if completed

            return matched;
        }

        /// <summary>
        /// Send a package to the server.
        /// </summary>
        /// <param name="package">Package to send to the server.</param>
        public void SendPackage(Package package)
        {
            byte[] send = package.GetBytes();
            this.stream.Write(send, 0, send.Length);
            if (package.ValidationPackage != null)
            {
                send = package.ValidationPackage.GetBytes();
                this.stream.Write(send, 0, send.Length);
            }
            this.stream.Flush();
            this.packages.Add(package);
        }
    }
}
