﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using NUnit.Framework;
using Kayak;
using System.Threading;
using Kayak.Tests.Net;
using Kayak.Tests;

namespace Kayak.Tests.Net
{
    // TODO
    // - closes connection if no OnConnection listeners
    // - raises OnClose after being closed.
    class ServerTests
    {
        ManualResetEventSlim wh;
        IServer server;

        public IPEndPoint LocalEP(int port)
        {
            return new IPEndPoint(IPAddress.Loopback, port);
        }

        [SetUp]
        public void SetUp()
        {
            wh = new ManualResetEventSlim();
            var schedulerDelegate = new SchedulerDelegate();
            schedulerDelegate.OnStoppedAction = () => wh.Set();
            var scheduler = new KayakScheduler(schedulerDelegate);

            var serverDelegate = new ServerDelegate();
            server = KayakServer.Factory.Create(serverDelegate, scheduler);
        }

        [TearDown]
        public void TearDown()
        {
            wh.Dispose();
            server.Dispose();
        }

        [Test]
        public void Listen_after_listen_throws_exception()
        {
            Exception e = null;

            var ep = LocalEP(Config.Port);
            server.Listen(ep);

            try
            {
                server.Listen(LocalEP(Config.Port));
            }
            catch (Exception ex)
            {
                e = ex;
            }

            Assert.That(e, Is.Not.Null);
            Assert.That(e.GetType(), Is.EqualTo(typeof(InvalidOperationException)));
            Assert.That(e.Message, Is.EqualTo("The server was already listening."));
        }
    }
}
