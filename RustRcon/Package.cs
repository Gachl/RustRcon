using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RustRcon
{
    /// <summary>
    /// Type of package, can either be Authentication for sending the RCON passphrase, Normal for regular RCON commands or Validation for validating a full package has been received.
    /// </summary>
    public enum PackageType
	{
        Auth,
        Normal,
        Validation
	}

    /// <summary>
    /// Handle requests to and replies from the server.
    /// </summary>
    public class Package
    {
        private static Int32 id_counter = 2; // Make sure not to start at 0 or 1 as those are reserved IDs.
        private Int32 id = -1;
        private int type = -1;
        private string content;
        private Package validationPackage = null;
        private bool complete = false;
        private string response = "";

        private List<Action<Package>> callbacks;

        // Answer
        /// <summary>
        /// Server response constructor.
        /// </summary>
        /// <param name="id">Response ID</param>
        /// <param name="type">Response type</param>
        /// <param name="content">Response content</param>
        public Package(Int32 id, Int32 type, string content)
        {
            this.id = id;
            this.type = type;
            this.response = content;
        }

        // Request
        /// <summary>
        /// Create a new request to send to the server. The content will be sent to the server.
        /// </summary>
        /// <param name="content">Message to send to the server</param>
        /// <param name="type">Message type, Normal, Auth or Validation</param>
        public Package(string content, PackageType type = PackageType.Normal)
        {
            this.id = Package.id_counter++;
            this.content = content;

            this.type = 2;
            if (type == PackageType.Auth)
                this.type = 3;

            if (type != PackageType.Validation)
                this.validationPackage = new Package("", PackageType.Validation);

            this.callbacks = new List<Action<Package>>();
        }

        /// <summary>
        /// Get the ID of this package.
        /// </summary>
        public Int32 ID
        {
            get { return this.id; }
        }

        /// <summary>
        /// Get the raw RCON type of this package.
        /// </summary>
        public int Type
        {
            get { return this.type; }
        }

        /// <summary>
        /// Get the content of this package.
        /// </summary>
        public string Content
        {
            get { return this.content; }
        }

        /// <summary>
        /// Get or set the server response of this package.
        /// </summary>
        public string Response
        {
            get { return this.response; }
            set { this.response = value; }
        }

        /// <summary>
        /// Get the package that is used for validating this package.
        /// </summary>
        public Package ValidationPackage
        {
            get { return this.validationPackage; }
        }

        /// <summary>
        /// Get or set whether or not the package has been validated and is complete and ready to use and start the callback process if necessary.
        /// </summary>
        public bool Complete
        {
            get { return this.complete; }
            set
            {
                this.complete = value;
                if (this.complete)
                    this.callback();
            }
        }

        /// <summary>
        /// Register a callback to be called as soon as the package has validated, with the package itself as parameter.
        /// </summary>
        /// <param name="callback">Action to call</param>
        public void RegisterCallback(Action<Package> callback)
        {
            this.callbacks.Add(callback);
        }

        private void callback()
        {
            foreach (Action<Package> callback in this.callbacks)
                new Task(() => { callback(this); }).Start();
        }
    }
}
