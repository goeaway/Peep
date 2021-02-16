﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Crawler.Options
{
    public class MessagingOptions
    {
        public const string Key = "Messaging";

        public string Hostname { get; set; }
        public string Exchange { get; set; }

        public string QueueRoutingKey { get; set; }
        public string CancelRoutingKey { get; set; }
    }
}