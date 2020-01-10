﻿using EluneBot.Interfaces;
using EluneBot.Services.Interfaces;
using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace TestBase
{
    [Export(typeof(IBase))]
    public class TestBase : IBase
    {
        readonly ILoggerService logger;
        readonly IObjectManagerService objectManager;

        [ImportingConstructor]
        public TestBase(
            [Import]ILoggerService logger,
            [Import]IObjectManagerService objectManager)
        {
            this.logger = logger;
            this.objectManager = objectManager;
        }

        CancellationTokenSource cts;
        CancellationToken token;

        public string Name => "TestBase";

        public Version Version => new Version(0, 0, 0, 0);

        public string Author => "krycess";

        public void Dispose()
        {

        }

        public void Start()
        {
            cts = new CancellationTokenSource();
            token = cts.Token;
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    await logger.GeneralLog(objectManager.LocalPlayer.MaxHealth);
                    token.ThrowIfCancellationRequested();
                    await Task.Delay(100);
                }
            }, token);
        }

        public void Stop()
        {
            cts.Cancel();
        }

        public void ToggleGUI()
        {

        }
    }
}
