using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using RabbitMQ.Client;

namespace Backoffice.Shared.Models
{
    public class MessageBrokerCredentialModel
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string VirtualHost { get; set; }
        public string Exchange { get; set; }
        public SslOption Ssl { get; set; }
        public string Queue { get; set; }
        public string Consumer { get; set; }
        public string RoutingKey { get; set; }
        public bool IsExclusive { get; set; } = false;
        public bool IsPriority { get; set; } = false;
        public string PrefetchCount { get; set; } = "";
    }
}
